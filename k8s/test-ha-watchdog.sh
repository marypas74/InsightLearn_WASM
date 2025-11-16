#!/bin/bash
################################################################################
# Test InsightLearn HA Watchdog
#
# This script tests the HA watchdog without waiting for the timer
#
# Usage: sudo ./test-ha-watchdog.sh
################################################################################

set -euo pipefail

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "ERROR: This script must be run as root"
   echo "Usage: sudo $0"
   exit 1
fi

echo "=========================================="
echo "HA Watchdog Test"
echo "=========================================="
echo ""

# Check if watchdog is installed
if [[ ! -f /usr/local/bin/insightlearn-ha-watchdog.sh ]]; then
    echo "ERROR: Watchdog not installed"
    echo "Run: sudo ./install-ha-watchdog.sh"
    exit 1
fi

echo "Running watchdog manually..."
echo ""

/usr/local/bin/insightlearn-ha-watchdog.sh

echo ""
echo "=========================================="
echo "Watchdog Test Complete"
echo "=========================================="
echo ""
echo "Check log for details:"
echo "  tail -30 /var/log/insightlearn-watchdog.log"
echo ""
