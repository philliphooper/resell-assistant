using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Models;
using Resell_Assistant.Models.Configuration;

namespace Resell_Assistant.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }        public DbSet<Product> Products { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<ComparisonListing> ComparisonListings { get; set; }
        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<SearchAlert> SearchAlerts { get; set; }
        public DbSet<UserPortfolio> UserPortfolios { get; set; }
        public DbSet<ApiCredentials> ApiCredentials { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            base.OnModelCreating(modelBuilder);            ConfigureProduct(modelBuilder);
            ConfigureDeal(modelBuilder);
            ConfigureComparisonListing(modelBuilder);
            ConfigurePriceHistory(modelBuilder);
            ConfigureSearchAlert(modelBuilder);
            ConfigureUserPortfolio(modelBuilder);
            ConfigureApiCredentials(modelBuilder);

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
        }        private void ConfigureDeal(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Deal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.PotentialProfit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.EstimatedSellPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DealScore).IsRequired();
                entity.Property(e => e.Confidence).IsRequired();
                entity.Property(e => e.TotalListingsAnalyzed).IsRequired();
                entity.Property(e => e.DiscoveryMethod).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.ComparisonListings)
                    .WithOne(cl => cl.Deal)
                    .HasForeignKey(cl => cl.DealId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.DealScore);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private void ConfigureComparisonListing(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ComparisonListing>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DealId).IsRequired();
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.ShippingCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Marketplace).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Condition).HasMaxLength(50);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Url).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl).HasMaxLength(1000);
                entity.Property(e => e.IsSelectedDeal).IsRequired();
                entity.Property(e => e.RankingPosition).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Deal)
                    .WithMany(d => d.ComparisonListings)
                    .HasForeignKey(e => e.DealId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.DealId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.Marketplace);
                entity.HasIndex(e => e.Price);
                entity.HasIndex(e => e.IsSelectedDeal);
                entity.HasIndex(e => e.RankingPosition);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private void ConfigurePriceHistory(ModelBuilder modelBuilder)
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
                entity.HasIndex(e => e.SellDate);            });        }

        private void ConfigureApiCredentials(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiCredentials>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Service).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EncryptedClientId).IsRequired();
                entity.Property(e => e.EncryptedClientSecret).IsRequired();
                entity.Property(e => e.Environment).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Service).IsUnique();
            });
        }

        // Removed SeedData method - application now relies purely on real data from external sources
    }
}
