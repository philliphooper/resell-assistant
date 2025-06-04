export interface DealDiscoverySettings {
  exactResultCount: number;
  targetBuyPrice?: number;
  uniqueProductCount: number;
  listingsPerProduct: number;
  searchTerms?: string;
  minProfitMargin: number;
  preferredMarketplaces: string[];
  enableNotifications: boolean;
}

export interface DiscoveryProgress {
  currentPhase: string;
  currentAction: string;
  productsFound: number;
  listingsAnalyzed: number;
  dealsCreated: number;
  percentComplete: number;
  recentFindings: string[];
}

export interface ComparisonListing {
  productId: number;
  title: string;
  price: number;
  shippingCost: number;
  marketplace: string;
  condition?: string;
  location?: string;
  url?: string;
  dateListed: string;
  isSelectedDeal: boolean;
}
