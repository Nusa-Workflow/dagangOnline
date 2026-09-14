using System.Security.Claims;

namespace dagangOnline.Authorization;

public class ResourceAuthorizationService
{
    public bool CanAccessOwnedResource(ClaimsPrincipal principal, string resourceOwnerId, string currentUserId)
    {
        if (principal == null)
        {
            return false;
        }

        if (principal.IsInRole(RoleConstants.Admin))
        {
            return true;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) && string.Equals(userId, resourceOwnerId, StringComparison.Ordinal) && string.Equals(userId, currentUserId, StringComparison.Ordinal);
    }

    public bool CanAssignRole(ClaimsPrincipal principal, string requestedRole, string currentRole)
    {
        if (principal == null)
        {
            return false;
        }

        if (principal.IsInRole(RoleConstants.Admin))
        {
            return true;
        }

        return !principal.IsInRole(RoleConstants.Admin)
            && !string.Equals(requestedRole, RoleConstants.Admin, StringComparison.Ordinal)
            && !string.Equals(requestedRole, RoleConstants.Mitra, StringComparison.Ordinal)
            && !string.Equals(currentRole, RoleConstants.Admin, StringComparison.Ordinal);
    }
}
