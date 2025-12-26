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

**Solution 1 - Move to Synchronous Loading**:
```html
<!-- CRITICAL section (loads synchronously) -->
<link rel="stylesheet" href="css/header-professional.css" />

<!-- DEFERRED section (loads async) -->
<link rel="stylesheet" href="css/footer.css" media="print" onload="this.media='all'" />
```

**Solution 2 - Visibility Hidden Pattern** (✅ RECOMMENDED for Blazor WASM):
```html
<head>
    <!-- Hide page until CSS loads -->
    <style>html{visibility:hidden;opacity:0;}</style>
</head>
<body>
    <!-- At end of body, show page after CSS is ready -->
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            document.documentElement.style.visibility = 'visible';
            document.documentElement.style.opacity = '1';
        });
    </script>
</body>
```

**Solution 3 - Inline Critical CSS**:
```html
<head>
    <style>
        /* Inline critical above-the-fold CSS */
        .main-header { position: sticky; top: 0; background: #fff; }
        .hero { min-height: 500px; background: linear-gradient(...); }
    </style>
</head>
```

**Solution 4 - Preload Critical Resources**:
```html
<link rel="preload" href="css/header-professional.css" as="style" />
<link rel="preload" href="fonts/inter.woff2" as="font" type="font/woff2" crossorigin />
```

**Blazor WASM Specific (dotnet/aspnetcore#26571)**:
- Blazor WASM initializes after DOM is parsed
- CSS loading can race with Blazor initialization
- Use visibility:hidden pattern + show in Blazor OnAfterRender
- Or use CSS animation: `html { animation: fadeIn 0.3s ease-in forwards; }`

**Best Practice Checklist**:
1. ✅ Critical CSS (header, hero, layout) - Synchronous `<link>`
2. ✅ Non-critical CSS (footer, modals) - Deferred `media="print"`
3. ✅ Visibility hidden fallback for race conditions
4. ✅ Preload fonts and critical resources
5. ✅ Test in incognito mode (no cache)

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

## CSS Grid & Flexbox Text Truncation Fix

**Problem**: Text in CSS Grid cells gets truncated (e.g., "Personal Growth" → "Personal Growt").

**Root Cause**: CSS Grid has `min-width: auto` by default, which prevents grid items from shrinking below their content size.

### When You Want Grid Items to SHRINK (Original Use Case)

**Solution 1 - On Grid Items**:
```css
.grid-item {
    min-width: 0; /* Critical: allows shrinking below content size */
}
```

**Solution 2 - At Grid Definition Level**:
```css
.grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) minmax(0, 1fr);
}
```

### When You Want Grid Items to EXPAND to Fit Content (v2.3.11 Fix)

**⚠️ IMPORTANT LESSON**: `min-width: 0` SHRINKS content, it does NOT EXPAND containers!

If you want text to be fully visible without truncation, you need:
1. Make the container WIDER
2. Use `width: max-content` to expand to fit content

**Correct Solution for Dropdowns/Menus**:
```css
/* Expand container to fit content */
.dropdown-menu.categories-menu {
    min-width: 680px !important;
    width: max-content !important;  /* KEY: expands to fit all content */
    max-width: 720px !important;
}

/* Grid columns with minimum width */
.categories-grid {
    grid-template-columns: repeat(3, minmax(200px, 1fr)) !important;
}

/* Items should expand, not shrink */
.category-item {
    min-width: max-content !important;
    width: auto !important;
}

/* Prevent text truncation */
.category-label {
    white-space: nowrap !important;
    overflow: visible !important;
    text-overflow: unset !important;
}
```

**Key Insight**:
- `min-width: 0` = allows SHRINKING below intrinsic content size
- `width: max-content` = EXPANDS to fit all content
- These are OPPOSITE behaviors - choose the right one for your use case!

**Sources**:
- [CSS-Tricks: Preventing a Grid Blowout](https://css-tricks.com/preventing-a-grid-blowout/)
- [Defensive CSS: Grid min-content-size](https://defensivecss.dev/tip/grid-min-content-size/)
- [MDN: width - max-content](https://developer.mozilla.org/en-US/docs/Web/CSS/max-content)

---

## Mobile Hamburger Menu Visibility in Blazor WASM

**Problem**: Mobile hamburger menu opens but shows no navigation items (empty panel).

**Root Cause**: Blazor WASM doesn't include Bootstrap JavaScript. Menu toggle must be handled in C# code block. Additionally, CSS rules with `display: none !important` may hide menu content.

**Solution 1 - C# Toggle in Razor**:
```razor
@code {
    private bool isMobileMenuOpen = false;

    private void ToggleMobileMenu()
    {
        isMobileMenuOpen = !isMobileMenuOpen;
    }
}

<button @onclick="ToggleMobileMenu" class="mobile-menu-toggle">
    <i class="fas fa-bars"></i>
</button>

<nav class="mobile-menu-panel @(isMobileMenuOpen ? "open" : "")">
    <!-- Menu content -->
</nav>
```

**Solution 2 - CSS Override with MAXIMUM Specificity (v2.3.11 Fix)**:

⚠️ **CRITICAL LESSON**: When CSS specificity wars occur, use **element + class** selector for maximum specificity:

```css
/* WRONG - Class alone may be overridden */
.mobile-menu-panel .mobile-nav-link {
    display: flex !important;
}

/* CORRECT - Element + Class for higher specificity */
nav.mobile-menu-panel .mobile-nav-link {
    display: flex !important;
    visibility: visible !important;  /* Add visibility! */
    opacity: 1 !important;           /* Add opacity! */
}

/* Also override for open state explicitly */
nav.mobile-menu-panel.open .mobile-nav-section {
    display: block !important;
    visibility: visible !important;
    opacity: 1 !important;
}

nav.mobile-menu-panel.open .mobile-nav-link {
    display: flex !important;
    visibility: visible !important;
    opacity: 1 !important;
    min-height: 48px !important;  /* Touch target size */
}
```

**Key Insight**: `display: flex !important` alone is NOT enough! Some CSS rules may hide elements using:
- `visibility: hidden`
- `opacity: 0`
- `height: 0` / `max-height: 0`

Always override ALL THREE visibility properties: `display`, `visibility`, AND `opacity`.

**Sources**:
- [Blazor Bootstrap hamburger issue](https://github.com/dotnet/aspnetcore/issues/16370)
- [Jon Hilton: Make a Responsive Navbar with Blazor](https://jonhilton.net/responsive-blazor-navbar/)
- [MDN: CSS Specificity](https://developer.mozilla.org/en-US/docs/Web/CSS/Specificity)

---

## Flexbox Mobile Header Centering

**Problem**: Logo and auth buttons not properly aligned in mobile header.

**Solution**: Use proper flexbox alignment with `space-between`:
```css
@media (max-width: 768px) {
    .main-header-container {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0 12px;
    }

    .header-logo-section {
        flex: 0 0 auto;
    }

    .auth-buttons {
        flex: 0 0 auto;
        gap: 8px;
    }

    .mobile-menu-toggle {
        order: -1; /* Place hamburger first (left side) */
        margin-right: 8px;
    }
}
```

**Best Practices**:
- Use `flex: 0 0 auto` for elements that should not grow/shrink
- Use `order: -1` to reposition elements visually
- Touch targets should be at least 44x44px (WCAG)

**Sources**:
- [Ahmad Shadeed: Building Website Headers with CSS Flexbox](https://ishadeed.com/article/website-headers-flexbox/)
- [Envato Tuts+: How to Build a Responsive Navigation Bar with Flexbox](https://webdesign.tutsplus.com/how-to-build-a-responsive-navigation-bar-with-flexbox--cms-33535t)

---

## Grafana GeoIP Geolocation Tracking

### Problema: Dashboard mostra solo IP del server

**Sintomo**: La mappa geografica in Grafana mostra solo un paese (es. Spain 100%) invece dei client reali.

**Root Cause**: Con Cloudflare Tunnel, il traffico arriva da localhost (127.0.0.1), quindi:
- Nginx vede `$remote_addr` = 127.0.0.1 (cloudflared)
- L'IP reale del client è nell'header `CF-Connecting-IP`
- Il paese del client è nell'header `CF-IPCountry`

### Soluzione 1: Usare Header Cloudflare (Consigliata) ✅

Cloudflare fornisce già la geolocalizzazione nei suoi headers:
- `CF-Connecting-IP`: IP reale del client
- `CF-IPCountry`: Codice paese ISO (es. IT, US, DE)
- `CF-IPCity`: Città del client
- `CF-IPContinent`: Continente
- `CF-IPLatitude`/`CF-IPLongitude`: Coordinate

**Nginx Config per Log con GeoIP Cloudflare**:
```nginx
# Log format con header Cloudflare GeoIP
log_format cloudflare_geoip escape=json '{'
    '"time":"$time_iso8601",'
    '"remote_addr":"$remote_addr",'
    '"cf_connecting_ip":"$http_cf_connecting_ip",'
    '"cf_ipcountry":"$http_cf_ipcountry",'
    '"cf_ipcity":"$http_cf_ipcity",'
    '"request_uri":"$request_uri",'
    '"status":"$status",'
    '"request_time":"$request_time",'
    '"user_agent":"$http_user_agent"'
'}';

access_log /var/log/nginx/access_geoip.log cloudflare_geoip;
```

**Processing con Promtail/Loki**:

Aggiungi al `promtail-config.yaml`:
```yaml
scrape_configs:
  - job_name: nginx_geoip
    static_configs:
      - targets:
          - localhost
        labels:
          job: nginx
          __path__: /var/log/nginx/access_geoip.log
    pipeline_stages:
      - json:
          expressions:
            cf_ipcountry: cf_ipcountry
            cf_ipcity: cf_ipcity
            cf_connecting_ip: cf_connecting_ip
            status: status
      - labels:
          country: cf_ipcountry
          city: cf_ipcity
```

### Soluzione 2: GeoIP con Promtail + MaxMind Database

Se vuoi geolocalizzare gli IP in autonomia (senza dipendere da Cloudflare):

1. **Scarica database GeoLite2**:
```bash
# Registrati su MaxMind e scarica GeoLite2-City.mmdb
# https://www.maxmind.com/en/geolite2/signup
wget "https://download.maxmind.com/app/geoip_download?edition_id=GeoLite2-City&license_key=YOUR_KEY&suffix=tar.gz"
```

2. **Config Promtail con GeoIP stage**:
```yaml
pipeline_stages:
  - regex:
      expression: '(?P<ip>\d+\.\d+\.\d+\.\d+)'
  - geoip:
      db: "/etc/promtail/GeoLite2-City.mmdb"
      source: ip
      db_type: "city"
```

**Sources**:
- [Promtail GeoIP Stage](https://grafana.com/docs/loki/latest/send-data/promtail/stages/geoip/)
- [Building the Eye of Sauron: Enriching Nginx logs with GeoIP](https://medium.com/devopsturkiye/building-the-eye-of-sauron-enriching-nginx-logs-with-geoip-106053bc8057)
- [NGINX + Grafana: Map Client Locations in Real-Time](https://medium.com/@solanki.kishan007/nginx-grafana-map-client-locations-in-real-time-7cd20965409d)

### Soluzione 3: Elasticsearch Ingest Pipeline (Per stack ELK)

Elasticsearch ha già il GeoIP processor built-in:

```json
PUT _ingest/pipeline/nginx-geoip
{
  "description": "Add geoip info",
  "processors": [
    {
      "geoip": {
        "field": "client_ip",
        "target_field": "geoip",
        "ignore_missing": true
      }
    }
  ]
}
```

**Sources**:
- [Using the geoip processor in a pipeline](https://www.elastic.co/guide/en/elasticsearch/plugins/current/using-ingest-geoip.html)
- [GeoIP in the Elastic Stack](https://www.elastic.co/blog/geoip-in-the-elastic-stack)

### Cloudflare Tunnel: Ottenere Real Client IP

**Problema**: Con cloudflared, `$remote_addr` = 127.0.0.1

**Soluzione Nginx**:
```nginx
# Trust localhost (cloudflared connection)
set_real_ip_from 127.0.0.1;

# Use Cloudflare header for real IP
real_ip_header CF-Connecting-IP;
```

**⚠️ Security Warning**: `CF-Connecting-IP` è sicuro solo se:
- Il traffico arriva SOLO da Cloudflare Tunnel
- Il server non è esposto direttamente a Internet
- Con Cloudflare Tunnel su localhost, è intrinsecamente sicuro

**Sources**:
- [Restoring original visitor IPs - Cloudflare Docs](https://developers.cloudflare.com/support/troubleshooting/restoring-visitor-ips/restoring-original-visitor-ips/)
- [Real IP using argo tunnel - Cloudflare Community](https://community.cloudflare.com/t/real-ip-using-argo-tunnel-and-nginx-proxy-manager/355271)

---

## Monitoraggio Spazio Disco con Prometheus

### Query PromQL per Disk Space

```promql
# Spazio usato in percentuale
(node_filesystem_size_bytes{mountpoint="/"} - node_filesystem_avail_bytes{mountpoint="/"})
/ node_filesystem_size_bytes{mountpoint="/"} * 100

# Spazio disponibile in GB
node_filesystem_avail_bytes{mountpoint="/"} / 1024 / 1024 / 1024

# Spazio totale in GB
node_filesystem_size_bytes{mountpoint="/"} / 1024 / 1024 / 1024
```

### Dashboard Disk Space Panels

**Panel 1 - Disk Usage Gauge**:
```json
{
  "type": "gauge",
  "title": "Root Filesystem Usage",
  "targets": [{
    "expr": "(1 - node_filesystem_avail_bytes{mountpoint=\"/\"} / node_filesystem_size_bytes{mountpoint=\"/\"}) * 100",
    "legendFormat": "Used %"
  }],
  "fieldConfig": {
    "defaults": {
      "unit": "percent",
      "min": 0,
      "max": 100,
      "thresholds": {
        "steps": [
          { "value": 0, "color": "green" },
          { "value": 70, "color": "yellow" },
          { "value": 85, "color": "orange" },
          { "value": 95, "color": "red" }
        ]
      }
    }
  }
}
```

**Panel 2 - Disk Space Table**:
```promql
# Multiple partitions
node_filesystem_size_bytes - node_filesystem_free_bytes
```

### Node Exporter Disk Metrics

Metriche disponibili:
- `node_filesystem_size_bytes`: Dimensione totale
- `node_filesystem_free_bytes`: Spazio libero (include reserved)
- `node_filesystem_avail_bytes`: Spazio disponibile per utenti (usa questo!)
- `node_filesystem_files`: Inodes totali
- `node_filesystem_files_free`: Inodes liberi

**Best Practice**: Usa `node_filesystem_avail_bytes` invece di `free_bytes` per evitare discrepanze con `df -h`.

**Sources**:
- [Monitoring Disk Space Across Servers Using Node Exporter](https://omarghader.github.io/monitor-disk-space-node-exporter-prometheus-grafana/)
- [Node Exporter Disk Graphs - Grafana Dashboard](https://grafana.com/grafana/dashboards/9852-stians-disk-graphs/)
- [Node Exporter Full Dashboard (ID: 1860)](https://grafana.com/grafana/dashboards/1860)

---

## Learning Resources

- [MDN CSS Specificity](https://developer.mozilla.org/en-US/docs/Web/CSS/Specificity)
- [K3s Documentation](https://docs.k3s.io/)
- [Blazor WebAssembly Docs](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [PageSpeed Insights](https://pagespeed.web.dev/)
- [CSS-Tricks: Preventing a Grid Blowout](https://css-tricks.com/preventing-a-grid-blowout/)
- [Defensive CSS](https://defensivecss.dev/)
- [Ahmad Shadeed: Building Website Headers with CSS Flexbox](https://ishadeed.com/article/website-headers-flexbox/)
- [Promtail GeoIP Stage - Grafana Docs](https://grafana.com/docs/loki/latest/send-data/promtail/stages/geoip/)
- [Cloudflare Original Visitor IPs](https://developers.cloudflare.com/support/troubleshooting/restoring-visitor-ips/restoring-original-visitor-ips/)
- [Node Exporter Full Dashboard](https://grafana.com/grafana/dashboards/1860)

---

## 🚨 CRITICAL: CSS File Must Be Loaded in index.html! (v2.3.12 Discovery)

**Problem**: CSS file exists but styles are NOT being applied.

**Root Cause**: The CSS file is NOT included in `index.html`! Just having the file in `wwwroot/css/` is NOT enough - it MUST be referenced in `<head>`.

### Diagnostic Checklist

When CSS rules are not being applied despite being correct:

1. ✅ **Check if file is loaded in index.html**
   ```html
   <!-- Is your file listed here? -->
   <link rel="stylesheet" href="css/your-file.css" />
   ```

2. ✅ **Check loading order** - CSS files loaded LATER override earlier files
   ```html
   <!-- Order matters! Later = higher priority -->
   <link rel="stylesheet" href="css/base.css" />          <!-- 1. Loads first -->
   <link rel="stylesheet" href="css/components.css" />    <!-- 2. Loads second -->
   <link rel="stylesheet" href="css/overrides.css" />     <!-- 3. Loads last - WINS! -->
   ```

3. ✅ **Check loading method** - Critical CSS must be synchronous
   ```html
   <!-- CRITICAL CSS (above-the-fold) - Synchronous loading -->
   <link rel="stylesheet" href="css/header.css" />

   <!-- NON-CRITICAL CSS - Deferred loading -->
   <link rel="stylesheet" href="css/footer.css" media="print" onload="this.media='all'" />
   ```

### The v2.3.11/v2.3.12 Bug

**What happened**: I added 400+ lines of CSS fixes to `header-enhancements.css` and deployed multiple versions - but the fixes NEVER worked.

**Why**: The file was NEVER referenced in `index.html`! The browser never loaded it.

**Fix**: Added the missing `<link>` tag:
```html
<!-- Header Professional - CRITICAL: prevents Chrome FOUC (v2.3.8 fix) -->
<link rel="stylesheet" href="css/header-professional.css" />
<!-- Header Enhancements - CRITICAL: fixes Categories dropdown, mobile menu (v2.3.11 fix) -->
<!-- MUST load AFTER header-professional.css for proper CSS specificity -->
<link rel="stylesheet" href="css/header-enhancements.css" />
```

### Key Lessons Learned

1. **ALWAYS verify CSS file is loaded in index.html before debugging specificity**
2. **Use browser DevTools → Network tab → Filter "CSS"** to see what files are actually loaded
3. **If a CSS file isn't in the list, it's not being loaded - no amount of `!important` will help**
4. **Load order determines cascade priority** - put override files AFTER base files

### Quick Verification

```bash
# Check if file is referenced in index.html
grep -n "your-file.css" src/InsightLearn.WebAssembly/wwwroot/index.html

# If no results, file is NOT loaded!
```

---

## Changelog

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-24 | 1.0.0 | Initial version with K8s, CSS, GDPR, Blazor patterns |
| 2025-12-24 | 1.1.0 | Added CSS Grid text truncation fix, Mobile hamburger menu visibility, Flexbox header centering |
| 2025-12-24 | 1.2.0 | Added Grafana GeoIP Geolocation Tracking (3 solutions), Disk Space Monitoring with Prometheus |
| 2025-12-24 | 1.3.0 | **MAJOR UPDATE**: Fixed CSS Grid/text truncation misconception - `min-width: 0` SHRINKS, `width: max-content` EXPANDS. Added CSS Specificity Wars fix with element+class selectors. Added visibility triple-override pattern (display + visibility + opacity). |
| 2025-12-25 | 1.4.0 | **🚨 CRITICAL DISCOVERY**: CSS file MUST be loaded in index.html! Just having the file in wwwroot/css/ is NOT enough. This was the root cause of v2.3.10/v2.3.11 fixes not working. |
| 2025-12-26 | 1.5.0 | **Loki + Promtail GeoIP Stack**: Complete log aggregation solution with real visitor geolocation from Cloudflare headers. NetworkPolicies for Loki/Promtail connectivity. |

---

## Loki + Promtail Log Aggregation Stack (v2.3.13)

### Architettura

```
WASM Pod (nginx) → stdout logs → Promtail (DaemonSet) → Loki (Deployment) → Grafana
                                      ↓
                              Parse GeoIP from
                              CF-* headers
```

### Problema Iniziale: NetworkPolicies Bloccano Promtail

**Sintomo**: Promtail log mostra `context deadline exceeded` quando tenta di connettersi a Loki.

**Root Cause**: `default-deny-ingress` e `default-deny-egress` NetworkPolicies bloccano il traffico tra pod.

**Soluzione**: Creare NetworkPolicies specifiche per Loki e Promtail:

```yaml
# k8s/29-loki-promtail-networkpolicy.yaml
---
# Loki - Accept ingress from Promtail and Grafana
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: loki-network-policy
  namespace: insightlearn
spec:
  podSelector:
    matchLabels:
      app: loki
  policyTypes:
    - Ingress
    - Egress
  ingress:
    - from:
        - podSelector:
            matchLabels:
              app: promtail
      ports:
        - protocol: TCP
          port: 3100
    - from:
        - podSelector:
            matchLabels:
              app: grafana
      ports:
        - protocol: TCP
          port: 3100
  egress:
    - to:
        - namespaceSelector: {}
      ports:
        - protocol: UDP
          port: 53  # DNS
---
# Promtail - Allow egress to Loki
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: promtail-network-policy
  namespace: insightlearn
spec:
  podSelector:
    matchLabels:
      app: promtail
  policyTypes:
    - Egress
  egress:
    - to:
        - namespaceSelector: {}
      ports:
        - protocol: UDP
          port: 53
    - to:
        - podSelector:
            matchLabels:
              app: loki
      ports:
        - protocol: TCP
          port: 3100
```

### Promtail Config per K3s CRI Log Format

K3s/containerd usa CRI log format: `TIMESTAMP stdout|stderr F CONTENT`

```yaml
# promtail-config.yaml
scrape_configs:
  - job_name: nginx-geoip
    static_configs:
      - targets:
          - localhost
        labels:
          job: nginx-geoip
          namespace: insightlearn
          __path__: /var/log/pods/insightlearn_insightlearn-wasm-blazor-webassembly-*/wasm-blazor/*.log
    pipeline_stages:
      # 1. Parse CRI log format
      - regex:
          expression: '^(?P<timestamp>\S+) (?P<stream>stdout|stderr) (?P<flags>\S+) (?P<content>.*)$'
      # 2. Extract JSON from content field
      - match:
          selector: '{job="nginx-geoip"}'
          stages:
            - json:
                source: content
                expressions:
                  cf_connecting_ip: cf_connecting_ip
                  cf_ipcountry: cf_ipcountry
                  cf_ipcity: cf_ipcity
                  status: status
            # 3. Add labels for queries
            - labels:
                country: cf_ipcountry
                city: cf_ipcity
                status:
```

### Grafana Dashboard Provisioning

**Problema**: Dashboard ConfigMap esiste ma non appare in Grafana.

**Root Cause**: Grafana non monta automaticamente nuovi ConfigMap - serve aggiornare:
1. `grafana-dashboards-provisioning` ConfigMap (aggiungere provider)
2. Grafana Deployment (aggiungere volume mount)

```bash
# 1. Add dashboard path to provisioning
kubectl patch configmap grafana-dashboards-provisioning -n insightlearn \
  --patch '{"data":{"dashboards.yml":"...add new provider path..."}}'

# 2. Add volume mount to Grafana deployment
kubectl patch deployment grafana -n insightlearn --type='json' -p='[
  {"op":"add","path":"/spec/template/spec/volumes/-","value":{"name":"grafana-dashboards-geoip","configMap":{"name":"grafana-geoip-dashboard"}}},
  {"op":"add","path":"/spec/template/spec/containers/0/volumeMounts/-","value":{"name":"grafana-dashboards-geoip","mountPath":"/var/lib/grafana/dashboards-geoip"}}
]'
```

### Verificare Loki Riceve Dati

```bash
# Query labels disponibili
kubectl exec -n insightlearn deployment/loki -- wget -qO- \
  'http://localhost:3100/loki/api/v1/labels'
# Output: {"status":"success","data":["city","country","job","namespace","status"]}

# Query dati per paese
kubectl exec -n insightlearn deployment/loki -- wget -qO- \
  'http://localhost:3100/loki/api/v1/query?query=sum%20by%20(country)%20(count_over_time({job="nginx-geoip"}[1h]))'
```

### File Coinvolti

| File | Scopo |
|------|-------|
| `k8s/25-loki-deployment.yaml` | Loki Deployment + Service |
| `k8s/26-promtail-daemonset.yaml` | Promtail DaemonSet + ConfigMap |
| `k8s/27-grafana-loki-datasource.yaml` | Loki datasource per Grafana |
| `k8s/28-grafana-geoip-dashboard.yaml` | Dashboard GeoIP + Disk Monitoring |
| `k8s/29-loki-promtail-networkpolicy.yaml` | NetworkPolicies per connettività |
| `docker/wasm-nginx.conf` | Nginx log format `geoip_json` |

### LogQL Query per Dashboard

```logql
# Requests per paese (esclude valori vuoti)
sum by (country) (count_over_time({job="nginx-geoip"} | json | country != "" | country != "-" [$__interval]))

# Requests per città
sum by (city) (count_over_time({job="nginx-geoip"} | json | city != "" | city != "-" [$__interval]))

# Visitatori unici (per IP)
count(count by (client_ip) (count_over_time({job="nginx-geoip"} | json | client_ip != "" [$__range])))

# Log recenti con formattazione
{job="nginx-geoip"} | json | line_format "{{.time}} | {{.cf_ipcountry}} | {{.cf_ipcity}} | {{.cf_connecting_ip}} | {{.request_uri}}"
```

### Troubleshooting

| Problema | Causa | Soluzione |
|----------|-------|-----------|
| Promtail "context deadline exceeded" | NetworkPolicy blocca egress | Creare `promtail-network-policy` |
| Loki riceve 0 log | Path log errato | Verificare `/var/log/pods/` pattern |
| Dashboard non appare | Volume non montato | Aggiornare Grafana deployment |
| Dati vuoti in Grafana | Promtail non parsing JSON | Verificare `pipeline_stages` |

---

*This document is continuously updated as new competencies are acquired.*
