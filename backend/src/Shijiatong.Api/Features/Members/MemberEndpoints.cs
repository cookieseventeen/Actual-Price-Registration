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

        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateStatusRequest req, AppDbContext db) =>
        {
            if (!Enum.TryParse<MemberStatus>(req.Status, ignoreCase: true, out var status))
                return Results.BadRequest(new { error = "invalid status" });

            var m = await db.Members.FindAsync(id);
            if (m is null) return Results.NotFound();
            m.Status = status;
            m.ReviewedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(MemberDto.From(m));
        }).WithSummary("變更會員狀態（核准/拒絕/停權/恢復）");

        group.MapPatch("/{id:guid}/plan", async (Guid id, UpdatePlanRequest req, AppDbContext db) =>
        {
            if (!Enum.TryParse<Plan>(req.Plan, ignoreCase: true, out var plan))
                return Results.BadRequest(new { error = "invalid plan" });

            var m = await db.Members.FindAsync(id);
            if (m is null) return Results.NotFound();
            m.Plan = plan;
            await db.SaveChangesAsync();
            return Results.Ok(MemberDto.From(m));
        }).WithSummary("變更會員方案");

        return app;
    }

    public record UpdateStatusRequest(string Status);
    public record UpdatePlanRequest(string Plan);
}
