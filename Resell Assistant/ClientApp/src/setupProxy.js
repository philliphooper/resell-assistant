const { createProxyMiddleware } = require('http-proxy-middleware');

module.exports = function(app) {
  // Proxy API calls to .NET backend
  app.use(
    '/api',
    createProxyMiddleware({
      target: 'https://localhost:5001',
      changeOrigin: true,
      secure: false, // Ignore SSL certificate errors in development
      logLevel: 'info',
      onError: function (err, req, res) {
        console.log('API Proxy Error:', err.message);
        // Fallback to HTTP if HTTPS fails
        createProxyMiddleware({
          target: 'http://localhost:5000',
          changeOrigin: true,
          logLevel: 'info'
        })(req, res);
      }
    })
  );

  // Handle WebSocket connections for hot reload
  app.use(
    '/ws',
    createProxyMiddleware({
      target: 'ws://localhost:3000',
      ws: true,
      changeOrigin: true,
      logLevel: 'info',
      onError: function (err, req, socket) {
        console.log('WebSocket Proxy Error:', err.message);
      }
    })
  );
};
