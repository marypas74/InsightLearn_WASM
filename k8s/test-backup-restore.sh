#!/bin/bash
################################################################################
# Backup & Restore Test Script
#
# Purpose: Comprehensive test of backup/restore procedures
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================="
echo "InsightLearn Backup/Restore Test Suite"
echo -e "==========================================${NC}"
echo ""

# Test 1: Check backup files
echo -e "${BLUE}[TEST 1/8]${NC} Checking backup files..."
BACKUP_DIR="/var/backups/k3s-cluster"

if [[ ! -d "$BACKUP_DIR" ]]; then
    echo -e "${RED}  ✗ FAIL: Backup directory not found${NC}"
    exit 1
fi

BACKUP_COUNT=$(find "$BACKUP_DIR" -name "*.tar.gz" -type f ! -name ".*" | wc -l)
echo -e "${GREEN}  ✓ PASS: Found $BACKUP_COUNT backup files${NC}"

ls -lh "$BACKUP_DIR"/*.tar.gz 2>/dev/null | while read line; do
    echo "    $line"
done

echo ""

# Test 2: Check latest backup symlink
echo -e "${BLUE}[TEST 2/8]${NC} Checking latest backup symlink..."

if [[ -L "$BACKUP_DIR/latest-backup.tar.gz" ]]; then
    LATEST_TARGET=$(readlink "$BACKUP_DIR/latest-backup.tar.gz")
    echo -e "${GREEN}  ✓ PASS: Symlink exists -> $LATEST_TARGET${NC}"
else
    echo -e "${RED}  ✗ FAIL: Latest backup symlink missing${NC}"
fi

echo ""

# Test 3: Check snapshot symlink (required for auto-restore)
echo -e "${BLUE}[TEST 3/8]${NC} Checking snapshot symlink..."

if [[ -L "$BACKUP_DIR/k3s-cluster-snapshot.tar.gz" ]]; then
    SNAPSHOT_TARGET=$(readlink "$BACKUP_DIR/k3s-cluster-snapshot.tar.gz")
    echo -e "${GREEN}  ✓ PASS: Snapshot symlink exists -> $SNAPSHOT_TARGET${NC}"
else
    echo -e "${YELLOW}  ! WARN: Snapshot symlink missing (auto-restore won't work)${NC}"
    echo -e "${YELLOW}    Fix: sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/fix-restore-symlink.sh${NC}"
fi

echo ""

# Test 4: Verify backup contents
echo -e "${BLUE}[TEST 4/8]${NC} Verifying backup contents..."

LATEST_BACKUP="$BACKUP_DIR/latest-backup.tar.gz"
if [[ -f "$LATEST_BACKUP" ]]; then
    RESOURCE_COUNT=$(tar -tzf "$LATEST_BACKUP" 2>/dev/null | grep -c "\.yaml$" || echo "0")
    echo -e "${GREEN}  ✓ PASS: Backup contains $RESOURCE_COUNT YAML resource files${NC}"

    # Check for critical resources
    CRITICAL_RESOURCES=("namespaces" "deployments" "statefulsets" "secrets" "configmaps")
    for resource in "${CRITICAL_RESOURCES[@]}"; do
        if tar -tzf "$LATEST_BACKUP" 2>/dev/null | grep -q "resources/${resource}.yaml"; then
            echo -e "${GREEN}    ✓ $resource.yaml${NC}"
        else
            echo -e "${RED}    ✗ Missing: $resource.yaml${NC}"
        fi
    done
else
    echo -e "${RED}  ✗ FAIL: Latest backup file not found${NC}"
fi

echo ""

# Test 5: Check backup script
echo -e "${BLUE}[TEST 5/8]${NC} Checking backup script..."

BACKUP_SCRIPT="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh"
if [[ -x "$BACKUP_SCRIPT" ]]; then
    echo -e "${GREEN}  ✓ PASS: Backup script exists and is executable${NC}"
else
    echo -e "${RED}  ✗ FAIL: Backup script missing or not executable${NC}"
fi

echo ""

# Test 6: Check restore script
echo -e "${BLUE}[TEST 6/8]${NC} Checking restore script..."

RESTORE_SCRIPT="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh"
if [[ -x "$RESTORE_SCRIPT" ]]; then
    echo -e "${GREEN}  ✓ PASS: Restore script exists and is executable${NC}"
else
    echo -e "${RED}  ✗ FAIL: Restore script missing or not executable${NC}"
fi

echo ""

# Test 7: Check auto-restore service
echo -e "${BLUE}[TEST 7/8]${NC} Checking auto-restore systemd service..."

if systemctl list-unit-files | grep -q "k3s-auto-restore.service"; then
    if systemctl is-enabled k3s-auto-restore.service &>/dev/null; then
        echo -e "${GREEN}  ✓ PASS: Service is enabled${NC}"
    else
        echo -e "${YELLOW}  ! WARN: Service exists but not enabled${NC}"
    fi

    # Check condition status
    CONDITION_RESULT=$(systemctl show k3s-auto-restore.service -p ConditionResult --value)
    if [[ "$CONDITION_RESULT" == "yes" ]]; then
        echo -e "${GREEN}  ✓ PASS: Service conditions met${NC}"
    else
        echo -e "${YELLOW}  ! WARN: Service conditions not met (snapshot file missing?)${NC}"
    fi
else
    echo -e "${RED}  ✗ FAIL: Auto-restore service not found${NC}"
fi

echo ""

# Test 8: Check backup logs
echo -e "${BLUE}[TEST 8/8]${NC} Checking backup logs..."

if [[ -f "/var/log/k3s-backup.log" ]]; then
    LAST_BACKUP=$(grep "Backup completed successfully" /var/log/k3s-backup.log | tail -1)
    if [[ -n "$LAST_BACKUP" ]]; then
        echo -e "${GREEN}  ✓ PASS: Last successful backup found${NC}"
        echo "    $LAST_BACKUP"
    else
        echo -e "${YELLOW}  ! WARN: No successful backups in log${NC}"
    fi
else
    echo -e "${YELLOW}  ! WARN: Backup log file not found${NC}"
fi

echo ""
echo -e "${BLUE}=========================================="
echo "Test Summary"
echo -e "==========================================${NC}"
echo ""

# Final recommendations
echo -e "${YELLOW}Recommendations:${NC}"
echo "1. If snapshot symlink is missing, run:"
echo "   ${BLUE}sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/fix-restore-symlink.sh${NC}"
echo ""
echo "2. To test manual backup:"
echo "   ${BLUE}sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh${NC}"
echo ""
echo "3. To test restore (DRY RUN - won't actually restore):"
echo "   ${BLUE}Read the restore script first:${NC}"
echo "   ${BLUE}cat /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh${NC}"
echo ""
echo "4. Check Grafana backup dashboard:"
echo "   ${BLUE}http://localhost:3000${NC} → Dashboards → InsightLearn → Disaster Recovery & Backups"
echo ""

exit 0
