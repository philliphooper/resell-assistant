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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureProduct(modelBuilder);
            ConfigureDeal(modelBuilder);
            ConfigurePriceHistory(modelBuilder);
            ConfigureSearchAlert(modelBuilder);
            ConfigureUserPortfolio(modelBuilder);

            // Seed initial data
            SeedData(modelBuilder);
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

                entity.HasOne<Product>()
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
        }

        private void ConfigureUserPortfolio(ModelBuilder modelBuilder)
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

                entity.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PurchaseDate);
                entity.HasIndex(e => e.SellDate);
            });
        }        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed some sample products for testing
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Title = "iPhone 13 Pro 128GB Unlocked",
                    Description = "Excellent condition iPhone 13 Pro",
                    Price = 650.00m,
                    ShippingCost = 15.00m,
                    Marketplace = "eBay",
                    Condition = "Used - Excellent",
                    Location = "New York, NY",
                    Url = "https://example.com/iphone13pro",
                    ImageUrl = "https://example.com/image1.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Product
                {
                    Id = 2,
                    Title = "MacBook Air M1 256GB",
                    Description = "2020 MacBook Air with M1 chip",
                    Price = 850.00m,
                    ShippingCost = 25.00m,
                    Marketplace = "Facebook Marketplace",
                    Condition = "Used - Good",
                    Location = "Los Angeles, CA",
                    Url = "https://example.com/macbook",
                    ImageUrl = "https://example.com/image2.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Product
                {
                    Id = 3,
                    Title = "Sony PlayStation 5 Console",
                    Description = "Brand new PS5 console in original packaging",
                    Price = 450.00m,
                    ShippingCost = 20.00m,
                    Marketplace = "Craigslist",
                    Condition = "New",
                    Location = "Chicago, IL",
                    Url = "https://example.com/ps5",
                    ImageUrl = "https://example.com/image3.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Product
                {
                    Id = 4,
                    Title = "AirPods Pro 2nd Generation",
                    Description = "Sealed AirPods Pro with active noise cancellation",
                    Price = 180.00m,
                    ShippingCost = 10.00m,
                    Marketplace = "eBay",
                    Condition = "New",
                    Location = "Austin, TX",
                    Url = "https://example.com/airpods",
                    ImageUrl = "https://example.com/image4.jpg",
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                },
                new Product
                {
                    Id = 5,
                    Title = "Nintendo Switch OLED",
                    Description = "White Nintendo Switch OLED console with joy-cons",
                    Price = 280.00m,
                    ShippingCost = 15.00m,
                    Marketplace = "Facebook Marketplace",
                    Condition = "Used - Like New",
                    Location = "Seattle, WA",
                    Url = "https://example.com/nintendo",
                    ImageUrl = "https://example.com/image5.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Product
                {
                    Id = 6,
                    Title = "iPad Air 5th Gen 64GB",
                    Description = "Space Gray iPad Air with Wi-Fi",
                    Price = 420.00m,
                    ShippingCost = 12.00m,
                    Marketplace = "eBay",
                    Condition = "Used - Very Good",
                    Location = "Miami, FL",
                    Url = "https://example.com/ipad",
                    ImageUrl = "https://example.com/image6.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Product
                {
                    Id = 7,
                    Title = "Samsung Galaxy S23 Ultra 256GB",
                    Description = "Unlocked Samsung flagship phone",
                    Price = 780.00m,
                    ShippingCost = 18.00m,
                    Marketplace = "Craigslist",
                    Condition = "Used - Good",
                    Location = "Phoenix, AZ",
                    Url = "https://example.com/samsung",
                    ImageUrl = "https://example.com/image7.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Product
                {
                    Id = 8,
                    Title = "Apple Watch Series 8 45mm",
                    Description = "GPS model Apple Watch with sport band",
                    Price = 320.00m,
                    ShippingCost = 8.00m,
                    Marketplace = "Facebook Marketplace",
                    Condition = "Used - Excellent",
                    Location = "Denver, CO",
                    Url = "https://example.com/applewatch",
                    ImageUrl = "https://example.com/image8.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                }
            );

            // Seed some sample deals
            modelBuilder.Entity<Deal>().HasData(
                new Deal
                {
                    Id = 1,
                    ProductId = 1,
                    PotentialProfit = 150.00m,
                    EstimatedSellPrice = 800.00m,
                    DealScore = 85,
                    Confidence = 78,
                    Reasoning = "iPhone 13 Pro selling below market value. High demand product with consistent resale value.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Deal
                {
                    Id = 2,
                    ProductId = 2,
                    PotentialProfit = 200.00m,
                    EstimatedSellPrice = 1050.00m,
                    DealScore = 92,
                    Confidence = 85,
                    Reasoning = "MacBook Air M1 priced significantly below retail. Strong market demand for Apple laptops.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Deal
                {
                    Id = 3,
                    ProductId = 3,
                    PotentialProfit = 100.00m,
                    EstimatedSellPrice = 550.00m,
                    DealScore = 75,
                    Confidence = 70,
                    Reasoning = "PS5 at below MSRP. Gaming console with steady demand, moderate profit margin.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Deal
                {
                    Id = 4,
                    ProductId = 4,
                    PotentialProfit = 70.00m,
                    EstimatedSellPrice = 250.00m,
                    DealScore = 88,
                    Confidence = 82,
                    Reasoning = "AirPods Pro 2nd gen at excellent price. High demand Apple accessory with strong resale market.",
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                },
                new Deal
                {
                    Id = 5,
                    ProductId = 5,
                    PotentialProfit = 65.00m,
                    EstimatedSellPrice = 345.00m,
                    DealScore = 78,
                    Confidence = 75,
                    Reasoning = "Nintendo Switch OLED priced well below retail. Popular gaming console with consistent demand.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Deal
                {
                    Id = 6,
                    ProductId = 6,
                    PotentialProfit = 180.00m,
                    EstimatedSellPrice = 600.00m,
                    DealScore = 90,
                    Confidence = 88,
                    Reasoning = "iPad Air 5th gen significantly underpriced. Apple tablets hold value well and sell quickly.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Deal
                {
                    Id = 7,
                    ProductId = 8,
                    PotentialProfit = 95.00m,
                    EstimatedSellPrice = 415.00m,
                    DealScore = 82,
                    Confidence = 79,
                    Reasoning = "Apple Watch Series 8 at competitive price. Wearables market strong with good profit margins.",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                }
            );

            // Seed multiple search alerts
            modelBuilder.Entity<SearchAlert>().HasData(
                new SearchAlert
                {
                    Id = 1,
                    SearchQuery = "iPhone 13 Pro",
                    MinProfit = 100.00m,
                    MaxPrice = 700.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new SearchAlert
                {
                    Id = 2,
                    SearchQuery = "MacBook Air M1",
                    MinProfit = 150.00m,
                    MaxPrice = 900.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new SearchAlert
                {
                    Id = 3,
                    SearchQuery = "PlayStation 5",
                    MinProfit = 80.00m,
                    MaxPrice = 500.00m,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            );
        }
    }
}
