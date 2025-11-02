#!/bin/bash
# Script to build Docker images for InsightLearn on Debian/Linux
# Supports automatic versioning from Directory.Build.props and git

set -e

echo "Building InsightLearn Docker images for Kubernetes..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker first."
    exit 1
fi

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

echo "Project root: $PROJECT_ROOT"
echo ""

# ========================================
# Version Detection
# ========================================

# Function to extract version from Directory.Build.props
get_version_from_props() {
    if [ -f "Directory.Build.props" ]; then
        VERSION_PREFIX=$(grep '<VersionPrefix>' Directory.Build.props | sed 's/.*<VersionPrefix>\(.*\)<\/VersionPrefix>.*/\1/' || echo "1.0.0")
        VERSION_SUFFIX=$(grep '<VersionSuffix' Directory.Build.props | sed 's/.*<VersionSuffix[^>]*>\(.*\)<\/VersionSuffix>.*/\1/' || echo "")

        if [ -n "$VERSION_SUFFIX" ]; then
            echo "${VERSION_PREFIX}-${VERSION_SUFFIX}"
        else
            echo "${VERSION_PREFIX}"
        fi
    else
        echo "1.0.0"
    fi
}

# Get version from props file
VERSION=$(get_version_from_props)

# Get git information if available
if git rev-parse --git-dir > /dev/null 2>&1; then
    GIT_COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "nogit")
    GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
    BUILD_NUMBER=$(git rev-list --count HEAD 2>/dev/null || echo "0")

    # Check if working directory is clean
    if [ -n "$(git status --porcelain 2>/dev/null)" ]; then
        GIT_DIRTY="-dirty"
    else
        GIT_DIRTY=""
    fi
else
    GIT_COMMIT="nogit"
    GIT_BRANCH="unknown"
    BUILD_NUMBER="0"
    GIT_DIRTY=""
fi

# Build full version string
FULL_VERSION="${VERSION}"
VERSION_TAG="v${VERSION}"
BUILD_METADATA="${GIT_COMMIT}${GIT_DIRTY}"

# Create version tags
DOCKER_TAGS=(
    "latest"
    "${VERSION_TAG}"
    "${VERSION}"
    "${VERSION}-build.${BUILD_NUMBER}"
)

# Add git commit tag if available
if [ "$GIT_COMMIT" != "nogit" ]; then
    DOCKER_TAGS+=("${VERSION}-${GIT_COMMIT}")
fi

# Display version information
echo "========================================="
echo "Build Version Information"
echo "========================================="
echo "Version:        ${VERSION}"
echo "Git Commit:     ${GIT_COMMIT}"
echo "Git Branch:     ${GIT_BRANCH}"
echo "Build Number:   ${BUILD_NUMBER}"
echo "Full Version:   ${FULL_VERSION}+${BUILD_METADATA}"
echo "========================================="
echo ""

# ========================================
# Build Docker Images
# ========================================

# Function to build image with multiple tags
build_image_with_tags() {
    local dockerfile=$1
    local image_name=$2
    local tag_args=""

    # Build tag arguments
    for tag in "${DOCKER_TAGS[@]}"; do
        tag_args="$tag_args -t ${image_name}:${tag}"
    done

    echo "Building ${image_name} with tags: ${DOCKER_TAGS[*]}"

    # Build with version information
    docker build \
        -f "$dockerfile" \
        --build-arg VERSION="${VERSION}" \
        --build-arg GIT_COMMIT="${GIT_COMMIT}" \
        --build-arg BUILD_NUMBER="${BUILD_NUMBER}" \
        --build-arg BUILD_DATE="$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
        ${tag_args} \
        .
}

# Build API image
echo ""
echo "=== Building API image ==="
build_image_with_tags "Dockerfile" "insightlearn/api"

# Build Web image
echo ""
echo "=== Building Web image ==="
build_image_with_tags "Dockerfile.web" "insightlearn/web"

echo ""
echo "=== Build completed successfully ==="
echo ""
echo "Images created:"
docker images | grep insightlearn | head -20

echo ""
echo "========================================="
echo "To load images into minikube:"
echo "========================================="
echo "  minikube image load insightlearn/api:${VERSION_TAG}"
echo "  minikube image load insightlearn/web:${VERSION_TAG}"
echo ""
echo "Or use 'latest' tag:"
echo "  minikube image load insightlearn/api:latest"
echo "  minikube image load insightlearn/web:latest"
echo ""
echo "To deploy with specific version:"
echo "  export IMAGE_VERSION=${VERSION_TAG}"
echo "  ./k8s/deploy.sh"
echo "========================================="
