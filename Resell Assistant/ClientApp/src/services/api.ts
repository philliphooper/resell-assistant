import { Product, Deal, PriceHistory, SearchAlert, UserPortfolio, DashboardStats, ApiError } from '../types/api';

class ApiService {
  private baseUrl: string;

  constructor() {
    // Use the same domain as the current page (for .NET integration)
    this.baseUrl = window.location.origin + '/api';
  }

  private async handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
      const errorText = await response.text();
      const error: ApiError = {
        message: errorText || `HTTP ${response.status}: ${response.statusText}`,
        status: response.status,
        details: errorText
      };
      throw error;
    }

    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      return await response.json();
    }
    
    // If not JSON, return empty object
    return {} as T;
  }

  private async get<T>(endpoint: string): Promise<T> {
    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      return await this.handleResponse<T>(response);
    } catch (error) {
      console.error(`API GET ${endpoint} failed:`, error);
      throw error;
    }
  }

  private async post<T>(endpoint: string, data: any): Promise<T> {
    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
      });
      return await this.handleResponse<T>(response);
    } catch (error) {
      console.error(`API POST ${endpoint} failed:`, error);
      throw error;
    }
  }

  // Product endpoints
  async getTopDeals(): Promise<Deal[]> {
    return this.get<Deal[]>('/products/top-deals');
  }

  async getRecentProducts(count: number = 10): Promise<Product[]> {
    return this.get<Product[]>(`/products/recent?count=${count}`);
  }

  async getProduct(id: number): Promise<Product> {
    return this.get<Product>(`/products/${id}`);
  }

  async searchProducts(query: string, marketplace?: string): Promise<Product[]> {
    const params = new URLSearchParams({ query });
    if (marketplace) {
      params.append('marketplace', marketplace);
    }
    return this.get<Product[]>(`/products/search?${params.toString()}`);
  }

  async analyzeProduct(product: Product): Promise<Deal> {
    return this.post<Deal>('/products/analyze', product);
  }

  async getPriceHistory(productId: number): Promise<PriceHistory[]> {
    return this.get<PriceHistory[]>(`/products/${productId}/price-history`);
  }

  // Dashboard stats (calculated from existing data)
  async getDashboardStats(): Promise<DashboardStats> {
    try {
      // Since we don't have a dedicated stats endpoint, calculate from existing data
      const [deals, products] = await Promise.all([
        this.getTopDeals(),
        this.getRecentProducts(100) // Get more for better stats
      ]);

      const totalProducts = products.length;
      const totalDeals = deals.length;
      const totalProfit = deals.reduce((sum, deal) => sum + deal.potentialProfit, 0);
      const averageDealScore = deals.length > 0 
        ? deals.reduce((sum, deal) => sum + deal.dealScore, 0) / deals.length 
        : 0;

      // Find most common marketplace
      const marketplaceCounts = products.reduce((acc, product) => {
        acc[product.marketplace] = (acc[product.marketplace] || 0) + 1;
        return acc;
      }, {} as Record<string, number>);
      
      const topMarketplace = Object.entries(marketplaceCounts)
        .sort(([,a], [,b]) => b - a)[0]?.[0] || 'Unknown';

      // Count recent deals (last 7 days)
      const oneWeekAgo = new Date();
      oneWeekAgo.setDate(oneWeekAgo.getDate() - 7);
      const recentDealsCount = deals.filter(deal => 
        new Date(deal.createdAt) > oneWeekAgo
      ).length;

      return {
        totalProducts,
        totalDeals,
        totalProfit,
        averageDealScore: Math.round(averageDealScore),
        topMarketplace,
        recentDealsCount
      };
    } catch (error) {
      console.error('Failed to calculate dashboard stats:', error);
      // Return default stats if calculation fails
      return {
        totalProducts: 0,
        totalDeals: 0,
        totalProfit: 0,
        averageDealScore: 0,
        topMarketplace: 'N/A',
        recentDealsCount: 0
      };
    }
  }

  // Health check
  async checkHealth(): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/products/recent?count=1`);
      return response.ok;
    } catch {
      return false;
    }
  }
}

// Export singleton instance
export const apiService = new ApiService();
export default apiService;
