// SAMPLE 1 — Database-backed, permission-based authorization (ASP.NET Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: endpoints declare [RequirePermission("X")] or [RequireAnyPermission("A","B")].
// A custom IAuthorizationPolicyProvider builds "AnyPermission:A,B" policies on demand, and the
// handlers check the user's CURRENT role permissions in the database on every request, so
// revoking a permission or disabling a user takes effect immediately (nothing is baked into the JWT).
// 
// This file: policy provider that builds OR-policies at runtime and delegates everything else to the default provider.
// ---------------------------------------------------------------------------

using CafeManagement.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CafeManagement.Api.Authorization;

/// <summary>
/// يبني سياسات "AnyPermission:CODE1,CODE2,…" ديناميكيًا عند الطلب (OR بين عدة صلاحيات)
/// حتى لا نحتاج لتسجيل كل توليفة صلاحيات مسبقًا في Program.cs،
/// ويُفوّض أي سياسة أخرى (مثل "Permission:CODE" المُسجّلة مسبقًا) لمزوّد ASP.NET الافتراضي.
/// </summary>
public sealed class AnyPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "AnyPermission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public AnyPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var codes = policyName[Prefix.Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new AnyPermissionRequirement(codes))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
