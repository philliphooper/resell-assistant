import React from 'react';
import { useApiHealth } from '../hooks/useApi';

const ApiStatusLED: React.FC = () => {
  const { isHealthy, checking: healthChecking, checkHealth } = useApiHealth();

  const getStatusColor = () => {
    if (healthChecking || isHealthy === null) {
      return 'bg-yellow-500 animate-pulse';
    }
    return isHealthy ? 'bg-green-500' : 'bg-red-500';
  };

  const getStatusText = () => {
    if (healthChecking || isHealthy === null) {
      return 'Checking API connection...';
    }
    return isHealthy ? 'API Connected' : 'API Connection Failed';
  };

  const handleClick = () => {
    if (!isHealthy && !healthChecking) {
      checkHealth();
    }
  };

  return (
    <div className="flex items-center">
      <button
        onClick={handleClick}
        className={`w-3 h-3 rounded-full mr-2 transition-all duration-200 ${getStatusColor()} ${
          !isHealthy && !healthChecking ? 'cursor-pointer hover:scale-110' : 'cursor-default'
        }`}
        title={getStatusText()}
        aria-label={getStatusText()}
        disabled={healthChecking}
      />
      {!isHealthy && !healthChecking && (
        <span className="text-xs text-red-600 dark:text-red-400 mr-2">
          API Issue
        </span>
      )}
    </div>
  );
};

export default ApiStatusLED;
