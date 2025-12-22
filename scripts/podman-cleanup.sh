#!/bin/bash
# ============================================================================
# podman-cleanup.sh - Automatic Podman/Buildah cleanup script
# ============================================================================
# Purpose: Prevents disk space exhaustion by cleaning unused container images
# Schedule: Runs hourly via systemd timer
# Log: /var/log/podman-cleanup.log
# ============================================================================

LOG_FILE="/var/log/podman-cleanup.log"
DATE=$(date '+%Y-%m-%d %H:%M:%S')

log() {
    echo "[$DATE] $1" | tee -a "$LOG_FILE"
}

log "=========================================="
log "Starting Podman cleanup..."

# Check disk usage before cleanup
BEFORE_USAGE=$(df /home --output=pcent | tail -1 | tr -d ' %')
log "Disk usage before: ${BEFORE_USAGE}%"

# 1. Remove buildah working containers (often left behind after builds)
BUILDAH_CONTAINERS=$(buildah containers -q 2>/dev/null | wc -l)
if [ "$BUILDAH_CONTAINERS" -gt 0 ]; then
    log "Removing $BUILDAH_CONTAINERS buildah containers..."
    buildah rm --all 2>/dev/null
    log "Buildah containers removed"
else
    log "No buildah containers to remove"
fi

# 2. Remove dangling images (untagged <none>:<none>)
DANGLING=$(podman images -f "dangling=true" -q 2>/dev/null | wc -l)
if [ "$DANGLING" -gt 0 ]; then
    log "Removing $DANGLING dangling images..."
    podman image prune -f 2>/dev/null
    log "Dangling images removed"
else
    log "No dangling images to remove"
fi

# 3. Remove stopped containers older than 24 hours
STOPPED=$(podman ps -a -f "status=exited" -q 2>/dev/null | wc -l)
if [ "$STOPPED" -gt 0 ]; then
    log "Removing $STOPPED stopped containers..."
    podman container prune -f 2>/dev/null
    log "Stopped containers removed"
else
    log "No stopped containers to remove"
fi

# 4. Optional: Remove unused volumes
# Uncomment if you want aggressive cleanup
# podman volume prune -f 2>/dev/null

# Check disk usage after cleanup
AFTER_USAGE=$(df /home --output=pcent | tail -1 | tr -d ' %')
SAVED=$((BEFORE_USAGE - AFTER_USAGE))

log "Disk usage after: ${AFTER_USAGE}%"
if [ "$SAVED" -gt 0 ]; then
    log "Space saved: ~${SAVED}% of /home"
else
    log "No significant space change"
fi

# Report current status
log "Current Podman status:"
podman system df 2>/dev/null | while read line; do
    log "  $line"
done

log "Cleanup completed successfully"
log "=========================================="
