#!/bin/bash

# Script to save eBay production credentials
# This will prompt for credentials and save them securely

echo "=== eBay Production Credentials Setup ==="
echo ""
echo "Please enter your eBay production credentials:"
echo ""

# Prompt for Client ID
read -p "eBay Client ID (40 characters): " CLIENT_ID

# Validate Client ID length
if [ ${#CLIENT_ID} -ne 40 ]; then
    echo "Error: Client ID should be exactly 40 characters long"
    exit 1
fi

# Prompt for Client Secret (hidden input)
echo -n "eBay Client Secret (36 characters): "
read -s CLIENT_SECRET
echo ""

# Validate Client Secret length
if [ ${#CLIENT_SECRET} -ne 36 ]; then
    echo "Error: Client Secret should be exactly 36 characters long"
    exit 1
fi

echo ""
echo "Saving credentials..."

# Create JSON payload
JSON_PAYLOAD=$(cat <<EOF
{
    "service": "eBay",
    "clientId": "$CLIENT_ID",
    "clientSecret": "$CLIENT_SECRET",
    "environment": "production"
}
EOF
)

# Save credentials via API
RESPONSE=$(curl -s -X POST "https://localhost:5001/api/settings/credentials/ebay" \
    -k \
    -H "Content-Type: application/json" \
    -d "$JSON_PAYLOAD" \
    --max-time 15)

echo "Response: $RESPONSE"

# Test the connection
echo ""
echo "Testing eBay API connection..."
TEST_RESPONSE=$(curl -s -X POST "https://localhost:5001/api/settings/credentials/ebay/test" \
    -k \
    -H "Content-Type: application/json" \
    --max-time 15)

echo "Test Result: $TEST_RESPONSE"

echo ""
if echo "$TEST_RESPONSE" | grep -q '"isConnected":true'; then
    echo "✅ SUCCESS: eBay production credentials are working!"
else
    echo "❌ FAILED: There may be an issue with the credentials or connection"
    echo "Please check that:"
    echo "1. The credentials are valid production credentials from your eBay Developer Account"
    echo "2. Your eBay app is approved for production"
    echo "3. The credentials have the necessary permissions (Browse API)"
fi
