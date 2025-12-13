#!/bin/bash
# InsightLearn Video Conversion Script
# Converts all MP4 videos in MongoDB to WebM format for universal browser support
#
# Usage: ./convert-videos-to-webm.sh
#
# Requirements:
# - ffmpeg installed on the system
# - kubectl access to insightlearn namespace
# - MongoDB password in Kubernetes secret

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=============================================="
echo "InsightLearn Video Conversion - MP4 to WebM"
echo "=============================================="
echo ""

# Check ffmpeg is installed
if ! command -v ffmpeg &> /dev/null; then
    echo -e "${RED}Error: ffmpeg is not installed${NC}"
    echo "Install with: sudo dnf install ffmpeg"
    exit 1
fi

# Get MongoDB password
MONGO_PASS=$(kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.mongodb-password}' | base64 -d)
if [ -z "$MONGO_PASS" ]; then
    echo -e "${RED}Error: Could not get MongoDB password from secret${NC}"
    exit 1
fi
echo -e "${GREEN}✓ MongoDB password retrieved${NC}"

# Get SQL Server password
SQL_PASS=$(kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.mssql-sa-password}' | base64 -d)
if [ -z "$SQL_PASS" ]; then
    echo -e "${RED}Error: Could not get SQL Server password from secret${NC}"
    exit 1
fi
echo -e "${GREEN}✓ SQL Server password retrieved${NC}"

# Create temp directory
TEMP_DIR="/tmp/insightlearn-video-conversion"
mkdir -p "$TEMP_DIR"
echo -e "${GREEN}✓ Temp directory created: $TEMP_DIR${NC}"

# Get list of MP4 videos from database
echo ""
echo "Fetching MP4 videos from database..."
VIDEOS=$(kubectl exec sqlserver-0 -n insightlearn -- /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SQL_PASS" -C -d InsightLearnDb \
    -Q "SET NOCOUNT ON; SELECT Id, Title, VideoUrl FROM Lessons WHERE VideoFormat = 'mp4' AND VideoUrl IS NOT NULL AND VideoUrl LIKE '/api/video/stream/%'" \
    -W -h -1 -s "|" 2>/dev/null | grep -v "^$" | head -50)

VIDEO_COUNT=$(echo "$VIDEOS" | wc -l)
echo -e "${YELLOW}Found $VIDEO_COUNT MP4 videos to convert${NC}"
echo ""

# Process each video
COUNTER=0
SUCCESS=0
FAILED=0

while IFS='|' read -r LESSON_ID TITLE VIDEO_URL; do
    COUNTER=$((COUNTER + 1))

    # Extract MongoDB file ID from URL
    MONGO_ID=$(echo "$VIDEO_URL" | sed 's|/api/video/stream/||')

    echo "[$COUNTER/$VIDEO_COUNT] Processing: $TITLE"
    echo "  Lesson ID: $LESSON_ID"
    echo "  MongoDB ID: $MONGO_ID"

    # Download video from MongoDB
    INPUT_FILE="$TEMP_DIR/${MONGO_ID}.mp4"
    OUTPUT_FILE="$TEMP_DIR/${MONGO_ID}.webm"

    echo "  Downloading from MongoDB..."
    curl -s "http://localhost:31081/api/video/stream/$MONGO_ID" -o "$INPUT_FILE"

    if [ ! -f "$INPUT_FILE" ] || [ ! -s "$INPUT_FILE" ]; then
        echo -e "  ${RED}✗ Download failed${NC}"
        FAILED=$((FAILED + 1))
        continue
    fi

    INPUT_SIZE=$(du -h "$INPUT_FILE" | cut -f1)
    echo "  Downloaded: $INPUT_SIZE"

    # Convert to WebM using VP9 codec
    echo "  Converting to WebM (VP9)..."
    ffmpeg -i "$INPUT_FILE" \
        -c:v libvpx-vp9 \
        -crf 30 \
        -b:v 0 \
        -c:a libopus \
        -b:a 128k \
        -y \
        -loglevel error \
        "$OUTPUT_FILE" 2>&1

    if [ ! -f "$OUTPUT_FILE" ] || [ ! -s "$OUTPUT_FILE" ]; then
        echo -e "  ${RED}✗ Conversion failed${NC}"
        FAILED=$((FAILED + 1))
        rm -f "$INPUT_FILE"
        continue
    fi

    OUTPUT_SIZE=$(du -h "$OUTPUT_FILE" | cut -f1)
    echo "  Converted: $OUTPUT_SIZE"

    # Upload new WebM video to MongoDB
    echo "  Uploading WebM to MongoDB..."
    NEW_MONGO_ID=$(curl -s -X POST "http://localhost:31081/api/video/upload" \
        -H "Content-Type: multipart/form-data" \
        -F "file=@$OUTPUT_FILE" \
        -F "lessonId=$LESSON_ID" \
        -F "userId=00000000-0000-0000-0000-000000000000" \
        -F "contentType=video/webm" \
        2>/dev/null | jq -r '.fileId // empty')

    if [ -z "$NEW_MONGO_ID" ]; then
        echo -e "  ${YELLOW}⚠ Upload may have failed, keeping original${NC}"
        FAILED=$((FAILED + 1))
        rm -f "$INPUT_FILE" "$OUTPUT_FILE"
        continue
    fi

    echo "  New MongoDB ID: $NEW_MONGO_ID"

    # Update database with new video URL
    echo "  Updating database..."
    kubectl exec sqlserver-0 -n insightlearn -- /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SQL_PASS" -C -d InsightLearnDb \
        -Q "UPDATE Lessons SET VideoUrl = '/api/video/stream/$NEW_MONGO_ID', VideoFormat = 'webm' WHERE Id = '$LESSON_ID'" \
        -W 2>/dev/null

    echo -e "  ${GREEN}✓ Converted successfully${NC}"
    SUCCESS=$((SUCCESS + 1))

    # Cleanup
    rm -f "$INPUT_FILE" "$OUTPUT_FILE"

    echo ""
done <<< "$VIDEOS"

# Summary
echo "=============================================="
echo "Conversion Complete!"
echo "=============================================="
echo "Total videos processed: $COUNTER"
echo -e "Successful: ${GREEN}$SUCCESS${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo ""
echo "Videos are now in WebM format (VP9 codec)"
echo "Compatible with all browsers including Firefox Linux"

# Cleanup temp directory
rm -rf "$TEMP_DIR"
