using System.Net.Http.Headers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shijiatong.Api.Tests.Auth;
using Shijiatong.Lvr;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Shijiatong.Api.Tests;

/// <summary>共用 Testcontainers（Postgres + Redis）與 WebApplicationFactory。</summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    static IntegrationTestFixture()
    {
        // DinD（dotnet SDK 容器掛 docker.sock）下 Ryuk 常無法啟動；改由 Dispose 手動清理
        TestcontainersSettings.ResourceReaperEnabled = false;
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("shijiatong_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .WithCleanUp(true)
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    /// <summary>以 <c>Auth:Enabled=true</c> 建立獨立 client（其餘連線設定與預設 fixture 相同）。</summary>
    public HttpClient CreateAuthEnabledClient() =>
        _factory!.WithWebHostBuilder(b => b.UseSetting("Auth:Enabled", "true")).CreateClient();

    /// <summary>
    /// Stage 2：Auth 全開且保護 admin 端點；可選帶入 Bearer token（測試用 SigningKey 與 TestJwtFactory 一致）。
    /// </summary>
    public HttpClient CreateAdminProtectedClient(string? bearerToken = null)
    {
        var client = _factory!.WithWebHostBuilder(b =>
        {
            b.UseSetting("Auth:Enabled", "true");
            b.UseSetting("Auth:ProtectAdminEndpoints", "true");
            b.UseSetting("Auth:Jwt:SigningKey", TestJwtFactory.SigningKey);
            b.UseSetting("Auth:Jwt:Issuer", TestJwtFactory.Issuer);
            b.UseSetting("Auth:Jwt:Audience", TestJwtFactory.Audience);
        }).CreateClient();

        if (bearerToken is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return client;
    }

    /// <summary>
    /// Stage 3：Auth 全開（含 Mock SSO）；可選帶入 Bearer token。
    /// </summary>
    public HttpClient CreateStage3Client(string? bearerToken = null)
    {
        var client = _factory!.WithWebHostBuilder(b =>
        {
            b.UseSetting("Auth:Enabled", "true");
            b.UseSetting("Auth:ProtectAdminEndpoints", "true");
            b.UseSetting("Auth:MockSsoEnabled", "true");
            b.UseSetting("Auth:AdminEmails:0", TestAuthSettings.Stage3AdminEmail);
            b.UseSetting("Auth:Jwt:SigningKey", TestJwtFactory.SigningKey);
            b.UseSetting("Auth:Jwt:Issuer", TestJwtFactory.Issuer);
            b.UseSetting("Auth:Jwt:Audience", TestJwtFactory.Audience);
        }).CreateClient();

        if (bearerToken is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return client;
    }

    /// <summary>
    /// 實價登錄匯入：開 <c>Lvr:ImportEnabled</c>，並以假檔案來源取代真下載（不連外網）。
    /// <paramref name="protect"/> 時同時開 admin 保護（可帶 Bearer token）。
    /// </summary>
    public HttpClient CreateLvrImportClient(ILvrFileSource source, bool protect = false, string? bearerToken = null)
    {
        var client = _factory!.WithWebHostBuilder(b =>
        {
            b.UseSetting("Lvr:ImportEnabled", "true");
            if (protect)
            {
                b.UseSetting("Auth:Enabled", "true");
                b.UseSetting("Auth:ProtectAdminEndpoints", "true");
                b.UseSetting("Auth:Jwt:SigningKey", TestJwtFactory.SigningKey);
                b.UseSetting("Auth:Jwt:Issuer", TestJwtFactory.Issuer);
                b.UseSetting("Auth:Jwt:Audience", TestJwtFactory.Audience);
            }
            b.ConfigureTestServices(s => s.AddSingleton(source));
        }).CreateClient();

        if (bearerToken is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return client;
    }

    public async Task InitializeAsync()
    {
        // dotnet SDK 容器 + 掛載 docker.sock（DinD）：Ryuk 常失敗，改由測試結束時手動 Dispose 容器
        Environment.SetEnvironmentVariable("DOCKER_HOST", "unix:///var/run/docker.sock");
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE")))
            Environment.SetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE", "host.docker.internal");

        await _postgres.StartAsync();
        await _redis.StartAsync();

        // DinD：測試行程在 SDK 容器內，須連 host 上映射埠，不能用容器內部 hostname
        var host = Environment.GetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE") ?? "host.docker.internal";
        var pgConn =
            $"Host={host};Port={_postgres.GetMappedPublicPort(5432)};Database=shijiatong_test;Username=test;Password=test";
        var redisConn = $"{host}:{_redis.GetMappedPublicPort(6379)}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Postgres", pgConn);
                builder.UseSetting("ConnectionStrings:Redis", redisConn);
                // 工廠預設 Development 環境會開匯入；基礎 client 一律關閉，避免任何測試連到真下載
                builder.UseSetting("Lvr:ImportEnabled", "false");
            });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        await _postgres.DisposeAsync().AsTask();
        await _redis.DisposeAsync().AsTask();
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration";
}
