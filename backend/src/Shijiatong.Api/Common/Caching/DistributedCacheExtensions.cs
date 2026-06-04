using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Shijiatong.Api.Common.Caching;

/// <summary>IDistributedCache (Redis) 的 JSON get-or-set 輔助。</summary>
public static class DistributedCacheExtensions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<T> GetOrSetAsync<T>(
        this IDistributedCache cache, string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        var cached = await cache.GetStringAsync(key);
        if (cached is not null)
            return JsonSerializer.Deserialize<T>(cached, Json)!;

        var value = await factory();
        await cache.SetStringAsync(key, JsonSerializer.Serialize(value, Json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        return value;
    }
}
