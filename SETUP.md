# Resell Assistant - Complete Setup Guide

## 🚀 Quick Start

### One Command Startup
From the root directory:
```bash
cd "Resell Assistant"
dotnet run
```

This single command:
- ✅ Starts .NET backend (ports 5001 HTTPS/5000 HTTP)
- ✅ Automatically starts React frontend (port 3000)
- ✅ Configures all proxy routing
- ✅ Opens your browser to https://localhost:5001

**Access your app at: https://localhost:5001**

## 📋 Prerequisites

### Required Software
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)
- [Visual Studio Code](https://code.visualstudio.com/) (recommended)
- Git

### VS Code Extensions (Recommended)
- C# Dev Kit
- C# 
- TypeScript and JavaScript Language Features
- Tailwind CSS IntelliSense
- ES7+ React/Redux/React-Native snippets
- Prettier - Code formatter
- Auto Rename Tag
- GitLens

## 🛠️ Detailed Setup Instructions

### 1. Clone and Setup
```bash
# Clone the repository
git clone https://github.com/philliphooper/resell-assistant.git
cd resell-assistant

# Open in VS Code
code .
```

### 2. Backend Setup
```bash
# Navigate to backend and restore packages
cd "Resell Assistant"
dotnet restore
dotnet build
```

### 3. Frontend Setup
```bash
# Install React dependencies
cd "ClientApp"
npm install
cd ..
```

### 4. Configuration
Copy and configure settings:
```bash
cp appsettings.template.json appsettings.json
```

Edit `appsettings.json` for your environment:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=resell_assistant.db"
  },
  "ApiKeys": {
    "EbayClientId": "YOUR_EBAY_CLIENT_ID",
    "EbayClientSecret": "YOUR_EBAY_CLIENT_SECRET"
  },
  "EbayApiSettings": {
    "Environment": "production",
    "BaseUrl": "https://api.ebay.com",
    "OAuthUrl": "https://auth.ebay.com",
    "RateLimitPerSecond": 5
  }
}
```

## 🎯 eBay API Setup (Required for Live Data)

### Step 1: Create eBay Developer Account
1. Visit: **https://developer.ebay.com/**
2. Click "Get Started" or "Join"
3. Sign in with existing eBay account or create new
4. Accept Developer Program Agreement

### Step 2: Create Application
1. Go to "My Account" → "Application Keysets"
2. Click "Create App Key"
3. Application Details:
   - **Name**: "Resell Assistant"
   - **Type**: "General Application"
   - **Description**: "Marketplace price analysis tool"

### Step 3: Configure OAuth Settings
- **Redirect URI**: `https://localhost:5001/auth/callback`
- **Scopes**: Select "Browse API" (for product search)

### Step 4: Get Your Credentials
You'll receive:
```
Production Client ID: YourApp-Production-a1b2c3d4
Production Client Secret: PRD-a1b2c3d4-e5f6-7890-abcd-efghijklmnop
```

### Step 5: Update Configuration
Add your credentials to `appsettings.json`:
```json
{
  "ApiKeys": {
    "EbayClientId": "YourApp-Production-a1b2c3d4",
    "EbayClientSecret": "PRD-a1b2c3d4-e5f6-7890-abcd-efghijklmnop"
  }
}
```

## 🏃‍♂️ Running the Application

### Option 1: VS Code (Recommended)
1. Press `F5` or click "Run > Start Debugging"
2. Select ".NET Core" if prompted
3. Application opens automatically at https://localhost:5001

### Option 2: Command Line
```bash
cd "Resell Assistant"
dotnet run
```

### Option 3: Batch File (Windows)
```bash
./start-dev.bat
```

### Option 4: Shell Script (Linux/Mac)
```bash
chmod +x start-dev.sh
./start-dev.sh
```

## 🌐 Access Points
- **Main Application**: https://localhost:5001
- **API Documentation**: https://localhost:5001/swagger
- **React Dev Server**: http://localhost:3000 (direct access)
- **HTTP Redirect**: http://localhost:5000 → https://localhost:5001

## 🔧 Development Tips

### VS Code Debugging
The project includes debugging configuration. If missing, create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/Resell Assistant/bin/Debug/net9.0/Resell Assistant.dll",
      "args": [],
      "cwd": "${workspaceFolder}/Resell Assistant",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Hot Reload
- **Backend**: Hot reload enabled for C# code changes
- **Frontend**: Auto-refresh for React/TypeScript changes
- **Database**: Automatically created on first run

## 🐛 Troubleshooting

### Port Issues
If ports are busy, kill processes:
```bash
# Windows
netstat -ano | findstr :5001
taskkill //F //PID <process_id>

# Linux/Mac
lsof -ti:5001 | xargs kill -9
```

### Database Issues
Reset database:
```bash
cd "Resell Assistant"
rm resell_assistant.db
dotnet run  # Database recreated automatically
```

### Node Modules Issues
```bash
cd "Resell Assistant/ClientApp"
rm -rf node_modules package-lock.json
npm install
```

### SSL Certificate Issues
```bash
dotnet dev-certs https --trust
```

### eBay API Issues
1. Verify credentials in application settings
2. Check eBay Developer Portal for application status
3. Ensure scopes include "Browse API"
4. Test connection via `/api/dashboard/test-ebay-connection`

## 📁 Project Structure
```
resell-assistant/
├── Resell Assistant/           # .NET Backend
│   ├── Controllers/           # API Controllers
│   ├── Services/             # Business Logic
│   │   └── External/         # External API Services
│   ├── Models/               # Data Models
│   │   └── Configuration/    # Configuration Models
│   ├── Data/                 # Database Context
│   ├── ClientApp/            # React Frontend
│   └── Program.cs            # Application Entry Point
├── Resell Assistant.Tests/    # Unit Tests
├── README.md                  # Project Overview
├── SETUP.md                   # This Setup Guide
└── start-dev.bat/sh          # Quick Start Scripts
```

## 🚀 Next Steps

After successful setup:
1. **Test eBay Integration**: Visit https://localhost:5001/swagger
2. **Search Products**: Use `/api/products/search` endpoint
3. **Configure Additional APIs**: Add Amazon, Facebook Marketplace
4. **Customize Features**: Modify search filters and analysis
5. **Deploy**: Configure for production deployment

## 📞 Support

For questions or issues:
1. Check this setup guide thoroughly
2. Review the troubleshooting section
3. Check project documentation in README.md
4. Open an issue on GitHub

You're now ready to start developing with real marketplace data! 🎉
