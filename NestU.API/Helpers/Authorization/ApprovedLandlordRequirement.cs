using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.Entities.Enums;

namespace NestU.API.Helpers.Authorization;

public class ApprovedLandlordRequirement : IAuthorizationRequirement { }

/// <summary>
/// Chỉ cho qua khi user có role Landlord và LandlordProfile.ApprovalStatus = Approved.
/// Không đạt → 403.
/// </summary>
public class ApprovedLandlordHandler : AuthorizationHandler<ApprovedLandlordRequirement>
{
    private readonly NestUDbContext _db;
    public ApprovedLandlordHandler(NestUDbContext db) => _db = db;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ApprovedLandlordRequirement requirement)
    {
        if (!context.User.IsInRole(Roles.Landlord)) return;
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return;

        var approved = await _db.LandlordProfiles.AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.ApprovalStatus == ApprovalStatus.Approved);
        if (approved) context.Succeed(requirement);
    }
}
