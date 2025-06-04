import { Product, Deal, PriceHistory, DashboardStats, ApiError } from '../types/api';
import { DealDiscoverySettings, DiscoveryProgress, ComparisonListing } from '../types/settings';

// Simple timeout helper - no shared state to avoid connection conflicts
function createTimeoutSignal(timeoutMs: number): AbortSignal {
  const controller = new AbortController();
  setTimeout(() => {
    controller.abort();
  }, timeoutMs);
  return controller.signal;
}

class ApiService {
  private baseUrl: string;
  private timeoutMs: number = 10000; // Reduced to 10 seconds for faster response

  constructor() {
    // Use the same domain as the current page (for .NET integration)
    this.baseUrl = window.location.origin + '/api';
  }

  private createTimeoutSignal(timeoutMs: number = this.timeoutMs): AbortSignal {
    return createTimeoutSignal(timeoutMs);
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
  }  private async get<T>(endpoint: string): Promise<T> {
    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Connection': 'close', // Close connections to prevent leaks
        },
        signal: this.createTimeoutSignal(),
        cache: 'no-store', // Prevent caching issues
      });
      return await this.handleResponse<T>(response);
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        const timeoutError: ApiError = {
          message: 'Request timed out after 10 seconds',
          status: 408,
          details: `GET ${endpoint} request timeout`
        };
        throw timeoutError;
      }
      console.error(`API GET ${endpoint} failed:`, error);
      throw error;
    }
  }  private async post<T>(endpoint: string, data: any): Promise<T> {
    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Connection': 'close', // Close connections to prevent leaks
        },
        body: JSON.stringify(data),
        signal: this.createTimeoutSignal(),
        cache: 'no-store', // Prevent caching issues
      });
      return await this.handleResponse<T>(response);
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        const timeoutError: ApiError = {
          message: 'Request timed out after 10 seconds',
          status: 408,
          details: `POST ${endpoint} request timeout`
        };
        throw timeoutError;
      }
      console.error(`API POST ${endpoint} failed:`, error);
      throw error;
    }
  }

  // Product endpoints
  async getTopDeals(): Promise<Deal[]> {
    return this.get<Deal[]>('/deals/top');
  }

  async discoverDeals(maxResults: number = 20): Promise<Deal[]> {
    // Use longer timeout for deal discovery (30 seconds)
    try {
      const response = await fetch(`${this.baseUrl}/deals/discover?maxResults=${maxResults}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Connection': 'close',
        },
        signal: this.createTimeoutSignal(30000), // 30 second timeout
        cache: 'no-store',
      });
      return await this.handleResponse<Deal[]>(response);
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        const timeoutError: ApiError = {
          message: 'Deal discovery timed out after 30 seconds',
          status: 408,
          details: 'Deal discovery request timeout - this operation may take longer than expected'
        };
        throw timeoutError;
      }
      console.error('API discoverDeals failed:', error);
      throw error;
    }
  }

  async findPriceDiscrepancies(query: string, minProfitMargin: number = 15): Promise<Deal[]> {
    const params = new URLSearchParams({ 
      query, 
      minProfitMargin: minProfitMargin.toString() 
    });
    return this.get<Deal[]>(`/deals/price-discrepancies?${params.toString()}`);
  }

  async getRealTimeDeals(): Promise<Deal[]> {
    return this.get<Deal[]>('/deals/real-time');
  }

  async getFilteredDeals(filters: {
    minScore?: number;
    minProfit?: number;
    marketplace?: string;
    limit?: number;
  }): Promise<Deal[]> {
    const params = new URLSearchParams();
    if (filters.minScore !== undefined) params.append('minScore', filters.minScore.toString());
    if (filters.minProfit !== undefined) params.append('minProfit', filters.minProfit.toString());
    if (filters.marketplace) params.append('marketplace', filters.marketplace);
    if (filters.limit !== undefined) params.append('limit', filters.limit.toString());
    
    return this.get<Deal[]>(`/deals/filtered?${params.toString()}`);
  }

  async getDealStats(): Promise<{
    totalDeals: number;
    averageScore: number;
    totalPotentialProfit: number;
    highValueDeals: number;
    marketplaceBreakdown: Array<{ marketplace: string; count: number }>;
    recentDeals: number;
  }> {
    return this.get('/deals/stats');
  }

  async analyzeProductForDeal(productId: number): Promise<Deal> {
    return this.post<Deal>(`/deals/analyze/${productId}`, {});
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
  // Dashboard stats
  async getDashboardStats(): Promise<DashboardStats> {
    return this.get<DashboardStats>('/dashboard/stats');
  }  // Health check
  async checkHealth(): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/dashboard/health`, {
        method: 'GET',
        headers: {
          'Connection': 'close', // Force connection closure
        },
        signal: this.createTimeoutSignal(10000), // 10 second timeout for health check
        cache: 'no-store', // Prevent caching issues
      });
      return response.ok;
    } catch {
      return false;
    }
  }

  // New intelligent discovery methods
  async discoverIntelligentDeals(settings: DealDiscoverySettings): Promise<Deal[]> {
    try {
      const response = await fetch(`${this.baseUrl}/deals/intelligent-discovery`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Connection': 'close',
        },
        body: JSON.stringify(settings),
        signal: this.createTimeoutSignal(60000), // 60 second timeout for intelligent discovery
        cache: 'no-store',
      });
      return await this.handleResponse<Deal[]>(response);
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        const timeoutError: ApiError = {
          message: 'Intelligent discovery timed out after 60 seconds',
          status: 408,
          details: 'Intelligent discovery request timeout - this operation may take longer than expected'
        };
        throw timeoutError;
      }
      console.error('API discoverIntelligentDeals failed:', error);
      throw error;
    }
  }

  async discoverIntelligentDealsWithProgress(
    settings: DealDiscoverySettings,
    onProgress: (progress: DiscoveryProgress) => void,
    onComplete: (deals: Deal[]) => void,
    onError: (error: string) => void
  ): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/deals/intelligent-discovery-stream`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'text/event-stream',
          'Cache-Control': 'no-cache',
        },
        body: JSON.stringify(settings),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error('Failed to get response reader');
      }

      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            try {
              const data = JSON.parse(line.slice(6));
              
              if (data.type === 'progress') {
                onProgress(data);
              } else if (data.type === 'complete') {
                onComplete(data.deals);
                return;
              } else if (data.type === 'error') {
                onError(data.message);
                return;
              } else {
                // Regular progress update (backward compatibility)
                onProgress(data);
              }
            } catch (e) {
              console.warn('Failed to parse SSE data:', line);
            }
          }
        }
      }
    } catch (error) {
      console.error('API discoverIntelligentDealsWithProgress failed:', error);
      onError(error instanceof Error ? error.message : 'Unknown error occurred');
    }
  }

  async getComparisonListings(dealId: number): Promise<ComparisonListing[]> {
    return this.get<ComparisonListing[]>(`/deals/${dealId}/comparison-listings`);
  }

  async getTrendingProducts(count: number = 10, searchTerms?: string): Promise<Product[]> {
    const params = new URLSearchParams({ count: count.toString() });
    if (searchTerms) {
      params.append('searchTerms', searchTerms);
    }
    return this.get<Product[]>(`/deals/trending-products?${params.toString()}`);
  }

  async validateExactResultCount(count: number): Promise<{ 
    canFulfill: boolean; 
    requestedCount: number; 
    message: string; 
  }> {
    return this.get(`/deals/validate-count/${count}`);
  }
}

// Export singleton instance
export const apiService = new ApiService();
export default apiService;
