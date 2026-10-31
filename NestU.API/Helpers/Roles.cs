namespace NestU.API.Helpers;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Landlord = "Landlord";
    public const string Tenant = "Tenant";
    public static readonly string[] All = { Admin, Landlord, Tenant };
}

public static class Policies
{
    /// <summary>Landlord đã được Admin duyệt hồ sơ.</summary>
    public const string ApprovedLandlord = "ApprovedLandlord";
}
