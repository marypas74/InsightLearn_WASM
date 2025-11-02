#!/bin/bash
# Version management script for InsightLearn
# Usage:
#   ./version.sh                    - Show current version
#   ./version.sh bump major         - Bump major version (1.0.0 -> 2.0.0)
#   ./version.sh bump minor         - Bump minor version (1.0.0 -> 1.1.0)
#   ./version.sh bump patch         - Bump patch version (1.0.0 -> 1.0.1)
#   ./version.sh set 2.3.5          - Set specific version
#   ./version.sh tag                - Create git tag for current version

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
PROPS_FILE="$PROJECT_ROOT/Directory.Build.props"

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to get current version from props file
get_current_version() {
    if [ ! -f "$PROPS_FILE" ]; then
        print_error "Directory.Build.props not found!"
        exit 1
    fi

    VERSION_PREFIX=$(grep '<VersionPrefix>' "$PROPS_FILE" | sed 's/.*<VersionPrefix>\(.*\)<\/VersionPrefix>.*/\1/')
    echo "$VERSION_PREFIX"
}

# Function to update version in props file
set_version() {
    local new_version=$1

    if [ ! -f "$PROPS_FILE" ]; then
        print_error "Directory.Build.props not found!"
        exit 1
    fi

    # Validate version format (semantic versioning)
    if ! [[ $new_version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        print_error "Invalid version format. Must be MAJOR.MINOR.PATCH (e.g., 1.2.3)"
        exit 1
    fi

    # Update VersionPrefix in props file
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        sed -i '' "s|<VersionPrefix>.*</VersionPrefix>|<VersionPrefix>$new_version</VersionPrefix>|" "$PROPS_FILE"
    else
        # Linux
        sed -i "s|<VersionPrefix>.*</VersionPrefix>|<VersionPrefix>$new_version</VersionPrefix>|" "$PROPS_FILE"
    fi

    print_success "Version updated to $new_version in Directory.Build.props"
}

# Function to bump version
bump_version() {
    local bump_type=$1
    local current_version=$(get_current_version)

    # Split version into parts
    IFS='.' read -r -a version_parts <<< "$current_version"
    local major="${version_parts[0]}"
    local minor="${version_parts[1]}"
    local patch="${version_parts[2]}"

    case $bump_type in
        major)
            major=$((major + 1))
            minor=0
            patch=0
            ;;
        minor)
            minor=$((minor + 1))
            patch=0
            ;;
        patch)
            patch=$((patch + 1))
            ;;
        *)
            print_error "Invalid bump type. Use: major, minor, or patch"
            exit 1
            ;;
    esac

    local new_version="$major.$minor.$patch"

    print_info "Bumping $bump_type version: $current_version -> $new_version"
    set_version "$new_version"

    return 0
}

# Function to create git tag
create_git_tag() {
    local version=$(get_current_version)
    local tag_name="v$version"

    # Check if git repo exists
    if ! git rev-parse --git-dir > /dev/null 2>&1; then
        print_warning "Not a git repository. Skipping tag creation."
        return 1
    fi

    # Check if tag already exists
    if git rev-parse "$tag_name" >/dev/null 2>&1; then
        print_error "Tag $tag_name already exists!"
        read -p "Do you want to delete and recreate it? (y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            git tag -d "$tag_name"
            print_info "Deleted existing tag $tag_name"
        else
            exit 1
        fi
    fi

    # Create annotated tag
    print_info "Creating git tag $tag_name..."
    git tag -a "$tag_name" -m "Release version $version"
    print_success "Git tag $tag_name created successfully"

    print_info ""
    print_info "To push the tag to remote, run:"
    print_info "  git push origin $tag_name"
}

# Function to display current version info
show_version() {
    local version=$(get_current_version)

    echo ""
    echo "========================================="
    echo "  InsightLearn Version Information"
    echo "========================================="
    echo "Current Version: $version"

    if git rev-parse --git-dir > /dev/null 2>&1; then
        local git_commit=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")
        local git_branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
        local build_number=$(git rev-list --count HEAD 2>/dev/null || echo "0")

        echo "Git Branch:      $git_branch"
        echo "Git Commit:      $git_commit"
        echo "Build Number:    $build_number"

        # Check for uncommitted changes
        if [ -n "$(git status --porcelain 2>/dev/null)" ]; then
            print_warning "Working directory has uncommitted changes"
        else
            print_success "Working directory is clean"
        fi

        # List recent tags
        echo ""
        echo "Recent Tags:"
        git tag -l --sort=-v:refname | head -5 || echo "  No tags found"
    else
        print_warning "Not a git repository"
    fi

    echo "========================================="
    echo ""
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: $0 [COMMAND] [OPTIONS]

Commands:
  (none)              Show current version information
  show                Show current version information
  bump major          Bump major version (X.0.0)
  bump minor          Bump minor version (0.X.0)
  bump patch          Bump patch version (0.0.X)
  set VERSION         Set specific version (e.g., set 2.3.5)
  tag                 Create git tag for current version

Examples:
  $0                  # Show version info
  $0 bump patch       # Bump patch version
  $0 set 2.0.0        # Set version to 2.0.0
  $0 tag              # Create git tag

Version Format:
  Semantic Versioning (MAJOR.MINOR.PATCH)
  - MAJOR: Breaking changes
  - MINOR: New features, backward compatible
  - PATCH: Bug fixes, backward compatible

Workflow:
  1. Make changes to code
  2. Bump version: ./version.sh bump patch
  3. Commit changes: git add . && git commit -m "Bump version"
  4. Create tag: ./version.sh tag
  5. Push: git push && git push --tags
  6. Build: ./k8s/build-images.sh

EOF
}

# Main script logic
case "${1:-show}" in
    show)
        show_version
        ;;
    bump)
        if [ -z "$2" ]; then
            print_error "Missing bump type. Use: major, minor, or patch"
            show_usage
            exit 1
        fi
        bump_version "$2"
        show_version
        ;;
    set)
        if [ -z "$2" ]; then
            print_error "Missing version number"
            show_usage
            exit 1
        fi
        set_version "$2"
        show_version
        ;;
    tag)
        create_git_tag
        ;;
    help|--help|-h)
        show_usage
        ;;
    *)
        print_error "Unknown command: $1"
        show_usage
        exit 1
        ;;
esac
