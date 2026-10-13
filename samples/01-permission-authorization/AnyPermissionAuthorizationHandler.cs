// SAMPLE 1 — Database-backed, permission-based authorization (ASP.NET Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: endpoints declare [RequirePermission("X")] or [RequireAnyPermission("A","B")].
// A custom IAuthorizationPolicyProvider builds "AnyPermission:A,B" policies on demand, and the
// handlers check the user's CURRENT role permissions in the database on every request, so
// revoking a permission or disabling a user takes effect immediately (nothing is baked into the JWT).
// 
// This file: handler for the OR case — one query checks all codes at once.
// ---------------------------------------------------------------------------

using System.Security.Claims;
using CafeManagement.Domain.Enums;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Infrastructure.Authorization;

/// <summary>ناجح إن كانت لدى المستخدم أي صلاحية واحدة من مجموعة [AnyPermissionRequirement] (OR)، أو إن كان Admin.</summary>
public sealed class AnyPermissionAuthorizationHandler(AppDbContext db) : AuthorizationHandler<AnyPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return;

        var user = await db.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Status != UserStatus.Active)
            return;

        if (string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        var hasAny = await db.RolePermissions.AsNoTracking()
            .AnyAsync(rp => rp.RoleId == user.RoleId &&
                            requirement.PermissionCodes.Contains(rp.Permission.Code));

        if (hasAny)
            context.Succeed(requirement);
    }
}
