import { useState, useEffect, useCallback } from 'react';
import { Deal, Product, DashboardStats, ApiError } from '../types/api';
import apiService from '../services/api';

interface UseApiState<T> {
  data: T | null;
  loading: boolean;
  error: ApiError | null;
  refresh: () => void;
}

// Generic hook for API calls with retry logic
function useApi<T>(
  apiCall: () => Promise<T>,
  dependencies: any[] = []
): UseApiState<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await apiCall();
      setData(result);
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError);
      console.error('API call failed:', apiError);
      
      // For connection errors, automatically retry once after delay
      if (apiError.message?.includes('ECONNRESET') || apiError.message?.includes('ECONNREFUSED')) {
        console.log('Connection error detected, retrying in 2 seconds...');
        setTimeout(async () => {
          try {
            const retryResult = await apiCall();
            setData(retryResult);
            setError(null);
          } catch (retryErr) {
            console.error('Retry failed:', retryErr);
            // Keep the original error
          }
        }, 2000);
      }
    } finally {
      setLoading(false);
    }
  }, [apiCall]);
  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fetchData, ...dependencies]);

  const refresh = useCallback(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refresh };
}

// Specific hooks for different data types
export function useTopDeals(): UseApiState<Deal[]> {
  const apiCall = useCallback(() => apiService.getTopDeals(), []);
  return useApi(apiCall);
}

export function useRecentProducts(count: number = 10): UseApiState<Product[]> {
  const apiCall = useCallback(() => apiService.getRecentProducts(count), [count]);
  return useApi(apiCall, [count]);
}

export function useDashboardStats(): UseApiState<DashboardStats> {
  const apiCall = useCallback(() => apiService.getDashboardStats(), []);
  return useApi(apiCall);
}

export function useProduct(id: number | null): UseApiState<Product> {
  const apiCall = useCallback(
    () => id ? apiService.getProduct(id) : Promise.reject(new Error('No product ID provided')),
    [id]
  );
  return useApi(apiCall, [id]);
}

export function useProductSearch(query: string, marketplace?: string): UseApiState<Product[]> {
  const apiCall = useCallback(
    () => query ? apiService.searchProducts(query, marketplace) : Promise.resolve([]),
    [query, marketplace]
  );
  return useApi(apiCall, [query, marketplace]);
}

// Hook for API health check
export function useApiHealth() {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null);
  const [checking, setChecking] = useState(false);
  const [lastCheckTime, setLastCheckTime] = useState<number>(0);

  const checkHealth = useCallback(async () => {
    const now = Date.now();
    // Throttle health checks to prevent rapid successive calls
    if (now - lastCheckTime < 10000) { // Minimum 10 seconds between checks
      return;
    }
    
    setChecking(true);
    setLastCheckTime(now);
    try {
      const healthy = await apiService.checkHealth();
      setIsHealthy(healthy);
    } catch {
      setIsHealthy(false);
    } finally {
      setChecking(false);
    }
  }, [lastCheckTime]);
  useEffect(() => {
    checkHealth();
    // Check health every 5 minutes (reduced from 30 seconds)
    const interval = setInterval(checkHealth, 300000);
    return () => clearInterval(interval);
  }, [checkHealth]);

  return { isHealthy, checking, checkHealth };
}

// Hook for manual API calls (like analyze product)
export function useApiCall<T>() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const execute = useCallback(async (apiCall: () => Promise<T>): Promise<T | null> => {
    try {
      setLoading(true);
      setError(null);
      const result = await apiCall();
      return result;
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError);
      console.error('Manual API call failed:', apiError);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  return { execute, loading, error };
}

export default useApi;
