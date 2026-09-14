namespace dagangOnline.Authorization;

public static class PermissionConstants
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    public const string MitraRead = "mitra.read";
    public const string MitraUpdate = "mitra.update";
    public const string MitraApprove = "mitra.approve";

    public const string PortfolioRead = "portfolio.read";
    public const string PortfolioCreateOwn = "portfolio.create.own";
    public const string PortfolioUpdateOwn = "portfolio.update.own";
    public const string PortfolioDeleteOwn = "portfolio.delete.own";
    public const string PortfolioPublish = "portfolio.publish";

    public const string ServicesRead = "services.read";
    public const string ServicesCreateOwn = "services.create.own";
    public const string ServicesUpdateOwn = "services.update.own";
    public const string ServicesDeleteOwn = "services.delete.own";

    public const string ContactReadOwn = "contact.read.own";
    public const string ContactUpdateOwn = "contact.update.own";
    public const string ContactAssign = "contact.assign";

    public const string ProfileReadOwn = "profile.read.own";
    public const string ProfileUpdateOwn = "profile.update.own";

    public const string AuditRead = "audit.read";

    public static readonly string[] All =
    {
        UsersRead,
        UsersCreate,
        UsersUpdate,
        UsersDelete,
        MitraRead,
        MitraUpdate,
        MitraApprove,
        PortfolioRead,
        PortfolioCreateOwn,
        PortfolioUpdateOwn,
        PortfolioDeleteOwn,
        PortfolioPublish,
        ServicesRead,
        ServicesCreateOwn,
        ServicesUpdateOwn,
        ServicesDeleteOwn,
        ContactReadOwn,
        ContactUpdateOwn,
        ContactAssign,
        ProfileReadOwn,
        ProfileUpdateOwn,
        AuditRead
    };
}
