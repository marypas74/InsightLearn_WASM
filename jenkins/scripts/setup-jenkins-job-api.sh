#!/bin/bash
# Create Jenkins job via REST API with CSRF protection

set -e

JENKINS_URL="http://localhost:32000"
JOB_NAME="insightlearn-automated-tests"
JOB_CONFIG="jenkins/config/job-config.xml"

echo "========================================="
echo "Creating Jenkins Job via REST API"
echo "========================================="
echo "Jenkins URL: $JENKINS_URL"
echo "Job Name: $JOB_NAME"
echo "========================================="

# Check if job already exists
if curl -sS -f "$JENKINS_URL/job/$JOB_NAME/api/json" > /dev/null 2>&1; then
    echo "✅ Job already exists!"
    echo "Job URL: $JENKINS_URL/job/$JOB_NAME"
    exit 0
fi

# Try to get CSRF crumb
echo "Attempting to get CSRF crumb..."
CRUMB=$(curl -sS "$JENKINS_URL/crumbIssuer/api/json" 2>/dev/null | grep -oP '(?<="crumb":")[^"]*' || echo "")

if [ -n "$CRUMB" ]; then
    echo "✅ Got CSRF crumb"
    echo "Creating job with crumb protection..."

    RESULT=$(curl -sS -w "\nHTTP_CODE:%{http_code}" -X POST \
        "$JENKINS_URL/createItem?name=$JOB_NAME" \
        --data-binary "@$JOB_CONFIG" \
        -H "Content-Type: application/xml" \
        -H "Jenkins-Crumb: $CRUMB" \
        2>&1)

    HTTP_CODE=$(echo "$RESULT" | grep "HTTP_CODE" | cut -d: -f2)

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "302" ]; then
        echo "✅ Job created successfully!"
    else
        echo "❌ Failed to create job. HTTP Code: $HTTP_CODE"
        echo "$RESULT"
        exit 1
    fi
else
    echo "⚠️  No CSRF protection or crumb not available"
    echo "Trying without crumb..."

    RESULT=$(curl -sS -w "\nHTTP_CODE:%{http_code}" -X POST \
        "$JENKINS_URL/createItem?name=$JOB_NAME" \
        --data-binary "@$JOB_CONFIG" \
        -H "Content-Type: application/xml" \
        2>&1)

    HTTP_CODE=$(echo "$RESULT" | grep "HTTP_CODE" | cut -d: -f2)

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "302" ]; then
        echo "✅ Job created successfully!"
    else
        echo "❌ Failed to create job. HTTP Code: $HTTP_CODE"
        echo ""
        echo "Manual setup required:"
        echo "1. Open $JENKINS_URL"
        echo "2. Click 'New Item'"
        echo "3. Name: $JOB_NAME"
        echo "4. Type: Pipeline"
        echo "5. Use Jenkinsfile from repository"
        exit 1
    fi
fi

echo ""
echo "========================================="
echo "✅ Job Configuration Complete!"
echo "========================================="
echo "Job URL: $JENKINS_URL/job/$JOB_NAME"
echo ""
echo "Next step: Trigger build"
echo "  curl -X POST $JENKINS_URL/job/$JOB_NAME/build"
echo "========================================="
