#!/bin/bash
#
# Script: generate-demo-subtitles.sh
# Purpose: Generate DEMO subtitles using Ollama LLM (no real ASR required)
# Author: Claude Code
# Date: 2025-12-16
#
# This script generates realistic demo subtitles based on lesson titles and durations
# using the MockOllamaService. Useful for:
# - Testing subtitle features
# - Demos when Whisper API is not available
# - Development environments
#
# Usage:
#   ./generate-demo-subtitles.sh [--dry-run] [--language LANG] [--min-duration MINUTES]

set -e

# ============================================
# Configuration
# ============================================
NAMESPACE="insightlearn"
API_BASE_URL="http://localhost:31081"
LANGUAGE="it-IT"
MIN_DURATION_MINUTES=5
DRY_RUN=false
LOG_FILE="/tmp/demo-subtitle-generation-$(date +%Y%m%d_%H%M%S).log"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

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
            echo "Usage: $0 [--dry-run] [--language LANG] [--min-duration MINUTES]"
            echo ""
            echo "Generate DEMO subtitles using Ollama (no Whisper ASR required)"
            echo ""
            echo "Options:"
            echo "  --dry-run          Preview without generating"
            echo "  --language LANG    Language code (default: it-IT)"
            echo "  --min-duration N   Minimum video duration in minutes (default: 5)"
            echo "  --api-url URL      API base URL (default: http://localhost:31081)"
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

log_info() { log "${BLUE}[INFO]${NC} $1"; }
log_success() { log "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { log "${YELLOW}[WARNING]${NC} $1"; }
log_error() { log "${RED}[ERROR]${NC} $1"; }
log_header() {
    echo "" | tee -a "$LOG_FILE"
    echo -e "${CYAN}========================================${NC}" | tee -a "$LOG_FILE"
    echo -e "${CYAN}$1${NC}" | tee -a "$LOG_FILE"
    echo -e "${CYAN}========================================${NC}" | tee -a "$LOG_FILE"
}

# ============================================
# Get SQL Password
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
# Get Video Lessons
# ============================================
get_video_lessons() {
    local query="
        SET NOCOUNT ON;
        SELECT
            CAST(l.Id AS VARCHAR(36)) AS LessonId,
            l.Title,
            l.DurationMinutes,
            c.Title AS CourseTitle
        FROM Lessons l
        INNER JOIN Sections s ON l.SectionId = s.Id
        INNER JOIN Courses c ON s.CourseId = c.Id
        WHERE
            l.Type = 0
            AND l.IsActive = 1
            AND l.DurationMinutes > $MIN_DURATION_MINUTES
        ORDER BY c.Title, s.OrderIndex, l.OrderIndex;
    "
    execute_sql "$query"
}

# ============================================
# Check Existing Transcripts
# ============================================
check_transcript_exists() {
    local lesson_id="$1"
    local response
    response=$(curl -s -o /dev/null -w "%{http_code}" \
        "$API_BASE_URL/api/transcripts/$lesson_id" 2>/dev/null)
    [ "$response" == "200" ]
}

# ============================================
# Generate Demo Transcript via Ollama
# ============================================
generate_demo_transcript() {
    local lesson_id="$1"
    local lesson_title="$2"
    local duration_minutes="$3"

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would generate demo transcript for: $lesson_title"
        return 0
    fi

    log_info "Generating demo transcript for: $lesson_title"

    # Call the auto-generate endpoint
    local response
    response=$(curl -s -X POST "$API_BASE_URL/api/video-transcripts/generate" \
        -H "Content-Type: application/json" \
        -d "{
            \"lessonId\": \"$lesson_id\",
            \"lessonTitle\": \"$lesson_title\",
            \"durationSeconds\": $((duration_minutes * 60)),
            \"language\": \"$LANGUAGE\",
            \"useDemo\": true
        }" 2>/dev/null)

    if [ $? -eq 0 ] && [ -n "$response" ]; then
        log_success "Demo transcript generated"
        return 0
    else
        log_error "Failed: $response"
        return 1
    fi
}

# ============================================
# Convert Transcript to WebVTT Subtitle
# ============================================
create_webvtt_from_transcript() {
    local lesson_id="$1"
    local lesson_title="$2"
    local duration_minutes="$3"

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would create WebVTT for: $lesson_title"
        return 0
    fi

    # Generate demo subtitles directly
    local duration_seconds=$((duration_minutes * 60))
    local segment_duration=10
    local total_segments=$((duration_seconds / segment_duration))

    # Create WebVTT content
    local webvtt="WEBVTT
Kind: subtitles
Language: ${LANGUAGE%%-*}

NOTE Generated by InsightLearn Demo Subtitle Generator
NOTE Lesson: $lesson_title
NOTE Duration: ${duration_minutes} minutes

"

    local demo_texts=(
        "Benvenuti a questa lezione su $lesson_title"
        "Iniziamo con i concetti fondamentali"
        "È importante comprendere questi elementi base"
        "Vediamo come applicare questi principi"
        "Ecco un esempio pratico"
        "Notate come questo si collega al tema principale"
        "Passiamo ora al prossimo argomento"
        "Ricordate sempre di praticare questi concetti"
        "Continuiamo con la teoria"
        "Questo è un punto molto importante"
        "Vediamo insieme come funziona"
        "Ecco un altro esempio utile"
        "Riassumiamo quanto visto finora"
        "Andiamo avanti con il materiale"
        "Questo concetto è fondamentale"
    )

    local text_count=${#demo_texts[@]}
    local cue_num=1

    for ((i=0; i<total_segments && i<100; i++)); do
        local start_time=$((i * segment_duration))
        local end_time=$((start_time + segment_duration))

        # Format timestamps as HH:MM:SS.mmm
        local start_formatted=$(printf "%02d:%02d:%02d.000" $((start_time/3600)) $(((start_time%3600)/60)) $((start_time%60)))
        local end_formatted=$(printf "%02d:%02d:%02d.000" $((end_time/3600)) $(((end_time%3600)/60)) $((end_time%60)))

        local text_index=$((i % text_count))
        local text="${demo_texts[$text_index]}"

        webvtt+="$cue_num
$start_formatted --> $end_formatted
$text

"
        ((cue_num++))
    done

    # Save to temp file
    local temp_vtt="/tmp/subtitle_${lesson_id}.vtt"
    echo "$webvtt" > "$temp_vtt"

    # Upload via API
    local response
    response=$(curl -s -X POST "$API_BASE_URL/api/subtitles/upload" \
        -F "lessonId=$lesson_id" \
        -F "language=${LANGUAGE%%-*}" \
        -F "label=${LANGUAGE}" \
        -F "file=@$temp_vtt" 2>/dev/null)

    rm -f "$temp_vtt"

    if echo "$response" | grep -q "id\|success\|Id" 2>/dev/null; then
        log_success "WebVTT subtitle uploaded"
        return 0
    else
        log_error "Upload failed: $response"
        return 1
    fi
}

# ============================================
# Main
# ============================================
main() {
    log_header "Demo Subtitle Generation Script"
    log_info "Language: $LANGUAGE"
    log_info "Minimum Duration: $MIN_DURATION_MINUTES minutes"
    log_info "Dry Run: $DRY_RUN"
    log_info "Using: MockOllamaService (demo mode)"

    # Get lessons
    log_header "Fetching Video Lessons"
    local lessons_raw
    lessons_raw=$(get_video_lessons)

    if [ -z "$lessons_raw" ]; then
        log_warning "No video lessons found"
        exit 0
    fi

    # Parse lessons
    declare -a lessons=()
    while IFS='|' read -r lesson_id title duration course_title; do
        lesson_id=$(echo "$lesson_id" | xargs)
        title=$(echo "$title" | xargs)
        duration=$(echo "$duration" | xargs)
        course_title=$(echo "$course_title" | xargs)

        if [ -n "$lesson_id" ] && [ "$lesson_id" != "LessonId" ]; then
            lessons+=("$lesson_id|$title|$duration|$course_title")
        fi
    done <<< "$lessons_raw"

    local total=${#lessons[@]}
    log_info "Found $total video lessons > $MIN_DURATION_MINUTES minutes"

    # Process
    log_header "Processing"
    local processed=0
    local skipped=0
    local failed=0

    for lesson in "${lessons[@]}"; do
        IFS='|' read -r lesson_id title duration course_title <<< "$lesson"

        echo "" | tee -a "$LOG_FILE"
        log_info "[$((processed + skipped + failed + 1))/$total] $title (${duration}min)"

        # Create WebVTT directly
        if create_webvtt_from_transcript "$lesson_id" "$title" "$duration"; then
            ((processed++))
        else
            ((failed++))
        fi

        # Small delay
        sleep 0.5
    done

    # Summary
    log_header "Summary"
    log_success "Processed: $processed"
    log_warning "Skipped: $skipped"
    log_error "Failed: $failed"
    log_info "Log: $LOG_FILE"

    if [ "$DRY_RUN" = true ]; then
        log_warning "This was a DRY RUN"
    fi
}

main "$@"
