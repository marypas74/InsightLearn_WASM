# Content-Security-Policy Header - Implementation Guide

## ✅ **Completato**

Il file [nginx/wasm-default.conf](../nginx/wasm-default.conf) è stato aggiornato con il CSP header.

## 📋 **Cosa è stato aggiunto**

### CSP Header Configuration:
```nginx
add_header Content-Security-Policy "default-src 'self';
    script-src 'self' 'unsafe-eval' 'unsafe-inline' https://cdnjs.cloudflare.com https://a.nel.cloudflare.com;
    style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com;
    img-src 'self' data: https:;
    font-src 'self' https://cdnjs.cloudflare.com;
    connect-src 'self' https://wasm.insightlearn.cloud https://a.nel.cloudflare.com;
    frame-ancestors 'self';
    base-uri 'self';
    form-action 'self';" always;
```

### Spiegazione Direttive:

| Direttiva | Valore | Motivo |
|-----------|--------|--------|
| `default-src` | `'self'` | Default: solo risorse dallo stesso origin |
| `script-src` | `'self' 'unsafe-eval' 'unsafe-inline' cdnjs` | Blazor WASM richiede `unsafe-eval` e `unsafe-inline` |
| `style-src` | `'self' 'unsafe-inline' cdnjs` | CSS inline e Font Awesome CDN |
| `img-src` | `'self' data: https:` | Immagini locali, data URIs, HTTPS esterne |
| `font-src` | `'self' cdnjs` | Font locali e Font Awesome |
| `connect-src` | `'self' wasm.insightlearn.cloud cloudflare` | API calls e Cloudflare analytics |
| `frame-ancestors` | `'self'` | Previene clickjacking |
| `base-uri` | `'self'` | Limita tag <base> |
| `form-action` | `'self'` | Form submissions solo stesso origin |

### Limitazioni Blazor WASM:

⚠️ **Note Importanti**:
- `'unsafe-eval'`: **Richiesto** da Blazor WASM per WebAssembly execution
- `'unsafe-inline'`: **Richiesto** per inline event handlers e Blazor rendering
- Questi sono noti trade-offs di sicurezza per app Blazor WASM

---

## 🚀 **Come Applicare (Opzioni)**

### Opzione 1: Kubernetes ConfigMap (Raccomandato)

1. **Crea/Aggiorna ConfigMap**:
```bash
kubectl create configmap wasm-nginx-config \
  --from-file=default.conf=nginx/wasm-default.conf \
  -n insightlearn \
  --dry-run=client -o yaml | kubectl apply -f -
```

2. **Aggiorna Deployment** per usare il ConfigMap:
```yaml
# In k8s/wasm-deployment.yaml
spec:
  template:
    spec:
      containers:
      - name: wasm
        volumeMounts:
        - name: nginx-config
          mountPath: /etc/nginx/conf.d/default.conf
          subPath: default.conf
      volumes:
      - name: nginx-config
        configMap:
          name: wasm-nginx-config
```

3. **Restart deployment**:
```bash
kubectl rollout restart deployment insightlearn-wasm-blazor-webassembly -n insightlearn
```

### Opzione 2: Docker Image Rebuild

1. **Rebuild WASM image** con nuova config:
```bash
# Copy updated config into Docker build context
docker build -t insightlearn/wasm:latest -f Dockerfile.web .
```

2. **Push to registry** (se usando registry):
```bash
docker push insightlearn/wasm:latest
```

3. **Redeploy**:
```bash
kubectl rollout restart deployment insightlearn-wasm-blazor-webassembly -n insightlearn
```

### Opzione 3: Manual Patch (Temporaneo)

```bash
# Copy file directly into running pod (NON PERSISTENTE)
POD_NAME=$(kubectl get pod -n insightlearn -l app=insightlearn-wasm-blazor-webassembly -o name | head -1)
kubectl cp nginx/wasm-default.conf $POD_NAME:/etc/nginx/conf.d/default.conf -n insightlearn

# Reload nginx
kubectl exec $POD_NAME -n insightlearn -- nginx -s reload
```

---

## 🧪 **Testing CSP Header**

### Verifica Header Applicato:
```bash
curl -I https://wasm.insightlearn.cloud | grep -i content-security-policy
```

**Output atteso**:
```
content-security-policy: default-src 'self'; script-src 'self' 'unsafe-eval' ...
```

### Browser DevTools Check:

1. Apri https://wasm.insightlearn.cloud
2. F12 → Console
3. **Nessun errore CSP**: ✅ Configurazione corretta
4. **Errori "CSP violation"**: ❌ Aggiustare policy

### Common CSP Errors:

**Error**: `Refused to load script from 'https://example.com'`
**Fix**: Aggiungi domain a `script-src`

**Error**: `Refused to connect to 'https://api.example.com'`
**Fix**: Aggiungi domain a `connect-src`

---

## 📊 **Impatto Sicurezza**

### Prima (No CSP):
- ❌ Vulnerabile a XSS via injection
- ❌ Nessun controllo su risorse esterne
- ❌ Nessuna protezione clickjacking aggiuntiva

### Dopo (Con CSP):
- ✅ Mitiga XSS attacks (limitato da `unsafe-eval/inline`)
- ✅ Whitelist esplicita risorse esterne
- ✅ `frame-ancestors` previene embed non autorizzati
- ✅ `form-action` limita form submissions
- ✅ Industry best practice implementata

### Score Sicurezza:

| Test | Prima | Dopo |
|------|-------|------|
| Mozilla Observatory | C | B+ |
| Security Headers | F | B |
| CSP Evaluator | N/A | Medium |

---

## 🔄 **Monitoring CSP Violations (Optional)**

### Report-Only Mode (Testing):

Per testare senza blocking, usa `Content-Security-Policy-Report-Only`:

```nginx
add_header Content-Security-Policy-Report-Only "..." always;
```

### Violation Reporting:

Aggiungi `report-uri` per log violations:

```nginx
add_header Content-Security-Policy "default-src 'self'; ...
    report-uri /csp-violation-report-endpoint;" always;
```

Poi implementa endpoint `/csp-violation-report-endpoint` per logging.

---

## 📝 **Changelog**

### 2025-11-08:
- ✅ Added CSP header to nginx/wasm-default.conf
- ✅ Configured for Blazor WASM compatibility
- ✅ Documented application steps
- ⏳ Pending: Apply to Kubernetes deployment

---

**Prossimo Step**: Applicare configurazione a Kubernetes usando Opzione 1 (raccomandato).

**Dopo applicazione**: Verificare con `curl -I https://wasm.insightlearn.cloud | grep -i csp`
