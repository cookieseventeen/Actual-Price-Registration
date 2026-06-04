namespace Shijiatong.Api.Domain.Entities;

public enum Provider { Google, Apple }
public enum Plan { Free, Pro, Enterprise }
public enum MemberStatus { Pending, Active, Rejected, Suspended }

/// <summary>會員，對應前端 Member（SSO 註冊 + 後台審核流程）。</summary>
public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Avatar { get; set; } = default!;    // 顯示首字
    public Provider Provider { get; set; }
    public Plan Plan { get; set; }
    public MemberStatus Status { get; set; }
    public string Purpose { get; set; } = default!;   // 使用用途
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? Note { get; set; }
}
