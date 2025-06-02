import { Product, Deal, PriceHistory, DashboardStats, ApiError } from '../types/api';

// Global connection manager to reuse connections
class ConnectionManager {
  private static instance: ConnectionManager;
  private abortController: AbortController | null = null;

  private constructor() {}

  static getInstance(): ConnectionManager {
    if (!ConnectionManager.instance) {
      ConnectionManager.instance = new ConnectionManager();
    }
    return ConnectionManager.instance;
  }

  createSignal(timeoutMs: number): AbortSignal {
    if (this.abortController) {
      this.abortController.abort();
    }
    this.abortController = new AbortController();
    setTimeout(() => {
      if (this.abortController) {
        this.abortController.abort();
      }
    }, timeoutMs);
    return this.abortController.signal;
  }

  cleanup() {
    if (this.abortController) {
      this.abortController.abort();
      this.abortController = null;
    }
  }
}

class ApiService {
  private baseUrl: string;
  private timeoutMs: number = 5000; // Reduced to 5 second timeout
  private connectionManager: ConnectionManager;

  constructor() {
    // Use the same domain as the current page (for .NET integration)
    this.baseUrl = window.location.origin + '/api';
    this.connectionManager = ConnectionManager.getInstance();
  }

  private createTimeoutSignal(timeoutMs: number = this.timeoutMs): AbortSignal {
    return this.connectionManager.createSignal(timeoutMs);
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
          'Connection': 'keep-alive', // Use keep-alive connections
        },
        signal: this.createTimeoutSignal(),
        cache: 'no-store', // Prevent caching issues
      });
      return await this.handleResponse<T>(response);
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        const timeoutError: ApiError = {
          message: 'Request timed out after 5 seconds',
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
          'Connection': 'keep-alive', // Use keep-alive connections
        },
        body: JSON.stringify(data),
        signal: this.createTimeoutSignal(),
        cache: 'no-store', // Prevent caching issues
      });
      return await this.handleResponse<T>(response);
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        const timeoutError: ApiError = {
          message: 'Request timed out after 30 seconds',
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
  // Dashboard stats
  async getDashboardStats(): Promise<DashboardStats> {
    return this.get<DashboardStats>('/dashboard/stats');
  }  // Health check
  async checkHealth(): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/products/recent?count=1`, {
        method: 'GET',
        headers: {
          'Connection': 'close', // Force connection closure
        },
        signal: this.createTimeoutSignal(5000), // Shorter timeout for health check
        cache: 'no-store', // Prevent caching issues
      });
      return response.ok;
    } catch {
      return false;
    }
  }
}

// Export singleton instance
export const apiService = new ApiService();
export default apiService;
