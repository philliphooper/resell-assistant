import React from 'react';

const Search: React.FC = () => {
  return (
    <div className="space-y-6">
      <div className="sm:flex sm:items-center">
        <div className="sm:flex-auto">
          <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Search</h1>
          <p className="mt-2 text-sm text-gray-700 dark:text-gray-300">
            Search for products across multiple marketplaces
          </p>
        </div>
      </div>

      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700 transition-colors duration-300">
        <p className="text-center text-gray-500 dark:text-gray-400 py-8">
          Search functionality will be implemented here
        </p>
      </div>
    </div>
  );
};

export default Search;
