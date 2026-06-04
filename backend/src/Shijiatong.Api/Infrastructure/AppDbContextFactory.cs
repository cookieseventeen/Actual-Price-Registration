using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shijiatong.Api.Infrastructure;

/// <summary>
/// 設計時 (design-time) factory — 供 `dotnet ef migrations` 使用，
/// 不需真正連線，連線字串僅供 provider 推斷型別。
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=shijiatong;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
