# Resell Assistant 🚀

A comprehensive web application for finding profitable resale opportunities by analyzing marketplace trends and price comparisons across multiple platforms.

## Features

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
- npm or yarn

### Installation

1. Clone the repository:
```bash
git clone https://github.com/philliphooper/resell-assistant.git
cd resell-assistant
```

2. Install frontend dependencies:
```bash
cd ClientApp
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

You'll need to configure API keys for marketplace access:

1. Copy `appsettings.example.json` to `appsettings.json`
2. Add your API keys:
   - eBay Developer API
   - Amazon Product Advertising API
   - Facebook Marketplace (if available)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see LICENSE file for details

## Disclaimer

This tool is for educational and research purposes. Always comply with marketplace terms of service and applicable laws.
