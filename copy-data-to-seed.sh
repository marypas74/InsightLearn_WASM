#!/bin/bash

###############################################################################
# Copy Production Data to Seed Data
# Questo script copia i dati dal backup più recente a seed-data/
# per commitarli nella repository
###############################################################################

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }

echo ""
echo "================================"
echo "Copy Data to Seed"
echo "================================"
echo ""

# Check if backups exist
if [ ! -d "backups" ]; then
    print_error "No backups directory found!"
    print_info "Run ./backup-data.sh first to create a backup"
    exit 1
fi

# Find latest backup
LATEST_BACKUP=$(find ./backups -type d -name "data_*" 2>/dev/null | sort -r | head -1)

if [ -z "$LATEST_BACKUP" ]; then
    print_error "No backups found!"
    print_info "Run ./backup-data.sh first to create a backup"
    exit 1
fi

print_info "Found backup: $LATEST_BACKUP"
echo ""

# Show backup info
if [ -f "$LATEST_BACKUP/backup-info.txt" ]; then
    cat "$LATEST_BACKUP/backup-info.txt"
    echo ""
fi

# Warning
print_warning "IMPORTANT:"
echo "  This will copy production data to seed-data/ directory"
echo "  The seed-data/ directory is COMMITTED to Git"
echo "  Make sure:"
echo "    1. Repository is PRIVATE on GitHub"
echo "    2. No sensitive data (passwords, credit cards, etc.)"
echo "    3. You want this data in version control"
echo ""

read -p "Continue? (yes/no) " -r
echo ""

if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    print_info "Cancelled"
    exit 0
fi

# Create seed-data directory
mkdir -p seed-data

# Copy SQL Server backup
if [ -f "$LATEST_BACKUP/InsightLearnDb.bak" ]; then
    print_info "Copying SQL Server database..."
    cp "$LATEST_BACKUP/InsightLearnDb.bak" seed-data/
    SIZE=$(du -h seed-data/InsightLearnDb.bak | cut -f1)
    print_success "SQL Server database copied: $SIZE"
else
    print_warning "SQL Server backup not found"
fi

# Copy MongoDB dump
if [ -d "$LATEST_BACKUP/mongodb_dump" ]; then
    print_info "Copying MongoDB data..."
    rm -rf seed-data/mongodb_dump
    cp -r "$LATEST_BACKUP/mongodb_dump" seed-data/
    SIZE=$(du -sh seed-data/mongodb_dump | cut -f1)
    print_success "MongoDB data copied: $SIZE"
else
    print_warning "MongoDB dump not found"
fi

# Copy Redis dump
if [ -f "$LATEST_BACKUP/redis_dump.rdb" ]; then
    print_info "Copying Redis data..."
    cp "$LATEST_BACKUP/redis_dump.rdb" seed-data/
    SIZE=$(du -h seed-data/redis_dump.rdb | cut -f1)
    print_success "Redis data copied: $SIZE"
else
    print_warning "Redis dump not found"
fi

# Copy API files (optional - can be large)
if [ -f "$LATEST_BACKUP/api-files.tar.gz" ]; then
    SIZE=$(du -h "$LATEST_BACKUP/api-files.tar.gz" | cut -f1)
    echo ""
    print_warning "API uploaded files found: $SIZE"
    read -p "Include uploaded files in seed data? (y/n) " -n 1 -r
    echo ""

    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Copying API files..."
        cp "$LATEST_BACKUP/api-files.tar.gz" seed-data/
        print_success "API files copied"
    else
        print_info "Skipping API files (can be added later if needed)"
    fi
fi

# Create info file
cat > seed-data/SEED-INFO.txt << EOF
Seed Data Information
=====================

Created: $(date)
Source Backup: $LATEST_BACKUP
Hostname: $(hostname)

Files Included:
---------------
$(ls -lh seed-data/ | tail -n +2)

Total Size: $(du -sh seed-data | cut -f1)

Usage:
------
These files are automatically detected and restored by deploy-oneclick.sh
when deploying to a new system.

The deployment script will prompt to restore data if these files exist.

Restoration:
------------
1. Clone repository (with seed-data/)
2. cd InsightLearn_WASM
3. ./deploy-oneclick.sh
4. Answer "yes" when prompted to restore data

Security:
---------
⚠️ This data is COMMITTED to Git repository!
Ensure repository is PRIVATE and data is not sensitive.
EOF

# Summary
echo ""
echo "================================"
echo "Copy Complete!"
echo "================================"
echo ""
print_success "Production data copied to seed-data/"
echo ""
print_info "Files in seed-data/:"
ls -lh seed-data/
echo ""
print_info "Total size: $(du -sh seed-data | cut -f1)"
echo ""
print_warning "Next steps:"
echo "  1. Review files in seed-data/"
echo "  2. git add seed-data/"
echo "  3. git commit -m 'feat: Add production data snapshot'"
echo "  4. git push"
echo ""
print_info "On new system, data will be auto-restored by deploy-oneclick.sh"
echo ""
