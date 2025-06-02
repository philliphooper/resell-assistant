#!/bin/bash
# eBay Credentials Setup Script

echo "======================================="
echo "eBay API Credentials Setup Assistant"
echo "======================================="
echo ""

# Check if template exists
if [ ! -f "appsettings.template.json" ]; then
    echo "[ERROR] Template file not found!"
    exit 1
fi

echo "This script will help you configure your eBay API credentials."
echo ""
echo "REQUIRED INFORMATION:"
echo "1. eBay Client ID (from developer.ebay.com)"
echo "2. eBay Client Secret (from developer.ebay.com)"
echo ""

# Get Client ID
read -p "Enter your eBay Client ID: " EBAY_CLIENT_ID
if [ -z "$EBAY_CLIENT_ID" ]; then
    echo "[ERROR] Client ID is required!"
    exit 1
fi

# Get Client Secret
read -s -p "Enter your eBay Client Secret: " EBAY_CLIENT_SECRET
echo ""
if [ -z "$EBAY_CLIENT_SECRET" ]; then
    echo "[ERROR] Client Secret is required!"
    exit 1
fi

echo ""
echo "Configuring eBay API credentials..."

# Update appsettings.json
cp appsettings.template.json appsettings.json
sed -i "s/REPLACE_WITH_YOUR_EBAY_CLIENT_ID/$EBAY_CLIENT_ID/g" appsettings.json
sed -i "s/REPLACE_WITH_YOUR_EBAY_CLIENT_SECRET/$EBAY_CLIENT_SECRET/g" appsettings.json

# Update appsettings.Development.json
cp appsettings.template.json "Resell Assistant/appsettings.Development.json"
sed -i "s/REPLACE_WITH_YOUR_EBAY_CLIENT_ID/$EBAY_CLIENT_ID/g" "Resell Assistant/appsettings.Development.json"
sed -i "s/REPLACE_WITH_YOUR_EBAY_CLIENT_SECRET/$EBAY_CLIENT_SECRET/g" "Resell Assistant/appsettings.Development.json"

echo "[OK] Configuration files updated!"
echo ""
echo "Testing configuration..."

# Test the configuration
sleep 2
echo ""
echo "Starting application test..."
echo "Please wait while we verify your eBay API connection..."

# The application should be running, test the connection
TEST_RESULT=$(curl -s -k "https://localhost:5001/api/dashboard/test-ebay-connection" 2>/dev/null)

if [ $? -eq 0 ]; then
    echo "[OK] Application is responsive"
    echo "Configuration test result:"
    echo "$TEST_RESULT" | jq '.' 2>/dev/null || echo "$TEST_RESULT"
    
    # Check if credentials are working
    if echo "$TEST_RESULT" | grep -q "Ready for API Integration"; then
        echo ""
        echo "🎉 SUCCESS! eBay API is ready for integration!"
        echo ""
        echo "Next steps:"
        echo "1. Test live eBay search: curl -k 'https://localhost:5001/api/products/search?query=iPhone&includeExternal=true'"
        echo "2. Monitor application logs for eBay API calls"
        echo "3. Check GitHub issues for completion updates"
    else
        echo ""
        echo "⚠️  Configuration applied but credentials may need verification"
        echo "Please check the eBay Developer portal for credential status"
    fi
else
    echo "[ERROR] Application not responding. Please make sure the server is running."
fi

echo ""
echo "Setup complete!"
