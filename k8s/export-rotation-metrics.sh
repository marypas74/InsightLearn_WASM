#!/bin/bash
################################################################################
# Password Rotation Metrics Exporter for Prometheus
#
# Purpose: Export rotation metrics to Prometheus for Grafana visualization
# Metrics:
#   - insightlearn_password_rotation_last_success_timestamp
#   - insightlearn_password_rotation_last_attempt_timestamp
#   - insightlearn_password_rotation_status (1=success, 0=failed, -1=running)
#   - insightlearn_password_rotation_next_scheduled_timestamp
#   - insightlearn_password_rotation_countdown_seconds
#   - insightlearn_password_age_seconds{secret="mssql|jwt|mongodb|redis|admin"}
#   - insightlearn_password_rotation_total_count
#   - insightlearn_password_rotation_failure_count
################################################################################

set -euo pipefail

NAMESPACE="${NAMESPACE:-insightlearn}"
METRICS_FILE="${METRICS_FILE:-/var/lib/node_exporter/textfile_collector/rotation_metrics.prom}"
METRICS_DIR="$(dirname "$METRICS_FILE")"
STATE_FILE="/tmp/rotation-state.json"

# Crea directory per metriche se non esiste
mkdir -p "$METRICS_DIR"

# Funzione per ottenere timestamp corrente
current_timestamp() {
    date +%s
}

# Funzione per ottenere prossima rotazione schedulata (ogni 2 giorni alle 2 AM UTC)
get_next_rotation_timestamp() {
    # Ottieni timestamp ultima rotazione o usa timestamp corrente
    local last_rotation=${1:-$(current_timestamp)}

    # Calcola prossima esecuzione CronJob (0 2 */2 * * = ogni 2 giorni alle 2 AM)
    # Semplificazione: aggiungi 2 giorni (172800 secondi)
    echo $((last_rotation + 172800))
}

# Funzione per calcolare età password (secondi dall'ultima rotazione)
get_password_age() {
    local secret_name=$1
    local last_rotation_time=${2:-$(current_timestamp)}

    # Ottieni timestamp dell'ultima modifica del secret da Kubernetes
    local secret_modified=$(kubectl get secret insightlearn-secrets -n "$NAMESPACE" \
        -o jsonpath='{.metadata.creationTimestamp}' 2>/dev/null || echo "")

    if [[ -n "$secret_modified" ]]; then
        local secret_timestamp=$(date -d "$secret_modified" +%s 2>/dev/null || echo "$last_rotation_time")
        echo $(($(current_timestamp) - secret_timestamp))
    else
        echo 0
    fi
}

# Funzione per ottenere hash SHA256 delle password (per tracking, non per storage in chiaro)
get_password_hash() {
    local secret_key=$1
    local password=$(kubectl get secret insightlearn-secrets -n "$NAMESPACE" \
        -o jsonpath="{.data.$secret_key}" 2>/dev/null | base64 -d | sha256sum | cut -d' ' -f1)
    echo "${password:-unknown}"
}

# Leggi stato da file precedente o inizializza
if [[ -f "$STATE_FILE" ]]; then
    LAST_SUCCESS=$(jq -r '.last_success // 0' "$STATE_FILE")
    LAST_ATTEMPT=$(jq -r '.last_attempt // 0' "$STATE_FILE")
    ROTATION_STATUS=$(jq -r '.status // -1' "$STATE_FILE")
    TOTAL_COUNT=$(jq -r '.total_count // 0' "$STATE_FILE")
    FAILURE_COUNT=$(jq -r '.failure_count // 0' "$STATE_FILE")
else
    LAST_SUCCESS=0
    LAST_ATTEMPT=0
    ROTATION_STATUS=-1  # -1=never run, 0=failed, 1=success
    TOTAL_COUNT=0
    FAILURE_COUNT=0
fi

# Se chiamato con argomento, aggiorna stato
if [[ $# -gt 0 ]]; then
    case "$1" in
        success)
            LAST_SUCCESS=$(current_timestamp)
            LAST_ATTEMPT=$(current_timestamp)
            ROTATION_STATUS=1
            TOTAL_COUNT=$((TOTAL_COUNT + 1))
            ;;
        failure)
            LAST_ATTEMPT=$(current_timestamp)
            ROTATION_STATUS=0
            TOTAL_COUNT=$((TOTAL_COUNT + 1))
            FAILURE_COUNT=$((FAILURE_COUNT + 1))
            ;;
        running)
            LAST_ATTEMPT=$(current_timestamp)
            ROTATION_STATUS=-1
            ;;
    esac

    # Salva stato
    cat > "$STATE_FILE" <<EOF
{
  "last_success": $LAST_SUCCESS,
  "last_attempt": $LAST_ATTEMPT,
  "status": $ROTATION_STATUS,
  "total_count": $TOTAL_COUNT,
  "failure_count": $FAILURE_COUNT
}
EOF
fi

# Calcola metriche
CURRENT_TIME=$(current_timestamp)
NEXT_ROTATION=$(get_next_rotation_timestamp "$LAST_SUCCESS")
COUNTDOWN=$((NEXT_ROTATION - CURRENT_TIME))
[[ $COUNTDOWN -lt 0 ]] && COUNTDOWN=0

# Ottieni età password per ogni secret
MSSQL_AGE=$(get_password_age "mssql-sa-password" "$LAST_SUCCESS")
JWT_AGE=$(get_password_age "jwt-secret-key" "$LAST_SUCCESS")
MONGODB_AGE=$(get_password_age "mongodb-password" "$LAST_SUCCESS")
REDIS_AGE=$(get_password_age "redis-password" "$LAST_SUCCESS")
ADMIN_AGE=$(get_password_age "admin-password" "$LAST_SUCCESS")

# Ottieni hash password correnti (per tracking, NON storage in chiaro)
MSSQL_HASH=$(get_password_hash "mssql-sa-password")
JWT_HASH=$(get_password_hash "jwt-secret-key")
MONGODB_HASH=$(get_password_hash "mongodb-password")
REDIS_HASH=$(get_password_hash "redis-password")
ADMIN_HASH=$(get_password_hash "admin-password")

# Genera file metriche Prometheus
cat > "$METRICS_FILE" <<EOF
# HELP insightlearn_password_rotation_last_success_timestamp Unix timestamp of last successful rotation
# TYPE insightlearn_password_rotation_last_success_timestamp gauge
insightlearn_password_rotation_last_success_timestamp $LAST_SUCCESS

# HELP insightlearn_password_rotation_last_attempt_timestamp Unix timestamp of last rotation attempt
# TYPE insightlearn_password_rotation_last_attempt_timestamp gauge
insightlearn_password_rotation_last_attempt_timestamp $LAST_ATTEMPT

# HELP insightlearn_password_rotation_status Current rotation status (1=success, 0=failed, -1=never_run)
# TYPE insightlearn_password_rotation_status gauge
insightlearn_password_rotation_status $ROTATION_STATUS

# HELP insightlearn_password_rotation_next_scheduled_timestamp Unix timestamp of next scheduled rotation
# TYPE insightlearn_password_rotation_next_scheduled_timestamp gauge
insightlearn_password_rotation_next_scheduled_timestamp $NEXT_ROTATION

# HELP insightlearn_password_rotation_countdown_seconds Seconds until next scheduled rotation
# TYPE insightlearn_password_rotation_countdown_seconds gauge
insightlearn_password_rotation_countdown_seconds $COUNTDOWN

# HELP insightlearn_password_age_seconds Age of password in seconds since last rotation
# TYPE insightlearn_password_age_seconds gauge
insightlearn_password_age_seconds{secret="mssql"} $MSSQL_AGE
insightlearn_password_age_seconds{secret="jwt"} $JWT_AGE
insightlearn_password_age_seconds{secret="mongodb"} $MONGODB_AGE
insightlearn_password_age_seconds{secret="redis"} $REDIS_AGE
insightlearn_password_age_seconds{secret="admin"} $ADMIN_AGE

# HELP insightlearn_password_rotation_total_count Total number of rotation attempts
# TYPE insightlearn_password_rotation_total_count counter
insightlearn_password_rotation_total_count $TOTAL_COUNT

# HELP insightlearn_password_rotation_failure_count Total number of failed rotations
# TYPE insightlearn_password_rotation_failure_count counter
insightlearn_password_rotation_failure_count $FAILURE_COUNT

# HELP insightlearn_password_hash_info Hash SHA256 of current passwords (for tracking only)
# TYPE insightlearn_password_hash_info gauge
insightlearn_password_hash_info{secret="mssql",hash="$MSSQL_HASH"} 1
insightlearn_password_hash_info{secret="jwt",hash="$JWT_HASH"} 1
insightlearn_password_hash_info{secret="mongodb",hash="$MONGODB_HASH"} 1
insightlearn_password_hash_info{secret="redis",hash="$REDIS_HASH"} 1
insightlearn_password_hash_info{secret="admin",hash="$ADMIN_HASH"} 1
EOF

echo "✓ Metriche esportate: $METRICS_FILE"
echo "  - Last success: $(date -d @$LAST_SUCCESS 2>/dev/null || echo 'Never')"
echo "  - Next rotation: $(date -d @$NEXT_ROTATION 2>/dev/null || echo 'Unknown')"
echo "  - Countdown: ${COUNTDOWN}s ($(($COUNTDOWN / 3600))h $(($COUNTDOWN % 3600 / 60))m)"
echo "  - Status: $ROTATION_STATUS (1=success, 0=failed, -1=never_run)"
