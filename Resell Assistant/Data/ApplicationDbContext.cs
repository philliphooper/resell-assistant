using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Models;

namespace Resell_Assistant.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<SearchAlert> SearchAlerts { get; set; }
        public DbSet<UserPortfolio> UserPortfolios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);            ConfigureProduct(modelBuilder);
            ConfigureDeal(modelBuilder);
            ConfigurePriceHistory(modelBuilder);
            ConfigureSearchAlert(modelBuilder);
            ConfigureUserPortfolio(modelBuilder);

            // Fake data removed - application now relies purely on real data from external sources
        }

        private void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ShippingCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Marketplace).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Condition).HasMaxLength(50);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Title);
                entity.HasIndex(e => e.Marketplace);
                entity.HasIndex(e => e.Price);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private void ConfigureDeal(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Deal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.PotentialProfit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.EstimatedSellPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DealScore).IsRequired();                entity.Property(e => e.Confidence).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.DealScore);
                entity.HasIndex(e => e.CreatedAt);
            });
        }        private void ConfigurePriceHistory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PriceHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Marketplace).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RecordedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.RecordedAt);
                entity.HasIndex(e => new { e.ProductId, e.Marketplace });
            });
        }

        private void ConfigureSearchAlert(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SearchAlert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SearchQuery).IsRequired().HasMaxLength(500);
                entity.Property(e => e.MinProfit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastTriggered).IsRequired(false);

                entity.HasIndex(e => e.SearchQuery);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
            });
        }        private void ConfigureUserPortfolio(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserPortfolio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.SellPrice).HasColumnType("decimal(18,2)").IsRequired(false);
                entity.Property(e => e.PurchaseDate).IsRequired();
                entity.Property(e => e.SellDate).IsRequired(false);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(1000);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PurchaseDate);
                entity.HasIndex(e => e.SellDate);
            });        }

        // Removed SeedData method - application now relies purely on real data from external sources
    }
}
