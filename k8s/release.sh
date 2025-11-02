#!/bin/bash
# Release workflow script for InsightLearn
# This script automates the complete release process:
# 1. Version bump
# 2. Build Docker images
# 3. Tag git commit
# 4. Deploy to Kubernetes (optional)

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

# Check if git repo
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    print_error "Not a git repository!"
    exit 1
fi

# Check for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    print_error "Working directory has uncommitted changes!"
    echo ""
    git status --short
    echo ""
    read -p "Do you want to continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Function to show usage
show_usage() {
    cat << EOF
Usage: $0 [VERSION_TYPE] [OPTIONS]

Version Types:
  patch               Bump patch version (bug fixes)
  minor               Bump minor version (new features)
  major               Bump major version (breaking changes)

Options:
  --skip-build        Skip Docker image build
  --skip-tag          Skip git tag creation
  --skip-deploy       Skip Kubernetes deployment
  --auto              Non-interactive mode (auto-confirm all)

Examples:
  $0 patch            # Release patch version (1.0.0 -> 1.0.1)
  $0 minor            # Release minor version (1.0.0 -> 1.1.0)
  $0 major            # Release major version (1.0.0 -> 2.0.0)
  $0 patch --skip-deploy  # Release without deploying

Workflow:
  1. Bump version in Directory.Build.props
  2. Build Docker images with new version
  3. Create git tag
  4. (Optional) Deploy to Kubernetes
  5. Show next steps

EOF
}

# Parse arguments
VERSION_TYPE="${1:-}"
SKIP_BUILD=false
SKIP_TAG=false
SKIP_DEPLOY=true  # Default: don't deploy automatically
AUTO_CONFIRM=false

shift || true
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-build) SKIP_BUILD=true ;;
        --skip-tag) SKIP_TAG=true ;;
        --skip-deploy) SKIP_DEPLOY=true ;;
        --deploy) SKIP_DEPLOY=false ;;
        --auto) AUTO_CONFIRM=true ;;
        *) print_error "Unknown option: $1"; show_usage; exit 1 ;;
    esac
    shift
done

# Validate version type
if [[ ! "$VERSION_TYPE" =~ ^(major|minor|patch)$ ]]; then
    print_error "Invalid or missing version type!"
    show_usage
    exit 1
fi

# Show current state
echo ""
echo "========================================="
echo "  InsightLearn Release Process"
echo "========================================="
echo ""

# Get current version
CURRENT_VERSION=$(grep -oP '(?<=<VersionPrefix>)[^<]+' "$PROJECT_ROOT/Directory.Build.props")
print_info "Current version: $CURRENT_VERSION"

# Confirm release
if [ "$AUTO_CONFIRM" = false ]; then
    echo ""
    read -p "Start $VERSION_TYPE release? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_warning "Release cancelled"
        exit 0
    fi
fi

# ========================================
# Step 1: Bump Version
# ========================================
echo ""
print_info "Step 1/4: Bumping $VERSION_TYPE version..."
"$SCRIPT_DIR/version.sh" bump "$VERSION_TYPE"

# Get new version
NEW_VERSION=$(grep -oP '(?<=<VersionPrefix>)[^<]+' "$PROJECT_ROOT/Directory.Build.props")
print_success "Version bumped: $CURRENT_VERSION -> $NEW_VERSION"

# ========================================
# Step 2: Build Docker Images
# ========================================
if [ "$SKIP_BUILD" = false ]; then
    echo ""
    print_info "Step 2/4: Building Docker images..."

    if "$SCRIPT_DIR/build-images.sh"; then
        print_success "Docker images built successfully"
    else
        print_error "Docker build failed!"
        exit 1
    fi
else
    print_warning "Step 2/4: Skipping Docker build"
fi

# ========================================
# Step 3: Create Git Tag
# ========================================
if [ "$SKIP_TAG" = false ]; then
    echo ""
    print_info "Step 3/4: Creating git tag..."

    # Commit version change if there are changes
    if [ -n "$(git status --porcelain Directory.Build.props)" ]; then
        git add Directory.Build.props
        git commit -m "Bump version to $NEW_VERSION"
        print_success "Committed version change"
    fi

    if "$SCRIPT_DIR/version.sh" tag; then
        print_success "Git tag v$NEW_VERSION created"
    else
        print_warning "Git tag creation skipped or failed"
    fi
else
    print_warning "Step 3/4: Skipping git tag creation"
fi

# ========================================
# Step 4: Deploy to Kubernetes (Optional)
# ========================================
if [ "$SKIP_DEPLOY" = false ]; then
    echo ""
    print_info "Step 4/4: Deploying to Kubernetes..."

    read -p "Deploy to Kubernetes now? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        export IMAGE_VERSION="v$NEW_VERSION"

        if "$SCRIPT_DIR/deploy.sh"; then
            print_success "Deployed to Kubernetes"
        else
            print_error "Deployment failed!"
            exit 1
        fi
    else
        print_warning "Deployment skipped"
    fi
else
    print_warning "Step 4/4: Skipping Kubernetes deployment"
fi

# ========================================
# Release Complete
# ========================================
echo ""
echo "========================================="
echo "  Release Complete!"
echo "========================================="
print_success "Released version: v$NEW_VERSION"
echo ""

# Show next steps
echo "Next Steps:"
echo ""
echo "1. Push commits and tags to remote:"
echo "   git push origin main"
echo "   git push origin v$NEW_VERSION"
echo ""

if [ "$SKIP_BUILD" = false ]; then
    echo "2. Load images into minikube (if not already done):"
    echo "   minikube image load insightlearn/api:v$NEW_VERSION"
    echo "   minikube image load insightlearn/web:v$NEW_VERSION"
    echo ""
fi

if [ "$SKIP_DEPLOY" = true ]; then
    echo "3. Deploy to Kubernetes:"
    echo "   export IMAGE_VERSION=v$NEW_VERSION"
    echo "   ./k8s/deploy.sh"
    echo ""
fi

echo "4. Verify deployment:"
echo "   ./k8s/status.sh"
echo "   kubectl get pods -n insightlearn"
echo ""

echo "5. Test application:"
echo "   https://192.168.1.103"
echo ""

echo "========================================="
