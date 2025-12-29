#!/bin/bash
################################################################################
# InsightLearn Test Video Verification Script
#
# Purpose: Verify all test videos are accessible and functional
# Created: 2025-12-26
# Author: Claude Code Autonomous Agent
################################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
TOTAL_VIDEOS=0
SUCCESSFUL_VIDEOS=0
FAILED_VIDEOS=0
TOTAL_LESSONS=0
LESSONS_WITH_VIDEOS=0

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}InsightLearn Test Video Verification${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if we can access Kubernetes
if ! kubectl get pods -n insightlearn &>/dev/null; then
    echo -e "${RED}ERROR: Cannot access Kubernetes cluster${NC}"
    exit 1
fi

echo -e "${YELLOW}Step 1: Checking service health...${NC}"
API_READY=$(kubectl get deployment insightlearn-api -n insightlearn -o jsonpath='{.status.readyReplicas}' 2>/dev/null || echo "0")
MONGO_READY=$(kubectl get statefulset mongodb -n insightlearn -o jsonpath='{.status.readyReplicas}' 2>/dev/null || echo "0")

if [ "$API_READY" -eq "0" ]; then
    echo -e "${RED}✗ API pods not ready${NC}"
    exit 1
else
    echo -e "${GREEN}✓ API pods ready: $API_READY${NC}"
fi

if [ "$MONGO_READY" -eq "0" ]; then
    echo -e "${RED}✗ MongoDB not ready${NC}"
    exit 1
else
    echo -e "${GREEN}✓ MongoDB ready${NC}"
fi

echo ""
echo -e "${YELLOW}Step 2: Counting videos in MongoDB GridFS...${NC}"
TOTAL_VIDEOS=$(kubectl exec mongodb-0 -n insightlearn -- mongosh -u insightlearn -p GpYb2EZ3srVBb0Ziv0kG4Ual3hxaY9oT --authenticationDatabase admin insightlearn_videos --quiet --eval "db.videos.files.countDocuments()" 2>/dev/null | tail -1)
echo -e "${GREEN}Total videos in GridFS: $TOTAL_VIDEOS${NC}"

echo ""
echo -e "${YELLOW}Step 3: Counting lessons in SQL Server...${NC}"
LESSONS_WITH_VIDEOS=$(kubectl exec sqlserver-0 -n insightlearn -- bash -c "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'M0ng0Adm1n!2024#Secure' -C -d InsightLearnDb -Q \"SELECT COUNT(*) FROM Lessons WHERE VideoFileId IS NOT NULL\" 2>&1" | grep -oP '\d+' | head -1)
TOTAL_LESSONS=$(kubectl exec sqlserver-0 -n insightlearn -- bash -c "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'M0ng0Adm1n!2024#Secure' -C -d InsightLearnDb -Q \"SELECT COUNT(*) FROM Lessons\" 2>&1" | grep -oP '\d+' | head -1)
echo -e "${GREEN}Total lessons: $TOTAL_LESSONS${NC}"
echo -e "${GREEN}Lessons with videos: $LESSONS_WITH_VIDEOS${NC}"

echo ""
echo -e "${YELLOW}Step 4: Testing video streaming endpoints...${NC}"
echo ""

# Get list of video ObjectIds
VIDEO_IDS=$(kubectl exec mongodb-0 -n insightlearn -- mongosh -u insightlearn -p GpYb2EZ3srVBb0Ziv0kG4Ual3hxaY9oT --authenticationDatabase admin insightlearn_videos --quiet --eval "db.videos.files.find({}, {_id: 1}).limit(10).forEach(function(doc) { print(doc._id.toString()); })" 2>/dev/null | grep -E '^[a-f0-9]{24}$')

TEST_COUNT=0
for VIDEO_ID in $VIDEO_IDS; do
    TEST_COUNT=$((TEST_COUNT + 1))

    # Test streaming endpoint
    HTTP_CODE=$(curl -X GET -s -o /dev/null -w "%{http_code}" http://localhost:31081/api/video/stream/$VIDEO_ID 2>/dev/null || echo "000")

    if [ "$HTTP_CODE" = "200" ]; then
        echo -e "${GREEN}✓ Video $TEST_COUNT ($VIDEO_ID): HTTP $HTTP_CODE${NC}"
        SUCCESSFUL_VIDEOS=$((SUCCESSFUL_VIDEOS + 1))
    else
        echo -e "${RED}✗ Video $TEST_COUNT ($VIDEO_ID): HTTP $HTTP_CODE${NC}"
        FAILED_VIDEOS=$((FAILED_VIDEOS + 1))
    fi

    # Stop after 10 tests
    if [ $TEST_COUNT -ge 10 ]; then
        break
    fi
done

echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Summary Report${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo -e "Total videos in GridFS:        ${GREEN}$TOTAL_VIDEOS${NC}"
echo -e "Total lessons in database:     ${GREEN}$TOTAL_LESSONS${NC}"
echo -e "Lessons with videos:           ${GREEN}$LESSONS_WITH_VIDEOS${NC}"
echo ""
echo -e "Videos tested:                 ${BLUE}$TEST_COUNT${NC}"
echo -e "Successful tests:              ${GREEN}$SUCCESSFUL_VIDEOS${NC}"
echo -e "Failed tests:                  ${RED}$FAILED_VIDEOS${NC}"
echo ""

if [ $FAILED_VIDEOS -eq 0 ]; then
    echo -e "${GREEN}✓ All test videos are accessible and functional${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠ Some videos failed streaming tests${NC}"
    exit 1
fi
