using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Models;

namespace Resell_Assistant.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<PriceHistory> PriceHistory { get; set; }
        public DbSet<SearchAlert> SearchAlerts { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<UserPortfolio> UserPortfolios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.ImageUrl).HasMaxLength(1000);
                entity.Property(e => e.ProductUrl).HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.ExternalId);
            });

            modelBuilder.Entity<PriceHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.PriceHistory)
                      .HasForeignKey(e => e.ProductId);
            });

            modelBuilder.Entity<SearchAlert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SearchQuery).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MaxPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinProfit).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Deal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PotentialProfit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Score).HasColumnType("decimal(5,2)");
                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId);
            });

            modelBuilder.Entity<UserPortfolio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SellPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Profit).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId);
            });
        }
    }
}
