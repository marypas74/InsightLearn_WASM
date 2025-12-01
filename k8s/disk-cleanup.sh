#!/bin/bash
# InsightLearn Disk Cleanup Script v3
# Previene disk pressure in K3s - Threshold 83%
# Log retention: 2 days
# Runs every 6 hours via systemd timer

LOG_FILE="/var/log/disk-cleanup.log"
THRESHOLD=83
LOG_RETENTION_DAYS=2

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> $LOG_FILE
}

log "=== Disk Cleanup Started ==="

# Get current usage percentage
USAGE=$(df / | awk 'NR==2 {gsub("%",""); print $5}')
log "Current disk usage: ${USAGE}%"

if [ "$USAGE" -gt "$THRESHOLD" ]; then
    log "WARNING: Disk usage ${USAGE}% exceeds threshold ${THRESHOLD}%"
    log "Starting cleanup..."

    # 1. Clean old rotated logs (older than 2 days)
    DELETED=$(find /var/log -name "*.gz" -mtime +${LOG_RETENTION_DAYS} -delete -print 2>/dev/null | wc -l)
    log "Deleted $DELETED compressed log files older than ${LOG_RETENTION_DAYS} days"

    # 2. Clean old rotated logs by date pattern (keep last 2 days)
    find /var/log -name "messages-*" -mtime +${LOG_RETENTION_DAYS} -delete 2>/dev/null
    find /var/log -name "secure-*" -mtime +${LOG_RETENTION_DAYS} -delete 2>/dev/null
    find /var/log -name "maillog-*" -mtime +${LOG_RETENTION_DAYS} -delete 2>/dev/null
    log "Cleaned dated rotated logs older than ${LOG_RETENTION_DAYS} days"

    # 3. Clean PCP (Performance Co-Pilot) logs older than 2 days
    find /var/log/pcp -type f -mtime +${LOG_RETENTION_DAYS} -delete 2>/dev/null
    log "Cleaned PCP logs older than ${LOG_RETENTION_DAYS} days"

    # 4. Truncate large active logs (keep last 10MB)
    for logfile in /var/log/messages /var/log/secure /var/log/insightlearn-watchdog.log; do
        if [ -f "$logfile" ]; then
            SIZE=$(stat -c%s "$logfile" 2>/dev/null || echo 0)
            if [ "$SIZE" -gt 10485760 ]; then
                tail -c 10485760 "$logfile" > "$logfile.tmp" && mv "$logfile.tmp" "$logfile"
                log "Truncated $logfile to 10MB"
            fi
        fi
    done

    # 5. Clean systemd journal (keep 2 days)
    journalctl --vacuum-time=${LOG_RETENTION_DAYS}d 2>/dev/null
    log "Vacuumed systemd journal to ${LOG_RETENTION_DAYS} days"

    # 6. Clean /tmp files older than 2 days
    DELETED=$(find /tmp -type f -mtime +${LOG_RETENTION_DAYS} -delete -print 2>/dev/null | wc -l)
    log "Deleted $DELETED old /tmp files"

    # 7. Clean tar/gz files in /tmp
    rm -f /tmp/*.tar /tmp/*.tar.gz /tmp/*.tar.xz /tmp/*.tgz 2>/dev/null
    log "Cleaned tar archives in /tmp"

    # 8. Clean DNF cache
    dnf clean all 2>/dev/null
    log "Cleaned DNF cache"

    # 9. Clean old coredumps
    rm -rf /var/lib/systemd/coredump/* 2>/dev/null
    log "Cleaned coredumps"

    # Check new usage
    NEW_USAGE=$(df / | awk 'NR==2 {gsub("%",""); print $5}')
    FREED=$((USAGE - NEW_USAGE))
    log "Cleanup complete. New usage: ${NEW_USAGE}% (freed ${FREED}%)"
else
    log "Disk usage ${USAGE}% is below threshold ${THRESHOLD}%. No cleanup needed."
fi

log "=== Disk Cleanup Finished ==="
