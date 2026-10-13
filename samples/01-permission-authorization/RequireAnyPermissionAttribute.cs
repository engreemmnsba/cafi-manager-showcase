// SAMPLE 1 — Database-backed, permission-based authorization (ASP.NET Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: endpoints declare [RequirePermission("X")] or [RequireAnyPermission("A","B")].
// A custom IAuthorizationPolicyProvider builds "AnyPermission:A,B" policies on demand, and the
// handlers check the user's CURRENT role permissions in the database on every request, so
// revoking a permission or disabling a user takes effect immediately (nothing is baked into the JWT).
// 
// This file: attribute for "any of these permissions" (OR).
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;

namespace CafeManagement.Api.Authorization;

/// <summary>
/// يسمح بالوصول إن توفرت لدى المستخدم صلاحية واحدة على الأقل من الصلاحيات المُمرَّرة (OR بينها).
/// يُستخدم عندما تخدم نقطة نهاية واحدة أكثر من "دور" منطقي (مثال: عرض فقط، أو تنفيذ إجراء أوسع).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissionCodes)
    {
        Policy = $"AnyPermission:{string.Join(',', permissionCodes)}";
    }
}
