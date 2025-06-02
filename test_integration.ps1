# Test script to verify marketplace integration
Write-Host "Testing Resell Assistant Marketplace Integration" -ForegroundColor Green

# Test 1: Local products endpoint
Write-Host ""
Write-Host "1. Testing local products endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://localhost:5001/api/products" -SkipCertificateCheck
    Write-Host "Success: Local products found: $($response.Count)" -ForegroundColor Green
    Write-Host "Sample product: $($response[0].title)" -ForegroundColor Cyan
} catch {
    Write-Host "Failed to fetch local products: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Combined marketplace search
Write-Host ""
Write-Host "2. Testing combined marketplace search..." -ForegroundColor Yellow
try {
    $searchUri = "https://localhost:5001/api/products/search?query=iPhone"
    $response = Invoke-RestMethod -Uri $searchUri -SkipCertificateCheck
    Write-Host "Success: Combined search results: $($response.Count)" -ForegroundColor Green
    
    $localCount = ($response | Where-Object { -not $_.isExternalListing }).Count
    $externalCount = ($response | Where-Object { $_.isExternalListing }).Count
    
    Write-Host "  - Local results: $localCount" -ForegroundColor Cyan
    Write-Host "  - External results: $externalCount" -ForegroundColor Cyan
} catch {
    Write-Host "Failed to perform combined search: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Top deals endpoint
Write-Host ""
Write-Host "3. Testing top deals endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://localhost:5001/api/products/top-deals" -SkipCertificateCheck
    Write-Host "Success: Top deals found: $($response.Count)" -ForegroundColor Green
    if ($response.Count -gt 0) {
        $topDeal = $response[0]
        Write-Host "Best deal: $($topDeal.product.title) - Profit: $($topDeal.potentialProfit)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "Failed to fetch top deals: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Integration test completed!" -ForegroundColor Green
