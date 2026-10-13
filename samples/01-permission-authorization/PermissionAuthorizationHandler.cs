// SAMPLE 1 — Database-backed, permission-based authorization (ASP.NET Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: endpoints declare [RequirePermission("X")] or [RequireAnyPermission("A","B")].
// A custom IAuthorizationPolicyProvider builds "AnyPermission:A,B" policies on demand, and the
// handlers check the user's CURRENT role permissions in the database on every request, so
// revoking a permission or disabling a user takes effect immediately (nothing is baked into the JWT).
// 
// This file: handler for a single permission (active user + role lookup in the DB).
// ---------------------------------------------------------------------------

using System.Security.Claims;
using CafeManagement.Domain.Enums;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(AppDbContext db) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
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

        var hasPermission = await db.RolePermissions.AsNoTracking()
            .AnyAsync(rp => rp.RoleId == user.RoleId &&
                            rp.Permission.Code == requirement.PermissionCode);

        if (hasPermission)
            context.Succeed(requirement);
    }
}
