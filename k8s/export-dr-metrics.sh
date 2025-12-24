#!/bin/bash
################################################################################
# Disaster Recovery Metrics Exporter for Prometheus
#
# Purpose: Export disaster recovery metrics in Prometheus format
#   - Backup status and size
#   - Restore status
#   - Cloudflare tunnel status
#   - System health metrics
#
# Output: Prometheus text format metrics
# Usage: Run manually or via cron to update metrics file
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Metrics output (stdout for direct scraping or file for node_exporter textfile collector)
METRICS_FILE="${METRICS_FILE:-/var/lib/node_exporter/textfile_collector/disaster_recovery.prom}"
TEMP_METRICS="/tmp/disaster_recovery_metrics.$$.prom"

# Ensure directory exists
mkdir -p "$(dirname "$METRICS_FILE")" 2>/dev/null || true

# Start writing metrics
cat > "$TEMP_METRICS" <<EOF
# HELP insightlearn_dr_backup_last_success_timestamp_seconds Unix timestamp of last successful backup
# TYPE insightlearn_dr_backup_last_success_timestamp_seconds gauge
EOF

# Get last backup timestamp (use latest-backup.tar.gz symlink to get most recent)
# Use -L flag to dereference symlinks and get actual file stats (not symlink size)
if [[ -f /var/backups/k3s-cluster/latest-backup.tar.gz ]]; then
    BACKUP_TIMESTAMP=$(stat -L -c %Y /var/backups/k3s-cluster/latest-backup.tar.gz 2>/dev/null || echo "0")
    BACKUP_SIZE=$(stat -L -c %s /var/backups/k3s-cluster/latest-backup.tar.gz 2>/dev/null || echo "0")
    echo "insightlearn_dr_backup_last_success_timestamp_seconds $BACKUP_TIMESTAMP" >> "$TEMP_METRICS"
else
    echo "insightlearn_dr_backup_last_success_timestamp_seconds 0" >> "$TEMP_METRICS"
    BACKUP_SIZE=0
fi

# Backup size metric
cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_backup_size_bytes Size of latest backup in bytes
# TYPE insightlearn_dr_backup_size_bytes gauge
insightlearn_dr_backup_size_bytes $BACKUP_SIZE
EOF

# Backup success metric (1 = success, 0 = failure)
BACKUP_SUCCESS=1
if [[ -f /var/log/k3s-backup.log ]]; then
    # Check if last backup was successful (looking for "Backup completed successfully")
    if tail -100 /var/log/k3s-backup.log | grep -q "Backup completed successfully"; then
        BACKUP_SUCCESS=1
    else
        BACKUP_SUCCESS=0
    fi
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_backup_last_status Last backup status (1=success, 0=failure)
# TYPE insightlearn_dr_backup_last_status gauge
insightlearn_dr_backup_last_status $BACKUP_SUCCESS
EOF

# Auto-restore service status
RESTORE_SERVICE_ENABLED=0
RESTORE_SERVICE_ACTIVE=0

if systemctl is-enabled k3s-auto-restore.service &>/dev/null; then
    RESTORE_SERVICE_ENABLED=1
fi

if systemctl is-active k3s-auto-restore.service &>/dev/null; then
    RESTORE_SERVICE_ACTIVE=1
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_restore_service_enabled Auto-restore service enabled (1=yes, 0=no)
# TYPE insightlearn_dr_restore_service_enabled gauge
insightlearn_dr_restore_service_enabled $RESTORE_SERVICE_ENABLED

# HELP insightlearn_dr_restore_service_active Auto-restore service active (1=yes, 0=no)
# TYPE insightlearn_dr_restore_service_active gauge
insightlearn_dr_restore_service_active $RESTORE_SERVICE_ACTIVE
EOF

# Last restore timestamp
if [[ -f /var/lib/k3s-restore-state ]]; then
    RESTORE_DATE=$(cat /var/lib/k3s-restore-state 2>/dev/null || echo "19700101")
    # Convert YYYYMMDD to Unix timestamp (approximate, midnight UTC)
    RESTORE_YEAR=${RESTORE_DATE:0:4}
    RESTORE_MONTH=${RESTORE_DATE:4:2}
    RESTORE_DAY=${RESTORE_DATE:6:2}
    RESTORE_TIMESTAMP=$(date -d "${RESTORE_YEAR}-${RESTORE_MONTH}-${RESTORE_DAY}" +%s 2>/dev/null || echo "0")
else
    RESTORE_TIMESTAMP=0
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_last_restore_timestamp_seconds Unix timestamp of last restore
# TYPE insightlearn_dr_last_restore_timestamp_seconds gauge
insightlearn_dr_last_restore_timestamp_seconds $RESTORE_TIMESTAMP
EOF

# Cloudflare tunnel status
CLOUDFLARE_SERVICE_ENABLED=0
CLOUDFLARE_SERVICE_ACTIVE=0
CLOUDFLARE_PROCESS_RUNNING=0

if systemctl is-enabled cloudflared-tunnel.service &>/dev/null 2>&1; then
    CLOUDFLARE_SERVICE_ENABLED=1
fi

if systemctl is-active cloudflared-tunnel.service &>/dev/null 2>&1; then
    CLOUDFLARE_SERVICE_ACTIVE=1
fi

if pgrep -f "cloudflared tunnel" >/dev/null 2>&1; then
    CLOUDFLARE_PROCESS_RUNNING=1
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_cloudflare_service_enabled Cloudflare tunnel service enabled (1=yes, 0=no)
# TYPE insightlearn_dr_cloudflare_service_enabled gauge
insightlearn_dr_cloudflare_service_enabled $CLOUDFLARE_SERVICE_ENABLED

# HELP insightlearn_dr_cloudflare_service_active Cloudflare tunnel service active (1=yes, 0=no)
# TYPE insightlearn_dr_cloudflare_service_active gauge
insightlearn_dr_cloudflare_service_active $CLOUDFLARE_SERVICE_ACTIVE

# HELP insightlearn_dr_cloudflare_process_running Cloudflare process running (1=yes, 0=no)
# TYPE insightlearn_dr_cloudflare_process_running gauge
insightlearn_dr_cloudflare_process_running $CLOUDFLARE_PROCESS_RUNNING
EOF

# External access check (optional, can be slow)
EXTERNAL_ACCESS=0
if command -v curl &>/dev/null; then
    if curl -s -m 5 https://www.insightlearn.cloud/health >/dev/null 2>&1; then
        EXTERNAL_ACCESS=1
    fi
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_external_access External access check (1=OK, 0=unreachable)
# TYPE insightlearn_dr_external_access gauge
insightlearn_dr_external_access $EXTERNAL_ACCESS
EOF

# Cron job status
CRON_JOB_CONFIGURED=0
if [[ -f /etc/cron.d/k3s-cluster-backup ]]; then
    CRON_JOB_CONFIGURED=1
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_cron_job_configured Backup cron job configured (1=yes, 0=no)
# TYPE insightlearn_dr_cron_job_configured gauge
insightlearn_dr_cron_job_configured $CRON_JOB_CONFIGURED
EOF

# K3s cluster health (pod count) - REMOVED
# This section was causing duplicate metrics with the insightlearn namespace metrics below
# The all-namespaces count (21/23) conflicted with namespace-specific count (14/14)
# Keeping only the insightlearn namespace metrics at the end of the file

# Backup age (seconds since last backup)
CURRENT_TIME=$(date +%s)
BACKUP_AGE=$((CURRENT_TIME - BACKUP_TIMESTAMP))

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_backup_age_seconds Age of latest backup in seconds
# TYPE insightlearn_dr_backup_age_seconds gauge
insightlearn_dr_backup_age_seconds $BACKUP_AGE
EOF

# Next backup scheduled time (calculated from cron schedule "5 * * * *")
# Use 10# prefix to force base-10 interpretation (avoids octal conversion errors for 08, 09)
CURRENT_HOUR=$((10#$(date +%H)))
CURRENT_MINUTE=$((10#$(date +%M)))

if [[ $CURRENT_MINUTE -lt 5 ]]; then
    # Next backup is at :05 of current hour
    NEXT_BACKUP_HOUR=$CURRENT_HOUR
else
    # Next backup is at :05 of next hour
    NEXT_BACKUP_HOUR=$(( (CURRENT_HOUR + 1) % 24 ))
fi

NEXT_BACKUP_TIME=$(date -d "today $NEXT_BACKUP_HOUR:05:00" +%s 2>/dev/null || echo "0")
if [[ $NEXT_BACKUP_TIME -lt $CURRENT_TIME ]]; then
    # If calculated time is in the past, add 24 hours
    NEXT_BACKUP_TIME=$((NEXT_BACKUP_TIME + 86400))
fi

SECONDS_TO_NEXT_BACKUP=$((NEXT_BACKUP_TIME - CURRENT_TIME))

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_next_backup_seconds Seconds until next scheduled backup
# TYPE insightlearn_dr_next_backup_seconds gauge
insightlearn_dr_next_backup_seconds $SECONDS_TO_NEXT_BACKUP
EOF

# Disk space metrics for backup location
BACKUP_DIR="/var/backups/k3s-cluster"
if [[ -d "$BACKUP_DIR" ]]; then
    # Get disk space info (in bytes)
    DISK_TOTAL=$(df -B1 "$BACKUP_DIR" | tail -1 | awk '{print $2}')
    DISK_USED=$(df -B1 "$BACKUP_DIR" | tail -1 | awk '{print $3}')
    DISK_AVAILABLE=$(df -B1 "$BACKUP_DIR" | tail -1 | awk '{print $4}')
    DISK_USAGE_PERCENT=$(df "$BACKUP_DIR" | tail -1 | awk '{print $5}' | sed 's/%//')

    # Count backup files
    BACKUP_COUNT=$(find "$BACKUP_DIR" -name "*.tar.gz" -type f 2>/dev/null | wc -l)
else
    DISK_TOTAL=0
    DISK_USED=0
    DISK_AVAILABLE=0
    DISK_USAGE_PERCENT=0
    BACKUP_COUNT=0
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_disk_total_bytes Total disk space for backup location
# TYPE insightlearn_dr_disk_total_bytes gauge
insightlearn_dr_disk_total_bytes $DISK_TOTAL

# HELP insightlearn_dr_disk_used_bytes Used disk space for backup location
# TYPE insightlearn_dr_disk_used_bytes gauge
insightlearn_dr_disk_used_bytes $DISK_USED

# HELP insightlearn_dr_disk_available_bytes Available disk space for backup location
# TYPE insightlearn_dr_disk_available_bytes gauge
insightlearn_dr_disk_available_bytes $DISK_AVAILABLE

# HELP insightlearn_dr_disk_usage_percent Disk usage percentage for backup location
# TYPE insightlearn_dr_disk_usage_percent gauge
insightlearn_dr_disk_usage_percent $DISK_USAGE_PERCENT

# HELP insightlearn_dr_backup_count Number of backup files
# TYPE insightlearn_dr_backup_count gauge
insightlearn_dr_backup_count $BACKUP_COUNT
EOF

# K3s Cluster Pod Metrics
# Get pod counts from namespace insightlearn
# Use k3s kubectl for K3s cluster (Rocky Linux)
if [[ -x /usr/local/bin/k3s ]]; then
    # Count running pods
    K3S_PODS_RUNNING=$(/usr/local/bin/k3s kubectl get pods -n insightlearn --no-headers 2>/dev/null | grep -c "Running" || echo "0")

    # Count total pods (all states)
    K3S_PODS_TOTAL=$(/usr/local/bin/k3s kubectl get pods -n insightlearn --no-headers 2>/dev/null | wc -l || echo "0")
else
    K3S_PODS_RUNNING=0
    K3S_PODS_TOTAL=0
fi

cat >> "$TEMP_METRICS" <<EOF

# HELP insightlearn_dr_k3s_pods_running Number of running pods in insightlearn namespace
# TYPE insightlearn_dr_k3s_pods_running gauge
insightlearn_dr_k3s_pods_running $K3S_PODS_RUNNING

# HELP insightlearn_dr_k3s_pods_total Total number of pods in insightlearn namespace
# TYPE insightlearn_dr_k3s_pods_total gauge
insightlearn_dr_k3s_pods_total $K3S_PODS_TOTAL
EOF

# Move temp file to final location atomically
mv "$TEMP_METRICS" "$METRICS_FILE"

# Output to stdout if not writing to file
if [[ "${OUTPUT_STDOUT:-0}" == "1" ]]; then
    cat "$METRICS_FILE"
fi

exit 0
