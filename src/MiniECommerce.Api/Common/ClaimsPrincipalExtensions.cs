using System.Security.Claims;

namespace MiniECommerce.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token içerisinde kullanıcı kimlik bilgisi bulunamadı.");

        return int.Parse(idClaim.Value);
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}