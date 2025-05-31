# Development Setup Guide

## Understanding Your Project Architecture

Your Resell Assistant is built using **ASP.NET Core with React SPA template**, which means:

### Project Structure
```
Resell Assistant/
├── Controllers/           # .NET API controllers
├── Services/             # Business logic services  
├── Data/                 # Entity Framework models
├── ClientApp/            # React frontend application
│   ├── src/              # React source code
│   ├── public/           # Static assets
│   └── package.json      # Frontend dependencies
└── Program.cs            # .NET application entry point
```

## How Development Startup Works

### Single Command Startup (Recommended)
Just run ONE command from the root directory:

```bash
cd "Resell Assistant"
dotnet run
```

**What this does automatically:**
1. Starts .NET backend on https://localhost:5001 and http://localhost:5000
2. Detects that `ClientApp/` exists and automatically runs `npm start`
3. Starts React dev server on http://localhost:3000
4. Configures proxy so React can call .NET APIs
5. Opens your browser to https://localhost:5001

### Port Configuration
- **5000**: HTTP backend (redirects to HTTPS)
- **5001**: HTTPS backend (main entry point)
- **3000**: React dev server (automatic)

### API Communication
Your React app (Search.tsx) makes calls like:
```typescript
fetch('/api/products/search?query=...')
```

This works because:
1. React's `package.json` has `"proxy": "https://localhost:5001"`
2. .NET's SPA middleware proxies non-API routes to React
3. Everything flows through port 5001 in your browser

## Alternative Startup Methods

### Using VS Code Tasks
1. Press `Ctrl+Shift+P`
2. Type "Tasks: Run Task"
3. Select "start-dev" or "watch"

### Using Batch Scripts
- Windows: Double-click `start-dev.bat`
- Linux/Mac: Run `./start-dev.sh`

## Troubleshooting

### Port Conflicts
If ports are busy:
```bash
# Check what's using ports
netstat -ano | findstr :5001
netstat -ano | findstr :3000

# Kill processes if needed
taskkill /PID <process-id> /F
```

### Node.js Issues
```bash
# Clear npm cache
cd "Resell Assistant/ClientApp"
npm cache clean --force
rm -rf node_modules
npm install
```

### Database Issues
```bash
# Recreate database
cd "Resell Assistant"
rm resell_assistant.db
dotnet run
```

## Production Build

```bash
cd "Resell Assistant"
dotnet publish -c Release -o ../publish-prod
```

This creates a single deployable package with the React app built and embedded.

## Key Files Explained

- **Program.cs**: Configures both .NET and React integration
- **package.json**: Contains React proxy settings
- **Resell Assistant.csproj**: Contains SPA build configuration
- **launchSettings.json**: Defines development ports

## Environment Variables

Development automatically uses:
- `ASPNETCORE_ENVIRONMENT=Development`
- Database: SQLite file `resell_assistant.db`
- APIs: Mock/development endpoints

## Common Development Workflow

1. Start development: `dotnet run`
2. Edit React code in `ClientApp/src/` - auto-reloads
3. Edit .NET code in Controllers/Services/ - auto-restarts
4. View at https://localhost:5001
5. API docs at https://localhost:5001/swagger
