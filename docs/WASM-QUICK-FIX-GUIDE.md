# WASM Homepage Fix - Quick Reference Guide
## Frontend Developer Fast-Track Instructions

**Status:** CRITICAL - Homepage is using wrong design
**Time to Fix:** ~1 hour
**Difficulty:** Medium

---

## TL;DR - What's Wrong

The WASM site is using a completely different homepage design than Production:

- **Production:** Professional design with `HeroSection`, `CourseCard`, `CategoryGrid` components
- **WASM:** Simplified emoji-based design with plain HTML

**Root Cause:** Wrong `Index.razor` + missing components + missing 2 CSS files + missing 10 JS files

---

## Quick Fix (Copy-Paste Commands)

### Step 1: Copy All Missing Files (2 minutes)

```bash
cd /home/mpasqui/kubernetes/Insightlearn

# Copy missing CSS files
cp src/InsightLearn.Web/wwwroot/css/main-style.css src/InsightLearn.WebAssembly/wwwroot/css/
cp src/InsightLearn.Web/wwwroot/css/learnupon-design-system.css src/InsightLearn.WebAssembly/wwwroot/css/
cp src/InsightLearn.Web/wwwroot/css/system-health-animations.css src/InsightLearn.WebAssembly/wwwroot/css/

# Copy all JS files
mkdir -p src/InsightLearn.WebAssembly/wwwroot/js
cp src/InsightLearn.Web/wwwroot/js/*.js src/InsightLearn.WebAssembly/wwwroot/js/

# Copy all components
mkdir -p src/InsightLearn.WebAssembly/Components
cp -r src/InsightLearn.Web/Components/* src/InsightLearn.WebAssembly/Components/

# Backup and replace Index.razor
cp src/InsightLearn.WebAssembly/Pages/Index.razor src/InsightLearn.WebAssembly/Pages/Index.razor.backup
cp src/InsightLearn.Web/Pages/Index.razor src/InsightLearn.WebAssembly/Pages/Index.razor

echo "✅ Files copied successfully!"
```

### Step 2: Update index.html (5 minutes)

**File:** `/home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.WebAssembly/wwwroot/index.html`

**Add after line 10 (after Bootstrap):**
```html
<!-- Font Awesome -->
<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet" />
```

**Add after line 34 (before Scoped Styles):**
```html
<!-- Main Style CSS -->
<link rel="stylesheet" href="css/main-style.css" />
<link rel="stylesheet" href="css/learnupon-design-system.css" />
<link rel="stylesheet" href="css/system-health-animations.css" />
```

**Add after line 37 (after Scoped Styles):**
```html
<!-- AOS Library -->
<link href="https://unpkg.com/aos@2.3.1/dist/aos.css" rel="stylesheet" />
```

**Add before `</body>` (after line 59):**
```html
<!-- Bootstrap JS -->
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>

<!-- Site JavaScript -->
<script src="js/site.js"></script>
<script src="js/header-clean.js"></script>
<script src="js/connection-handler.js"></script>
<script src="js/error-monitoring.js"></script>
<script src="js/google-auth.js"></script>
<script src="js/google-auth-new.js"></script>
<script src="js/google-identity-services.js"></script>
<script src="js/google-signin-button.js"></script>
<script src="js/google-signin-simple.js"></script>
<script src="js/download-utils.js"></script>
<script src="js/enterprise-charts.js"></script>

<!-- AOS Init -->
<script src="https://unpkg.com/aos@2.3.1/dist/aos.js"></script>
<script>
    document.addEventListener('DOMContentLoaded', function() {
        AOS.init({
            duration: 800,
            easing: 'ease-out-cubic',
            once: true,
            offset: 50
        });
    });
</script>
```

### Step 3: Rebuild & Deploy (10 minutes)

```bash
cd /home/mpasqui/kubernetes/Insightlearn

# Clean and build
dotnet clean src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj
dotnet build src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj --configuration Release

# Publish
dotnet publish src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj \
  --configuration Release \
  --output ./publish/wasm

# Build Docker image
docker build -f Dockerfile.wasm -t insightlearn/wasm:latest .

# Deploy to minikube
minikube image rm insightlearn/wasm:latest
minikube image load insightlearn/wasm:latest

# Restart deployment
kubectl rollout restart deployment/insightlearn-wasm -n insightlearn
kubectl wait --for=condition=ready pod -l app=insightlearn-wasm -n insightlearn --timeout=120s

echo "✅ Deployment complete! Test at http://192.168.49.2:31090"
```

### Step 4: Verify (5 minutes)

**Open Browser:**
1. Go to `http://192.168.49.2:31090`
2. Press `F12` → Network Tab
3. Refresh page with `Ctrl+Shift+R`

**Check CSS Files Load (Status 200):**
- ✅ main-style.css
- ✅ learnupon-design-system.css
- ✅ homepage-modern.css
- ✅ homepage-enhancements.css

**Visual Verification:**
- ✅ Hero section has gradient background
- ✅ Animated stats counter
- ✅ Course cards show images
- ✅ Instructor avatars visible
- ✅ Company logos appear
- ✅ Smooth scroll animations

---

## Troubleshooting

### Issue: CSS Files Return 404

**Solution:**
```bash
# Verify files exist
ls -la src/InsightLearn.WebAssembly/wwwroot/css/main-style.css
ls -la src/InsightLearn.WebAssembly/wwwroot/css/learnupon-design-system.css

# If missing, copy again
cp src/InsightLearn.Web/wwwroot/css/main-style.css src/InsightLearn.WebAssembly/wwwroot/css/
cp src/InsightLearn.Web/wwwroot/css/learnupon-design-system.css src/InsightLearn.WebAssembly/wwwroot/css/
```

### Issue: Components Not Found

**Solution:**
```bash
# Verify components exist
ls -la src/InsightLearn.WebAssembly/Components/

# Should see:
# - HeroSection.razor
# - CourseCard.razor
# - CategoryGrid.razor

# If missing, copy again
cp -r src/InsightLearn.Web/Components/* src/InsightLearn.WebAssembly/Components/
```

### Issue: Page Still Looks Wrong After Deploy

**Solution:**
```bash
# 1. Clear browser cache
# Press: Ctrl+Shift+R

# 2. Clear Docker cache
docker rmi insightlearn/wasm:latest
docker build --no-cache -f Dockerfile.wasm -t insightlearn/wasm:latest .

# 3. Force reload in minikube
minikube image rm insightlearn/wasm:latest
minikube image load insightlearn/wasm:latest

# 4. Delete pod (forces fresh start)
kubectl delete pod -l app=insightlearn-wasm -n insightlearn
kubectl wait --for=condition=ready pod -l app=insightlearn-wasm -n insightlearn --timeout=120s
```

### Issue: Build Errors

**Common Errors:**

**Error:** `The type or namespace name 'HeroSection' could not be found`
**Solution:** Components not copied. Run Step 1 again.

**Error:** `Could not find a part of the path`.css
**Solution:** CSS files not copied. Run Step 1 again.

**Error:** `System.IO.FileNotFoundException: Could not find file 'wwwroot/js/site.js'`
**Solution:** JS files not copied. Run Step 1 again.

---

## Verification Checklist

After completing all steps, verify:

- [ ] Files copied successfully (Step 1)
- [ ] index.html updated (Step 2)
- [ ] Build completed without errors (Step 3)
- [ ] Docker image built successfully (Step 3)
- [ ] Deployment completed (Step 3)
- [ ] CSS files load (Status 200) (Step 4)
- [ ] Hero section has gradient (Step 4)
- [ ] Course cards show images (Step 4)
- [ ] Animations work on scroll (Step 4)

---

## Files Changed

### New Files Created:
- `src/InsightLearn.WebAssembly/wwwroot/css/main-style.css`
- `src/InsightLearn.WebAssembly/wwwroot/css/learnupon-design-system.css`
- `src/InsightLearn.WebAssembly/wwwroot/css/system-health-animations.css`
- `src/InsightLearn.WebAssembly/wwwroot/js/*.js` (10+ files)
- `src/InsightLearn.WebAssembly/Components/*.razor` (all components)

### Modified Files:
- `src/InsightLearn.WebAssembly/wwwroot/index.html`
- `src/InsightLearn.WebAssembly/Pages/Index.razor`

### Backup Created:
- `src/InsightLearn.WebAssembly/Pages/Index.razor.backup`

---

## Need Help?

See full detailed report: `/home/mpasqui/kubernetes/Insightlearn/WASM-UX-COMPARISON-REPORT.md`

**Created:** 2025-10-22
**Author:** Senior UX Designer (Claude)
