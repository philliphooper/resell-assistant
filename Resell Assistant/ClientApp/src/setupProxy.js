const { createProxyMiddleware } = require('http-proxy-middleware');

module.exports = function(app) {
  // Proxy API calls to .NET backend
  app.use(
    '/api',
    createProxyMiddleware({
      target: 'https://localhost:5001', // Use HTTPS endpoint
      changeOrigin: true,
      secure: false, // Ignore SSL certificate errors in development
      logLevel: 'info', // Reduced log level
      timeout: 30000, // Increased timeout to 30 seconds
      proxyTimeout: 30000, // Increased proxy timeout to 30 seconds
      headers: {
        'Connection': 'keep-alive',
        'Accept': 'application/json'
      },
      // Enhanced retry logic
      retry: {
        times: 3,
        delay: 1000
      },
      onError: function (err, req, res) {
        console.log('API Proxy Error:', err.code || err.message);
        if (res && !res.headersSent) {
          res.status(502).json({ 
            error: 'Backend server connection failed', 
            details: err.code || err.message,
            suggestion: 'Please ensure the .NET backend is running on https://localhost:5001'
          });
        }
      },
      onProxyReq: function (proxyReq, req, res) {
        console.log(`→ ${req.method} ${req.url}`);
      },
      onProxyRes: function (proxyRes, req, res) {
        console.log(`← ${proxyRes.statusCode} ${req.url}`);
      }
    })
  );
};
