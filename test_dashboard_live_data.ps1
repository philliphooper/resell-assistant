#!/usr/bin/env pwsh
# Test script to verify dashboard integration with live marketplace data

Write-Host "=== Dashboard Live Data Integration Test ===" -ForegroundColor Green
Write-Host ""

# Check if server is running
$serverRunning = netstat -an | Select-String ":5000.*LISTENING"
if ($serverRunning) {
    Write-Host "✓ Server is running on port 5000" -ForegroundColor Green
} else {
    Write-Host "✗ Server is not running on port 5000" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Testing dashboard stats endpoint..." -ForegroundColor Yellow

# Test the dashboard endpoint
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/dashboard/stats" -Method GET -TimeoutSec 30
    
    Write-Host "✓ Dashboard endpoint responded successfully" -ForegroundColor Green
    Write-Host ""
    
    # Display key metrics
    Write-Host "Dashboard Statistics:" -ForegroundColor Cyan
    Write-Host "- Total Products: $($response.totalProducts)"
    Write-Host "- Local Products: $($response.localProductsCount)"
    Write-Host "- Live Products: $($response.liveProductsCount)"
    Write-Host "- Top Marketplace: $($response.topMarketplace)"
    Write-Host "- Top Categories: $($response.topCategories -join ', ')"
    
    # Check if live data is included
    if ($response.liveProductsCount -gt 0) {
        Write-Host ""
        Write-Host "✓ Live marketplace data is successfully integrated!" -ForegroundColor Green
        Write-Host "✓ Dashboard shows $($response.liveProductsCount) live products from external marketplaces" -ForegroundColor Green
        
        if ($response.marketplaceCounts) {
            Write-Host ""
            Write-Host "Marketplace Distribution:" -ForegroundColor Cyan
            $response.marketplaceCounts.PSObject.Properties | ForEach-Object {
                Write-Host "- $($_.Name): $($_.Value) products"
            }
        }
    } else {
        Write-Host "⚠ No live marketplace data found in dashboard" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ Failed to fetch dashboard stats: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Green
