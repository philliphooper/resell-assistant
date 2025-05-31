# Development Access Guide

## 🎯 **Recommended Development Workflow**

### **Option 1: Full-Stack Development (Recommended)**
Access your app at: **http://localhost:3000**
- ✅ React hot reload works perfectly
- ✅ API calls proxy to .NET backend
- ✅ No WebSocket errors
- ✅ Full development experience

### **Option 2: Backend-First Development**
Access your app at: **https://localhost:5001**
- ✅ Serves through .NET (production-like)
- ⚠️ WebSocket errors (hot reload disabled)
- ✅ API calls work directly

## 🚀 **How to Start for Best Experience**

**Step 1**: Start both servers
```bash
# Terminal 1: Start .NET backend
cd "Resell Assistant"
dotnet run

# Terminal 2: Start React frontend
cd "Resell Assistant/ClientApp"
npm start
```

**Step 2**: Access the app
- **Primary**: http://localhost:3000 (Best for development)
- **Secondary**: https://localhost:5001 (Testing production flow)

## ⚡ **Why This Setup Works Better**

### **At localhost:3000:**
- React dev server handles the page
- Hot reload works perfectly
- API calls proxy to backend
- WebSocket connections work

### **At localhost:5001:**
- .NET server handles the page
- Mimics production setup
- WebSocket errors (expected in this mode)
- Good for final testing

## 🛠️ **WebSocket Error Solutions**

If you see `WebSocket connection to 'wss://localhost:3000/ws' failed`:

### **Solution 1 (Recommended): Use React Dev Server**
```bash
# Access your app at:
http://localhost:3000
```

### **Solution 2: Ignore WebSocket Errors**
- The error is harmless when using .NET server
- Your app still works, just no hot reload
- Good for production testing

### **Solution 3: Add to Browser Console**
```javascript
// Disable WebSocket reconnection attempts
window.__REACT_DEVTOOLS_GLOBAL_HOOK__ = { onCommitFiberRoot: () => {}, onCommitFiberUnmount: () => {} };
```
