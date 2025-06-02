import { useState, useEffect, useCallback, useRef } from 'react';
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
      setLoading(false);
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError);
      console.error('API call failed:', apiError);
      
      // For connection errors, automatically retry once after delay
      if (apiError.message?.includes('ECONNRESET') || 
          apiError.message?.includes('ECONNREFUSED') ||
          apiError.message?.includes('fetch')) {
        console.log('Connection error detected, retrying in 3 seconds...');
        // Keep loading=true during retry
        setTimeout(async () => {
          try {
            setError(null); // Clear error during retry
            const retryResult = await apiCall();
            setData(retryResult);
          } catch (retryErr) {
            console.error('Retry failed:', retryErr);
            setError(retryErr as ApiError);
          } finally {
            setLoading(false); // Only set loading=false after retry completes
          }
        }, 3000); // Reduced retry delay to 3 seconds for better UX
      } else {
        setLoading(false); // Set loading=false immediately for non-retry errors
      }
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
  const [checking, setChecking] = useState(true); // Start with checking=true to show loading state
  const lastCheckTimeRef = useRef<number>(0);

  console.log('[useApiHealth] Render - isHealthy:', isHealthy, 'checking:', checking);

  const checkHealth = useCallback(async () => {
    const now = Date.now();
    // Throttle health checks to prevent rapid successive calls, but allow first check
    if (lastCheckTimeRef.current > 0 && now - lastCheckTimeRef.current < 30000) { // Allow immediate first check
      console.log('[useApiHealth] Throttled health check, skipping');
      return;
    }
    
    console.log('[useApiHealth] Starting health check');
    setChecking(true);
    lastCheckTimeRef.current = now;
    try {
      const healthy = await apiService.checkHealth();
      console.log('[useApiHealth] Health check result:', healthy);
      setIsHealthy(healthy);
    } catch (error) {
      console.error('[useApiHealth] Health check failed:', error);
      setIsHealthy(false);
    } finally {
      console.log('[useApiHealth] Health check finished, setting checking=false');
      setChecking(false);
    }
  }, []);
  
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    checkHealth();
    // Check health every 10 minutes for better stability
    const interval = setInterval(checkHealth, 600000);
    return () => clearInterval(interval);
  }, []); // Empty dependency array to run only once

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
