@echo off
echo Starting Resell Assistant in Development Mode...
echo.
echo This will start both the .NET backend (ports 5001 HTTPS/5000 HTTP) and React frontend (port 3000)
echo Access your application at: https://localhost:5001
echo.
cd "Resell Assistant"
dotnet run
