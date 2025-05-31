import React from 'react';
import { Deal } from '../types/api';

interface DealCardProps {
  deal: Deal;
  onViewDetails?: (deal: Deal) => void;
}

const DealCard: React.FC<DealCardProps> = ({ deal, onViewDetails }) => {
  const getDealScoreColor = (score: number) => {
    if (score >= 80) return 'text-green-500';
    if (score >= 60) return 'text-yellow-500';
    return 'text-red-500';
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

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700 hover:shadow-lg transition-shadow duration-200">
      <div className="flex justify-between items-start mb-4">
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 line-clamp-2">
            {deal.product?.title || `Product #${deal.productId}`}
          </h3>
          {deal.product?.marketplace && (
            <span className="inline-block bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs px-2 py-1 rounded-full">
              {deal.product.marketplace}
            </span>
          )}
        </div>
        <div className={`text-2xl font-bold ${getDealScoreColor(deal.dealScore)}`}>
          {deal.dealScore}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 mb-4">
        <div>
          <p className="text-sm text-gray-600 dark:text-gray-400">Current Price</p>
          <p className="text-lg font-semibold text-gray-900 dark:text-white">
            {deal.product ? formatCurrency(deal.product.price) : 'N/A'}
          </p>
        </div>
        <div>
          <p className="text-sm text-gray-600 dark:text-gray-400">Est. Sell Price</p>
          <p className="text-lg font-semibold text-gray-900 dark:text-white">
            {formatCurrency(deal.estimatedSellPrice)}
          </p>
        </div>
      </div>

      <div className="mb-4">
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm text-gray-600 dark:text-gray-400">Potential Profit</span>
          <span className="text-sm text-gray-600 dark:text-gray-400">Confidence: {deal.confidence}%</span>
        </div>
        <div className="flex items-center">
          <span className={`text-xl font-bold ${deal.potentialProfit > 0 ? 'text-green-600' : 'text-red-600'}`}>
            {formatCurrency(deal.potentialProfit)}
          </span>
          {deal.product && (
            <span className="ml-2 text-sm text-gray-500">
              ({Math.round((deal.potentialProfit / deal.product.price) * 100)}% margin)
            </span>
          )}
        </div>
      </div>

      {deal.reasoning && (
        <div className="mb-4">
          <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-3">
            {deal.reasoning}
          </p>
        </div>
      )}

      <div className="flex justify-between items-center">
        <span className="text-xs text-gray-500">
          {formatDate(deal.createdAt)}
        </span>
        
        <div className="flex space-x-2">
          {deal.product?.url && (
            <a
              href={deal.product.url}
              target="_blank"
              rel="noopener noreferrer"
              className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm font-medium"
            >
              View Listing
            </a>
          )}
          {onViewDetails && (
            <button
              onClick={() => onViewDetails(deal)}
              className="text-gray-600 hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200 text-sm font-medium"
            >
              Details
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default DealCard;
