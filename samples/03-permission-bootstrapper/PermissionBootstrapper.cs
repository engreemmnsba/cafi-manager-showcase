// SAMPLE 3 — Idempotent permission bootstrapper (runs on every API start)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: new permission codes added in code reach production automatically and are granted
// to built-in Admin roles. When a coarse permission is split into finer ones, GrantImpliedPermissionsAsync
// gives every role that had the old permission the new ones, so no user silently loses access after a
// release. Safe to run repeatedly — it only inserts what is missing.
// ---------------------------------------------------------------------------

using CafeManagement.Application.Security;
using CafeManagement.Domain.Entities;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Infrastructure.Seeding;

public static class PermissionBootstrapper
{
    /// <summary>يضيف أي صلاحيات جديدة من PermissionCodes ويربطها بأدوار Admin النظامية.</summary>
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var code in PermissionCodes.All)
        {
            var exists = await db.Permissions.AnyAsync(p => p.Code == code, cancellationToken);
            if (exists)
                continue;

            db.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Description = code
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var allPerms = await db.Permissions.ToListAsync(cancellationToken);
        var adminRoles = await db.Roles
            .Where(r => r.Name == "Admin" && r.IsSystemRole)
            .ToListAsync(cancellationToken);

        foreach (var role in adminRoles)
        {
            foreach (var p in allPerms)
            {
                var linked = await db.RolePermissions
                    .AnyAsync(x => x.RoleId == role.Id && x.PermissionId == p.Id, cancellationToken);
                if (!linked)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // ترقية توافقية: أي دور كان يملك صلاحية "إنشاء مصروف" قبل تجزئتها (2026-09) يحصل تلقائياً
        // على صلاحيات العرض/التعديل/الحذف الجديدة، حتى لا تختفي شاشة المصاريف عن الأدوار غير الأدمن.
        await GrantImpliedPermissionsAsync(
            db,
            allPerms,
            fromCode: PermissionCodes.BillingExpenseCreate,
            toCodes: [PermissionCodes.BillingExpenseView, PermissionCodes.BillingExpenseUpdate, PermissionCodes.BillingExpenseDelete],
            cancellationToken);

        // ترقية توافقية: صلاحية استهلاك العمال كانت مدمجة داخل صلاحيات المخزون (2026-09).
        // أي دور يملك INVENTORY_MANAGE يحصل تلقائياً على STAFF_CONSUMPTION_MANAGE (+ العرض)،
        // وأي دور يملك INVENTORY_VIEW يحصل تلقائياً على STAFF_CONSUMPTION_VIEW، حتى لا تختفي
        // شاشة استهلاك العمال عن أي دور كان يستخدمها قبل فصل الصلاحية.
        await GrantImpliedPermissionsAsync(
            db,
            allPerms,
            fromCode: PermissionCodes.InventoryManage,
            toCodes: [PermissionCodes.StaffConsumptionManage, PermissionCodes.StaffConsumptionView],
            cancellationToken);
        await GrantImpliedPermissionsAsync(
            db,
            allPerms,
            fromCode: PermissionCodes.InventoryView,
            toCodes: [PermissionCodes.StaffConsumptionView],
            cancellationToken);
    }

    /// <summary>
    /// لأي دور يملك صلاحية <paramref name="fromCode"/> فعلياً، يضيف له أي صلاحية من <paramref name="toCodes"/>
    /// لا يملكها بعد. يُستخدم عند تجزئة صلاحية قديمة إلى عدة صلاحيات أدق دون فقدان وصول موجود.
    /// </summary>
    private static async Task GrantImpliedPermissionsAsync(
        AppDbContext db,
        List<Permission> allPerms,
        string fromCode,
        IReadOnlyList<string> toCodes,
        CancellationToken cancellationToken)
    {
        var fromPerm = allPerms.FirstOrDefault(p => p.Code == fromCode);
        if (fromPerm is null) return;

        var toPerms = allPerms.Where(p => toCodes.Contains(p.Code)).ToList();
        if (toPerms.Count == 0) return;

        var roleIdsWithFrom = await db.RolePermissions
            .Where(x => x.PermissionId == fromPerm.Id)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var roleId in roleIdsWithFrom)
        {
            foreach (var toPerm in toPerms)
            {
                var linked = await db.RolePermissions
                    .AnyAsync(x => x.RoleId == roleId && x.PermissionId == toPerm.Id, cancellationToken);
                if (!linked)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = toPerm.Id });
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
