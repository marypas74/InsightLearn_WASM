#!/bin/bash

###############################################################################
# InsightLearn Data Backup Script
# Esporta TUTTI i dati da SQL Server, MongoDB, Redis per ripristino completo
###############################################################################

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }

# Load environment
if [ -f .env ]; then
    set -a
    source .env
    set +a
else
    print_error ".env file not found!"
    exit 1
fi

# Create backup directory
BACKUP_DIR="./backups/data_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"

print_info "Creating complete data backup in: $BACKUP_DIR"
echo ""

###############################################################################
# 1. SQL Server Database Backup
###############################################################################

print_info "Step 1/5: Backing up SQL Server database..."

# Check if SQL Server is running
if ! docker ps | grep -q insightlearn-sqlserver; then
    print_error "SQL Server container not running!"
    print_info "Start it with: docker-compose up -d sqlserver"
    exit 1
fi

# Create SQL Server backup inside container
print_info "Creating SQL Server .bak file..."
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -C \
    -Q "BACKUP DATABASE InsightLearnDb TO DISK = '/tmp/InsightLearnDb.bak' WITH FORMAT, INIT, NAME = 'InsightLearn Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

if [ $? -eq 0 ]; then
    print_success "SQL Server backup created"
else
    print_error "SQL Server backup failed!"
    exit 1
fi

# Copy backup file from container to host
print_info "Copying SQL Server backup to host..."
docker cp insightlearn-sqlserver:/tmp/InsightLearnDb.bak "$BACKUP_DIR/InsightLearnDb.bak"

if [ -f "$BACKUP_DIR/InsightLearnDb.bak" ]; then
    SIZE=$(du -h "$BACKUP_DIR/InsightLearnDb.bak" | cut -f1)
    print_success "SQL Server backup saved: $SIZE"
else
    print_error "Failed to copy SQL Server backup!"
    exit 1
fi

# Export SQL schema (for reference)
print_info "Exporting SQL schema..."
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -C \
    -Q "SELECT TABLE_SCHEMA, TABLE_NAME FROM InsightLearnDb.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME" \
    -o "$BACKUP_DIR/sql_tables_list.txt"

print_success "SQL schema exported"

###############################################################################
# 2. MongoDB Database Backup
###############################################################################

print_info "Step 2/5: Backing up MongoDB database..."

if ! docker ps | grep -q insightlearn-mongodb; then
    print_warning "MongoDB container not running, skipping..."
else
    # MongoDB dump
    print_info "Creating MongoDB dump..."
    docker exec insightlearn-mongodb mongodump \
        --username=admin \
        --password="${MONGO_PASSWORD}" \
        --authenticationDatabase=admin \
        --db=insightlearn \
        --out=/tmp/mongodb_backup

    # Copy MongoDB dump to host
    docker cp insightlearn-mongodb:/tmp/mongodb_backup "$BACKUP_DIR/mongodb_dump"

    if [ -d "$BACKUP_DIR/mongodb_dump" ]; then
        SIZE=$(du -sh "$BACKUP_DIR/mongodb_dump" | cut -f1)
        print_success "MongoDB backup saved: $SIZE"
    else
        print_warning "MongoDB backup may have failed"
    fi
fi

###############################################################################
# 3. Redis Data Backup
###############################################################################

print_info "Step 3/5: Backing up Redis data..."

if ! docker ps | grep -q insightlearn-redis; then
    print_warning "Redis container not running, skipping..."
else
    # Redis RDB snapshot
    print_info "Creating Redis snapshot..."
    docker exec insightlearn-redis redis-cli -a "${REDIS_PASSWORD}" --no-auth-warning SAVE

    # Copy Redis dump
    docker cp insightlearn-redis:/data/dump.rdb "$BACKUP_DIR/redis_dump.rdb"

    if [ -f "$BACKUP_DIR/redis_dump.rdb" ]; then
        SIZE=$(du -h "$BACKUP_DIR/redis_dump.rdb" | cut -f1)
        print_success "Redis backup saved: $SIZE"
    else
        print_warning "Redis backup may have failed"
    fi
fi

###############################################################################
# 4. User-uploaded Files Backup
###############################################################################

print_info "Step 4/5: Backing up user-uploaded files..."

# Backup API uploaded files volume
if docker volume ls | grep -q insightlearn-wasm_api-files; then
    print_info "Backing up API uploaded files..."
    docker run --rm \
        -v insightlearn-wasm_api-files:/data \
        -v "$PWD/$BACKUP_DIR":/backup \
        ubuntu tar czf /backup/api-files.tar.gz /data 2>/dev/null

    if [ -f "$BACKUP_DIR/api-files.tar.gz" ]; then
        SIZE=$(du -h "$BACKUP_DIR/api-files.tar.gz" | cut -f1)
        print_success "API files backup saved: $SIZE"
    fi
fi

###############################################################################
# 5. Create Restore Script
###############################################################################

print_info "Step 5/5: Creating restore script..."

cat > "$BACKUP_DIR/restore-data.sh" << 'RESTORE_SCRIPT'
#!/bin/bash

###############################################################################
# InsightLearn Data Restore Script
# Ripristina TUTTI i dati da backup
###############################################################################

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }

# Get backup directory
BACKUP_DIR=$(dirname "$0")
cd "$BACKUP_DIR"

print_info "Restoring data from: $BACKUP_DIR"
echo ""

# Load environment
if [ -f ../../.env ]; then
    set -a
    source ../../.env
    set +a
else
    print_error ".env file not found in repository root!"
    exit 1
fi

###############################################################################
# 1. Restore SQL Server Database
###############################################################################

print_info "Step 1/4: Restoring SQL Server database..."

# Check if container is running
if ! docker ps | grep -q insightlearn-sqlserver; then
    print_error "SQL Server container not running!"
    print_info "Start it with: docker-compose up -d sqlserver"
    exit 1
fi

# Wait for SQL Server to be ready
print_info "Waiting for SQL Server to be ready..."
sleep 10

# Copy backup file to container
print_info "Copying backup file to container..."
docker cp InsightLearnDb.bak insightlearn-sqlserver:/tmp/InsightLearnDb.bak

# Restore database
print_info "Restoring database (this may take a few minutes)..."
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -C \
    -Q "RESTORE DATABASE InsightLearnDb FROM DISK = '/tmp/InsightLearnDb.bak' WITH REPLACE, STATS = 10"

if [ $? -eq 0 ]; then
    print_success "SQL Server database restored"
else
    print_error "SQL Server restore failed!"
    exit 1
fi

###############################################################################
# 2. Restore MongoDB Database
###############################################################################

print_info "Step 2/4: Restoring MongoDB database..."

if [ ! -d "mongodb_dump" ]; then
    print_warning "MongoDB backup not found, skipping..."
else
    if ! docker ps | grep -q insightlearn-mongodb; then
        print_error "MongoDB container not running!"
        exit 1
    fi

    # Copy dump to container
    docker cp mongodb_dump insightlearn-mongodb:/tmp/mongodb_backup

    # Restore MongoDB
    docker exec insightlearn-mongodb mongorestore \
        --username=admin \
        --password="${MONGO_PASSWORD}" \
        --authenticationDatabase=admin \
        --db=insightlearn \
        --drop \
        /tmp/mongodb_backup/insightlearn

    print_success "MongoDB database restored"
fi

###############################################################################
# 3. Restore Redis Data
###############################################################################

print_info "Step 3/4: Restoring Redis data..."

if [ ! -f "redis_dump.rdb" ]; then
    print_warning "Redis backup not found, skipping..."
else
    if ! docker ps | grep -q insightlearn-redis; then
        print_error "Redis container not running!"
        exit 1
    fi

    # Stop Redis temporarily
    docker-compose stop redis

    # Copy dump file
    docker cp redis_dump.rdb insightlearn-redis:/data/dump.rdb

    # Restart Redis
    docker-compose start redis
    sleep 5

    print_success "Redis data restored"
fi

###############################################################################
# 4. Restore User Files
###############################################################################

print_info "Step 4/4: Restoring user-uploaded files..."

if [ ! -f "api-files.tar.gz" ]; then
    print_warning "API files backup not found, skipping..."
else
    # Restore API files volume
    docker run --rm \
        -v insightlearn-wasm_api-files:/data \
        -v "$PWD":/backup \
        ubuntu tar xzf /backup/api-files.tar.gz -C / 2>/dev/null

    print_success "User files restored"
fi

###############################################################################
# Complete
###############################################################################

echo ""
print_success "Data restore complete!"
echo ""
print_info "Restart application:"
echo "  docker-compose restart api web"
echo ""
print_info "Verify data:"
echo "  docker exec insightlearn-sqlserver sqlcmd -S localhost -U sa -P '${DB_PASSWORD}' -C -Q 'SELECT COUNT(*) FROM InsightLearnDb.dbo.Users'"
echo "  docker exec insightlearn-mongodb mongosh -u admin -p '${MONGO_PASSWORD}' --eval 'use insightlearn; db.videos.countDocuments()'"
echo ""
RESTORE_SCRIPT

chmod +x "$BACKUP_DIR/restore-data.sh"
print_success "Restore script created: restore-data.sh"

###############################################################################
# 6. Create Backup Metadata
###############################################################################

cat > "$BACKUP_DIR/backup-info.txt" << EOF
InsightLearn Data Backup Information
=====================================

Backup Date: $(date)
Backup Directory: $BACKUP_DIR
Hostname: $(hostname)

Files Included:
---------------
$(ls -lh "$BACKUP_DIR" | tail -n +2)

Database Sizes:
---------------
SQL Server: $(du -h "$BACKUP_DIR/InsightLearnDb.bak" 2>/dev/null | cut -f1 || echo "N/A")
MongoDB: $(du -sh "$BACKUP_DIR/mongodb_dump" 2>/dev/null | cut -f1 || echo "N/A")
Redis: $(du -h "$BACKUP_DIR/redis_dump.rdb" 2>/dev/null | cut -f1 || echo "N/A")
API Files: $(du -h "$BACKUP_DIR/api-files.tar.gz" 2>/dev/null | cut -f1 || echo "N/A")

Total Backup Size: $(du -sh "$BACKUP_DIR" | cut -f1)

Restore Instructions:
---------------------
1. Copy this entire backup directory to the new system
2. Navigate to the backup directory
3. Run: ./restore-data.sh
4. Restart application: docker-compose restart api web

Environment Variables Required:
--------------------------------
DB_PASSWORD (SQL Server)
MONGO_PASSWORD (MongoDB)
REDIS_PASSWORD (Redis)

These should be set in .env file in repository root.
EOF

###############################################################################
# Summary
###############################################################################

echo ""
echo "========================================"
echo "Backup Complete!"
echo "========================================"
echo ""
print_success "Location: $BACKUP_DIR"
echo ""
print_info "Backup Contents:"
ls -lh "$BACKUP_DIR"
echo ""
print_info "Total Size: $(du -sh "$BACKUP_DIR" | cut -f1)"
echo ""
print_info "To restore on another system:"
echo "  1. Copy backup directory to new system"
echo "  2. cd $BACKUP_DIR"
echo "  3. ./restore-data.sh"
echo ""
print_warning "IMPORTANT: Keep backup files secure - they contain production data!"
echo ""
