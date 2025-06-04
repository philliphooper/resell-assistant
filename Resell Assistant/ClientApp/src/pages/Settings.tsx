import React, { useState, useEffect } from 'react';
import { ExclamationCircleIcon, CheckCircleIcon, CogIcon } from '@heroicons/react/24/outline';
import LoadingSpinner from '../components/LoadingSpinner';

interface CredentialStatus {
  service: string;
  isConfigured: boolean;
  environment: string;
  lastUpdated?: string;
}

interface CredentialStatusResponse {
  eBay: CredentialStatus;
}

interface ApiCredentialsRequest {
  service: string;
  clientId: string;
  clientSecret: string;
  environment: string;
}

const Settings: React.FC = () => {
  const [credentialStatus, setCredentialStatus] = useState<CredentialStatusResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [testLoading, setTestLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  
  const [formData, setFormData] = useState<ApiCredentialsRequest>({
    service: 'eBay',
    clientId: '',
    clientSecret: '',
    environment: 'sandbox'
  });

  useEffect(() => {
    loadCredentialStatus();
  }, []);

  const loadCredentialStatus = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/settings/credentials/status');
      if (response.ok) {
        const data = await response.json();
        setCredentialStatus(data);
      } else {
        setError('Failed to load credential status');
      }
    } catch (err) {
      setError('Failed to connect to server');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const response = await fetch('/api/settings/credentials/ebay', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData),
      });

      if (response.ok) {
        setSuccess('eBay credentials saved successfully!');
        setShowForm(false);
        setFormData({ service: 'eBay', clientId: '', clientSecret: '', environment: 'sandbox' });
        await loadCredentialStatus();
      } else {
        const errorData = await response.json();
        setError(errorData.message || 'Failed to save credentials');
      }
    } catch (err) {
      setError('Failed to save credentials');
    } finally {
      setLoading(false);
    }
  };

  const handleTest = async () => {
    setTestLoading(true);
    setError(null);
    setSuccess(null);

    try {      const response = await fetch('/api/settings/credentials/ebay/test', {
        method: 'POST',
      });
      
      if (response.ok) {
        await response.json(); // Read response to prevent memory leaks
        setSuccess('eBay API connection test successful!');
      } else {
        const errorData = await response.json();
        setError(errorData.message || 'Connection test failed');
      }
    } catch (err) {
      setError('Failed to test connection');
    } finally {
      setTestLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!window.confirm('Are you sure you want to delete your eBay credentials?')) {
      return;
    }

    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const response = await fetch('/api/settings/credentials/ebay', {
        method: 'DELETE',
      });

      if (response.ok) {
        setSuccess('eBay credentials deleted successfully!');
        await loadCredentialStatus();
      } else {
        const errorData = await response.json();
        setError(errorData.message || 'Failed to delete credentials');
      }
    } catch (err) {
      setError('Failed to delete credentials');
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  if (loading && !credentialStatus) {
    return <LoadingSpinner />;
  }

  return (
    <div className="space-y-6">
      <div className="md:flex md:items-center md:justify-between">
        <div className="flex-1 min-w-0">
          <h2 className="text-2xl font-bold leading-7 text-gray-900 dark:text-white sm:text-3xl sm:truncate">
            Settings
          </h2>
          <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
            Manage your API credentials and application settings
          </p>
        </div>
      </div>

      {/* Alert Messages */}
      {error && (
        <div className="rounded-md bg-red-50 dark:bg-red-900/20 p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <ExclamationCircleIcon className="h-5 w-5 text-red-400" aria-hidden="true" />
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-red-800 dark:text-red-300">{error}</p>
            </div>
          </div>
        </div>
      )}

      {success && (
        <div className="rounded-md bg-green-50 dark:bg-green-900/20 p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <CheckCircleIcon className="h-5 w-5 text-green-400" aria-hidden="true" />
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-green-800 dark:text-green-300">{success}</p>
            </div>
          </div>
        </div>
      )}

      {/* eBay Credentials Section */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <div className="md:flex md:items-center md:justify-between">
            <div className="flex-1 min-w-0">
              <h3 className="text-lg font-medium leading-6 text-gray-900 dark:text-white">
                eBay API Credentials
              </h3>
              <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                Configure your eBay API credentials for marketplace data integration
              </p>
              {credentialStatus?.eBay && (
                <div className="mt-2 flex items-center space-x-4">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                    credentialStatus.eBay.isConfigured
                      ? 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'
                      : 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'
                  }`}>
                    {credentialStatus.eBay.isConfigured ? 'Configured' : 'Not Configured'}
                  </span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">
                    Environment: {credentialStatus.eBay.environment}
                  </span>
                  {credentialStatus.eBay.lastUpdated && (
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      Last updated: {new Date(credentialStatus.eBay.lastUpdated).toLocaleDateString()}
                    </span>
                  )}
                </div>
              )}
            </div>
            <div className="mt-4 md:mt-0 md:ml-4 flex space-x-3">
              {credentialStatus?.eBay?.isConfigured && (
                <>
                  <button
                    onClick={handleTest}
                    disabled={testLoading}
                    className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
                  >
                    {testLoading ? <LoadingSpinner size="sm" className="mr-2" /> : <CogIcon className="mr-2 h-4 w-4" />}
                    Test Connection
                  </button>
                  <button
                    onClick={handleDelete}
                    disabled={loading}
                    className="inline-flex items-center px-4 py-2 border border-red-300 dark:border-red-600 rounded-md shadow-sm text-sm font-medium text-red-700 dark:text-red-300 bg-white dark:bg-gray-700 hover:bg-red-50 dark:hover:bg-red-900/20 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50"
                  >
                    Delete
                  </button>
                </>
              )}
              <button
                onClick={() => setShowForm(!showForm)}
                className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
              >
                {credentialStatus?.eBay?.isConfigured ? 'Update' : 'Configure'}
              </button>
            </div>
          </div>

          {/* Credentials Form */}
          {showForm && (
            <div className="mt-6 border-t border-gray-200 dark:border-gray-700 pt-6">
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label htmlFor="clientId" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Client ID
                    </label>
                    <input
                      type="text"
                      name="clientId"
                      id="clientId"
                      value={formData.clientId}
                      onChange={handleInputChange}
                      required
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-primary-500 focus:border-primary-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                      placeholder="Your eBay Client ID"
                    />
                  </div>
                  <div>
                    <label htmlFor="clientSecret" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Client Secret
                    </label>
                    <input
                      type="password"
                      name="clientSecret"
                      id="clientSecret"
                      value={formData.clientSecret}
                      onChange={handleInputChange}
                      required
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-primary-500 focus:border-primary-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                      placeholder="Your eBay Client Secret"
                    />
                  </div>
                  <div className="sm:col-span-2">
                    <label htmlFor="environment" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                      Environment
                    </label>
                    <select
                      name="environment"
                      id="environment"
                      value={formData.environment}
                      onChange={handleInputChange}
                      className="mt-1 block w-full border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-primary-500 focus:border-primary-500 sm:text-sm dark:bg-gray-700 dark:text-white"
                    >
                      <option value="sandbox">Sandbox (Testing)</option>
                      <option value="production">Production</option>
                    </select>
                  </div>
                </div>

                <div className="bg-blue-50 dark:bg-blue-900/20 p-4 rounded-md">
                  <p className="text-sm text-blue-700 dark:text-blue-300">
                    <strong>Note:</strong> Your credentials are encrypted and stored securely. They are never synced to GitHub or exposed in logs.
                    For production use, ensure you have the proper eBay developer account and application approval.
                  </p>
                </div>

                <div className="flex justify-end space-x-3">
                  <button
                    type="button"
                    onClick={() => setShowForm(false)}
                    className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={loading}
                    className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
                  >
                    {loading ? <LoadingSpinner size="sm" className="mr-2" /> : null}
                    Save Credentials
                  </button>
                </div>
              </form>
            </div>
          )}
        </div>
      </div>

      {/* Setup Instructions */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <h3 className="text-lg font-medium leading-6 text-gray-900 dark:text-white mb-4">
            eBay Developer Setup Instructions
          </h3>
          <div className="space-y-4 text-sm text-gray-600 dark:text-gray-400">
            <div>
              <h4 className="font-medium text-gray-900 dark:text-white">1. Create eBay Developer Account</h4>
              <p>Visit <a href="https://developer.ebay.com" target="_blank" rel="noopener noreferrer" className="text-primary-600 hover:text-primary-700">https://developer.ebay.com</a> and create a developer account.</p>
            </div>
            <div>
              <h4 className="font-medium text-gray-900 dark:text-white">2. Create Application</h4>
              <p>Go to "My Account" → "Application Keysets" → "Create App Key" and create a new application with Browse API access.</p>
            </div>
            <div>
              <h4 className="font-medium text-gray-900 dark:text-white">3. Get Credentials</h4>
              <p>Copy your Client ID and Client Secret from the eBay Developer portal and enter them above.</p>
            </div>
            <div>
              <h4 className="font-medium text-gray-900 dark:text-white">4. Test Configuration</h4>
              <p>Use the "Test Connection" button to verify your credentials are working correctly.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Settings;
