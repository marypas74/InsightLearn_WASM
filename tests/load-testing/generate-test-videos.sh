#!/bin/bash
# Generate Test Videos for MongoDB Load Testing
# Target: Fill 50% of MongoDB storage (~112 GB)

set -e

OUTPUT_DIR="/tmp/insightlearn-test-videos"
VIDEO_SIZE_MB=100  # Each video is 100MB
TARGET_SIZE_GB=112
VIDEOS_COUNT=$((TARGET_SIZE_GB * 1024 / VIDEO_SIZE_MB))  # ~1152 videos

echo "🎬 InsightLearn Video Generator for Load Testing"
echo "================================================"
echo "Target storage: ${TARGET_SIZE_GB} GB"
echo "Video size: ${VIDEO_SIZE_MB} MB each"
echo "Videos to generate: ${VIDEOS_COUNT}"
echo ""

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Check ffmpeg availability
if ! command -v ffmpeg &> /dev/null; then
    echo "❌ ffmpeg not found. Installing..."
    sudo dnf install -y ffmpeg
fi

echo "✅ Starting video generation..."

for i in $(seq 1 $VIDEOS_COUNT); do
    VIDEO_FILE="$OUTPUT_DIR/test-video-${i}.mp4"

    # Skip if already exists
    if [ -f "$VIDEO_FILE" ]; then
        echo "⏭️  Video $i already exists, skipping..."
        continue
    fi

    # Generate synthetic video with color bars and timer
    # Duration: ~5 minutes, bitrate adjusted for 100MB target
    ffmpeg -f lavfi -i testsrc=duration=300:size=1280x720:rate=30 \
           -f lavfi -i sine=frequency=1000:duration=300 \
           -c:v libx264 -preset ultrafast -b:v 2700k \
           -c:a aac -b:a 128k \
           -movflags +faststart \
           "$VIDEO_FILE" \
           -loglevel error -y

    ACTUAL_SIZE=$(du -m "$VIDEO_FILE" | cut -f1)
    echo "✅ Generated video $i/$VIDEOS_COUNT (${ACTUAL_SIZE}MB)"

    # Progress indicator every 50 videos
    if [ $((i % 50)) -eq 0 ]; then
        PROGRESS=$((i * 100 / VIDEOS_COUNT))
        echo "📊 Progress: ${PROGRESS}% (${i}/${VIDEOS_COUNT} videos)"
    fi
done

echo ""
echo "🎉 Video generation complete!"
echo "📁 Location: $OUTPUT_DIR"
echo "📦 Total videos: $(ls -1 "$OUTPUT_DIR"/*.mp4 | wc -l)"
echo "💾 Total size: $(du -sh "$OUTPUT_DIR" | cut -f1)"
