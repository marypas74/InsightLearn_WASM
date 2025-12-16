#!/bin/bash
#
# Script: manage-subtitle-job.sh
# Purpose: Deploy and manage the Kubernetes subtitle generation job
# Usage: ./manage-subtitle-job.sh [deploy|run|status|logs|delete|cronjob-enable|cronjob-disable]
#

set -e

NAMESPACE="insightlearn"
JOB_NAME="subtitle-generation-job"
CRONJOB_NAME="subtitle-generation-cronjob"
CONFIGMAP_NAME="subtitle-generation-script"
MANIFEST_FILE="$(dirname "$0")/../k8s/19-subtitle-generation-job.yaml"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

log() { echo -e "${CYAN}[$(date '+%H:%M:%S')]${NC} $1"; }
log_success() { echo -e "${GREEN}✓${NC} $1"; }
log_error() { echo -e "${RED}✗${NC} $1"; }
log_info() { echo -e "${BLUE}ℹ${NC} $1"; }

usage() {
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  deploy          Deploy ConfigMap and Job/CronJob manifests"
    echo "  run             Run the job immediately (creates new job instance)"
    echo "  status          Show status of job and cronjob"
    echo "  logs            Show logs from the latest job pod"
    echo "  delete          Delete all subtitle generation resources"
    echo "  cronjob-enable  Enable the daily CronJob (2:00 AM)"
    echo "  cronjob-disable Disable the CronJob (suspend)"
    echo ""
    exit 1
}

deploy() {
    log "Deploying Subtitle Generation Job..."

    # Check manifest exists
    if [ ! -f "$MANIFEST_FILE" ]; then
        log_error "Manifest file not found: $MANIFEST_FILE"
        exit 1
    fi

    # Apply manifests
    log "Applying ConfigMap and Job definitions..."
    kubectl apply -f "$MANIFEST_FILE"

    log_success "Deployment complete!"
    log_info "ConfigMap: $CONFIGMAP_NAME"
    log_info "Job: $JOB_NAME"
    log_info "CronJob: $CRONJOB_NAME (runs daily at 2:00 AM)"
}

run_job() {
    log "Starting subtitle generation job..."

    # Delete existing job if present
    if kubectl get job "$JOB_NAME" -n "$NAMESPACE" &>/dev/null; then
        log "Deleting existing job..."
        kubectl delete job "$JOB_NAME" -n "$NAMESPACE" --ignore-not-found
        sleep 2
    fi

    # Create new job from manifest (only the Job part)
    log "Creating new job..."
    kubectl apply -f "$MANIFEST_FILE"

    log_success "Job started!"
    log_info "Monitor with: $0 status"
    log_info "View logs with: $0 logs"

    # Show initial status
    sleep 3
    kubectl get job "$JOB_NAME" -n "$NAMESPACE"
}

show_status() {
    log "Subtitle Generation Status"
    echo ""

    echo -e "${CYAN}=== Job Status ===${NC}"
    kubectl get job "$JOB_NAME" -n "$NAMESPACE" 2>/dev/null || echo "Job not found"
    echo ""

    echo -e "${CYAN}=== CronJob Status ===${NC}"
    kubectl get cronjob "$CRONJOB_NAME" -n "$NAMESPACE" 2>/dev/null || echo "CronJob not found"
    echo ""

    echo -e "${CYAN}=== Related Pods ===${NC}"
    kubectl get pods -n "$NAMESPACE" -l app=subtitle-generator --sort-by=.metadata.creationTimestamp 2>/dev/null || echo "No pods found"
    echo ""

    # Show last run details
    local latest_pod
    latest_pod=$(kubectl get pods -n "$NAMESPACE" -l app=subtitle-generator --sort-by=.metadata.creationTimestamp -o jsonpath='{.items[-1].metadata.name}' 2>/dev/null)

    if [ -n "$latest_pod" ]; then
        echo -e "${CYAN}=== Latest Pod Details ===${NC}"
        kubectl describe pod "$latest_pod" -n "$NAMESPACE" | grep -E "^Status:|^Reason:|Started:|Finished:|Exit Code:" || true
    fi
}

show_logs() {
    log "Fetching logs from subtitle generator..."

    local latest_pod
    latest_pod=$(kubectl get pods -n "$NAMESPACE" -l app=subtitle-generator --sort-by=.metadata.creationTimestamp -o jsonpath='{.items[-1].metadata.name}' 2>/dev/null)

    if [ -z "$latest_pod" ]; then
        log_error "No subtitle generator pods found"
        exit 1
    fi

    log_info "Pod: $latest_pod"
    echo ""
    kubectl logs "$latest_pod" -n "$NAMESPACE" -f 2>/dev/null || kubectl logs "$latest_pod" -n "$NAMESPACE"
}

delete_all() {
    log "Deleting all subtitle generation resources..."

    kubectl delete job "$JOB_NAME" -n "$NAMESPACE" --ignore-not-found
    kubectl delete cronjob "$CRONJOB_NAME" -n "$NAMESPACE" --ignore-not-found
    kubectl delete configmap "$CONFIGMAP_NAME" -n "$NAMESPACE" --ignore-not-found

    # Clean up completed pods
    kubectl delete pods -n "$NAMESPACE" -l app=subtitle-generator --field-selector=status.phase==Succeeded --ignore-not-found
    kubectl delete pods -n "$NAMESPACE" -l app=subtitle-generator --field-selector=status.phase==Failed --ignore-not-found

    log_success "All resources deleted"
}

enable_cronjob() {
    log "Enabling CronJob..."
    kubectl patch cronjob "$CRONJOB_NAME" -n "$NAMESPACE" -p '{"spec":{"suspend":false}}'
    log_success "CronJob enabled - will run daily at 2:00 AM"
}

disable_cronjob() {
    log "Disabling CronJob..."
    kubectl patch cronjob "$CRONJOB_NAME" -n "$NAMESPACE" -p '{"spec":{"suspend":true}}'
    log_success "CronJob disabled (suspended)"
}

# Main
case "${1:-}" in
    deploy)
        deploy
        ;;
    run)
        run_job
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    delete)
        delete_all
        ;;
    cronjob-enable)
        enable_cronjob
        ;;
    cronjob-disable)
        disable_cronjob
        ;;
    *)
        usage
        ;;
esac
