#!/bin/bash
# ================================================================
# IndexNow Submission Script for InsightLearn
# ================================================================
# This script submits URLs to IndexNow API for instant indexing
# by Bing, Yandex, Seznam, and Naver search engines.
#
# Usage: ./submit-indexnow.sh [--all | --sitemap | --custom <url>]
#
# Options:
#   --all      Submit all known SEO-optimized pages
#   --sitemap  Parse sitemap.xml and submit all URLs
#   --custom   Submit a single custom URL
#
# IndexNow Key: ebd57a262cfe8ff8de852eba65288c19
# ================================================================

set -e

# Configuration
HOST="www.insightlearn.cloud"
KEY="ebd57a262cfe8ff8de852eba65288c19"
KEY_LOCATION="https://${HOST}/${KEY}.txt"
INDEXNOW_ENDPOINT="https://api.indexnow.org/indexnow"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Main SEO-optimized URLs
MAIN_URLS=(
    "https://www.insightlearn.cloud/"
    "https://www.insightlearn.cloud/courses"
    "https://www.insightlearn.cloud/categories"
    "https://www.insightlearn.cloud/about"
    "https://www.insightlearn.cloud/faq"
    "https://www.insightlearn.cloud/contact"
    "https://www.insightlearn.cloud/blog"
    "https://www.insightlearn.cloud/search"
    "https://www.insightlearn.cloud/instructors"
    "https://www.insightlearn.cloud/pricing"
    "https://www.insightlearn.cloud/privacy-policy"
    "https://www.insightlearn.cloud/terms-of-service"
)

# Category landing pages
CATEGORY_URLS=(
    "https://www.insightlearn.cloud/courses?category=web-development"
    "https://www.insightlearn.cloud/courses?category=data-science"
    "https://www.insightlearn.cloud/courses?category=cloud-computing"
    "https://www.insightlearn.cloud/courses?category=mobile-development"
    "https://www.insightlearn.cloud/courses?category=cybersecurity"
    "https://www.insightlearn.cloud/courses?category=ui-ux-design"
    "https://www.insightlearn.cloud/courses?category=devops"
    "https://www.insightlearn.cloud/courses?category=programming"
)

# Skill landing pages
SKILL_URLS=(
    "https://www.insightlearn.cloud/courses?skill=python"
    "https://www.insightlearn.cloud/courses?skill=javascript"
    "https://www.insightlearn.cloud/courses?skill=react"
    "https://www.insightlearn.cloud/courses?skill=aws"
    "https://www.insightlearn.cloud/courses?skill=docker"
    "https://www.insightlearn.cloud/courses?skill=kubernetes"
)

print_header() {
    echo -e "${BLUE}================================================================${NC}"
    echo -e "${BLUE}  IndexNow Submission Script for InsightLearn${NC}"
    echo -e "${BLUE}================================================================${NC}"
    echo -e "Host: ${GREEN}${HOST}${NC}"
    echo -e "Key:  ${GREEN}${KEY}${NC}"
    echo ""
}

submit_urls() {
    local urls=("$@")
    local url_count=${#urls[@]}

    echo -e "${YELLOW}Submitting ${url_count} URLs to IndexNow...${NC}"
    echo ""

    # Build URL list JSON
    local url_list=""
    for url in "${urls[@]}"; do
        if [ -n "$url_list" ]; then
            url_list+=","
        fi
        url_list+="\"${url}\""
    done

    # Create JSON payload
    local payload=$(cat <<EOF
{
    "host": "${HOST}",
    "key": "${KEY}",
    "keyLocation": "${KEY_LOCATION}",
    "urlList": [${url_list}]
}
EOF
)

    echo -e "${BLUE}Payload:${NC}"
    echo "$payload" | head -20
    if [ $url_count -gt 10 ]; then
        echo "... (${url_count} URLs total)"
    fi
    echo ""

    # Submit to IndexNow API
    echo -e "${YELLOW}Sending request to IndexNow API...${NC}"

    response=$(curl -s -w "\n%{http_code}" -X POST "$INDEXNOW_ENDPOINT" \
        -H "Content-Type: application/json; charset=utf-8" \
        -d "$payload")

    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')

    echo ""
    case $http_code in
        200)
            echo -e "${GREEN}SUCCESS!${NC} URLs submitted successfully (HTTP 200)"
            echo -e "Response: OK - URLs accepted for indexing"
            ;;
        202)
            echo -e "${GREEN}SUCCESS!${NC} URLs accepted for processing (HTTP 202)"
            echo -e "Response: Accepted - URLs will be processed"
            ;;
        400)
            echo -e "${RED}ERROR!${NC} Bad request (HTTP 400)"
            echo -e "Response: $body"
            echo -e "Check: Invalid URL format or missing required fields"
            ;;
        403)
            echo -e "${RED}ERROR!${NC} Forbidden (HTTP 403)"
            echo -e "Response: $body"
            echo -e "Check: Key doesn't exist or doesn't match key location"
            ;;
        422)
            echo -e "${YELLOW}WARNING!${NC} Unprocessable (HTTP 422)"
            echo -e "Response: $body"
            echo -e "Note: URLs may not match the host or be invalid"
            ;;
        429)
            echo -e "${YELLOW}WARNING!${NC} Too many requests (HTTP 429)"
            echo -e "Wait and try again later"
            ;;
        *)
            echo -e "${YELLOW}Response:${NC} HTTP $http_code"
            if [ -n "$body" ]; then
                echo -e "Body: $body"
            fi
            ;;
    esac

    echo ""
    echo -e "${BLUE}================================================================${NC}"
    echo -e "Submitted ${GREEN}${url_count}${NC} URLs to IndexNow"
    echo -e "Search engines notified: Bing, Yandex, Seznam, Naver"
    echo -e "${BLUE}================================================================${NC}"
}

submit_from_sitemap() {
    local sitemap_url="https://${HOST}/sitemap.xml"

    echo -e "${YELLOW}Fetching sitemap from ${sitemap_url}...${NC}"

    # Fetch sitemap and extract URLs
    local sitemap_content=$(curl -s "$sitemap_url")

    if [ -z "$sitemap_content" ]; then
        echo -e "${RED}ERROR: Could not fetch sitemap${NC}"
        exit 1
    fi

    # Extract URLs from sitemap using grep and sed
    local urls=($(echo "$sitemap_content" | grep -oP '(?<=<loc>)[^<]+' | head -100))

    if [ ${#urls[@]} -eq 0 ]; then
        echo -e "${RED}ERROR: No URLs found in sitemap${NC}"
        exit 1
    fi

    echo -e "${GREEN}Found ${#urls[@]} URLs in sitemap${NC}"
    echo ""

    submit_urls "${urls[@]}"
}

show_usage() {
    echo "Usage: $0 [--all | --sitemap | --custom <url>]"
    echo ""
    echo "Options:"
    echo "  --all      Submit all known SEO-optimized pages (${#MAIN_URLS[@]} + ${#CATEGORY_URLS[@]} + ${#SKILL_URLS[@]} URLs)"
    echo "  --sitemap  Parse sitemap.xml and submit all URLs"
    echo "  --custom   Submit a single custom URL"
    echo "  --main     Submit only main pages (${#MAIN_URLS[@]} URLs)"
    echo "  --help     Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 --all"
    echo "  $0 --sitemap"
    echo "  $0 --custom https://www.insightlearn.cloud/new-page"
    echo ""
}

# Main script
print_header

case "${1:-}" in
    --all)
        all_urls=("${MAIN_URLS[@]}" "${CATEGORY_URLS[@]}" "${SKILL_URLS[@]}")
        submit_urls "${all_urls[@]}"
        ;;
    --sitemap)
        submit_from_sitemap
        ;;
    --custom)
        if [ -z "${2:-}" ]; then
            echo -e "${RED}ERROR: Please provide a URL${NC}"
            show_usage
            exit 1
        fi
        submit_urls "$2"
        ;;
    --main)
        submit_urls "${MAIN_URLS[@]}"
        ;;
    --help|-h|"")
        show_usage
        ;;
    *)
        echo -e "${RED}Unknown option: $1${NC}"
        show_usage
        exit 1
        ;;
esac
