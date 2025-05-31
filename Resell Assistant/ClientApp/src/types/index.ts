export interface Product {
  id: number;
  title: string;
  description?: string;
  externalId: string;
  marketplace: string;
  price: number;
  imageUrl?: string;
  productUrl?: string;
  category?: string;
  condition?: string;
  location?: string;
  seller?: string;
  viewCount?: number;
  watchCount?: number;
  dateListed: string;
  dateUpdated: string;
  isActive: boolean;
  priceHistory: PriceHistory[];
}

export interface PriceHistory {
  id: number;
  productId: number;
  price: number;
  date: string;
  source?: string;
}

export interface Deal {
  id: number;
  productId: number;
  product: Product;
  potentialProfit: number;
  score: number;
  reasoning?: string;
  identifiedDate: string;
  isActive: boolean;
  marketComparison?: string;
  daysOnMarket: number;
  estimatedSellPrice?: number;
  confidenceLevel?: number;
}

export interface SearchAlert {
  id: number;
  searchQuery: string;
  maxPrice?: number;
  minProfit?: number;
  category?: string;
  condition?: string;
  location?: string;
  marketplaces: string[];
  isActive: boolean;
  createdDate: string;
  lastTriggered?: string;
  emailNotification?: string;
}

export interface UserPortfolio {
  id: number;
  productId: number;
  product: Product;
  purchasePrice: number;
  purchaseDate: string;
  sellPrice?: number;
  sellDate?: string;
  profit?: number;
  status: string;
  notes?: string;
  purchaseLocation?: string;
  sellLocation?: string;
  shippingCost?: number;
  sellingFees?: number;
  daysToSell?: number;
}

export interface SearchFilters {
  query: string;
  marketplace?: string;
  maxPrice?: number;
  category?: string;
  condition?: string;
  location?: string;
}

export interface DashboardStats {
  totalDeals: number;
  totalProfit: number;
  averageDealScore: number;
  activeAlerts: number;
  portfolioValue: number;
  weeklyProfit: number;
  topCategories: string[];
  recentDeals: Deal[];
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  error?: string;
}

export type MarketplaceType = 'eBay' | 'Facebook' | 'Craigslist' | 'All';
export type DealScoreLevel = 'excellent' | 'good' | 'fair' | 'poor';
export type PortfolioStatus = 'Purchased' | 'Listed' | 'Sold';
