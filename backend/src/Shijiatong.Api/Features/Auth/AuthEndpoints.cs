using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Contracts;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Infrastructure;
using Shijiatong.Api.Infrastructure.Auth;

namespace Shijiatong.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>($"{AuthOptions.SectionName}:MockSsoEnabled"))
            return app;

        var group = app.MapGroup("/api/auth");

        group.MapPost("/sso/{provider}", MockSso)
            .WithSummary("模擬 SSO 登入（對齊前端 mockSSO）");

        group.MapPost("/register", Register)
            .WithSummary("新會員註冊（pending）");

        group.MapGet("/me", Me)
            .RequireAuthorization()
            .WithSummary("目前登入會員");

        return app;
    }

    private static IResult? SigningKeyNotConfigured(IJwtTokenService tokens) =>
        tokens.IsConfigured
            ? null
            : Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "JWT 簽發未設定",
                detail: "請設定 Auth:Jwt:SigningKey 後再使用認證端點。");

    private static async Task<IResult> MockSso(
        string provider,
        IJwtTokenService tokens,
        MockSsoService sso,
        AppDbContext db)
    {
        if (SigningKeyNotConfigured(tokens) is { } notReady)
            return notReady;

        if (!sso.TryGetProfile(provider, out var profile))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["provider"] = ["無效值；合法值：google, apple, admin, member"],
            });
        }

        var member = await db.Members.FirstOrDefaultAsync(m =>
            m.Email.ToLower() == profile.Email.ToLower());

        if (member is null)
            return Results.Ok(profile);

        if (member.Status != MemberStatus.Active)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "帳號無法登入",
                detail: $"會員狀態為 {member.Status.ToString().ToLowerInvariant()}，僅 active 可登入。");
        }

        var token = tokens.CreateAccessToken(member);
        return Results.Ok(new SsoLoginResponse(profile.Email, profile.Name, profile.Avatar, profile.Provider, token));
    }

    private static async Task<IResult> Register(
        RegisterRequest? req,
        IJwtTokenService tokens,
        MockSsoService sso,
        AppDbContext db)
    {
        if (SigningKeyNotConfigured(tokens) is { } notReady)
            return notReady;

        if (req is null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Name)
            || string.IsNullOrWhiteSpace(req.Provider) || string.IsNullOrWhiteSpace(req.Plan)
            || string.IsNullOrWhiteSpace(req.Purpose))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["email、name、provider、plan、purpose 為必填"],
            });
        }

        if (!Enum.TryParse<Plan>(req.Plan, ignoreCase: true, out var plan))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["plan"] = [$"無效值 '{req.Plan}'；合法值：{string.Join(", ", Enum.GetNames<Plan>())}"],
            });
        }

        var provider = sso.ToDomainProvider(req.Provider);
        var email = req.Email.Trim();

        if (await db.Members.AnyAsync(m => m.Email.ToLower() == email.ToLower()))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email 已註冊",
                detail: "此 Email 已有會員資料。");
        }

        var avatar = string.IsNullOrWhiteSpace(req.Avatar)
            ? req.Name.Trim()[..1]
            : req.Avatar.Trim();

        var member = new Member
        {
            Name = req.Name.Trim(),
            Email = email,
            Avatar = avatar,
            Provider = provider,
            Plan = plan,
            Status = MemberStatus.Pending,
            Role = MemberRole.Member,
            Purpose = req.Purpose.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();

        var token = tokens.CreateAccessToken(member);
        return Results.Ok(new RegisterResponse(token, MemberDto.From(member)));
    }

    private static async Task<IResult> Me(ClaimsPrincipal user, AppDbContext db)
    {
        var sub = user.FindFirstValue(ShijiatongClaimTypes.Sub);
        if (sub is null || !Guid.TryParse(sub, out var id))
            return Results.Unauthorized();

        var member = await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        return member is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "找不到會員")
            : Results.Ok(MemberDto.From(member));
    }

    public record SsoLoginResponse(string Email, string Name, string Avatar, string Provider, string Token);

    public record RegisterRequest(
        string Email, string Name, string Avatar, string Provider, string Plan, string Purpose);

    public record RegisterResponse(string Token, MemberDto Member);
}
