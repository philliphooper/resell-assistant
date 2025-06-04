import React, { useState, useEffect } from 'react';
import { useTopDeals, useDashboardStats, useApiHealth } from '../hooks/useApi';
import DealCard from '../components/DealCard';
import StatsCard from '../components/StatsCard';
import LoadingSpinner from '../components/LoadingSpinner';
import DealDiscoverySettingsModal from '../components/DealDiscoverySettingsModal';
import { apiService } from '../services/api';
import { Deal } from '../types/api';
import { DealDiscoverySettings, DiscoveryProgress } from '../types/settings';

const Dashboard: React.FC = () => {
  const { data: deals, loading: dealsLoading, error: dealsError, refresh: refreshDeals } = useTopDeals();
  const { data: stats, loading: statsLoading, error: statsError, refresh: refreshStats } = useDashboardStats();
  const { checkHealth } = useApiHealth();
  // Enhanced state for deal discovery
  const [discoveringDeals, setDiscoveringDeals] = useState(false);
  const [discoveredDeals, setDiscoveredDeals] = useState<Deal[]>([]);
  const [realTimeDeals, setRealTimeDeals] = useState<Deal[]>([]);
  const [activeTab, setActiveTab] = useState<'stored' | 'discovered' | 'realtime'>('stored');
  const [searchQuery, setSearchQuery] = useState('');
  const [isSearching, setIsSearching] = useState(false);
  const [searchResults, setSearchResults] = useState<Deal[]>([]);
  const [discoveryError, setDiscoveryError] = useState<string | null>(null);
  const [discoverySuccess, setDiscoverySuccess] = useState<string | null>(null);
  const [discoveryProgress, setDiscoveryProgress] = useState<DiscoveryProgress | null>(null);
  
  // Deal Discovery Settings Modal
  const [isSettingsModalOpen, setIsSettingsModalOpen] = useState(false);
  const [dealDiscoverySettings, setDealDiscoverySettings] = useState<DealDiscoverySettings>({
    exactResultCount: 10,
    targetBuyPrice: undefined,
    uniqueProductCount: 5,
    listingsPerProduct: 5,
    searchTerms: '',
    minProfitMargin: 15,
    preferredMarketplaces: ['eBay', 'Facebook Marketplace'],
    enableNotifications: true,
  });

  // Auto-refresh real-time deals every 5 minutes
  useEffect(() => {
    const fetchRealTimeDeals = async () => {
      try {
        const realTime = await apiService.getRealTimeDeals();
        setRealTimeDeals(realTime);
      } catch (error) {
        console.error('Failed to fetch real-time deals:', error);
      }
    };

    fetchRealTimeDeals();
    const interval = setInterval(fetchRealTimeDeals, 5 * 60 * 1000); // 5 minutes

    return () => clearInterval(interval);
  }, []);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  };

  const handleRefresh = () => {
    refreshDeals();
    refreshStats();
    checkHealth(); // Also refresh the API health status
  };
  // Enhanced deal discovery with intelligent discovery and live progress
  const discoverNewDeals = async () => {
    setDiscoveringDeals(true);
    setDiscoveryError(null);
    setDiscoverySuccess(null);
    setDiscoveryProgress(null);
    
    try {
      console.log('Starting intelligent deal discovery with settings:', dealDiscoverySettings);
      
      // First validate if we can fulfill the exact result count
      const validation = await apiService.validateExactResultCount(dealDiscoverySettings.exactResultCount);
      if (!validation.canFulfill) {
        setDiscoveryError(`Cannot guarantee ${dealDiscoverySettings.exactResultCount} deals with current data. ${validation.message}`);
        return;
      }

      // Use Server-Sent Events for live progress updates
      await apiService.discoverIntelligentDealsWithProgress(
        dealDiscoverySettings,
        (progress: DiscoveryProgress) => {
          setDiscoveryProgress(progress);
          console.log('Discovery progress:', progress);
        },
        (deals: Deal[]) => {
          console.log('Intelligent discovery completed:', deals);
          setDiscoveredDeals(deals);
          setActiveTab('discovered');
          setDiscoveryProgress(null);
          
          if (deals.length > 0) {
            const totalProfit = deals.reduce((sum, deal) => sum + deal.potentialProfit, 0);
            const avgScore = Math.round(deals.reduce((sum, deal) => sum + deal.dealScore, 0) / deals.length);
            setDiscoverySuccess(
              `Found exactly ${deals.length} deals! Total potential profit: ${formatCurrency(totalProfit)} (Avg score: ${avgScore}/100)`
            );
          } else {
            setDiscoveryError('No profitable deals found with current settings. Try adjusting your criteria.');
          }
        },
        (error: string) => {
          console.error('Intelligent discovery failed:', error);
          setDiscoveryError(`Discovery failed: ${error}`);
          setDiscoveryProgress(null);
        }
      );
    } catch (error) {
      console.error('Failed to start intelligent discovery:', error);
      
      if (error instanceof Error) {
        if (error.message.includes('timeout') || error.message.includes('AbortError')) {
          setDiscoveryError('Discovery is taking longer than expected. The process may still be running in the background.');
        } else if (error.message.includes('Network Error') || error.message.includes('fetch')) {
          setDiscoveryError('Network connection issue. Please check your internet connection and try again.');
        } else {
          setDiscoveryError(`Discovery failed: ${error.message}`);
        }
      } else {
        setDiscoveryError('An unexpected error occurred during discovery. Please try again.');
      }
    } finally {
      setDiscoveringDeals(false);
    }
  };

  const searchForDeals = async () => {
    if (!searchQuery.trim()) return;
    
    setIsSearching(true);
    try {
      const results = await apiService.findPriceDiscrepancies(searchQuery, 15);
      setSearchResults(results);
    } catch (error) {
      console.error('Failed to search for deals:', error);
    } finally {
      setIsSearching(false);
    }
  };

  const getCurrentDeals = () => {
    switch (activeTab) {
      case 'discovered':
        return discoveredDeals;
      case 'realtime':
        return realTimeDeals;
      case 'stored':
      default:
        return deals || [];
    }
  };

  const getCurrentLoading = () => {
    switch (activeTab) {
      case 'discovered':
        return discoveringDeals;
      case 'realtime':
        return false;
      case 'stored':
      default:
        return dealsLoading;
    }  };
  // Handle settings save
  const handleSettingsSave = (newSettings: DealDiscoverySettings) => {
    setDealDiscoverySettings(newSettings);
    // You could also persist these settings to localStorage or API here
    console.log('Deal Discovery settings updated:', newSettings);
  };

  // Enhanced deal summary for discovered deals
  const renderDealSummary = () => {
    if (activeTab !== 'discovered' || discoveredDeals.length === 0) return null;

    const totalProfit = discoveredDeals.reduce((sum, deal) => sum + deal.potentialProfit, 0);
    const avgScore = Math.round(discoveredDeals.reduce((sum, deal) => sum + deal.dealScore, 0) / discoveredDeals.length);
    const topDeal = discoveredDeals.reduce((top, deal) => deal.potentialProfit > top.potentialProfit ? deal : top);
    const highScoreDeals = discoveredDeals.filter(deal => deal.dealScore >= 80);

    return (
      <div className="mb-6 p-4 bg-gradient-to-r from-green-50 to-blue-50 dark:from-green-900 dark:to-blue-900 rounded-lg border border-green-200 dark:border-green-700">
        <h4 className="text-lg font-semibold text-green-800 dark:text-green-200 mb-3">Discovery Summary</h4>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="text-center">
            <p className="text-2xl font-bold text-green-600 dark:text-green-400">{discoveredDeals.length}</p>
            <p className="text-sm text-green-700 dark:text-green-300">Deals Found</p>
          </div>
          <div className="text-center">
            <p className="text-2xl font-bold text-blue-600 dark:text-blue-400">{formatCurrency(totalProfit)}</p>
            <p className="text-sm text-blue-700 dark:text-blue-300">Total Profit</p>
          </div>
          <div className="text-center">
            <p className="text-2xl font-bold text-purple-600 dark:text-purple-400">{avgScore}/100</p>
            <p className="text-sm text-purple-700 dark:text-purple-300">Avg Score</p>
          </div>
          <div className="text-center">
            <p className="text-2xl font-bold text-orange-600 dark:text-orange-400">{highScoreDeals.length}</p>
            <p className="text-sm text-orange-700 dark:text-orange-300">High Quality</p>
          </div>
        </div>
        {topDeal && (
          <div className="mt-3 p-3 bg-white dark:bg-gray-800 rounded border">
            <p className="text-sm font-medium text-gray-900 dark:text-white">🏆 Top Deal:</p>
            <p className="text-sm text-gray-700 dark:text-gray-300">
              {topDeal.product?.title || 'Unknown Product'} - {formatCurrency(topDeal.potentialProfit)} profit
            </p>
          </div>
        )}
      </div>
    );
  };
  // Enhanced status messages
  const renderStatusMessages = () => {
    return (
      <>
        {discoverySuccess && (
          <div className="mb-4 p-4 bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200 rounded-lg border border-green-300 dark:border-green-700">
            <div className="flex items-center">
              <svg className="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              <span className="font-medium">{discoverySuccess}</span>
              <button
                onClick={() => setDiscoverySuccess(null)}
                className="ml-auto text-green-600 hover:text-green-800"
              >
                ×
              </button>
            </div>
          </div>
        )}
        
        {discoveryError && (
          <div className="mb-4 p-4 bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 rounded-lg border border-red-300 dark:border-red-700">
            <div className="flex items-start">
              <svg className="w-5 h-5 mr-2 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
              <div className="flex-1">
                <p className="font-medium">Deal Discovery Error</p>
                <p className="text-sm mt-1">{discoveryError}</p>
              </div>
              <button
                onClick={() => setDiscoveryError(null)}
                className="ml-2 text-red-600 hover:text-red-800"
              >
                ×
              </button>
            </div>
          </div>
        )}
      </>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Dashboard</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            Your reselling opportunities at a glance
          </p>
        </div>
        <button
          onClick={handleRefresh}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors duration-200 flex items-center"
        >
          <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          Refresh
        </button>
      </div>      {/* Status Messages */}
      {renderStatusMessages()}

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statsLoading ? (
          // Show loading skeleton for each card
          Array.from({ length: 4 }, (_, index) => (
            <div key={index} className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700 animate-pulse">
              <div className="flex items-center justify-between">
                <div className="flex-1">
                  <div className="h-4 bg-gray-200 dark:bg-gray-600 rounded w-3/4 mb-2"></div>
                  <div className="h-8 bg-gray-200 dark:bg-gray-600 rounded w-1/2"></div>
                </div>
                <div className="w-12 h-12 bg-gray-200 dark:bg-gray-600 rounded-lg"></div>
              </div>
            </div>
          ))
        ) : statsError ? (
          <div className="col-span-full">
            <div className="bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 p-6 rounded-lg">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-medium">Failed to load statistics</p>
                  <p className="text-sm mt-1">{statsError.message}</p>
                </div>
                <button
                  onClick={refreshStats}
                  className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors duration-200 text-sm"
                >
                  Retry
                </button>
              </div>
            </div>
          </div>
        ) : stats ? (
          <>
            <StatsCard
              title="Total Products"
              value={stats.totalProducts}
              icon={
                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                </svg>
              }
              color="blue"
            />
            <StatsCard
              title="Active Deals"
              value={stats.totalDeals}
              subtitle="opportunities"
              icon={
                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
              }
              color="green"
            />
            <StatsCard
              title="Total Profit"
              value={formatCurrency(stats.totalProfit)}
              icon={
                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
                </svg>
              }
              color="yellow"
            />
            <StatsCard
              title="Avg Deal Score"
              value={stats.averageDealScore}
              subtitle="/ 100"
              icon={
                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                </svg>
              }
              color="purple"
            />
          </>
        ) : (
          // Fallback state when no data is available but no error occurred
          <div className="col-span-full">
            <div className="bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200 p-6 rounded-lg text-center">
              <p className="font-medium">No statistics available</p>
              <p className="text-sm mt-1">Click refresh to load dashboard data</p>
              <button
                onClick={refreshStats}
                className="mt-3 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors duration-200 text-sm"
              >
                Load Statistics
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Enhanced Stats Section */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Portfolio Value</h3>
            <p className="text-2xl font-bold text-indigo-600 dark:text-indigo-400">{formatCurrency(stats.portfolioValue)}</p>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Current investments</p>
          </div>

          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Weekly Profit</h3>
            <p className="text-2xl font-bold text-green-600 dark:text-green-400">{formatCurrency(stats.weeklyProfit)}</p>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Last 7 days</p>
          </div>

          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Active Alerts</h3>
            <p className="text-2xl font-bold text-orange-600 dark:text-orange-400">{stats.activeAlerts}</p>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Monitoring deals</p>
          </div>

          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Top Categories</h3>
            <div className="space-y-1">
              {stats.topCategories.slice(0, 3).map((category, index) => (
                <div key={category} className="flex items-center">
                  <span className={`inline-block w-2 h-2 rounded-full mr-2 ${
                    index === 0 ? 'bg-blue-500' : index === 1 ? 'bg-green-500' : 'bg-yellow-500'
                  }`}></span>
                  <span className="text-sm text-gray-700 dark:text-gray-300">{category}</span>
                </div>
              ))}
              {stats.topCategories.length === 0 && (
                <span className="text-sm text-gray-500 dark:text-gray-400">No categories found</span>
              )}
            </div>
          </div>
        </div>
      )}      {/* Enhanced Deal Discovery Section */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Deal Discovery</h3>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
              Find profitable resale opportunities across marketplaces
            </p>
          </div>
          <div className="flex items-center space-x-3">
            {/* Settings Filter Icon */}
            <button
              onClick={() => setIsSettingsModalOpen(true)}
              className="p-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors duration-200"
              title="Deal Discovery Settings"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 100 4m0-4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 100 4m0-4v2m0-6V4" />
              </svg>
            </button>
            
            {/* Discover Deals Button */}
            <button
              onClick={discoverNewDeals}
              disabled={discoveringDeals}
              className={`px-6 py-3 rounded-lg font-medium transition-all duration-200 flex items-center ${
                discoveringDeals
                  ? 'bg-gray-400 cursor-not-allowed'
                  : 'bg-green-600 hover:bg-green-700 hover:scale-105 shadow-lg'
              } text-white`}
            >
              {discoveringDeals ? (
                <>
                  <svg className="w-5 h-5 mr-2 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                  Discovering Deals...
                </>
              ) : (
                <>
                  <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                  Discover Deals
                </>
              )}
            </button>
          </div>
        </div>

        {/* Search for specific deals */}
        <div className="mb-4">
          <div className="flex gap-2">
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search for specific items (e.g., iPhone, MacBook, PlayStation)"
              className="flex-1 p-3 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              onKeyPress={(e) => e.key === 'Enter' && searchForDeals()}
            />
            <button
              onClick={searchForDeals}
              disabled={isSearching || !searchQuery.trim()}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 transition-colors duration-200"
            >
              {isSearching ? 'Searching...' : 'Search'}
            </button>
          </div>
          {searchResults.length > 0 && (
            <div className="mt-4 p-4 bg-blue-50 dark:bg-blue-900 rounded-lg">
              <h4 className="font-medium text-blue-900 dark:text-blue-100 mb-2">
                Found {searchResults.length} deal{searchResults.length !== 1 ? 's' : ''} for "{searchQuery}"
              </h4>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {searchResults.slice(0, 4).map((deal) => (
                  <div key={deal.id} className="p-3 bg-white dark:bg-gray-800 rounded border">
                    <p className="font-medium text-sm text-gray-900 dark:text-white truncate">
                      {deal.product?.title || 'Unknown Product'}
                    </p>
                    <p className="text-green-600 dark:text-green-400 text-sm">
                      Profit: {formatCurrency(deal.potentialProfit)} ({deal.dealScore}/100)
                    </p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>        {/* Discovery Progress Display */}
        {discoveryProgress && (
          <div className="mb-4 p-4 bg-blue-50 dark:bg-blue-900 rounded-lg border border-blue-200 dark:border-blue-700">
            <div className="flex items-center justify-between mb-3">
              <h4 className="font-medium text-blue-900 dark:text-blue-100">
                {discoveryProgress.currentPhase}
              </h4>
              <span className="text-sm text-blue-700 dark:text-blue-300">
                {discoveryProgress.percentComplete}% Complete
              </span>
            </div>
            
            {/* Progress Bar */}
            <div className="w-full bg-blue-200 dark:bg-blue-800 rounded-full h-2 mb-3">
              <div 
                className="bg-blue-600 dark:bg-blue-400 h-2 rounded-full transition-all duration-300"
                style={{ width: `${discoveryProgress.percentComplete}%` }}
              ></div>
            </div>
            
            {/* Current Action */}
            <p className="text-sm text-blue-800 dark:text-blue-200 mb-2">
              {discoveryProgress.currentAction}
            </p>
            
            {/* Progress Stats */}
            <div className="grid grid-cols-3 gap-4 text-sm">
              <div className="text-center">
                <div className="font-medium text-blue-900 dark:text-blue-100">
                  {discoveryProgress.productsFound}
                </div>
                <div className="text-blue-700 dark:text-blue-300">Products Found</div>
              </div>
              <div className="text-center">
                <div className="font-medium text-blue-900 dark:text-blue-100">
                  {discoveryProgress.listingsAnalyzed}
                </div>
                <div className="text-blue-700 dark:text-blue-300">Listings Analyzed</div>
              </div>
              <div className="text-center">
                <div className="font-medium text-blue-900 dark:text-blue-100">
                  {discoveryProgress.dealsCreated}
                </div>
                <div className="text-blue-700 dark:text-blue-300">Deals Created</div>
              </div>
            </div>
            
            {/* Recent Findings */}
            {discoveryProgress.recentFindings.length > 0 && (
              <div className="mt-3 pt-3 border-t border-blue-200 dark:border-blue-700">
                <h5 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-2">
                  Recent Findings:
                </h5>
                <div className="space-y-1">
                  {discoveryProgress.recentFindings.slice(-3).map((finding, index) => (
                    <p key={index} className="text-xs text-blue-700 dark:text-blue-300">
                      • {finding}
                    </p>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Discovery Tips */}
        {!discoveringDeals && discoveredDeals.length === 0 && !discoveryProgress && (
          <div className="mt-4 p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
            <h4 className="font-medium text-gray-900 dark:text-white mb-2">💡 Discovery Tips</h4>
            <ul className="text-sm text-gray-600 dark:text-gray-400 space-y-1">
              <li>• Deal discovery analyzes trending items across multiple marketplaces</li>
              <li>• Process typically takes 30-60 seconds to find the best opportunities</li>
              <li>• High-quality deals (80+ score) indicate strong profit potential</li>
              <li>• Use the search function above to find deals for specific products</li>
            </ul>
          </div>
        )}
      </div>

      {/* Recent Deals Summary */}
      {stats && stats.recentDeals && stats.recentDeals.length > 0 && (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Recent Deals</h3>
            <span className="text-sm text-gray-600 dark:text-gray-400">Last 7 days</span>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {stats.recentDeals.slice(0, 3).map((deal) => (
              <div key={deal.id} className="p-4 border border-gray-200 dark:border-gray-600 rounded-lg">
                <h4 className="font-medium text-gray-900 dark:text-white text-sm mb-2 truncate">
                  {deal.product?.title || 'Unknown Product'}
                </h4>
                <div className="space-y-1">
                  <div className="flex justify-between items-center">
                    <span className="text-xs text-gray-600 dark:text-gray-400">Profit:</span>
                    <span className="text-sm font-medium text-green-600 dark:text-green-400">
                      {formatCurrency(deal.potentialProfit)}
                    </span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-xs text-gray-600 dark:text-gray-400">Score:</span>
                    <span className="text-sm font-medium text-purple-600 dark:text-purple-400">
                      {deal.dealScore}/100
                    </span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-xs text-gray-600 dark:text-gray-400">Platform:</span>
                    <span className="text-sm text-gray-700 dark:text-gray-300">
                      {deal.product?.marketplace || 'N/A'}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
          {stats.recentDeals.length > 3 && (
            <div className="mt-4 text-center">
              <span className="text-sm text-gray-600 dark:text-gray-400">
                +{stats.recentDeals.length - 3} more recent deals
              </span>
            </div>
          )}
        </div>
      )}

      {/* Top Deals with Enhanced Tabs */}
      <div>
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Deals</h2>
          <div className="flex space-x-1 bg-gray-100 dark:bg-gray-700 rounded-lg p-1">
            <button
              onClick={() => setActiveTab('stored')}
              className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                activeTab === 'stored'
                  ? 'bg-white dark:bg-gray-600 text-gray-900 dark:text-white shadow'
                  : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              Stored ({deals?.length || 0})
            </button>
            <button
              onClick={() => setActiveTab('discovered')}
              className={`px-4 py-2 rounded-md text-sm font-medium transition-colors relative ${
                activeTab === 'discovered'
                  ? 'bg-white dark:bg-gray-600 text-gray-900 dark:text-white shadow'
                  : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              Discovered ({discoveredDeals.length})
              {discoveredDeals.length > 0 && (
                <span className="absolute -top-1 -right-1 w-3 h-3 bg-green-500 rounded-full animate-pulse"></span>
              )}
            </button>
            <button
              onClick={() => setActiveTab('realtime')}
              className={`px-4 py-2 rounded-md text-sm font-medium transition-colors relative ${
                activeTab === 'realtime'
                  ? 'bg-white dark:bg-gray-600 text-gray-900 dark:text-white shadow'
                  : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              Real-time ({realTimeDeals.length})
              {realTimeDeals.length > 0 && (
                <span className="absolute -top-1 -right-1 w-3 h-3 bg-blue-500 rounded-full animate-pulse"></span>
              )}
            </button>
          </div>
        </div>
        
        {/* Deal Summary for discovered deals */}
        {renderDealSummary()}
        
        {getCurrentLoading() ? (
          <div className="text-center py-12">
            <LoadingSpinner text={`Finding ${activeTab} deals...`} className="py-8" />
            {activeTab === 'discovered' && (
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-4">
                This process analyzes trending items across multiple marketplaces...
              </p>
            )}
          </div>
        ) : dealsError && activeTab === 'stored' ? (
          <div className="bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 p-6 rounded-lg">
            <h3 className="font-medium">Failed to load deals</h3>
            <p className="text-sm mt-1">{dealsError.message}</p>
            <button
              onClick={refreshDeals}
              className="mt-3 text-sm underline hover:no-underline"
            >
              Try again
            </button>
          </div>
        ) : getCurrentDeals().length > 0 ? (
          <>
            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
              {getCurrentDeals().slice(0, 6).map((deal) => (
                <DealCard
                  key={deal.id}
                  deal={deal}
                  onViewDetails={(deal) => {
                    console.log('View details for deal:', deal);
                    // TODO: Implement deal details modal or navigation
                  }}
                />
              ))}
            </div>
            {getCurrentDeals().length > 6 && (
              <div className="mt-6 text-center">
                <p className="text-gray-600 dark:text-gray-400 mb-4">
                  Showing 6 of {getCurrentDeals().length} deals
                </p>
                <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">
                  View All Deals
                </button>
              </div>
            )}
          </>
        ) : (
          <div className="text-center py-12">
            <svg className="w-12 h-12 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
            </svg>
            <h3 className="text-lg font-medium text-gray-900 dark:text-white">
              {activeTab === 'discovered' ? 'No deals discovered yet' : 
               activeTab === 'realtime' ? 'No real-time deals found' : 
               'No deals found'}
            </h3>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              {activeTab === 'discovered' ? 'Click "Discover Deals" to find new opportunities' :
               activeTab === 'realtime' ? 'Real-time deals will appear here automatically' :
               'Check back later for new opportunities or try refreshing the data.'}
            </p>
          </div>        )}
      </div>
      
      {/* Deal Discovery Settings Modal */}
      <DealDiscoverySettingsModal
        isOpen={isSettingsModalOpen}
        onClose={() => setIsSettingsModalOpen(false)}
        onSave={handleSettingsSave}
        currentSettings={dealDiscoverySettings}
      />
    </div>
  );
};

export default Dashboard;
