using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Cortex.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(value!);
    }
}