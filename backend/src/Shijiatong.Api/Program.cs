using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Features.Analysis;
using Shijiatong.Api.Features.CrawlTasks;
using Shijiatong.Api.Features.Districts;
using Shijiatong.Api.Features.Health;
using Shijiatong.Api.Features.Members;
using Shijiatong.Api.Features.Transactions;
using Shijiatong.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var pgConn = builder.Configuration.GetConnectionString("Postgres")
             ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:Postgres");
var redisConn = builder.Configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:Redis");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(pgConn));

builder.Services.AddStackExchangeRedisCache(o =>
{
    o.Configuration = redisConn;
    o.InstanceName = "shijiatong:";
});

// CORS：允許前端 (Vite dev / GitHub Pages) 跨域呼叫
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi();

var app = builder.Build();

await MigrateAndSeedAsync(app);

if (app.Environment.IsDevelopment())
    app.MapOpenApi(); // /openapi/v1.json

app.UseCors();

app.MapHealthEndpoints();
app.MapDistrictEndpoints();
app.MapTransactionEndpoints();
app.MapAnalysisEndpoints();
app.MapCrawlTaskEndpoints();
app.MapMemberEndpoints();

app.Run();

// 啟動時套用 migration 並灌種子；DB 尚未就緒時退避重試（支援 compose 重啟 / 重建）。
static async Task MigrateAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 12;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await DbSeeder.SeedAsync(db);
            logger.LogInformation("資料庫 migration 與種子完成");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "資料庫尚未就緒（第 {Attempt}/{Max} 次），3 秒後重試", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

public partial class Program { }
