using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public class MarketplaceService : IMarketplaceService
    {
        private readonly ApplicationDbContext _context;

        public MarketplaceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> SearchProductsAsync(string query, string? marketplace = null)
        {
            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.Title.Contains(query) || 
                                                        (p.Description != null && p.Description.Contains(query)));
            }

            if (!string.IsNullOrEmpty(marketplace))
            {
                productsQuery = productsQuery.Where(p => p.Marketplace == marketplace);
            }

            return await productsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<Deal>> FindDealsAsync()
        {
            return await _context.Deals
                .Include(d => d.Product)
                .OrderByDescending(d => d.DealScore)
                .Take(20)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<List<Product>> GetRecentProductsAsync(int count = 10)
        {
            return await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
