#!/bin/bash

# ===================================================================
# InsightLearn - Qdrant Vector Database Testing Script
# ===================================================================
# Purpose: Deploy Qdrant, index test videos, and verify search works
# Author: InsightLearn Development Team
# Version: 1.0.0
# Date: 2025-12-27
# ===================================================================

set -e  # Exit on error

NAMESPACE="insightlearn"
API_URL="http://localhost:31081"
QDRANT_URL="http://localhost:31333"

echo "====================================================================="
echo "🚀 InsightLearn Vector Database Testing"
echo "====================================================================="
echo ""

# ===================================================================
# STEP 1: Deploy Qdrant to K3s
# ===================================================================
echo "📦 Step 1: Deploying Qdrant to K3s..."
echo ""

kubectl apply -f /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/32-qdrant-pvc.yaml
kubectl apply -f /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/33-qdrant-deployment.yaml
kubectl apply -f /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/34-qdrant-service.yaml

echo "✅ Qdrant manifests applied"
echo ""

# ===================================================================
# STEP 2: Wait for Qdrant pod to be ready
# ===================================================================
echo "⏳ Step 2: Waiting for Qdrant pod to be ready..."
echo ""

kubectl wait --for=condition=ready pod -l app=qdrant -n $NAMESPACE --timeout=180s

echo "✅ Qdrant pod is ready"
echo ""

# ===================================================================
# STEP 3: Verify Qdrant API is responding
# ===================================================================
echo "🔍 Step 3: Verifying Qdrant API..."
echo ""

sleep 5  # Give Qdrant a moment to fully initialize

QDRANT_HEALTH=$(curl -s "${QDRANT_URL}/" -w "%{http_code}" -o /dev/null)

if [ "$QDRANT_HEALTH" = "200" ]; then
    echo "✅ Qdrant API is responding (HTTP 200)"
else
    echo "❌ Qdrant API returned HTTP $QDRANT_HEALTH"
    exit 1
fi

echo ""

# ===================================================================
# STEP 4: Index 10 test videos with sample embeddings
# ===================================================================
echo "📝 Step 4: Indexing 10 test videos..."
echo ""

# Generate random 384-dimensional normalized embeddings
generate_embedding() {
    python3 -c "
import random
import math
import json

# Generate random vector
embedding = [random.random() for _ in range(384)]

# Normalize to unit vector
magnitude = math.sqrt(sum(x*x for x in embedding))
embedding = [x/magnitude for x in embedding]

print(json.dumps(embedding))
"
}

# Test video data
declare -a videos=(
    '{"id":"11111111-1111-1111-1111-111111111111","title":"Introduction to Python","description":"Learn Python basics from scratch"}'
    '{"id":"22222222-2222-2222-2222-222222222222","title":"Advanced Python","description":"Master advanced Python concepts"}'
    '{"id":"33333333-3333-3333-3333-333333333333","title":"JavaScript Fundamentals","description":"Complete JavaScript course"}'
    '{"id":"44444444-4444-4444-4444-444444444444","title":"React for Beginners","description":"Build modern web apps with React"}'
    '{"id":"55555555-5555-5555-5555-555555555555","title":"Node.js Backend","description":"Server-side JavaScript with Node"}'
    '{"id":"66666666-6666-6666-6666-666666666666","title":"SQL Database Design","description":"Relational database fundamentals"}'
    '{"id":"77777777-7777-7777-7777-777777777777","title":"NoSQL with MongoDB","description":"Document databases explained"}'
    '{"id":"88888888-8888-8888-8888-888888888888","title":"Docker Containers","description":"Containerize your applications"}'
    '{"id":"99999999-9999-9999-9999-999999999999","title":"Kubernetes Basics","description":"Orchestrate containers at scale"}'
    '{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","title":"CI/CD Pipeline","description":"Automate your deployment workflow"}'
)

SUCCESS_COUNT=0
FAIL_COUNT=0

for video_json in "${videos[@]}"; do
    VIDEO_ID=$(echo "$video_json" | jq -r '.id')
    TITLE=$(echo "$video_json" | jq -r '.title')
    DESCRIPTION=$(echo "$video_json" | jq -r '.description')

    echo "   Indexing: $TITLE..."

    # Generate random embedding
    EMBEDDING=$(generate_embedding)

    # Create request payload
    REQUEST_PAYLOAD=$(jq -n \
        --arg vid "$VIDEO_ID" \
        --arg title "$TITLE" \
        --arg desc "$DESCRIPTION" \
        --argjson embed "$EMBEDDING" \
        '{
            videoId: $vid,
            title: $title,
            description: $desc,
            embedding: $embed
        }'
    )

    # Send to API
    RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "${API_URL}/api/vector/index-video" \
        -H "Content-Type: application/json" \
        -d "$REQUEST_PAYLOAD")

    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

    if [ "$HTTP_CODE" = "200" ]; then
        ((SUCCESS_COUNT++))
        echo "   ✅ Success"
    else
        ((FAIL_COUNT++))
        echo "   ❌ Failed (HTTP $HTTP_CODE)"
    fi
done

echo ""
echo "📊 Indexing Results: $SUCCESS_COUNT success, $FAIL_COUNT failed"
echo ""

if [ $SUCCESS_COUNT -eq 0 ]; then
    echo "❌ No videos were indexed. Check API logs:"
    echo "   kubectl logs -n $NAMESPACE -l app=insightlearn-api --tail=50"
    exit 1
fi

# ===================================================================
# STEP 5: Run similarity search queries
# ===================================================================
echo "🔎 Step 5: Running similarity search queries..."
echo ""

declare -a queries=(
    "Python programming"
    "JavaScript web development"
    "Database design"
    "Container orchestration"
)

for query in "${queries[@]}"; do
    echo "   Query: \"$query\""

    ENCODED_QUERY=$(python3 -c "import urllib.parse; print(urllib.parse.quote('$query'))")

    SEARCH_RESPONSE=$(curl -s "${API_URL}/api/vector/search?query=${ENCODED_QUERY}&limit=3")

    RESULT_COUNT=$(echo "$SEARCH_RESPONSE" | jq -r '.count // 0')

    echo "   Results: $RESULT_COUNT videos found"

    if [ "$RESULT_COUNT" -gt 0 ]; then
        echo "$SEARCH_RESPONSE" | jq -r '.results[] | "     - \(.title) (score: \(.similarityScore))"'
    fi

    echo ""
done

# ===================================================================
# STEP 6: Verify collection statistics
# ===================================================================
echo "📈 Step 6: Verifying collection statistics..."
echo ""

STATS_RESPONSE=$(curl -s "${API_URL}/api/vector/stats")

TOTAL_VECTORS=$(echo "$STATS_RESPONSE" | jq -r '.totalVectors // 0')
DIMENSIONS=$(echo "$STATS_RESPONSE" | jq -r '.vectorDimensions // 0')
IS_READY=$(echo "$STATS_RESPONSE" | jq -r '.isReady // false')

echo "   Collection: $(echo "$STATS_RESPONSE" | jq -r '.collectionName')"
echo "   Total Vectors: $TOTAL_VECTORS"
echo "   Vector Dimensions: $DIMENSIONS"
echo "   Status: $([ "$IS_READY" = "true" ] && echo "✅ Ready" || echo "❌ Not Ready")"

echo ""

# ===================================================================
# STEP 7: Cleanup Test (Optional)
# ===================================================================
echo "🧹 Step 7: Cleanup test data (optional)..."
echo ""

read -p "Do you want to delete test videos from index? (y/N) " -n 1 -r
echo

if [[ $REPLY =~ ^[Yy]$ ]]; then
    DELETE_COUNT=0

    for video_json in "${videos[@]}"; do
        VIDEO_ID=$(echo "$video_json" | jq -r '.id')

        DELETE_RESPONSE=$(curl -s -w "\n%{http_code}" -X DELETE "${API_URL}/api/vector/videos/${VIDEO_ID}")
        HTTP_CODE=$(echo "$DELETE_RESPONSE" | tail -n1)

        if [ "$HTTP_CODE" = "200" ]; then
            ((DELETE_COUNT++))
        fi
    done

    echo "   ✅ Deleted $DELETE_COUNT test videos"
else
    echo "   ℹ️  Test videos left in index for manual inspection"
fi

echo ""

# ===================================================================
# FINAL REPORT
# ===================================================================
echo "====================================================================="
echo "✅ Vector Database Testing Complete"
echo "====================================================================="
echo ""
echo "Summary:"
echo "  - Qdrant pod: Running"
echo "  - Videos indexed: $SUCCESS_COUNT"
echo "  - Search queries: 4 executed"
echo "  - Total vectors: $TOTAL_VECTORS"
echo ""
echo "Access Qdrant Dashboard:"
echo "  http://localhost:31333/dashboard"
echo ""
echo "API Endpoints:"
echo "  POST   ${API_URL}/api/vector/index-video"
echo "  GET    ${API_URL}/api/vector/search?query=...&limit=10"
echo "  DELETE ${API_URL}/api/vector/videos/{videoId}"
echo "  GET    ${API_URL}/api/vector/stats"
echo ""
echo "====================================================================="
