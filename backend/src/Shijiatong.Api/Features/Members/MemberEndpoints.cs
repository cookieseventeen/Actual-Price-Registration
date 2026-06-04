using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Contracts;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.Members;

// 後台會員管理（對應前端 AdminView）。
// TODO(Phase 3)：以 JWT + admin 角色保護以下端點；認證改為真 OAuth。
public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/members");

        group.MapGet("", async (AppDbContext db) =>
        {
            var members = await db.Members.AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => MemberDto.From(m))
                .ToListAsync();
            return Results.Ok(members);
        }).WithSummary("會員列表");

        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateStatusRequest? req, AppDbContext db) =>
        {
            if (!TryParseEnum<MemberStatus>(req?.Status, out var status, out var problem))
                return problem;

            var m = await db.Members.FindAsync(id);
            if (m is null) return NotFoundMember();
            m.Status = status;
            m.ReviewedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(MemberDto.From(m));
        }).WithSummary("變更會員狀態（核准/拒絕/停權/恢復）");

        group.MapPatch("/{id:guid}/plan", async (Guid id, UpdatePlanRequest? req, AppDbContext db) =>
        {
            if (!TryParseEnum<Plan>(req?.Plan, out var plan, out var problem))
                return problem;

            var m = await db.Members.FindAsync(id);
            if (m is null) return NotFoundMember();
            m.Plan = plan;
            await db.SaveChangesAsync();
            return Results.Ok(MemberDto.From(m));
        }).WithSummary("變更會員方案");

        return app;
    }

    // 解析 enum 欄位；缺值或非法值回 RFC 9457 ValidationProblem(400)，並列出合法值。
    private static bool TryParseEnum<TEnum>(string? value, out TEnum result, out IResult problem)
        where TEnum : struct, Enum
    {
        var field = typeof(TEnum).Name == nameof(MemberStatus) ? "status" : "plan";
        var allowed = string.Join(", ", Enum.GetNames<TEnum>());

        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            problem = Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [field] = [$"必填；合法值：{allowed}"],
            });
            return false;
        }

        if (!Enum.TryParse(value, ignoreCase: true, out result))
        {
            problem = Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [field] = [$"無效值 '{value}'；合法值：{allowed}"],
            });
            return false;
        }

        problem = Results.Empty;
        return true;
    }

    private static IResult NotFoundMember() =>
        Results.Problem(statusCode: 404, title: "找不到會員");

    public record UpdateStatusRequest(string Status);
    public record UpdatePlanRequest(string Plan);
}
