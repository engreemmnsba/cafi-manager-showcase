// SAMPLE 2 — Multi-tenant data isolation (EF Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: every business entity belongs to one café (CafeId). The café is resolved on the
// server from the authenticated user — never accepted from the client — and every query and
// lookup-by-id is scoped by it, so a user cannot read or modify another café's records by guessing IDs.
// 
// This file: resolves the caller's café from their user id.
// ---------------------------------------------------------------------------

using CafeManagement.Application.Exceptions;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Infrastructure.Services;

internal static class UserCafeProvider
{
    public static async Task<Guid> GetCafeIdAsync(AppDbContext db, Guid userId, CancellationToken cancellationToken = default)
    {
        var cafeId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CafeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (cafeId == Guid.Empty)
            throw new AppException("NOT_FOUND_USER", "المستخدم غير موجود.");

        return cafeId;
    }
}
