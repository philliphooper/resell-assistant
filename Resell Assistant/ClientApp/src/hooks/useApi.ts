import { useState, useEffect, useCallback } from 'react';
import { Deal, Product, DashboardStats, ApiError } from '../types/api';
import apiService from '../services/api';

interface UseApiState<T> {
  data: T | null;
  loading: boolean;
  error: ApiError | null;
  refresh: () => void;
}

// Generic hook for API calls
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
    } finally {
      setLoading(false);
    }
  }, dependencies);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const refresh = useCallback(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refresh };
}

// Specific hooks for different data types
export function useTopDeals(): UseApiState<Deal[]> {
  return useApi(() => apiService.getTopDeals());
}

export function useRecentProducts(count: number = 10): UseApiState<Product[]> {
  return useApi(() => apiService.getRecentProducts(count), [count]);
}

export function useDashboardStats(): UseApiState<DashboardStats> {
  return useApi(() => apiService.getDashboardStats());
}

export function useProduct(id: number | null): UseApiState<Product> {
  return useApi(
    () => id ? apiService.getProduct(id) : Promise.reject(new Error('No product ID provided')),
    [id]
  );
}

export function useProductSearch(query: string, marketplace?: string): UseApiState<Product[]> {
  return useApi(
    () => query ? apiService.searchProducts(query, marketplace) : Promise.resolve([]),
    [query, marketplace]
  );
}

// Hook for API health check
export function useApiHealth() {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null);
  const [checking, setChecking] = useState(false);

  const checkHealth = useCallback(async () => {
    setChecking(true);
    try {
      const healthy = await apiService.checkHealth();
      setIsHealthy(healthy);
    } catch {
      setIsHealthy(false);
    } finally {
      setChecking(false);
    }
  }, []);

  useEffect(() => {
    checkHealth();
    // Check health every 30 seconds
    const interval = setInterval(checkHealth, 30000);
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
