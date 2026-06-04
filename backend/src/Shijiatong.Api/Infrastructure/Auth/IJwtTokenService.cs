using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Infrastructure.Auth;

public interface IJwtTokenService
{
    bool IsConfigured { get; }

    string CreateAccessToken(Member member);
}
