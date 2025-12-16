#!/bin/bash
#
# Script: generate-all-subtitles.sh
# Purpose: Generate subtitles for all video lessons > 5 minutes
# Author: Claude Code
# Date: 2025-12-16
#
# Requirements:
# - kubectl access to insightlearn namespace
# - SQL Server pod running (sqlserver-0)
# - API running and accessible
# - MongoDB with video files
#
# Usage:
#   ./generate-all-subtitles.sh [--dry-run] [--language LANG] [--min-duration MINUTES]
#
# Examples:
#   ./generate-all-subtitles.sh                    # Generate Italian subtitles for videos > 5 min
#   ./generate-all-subtitles.sh --language en-US   # Generate English subtitles
#   ./generate-all-subtitles.sh --min-duration 10  # Videos > 10 minutes
#   ./generate-all-subtitles.sh --dry-run          # Preview without generating

set -e

# ============================================
# Configuration
# ============================================
NAMESPACE="insightlearn"
API_BASE_URL="http://localhost:31081"
LANGUAGE="it-IT"
MIN_DURATION_MINUTES=5
DRY_RUN=false
BATCH_SIZE=10
BATCH_DELAY_SECONDS=5
LOG_FILE="/tmp/subtitle-generation-$(date +%Y%m%d_%H%M%S).log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# ============================================
# Parse Arguments
# ============================================
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --language)
            LANGUAGE="$2"
            shift 2
            ;;
        --min-duration)
            MIN_DURATION_MINUTES="$2"
            shift 2
            ;;
        --api-url)
            API_BASE_URL="$2"
            shift 2
            ;;
        --help|-h)
            echo "Usage: $0 [--dry-run] [--language LANG] [--min-duration MINUTES] [--api-url URL]"
            echo ""
            echo "Options:"
            echo "  --dry-run          Preview lessons without generating subtitles"
            echo "  --language LANG    Language code (default: it-IT)"
            echo "  --min-duration N   Minimum video duration in minutes (default: 5)"
            echo "  --api-url URL      API base URL (default: http://localhost:31081)"
            echo ""
            echo "Supported languages: it-IT, en-US, es-ES, fr-FR, de-DE, pt-BR, ru-RU, zh-CN, ja-JP, ko-KR"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# ============================================
# Logging Functions
# ============================================
log() {
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo -e "${timestamp} | $1" | tee -a "$LOG_FILE"
}

log_info() {
    log "${BLUE}[INFO]${NC} $1"
}

log_success() {
    log "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    log "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    log "${RED}[ERROR]${NC} $1"
}

log_header() {
    echo "" | tee -a "$LOG_FILE"
    echo -e "${CYAN}========================================${NC}" | tee -a "$LOG_FILE"
    echo -e "${CYAN}$1${NC}" | tee -a "$LOG_FILE"
    echo -e "${CYAN}========================================${NC}" | tee -a "$LOG_FILE"
}

# ============================================
# Prerequisites Check
# ============================================
check_prerequisites() {
    log_header "Checking Prerequisites"

    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl not found. Please install kubectl."
        exit 1
    fi
    log_success "kubectl found"

    # Check kubectl connection
    if ! kubectl get nodes &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    log_success "Kubernetes cluster connected"

    # Check namespace exists
    if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
        log_error "Namespace '$NAMESPACE' not found"
        exit 1
    fi
    log_success "Namespace '$NAMESPACE' found"

    # Check SQL Server pod
    if ! kubectl get pod sqlserver-0 -n "$NAMESPACE" &> /dev/null; then
        log_error "SQL Server pod (sqlserver-0) not found"
        exit 1
    fi
    log_success "SQL Server pod found"

    # Check API availability
    if ! curl -s "$API_BASE_URL/health" > /dev/null 2>&1; then
        log_warning "API at $API_BASE_URL might not be accessible"
    else
        log_success "API is accessible at $API_BASE_URL"
    fi

    # Check jq
    if ! command -v jq &> /dev/null; then
        log_error "jq not found. Please install jq (sudo dnf install jq)"
        exit 1
    fi
    log_success "jq found"
}

# ============================================
# Get SQL Server Password from Secret
# ============================================
get_sql_password() {
    kubectl get secret insightlearn-secrets -n "$NAMESPACE" \
        -o jsonpath='{.data.mssql-sa-password}' 2>/dev/null | base64 -d
}

# ============================================
# Execute SQL Query
# ============================================
execute_sql() {
    local query="$1"
    local password
    password=$(get_sql_password)

    kubectl exec sqlserver-0 -n "$NAMESPACE" -- \
        /opt/mssql-tools18/bin/sqlcmd \
        -S localhost \
        -U sa \
        -P "$password" \
        -C \
        -d InsightLearnDb \
        -h -1 \
        -W \
        -Q "$query" 2>/dev/null
}

# ============================================
# Get Video Lessons > X Minutes
# ============================================
get_video_lessons() {
    log_header "Fetching Video Lessons > $MIN_DURATION_MINUTES minutes"

    local query="
        SET NOCOUNT ON;
        SELECT
            CAST(l.Id AS VARCHAR(36)) AS LessonId,
            l.Title,
            l.DurationMinutes,
            ISNULL(l.VideoUrl, '') AS VideoUrl,
            s.Title AS SectionTitle,
            c.Title AS CourseTitle,
            CAST(c.Id AS VARCHAR(36)) AS CourseId
        FROM Lessons l
        INNER JOIN Sections s ON l.SectionId = s.Id
        INNER JOIN Courses c ON s.CourseId = c.Id
        WHERE
            l.Type = 0
            AND l.IsActive = 1
            AND l.DurationMinutes > $MIN_DURATION_MINUTES
            AND (l.VideoUrl IS NOT NULL AND l.VideoUrl != '')
        ORDER BY c.Title, s.OrderIndex, l.OrderIndex;
    "

    execute_sql "$query"
}

# ============================================
# Check if Subtitles Exist
# ============================================
check_subtitles_exist() {
    local lesson_id="$1"
    local lang_code="${LANGUAGE%%-*}"  # Extract "it" from "it-IT"

    local response
    response=$(curl -s -o /dev/null -w "%{http_code}" \
        "$API_BASE_URL/api/subtitles/lesson/$lesson_id" 2>/dev/null)

    if [ "$response" == "200" ]; then
        # Check if our language exists
        local subtitles
        subtitles=$(curl -s "$API_BASE_URL/api/subtitles/lesson/$lesson_id" 2>/dev/null)

        if echo "$subtitles" | jq -e ".[] | select(.language == \"$lang_code\")" > /dev/null 2>&1; then
            return 0  # Subtitles exist
        fi
    fi
    return 1  # Subtitles don't exist
}

# ============================================
# Get Video File ID from MongoDB
# ============================================
get_video_file_id() {
    local lesson_id="$1"

    # Query MongoDB for video file ID
    local mongo_query="db.fs.files.findOne({\"metadata.lessonId\": \"$lesson_id\"}, {_id: 1})"

    local result
    result=$(kubectl exec mongodb-0 -n "$NAMESPACE" -- \
        mongosh --quiet \
        -u insightlearn \
        -p "$(kubectl get secret insightlearn-secrets -n "$NAMESPACE" -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
        --authenticationDatabase admin \
        insightlearn_videos \
        --eval "$mongo_query" 2>/dev/null | grep -oP "ObjectId\('\K[a-f0-9]+")

    echo "$result"
}

# ============================================
# Queue Subtitle Generation
# ============================================
queue_subtitle_generation() {
    local lesson_id="$1"
    local video_file_id="$2"
    local lesson_title="$3"

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would generate subtitles for: $lesson_title"
        return 0
    fi

    log_info "Queueing subtitle generation for: $lesson_title"

    local payload=$(cat <<EOF
{
    "lessonId": "$lesson_id",
    "videoFileId": "$video_file_id",
    "language": "$LANGUAGE"
}
EOF
)

    local response
    response=$(curl -s -X POST "$API_BASE_URL/api/subtitles/generate" \
        -H "Content-Type: application/json" \
        -d "$payload" 2>/dev/null)

    if echo "$response" | jq -e '.jobId' > /dev/null 2>&1; then
        local job_id
        job_id=$(echo "$response" | jq -r '.jobId')
        log_success "Queued successfully. Job ID: $job_id"
        return 0
    else
        log_error "Failed to queue: $response"
        return 1
    fi
}

# ============================================
# Generate Demo Subtitles (Ollama)
# ============================================
generate_demo_subtitles() {
    local lesson_id="$1"
    local lesson_title="$2"
    local duration_minutes="$3"

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would generate demo subtitles for: $lesson_title"
        return 0
    fi

    log_info "Generating demo subtitles for: $lesson_title"

    local payload=$(cat <<EOF
{
    "lessonId": "$lesson_id",
    "lessonTitle": "$lesson_title",
    "durationSeconds": $((duration_minutes * 60)),
    "language": "$LANGUAGE"
}
EOF
)

    local response
    response=$(curl -s -X POST "$API_BASE_URL/api/transcripts/$lesson_id/auto-generate" \
        -H "Content-Type: application/json" \
        -d "$payload" 2>/dev/null)

    if [ $? -eq 0 ]; then
        log_success "Demo transcript generated for: $lesson_title"
        return 0
    else
        log_error "Failed to generate demo transcript: $response"
        return 1
    fi
}

# ============================================
# Main Processing
# ============================================
main() {
    log_header "Subtitle Generation Script"
    log_info "Language: $LANGUAGE"
    log_info "Minimum Duration: $MIN_DURATION_MINUTES minutes"
    log_info "Dry Run: $DRY_RUN"
    log_info "Log File: $LOG_FILE"

    # Check prerequisites
    check_prerequisites

    # Get video lessons
    local lessons_raw
    lessons_raw=$(get_video_lessons)

    if [ -z "$lessons_raw" ]; then
        log_warning "No video lessons found > $MIN_DURATION_MINUTES minutes"
        exit 0
    fi

    # Parse lessons into array
    declare -a lessons=()
    while IFS='|' read -r lesson_id title duration video_url section_title course_title course_id; do
        # Skip empty lines and headers
        if [ -n "$lesson_id" ] && [ "$lesson_id" != "LessonId" ]; then
            # Trim whitespace
            lesson_id=$(echo "$lesson_id" | xargs)
            title=$(echo "$title" | xargs)
            duration=$(echo "$duration" | xargs)

            if [ -n "$lesson_id" ]; then
                lessons+=("$lesson_id|$title|$duration|$course_title")
            fi
        fi
    done <<< "$lessons_raw"

    local total_lessons=${#lessons[@]}
    log_info "Found $total_lessons video lessons > $MIN_DURATION_MINUTES minutes"

    if [ "$total_lessons" -eq 0 ]; then
        log_warning "No lessons to process"
        exit 0
    fi

    # Display summary
    log_header "Lessons to Process"
    for lesson in "${lessons[@]}"; do
        IFS='|' read -r lesson_id title duration course_title <<< "$lesson"
        echo "  - $title (${duration}min) - Course: $course_title"
    done | tee -a "$LOG_FILE"

    # Process lessons
    log_header "Processing Lessons"

    local processed=0
    local skipped=0
    local failed=0
    local batch_count=0

    for lesson in "${lessons[@]}"; do
        IFS='|' read -r lesson_id title duration course_title <<< "$lesson"

        ((batch_count++))

        echo "" | tee -a "$LOG_FILE"
        log_info "[$((processed + skipped + failed + 1))/$total_lessons] Processing: $title"

        # Check if subtitles already exist
        if check_subtitles_exist "$lesson_id"; then
            log_warning "Subtitles already exist, skipping"
            ((skipped++))
            continue
        fi

        # Get video file ID from MongoDB
        local video_file_id
        video_file_id=$(get_video_file_id "$lesson_id")

        if [ -z "$video_file_id" ]; then
            log_warning "No video file found in MongoDB for lesson: $lesson_id"
            log_info "Attempting to generate demo subtitles using Ollama..."

            if generate_demo_subtitles "$lesson_id" "$title" "$duration"; then
                ((processed++))
            else
                ((failed++))
            fi
            continue
        fi

        # Queue subtitle generation
        if queue_subtitle_generation "$lesson_id" "$video_file_id" "$title"; then
            ((processed++))
        else
            ((failed++))
        fi

        # Batch delay
        if [ $batch_count -ge $BATCH_SIZE ]; then
            log_info "Batch complete. Waiting ${BATCH_DELAY_SECONDS}s before next batch..."
            sleep $BATCH_DELAY_SECONDS
            batch_count=0
        fi
    done

    # Summary
    log_header "Processing Complete"
    log_info "Total Lessons: $total_lessons"
    log_success "Processed: $processed"
    log_warning "Skipped (already exist): $skipped"
    log_error "Failed: $failed"
    echo ""
    log_info "Log file: $LOG_FILE"

    if [ "$DRY_RUN" = true ]; then
        echo ""
        log_warning "This was a DRY RUN. No subtitles were actually generated."
        log_info "Run without --dry-run to generate subtitles."
    fi
}

# ============================================
# Run Main
# ============================================
main "$@"
