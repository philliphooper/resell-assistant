import React, { useState } from 'react';
import { DealDiscoverySettings } from '../types/settings';

interface DealDiscoverySettingsProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (settings: DealDiscoverySettings) => void;
  currentSettings?: DealDiscoverySettings;
}

const DealDiscoverySettingsModal: React.FC<DealDiscoverySettingsProps> = ({
  isOpen,
  onClose,
  onSave,
  currentSettings
}) => {
  const [settings, setSettings] = useState<DealDiscoverySettings>(
    currentSettings || {
      exactResultCount: 10,
      targetBuyPrice: undefined,
      uniqueProductCount: 5,
      listingsPerProduct: 5,
      searchTerms: '',
      minProfitMargin: 15,
      preferredMarketplaces: ['eBay'], // Facebook Marketplace temporarily disabled
      enableNotifications: true,
    }
  );

  const handleSave = () => {
    onSave(settings);
    onClose();
  };

  const handleMarketplaceToggle = (marketplace: string) => {
    setSettings(prev => ({
      ...prev,
      preferredMarketplaces: prev.preferredMarketplaces.includes(marketplace)
        ? prev.preferredMarketplaces.filter(m => m !== marketplace)
        : [...prev.preferredMarketplaces, marketplace]
    }));
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={onClose}
        ></div>

        {/* Modal */}
        <div className="inline-block align-bottom bg-white dark:bg-gray-800 rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white dark:bg-gray-800 px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="sm:flex sm:items-start">
              <div className="mt-3 text-center sm:mt-0 sm:text-left w-full">                <h3 className="text-lg leading-6 font-medium text-gray-900 dark:text-white mb-2">
                  Intelligent Discovery Settings
                </h3>
                <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                  Configure parameters for intelligent product discovery with guaranteed exact results
                </p>
                  <div className="space-y-6">
                  {/* Exact Result Count */}
                  <div>
                    <label htmlFor="exactResultCount" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Exact Result Count
                    </label>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                      The system will find exactly this many deals (not maximum)
                    </p>
                    <input
                      type="number"
                      id="exactResultCount"
                      min="1"
                      max="100"
                      value={settings.exactResultCount}
                      onChange={(e) => setSettings(prev => ({ ...prev, exactResultCount: parseInt(e.target.value) || 1 }))}
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>

                  {/* Unique Product Count */}
                  <div>
                    <label htmlFor="uniqueProductCount" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Unique Product Count
                    </label>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                      Number of different products to discover
                    </p>
                    <input
                      type="number"
                      id="uniqueProductCount"
                      min="1"
                      max="50"
                      value={settings.uniqueProductCount}
                      onChange={(e) => setSettings(prev => ({ ...prev, uniqueProductCount: parseInt(e.target.value) || 1 }))}
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>

                  {/* Listings Per Product */}
                  <div>
                    <label htmlFor="listingsPerProduct" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Listings Per Product
                    </label>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                      Number of listings to analyze for each product (for comparison)
                    </p>
                    <input
                      type="number"
                      id="listingsPerProduct"
                      min="1"
                      max="20"
                      value={settings.listingsPerProduct}
                      onChange={(e) => setSettings(prev => ({ ...prev, listingsPerProduct: parseInt(e.target.value) || 1 }))}
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>

                  {/* Search Terms */}
                  <div>
                    <label htmlFor="searchTerms" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Search Terms (Optional)
                    </label>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                      Specific keywords to focus on, leave empty for trending products
                    </p>
                    <input
                      type="text"
                      id="searchTerms"
                      value={settings.searchTerms || ''}
                      onChange={(e) => setSettings(prev => ({ ...prev, searchTerms: e.target.value }))}
                      placeholder="e.g., iPhone, MacBook, vintage watches"
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>

                  {/* Target Buy Price */}
                  <div>
                    <label htmlFor="targetBuyPrice" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Target Buy Price (Optional)
                    </label>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                      Focus on products within this price range
                    </p>
                    <input
                      type="number"
                      id="targetBuyPrice"
                      min="0.01"
                      max="10000"
                      step="0.01"
                      value={settings.targetBuyPrice || ''}
                      onChange={(e) => setSettings(prev => ({ ...prev, targetBuyPrice: e.target.value ? parseFloat(e.target.value) : undefined }))}
                      placeholder="Enter maximum buy price"
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>

                  {/* Min Profit Margin */}
                  <div>
                    <label htmlFor="minProfitMargin" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Minimum Profit Margin (%)
                    </label>
                    <input
                      type="number"
                      id="minProfitMargin"
                      min="5"
                      max="100"
                      value={settings.minProfitMargin}
                      onChange={(e) => setSettings(prev => ({ ...prev, minProfitMargin: parseInt(e.target.value) || 5 }))}
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>

                  {/* Preferred Marketplaces */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Preferred Marketplaces
                    </label>                    <div className="space-y-2">
                      {['eBay'].map((marketplace) => ( // Facebook Marketplace temporarily disabled
                        <label key={marketplace} className="flex items-center">
                          <input
                            type="checkbox"
                            checked={settings.preferredMarketplaces.includes(marketplace)}
                            onChange={() => handleMarketplaceToggle(marketplace)}
                            className="rounded border-gray-300 text-blue-600 shadow-sm focus:border-blue-300 focus:ring focus:ring-blue-200 focus:ring-opacity-50"
                          />
                          <span className="ml-2 text-sm text-gray-700 dark:text-gray-300">{marketplace}</span>
                        </label>
                      ))}
                    </div>
                  </div>

                  {/* Enable Notifications */}
                  <div>
                    <label className="flex items-center">
                      <input
                        type="checkbox"
                        checked={settings.enableNotifications}
                        onChange={(e) => setSettings(prev => ({ ...prev, enableNotifications: e.target.checked }))}
                        className="rounded border-gray-300 text-blue-600 shadow-sm focus:border-blue-300 focus:ring focus:ring-blue-200 focus:ring-opacity-50"
                      />
                      <span className="ml-2 text-sm text-gray-700 dark:text-gray-300">
                        Enable notifications for new deals
                      </span>
                    </label>
                  </div>
                </div>
              </div>
            </div>
          </div>
          
          <div className="bg-gray-50 dark:bg-gray-700 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              type="button"
              onClick={handleSave}
              className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm"
            >
              Save Settings
            </button>
            <button
              type="button"
              onClick={onClose}
              className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-600 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DealDiscoverySettingsModal;
