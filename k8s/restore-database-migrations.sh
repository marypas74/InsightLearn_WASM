#!/bin/bash

###################################################################################
# InsightLearn Database Migration Restore Script
###################################################################################
#
# SCOPO: Ripristinare lo stato delle migrazioni EF Core in caso di danneggiamento
#        del database o inconsistenze tra codice e database.
#
# QUANDO USARE:
# - Errore "There is already an object named 'X' in the database"
# - Inconsistenza tra __EFMigrationsHistory e stato reale del database
# - Database esistente ma senza history delle migrazioni
# - Dopo reset manuale del database
#
# PREREQUISITI:
# - kubectl configurato e funzionante
# - SQL Server pod running in namespace insightlearn
# - Database InsightLearnDb esistente con tutte le tabelle
#
###################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="insightlearn"
SQLSERVER_POD="sqlserver-0"
SA_PASSWORD="InsightLearn123@#"
DATABASE_NAME="InsightLearnDb"

# Current migration from code
CURRENT_MIGRATION="20251017122412_InitialCreate"
PRODUCT_VERSION="8.0.8"

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}InsightLearn Migration Restore Script${NC}"
echo -e "${YELLOW}========================================${NC}"
echo ""

# Step 1: Check SQL Server pod is running
echo -e "${YELLOW}[1/5]${NC} Verifying SQL Server pod..."
if kubectl get pod $SQLSERVER_POD -n $NAMESPACE &> /dev/null; then
    POD_STATUS=$(kubectl get pod $SQLSERVER_POD -n $NAMESPACE -o jsonpath='{.status.phase}')
    if [ "$POD_STATUS" != "Running" ]; then
        echo -e "${RED}ERROR: SQL Server pod is not Running (status: $POD_STATUS)${NC}"
        exit 1
    fi
    echo -e "${GREEN}✓ SQL Server pod is Running${NC}"
else
    echo -e "${RED}ERROR: SQL Server pod '$SQLSERVER_POD' not found in namespace '$NAMESPACE'${NC}"
    exit 1
fi

# Step 2: Check database exists
echo -e "${YELLOW}[2/5]${NC} Checking database exists..."
DB_CHECK=$(kubectl exec -n $NAMESPACE $SQLSERVER_POD -- /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -h -1 \
    -Q "SELECT COUNT(*) FROM sys.databases WHERE name = '$DATABASE_NAME'" 2>&1 | grep -oP '^\s*\d+' | tr -d ' ')

if [ "$DB_CHECK" != "1" ]; then
    echo -e "${RED}ERROR: Database '$DATABASE_NAME' does not exist${NC}"
    echo -e "${YELLOW}Create the database first or run migrations.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Database '$DATABASE_NAME' exists${NC}"

# Step 3: Check migration history table exists
echo -e "${YELLOW}[3/5]${NC} Checking migration history table..."
HISTORY_CHECK=$(kubectl exec -n $NAMESPACE $SQLSERVER_POD -- /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -d $DATABASE_NAME -h -1 \
    -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory'" 2>&1 | grep -oP '^\s*\d+' | tr -d ' ')

if [ "$HISTORY_CHECK" != "1" ]; then
    echo -e "${YELLOW}Migration history table does not exist. Creating...${NC}"
    kubectl exec -n $NAMESPACE $SQLSERVER_POD -- /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SA_PASSWORD" -C -d $DATABASE_NAME \
        -Q "CREATE TABLE [__EFMigrationsHistory] (
            [MigrationId] nvarchar(150) NOT NULL,
            [ProductVersion] nvarchar(32) NOT NULL,
            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
        );"
    echo -e "${GREEN}✓ Migration history table created${NC}"
else
    echo -e "${GREEN}✓ Migration history table exists${NC}"
fi

# Step 4: Show current migration history
echo -e "${YELLOW}[4/5]${NC} Current migration history:"
kubectl exec -n $NAMESPACE $SQLSERVER_POD -- /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -d $DATABASE_NAME -h -1 \
    -Q "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId" || echo "(empty)"

# Step 5: Reset migration history to current code
echo ""
read -p "$(echo -e ${YELLOW}Do you want to reset migration history to current code migration? [y/N]: ${NC})" -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}[5/5]${NC} Resetting migration history..."

    kubectl exec -n $NAMESPACE $SQLSERVER_POD -- /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SA_PASSWORD" -C -d $DATABASE_NAME \
        -Q "DELETE FROM __EFMigrationsHistory;
            INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
            VALUES ('$CURRENT_MIGRATION', '$PRODUCT_VERSION');"

    echo -e "${GREEN}✓ Migration history reset to: $CURRENT_MIGRATION${NC}"

    echo ""
    echo -e "${YELLOW}New migration history:${NC}"
    kubectl exec -n $NAMESPACE $SQLSERVER_POD -- /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SA_PASSWORD" -C -d $DATABASE_NAME -h -1 \
        -Q "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId"
else
    echo -e "${YELLOW}Migration history not changed.${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Script completed successfully!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Restart API pods: kubectl delete pod -l app=insightlearn-api -n $NAMESPACE"
echo "2. Check logs: kubectl logs -n $NAMESPACE -l app=insightlearn-api -f"
echo "3. Verify API starts without migration errors"
echo ""
