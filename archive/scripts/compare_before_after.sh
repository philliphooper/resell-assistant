#!/bin/bash
# Before/After eBay Integration Comparison

echo "============================================="
echo "eBay Integration: Before vs After Comparison"
echo "============================================="

BASE_URL="https://localhost:5001"

echo ""
echo "🔍 Testing search for 'iPhone'..."
echo ""

# Current search results
CURRENT_SEARCH=$(curl -s -k "$BASE_URL/api/products/search?query=iPhone&includeExternal=true" 2>/dev/null)
CURRENT_COUNT=$(echo "$CURRENT_SEARCH" | grep -o "\"title\"" | wc -l)

echo "📊 CURRENT RESULTS:"
echo "   Total Products: $CURRENT_COUNT"

# Count by source
LOCAL_COUNT=$(echo "$CURRENT_SEARCH" | grep -o '"isExternalListing":false' | wc -l)
FACEBOOK_COUNT=$(echo "$CURRENT_SEARCH" | grep -o '"marketplace":"Facebook Marketplace"' | wc -l)
EBAY_COUNT=$(echo "$CURRENT_SEARCH" | grep -o '"marketplace":"eBay".*"isExternalListing":true' | wc -l)

echo "   • Local Database: $LOCAL_COUNT products"
echo "   • Facebook Marketplace: $FACEBOOK_COUNT products (sample data)"
echo "   • eBay: $EBAY_COUNT products"

echo ""
echo "🎯 AFTER eBay Setup (Expected):"
echo "   Total Products: 8-12 products"
echo "   • Local Database: $LOCAL_COUNT products (same)"
echo "   • Facebook Marketplace: $FACEBOOK_COUNT products (same)"
echo "   • eBay: 5-8 products (LIVE DATA)"

echo ""
echo "💡 Sample eBay products you'll see:"
echo "   • iPhone 13 Pro 128GB - $649.99"
echo "   • iPhone 14 Unlocked - $799.00"
echo "   • iPhone 12 Mini - $549.50"
echo "   • And more live eBay listings..."

echo ""
echo "🚀 Ready to configure eBay credentials?"
echo "   Run: bash setup_ebay_credentials.sh"
