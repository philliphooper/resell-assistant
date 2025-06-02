# Resell Assistant 🚀

A comprehensive web application for finding profitable resale opportunities by analyzing marketplace trends and price comparisons across multiple platforms.

## ✅ Current Status

**Latest Update** (June 2, 2025): 🔧 **Connection Leak Issues Resolved** 
- ✅ Fixed dashboard loading states - no more immediate error messages
- ✅ Eliminated API connection leaks causing timeouts  
- ✅ Optimized request handling with 10s timeout for faster response
- ✅ Enhanced user experience with proper loading → connected → data flow

**Phase 1 Complete**: eBay API Integration fully operational with real marketplace data
- ✅ eBay API connection and authentication working
- ✅ Real-time product search across eBay marketplace
- ✅ Secure credential management with encryption
- ✅ Connection test functionality verified
- 🔄 **Phase 2 In Progress**: Facebook Marketplace integration

## Features

- **✅ eBay Integration**: Live eBay product search with real marketplace data
- **Multi-Platform Search**: Search across eBay, Amazon, Facebook Marketplace, and more
- **Price Trend Analysis**: Track historical pricing data to identify profitable opportunities
- **Profit Calculator**: Calculate potential profit margins with fees and shipping costs
- **Deal Alerts**: Get notified when items drop below target prices
- **Market Intelligence**: Analyze sell-through rates and demand patterns
- **Advanced Filtering**: Filter by price range, condition, location, and more
- **Portfolio Tracking**: Track your buying and selling activities

## Technology Stack

- **Frontend**: React with TypeScript, Tailwind CSS
- **Backend**: .NET 9 Web API
- **Database**: Entity Framework Core with SQLite
- **APIs**: eBay API, Amazon Product Advertising API, Facebook Marketplace
- **Deployment**: Docker support for easy deployment

## Getting Started

### Prerequisites

- .NET 9 SDK
- Node.js 18+
- npm

### Quick Start

The easiest way to start the application is using the provided startup scripts:

**For Windows:**
```bash
start-dev.bat
```

**For Linux/Mac:**
```bash
chmod +x start-dev.sh
./start-dev.sh
```

**Or manually:**
```bash
cd "Resell Assistant"
dotnet run
```

This single command will:
1. Start the .NET backend on ports 5000 (HTTP) and 5001 (HTTPS)
2. Automatically start the React frontend on port 3000
3. Configure proxy routing between frontend and backend

### How It Works

- **Backend**: .NET Web API serves on https://localhost:5001
- **Frontend**: React dev server runs on http://localhost:3000
- **Communication**: React proxies API calls to the .NET backend
- **Access**: Open https://localhost:5001 in your browser (the .NET app will proxy to React)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/philliphooper/resell-assistant.git
cd resell-assistant
```

2. Install frontend dependencies:
```bash
cd "Resell Assistant/ClientApp"
npm install
```

3. Install backend dependencies:
```bash
cd ../
dotnet restore
```

4. Run the application:
```bash
dotnet run
```

The application will be available at `https://localhost:5001`

## API Configuration

✅ **eBay API**: Fully configured and operational with production credentials

For additional marketplace access, you'll need to configure API keys:

1. Copy `appsettings.template.json` to `appsettings.json`
2. Add your API keys:
   - ✅ **eBay Developer API**: Working with production environment
   - Amazon Product Advertising API (coming soon)
   - Facebook Marketplace (Phase 2 - in progress)

### eBay API Setup
The eBay integration is already configured for production use. If you need to set up your own eBay developer account:
- See `EBAY_API_SETUP_GUIDE.md` for detailed setup instructions
- Use the secure credential management system in the application settings

## Architecture

### Backend Structure
- **Controllers**: RESTful API endpoints
- **Services**: Business logic for marketplace integration and price analysis
- **Models**: Data models for products, deals, and user portfolio
- **Data**: Entity Framework context and database configuration

### Frontend Structure
- **React Components**: Modern, responsive UI components
- **TypeScript**: Type-safe development
- **Tailwind CSS**: Utility-first styling
- **API Integration**: Axios-based API client

## Key Features

### Deal Analysis Algorithm
The application uses a sophisticated scoring system to identify profitable deals:
- Price comparison across multiple marketplaces
- Historical price trend analysis
- Market demand indicators
- Seller motivation factors
- Time-on-market analysis

### Search Capabilities
- Multi-marketplace search aggregation
- Advanced filtering options
- Real-time price monitoring
- Custom search alerts

### Portfolio Management
- Purchase tracking
- Profit/loss calculations
- Performance analytics
- Inventory management

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see LICENSE file for details

## Disclaimer

This tool is for educational and research purposes. Always comply with marketplace terms of service and applicable laws. Web scraping should be done responsibly and in accordance with robots.txt and rate limiting guidelines.

## Support

For questions, issues, or feature requests, please open an issue on GitHub.