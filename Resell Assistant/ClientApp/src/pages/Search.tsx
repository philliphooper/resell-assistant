import React, { useState } from 'react';

interface Product {
  id: number;
  title: string;
  price: number;
  marketplace: string;
  url?: string;
}

const Search: React.FC = () => {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Product[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    setTouched(true);
    setError(null);
    setResults([]);
    if (query.trim().length < 2) {
      setError('Search query must be at least 2 characters.');
      return;
    }
    setLoading(true);
    try {
      const resp = await fetch(`/api/products/search?query=${encodeURIComponent(query)}`);
      if (!resp.ok) {
        const data = await resp.json();
        setError(data.details || data.message || 'Search failed.');
      } else {
        const data = await resp.json();
        setResults(data);
        if (data.length === 0) setError('No results found.');
      }
    } catch (err) {
      setError('Network error.');
    } finally {
      setLoading(false);
    }
  };

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

      <form onSubmit={handleSearch} className="flex items-center space-x-2">
        <input
          type="text"
          className="flex-1 rounded border px-3 py-2 text-gray-900 dark:text-white bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Search products..."
          value={query}
          onChange={e => setQuery(e.target.value)}
          onBlur={() => setTouched(true)}
        />
        <button
          type="submit"
          className="px-4 py-2 rounded bg-blue-600 text-white font-semibold hover:bg-blue-700 transition-colors"
          disabled={loading}
        >
          {loading ? 'Searching...' : 'Search'}
        </button>
      </form>

      {error && touched && (
        <div className="text-red-600 dark:text-red-400 text-center">{error}</div>
      )}

      {!error && results.length > 0 && (
        <div className="mt-6">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead>
              <tr>
                <th className="px-4 py-2 text-left">Title</th>
                <th className="px-4 py-2 text-left">Price</th>
                <th className="px-4 py-2 text-left">Marketplace</th>
                <th className="px-4 py-2 text-left">Link</th>
              </tr>
            </thead>
            <tbody>
              {results.map(product => (
                <tr key={product.id} className="border-b border-gray-100 dark:border-gray-700">
                  <td className="px-4 py-2">{product.title}</td>
                  <td className="px-4 py-2">${product.price.toFixed(2)}</td>
                  <td className="px-4 py-2">{product.marketplace}</td>
                  <td className="px-4 py-2">
                    {product.url ? (
                      <a href={product.url} target="_blank" rel="noopener noreferrer" className="text-blue-600 underline">View</a>
                    ) : (
                      <span className="text-gray-400">N/A</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default Search;
