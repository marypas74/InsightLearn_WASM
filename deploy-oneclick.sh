#!/bin/bash

###############################################################################
# InsightLearn One-Click Deployment Script
# Questo script automatizza il deployment completo di InsightLearn
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Functions
print_header() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   print_error "This script should NOT be run as root"
   print_info "Run as normal user with docker permissions"
   exit 1
fi

# Header
clear
echo ""
print_header "InsightLearn One-Click Deployment"
echo ""
echo "This script will:"
echo "  1. Check prerequisites"
echo "  2. Configure environment"
echo "  3. Generate SSL certificates"
echo "  4. Build Docker images"
echo "  5. Start all services"
echo "  6. Initialize databases"
echo "  7. Verify deployment"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 0
fi

###############################################################################
# Step 1: Check Prerequisites
###############################################################################

print_header "Step 1/7: Checking Prerequisites"

# Check Docker
if ! command -v docker &> /dev/null; then
    print_error "Docker not found. Please install Docker first."
    exit 1
fi
print_success "Docker found: $(docker --version)"

# Check Docker Compose
if ! command -v docker-compose &> /dev/null; then
    print_error "Docker Compose not found. Please install Docker Compose first."
    exit 1
fi
print_success "Docker Compose found: $(docker-compose --version)"

# Check Docker daemon
if ! docker info &> /dev/null; then
    print_error "Docker daemon not running. Please start Docker."
    exit 1
fi
print_success "Docker daemon running"

# Check disk space (minimum 50GB)
AVAILABLE_SPACE=$(df -BG . | awk 'NR==2 {print $4}' | sed 's/G//')
if [ "$AVAILABLE_SPACE" -lt 50 ]; then
    print_warning "Low disk space: ${AVAILABLE_SPACE}GB available (recommended: 50GB+)"
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 0
    fi
else
    print_success "Disk space OK: ${AVAILABLE_SPACE}GB available"
fi

###############################################################################
# Step 2: Configure Environment
###############################################################################

print_header "Step 2/7: Configuring Environment"

# Check if .env exists
if [ -f .env ]; then
    print_warning ".env file already exists"
    read -p "Overwrite? (y/n) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        mv .env .env.backup.$(date +%Y%m%d_%H%M%S)
        print_info "Backed up existing .env"
    else
        print_info "Using existing .env file"
    fi
fi

# Generate secure random passwords
generate_password() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-32
}

if [ ! -f .env ]; then
    print_info "Generating .env file with secure passwords..."

    cat > .env << EOF
# InsightLearn Environment Configuration
# Generated: $(date)

# Database Passwords
DB_PASSWORD=$(generate_password)
MONGO_PASSWORD=$(generate_password)
REDIS_PASSWORD=$(generate_password)

# JWT Authentication (minimum 32 characters)
JWT_SECRET_KEY=$(generate_password)$(generate_password)

# Admin Credentials
ADMIN_PASSWORD=Admin123!Change

# Encryption Keys (minimum 32 characters)
ENCRYPTION_MASTER_KEY=$(generate_password)$(generate_password)
VIDEO_ENCRYPTION_KEY=$(generate_password)

# Google OAuth (CHANGE THESE WITH REAL VALUES!)
GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID_HERE
GOOGLE_CLIENT_SECRET=YOUR_GOOGLE_CLIENT_SECRET_HERE

# Stripe (CHANGE THESE WITH REAL VALUES!)
STRIPE_PUBLISHABLE_KEY=pk_test_your_stripe_publishable_key
STRIPE_SECRET_KEY=sk_test_your_stripe_secret_key
EOF

    chmod 600 .env
    print_success ".env file created with secure passwords"
    print_warning "IMPORTANT: Update Google OAuth and Stripe keys in .env before production!"
else
    print_info "Using existing .env configuration"
fi

# Load environment variables
set -a
source .env
set +a

print_success "Environment configured"

###############################################################################
# Step 3: Generate SSL Certificates
###############################################################################

print_header "Step 3/7: Generating SSL Certificates"

mkdir -p nginx/certs

if [ -f nginx/certs/tls.crt ] && [ -f nginx/certs/tls.key ]; then
    print_warning "SSL certificates already exist"
    read -p "Regenerate? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_info "Using existing certificates"
    else
        rm -f nginx/certs/tls.*
    fi
fi

if [ ! -f nginx/certs/tls.crt ]; then
    print_info "Generating self-signed certificate..."
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
      -keyout nginx/certs/tls.key \
      -out nginx/certs/tls.crt \
      -subj "/C=IT/ST=Italy/L=Milan/O=InsightLearn/CN=localhost" \
      2>/dev/null

    chmod 600 nginx/certs/tls.key
    chmod 644 nginx/certs/tls.crt
    print_success "SSL certificates generated (self-signed, valid 365 days)"
fi

###############################################################################
# Step 4: Build Docker Images
###############################################################################

print_header "Step 4/7: Building Docker Images"

print_info "This may take 10-20 minutes on first run..."

# Build images
if docker-compose build --no-cache 2>&1 | tee /tmp/build.log; then
    print_success "Docker images built successfully"
else
    print_error "Build failed. Check /tmp/build.log for details"
    exit 1
fi

###############################################################################
# Step 5: Start Services
###############################################################################

print_header "Step 5/7: Starting Services"

print_info "Starting services in correct order..."

# Function to wait for healthy status
wait_for_healthy() {
    local service=$1
    local max_wait=${2:-120}
    local waited=0

    print_info "Waiting for $service to be healthy..."

    while [ $waited -lt $max_wait ]; do
        if docker-compose ps $service | grep -q "healthy"; then
            print_success "$service is healthy"
            return 0
        fi
        sleep 2
        waited=$((waited + 2))
        echo -n "."
    done

    echo ""
    print_error "$service failed to become healthy after ${max_wait}s"
    docker-compose logs --tail=50 $service
    return 1
}

# 1. Database layer
print_info "Starting database layer..."
docker-compose up -d sqlserver redis mongodb elasticsearch

wait_for_healthy sqlserver 120
wait_for_healthy redis 60
wait_for_healthy mongodb 90

# 2. AI/LLM
print_info "Starting Ollama..."
docker-compose up -d ollama
wait_for_healthy ollama 90

# 3. Monitoring
print_info "Starting Prometheus..."
docker-compose up -d prometheus
sleep 10

# 4. Application layer
print_info "Starting application layer..."
docker-compose up -d api web

wait_for_healthy api 90
wait_for_healthy web 90

# 5. Reverse proxy
print_info "Starting Nginx..."
docker-compose up -d nginx
sleep 10

# 6. Grafana
print_info "Starting Grafana..."
docker-compose up -d grafana
sleep 15

# 7. Jenkins (optional)
if [ "$SKIP_JENKINS" != "true" ]; then
    print_info "Starting Jenkins..."
    docker-compose up -d jenkins
fi

print_success "All services started"

###############################################################################
# Step 6: Initialize Databases
###############################################################################

print_header "Step 6/7: Initializing Databases"

# Wait a bit more for services to stabilize
print_info "Waiting for services to stabilize..."
sleep 20

# MongoDB initialization
print_info "Initializing MongoDB collections..."
docker exec -i insightlearn-mongodb mongosh -u admin -p "${MONGO_PASSWORD}" --authenticationDatabase admin << 'MONGO_EOF'
use insightlearn
db.createCollection("videos")
db.createCollection("chatbot_contacts")
db.createCollection("chatbot_messages")
db.createCollection("video_metadata")
db.createCollection("user_sessions")
print("✅ MongoDB collections created")
MONGO_EOF

print_success "MongoDB initialized"

# Ollama model download
print_info "Downloading Ollama llama2 model (this may take 5-10 minutes)..."
if docker exec insightlearn-ollama ollama pull llama2 2>&1 | grep -q "success"; then
    print_success "Ollama llama2 model downloaded"
else
    print_warning "Ollama model download may have failed, check logs"
fi

print_success "Databases initialized"

###############################################################################
# Auto-Restore Data if Backup Exists
###############################################################################

print_info "Checking for existing data backups..."

# Check for seed data first (committed data)
if [ -f "seed-data/InsightLearnDb.bak" ] || [ -d "seed-data/mongodb_dump" ]; then
    LATEST_BACKUP="seed-data"
    BACKUP_TYPE="seed"
    print_success "Found seed data (committed production snapshot)"
else
    # Find most recent backup
    LATEST_BACKUP=$(find ./backups -type d -name "data_*" 2>/dev/null | sort -r | head -1)
    BACKUP_TYPE="backup"
fi

if [ -n "$LATEST_BACKUP" ] && [ -d "$LATEST_BACKUP" ]; then
    if [ "$BACKUP_TYPE" = "seed" ]; then
        print_warning "Found seed data: $LATEST_BACKUP"
    else
        print_warning "Found existing data backup: $LATEST_BACKUP"
    fi
    echo ""
    echo "Do you want to restore data from this backup?"
    echo "This will:"
    echo "  - Restore SQL Server database with all users, courses, etc."
    echo "  - Restore MongoDB data (videos, chatbot messages)"
    echo "  - Restore Redis cache"
    echo "  - Restore user-uploaded files"
    echo ""
    read -p "Restore data? (y/n) " -n 1 -r
    echo ""

    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Starting data restore..."
        echo ""

        # Check if restore script exists
        RESTORE_SCRIPT=""
        if [ "$BACKUP_TYPE" = "seed" ] && [ -f "$LATEST_BACKUP/restore-seed-data.sh" ]; then
            RESTORE_SCRIPT="$LATEST_BACKUP/restore-seed-data.sh"
        elif [ -f "$LATEST_BACKUP/restore-data.sh" ]; then
            RESTORE_SCRIPT="$LATEST_BACKUP/restore-data.sh"
        fi

        if [ -n "$RESTORE_SCRIPT" ]; then
            # Execute restore script
            cd "$LATEST_BACKUP"
            chmod +x $(basename "$RESTORE_SCRIPT")
            ./$(basename "$RESTORE_SCRIPT")

            if [ $? -eq 0 ]; then
                cd - > /dev/null
                print_success "Data restore completed successfully!"
                print_info "Restarting application services..."
                docker-compose restart api web
                sleep 10
                print_success "Application restarted with restored data"
            else
                cd - > /dev/null
                print_error "Data restore failed. Application will start with empty database."
            fi
        else
            print_warning "Restore script not found in backup. Skipping data restore."
        fi
    else
        print_info "Skipping data restore. Application will start with empty database."
    fi
else
    print_info "No existing data backups found. Application will start with empty database."
    print_info "To create a backup later, run: ./backup-data.sh"
fi

echo ""

###############################################################################
# Step 7: Verify Deployment
###############################################################################

print_header "Step 7/7: Verifying Deployment"

# Function to test endpoint
test_endpoint() {
    local name=$1
    local url=$2
    local expected=${3:-200}

    response=$(curl -sk -o /dev/null -w "%{http_code}" "$url" 2>/dev/null)

    if [ "$response" = "$expected" ]; then
        print_success "$name: OK ($response)"
        return 0
    else
        print_error "$name: FAIL (expected $expected, got $response)"
        return 1
    fi
}

print_info "Running health checks..."
echo ""

# Database Layer
echo "Database Layer:"
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${DB_PASSWORD}" -C -Q "SELECT 1" > /dev/null 2>&1 && print_success "  SQL Server" || print_error "  SQL Server"
docker exec insightlearn-redis redis-cli -a "${REDIS_PASSWORD}" --no-auth-warning ping > /dev/null 2>&1 && print_success "  Redis" || print_error "  Redis"
docker exec insightlearn-mongodb mongosh -u admin -p "${MONGO_PASSWORD}" --quiet --eval "db.adminCommand('ping')" > /dev/null 2>&1 && print_success "  MongoDB" || print_error "  MongoDB"
test_endpoint "  Elasticsearch" "http://localhost:9200" 200
echo ""

# Application Layer
echo "Application Layer:"
test_endpoint "  API Health" "http://localhost:7001/health" 200
test_endpoint "  Web Application" "http://localhost:7003" 200
test_endpoint "  Nginx HTTP" "http://localhost" 200
test_endpoint "  Nginx HTTPS" "https://localhost" 200
echo ""

# AI/LLM Layer
echo "AI/LLM Layer:"
test_endpoint "  Ollama API" "http://localhost:11434/api/tags" 200
echo ""

# Monitoring Layer
echo "Monitoring Layer:"
test_endpoint "  Prometheus" "http://localhost:9090/-/healthy" 200
test_endpoint "  Grafana" "http://localhost:3000/api/health" 200
echo ""

# CI/CD Layer
if [ "$SKIP_JENKINS" != "true" ]; then
    echo "CI/CD Layer:"
    test_endpoint "  Jenkins" "http://localhost:8080/login" 200
    echo ""
fi

# Container status
print_info "Container Status:"
docker-compose ps

###############################################################################
# Deployment Complete
###############################################################################

echo ""
print_header "Deployment Complete! 🎉"
echo ""
print_success "InsightLearn is now running!"
echo ""
echo "Access URLs:"
echo "  • Application:     https://localhost"
echo "  • API Direct:      http://localhost:7001"
echo "  • Grafana:         http://localhost:3000 (admin/admin)"
echo "  • Prometheus:      http://localhost:9090"
if [ "$SKIP_JENKINS" != "true" ]; then
    echo "  • Jenkins:         http://localhost:8080"
    echo "    Initial password: docker exec insightlearn-jenkins cat /var/jenkins_home/secrets/initialAdminPassword"
fi
echo ""
echo "Admin Login:"
echo "  Email:    admin@insightlearn.cloud"
echo "  Password: ${ADMIN_PASSWORD}"
echo ""
print_warning "IMPORTANT:"
echo "  1. Change admin password after first login!"
echo "  2. Update Google OAuth credentials in .env for production"
echo "  3. Update Stripe credentials in .env for payments"
echo "  4. SSL certificate is self-signed (browser will show warning)"
echo ""
print_info "View logs:        docker-compose logs -f"
print_info "Stop all:         docker-compose down"
print_info "Restart:          docker-compose restart"
print_info "Full docs:        cat DEPLOYMENT-COMPLETE-GUIDE.md"
echo ""
