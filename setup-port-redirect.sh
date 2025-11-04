#!/bin/bash
###############################################################################
# Setup iptables redirect: port 80 → 8080
# This allows Cloudflare to access the API without needing sudo for each run
###############################################################################

if [[ $EUID -ne 0 ]]; then
   echo "❌ This script must be run as root (use sudo)"
   exit 1
fi

echo "Setting up iptables redirect: 80 → 8080"

# Enable IP forwarding
sysctl -w net.ipv4.ip_forward=1

# Add iptables rules
iptables -t nat -A PREROUTING -p tcp --dport 80 -j REDIRECT --to-port 8080
iptables -t nat -A OUTPUT -p tcp --dport 80 -j REDIRECT --to-port 8080

echo "✅ Redirect configured!"
echo ""
echo "Port 80 now redirects to 8080"
echo "To make permanent, add to /etc/sysconfig/iptables"
echo ""
echo "To remove:"
echo "  iptables -t nat -D PREROUTING -p tcp --dport 80 -j REDIRECT --to-port 8080"
echo "  iptables -t nat -D OUTPUT -p tcp --dport 80 -j REDIRECT --to-port 8080"
