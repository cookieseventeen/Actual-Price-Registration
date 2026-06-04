namespace Shijiatong.Api.Domain.Entities;

public enum CrawlStatus { Done, Running, Queued, Error }

/// <summary>資料來源 / 爬蟲任務狀態，對應前端 CrawlTask。</summary>
public class CrawlTask
{
    public string Id { get; set; } = default!;        // C01
    public string DistrictName { get; set; } = default!;
    public CrawlStatus Status { get; set; }
    public int Records { get; set; }
    public DateTime? LastRun { get; set; }            // running 時為 null
    public DateTime? Next { get; set; }
    public string Duration { get; set; } = default!;  // 4分12秒 / —
}
