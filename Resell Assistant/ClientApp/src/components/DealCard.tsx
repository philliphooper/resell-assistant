import React from 'react';
import { Deal } from '../types/api';

interface DealCardProps {
  deal: Deal;
  onViewDetails?: (deal: Deal) => void;
}

const DealCard: React.FC<DealCardProps> = ({ deal, onViewDetails }) => {
  const getDealScoreColor = (score: number) => {
    if (score >= 80) return 'text-green-500 bg-green-100 dark:bg-green-900';
    if (score >= 60) return 'text-yellow-500 bg-yellow-100 dark:bg-yellow-900';
    return 'text-red-500 bg-red-100 dark:bg-red-900';
  };

  const getDealScoreBadge = (score: number) => {
    if (score >= 80) return 'Excellent';
    if (score >= 60) return 'Good';
    return 'Fair';
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const calculateMargin = () => {
    if (!deal.product) return 0;
    return Math.round((deal.potentialProfit / deal.product.price) * 100);
  };

  const getConfidenceColor = (confidence: number) => {
    if (confidence >= 80) return 'text-green-600 dark:text-green-400';
    if (confidence >= 60) return 'text-yellow-600 dark:text-yellow-400';
    return 'text-red-600 dark:text-red-400';
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700 hover:shadow-lg transition-all duration-200 hover:scale-[1.02]">
      <div className="flex justify-between items-start mb-4">
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 line-clamp-2">
            {deal.product?.title || `Product #${deal.productId}`}
          </h3>
          <div className="flex flex-wrap gap-2">
            {deal.product?.marketplace && (
              <span className="inline-block bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs px-2 py-1 rounded-full">
                {deal.product.marketplace}
              </span>
            )}
            {deal.product?.condition && (
              <span className="inline-block bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200 text-xs px-2 py-1 rounded-full">
                {deal.product.condition}
              </span>
            )}
          </div>
        </div>
        <div className={`px-3 py-1 rounded-full text-sm font-bold ${getDealScoreColor(deal.dealScore)}`}>
          {deal.dealScore}/100
        </div>
      </div>

      {/* Enhanced Deal Score Badge */}
      <div className="mb-4">
        <div className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getDealScoreColor(deal.dealScore)}`}>
          <svg className="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
          </svg>
          {getDealScoreBadge(deal.dealScore)} Deal
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 mb-4">
        <div>
          <p className="text-sm text-gray-600 dark:text-gray-400">Buy Price</p>
          <p className="text-lg font-semibold text-gray-900 dark:text-white">
            {deal.product ? formatCurrency(deal.product.price) : 'N/A'}
          </p>
          {deal.product?.shippingCost && deal.product.shippingCost > 0 && (
            <p className="text-xs text-gray-500">
              +{formatCurrency(deal.product.shippingCost)} shipping
            </p>
          )}
        </div>
        <div>
          <p className="text-sm text-gray-600 dark:text-gray-400">Est. Sell Price</p>
          <p className="text-lg font-semibold text-gray-900 dark:text-white">
            {formatCurrency(deal.estimatedSellPrice)}
          </p>
        </div>
      </div>

      {/* Enhanced Profit Section */}
      <div className="mb-4 p-3 bg-gradient-to-r from-green-50 to-green-100 dark:from-green-900 dark:to-green-800 rounded-lg border border-green-200 dark:border-green-700">
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-green-800 dark:text-green-200">Potential Profit</span>
          <span className={`text-sm font-medium ${getConfidenceColor(deal.confidence)}`}>
            {deal.confidence}% confidence
          </span>
        </div>
        <div className="flex items-center justify-between">
          <span className={`text-xl font-bold ${deal.potentialProfit > 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
            {formatCurrency(deal.potentialProfit)}
          </span>
          {deal.product && (
            <div className="text-right">
              <span className="text-sm font-medium text-green-700 dark:text-green-300">
                {calculateMargin()}% margin
              </span>
              <div className="text-xs text-green-600 dark:text-green-400">
                ROI: {Math.round((deal.potentialProfit / deal.product.price) * 100)}%
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Deal Reasoning */}
      {deal.reasoning && (
        <div className="mb-4 p-3 bg-gray-50 dark:bg-gray-700 rounded-lg">
          <p className="text-sm text-gray-600 dark:text-gray-400 font-medium mb-1">Analysis:</p>
          <p className="text-sm text-gray-700 dark:text-gray-300 line-clamp-3">
            {deal.reasoning}
          </p>
        </div>
      )}

      {/* Product Details */}
      {deal.product?.location && (
        <div className="mb-4">
          <div className="flex items-center text-sm text-gray-600 dark:text-gray-400">
            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            {deal.product.location}
          </div>
        </div>
      )}

      {/* Action Buttons */}
      <div className="flex justify-between items-center pt-4 border-t border-gray-200 dark:border-gray-600">
        <span className="text-xs text-gray-500">
          {formatDate(deal.createdAt)}
        </span>
        
        <div className="flex space-x-2">
          {deal.product?.url && (
            <a
              href={deal.product.url}
              target="_blank"
              rel="noopener noreferrer"
              className="px-3 py-1 bg-blue-600 text-white text-sm font-medium rounded hover:bg-blue-700 transition-colors flex items-center"
            >
              <svg className="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
              </svg>
              View Listing
            </a>
          )}
          {onViewDetails && (
            <button
              onClick={() => onViewDetails(deal)}
              className="px-3 py-1 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 text-sm font-medium rounded hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
            >
              Details
            </button>
          )}
        </div>
      </div>

      {/* Visual Profit Indicator */}
      <div className="mt-4">
        <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
          <div
            className={`h-2 rounded-full transition-all duration-300 ${
              deal.dealScore >= 80 ? 'bg-green-500' :
              deal.dealScore >= 60 ? 'bg-yellow-500' : 'bg-red-500'
            }`}
            style={{ width: `${Math.min(deal.dealScore, 100)}%` }}
          ></div>
        </div>
        <div className="flex justify-between text-xs text-gray-500 mt-1">
          <span>Deal Quality</span>
          <span>{deal.dealScore}/100</span>
        </div>
      </div>
    </div>
  );
};

export default DealCard;
