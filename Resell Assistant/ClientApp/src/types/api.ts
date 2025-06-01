// API Types matching the C# backend models
export interface Product {
  id: number;
  title: string;
  description?: string;
  price: number;
  shippingCost: number;
  marketplace: string;
  condition?: string;
  location?: string;
  url?: string;
  imageUrl?: string;
  createdAt: string;
}

export interface Deal {
  id: number;
  productId: number;
  potentialProfit: number;
  estimatedSellPrice: number;
  dealScore: number;
  confidence: number;
  reasoning?: string;
  createdAt: string;
  product?: Product;
}

export interface PriceHistory {
  id: number;
  productId: number;
  price: number;
  marketplace: string;
  recordedAt: string;
}

export interface SearchAlert {
  id: number;
  searchQuery: string;
  minProfit?: number;
  maxPrice?: number;
  marketplace?: string;
  isActive: boolean;
  createdAt: string;
  lastTriggered?: string;
  notes?: string;
}

export interface UserPortfolio {
  id: number;
  productId: number;
  purchasePrice: number;
  sellPrice?: number;
  purchaseDate: string;
  sellDate?: string;
  status: string;
  notes?: string;
  profit?: number;
  product?: Product;
}

export interface DashboardStats {
  totalProducts: number;
  totalDeals: number;
  totalProfit: number;
  averageDealScore: number;
  topMarketplace: string;
  recentDealsCount: number;
  activeAlerts: number;
  portfolioValue: number;
  weeklyProfit: number;
  topCategories: string[];
  recentDeals: Deal[];
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

export interface ApiError {
  message: string;
  status: number;
  details?: string;
}
