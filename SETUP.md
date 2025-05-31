# Setup and Development Guide

## Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)
- [Visual Studio Code](https://code.visualstudio.com/)
- Git

## VS Code Extensions (Recommended)
Install these extensions for the best development experience:
- C# Dev Kit
- C# 
- TypeScript and JavaScript Language Features
- Tailwind CSS IntelliSense
- ES7+ React/Redux/React-Native snippets
- Prettier - Code formatter
- Auto Rename Tag
- GitLens

## Step-by-Step Setup

### 1. Clone the Repository
```bash
# Clone the repository
git clone https://github.com/philliphooper/resell-assistant.git

# Navigate to the project directory
cd resell-assistant
```

### 2. Open in VS Code
```bash
# Open the project in VS Code
code .
```

### 3. Backend Setup (.NET)
```bash
# Navigate to the backend project
cd "Resell Assistant"

# Restore .NET packages
dotnet restore

# Build the project (this will also build the React app)
dotnet build
```

### 4. Frontend Setup (React)
```bash
# Navigate to the React app directory
cd "ClientApp"

# Install Node.js dependencies
npm install

# Return to the main project directory
cd ..
```

### 5. Configuration
1. Copy the example configuration:
```bash
cp appsettings.json appsettings.Development.json
```

2. Edit `appsettings.Development.json` and add your API keys:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=resell_assistant.db"
  },
  "ApiKeys": {
    "EbayClientId": "YOUR_EBAY_CLIENT_ID",
    "EbayClientSecret": "YOUR_EBAY_CLIENT_SECRET"
  }
}
```

## Running the Application

### Option 1: VS Code Debugging (Recommended)

1. **Open VS Code** in the project root directory
2. **Press F5** or click "Run > Start Debugging"
3. Select ".NET Core" if prompted
4. The application will start with the debugger attached

The app will be available at:
- Backend API: `https://localhost:5001`
- Frontend: `http://localhost:3000` (auto-opens)
- Swagger API Docs: `https://localhost:5001/swagger`

### Option 2: Command Line

#### Start Backend Only:
```bash
cd "Resell Assistant"
dotnet run
```

#### Start Frontend Only (separate terminal):
```bash
cd "Resell Assistant/ClientApp"
npm start
```

#### Start Both (Production Mode):
```bash
cd "Resell Assistant"
dotnet run --launch-profile "Resell Assistant"
```

## Debugging Tips

### VS Code Launch Configuration
The project includes a `.vscode/launch.json` file for debugging. If it doesn't exist, create it:

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
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

### Common Issues and Solutions

1. **Port Already in Use**:
   - Change ports in `Properties/launchSettings.json`
   - Or kill the process using the port

2. **Database Issues**:
   - Delete `resell_assistant.db` file and restart
   - The database will be recreated automatically

3. **Node Modules Issues**:
   ```bash
   cd "Resell Assistant/ClientApp"
   rm -rf node_modules
   npm install
   ```

4. **SSL Certificate Issues**:
   ```bash
   dotnet dev-certs https --trust
   ```

## Development Workflow

1. **Set Breakpoints** in your C# code
2. **Press F5** to start debugging
3. **Make changes** to backend code - hot reload is enabled
4. **Frontend changes** auto-refresh in the browser
5. **Use Swagger** at `/swagger` to test API endpoints

## Project Structure

```
resell-assistant/
├── Resell Assistant/           # .NET Backend
│   ├── Controllers/           # API Controllers
│   ├── Services/             # Business Logic
│   ├── Models/               # Data Models
│   ├── Data/                 # Database Context
│   ├── ClientApp/            # React Frontend
│   └── Program.cs            # Application Entry Point
├── README.md
├── SETUP.md                  # This file
└── Dockerfile               # Container Configuration
```

## Next Development Steps

1. **Complete the React frontend components**
2. **Add authentication/authorization**
3. **Implement real marketplace API integrations**
4. **Add comprehensive error handling**
5. **Write unit tests**
6. **Set up deployment pipeline**

You're now ready to start developing! 🚀
