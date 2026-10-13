// SAMPLE 2 — Multi-tenant data isolation (EF Core 8)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: every business entity belongs to one café (CafeId). The café is resolved on the
// server from the authenticated user — never accepted from the client — and every query and
// lookup-by-id is scoped by it, so a user cannot read or modify another café's records by guessing IDs.
// 
// This file: excerpt — per-tenant unique indexes and foreign keys in the EF Core model.
// ---------------------------------------------------------------------------

        modelBuilder.Entity<User>(b =>
        {

    // ...

            b.HasIndex(x => new { x.CafeId, x.Email }).IsUnique();
            b.HasOne(x => x.Cafe).WithMany(x => x.Users).HasForeignKey(x => x.CafeId).OnDelete(DeleteBehavior.Restrict);

    // ...

        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.CafeId, x.Name }).IsUnique();
            b.HasOne(x => x.Cafe).WithMany(x => x.Roles).HasForeignKey(x => x.CafeId).OnDelete(DeleteBehavior.Cascade);
        });

    // ...

        modelBuilder.Entity<Customer>(b =>
        {

    // ...

            b.HasIndex(x => new { x.CafeId, x.CustomCode });
            b.HasIndex(x => new { x.CafeId, x.PhoneNumber });
            b.HasIndex(x => new { x.CafeId, x.DeletedAt });
            b.HasOne(x => x.Cafe).WithMany(x => x.Customers).HasForeignKey(x => x.CafeId).OnDelete(DeleteBehavior.Cascade);
