using AuctionService.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;

// AuctionContext is your Entity Framework Core DbContext.
// It acts as a bridge between your C# classes and your PostgreSQL database.
// DbSet<Auction> Auctions represents the Auctions table.
// When you call _context.Auctions, you’re querying or modifying that table.
public class AuctionContext : DbContext
{
    public AuctionContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Auction> Auctions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}