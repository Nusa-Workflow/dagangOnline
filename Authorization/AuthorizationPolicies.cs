namespace dagangOnline.Authorization;

public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireUser = "RequireUser";
    public const string RequireMitra = "RequireMitra";
    public const string RequireAgent = "RequireAgent";
    public const string RequireUserOrAdmin = "RequireUserOrAdmin";
    public const string RequireMitraOrAdmin = "RequireMitraOrAdmin";
    public const string RequireAgentOrAdmin = "RequireAgentOrAdmin";
}
