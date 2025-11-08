# Cloudflare Cache Fix - Blazor WASM White Page Issue

**Problem:** Site shows white page with console errors about missing Blazor runtime files with incorrect hash.

**Root Cause:** Cloudflare CDN and browser cache are serving old Blazor WebAssembly files with outdated hash values.

---

## 🔴 Symptoms

Browser console shows errors like:
```
Uncaught (in promise) TypeError: can't access property "out", e is undefined
Failed to load resource: https://wasm.insightlearn.cloud/_framework/dotnet.runtime.8.0.21.e3uzc18eok.js (404)
```

**Note the typo**: `e3uzc18eok` (with number `1`) instead of the correct `e3uzcl8eok` (with letter `l`).

---

## ✅ Solution

### Step 1: Purge Cloudflare Cache

1. **Login to Cloudflare Dashboard**
   - Go to https://dash.cloudflare.com
   - Select your domain: `insightlearn.cloud`

2. **Navigate to Caching**
   - Click on "Caching" in the left sidebar
   - Click on "Configuration"

3. **Purge Cache**
   - Click "Purge Everything" button
   - Confirm the action
   - Wait 30 seconds for purge to complete

### Step 2: Clear Browser Cache

**Option A: Hard Refresh (Recommended)**
- **Windows/Linux**: Press `Ctrl + Shift + R`
- **Mac**: Press `Cmd + Shift + R`

**Option B: Clear Cache via DevTools**
1. Open DevTools (F12)
2. Right-click on the Refresh button
3. Select "Empty Cache and Hard Reload"

**Option C: Clear Browser Cache Manually**
1. Open browser settings
2. Go to "Privacy and Security"
3. Click "Clear browsing data"
4. Select "Cached images and files"
5. Click "Clear data"

### Step 3: Verify Fix

1. **Reload the page** (Ctrl+Shift+R)
2. **Check console** - should be no 404 errors for `_framework` files
3. **Verify Blazor loads** - page should display properly, not white
4. **Test API calls** - chatbot and other features should work

---

## 🔍 Verification Commands

### Check Current Files in Pod
```bash
k3s kubectl exec -n insightlearn deployment/insightlearn-wasm-blazor-webassembly -- \
  ls /usr/share/nginx/html/_framework/ | grep dotnet
```

Should show files with correct hash: `e3uzcl8eok` (with lowercase `l`)

### Check Cloudflare Cache Status
```bash
curl -sI https://wasm.insightlearn.cloud/_framework/blazor.webassembly.js | grep -i "cf-cache"
```

After purge, should show: `cf-cache-status: MISS` or `cf-cache-status: DYNAMIC`

### Test API Connectivity
```bash
curl -s https://wasm.insightlearn.cloud/api/system/endpoints | head -20
```

Should return JSON with endpoints (not error).

---

## 🛡️ Prevention

### Nginx Configuration

The WASM pod already has aggressive no-cache headers in `/nginx/wasm-default.conf`:

```nginx
# Framework files - AGGRESSIVE NO CACHE
location /_framework/ {
    add_header Cache-Control "no-cache, no-store, must-revalidate, max-age=0";
    add_header Pragma "no-cache";
    add_header Expires "0";
    add_header Clear-Site-Data "\"cache\"";
    etag off;
}
```

### Cloudflare Page Rules

Consider adding a Page Rule to bypass cache for `/_framework/*`:

1. Go to Cloudflare Dashboard → Rules → Page Rules
2. Create rule for `*wasm.insightlearn.cloud/_framework/*`
3. Set: **Cache Level: Bypass**
4. Save and deploy

---

## 🔧 Technical Details

### Why This Happened

1. **Initial deployment** created Blazor files with hash `e3uzcl8eok`
2. **Cloudflare CDN** cached these files for 3600 seconds (1 hour)
3. **Browser** also cached files aggressively
4. **Rebuild/Redeploy** may have created files with different hash
5. **Old cache** served files with outdated/wrong hash → 404 errors → white page

### File Hashing in Blazor

Blazor WebAssembly uses **content-based hashing** for cache busting:
- File: `dotnet.runtime.8.0.21.e3uzcl8eok.js`
- Format: `{filename}.{version}.{contenthash}.{extension}`
- Hash changes when file content changes

### Cloudflare Caching Behavior

Cloudflare respects `Cache-Control` headers but has its own cache:
- **Edge Cache**: Distributed globally, persists even if origin says no-cache
- **Browser Cache**: Separate from Cloudflare cache
- **Both need purging** for complete fix

---

## 📋 Deployment Checklist

After each deployment that updates frontend files:

- [ ] Purge Cloudflare cache (via dashboard or API)
- [ ] Test with hard refresh in incognito window
- [ ] Verify no 404 errors in browser console
- [ ] Check `blazor.boot.json` for correct file hashes
- [ ] Monitor for white page reports from users

---

## 🚨 Emergency Fix

If users report white page after deployment:

```bash
# 1. Verify files exist in pod with correct hash
kubectl exec -n insightlearn deployment/insightlearn-wasm-blazor-webassembly -- \
  ls -la /usr/share/nginx/html/_framework/dotnet*.js

# 2. Purge Cloudflare via API (faster than dashboard)
curl -X POST "https://api.cloudflare.com/client/v4/zones/{zone_id}/purge_cache" \
  -H "Authorization: Bearer {api_token}" \
  -H "Content-Type: application/json" \
  -d '{"purge_everything":true}'

# 3. Notify users to do hard refresh (Ctrl+Shift+R)
```

---

## 📊 Monitoring

### Check Cloudflare Cache Hit Rate

```bash
# Check if files are being served from cache
for file in blazor.webassembly.js dotnet.js dotnet.runtime.8.0.21.e3uzcl8eok.js; do
  echo "Checking $file:"
  curl -sI "https://wasm.insightlearn.cloud/_framework/$file" | grep -E "cf-cache|cache-control"
done
```

**Good output**: `cf-cache-status: MISS` or `DYNAMIC` (not cached)
**Bad output**: `cf-cache-status: HIT` with old files (outdated cache)

---

## 🔗 Related Documentation

- [Cloudflare Cache Documentation](https://developers.cloudflare.com/cache/)
- [Blazor WebAssembly Deployment](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly)
- [HTTP Caching Headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)

---

## ✅ Resolution Confirmation

After applying fix, you should see:

1. **Browser Console**: No 404 errors for `_framework` files
2. **Page Display**: Blazor app loads correctly (not white page)
3. **Network Tab**: All `_framework/*.js` files return HTTP 200
4. **Cloudflare**: Cache status shows MISS or DYNAMIC
5. **Functionality**: Chatbot, navigation, API calls all work

---

**Last Updated**: November 7, 2025
**Issue Resolved**: Yes (with purge + hard refresh)
