# 🚀 Simplified Startup Guide - Resell Assistant

## ✅ Your Project is Now Optimized!

### One Command to Rule Them All

From the root folder, run:
```bash
cd "Resell Assistant"
dotnet run
```

**That's it!** This single command:
- ✅ Starts .NET backend (ports 5000/5001)
- ✅ Automatically starts React frontend (port 3000)  
- ✅ Configures all proxy routing
- ✅ Opens your browser to https://localhost:5001

## 🔧 What Was Fixed

### Before (Confusing):
- Had to manually manage two processes
- Port conflicts and proxy issues
- Unclear startup order
- SSL certificate problems

### After (Simple):
- Single command startup
- Automatic port management
- Robust error handling
- SSL fallback configured

## 🌐 How to Access Your App

**Primary URL:** https://localhost:5001
- This serves your React app through the .NET backend
- All API calls work seamlessly
- SSL certificate warnings are handled gracefully

**Alternative URLs:**
- http://localhost:5000 (redirects to HTTPS)
- http://localhost:3000 (direct React dev server)

## 📁 Project Architecture Summary

```
Your Browser (https://localhost:5001)
    ↓
.NET Backend (ports 5000/5001)
    ↓ (proxies non-API routes to)
React Frontend (port 3000)
    ↓ (API calls proxied back to)
.NET API Controllers (/api/*)
```

## 🛠️ VS Code Integration

You can also start from VS Code:
1. Press `Ctrl+Shift+P`
2. Type "Tasks: Run Task"
3. Select "start-dev"

## 🐛 Quick Troubleshooting

**Port Busy Error:**
```bash
netstat -ano | findstr :5001
# Kill the process if needed
```

**Node.js Issues:**
```bash
cd "Resell Assistant/ClientApp"
npm cache clean --force
npm install
```

**Database Issues:**
```bash
cd "Resell Assistant"
rm resell_assistant.db
dotnet run
```

## 📚 Additional Resources

- `DEV_GUIDE.md` - Comprehensive development documentation
- `README.md` - Project overview and features
- VS Code Tasks - Use Ctrl+Shift+P → "Tasks: Run Task"

## 🎉 You're All Set!

Your Resell Assistant is now configured for the smoothest possible development experience. Just run `dotnet run` and start coding!
