#!/bin/bash
#
# Create Jenkins Jobs for InsightLearn Automated Testing
#
# This script creates the Jenkins Pipeline jobs from XML definitions
# stored in jenkins/jobs/*.xml
#
# Prerequisites:
#   - Jenkins running on K3s (accessible at localhost:32000)
#   - Required plugins installed: workflow-job, workflow-cps, git
#
# Usage:
#   ./jenkins/create-jenkins-jobs.sh
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

JENKINS_URL="${JENKINS_URL:-http://localhost:32000}"

echo -e "${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║      InsightLearn - Create Jenkins Automated Test Jobs       ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"

# Check if Jenkins is accessible
echo -e "${BLUE}🔍 Checking Jenkins availability...${NC}"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$JENKINS_URL/api/json" 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Jenkins is accessible at $JENKINS_URL${NC}"
else
    echo -e "${RED}❌ Jenkins is not accessible at $JENKINS_URL (HTTP $HTTP_CODE)${NC}"
    echo -e "${YELLOW}Make sure Jenkins is running:${NC}"
    echo -e "  ${GREEN}kubectl get pods -n jenkins${NC}"
    exit 1
fi

# Check if required plugins are installed
echo -e "\n${BLUE}🔌 Checking required plugins...${NC}"
PLUGINS=$(curl -s "$JENKINS_URL/pluginManager/api/json?depth=1" | jq -r '.plugins[].shortName' 2>/dev/null || echo "")
REQUIRED_PLUGINS=("workflow-job" "workflow-cps" "git")
MISSING_PLUGINS=()

for plugin in "${REQUIRED_PLUGINS[@]}"; do
    if echo "$PLUGINS" | grep -q "^$plugin$"; then
        echo -e "  ${GREEN}✓${NC} $plugin"
    else
        echo -e "  ${RED}✗${NC} $plugin (missing)"
        MISSING_PLUGINS+=("$plugin")
    fi
done

if [ ${#MISSING_PLUGINS[@]} -gt 0 ]; then
    echo -e "\n${YELLOW}⚠️  Missing plugins detected. Installing...${NC}"
    JENKINS_POD=$(kubectl get pod -n jenkins -l app=jenkins -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
    if [ -n "$JENKINS_POD" ]; then
        # Copy plugins from ref to jenkins_home (Jenkins Alpine fix)
        kubectl exec -n jenkins "$JENKINS_POD" -- sh -c "cp -r /usr/share/jenkins/ref/plugins/* /var/jenkins_home/plugins/ 2>/dev/null" || true
        echo -e "${YELLOW}Plugins copied. Restarting Jenkins...${NC}"
        kubectl rollout restart deployment/jenkins -n jenkins
        kubectl rollout status deployment/jenkins -n jenkins --timeout=180s
        sleep 30
    fi
fi

# Setup cookie jar for session management
COOKIE_JAR=$(mktemp)

# Get CSRF crumb token
echo -e "\n${BLUE}🔑 Getting CSRF crumb token...${NC}"
curl -s -c "$COOKIE_JAR" "$JENKINS_URL/crumbIssuer/api/json" > /tmp/crumb.json 2>/dev/null
CRUMB=$(cat /tmp/crumb.json | jq -r '.crumb' 2>/dev/null)
CRUMB_HEADER=$(cat /tmp/crumb.json | jq -r '.crumbRequestField' 2>/dev/null)

if [ -n "$CRUMB" ] && [ "$CRUMB" != "null" ]; then
    echo -e "${GREEN}✅ CSRF crumb obtained${NC}"
else
    echo -e "${YELLOW}⚠️  CSRF protection may be disabled${NC}"
    CRUMB=""
    CRUMB_HEADER=""
fi

# Function to create or update a job
create_job() {
    local job_name=$1
    local job_file=$2

    echo -e "\n${BLUE}📝 Creating job: ${job_name}${NC}"

    if [ ! -f "$job_file" ]; then
        echo -e "${RED}❌ Job file not found: $job_file${NC}"
        return 1
    fi

    # Try to create the job
    HTTP_CODE=$(curl -s -o /tmp/jenkins-response.txt -w "%{http_code}" \
        -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
        -X POST \
        -H "$CRUMB_HEADER: $CRUMB" \
        -H "Content-Type: application/xml" \
        --data-binary "@$job_file" \
        "$JENKINS_URL/createItem?name=$job_name")

    if [ "$HTTP_CODE" = "200" ]; then
        echo -e "${GREEN}✅ Job '$job_name' created successfully${NC}"
        return 0
    elif [ "$HTTP_CODE" = "400" ]; then
        echo -e "${YELLOW}⚠️  Job '$job_name' already exists, updating...${NC}"

        # Update existing job
        HTTP_CODE=$(curl -s -o /tmp/jenkins-response.txt -w "%{http_code}" \
            -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
            -X POST \
            -H "$CRUMB_HEADER: $CRUMB" \
            -H "Content-Type: application/xml" \
            --data-binary "@$job_file" \
            "$JENKINS_URL/job/$job_name/config.xml")

        if [ "$HTTP_CODE" = "200" ]; then
            echo -e "${GREEN}✅ Job '$job_name' updated successfully${NC}"
            return 0
        else
            echo -e "${RED}❌ Failed to update job '$job_name' (HTTP $HTTP_CODE)${NC}"
            cat /tmp/jenkins-response.txt | head -10
            return 1
        fi
    else
        echo -e "${RED}❌ Failed to create job '$job_name' (HTTP $HTTP_CODE)${NC}"
        cat /tmp/jenkins-response.txt | head -10
        return 1
    fi
}

# Create jobs from jenkins/jobs/*.xml files
echo -e "\n${BLUE}Creating Jenkins jobs from XML files...${NC}"

cd "$PROJECT_ROOT"

# Job 1: insightlearn-automated-tests (hourly)
create_job "insightlearn-automated-tests" "jenkins/jobs/insightlearn-automated-tests.xml"

# Job 2: weekly-heavy-load-test (Sundays 2 AM)
create_job "weekly-heavy-load-test" "jenkins/jobs/weekly-heavy-load-test.xml"

# Cleanup
rm -f "$COOKIE_JAR" /tmp/crumb.json /tmp/jenkins-response.txt

# List all jobs
echo -e "\n${BLUE}📋 Current Jenkins jobs:${NC}"
curl -s "$JENKINS_URL/api/json" | jq -r '.jobs[].name' 2>/dev/null | while read job; do
    echo -e "  ${GREEN}•${NC} $job"
done

echo -e "\n${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                   Jobs Created Successfully!                  ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${GREEN}Jenkins Jobs:${NC}"
echo -e "  1. ${BLUE}insightlearn-automated-tests${NC} - Runs every hour (H * * * *)"
echo -e "     Uses: Jenkinsfile (9 testing stages)"
echo -e "  2. ${BLUE}weekly-heavy-load-test${NC} - Runs every Sunday at 2:00 AM"
echo -e "     Uses: jenkins/pipelines/weekly-heavy-load-test.Jenkinsfile"

echo -e "\n${YELLOW}Access Jenkins:${NC} ${BLUE}$JENKINS_URL${NC}"
echo -e "\n${GREEN}To trigger a build manually:${NC}"
echo -e "  curl -X POST '$JENKINS_URL/job/insightlearn-automated-tests/build'"

echo -e "\n${GREEN}Done! 🚀${NC}\n"
