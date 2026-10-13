// SAMPLE 1 — Database-backed, permission-based authorization (ASP.NET Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: endpoints declare [RequirePermission("X")] or [RequireAnyPermission("A","B")].
// A custom IAuthorizationPolicyProvider builds "AnyPermission:A,B" policies on demand, and the
// handlers check the user's CURRENT role permissions in the database on every request, so
// revoking a permission or disabling a user takes effect immediately (nothing is baked into the JWT).
// 
// This file: the requirement object carrying the permission codes.
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;

namespace CafeManagement.Infrastructure.Authorization;

/// <summary>يتحقق أن للمستخدم صلاحية واحدة على الأقل من مجموعة صلاحيات (OR بينها).</summary>
public sealed class AnyPermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> PermissionCodes { get; }

    public AnyPermissionRequirement(IEnumerable<string> permissionCodes)
    {
        PermissionCodes = permissionCodes.ToArray();
    }
}
