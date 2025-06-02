#!/bin/bash

# Connection Leak Fix Test Script
# Tests the fixes applied to resolve TCP connection leaks on port 5001

echo "Connection Leak Fix Test - $(date)"
echo "==========================================="

# Initial connection count
echo "Initial connection count:"
netstat -an | grep ":5001" | grep "ESTABLISHED" | wc -l

echo ""
echo "Running test sequence..."

# Test 1: Health check endpoints (previously leaked connections)
echo "Test 1: Health check endpoints (10 requests)"
for i in {1..10}; do
    curl -H "Connection: close" https://localhost:5001/api/dashboard/health -k -s > /dev/null
done
echo "Connections after health checks: $(netstat -an | grep ":5001" | grep "ESTABLISHED" | wc -l)"

# Test 2: Dashboard stats (uses internal services)
echo "Test 2: Dashboard stats (5 requests)"
for i in {1..5}; do
    curl -H "Connection: close" https://localhost:5001/api/dashboard/stats -k -s > /dev/null
done
echo "Connections after dashboard stats: $(netstat -an | grep ":5001" | grep "ESTABLISHED" | wc -l)"

# Test 3: Product search (uses marketplace services with HttpClient)
echo "Test 3: Product search (15 requests)"
for i in {1..15}; do
    curl -H "Connection: close" "https://localhost:5001/api/products/search?query=test$i" -k -s > /dev/null
    sleep 0.1
done
echo "Connections after product searches: $(netstat -an | grep ":5001" | grep "ESTABLISHED" | wc -l)"

echo ""
echo "Final connection count:"
netstat -an | grep ":5001" | grep "ESTABLISHED" | wc -l

echo ""
echo "Test completed. If connection count remains low (≤5), the fix is successful!"
