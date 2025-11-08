#!/bin/bash
# Script to create Jenkins job via Jenkins CLI or REST API

set -e

JENKINS_URL="${JENKINS_URL:-http://localhost:8080}"
JOB_NAME="insightlearn-automated-tests"
JOB_CONFIG="jenkins/config/job-config.xml"

echo "========================================="
echo "Creating Jenkins Job"
echo "========================================="
echo "Jenkins URL: $JENKINS_URL"
echo "Job Name: $JOB_NAME"
echo "========================================="

# Check if Jenkins is accessible
if ! curl -s -f "$JENKINS_URL" > /dev/null; then
    echo "❌ Error: Jenkins is not accessible at $JENKINS_URL"
    echo "Please start Jenkins and try again."
    exit 1
fi

echo "✅ Jenkins is accessible"

# Method 1: Using Jenkins CLI (if available)
if command -v jenkins-cli &> /dev/null; then
    echo "Using Jenkins CLI..."
    jenkins-cli -s "$JENKINS_URL" create-job "$JOB_NAME" < "$JOB_CONFIG"
    echo "✅ Job created successfully via CLI"
    exit 0
fi

# Method 2: Using curl and REST API
echo "Using REST API..."

# Check if job already exists
if curl -s -f "$JENKINS_URL/job/$JOB_NAME/config.xml" > /dev/null 2>&1; then
    echo "⚠️  Job '$JOB_NAME' already exists"
    read -p "Do you want to update it? (yes/no): " confirm
    if [ "$confirm" != "yes" ]; then
        echo "Cancelled"
        exit 0
    fi

    # Update existing job
    curl -X POST "$JENKINS_URL/job/$JOB_NAME/config.xml" \
        --data-binary "@$JOB_CONFIG" \
        -H "Content-Type: application/xml"

    echo "✅ Job updated successfully"
else
    # Create new job
    curl -X POST "$JENKINS_URL/createItem?name=$JOB_NAME" \
        --data-binary "@$JOB_CONFIG" \
        -H "Content-Type: application/xml"

    echo "✅ Job created successfully"
fi

echo ""
echo "========================================="
echo "Job Configuration Complete!"
echo "========================================="
echo "Access job at: $JENKINS_URL/job/$JOB_NAME"
echo ""
echo "Next steps:"
echo "1. Open Jenkins: $JENKINS_URL"
echo "2. Configure credentials if needed"
echo "3. Click 'Build Now' to run first test"
echo "========================================="
