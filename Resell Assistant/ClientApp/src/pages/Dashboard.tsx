import React from 'react';
import { useTopDeals, useDashboardStats, useApiHealth } from '../hooks/useApi';
import DealCard from '../components/DealCard';
import StatsCard from '../components/StatsCard';
import LoadingSpinner from '../components/LoadingSpinner';

const Dashboard: React.FC = () => {
  const { data: deals, loading: dealsLoading, error: dealsError, refresh: refreshDeals } = useTopDeals();
  const { data: stats, loading: statsLoading, error: statsError, refresh: refreshStats } = useDashboardStats();
  const { isHealthy } = useApiHealth();

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  };

  const handleRefresh = () => {
    refreshDeals();
    refreshStats();
  };

  // Show API connection status
  const renderApiStatus = () => {
    if (isHealthy === null) return null;
    
    return (
      <div className={`mb-6 p-4 rounded-lg ${
        isHealthy 
          ? 'bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200' 
          : 'bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200'
      }`}>
        <div className="flex items-center">
          <div className={`w-3 h-3 rounded-full mr-3 ${
            isHealthy ? 'bg-green-500' : 'bg-red-500'
          }`}></div>
          <span className="font-medium">
            {isHealthy ? 'API Connected' : 'API Connection Failed'}
          </span>
          {!isHealthy && (
            <button
              onClick={handleRefresh}
              className="ml-4 text-sm underline hover:no-underline"
            >
              Retry
            </button>
          )}
        </div>
      </div>
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
      </div>

      {/* API Status */}
      {renderApiStatus()}      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statsLoading ? (
          <div className="col-span-full">
            <LoadingSpinner text="Loading statistics..." className="py-8" />
          </div>
        ) : statsError ? (
          <div className="col-span-full">
            <div className="bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 p-4 rounded-lg">
              <p className="font-medium">Failed to load statistics</p>
              <p className="text-sm mt-1">{statsError.message}</p>
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
        ) : null}
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
      )}      {/* Recent Deals Summary */}
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

      {/* Top Deals */}
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Top Deals</h2>
        
        {dealsLoading ? (
          <LoadingSpinner text="Loading top deals..." className="py-12" />
        ) : dealsError ? (
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
        ) : deals && deals.length > 0 ? (
          <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
            {deals.slice(0, 6).map((deal) => (
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
        ) : (
          <div className="text-center py-12">
            <svg className="w-12 h-12 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
            </svg>
            <h3 className="text-lg font-medium text-gray-900 dark:text-white">No deals found</h3>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              Check back later for new opportunities or try refreshing the data.
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
