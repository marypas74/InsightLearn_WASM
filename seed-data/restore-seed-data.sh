#!/bin/bash

###############################################################################
# Restore Seed Data Script
# Auto-generated restore script for seed data
###############################################################################

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }

SEED_DIR=$(dirname "$0")
cd "$SEED_DIR"

print_info "Restoring seed data..."
echo ""

# Load environment
if [ -f ../.env ]; then
    set -a
    source ../.env
    set +a
else
    print_error ".env file not found in repository root!"
    exit 1
fi

###############################################################################
# 1. Restore SQL Server Database
###############################################################################

if [ -f "InsightLearnDb.bak" ]; then
    print_info "Step 1/4: Restoring SQL Server database..."

    if ! docker ps | grep -q insightlearn-sqlserver; then
        print_error "SQL Server container not running!"
        exit 1
    fi

    # Wait for SQL Server
    sleep 10

    # Copy backup to container
    docker cp InsightLearnDb.bak insightlearn-sqlserver:/tmp/

    # Restore database
    docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "${DB_PASSWORD}" -C \
        -Q "RESTORE DATABASE InsightLearnDb FROM DISK = '/tmp/InsightLearnDb.bak' WITH REPLACE, STATS = 10"

    if [ $? -eq 0 ]; then
        print_success "SQL Server database restored"
    else
        print_error "SQL Server restore failed!"
        exit 1
    fi
else
    print_info "Step 1/4: No SQL Server backup found, skipping..."
fi

###############################################################################
# 2. Restore MongoDB Database
###############################################################################

if [ -d "mongodb_dump/insightlearn" ]; then
    print_info "Step 2/4: Restoring MongoDB database..."

    if ! docker ps | grep -q insightlearn-mongodb; then
        print_error "MongoDB container not running!"
        exit 1
    fi

    # Copy dump to container
    docker cp mongodb_dump insightlearn-mongodb:/tmp/

    # Restore MongoDB
    docker exec insightlearn-mongodb mongorestore \
        --username=admin \
        --password="${MONGO_PASSWORD}" \
        --authenticationDatabase=admin \
        --db=insightlearn \
        --drop \
        /tmp/mongodb_dump/insightlearn

    print_success "MongoDB database restored"
else
    print_info "Step 2/4: No MongoDB dump found, skipping..."
fi

###############################################################################
# 3. Restore Redis Data
###############################################################################

if [ -f "redis_dump.rdb" ]; then
    print_info "Step 3/4: Restoring Redis data..."

    if ! docker ps | grep -q insightlearn-redis; then
        print_error "Redis container not running!"
        exit 1
    fi

    # Stop Redis
    docker-compose -f ../docker-compose.yml stop redis

    # Copy dump
    docker cp redis_dump.rdb insightlearn-redis:/data/dump.rdb

    # Restart Redis
    docker-compose -f ../docker-compose.yml start redis
    sleep 5

    print_success "Redis data restored"
else
    print_info "Step 3/4: No Redis dump found, skipping..."
fi

###############################################################################
# 4. Restore User Files
###############################################################################

if [ -f "api-files.tar.gz" ]; then
    print_info "Step 4/4: Restoring user-uploaded files..."

    docker run --rm \
        -v insightlearn-wasm_api-files:/data \
        -v "$PWD":/backup \
        ubuntu tar xzf /backup/api-files.tar.gz -C / 2>/dev/null

    print_success "User files restored"
else
    print_info "Step 4/4: No API files found, skipping..."
fi

###############################################################################
# Complete
###############################################################################

echo ""
print_success "Seed data restore complete!"
echo ""
