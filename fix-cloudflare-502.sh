#!/bin/bash
# Fix script for Cloudflare 502 Bad Gateway errors
# Implements http2Origin:false configuration for ASP.NET Core Kestrel compatibility

echo "=== Fix Cloudflare 502 - Login Issue ==="
echo ""
echo "1. Stopping all cloudflared processes..."
pkill -9 cloudflared
sleep 3

echo "2. Verifying cloudflared config has http2Origin:false..."
if grep -q "http2Origin: false" /home/mpasqui/.cloudflared/config.yml; then
    echo "✅ Config already has http2Origin:false"
else
    echo "❌ Config missing http2Origin:false - manual edit required"
    exit 1
fi

echo "3. Restarting cloudflared systemd service..."
echo "SS1-Temp1234" | sudo -S systemctl restart cloudflared-tunnel.service
sleep 8

echo "4. Checking service status..."
echo "SS1-Temp1234" | sudo -S systemctl status cloudflared-tunnel.service --no-pager | head -15

echo ""
echo "5. Testing API endpoints..."
echo "Test /api/info:"
curl -s https://www.insightlearn.cloud/api/info | jq -r '.name, .status' || echo "FAIL"

echo ""
echo "Test /api/auth/login:"
curl -s -X POST https://www.insightlearn.cloud/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"test"}' | jq .

echo ""
echo "✅ Fix completed! Try login at https://www.insightlearn.cloud"
