// SAMPLE 1 — Database-backed, permission-based authorization (ASP.NET Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: endpoints declare [RequirePermission("X")] or [RequireAnyPermission("A","B")].
// A custom IAuthorizationPolicyProvider builds "AnyPermission:A,B" policies on demand, and the
// handlers check the user's CURRENT role permissions in the database on every request, so
// revoking a permission or disabling a user takes effect immediately (nothing is baked into the JWT).
// 
// This file: attribute for a single required permission.
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;

namespace CafeManagement.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        Policy = $"Permission:{permissionCode}";
    }
}
