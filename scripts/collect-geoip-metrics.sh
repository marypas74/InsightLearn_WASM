#!/bin/bash
# =============================================================================
# GeoIP Metrics Collection Script for InsightLearn
# Collects country data from Cloudflare CF-IPCountry header via nginx logs
# Writes to node_exporter textfile collector for Prometheus/Grafana
# =============================================================================

set -e

# Configuration
NAMESPACE="insightlearn"
WASM_POD_LABEL="app=insightlearn-wasm-blazor-webassembly"
NGINX_LOG="/var/log/nginx/geoip_access.log"
METRICS_FILE="/var/lib/node_exporter/textfile_collector/geoip_metrics.prom"
TEMP_FILE="/tmp/geoip_metrics_temp.prom"
LOG_FILE="/tmp/geoip-collector.log"

# Ensure kubectl can access K3s when running as root
if [ -f /etc/rancher/k3s/k3s.yaml ]; then
    export KUBECONFIG=/etc/rancher/k3s/k3s.yaml
fi

# Country code to full name mapping (ISO 3166-1 alpha-2)
declare -A COUNTRY_NAMES=(
    ["IT"]="Italy"
    ["US"]="United States"
    ["DE"]="Germany"
    ["FR"]="France"
    ["GB"]="United Kingdom"
    ["ES"]="Spain"
    ["NL"]="Netherlands"
    ["BR"]="Brazil"
    ["CN"]="China"
    ["JP"]="Japan"
    ["CA"]="Canada"
    ["AU"]="Australia"
    ["IN"]="India"
    ["RU"]="Russia"
    ["PL"]="Poland"
    ["CH"]="Switzerland"
    ["AT"]="Austria"
    ["BE"]="Belgium"
    ["SE"]="Sweden"
    ["PT"]="Portugal"
    ["MX"]="Mexico"
    ["KR"]="South Korea"
    ["AR"]="Argentina"
    ["ZA"]="South Africa"
    ["SG"]="Singapore"
    ["HK"]="Hong Kong"
    ["IE"]="Ireland"
    ["NO"]="Norway"
    ["DK"]="Denmark"
    ["FI"]="Finland"
    ["CZ"]="Czech Republic"
    ["RO"]="Romania"
    ["HU"]="Hungary"
    ["TR"]="Turkey"
    ["UA"]="Ukraine"
    ["GR"]="Greece"
    ["IL"]="Israel"
    ["AE"]="United Arab Emirates"
    ["TH"]="Thailand"
    ["VN"]="Vietnam"
    ["PH"]="Philippines"
    ["ID"]="Indonesia"
    ["MY"]="Malaysia"
    ["NZ"]="New Zealand"
    ["CO"]="Colombia"
    ["CL"]="Chile"
    ["PE"]="Peru"
)

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo "$msg"
    echo "$msg" >> "$LOG_FILE" 2>/dev/null || true
}

# Get the WASM pod name
get_wasm_pod() {
    kubectl get pod -n "$NAMESPACE" -l "$WASM_POD_LABEL" -o jsonpath='{.items[0].metadata.name}' 2>/dev/null
}

# Main collection function
collect_metrics() {
    log "Starting GeoIP metrics collection..."

    # Get WASM pod
    WASM_POD=$(get_wasm_pod)
    if [ -z "$WASM_POD" ]; then
        log "ERROR: WASM pod not found"
        return 1
    fi
    log "Found WASM pod: $WASM_POD"

    # Check if log file exists in pod
    if ! kubectl exec -n "$NAMESPACE" "$WASM_POD" -- test -f "$NGINX_LOG" 2>/dev/null; then
        log "WARNING: Log file not yet created (no API traffic yet?)"
        # Create empty metrics file
        echo "# HELP node_api_requests_by_country API requests by client country (Cloudflare)" > "$TEMP_FILE"
        echo "# TYPE node_api_requests_by_country gauge" >> "$TEMP_FILE"
        echo "# No traffic data yet" >> "$TEMP_FILE"
        mv "$TEMP_FILE" "$METRICS_FILE"
        return 0
    fi

    # Get log content and process
    LOG_CONTENT=$(kubectl exec -n "$NAMESPACE" "$WASM_POD" -- cat "$NGINX_LOG" 2>/dev/null || echo "")

    if [ -z "$LOG_CONTENT" ]; then
        log "WARNING: Log file is empty"
        return 0
    fi

    # Count requests per country code
    declare -A COUNTRY_COUNTS

    while IFS=' ' read -r country_code rest; do
        # Skip empty lines and invalid entries
        if [ -z "$country_code" ] || [ "$country_code" == "-" ]; then
            continue
        fi

        # Only count successful requests (status 2xx or 3xx)
        status=$(echo "$rest" | awk '{print $NF}')
        if [[ "$status" =~ ^[23] ]]; then
            COUNTRY_COUNTS["$country_code"]=$((${COUNTRY_COUNTS["$country_code"]:-0} + 1))
        fi
    done <<< "$LOG_CONTENT"

    # Generate Prometheus metrics
    {
        echo "# HELP node_api_requests_by_country API requests by client country (Cloudflare)"
        echo "# TYPE node_api_requests_by_country gauge"
        echo "# Generated: $(date -Iseconds)"
        echo "# Source: Real Cloudflare CF-IPCountry header data"

        # Sort by count (descending) and output metrics
        for code in "${!COUNTRY_COUNTS[@]}"; do
            count=${COUNTRY_COUNTS[$code]}
            # Get full country name, fallback to code if unknown
            full_name="${COUNTRY_NAMES[$code]:-$code}"
            echo "node_api_requests_by_country{country=\"$full_name\"} $count"
        done | sort -t'=' -k2 -rn
    } > "$TEMP_FILE"

    # Atomically update metrics file
    mv "$TEMP_FILE" "$METRICS_FILE"
    chmod 644 "$METRICS_FILE"

    # Log rotation: truncate the log file in the pod after processing
    kubectl exec -n "$NAMESPACE" "$WASM_POD" -- sh -c "cat /dev/null > $NGINX_LOG" 2>/dev/null || true

    # Count total requests processed
    total_requests=0
    for count in "${COUNTRY_COUNTS[@]}"; do
        total_requests=$((total_requests + count))
    done

    log "SUCCESS: Processed $total_requests requests from ${#COUNTRY_COUNTS[@]} countries"
    log "Top countries: $(for code in "${!COUNTRY_COUNTS[@]}"; do echo "${COUNTRY_NAMES[$code]:-$code}:${COUNTRY_COUNTS[$code]}"; done | sort -t: -k2 -rn | head -3 | tr '\n' ' ')"
}

# Run collection
collect_metrics

log "GeoIP metrics collection completed"
