# Test script to verify marketplace integration
Write-Host "Testing Resell Assistant Marketplace Integration" -ForegroundColor Green

# Test 1: Local products endpoint
Write-Host "`n1. Testing local products endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://localhost:5001/api/products" -SkipCertificateCheck
    Write-Host "✓ Local products: $($response.Count) products found" -ForegroundColor Green
    Write-Host "Sample product: $($response[0].title)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to fetch local products: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Combined marketplace search
Write-Host "`n2. Testing combined marketplace search..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://localhost:5001/api/products/search?query=iPhone`&limit=10" -SkipCertificateCheck
    Write-Host "✓ Combined search: $($response.Count) results found" -ForegroundColor Green
    
    # Analyze result sources
    $localCount = ($response | Where-Object { -not $_.isExternalListing }).Count
    $ebayCount = ($response | Where-Object { $_.marketplace -eq "eBay" -and $_.isExternalListing }).Count
    $facebookCount = ($response | Where-Object { $_.marketplace -eq "Facebook Marketplace" -and $_.isExternalListing }).Count
    
    Write-Host "  - Local results: $localCount" -ForegroundColor Cyan
    Write-Host "  - eBay results: $ebayCount" -ForegroundColor Cyan
    Write-Host "  - Facebook results: $facebookCount" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to perform combined search: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Top deals endpoint
Write-Host "`n3. Testing top deals endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://localhost:5001/api/products/top-deals" -SkipCertificateCheck
    Write-Host "✓ Top deals: $($response.Count) deals found" -ForegroundColor Green
    if ($response.Count -gt 0) {
        $topDeal = $response[0]
        Write-Host "Best deal: $($topDeal.product.title) - Profit: `$$($topDeal.potentialProfit)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ Failed to fetch top deals: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nIntegration test completed!" -ForegroundColor Green
