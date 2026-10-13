using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Specifications;

public sealed class RefreshTokenByHashSpecification : Specification<AuthenticationRefreshToken>
{
    public RefreshTokenByHashSpecification(string tokenHash)
    {
        Where(refreshToken => refreshToken.TokenHash == tokenHash);
        ApplyPaging(0, 1);
    }
}
