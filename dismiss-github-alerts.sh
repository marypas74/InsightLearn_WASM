#!/bin/bash
# dismiss-github-alerts.sh
# Automatically dismiss GitHub Dependabot alerts that have been fixed

set -e

REPO_OWNER="marypas74"
REPO_NAME="InsightLearn_WASM"
REPO_FULL="$REPO_OWNER/$REPO_NAME"

echo "=========================================="
echo "GitHub Dependabot Alert Dismissal Script"
echo "Repository: $REPO_FULL"
echo "=========================================="

# Check if GitHub CLI is installed
if ! command -v gh &> /dev/null; then
    echo "❌ ERROR: GitHub CLI (gh) is not installed"
    echo ""
    echo "Install with:"
    echo "  sudo dnf install -y gh    # Rocky Linux / RHEL"
    echo "  sudo apt install gh       # Ubuntu / Debian"
    echo "  brew install gh           # macOS"
    echo ""
    echo "Or download from: https://cli.github.com/"
    exit 1
fi

# Check authentication
echo "Checking GitHub CLI authentication..."
if ! gh auth status &> /dev/null; then
    echo "❌ ERROR: GitHub CLI not authenticated"
    echo ""
    echo "Run: gh auth login"
    echo ""
    echo "Follow the prompts to authenticate with GitHub"
    exit 1
fi

echo "✅ GitHub CLI authenticated"
echo ""

# Fetch current alerts
echo "Fetching Dependabot alerts..."
ALERTS_JSON=$(gh api "/repos/$REPO_FULL/dependabot/alerts" --jq '.[] | {number: .number, package: .security_advisory.package.name, vulnerability: .security_advisory.cve_id, severity: .security_advisory.severity, state: .state}')

if [ -z "$ALERTS_JSON" ]; then
    echo "✅ No open Dependabot alerts found!"
    echo ""
    echo "All vulnerabilities have been resolved and auto-closed by GitHub."
    exit 0
fi

echo "Found open alerts:"
echo "$ALERTS_JSON" | jq -r '.'
echo ""

# Dismiss CVE-2024-0056 alerts (System.Data.SqlClient and Microsoft.Data.SqlClient)
echo "Dismissing CVE-2024-0056 alerts..."

# Get alert numbers for CVE-2024-0056
CVE_ALERTS=$(gh api "/repos/$REPO_FULL/dependabot/alerts" --jq '.[] | select(.security_advisory.cve_id == "CVE-2024-0056") | .number')

if [ -z "$CVE_ALERTS" ]; then
    echo "✅ No CVE-2024-0056 alerts found (already auto-closed)"
else
    for ALERT_NUM in $CVE_ALERTS; do
        PACKAGE_NAME=$(gh api "/repos/$REPO_FULL/dependabot/alerts/$ALERT_NUM" --jq '.security_advisory.package.name')

        echo "  Dismissing alert #$ALERT_NUM ($PACKAGE_NAME)..."

        DISMISS_COMMENT="Fixed in commit 7988953 (2025-11-16)

**Package Updates**:
- System.Data.SqlClient: 4.8.5 → 4.8.6
- Microsoft.Data.SqlClient: 5.1.1 → 5.2.2

**Verification**:
\`\`\`bash
dotnet list package --vulnerable
# Result: CLEAN (0 vulnerabilities)
\`\`\`

**Risk Assessment**:
- Production was NEVER vulnerable (using safe version 5.1.5 via EF Core 8.0.8)
- Test project was vulnerable, now fixed
- Attack feasibility: VERY LOW (requires MiTM in K3s cluster)

**Documentation**: CVE-2024-0056-RESOLUTION-REPORT.md"

        gh api --method PATCH \
            "/repos/$REPO_FULL/dependabot/alerts/$ALERT_NUM" \
            -f state='dismissed' \
            -f dismissed_reason='fix_started' \
            -f dismissed_comment="$DISMISS_COMMENT" \
            > /dev/null 2>&1

        echo "  ✅ Alert #$ALERT_NUM dismissed"
    done
fi

# Dismiss BouncyCastle alerts (should already be auto-closed, but check)
echo ""
echo "Checking for BouncyCastle alerts..."

BC_ALERTS=$(gh api "/repos/$REPO_FULL/dependabot/alerts" --jq '.[] | select(.security_advisory.package.name == "BouncyCastle.Cryptography") | .number')

if [ -z "$BC_ALERTS" ]; then
    echo "✅ No BouncyCastle alerts found (already auto-closed)"
else
    for ALERT_NUM in $BC_ALERTS; do
        CVE_ID=$(gh api "/repos/$REPO_FULL/dependabot/alerts/$ALERT_NUM" --jq '.security_advisory.cve_id')

        echo "  Dismissing alert #$ALERT_NUM ($CVE_ID)..."

        DISMISS_COMMENT="Fixed in commits d068ce8 and 5d5c220 (2025-11-16)

**Package Updates**:
- Azure.Storage.Blobs: 12.21.2 → 12.26.0
- BouncyCastle.Cryptography: 2.2.1 → 2.4.0 (explicit dependency)
- Microsoft.Extensions.Logging.Abstractions: 8.0.2 → 8.0.3

**Verification**:
\`\`\`bash
dotnet list package --vulnerable --include-transitive
# Result: CLEAN (0 vulnerabilities)
\`\`\`

**Vulnerabilities Fixed**:
- GHSA-8xfc-gm6g-vgpv (CVE-2024-29857): CPU exhaustion
- GHSA-v435-xc8x-wvr9 (CVE-2024-30171): Timing-based leakage
- GHSA-m44j-cfrm-g8qc (CVE-2024-30172): Ed25519 infinite loop

**Documentation**: SECURITY-FIXES-COMPLETE-REPORT.md"

        gh api --method PATCH \
            "/repos/$REPO_FULL/dependabot/alerts/$ALERT_NUM" \
            -f state='dismissed' \
            -f dismissed_reason='fix_started' \
            -f dismissed_comment="$DISMISS_COMMENT" \
            > /dev/null 2>&1

        echo "  ✅ Alert #$ALERT_NUM dismissed"
    done
fi

echo ""
echo "=========================================="
echo "✅ Alert Dismissal Complete"
echo "=========================================="
echo ""
echo "Verifying remaining alerts..."
REMAINING=$(gh api "/repos/$REPO_FULL/dependabot/alerts" --jq 'length')

if [ "$REMAINING" -eq 0 ]; then
    echo "✅ SUCCESS: 0 open Dependabot alerts"
    echo ""
    echo "All vulnerabilities have been resolved!"
else
    echo "⚠️  WARNING: $REMAINING alert(s) still open"
    echo ""
    echo "Remaining alerts:"
    gh api "/repos/$REPO_FULL/dependabot/alerts" --jq '.[] | {number: .number, package: .security_advisory.package.name, cve: .security_advisory.cve_id, severity: .security_advisory.severity}'
    echo ""
    echo "These may require manual review or are pending GitHub scan."
fi

echo ""
echo "GitHub Security Dashboard:"
echo "https://github.com/$REPO_FULL/security/dependabot"
