# GitHub Dependabot Alerts - Manual Dismissal Guide

**Status**: 4 HIGH alerts pending (CVE-2024-0056)
**Expected Auto-Close**: 24-48 hours
**Manual Dismissal**: Use this guide if alerts don't auto-close

---

## Quick Check

Visit: https://github.com/marypas74/InsightLearn_WASM/security/dependabot

**Current Status** (2025-11-16):
- ✅ **3 MODERATE BouncyCastle alerts**: CLOSED (GitHub processed commits)
- ⏳ **4 HIGH CVE-2024-0056 alerts**: Pending auto-close

---

## Option 1: Automated Dismissal (Recommended)

### Prerequisites

1. **Install GitHub CLI**:
   ```bash
   sudo dnf install -y gh
   ```

2. **Authenticate**:
   ```bash
   gh auth login
   ```
   - Select: GitHub.com
   - Protocol: HTTPS
   - Authenticate: Login with a web browser
   - Follow browser prompts to authorize GitHub CLI

3. **Verify Authentication**:
   ```bash
   gh auth status
   ```
   Expected output: `✓ Logged in to github.com account marypas74`

### Run Automated Dismissal

```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM
./dismiss-github-alerts.sh
```

**What This Script Does**:
- ✅ Checks GitHub CLI installation and authentication
- ✅ Fetches all open Dependabot alerts
- ✅ Dismisses CVE-2024-0056 alerts with detailed comments
- ✅ Dismisses BouncyCastle alerts (if still open)
- ✅ Provides verification summary

**Expected Output**:
```
==========================================
GitHub Dependabot Alert Dismissal Script
Repository: marypas74/InsightLearn_WASM
==========================================
✅ GitHub CLI authenticated

Fetching Dependabot alerts...
Found open alerts:
  Alert #1: System.Data.SqlClient (CVE-2024-0056) - HIGH
  Alert #2: Microsoft.Data.SqlClient (CVE-2024-0056) - HIGH

Dismissing CVE-2024-0056 alerts...
  Dismissing alert #1 (System.Data.SqlClient)...
  ✅ Alert #1 dismissed
  Dismissing alert #2 (Microsoft.Data.SqlClient)...
  ✅ Alert #2 dismissed

==========================================
✅ Alert Dismissal Complete
==========================================

✅ SUCCESS: 0 open Dependabot alerts
All vulnerabilities have been resolved!
```

---

## Option 2: Manual Dismissal via Web UI

### Step-by-Step Instructions

1. **Navigate to Security Tab**:
   - Visit: https://github.com/marypas74/InsightLearn_WASM
   - Click "Security" tab (top menu)
   - Click "Dependabot alerts" (left sidebar)

2. **Dismiss Alert #1 - System.Data.SqlClient**:
   - Click on alert "System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass"
   - Click "Dismiss alert" button (top right)
   - Select reason: **"A fix has already started"**
   - Add comment:
     ```
     Fixed in commit 7988953 (2025-11-16)

     Package Updates:
     - System.Data.SqlClient: 4.8.5 → 4.8.6

     Verification: dotnet list package --vulnerable returns CLEAN
     Documentation: CVE-2024-0056-RESOLUTION-REPORT.md
     ```
   - Click "Dismiss alert"

3. **Dismiss Alert #2 - Microsoft.Data.SqlClient**:
   - Click on alert "Microsoft.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass"
   - Click "Dismiss alert" button
   - Select reason: **"A fix has already started"**
   - Add comment:
     ```
     Fixed in commit 7988953 (2025-11-16)

     Package Updates:
     - Microsoft.Data.SqlClient: 5.1.1 → 5.2.2

     Verification: dotnet list package --vulnerable returns CLEAN
     Documentation: CVE-2024-0056-RESOLUTION-REPORT.md
     ```
   - Click "Dismiss alert"

4. **Repeat for Alerts #3 and #4** (if present - likely duplicates)

5. **Verify All Dismissed**:
   - Return to Dependabot alerts page
   - Should show: **"0 open alerts"**
   - Check "Closed" tab to see dismissed alerts

---

## Option 3: GitHub API (Advanced)

### Using curl

```bash
# Set your GitHub Personal Access Token
export GITHUB_TOKEN="ghp_your_token_here"

# Fetch all alerts
curl -s -H "Authorization: token $GITHUB_TOKEN" \
  "https://api.github.com/repos/marypas74/InsightLearn_WASM/dependabot/alerts"

# Dismiss alert #1
curl -X PATCH \
  -H "Authorization: token $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  "https://api.github.com/repos/marypas74/InsightLearn_WASM/dependabot/alerts/1" \
  -d '{
    "state": "dismissed",
    "dismissed_reason": "fix_started",
    "dismissed_comment": "Fixed in commit 7988953 - System.Data.SqlClient updated to 4.8.6"
  }'
```

**Create Personal Access Token**:
1. Go to: https://github.com/settings/tokens
2. Click "Generate new token (classic)"
3. Scopes: Select `repo` (full control of private repositories)
4. Click "Generate token"
5. Copy token (save securely - shown only once)

---

## Verification Checklist

After dismissing alerts, verify:

### 1. GitHub Security Dashboard
- [ ] Visit: https://github.com/marypas74/InsightLearn_WASM/security/dependabot
- [ ] Confirm: **"0 open alerts"**
- [ ] Check "Closed" tab: Should show 7 closed alerts

### 2. Local Vulnerability Scan
```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM
dotnet list package --vulnerable --include-transitive
```
- [ ] Expected: "The given project has no vulnerable packages"

### 3. Test Project Packages
```bash
dotnet list tests/InsightLearn.Tests.csproj package | grep -E "SqlClient|BouncyCastle|Logging.Abstractions"
```
- [ ] System.Data.SqlClient: **4.8.6** ✅
- [ ] Microsoft.Data.SqlClient: **5.2.2** ✅
- [ ] BouncyCastle.Cryptography: **2.4.0** ✅
- [ ] Microsoft.Extensions.Logging.Abstractions: **8.0.3** ✅

### 4. Application Project Packages
```bash
dotnet list src/InsightLearn.Application/InsightLearn.Application.csproj package | grep -E "Azure.Storage|BouncyCastle"
```
- [ ] Azure.Storage.Blobs: **12.26.0** ✅
- [ ] BouncyCastle.Cryptography: **2.4.0** ✅

### 5. Build Verification
```bash
dotnet build InsightLearn.WASM.sln
```
- [ ] Build succeeded: **0 Error(s), 0 Warning(s)**

### 6. Git Status
```bash
git status
git log --oneline -5
```
- [ ] Working tree clean
- [ ] Latest commits include security fixes (7988953, d068ce8, 5d5c220, 85e20dc, 9d41903)

---

## Troubleshooting

### Issue: GitHub CLI Not Found
```bash
bash: gh: command not found
```

**Solution**:
```bash
# Rocky Linux / RHEL
sudo dnf install -y gh

# Ubuntu / Debian
sudo apt install gh

# macOS
brew install gh
```

### Issue: Authentication Failed
```
gh auth status
✗ You are not logged into any GitHub hosts
```

**Solution**:
```bash
gh auth login
# Follow prompts to authenticate via browser
```

### Issue: Alerts Still Showing After 48 Hours
**Possible Causes**:
1. GitHub Dependabot scan hasn't run yet (runs every 24h)
2. Package versions not detected correctly
3. Manual dismissal required

**Solution**:
- Wait additional 24 hours for next scan cycle
- Use automated script: `./dismiss-github-alerts.sh`
- Manually dismiss via web UI (see Option 2 above)

### Issue: "No Permission to Dismiss Alerts"
**Error**:
```
Resource not accessible by personal access token
```

**Solution**:
- Verify you have "Write" access to repository
- Check Personal Access Token has `repo` scope
- Contact repository admin (marypas74) for permissions

---

## Expected Timeline

| Time | Event | Status |
|------|-------|--------|
| **2025-11-16 18:00** | All fixes committed and pushed | ✅ Complete |
| **2025-11-16 19:00** | BouncyCastle alerts auto-closed | ✅ Complete |
| **2025-11-17 18:00** | First GitHub Dependabot scan (24h) | ⏳ Pending |
| **2025-11-18 18:00** | Second scan if needed (48h) | ⏳ Pending |
| **After auto-close** | Manual dismissal if needed | 📋 Use this guide |

---

## Summary

### Automated Approach (Recommended)
```bash
# 1. Install GitHub CLI
sudo dnf install -y gh

# 2. Authenticate
gh auth login

# 3. Run dismissal script
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM
./dismiss-github-alerts.sh
```

**Time Required**: ~5 minutes

### Manual Approach
1. Visit GitHub Security Dashboard
2. Click each alert
3. Dismiss with reason "Fix has already started"
4. Add commit reference in comment

**Time Required**: ~10 minutes

---

## Files Reference

| File | Purpose |
|------|---------|
| **SECURITY-FIXES-COMPLETE-REPORT.md** | Complete security fixes documentation |
| **CVE-2024-0056-RESOLUTION-REPORT.md** | CVE-2024-0056 specific resolution |
| **dismiss-github-alerts.sh** | Automated dismissal script |
| **GITHUB-ALERTS-DISMISSAL-GUIDE.md** | This guide |

---

## Support

**Questions or Issues?**
- GitHub Repository: https://github.com/marypas74/InsightLearn_WASM
- Security Dashboard: https://github.com/marypas74/InsightLearn_WASM/security
- Documentation: All files in repository root

**Contact**:
- Repository Owner: marypas74
- Email: marcello.pasqui@gmail.com

---

**Last Updated**: 2025-11-16 19:20:00
**Status**: All vulnerabilities fixed locally, GitHub alerts pending auto-close
