#!/bin/bash
################################################################################
# Restore Single Resource from Backup
#
# Purpose: Interactive script to restore a single Kubernetes resource from backup
#   - Lists available backups
#   - Extracts selected backup
#   - Shows available resources
#   - Restores selected resource
#
# Usage:
#   ./restore-single-resource.sh
#   ./restore-single-resource.sh --resource deployment --name insightlearn-api --namespace insightlearn
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Configuration
BACKUP_DIR="/var/backups/k3s-cluster"
TEMP_DIR="/tmp/k3s-restore-$(date +%Y%m%d-%H%M%S)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[INFO]${NC} $*"
}

error() {
    echo -e "${RED}[ERROR]${NC} $*" >&2
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $*"
}

info() {
    echo -e "${BLUE}[INFO]${NC} $*"
}

header() {
    echo ""
    echo -e "${CYAN}══════════════════════════════════════════════════════${NC}"
    echo -e "${CYAN}  $*${NC}"
    echo -e "${CYAN}══════════════════════════════════════════════════════${NC}"
    echo ""
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root (use sudo)"
   exit 1
fi

header "Restore Single Resource from Backup"

# Parse command line arguments
RESOURCE_TYPE=""
RESOURCE_NAME=""
RESOURCE_NAMESPACE=""
BACKUP_FILE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --resource)
            RESOURCE_TYPE="$2"
            shift 2
            ;;
        --name)
            RESOURCE_NAME="$2"
            shift 2
            ;;
        --namespace)
            RESOURCE_NAMESPACE="$2"
            shift 2
            ;;
        --backup)
            BACKUP_FILE="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --resource TYPE       Resource type (deployment, service, secret, etc.)"
            echo "  --name NAME          Resource name"
            echo "  --namespace NS       Namespace (optional)"
            echo "  --backup FILE        Backup file (optional, uses latest if not specified)"
            echo "  --help               Show this help"
            echo ""
            echo "Examples:"
            echo "  $0"
            echo "  $0 --resource deployment --name insightlearn-api --namespace insightlearn"
            exit 0
            ;;
        *)
            error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Step 1: Select backup file
if [[ -z "$BACKUP_FILE" ]]; then
    log "Available backups:"
    echo ""

    if [[ ! -d "$BACKUP_DIR" ]] || [[ -z "$(ls -A "$BACKUP_DIR"/*.tar.gz 2>/dev/null)" ]]; then
        error "No backups found in $BACKUP_DIR"
        exit 1
    fi

    BACKUPS=($(ls -t "$BACKUP_DIR"/*.tar.gz))

    for i in "${!BACKUPS[@]}"; do
        BACKUP="${BACKUPS[$i]}"
        SIZE=$(du -h "$BACKUP" | cut -f1)
        DATE=$(stat -c %y "$BACKUP" | cut -d'.' -f1)
        echo -e "  ${GREEN}[$((i+1))]${NC} $(basename "$BACKUP") - $SIZE - $DATE"
    done

    echo ""
    read -p "Select backup number [1]: " BACKUP_NUM
    BACKUP_NUM=${BACKUP_NUM:-1}

    if [[ $BACKUP_NUM -lt 1 ]] || [[ $BACKUP_NUM -gt ${#BACKUPS[@]} ]]; then
        error "Invalid selection"
        exit 1
    fi

    BACKUP_FILE="${BACKUPS[$((BACKUP_NUM-1))]}"
fi

log "Using backup: $(basename "$BACKUP_FILE")"

# Step 2: Extract backup
log "Extracting backup to $TEMP_DIR..."
mkdir -p "$TEMP_DIR"

if ! tar -xzf "$BACKUP_FILE" -C "$TEMP_DIR" 2>/dev/null; then
    error "Failed to extract backup"
    exit 1
fi

BACKUP_EXTRACTED=$(find "$TEMP_DIR" -mindepth 1 -maxdepth 1 -type d | head -1)
RESOURCES_DIR="$BACKUP_EXTRACTED/resources"

if [[ ! -d "$RESOURCES_DIR" ]]; then
    error "Resources directory not found in backup"
    rm -rf "$TEMP_DIR"
    exit 1
fi

log "Backup extracted successfully"

# Step 3: Select resource type
if [[ -z "$RESOURCE_TYPE" ]]; then
    log "Available resource types:"
    echo ""

    RESOURCE_FILES=($(ls "$RESOURCES_DIR"/*.yaml 2>/dev/null))

    if [[ ${#RESOURCE_FILES[@]} -eq 0 ]]; then
        error "No resource files found in backup"
        rm -rf "$TEMP_DIR"
        exit 1
    fi

    for i in "${!RESOURCE_FILES[@]}"; do
        FILE="${RESOURCE_FILES[$i]}"
        BASENAME=$(basename "$FILE" .yaml)
        COUNT=$(grep -c "^kind: " "$FILE" 2>/dev/null || echo "0")
        echo -e "  ${GREEN}[$((i+1))]${NC} $BASENAME ($COUNT resources)"
    done

    echo ""
    read -p "Select resource type number: " RESOURCE_TYPE_NUM

    if [[ $RESOURCE_TYPE_NUM -lt 1 ]] || [[ $RESOURCE_TYPE_NUM -gt ${#RESOURCE_FILES[@]} ]]; then
        error "Invalid selection"
        rm -rf "$TEMP_DIR"
        exit 1
    fi

    RESOURCE_FILE="${RESOURCE_FILES[$((RESOURCE_TYPE_NUM-1))]}"
    RESOURCE_TYPE=$(basename "$RESOURCE_FILE" .yaml)
else
    RESOURCE_FILE="$RESOURCES_DIR/${RESOURCE_TYPE}.yaml"

    if [[ ! -f "$RESOURCE_FILE" ]]; then
        error "Resource type '$RESOURCE_TYPE' not found in backup"
        rm -rf "$TEMP_DIR"
        exit 1
    fi
fi

log "Selected resource type: $RESOURCE_TYPE"

# Step 4: Select specific resource
if [[ -z "$RESOURCE_NAME" ]]; then
    log "Available resources in backup:"
    echo ""

    # Extract resource names from YAML
    RESOURCE_NAMES=($(kubectl get -f "$RESOURCE_FILE" --all-namespaces -o custom-columns=NAME:.metadata.name --no-headers 2>/dev/null | sort -u))

    if [[ ${#RESOURCE_NAMES[@]} -eq 0 ]]; then
        error "No resources found in $RESOURCE_TYPE file"
        rm -rf "$TEMP_DIR"
        exit 1
    fi

    for i in "${!RESOURCE_NAMES[@]}"; do
        NAME="${RESOURCE_NAMES[$i]}"
        echo -e "  ${GREEN}[$((i+1))]${NC} $NAME"
    done

    echo ""
    read -p "Select resource number: " RESOURCE_NAME_NUM

    if [[ $RESOURCE_NAME_NUM -lt 1 ]] || [[ $RESOURCE_NAME_NUM -gt ${#RESOURCE_NAMES[@]} ]]; then
        error "Invalid selection"
        rm -rf "$TEMP_DIR"
        exit 1
    fi

    RESOURCE_NAME="${RESOURCE_NAMES[$((RESOURCE_NAME_NUM-1))]}"
fi

log "Selected resource: $RESOURCE_NAME"

# Step 5: Select namespace (if needed)
if [[ -z "$RESOURCE_NAMESPACE" ]]; then
    # Try to get namespace from resource
    RESOURCE_NAMESPACE=$(kubectl get -f "$RESOURCE_FILE" --all-namespaces -o custom-columns=NS:.metadata.namespace,NAME:.metadata.name --no-headers 2>/dev/null | grep "$RESOURCE_NAME" | awk '{print $1}' | head -1)

    if [[ -z "$RESOURCE_NAMESPACE" ]] || [[ "$RESOURCE_NAMESPACE" == "<none>" ]]; then
        RESOURCE_NAMESPACE="default"
    fi
fi

log "Target namespace: $RESOURCE_NAMESPACE"

# Step 6: Extract and show resource YAML
log "Extracting resource definition..."

RESOURCE_YAML=$(kubectl get "$RESOURCE_TYPE" "$RESOURCE_NAME" -n "$RESOURCE_NAMESPACE" -f "$RESOURCE_FILE" -o yaml 2>/dev/null)

if [[ -z "$RESOURCE_YAML" ]]; then
    error "Failed to extract resource from backup"
    rm -rf "$TEMP_DIR"
    exit 1
fi

info "Resource definition:"
echo ""
echo "$RESOURCE_YAML" | head -30
echo ""
echo -e "${CYAN}[... output truncated, full YAML will be applied ...]${NC}"
echo ""

# Step 7: Confirm restore
warn "This will restore the resource from backup."
warn "Any existing resource with the same name will be OVERWRITTEN."
echo ""
read -p "Continue? (yes/no) [no]: " CONFIRM
CONFIRM=${CONFIRM:-no}

if [[ "$CONFIRM" != "yes" ]]; then
    warn "Restore cancelled"
    rm -rf "$TEMP_DIR"
    exit 0
fi

# Step 8: Apply resource
log "Restoring resource..."

if echo "$RESOURCE_YAML" | kubectl apply -f - 2>&1 | tee /tmp/restore-output.log; then
    log "✓ Resource restored successfully!"

    # Wait for resource to be ready
    if [[ "$RESOURCE_TYPE" == "deployments" ]] || [[ "$RESOURCE_TYPE" == "deployment" ]]; then
        log "Waiting for deployment to be ready..."
        if kubectl rollout status deployment/"$RESOURCE_NAME" -n "$RESOURCE_NAMESPACE" --timeout=60s 2>/dev/null; then
            log "✓ Deployment is ready"
        else
            warn "Deployment rollout timeout (still rolling out...)"
        fi
    fi

    # Show current status
    echo ""
    info "Current status:"
    kubectl get "$RESOURCE_TYPE" "$RESOURCE_NAME" -n "$RESOURCE_NAMESPACE" 2>/dev/null || true

else
    error "Failed to restore resource"
    cat /tmp/restore-output.log
    rm -rf "$TEMP_DIR"
    exit 1
fi

# Cleanup
rm -rf "$TEMP_DIR"
rm -f /tmp/restore-output.log

echo ""
log "═════════════════════════════════════════════════════"
log "  Restore completed successfully!"
log "═════════════════════════════════════════════════════"
echo ""

info "Next steps:"
echo "  • Verify resource: kubectl get $RESOURCE_TYPE $RESOURCE_NAME -n $RESOURCE_NAMESPACE"
echo "  • Check logs: kubectl logs -n $RESOURCE_NAMESPACE $RESOURCE_TYPE/$RESOURCE_NAME"
echo "  • Monitor events: kubectl describe $RESOURCE_TYPE $RESOURCE_NAME -n $RESOURCE_NAMESPACE"

exit 0
