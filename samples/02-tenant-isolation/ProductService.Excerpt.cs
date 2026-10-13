// SAMPLE 2 — Multi-tenant data isolation (EF Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: every business entity belongs to one café (CafeId). The café is resolved on the
// server from the authenticated user — never accepted from the client — and every query and
// lookup-by-id is scoped by it, so a user cannot read or modify another café's records by guessing IDs.
// 
// This file: excerpt — list and delete are both scoped by the resolved CafeId.
// ---------------------------------------------------------------------------

    public async Task<IReadOnlyList<ProductListItemDto>> GetListAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var cafeId = await UserCafeProvider.GetCafeIdAsync(db, currentUserId, cancellationToken);
        var products = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.InventoryRecipeLines)
                .ThenInclude(l => l.InventoryItem)
            .Where(p => p.CafeId == cafeId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return products.Select(Map).ToList();
    }

    // ...

    public async Task DeleteAsync(Guid currentUserId, Guid id, CancellationToken cancellationToken = default)
    {
        var cafeId = await UserCafeProvider.GetCafeIdAsync(db, currentUserId, cancellationToken);
        var entity = await db.Products
            .Include(p => p.InventoryRecipeLines)
            .FirstOrDefaultAsync(p => p.Id == id && p.CafeId == cafeId, cancellationToken)
            ?? throw new AppException("NOT_FOUND", "المنتج غير موجود.");
