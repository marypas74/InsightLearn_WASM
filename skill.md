# SKILL.md - Claude Code Learned Competencies Master File

> **Purpose**: This file documents all competencies, patterns, troubleshooting solutions, and best practices learned during development of InsightLearn WASM.
>
> **Last Updated**: 2025-12-24
> **Version**: 1.0.0

---

## Table of Contents

1. [Kubernetes & K3s](#kubernetes--k3s)
2. [CSS & Frontend](#css--frontend)
3. [Blazor WebAssembly](#blazor-webassembly)
4. [Docker & Podman](#docker--podman)
5. [GDPR & Cookie Consent](#gdpr--cookie-consent)
6. [Troubleshooting Patterns](#troubleshooting-patterns)
7. [Performance Optimization](#performance-optimization)
8. [SEO & Indexing](#seo--indexing)

---

## Kubernetes & K3s

### K3s Image Management

**Problem**: K3s uses containerd, not Docker. Standard `docker load` doesn't work.

**Solution**:
```bash
# Export image from podman/docker
podman save localhost/image:tag -o /tmp/image.tar

# Import to K3s containerd (requires sudo)
sudo /usr/local/bin/k3s ctr images import /tmp/image.tar
```

### Deployment Update Pattern

**Best Practice**: Always increment version before deploy to force pod recreation.

```bash
# 1. Increment version in Directory.Build.props
# 2. Build with new tag
podman build -t localhost/insightlearn/wasm:X.X.X-dev .
# 3. Export and import
podman save ... && sudo k3s ctr images import ...
# 4. Update deployment image
kubectl set image deployment/NAME -n NAMESPACE container=localhost/image:X.X.X-dev
# 5. Wait for rollout
kubectl rollout status deployment/NAME -n NAMESPACE --timeout=120s
```

### Node Affinity Issues

**Problem**: After node name change, PersistentVolumes fail with "didn't match node affinity".

**Solution**: PV nodeAffinity is immutable - must delete and recreate PV with new node name, preserving data path.

---

## CSS & Frontend

### Flash of Unstyled Content (FOUC) on Chrome

**Problem**: CSS loaded with `media="print" onload="this.media='all'"` causes layout shift on Chrome.

**Root Cause**: Deferred CSS loading means critical styles aren't applied immediately.

**Solution**: Move critical CSS from deferred section to synchronous loading:

```html
<!-- CRITICAL section (loads synchronously) -->
<link rel="stylesheet" href="css/header-professional.css" />

<!-- DEFERRED section (loads async) -->
<!-- Move non-critical CSS here -->
<link rel="stylesheet" href="css/footer.css" media="print" onload="this.media='all'" />
```

### Mobile Navigation Disappearing

**Problem**: Nav items (Login, Sign Up, Categories) hidden on mobile.

**Root Cause**: Overly aggressive `display: none !important` in media query.

**Solution**: Use `display: flex !important` with compact styling instead:

```css
@media (max-width: 768px) {
    /* DON'T */
    .auth-buttons { display: none !important; }

    /* DO */
    .auth-buttons {
        display: flex !important;
        padding: 4px !important;
        gap: 6px !important;
    }
    .auth-buttons .btn {
        padding: 6px 12px !important;
        font-size: 13px !important;
    }
}
```

### Dropdown Overflow on Mobile

**Problem**: Fixed-width dropdown (400px) overflows on small screens.

**Solution**: Use viewport-relative sizing with max-width constraint:

```css
@media (max-width: 768px) {
    .dropdown-menu {
        min-width: auto !important;
        width: calc(100vw - 32px) !important;
        max-width: 350px !important;
        left: 50% !important;
        transform: translateX(-50%) !important;
        max-height: 70vh !important;
        overflow-y: auto !important;
    }
}
```

### CSS Specificity Wars

**Learned Pattern**: When overriding framework styles, use same or higher specificity:

```css
/* If framework uses: */
.component { display: none; }

/* Override with same selector + !important: */
.component { display: flex !important; }

/* Or higher specificity: */
.parent .component { display: flex; }
```

---

## Blazor WebAssembly

### Component State Not Updating

**Problem**: UI doesn't reflect state changes after async operations.

**Solution**: Always call `StateHasChanged()` after modifying state:

```csharp
private async Task SaveConsent()
{
    _isVisible = false;
    StateHasChanged();  // Force UI update

    await JSRuntime.InvokeVoidAsync("someFunction");
    StateHasChanged();  // After async operation too
}
```

### JS Interop for DOM Manipulation

**Pattern**: Use `eval` for quick inline scripts (not recommended for production):

```csharp
await JSRuntime.InvokeVoidAsync("eval", @"
    document.querySelectorAll('.overlay').forEach(el => {
        el.style.display = 'none';
        el.classList.add('hidden');
    });
");
```

**Better Pattern**: Create separate JS file with named functions.

---

## Docker & Podman

### Podman vs Docker Differences

| Feature | Docker | Podman |
|---------|--------|--------|
| Daemon | Required | Daemonless |
| Root | Default | Rootless default |
| Image format | OCI/Docker | OCI (HEALTHCHECK warning) |
| Compose | docker-compose | podman-compose |

### Build Cache Issues

**Problem**: Old layers cached, changes not reflected.

**Solution**:
```bash
# Clean build cache
podman builder prune -a

# Build with no cache
podman build --no-cache -t image:tag .
```

---

## GDPR & Cookie Consent

### Banner Not Hiding After Accept/Reject

**Problem**: CSS `.hidden` class not overriding inline `display` style.

**Solution**: Use multiple selectors with maximum specificity:

```css
/* Target both class and inline style */
.cookie-wall-overlay.hidden,
.cookie-wall-overlay[style*="display: none"] {
    display: none !important;
    visibility: hidden !important;
    opacity: 0 !important;
    pointer-events: none !important;
    height: 0 !important;
    overflow: hidden !important;
}
```

### Multiple Cookie Walls (Static + Blazor)

**Pattern**: Hide both static HTML wall and Blazor component:

```csharp
await JSRuntime.InvokeVoidAsync("eval", @"
    // Hide static wall
    var staticWall = document.getElementById('static-cookie-wall');
    if (staticWall) {
        staticWall.style.display = 'none';
        staticWall.classList.add('hidden');
    }
    // Hide Blazor walls
    document.querySelectorAll('.cookie-wall-overlay').forEach(el => {
        el.style.display = 'none';
        el.classList.add('hidden');
    });
    // Restore body scroll
    document.body.style.overflow = '';
");
```

---

## Troubleshooting Patterns

### Diagnostic Checklist

1. **CSS not applying?**
   - Check browser DevTools → Styles panel
   - Look for crossed-out rules (lower specificity)
   - Check media queries (resize window)
   - Verify CSS file is loaded (Network tab)

2. **Pod not starting?**
   - `kubectl describe pod <name> -n <namespace>`
   - Check Events section for errors
   - `kubectl logs <pod> -n <namespace>`
   - Check image exists: `sudo k3s ctr images ls | grep image`

3. **Layout broken on specific browser?**
   - Test in incognito (no cache)
   - Check CSS loading order
   - Move critical CSS to synchronous loading

### Browser Cache Busting

**Problem**: Old CSS/JS cached despite changes.

**Solutions**:
1. Hard refresh: Ctrl+Shift+R (Windows) / Cmd+Shift+R (Mac)
2. Add version query string: `style.css?v=2.3.8`
3. Clear browser cache in DevTools

---

## Performance Optimization

### CSS Loading Strategy

| CSS Type | Loading Method | Use Case |
|----------|---------------|----------|
| Critical (above-fold) | Inline `<style>` | Header, hero section |
| Essential (layout) | Synchronous `<link>` | Framework, navigation |
| Non-critical | Deferred `media="print"` | Footer, modals, pages |

### Image Optimization

```html
<!-- Hero/LCP image -->
<img fetchpriority="high" loading="eager" decoding="sync"
     src="hero.webp" width="1200" height="600" alt="..." />

<!-- Below-fold images -->
<img loading="lazy" decoding="async"
     src="image.webp" width="400" height="300" alt="..." />
```

---

## SEO & Indexing

### IndexNow Instant Indexing

**Purpose**: Notify Bing/Yandex immediately when pages change.

```bash
curl -X POST "https://api.indexnow.org/indexnow" \
  -H "Content-Type: application/json" \
  -d '{
    "host": "www.insightlearn.cloud",
    "key": "your-indexnow-key",
    "urlList": ["https://www.insightlearn.cloud/page1", ...]
  }'
```

### Pre-rendering for Crawlers

**Problem**: Blazor WASM renders client-side, crawlers see empty HTML.

**Solution**: Nginx crawler detection serving static snapshots:

```nginx
map $http_user_agent $is_crawler {
    default 0;
    ~*googlebot 1;
    ~*bingbot 1;
}

location = / {
    if ($is_crawler) {
        rewrite ^ /seo-snapshots/index.html break;
    }
    try_files $uri /index.html;
}
```

---

## Quick Reference Commands

### Kubernetes

```bash
# Pod status
kubectl get pods -n insightlearn

# Logs
kubectl logs -n insightlearn <pod-name>

# Describe (for events/errors)
kubectl describe pod -n insightlearn <pod-name>

# Rollout
kubectl rollout restart deployment/<name> -n insightlearn
kubectl rollout status deployment/<name> -n insightlearn

# Image update
kubectl set image deployment/<name> -n insightlearn container=image:tag
```

### Build & Deploy

```bash
# Podman build
podman build -f Dockerfile.wasm -t localhost/insightlearn/wasm:X.X.X-dev .

# Export to tar
podman save localhost/insightlearn/wasm:X.X.X-dev -o /tmp/wasm.tar

# Import to K3s
sudo /usr/local/bin/k3s ctr images import /tmp/wasm.tar

# Verify
sudo k3s ctr images ls | grep insightlearn
```

---

## Learning Resources

- [MDN CSS Specificity](https://developer.mozilla.org/en-US/docs/Web/CSS/Specificity)
- [K3s Documentation](https://docs.k3s.io/)
- [Blazor WebAssembly Docs](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [PageSpeed Insights](https://pagespeed.web.dev/)

---

## Changelog

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-24 | 1.0.0 | Initial version with K8s, CSS, GDPR, Blazor patterns |

---

*This document is continuously updated as new competencies are acquired.*
