import React, { useState, useEffect } from 'react';
import { Deal } from '../types/api';
import { ComparisonListing } from '../types/settings';
import { apiService } from '../services/api';
import LoadingSpinner from './LoadingSpinner';

interface ComparisonModalProps {
  isOpen: boolean;
  onClose: () => void;
  deal: Deal;
}

const ComparisonModal: React.FC<ComparisonModalProps> = ({ isOpen, onClose, deal }) => {
  const [comparisonListings, setComparisonListings] = useState<ComparisonListing[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen && deal.id) {
      fetchComparisonListings();
    }
  }, [isOpen, deal.id]);

  const fetchComparisonListings = async () => {
    setLoading(true);
    setError(null);
    try {
      const listings = await apiService.getComparisonListings(deal.id);
      setComparisonListings(listings);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load comparison data');
    } finally {
      setLoading(false);
    }
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
      year: 'numeric'
    });
  };

  const selectedListing = comparisonListings.find(listing => listing.isSelectedDeal);
  const otherListings = comparisonListings.filter(listing => !listing.isSelectedDeal);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden">
        {/* Header */}
        <div className="flex justify-between items-center p-6 border-b border-gray-200 dark:border-gray-700">
          <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
              Deal Comparison Analysis
            </h2>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
              How we calculated this deal's potential
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
          >
            <svg className="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto max-h-[calc(90vh-120px)]">
          {loading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner text="Loading comparison data..." />
            </div>
          ) : error ? (
            <div className="bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 p-4 rounded-lg">
              <p className="font-medium">Error loading comparison data</p>
              <p className="text-sm mt-1">{error}</p>
              <button
                onClick={fetchComparisonListings}
                className="mt-3 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors text-sm"
              >
                Retry
              </button>
            </div>
          ) : comparisonListings.length === 0 ? (
            <div className="text-center py-8">
              <p className="text-gray-600 dark:text-gray-400">
                No comparison data available for this deal.
              </p>
            </div>
          ) : (
            <div className="space-y-6">
              {/* Deal Summary */}
              <div className="bg-blue-50 dark:bg-blue-900 p-4 rounded-lg">
                <h3 className="font-medium text-blue-900 dark:text-blue-100 mb-2">
                  Deal Summary
                </h3>
                <div className="grid grid-cols-3 gap-4 text-sm">
                  <div>
                    <span className="text-blue-700 dark:text-blue-300">Selected Deal:</span>
                    <div className="font-medium text-blue-900 dark:text-blue-100">
                      {formatCurrency(deal.product?.price || 0)}
                    </div>
                  </div>
                  <div>
                    <span className="text-blue-700 dark:text-blue-300">Est. Sell Price:</span>
                    <div className="font-medium text-blue-900 dark:text-blue-100">
                      {formatCurrency(deal.estimatedSellPrice)}
                    </div>
                  </div>
                  <div>
                    <span className="text-blue-700 dark:text-blue-300">Potential Profit:</span>
                    <div className="font-medium text-green-600 dark:text-green-400">
                      {formatCurrency(deal.potentialProfit)}
                    </div>
                  </div>
                </div>
                <div className="mt-2 text-sm text-blue-700 dark:text-blue-300">
                  <span className="font-medium">Analysis Based On:</span> {comparisonListings.length} similar listings
                </div>
              </div>

              {/* Selected Deal */}
              {selectedListing && (
                <div>
                  <h3 className="font-medium text-gray-900 dark:text-white mb-3 flex items-center">
                    <span className="inline-block w-3 h-3 bg-green-500 rounded-full mr-2"></span>
                    Selected Deal (Best Buy Opportunity)
                  </h3>
                  <div className="bg-green-50 dark:bg-green-900 border border-green-200 dark:border-green-700 rounded-lg p-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                      <div>
                        <span className="text-sm text-green-700 dark:text-green-300">Price:</span>
                        <div className="font-medium text-green-900 dark:text-green-100">
                          {formatCurrency(selectedListing.price)}
                        </div>
                      </div>
                      <div>
                        <span className="text-sm text-green-700 dark:text-green-300">Marketplace:</span>
                        <div className="font-medium text-green-900 dark:text-green-100">
                          {selectedListing.marketplace}
                        </div>
                      </div>
                      <div>
                        <span className="text-sm text-green-700 dark:text-green-300">Condition:</span>
                        <div className="font-medium text-green-900 dark:text-green-100">
                          {selectedListing.condition || 'N/A'}
                        </div>
                      </div>
                      <div>
                        <span className="text-sm text-green-700 dark:text-green-300">Date Listed:</span>
                        <div className="font-medium text-green-900 dark:text-green-100">
                          {formatDate(selectedListing.dateListed)}
                        </div>
                      </div>
                    </div>
                    <div className="mt-3">
                      <div className="text-sm text-green-700 dark:text-green-300 truncate">
                        <span className="font-medium">Title:</span> {selectedListing.title}
                      </div>
                      {selectedListing.location && (
                        <div className="text-sm text-green-700 dark:text-green-300 mt-1">
                          <span className="font-medium">Location:</span> {selectedListing.location}
                        </div>
                      )}
                    </div>
                    {selectedListing.url && (
                      <div className="mt-3">
                        <a
                          href={selectedListing.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="inline-flex items-center px-3 py-1 bg-green-600 text-white text-sm rounded-lg hover:bg-green-700 transition-colors"
                        >
                          <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                          </svg>
                          View Listing
                        </a>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Comparison Listings */}
              {otherListings.length > 0 && (
                <div>
                  <h3 className="font-medium text-gray-900 dark:text-white mb-3 flex items-center">
                    <span className="inline-block w-3 h-3 bg-blue-500 rounded-full mr-2"></span>
                    Comparison Listings ({otherListings.length})
                  </h3>
                  <div className="space-y-3">
                    {otherListings.map((listing, index) => (
                      <div key={index} className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4 border border-gray-200 dark:border-gray-600">
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                          <div>
                            <span className="text-sm text-gray-600 dark:text-gray-400">Price:</span>
                            <div className="font-medium text-gray-900 dark:text-white">
                              {formatCurrency(listing.price)}
                              {listing.shippingCost > 0 && (
                                <span className="text-xs text-gray-500 ml-1">
                                  (+{formatCurrency(listing.shippingCost)} shipping)
                                </span>
                              )}
                            </div>
                          </div>
                          <div>
                            <span className="text-sm text-gray-600 dark:text-gray-400">Marketplace:</span>
                            <div className="font-medium text-gray-900 dark:text-white">
                              {listing.marketplace}
                            </div>
                          </div>
                          <div>
                            <span className="text-sm text-gray-600 dark:text-gray-400">Condition:</span>
                            <div className="font-medium text-gray-900 dark:text-white">
                              {listing.condition || 'N/A'}
                            </div>
                          </div>
                          <div>
                            <span className="text-sm text-gray-600 dark:text-gray-400">Date Listed:</span>
                            <div className="font-medium text-gray-900 dark:text-white">
                              {formatDate(listing.dateListed)}
                            </div>
                          </div>
                        </div>
                        <div className="mt-3">
                          <div className="text-sm text-gray-700 dark:text-gray-300 truncate">
                            <span className="font-medium">Title:</span> {listing.title}
                          </div>
                          {listing.location && (
                            <div className="text-sm text-gray-700 dark:text-gray-300 mt-1">
                              <span className="font-medium">Location:</span> {listing.location}
                            </div>
                          )}
                        </div>
                        {listing.url && (
                          <div className="mt-3">
                            <a
                              href={listing.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center px-3 py-1 bg-gray-600 text-white text-sm rounded-lg hover:bg-gray-700 transition-colors"
                            >
                              <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                              </svg>
                              View Listing
                            </a>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Pricing Analysis */}
              <div className="bg-gray-50 dark:bg-gray-700 p-4 rounded-lg">
                <h3 className="font-medium text-gray-900 dark:text-white mb-3">
                  Pricing Analysis
                </h3>
                <div className="grid grid-cols-3 gap-4 text-sm">
                  <div>
                    <span className="text-gray-600 dark:text-gray-400">Lowest Price:</span>
                    <div className="font-medium text-gray-900 dark:text-white">
                      {formatCurrency(Math.min(...comparisonListings.map(l => l.price)))}
                    </div>
                  </div>
                  <div>
                    <span className="text-gray-600 dark:text-gray-400">Average Price:</span>
                    <div className="font-medium text-gray-900 dark:text-white">
                      {formatCurrency(comparisonListings.reduce((sum, l) => sum + l.price, 0) / comparisonListings.length)}
                    </div>
                  </div>
                  <div>
                    <span className="text-gray-600 dark:text-gray-400">Highest Price:</span>
                    <div className="font-medium text-gray-900 dark:text-white">
                      {formatCurrency(Math.max(...comparisonListings.map(l => l.price)))}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ComparisonModal;
