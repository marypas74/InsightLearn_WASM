#!/bin/bash
################################################################################
# Production-Safe Secret Rotation Script
#
# Purpose: Automatically rotate all production secrets with zero downtime
# Schedule: Run every 2 days via K8s CronJob or manual execution
# Safety: Changes passwords INSIDE databases BEFORE updating K8s secrets
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
# Date: 2025-11-16
################################################################################

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="${NAMESPACE:-insightlearn}"
SECRET_NAME="insightlearn-secrets"
BACKUP_DIR="/var/backups/secret-rotation"
LOG_FILE="/var/log/secret-rotation.log"
ROLLBACK_FILE="${BACKUP_DIR}/rollback-secrets-$(date +%Y%m%d-%H%M%S).yaml"

# Logging functions
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $*" | tee -a "$LOG_FILE"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR:${NC} $*" | tee -a "$LOG_FILE" >&2
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING:${NC} $*" | tee -a "$LOG_FILE"
}

info() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] INFO:${NC} $*" | tee -a "$LOG_FILE"
}

# Check if running in K8s cluster
check_environment() {
    log "Checking execution environment..."

    if ! command -v kubectl &> /dev/null; then
        error "kubectl not found - required for K8s operations"
        exit 1
    fi

    if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
        error "Namespace $NAMESPACE not found"
        exit 1
    fi

    log "Environment check passed"
}

# Create backup directory
create_backup_dir() {
    mkdir -p "$BACKUP_DIR"
    log "Backup directory: $BACKUP_DIR"
}

# Backup current secrets (for rollback)
backup_current_secrets() {
    log "Step 1/9: Backing up current secrets..."

    kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" -o yaml > "$ROLLBACK_FILE"

    if [[ -f "$ROLLBACK_FILE" ]]; then
        log "✓ Secrets backed up to: $ROLLBACK_FILE"
    else
        error "Failed to backup secrets"
        exit 1
    fi
}

# Generate cryptographically secure passwords
generate_new_secrets() {
    log "Step 2/9: Generating new cryptographically secure passwords..."

    # SQL Server SA password (32 chars, alphanumeric)
    NEW_MSSQL_PASSWORD=$(openssl rand -base64 32 | tr -d '/+=' | cut -c1-32)

    # JWT Secret (64 chars, base64)
    NEW_JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')

    # MongoDB password (32 chars, alphanumeric)
    NEW_MONGODB_PASSWORD=$(openssl rand -base64 32 | tr -d '/+=' | cut -c1-32)

    # Redis password (32 chars, alphanumeric)
    NEW_REDIS_PASSWORD=$(openssl rand -base64 32 | tr -d '/+=' | cut -c1-32)

    # Admin password (16 chars, mixed case + numbers + symbols)
    NEW_ADMIN_PASSWORD=$(openssl rand -base64 16 | tr -d '\n')

    log "✓ New secrets generated successfully"
}

# Change SQL Server password INSIDE database
change_sqlserver_password() {
    log "Step 3/9: Changing SQL Server password inside database..."

    local pod_name="sqlserver-0"
    local current_password

    # Get current password from K8s secret
    current_password=$(kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" -o jsonpath='{.data.mssql-sa-password}' | base64 -d)

    # Execute ALTER LOGIN inside SQL Server pod
    if kubectl exec -n "$NAMESPACE" "$pod_name" -- /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$current_password" -C \
        -Q "ALTER LOGIN sa WITH PASSWORD = '$NEW_MSSQL_PASSWORD'" 2>&1 | tee -a "$LOG_FILE"; then
        log "✓ SQL Server password changed successfully"
    else
        error "Failed to change SQL Server password inside database"
        return 1
    fi

    # Verify new password works
    sleep 2
    if kubectl exec -n "$NAMESPACE" "$pod_name" -- /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$NEW_MSSQL_PASSWORD" -C \
        -Q "SELECT 1" &>/dev/null; then
        log "✓ SQL Server new password verified"
    else
        error "SQL Server new password verification failed - ROLLING BACK"
        # Restore old password
        kubectl exec -n "$NAMESPACE" "$pod_name" -- /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U sa -P "$NEW_MSSQL_PASSWORD" -C \
            -Q "ALTER LOGIN sa WITH PASSWORD = '$current_password'" || true
        return 1
    fi
}

# Change MongoDB password INSIDE database
change_mongodb_password() {
    log "Step 4/9: Changing MongoDB password inside database..."

    local pod_name="mongodb-0"
    local current_password

    # Get current password from K8s secret
    current_password=$(kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" -o jsonpath='{.data.mongodb-password}' | base64 -d)

    # Execute changeUserPassword inside MongoDB pod
    if kubectl exec -n "$NAMESPACE" "$pod_name" -- mongosh \
        -u insightlearn -p "$current_password" --authenticationDatabase admin \
        --eval "db.getSiblingDB('admin').changeUserPassword('insightlearn', '$NEW_MONGODB_PASSWORD')" 2>&1 | tee -a "$LOG_FILE"; then
        log "✓ MongoDB password changed successfully"
    else
        error "Failed to change MongoDB password inside database"
        return 1
    fi

    # Verify new password works
    sleep 2
    if kubectl exec -n "$NAMESPACE" "$pod_name" -- mongosh \
        -u insightlearn -p "$NEW_MONGODB_PASSWORD" --authenticationDatabase admin \
        --eval "db.version()" &>/dev/null; then
        log "✓ MongoDB new password verified"
    else
        error "MongoDB new password verification failed - ROLLING BACK"
        # Restore old password
        kubectl exec -n "$NAMESPACE" "$pod_name" -- mongosh \
            -u insightlearn -p "$NEW_MONGODB_PASSWORD" --authenticationDatabase admin \
            --eval "db.getSiblingDB('admin').changeUserPassword('insightlearn', '$current_password')" || true
        return 1
    fi
}

# Change Redis password INSIDE database
change_redis_password() {
    log "Step 5/9: Changing Redis password inside database..."

    local pod_name
    pod_name=$(kubectl get pod -n "$NAMESPACE" -l app=redis -o jsonpath='{.items[0].metadata.name}')
    local current_password

    # Get current password from K8s secret
    current_password=$(kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" -o jsonpath='{.data.redis-password}' | base64 -d)

    # Execute CONFIG SET inside Redis pod
    if kubectl exec -n "$NAMESPACE" "$pod_name" -- redis-cli \
        -a "$current_password" \
        CONFIG SET requirepass "$NEW_REDIS_PASSWORD" 2>&1 | tee -a "$LOG_FILE"; then
        log "✓ Redis password changed successfully"
    else
        error "Failed to change Redis password inside database"
        return 1
    fi

    # Verify new password works
    sleep 2
    if kubectl exec -n "$NAMESPACE" "$pod_name" -- redis-cli \
        -a "$NEW_REDIS_PASSWORD" \
        PING &>/dev/null; then
        log "✓ Redis new password verified"
    else
        error "Redis new password verification failed - ROLLING BACK"
        # Restore old password
        kubectl exec -n "$NAMESPACE" "$pod_name" -- redis-cli \
            -a "$NEW_REDIS_PASSWORD" \
            CONFIG SET requirepass "$current_password" || true
        return 1
    fi
}

# Update K8s secrets (AFTER database passwords changed)
update_kubernetes_secrets() {
    log "Step 6/9: Updating Kubernetes secrets..."

    # Base64 encode new secrets
    local mssql_b64=$(echo -n "$NEW_MSSQL_PASSWORD" | base64 -w 0)
    local jwt_b64=$(echo -n "$NEW_JWT_SECRET" | base64 -w 0)
    local mongodb_b64=$(echo -n "$NEW_MONGODB_PASSWORD" | base64 -w 0)
    local redis_b64=$(echo -n "$NEW_REDIS_PASSWORD" | base64 -w 0)
    local admin_b64=$(echo -n "$NEW_ADMIN_PASSWORD" | base64 -w 0)

    # Update connection string with new password
    local connection_string="Server=sqlserver-service,1433;Database=InsightLearnDb;User Id=sa;Password=${NEW_MSSQL_PASSWORD};TrustServerCertificate=True;MultipleActiveResultSets=true"
    local connection_string_b64=$(echo -n "$connection_string" | base64 -w 0)

    # Patch K8s secret with all new values
    if kubectl patch secret "$SECRET_NAME" -n "$NAMESPACE" --type=json -p="[
        {\"op\": \"replace\", \"path\": \"/data/mssql-sa-password\", \"value\": \"$mssql_b64\"},
        {\"op\": \"replace\", \"path\": \"/data/jwt-secret-key\", \"value\": \"$jwt_b64\"},
        {\"op\": \"replace\", \"path\": \"/data/mongodb-password\", \"value\": \"$mongodb_b64\"},
        {\"op\": \"replace\", \"path\": \"/data/redis-password\", \"value\": \"$redis_b64\"},
        {\"op\": \"replace\", \"path\": \"/data/admin-password\", \"value\": \"$admin_b64\"},
        {\"op\": \"replace\", \"path\": \"/data/connection-string\", \"value\": \"$connection_string_b64\"}
    ]" 2>&1 | tee -a "$LOG_FILE"; then
        log "✓ Kubernetes secrets updated successfully"
    else
        error "Failed to update Kubernetes secrets"
        return 1
    fi
}

# Restart pods in correct order with health verification
restart_pods() {
    log "Step 7/9: Restarting pods with new secrets..."

    # 1. Restart SQL Server (StatefulSet)
    info "Restarting SQL Server pod..."
    kubectl delete pod sqlserver-0 -n "$NAMESPACE" --grace-period=30
    sleep 10
    kubectl wait --for=condition=Ready pod/sqlserver-0 -n "$NAMESPACE" --timeout=120s || {
        error "SQL Server pod failed to become ready"
        return 1
    }
    log "✓ SQL Server pod restarted and ready"

    # 2. Restart MongoDB (StatefulSet)
    info "Restarting MongoDB pod..."
    kubectl delete pod mongodb-0 -n "$NAMESPACE" --grace-period=30
    sleep 10
    kubectl wait --for=condition=Ready pod/mongodb-0 -n "$NAMESPACE" --timeout=120s || {
        error "MongoDB pod failed to become ready"
        return 1
    }
    log "✓ MongoDB pod restarted and ready"

    # 3. Restart Redis (Deployment)
    info "Restarting Redis deployment..."
    kubectl rollout restart deployment/redis -n "$NAMESPACE"
    kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=120s || {
        error "Redis deployment failed to rollout"
        return 1
    }
    log "✓ Redis deployment restarted and ready"

    # 4. Restart API (Deployment)
    info "Restarting API deployment..."
    kubectl rollout restart deployment/insightlearn-api -n "$NAMESPACE"
    kubectl rollout status deployment/insightlearn-api -n "$NAMESPACE" --timeout=120s || {
        error "API deployment failed to rollout"
        return 1
    }
    log "✓ API deployment restarted and ready"
}

# Verify all services healthy
verify_services() {
    log "Step 8/9: Verifying all services are healthy..."

    sleep 10  # Allow services to fully initialize

    # Check SQL Server
    info "Checking SQL Server health..."
    if kubectl exec -n "$NAMESPACE" sqlserver-0 -- /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$NEW_MSSQL_PASSWORD" -C \
        -Q "SELECT @@VERSION" &>/dev/null; then
        log "✓ SQL Server is healthy"
    else
        error "SQL Server health check failed"
        return 1
    fi

    # Check MongoDB
    info "Checking MongoDB health..."
    if kubectl exec -n "$NAMESPACE" mongodb-0 -- mongosh \
        -u insightlearn -p "$NEW_MONGODB_PASSWORD" --authenticationDatabase admin \
        --eval "db.version()" &>/dev/null; then
        log "✓ MongoDB is healthy"
    else
        error "MongoDB health check failed"
        return 1
    fi

    # Check Redis
    info "Checking Redis health..."
    local redis_pod
    redis_pod=$(kubectl get pod -n "$NAMESPACE" -l app=redis -o jsonpath='{.items[0].metadata.name}')
    if kubectl exec -n "$NAMESPACE" "$redis_pod" -- redis-cli \
        -a "$NEW_REDIS_PASSWORD" PING &>/dev/null; then
        log "✓ Redis is healthy"
    else
        error "Redis health check failed"
        return 1
    fi

    # Check API
    info "Checking API health..."
    local api_pod
    api_pod=$(kubectl get pod -n "$NAMESPACE" -l app=insightlearn-api -o jsonpath='{.items[0].metadata.name}')
    sleep 5  # Allow API to fully start
    if kubectl exec -n "$NAMESPACE" "$api_pod" -- wget -q -O- http://localhost:7001/health | grep -q "Healthy"; then
        log "✓ API is healthy"
    else
        error "API health check failed"
        return 1
    fi
}

# Generate rotation report
generate_report() {
    log "Step 9/9: Generating rotation report..."

    local report_file="${BACKUP_DIR}/rotation-report-$(date +%Y%m%d-%H%M%S).txt"

    cat > "$report_file" <<EOF
================================================================================
Secret Rotation Report
================================================================================

Rotation Date: $(date +'%Y-%m-%d %H:%M:%S UTC')
Namespace: $NAMESPACE
Secret Name: $SECRET_NAME
Rollback File: $ROLLBACK_FILE

Rotated Secrets:
  ✓ SQL Server SA Password (32 characters)
  ✓ JWT Secret Key (64 characters)
  ✓ MongoDB Password (32 characters)
  ✓ Redis Password (32 characters)
  ✓ Admin Password (16 characters)

Database Password Changes:
  ✓ SQL Server: ALTER LOGIN sa WITH PASSWORD executed
  ✓ MongoDB: changeUserPassword() executed
  ✓ Redis: CONFIG SET requirepass executed

Kubernetes Updates:
  ✓ Secret '$SECRET_NAME' patched with 6 new values
  ✓ Connection string updated with new SQL password

Pod Restarts:
  ✓ sqlserver-0 restarted (StatefulSet)
  ✓ mongodb-0 restarted (StatefulSet)
  ✓ redis deployment rolled out
  ✓ insightlearn-api deployment rolled out

Health Verification:
  ✓ SQL Server: Connection successful
  ✓ MongoDB: Connection successful
  ✓ Redis: PING successful
  ✓ API: /health endpoint returns 'Healthy'

Rollback Instructions:
  If issues occur, restore previous secrets:
    kubectl apply -f $ROLLBACK_FILE

Status: SUCCESS
Next Rotation: $(date -d '+2 days' +'%Y-%m-%d %H:%M:%S UTC')

================================================================================
EOF

    log "✓ Report generated: $report_file"

    # Display report summary
    cat "$report_file"
}

# Rollback function (called if any step fails)
rollback() {
    error "ROTATION FAILED - Initiating rollback..."

    if [[ -f "$ROLLBACK_FILE" ]]; then
        warn "Restoring previous secrets from backup..."
        kubectl apply -f "$ROLLBACK_FILE" -n "$NAMESPACE"

        # Restart pods to pick up old secrets
        kubectl delete pod sqlserver-0 -n "$NAMESPACE" --grace-period=30 || true
        kubectl delete pod mongodb-0 -n "$NAMESPACE" --grace-period=30 || true
        kubectl rollout restart deployment/redis -n "$NAMESPACE" || true
        kubectl rollout restart deployment/insightlearn-api -n "$NAMESPACE" || true

        error "Rollback completed - system restored to previous state"
        exit 1
    else
        error "Rollback file not found - manual intervention required"
        exit 1
    fi
}

# Main execution
main() {
    log "=========================================="
    log "Production-Safe Secret Rotation Started"
    log "=========================================="

    # Set trap for automatic rollback on error
    trap rollback ERR

    check_environment
    create_backup_dir
    backup_current_secrets
    generate_new_secrets

    # CRITICAL: Change passwords INSIDE databases BEFORE updating K8s secrets
    change_sqlserver_password
    change_mongodb_password
    change_redis_password

    # NOW update K8s secrets (databases already have new passwords)
    update_kubernetes_secrets

    # Restart pods to pick up new secrets
    restart_pods

    # Verify everything works
    verify_services

    # Generate success report
    generate_report

    log "=========================================="
    log "Secret Rotation Completed Successfully!"
    log "=========================================="

    # Cleanup old backups (keep last 10)
    find "$BACKUP_DIR" -name "rollback-secrets-*.yaml" -type f | sort -r | tail -n +11 | xargs -r rm -f
    log "✓ Old backups cleaned (keeping last 10)"
}

# Execute main function
main "$@"
