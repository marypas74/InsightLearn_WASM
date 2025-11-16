# Middleware Pipeline

This directory contains all middleware components for the InsightLearn API request processing pipeline.

## Pipeline Architecture

The middleware pipeline processes every HTTP request in this order:

```
Request
  ↓
SecurityHeadersMiddleware (P1.2)
  ├─ X-Frame-Options: DENY
  ├─ X-Content-Type-Options: nosniff
  ├─ Strict-Transport-Security (prod only)
  ├─ Content-Security-Policy
  ├─ Permissions-Policy
  └─ Cross-Origin-*-Policy
  ↓
DistributedRateLimitMiddleware (P1.1)
  ├─ Global rate limit: 200 req/min per IP
  ├─ Auth rate limit: 5 req/min (brute force protection)
  ├─ Redis-backed (cross-pod coordination)
  └─ Returns 429 Too Many Requests if exceeded
  ↓
RequestValidationMiddleware (P0.4)
  ├─ SQL injection detection
  ├─ XSS pattern detection
  ├─ Path traversal detection
  ├─ Request size validation
  └─ Returns 400 Bad Request if malicious
  ↓
ModelValidationMiddleware (P3.2) ← NEW
  ├─ Captures 400 Bad Request responses
  ├─ Extracts validation error details
  ├─ Logs validation failures with context
  ├─ Detects security patterns (brute force, injection)
  └─ Zero overhead for non-400 responses
  ↓
Authentication
  ├─ JWT token validation
  └─ Returns 401 Unauthorized if invalid
  ↓
CsrfProtectionMiddleware (P0.2)
  ├─ CSRF token validation
  ├─ Double-submit cookie pattern
  └─ Returns 403 Forbidden if invalid
  ↓
Authorization
  ├─ Role-based access control
  └─ Returns 403 Forbidden if unauthorized
  ↓
AuditLoggingMiddleware (P0.5)
  ├─ Logs sensitive operations
  ├─ Uses IDbContextFactory (deadlock-safe)
  └─ Captures user context
  ↓
Endpoint Handler
  ↓
Response
```

## Middleware Components

### 1. SecurityHeadersMiddleware.cs (P1.2)

**Purpose**: Add comprehensive security headers to all responses

**Status**: ✅ Production Ready

**Headers Implemented**:
- X-Frame-Options: DENY (clickjacking protection)
- X-Content-Type-Options: nosniff (MIME sniffing)
- Strict-Transport-Security: max-age=31536000 (production only)
- Content-Security-Policy: Blazor WASM compatible
- Permissions-Policy: LMS features (clipboard, fullscreen, autoplay)
- Cross-Origin-Embedder-Policy: require-corp
- Cross-Origin-Opener-Policy: same-origin
- Cross-Origin-Resource-Policy: same-origin
- X-XSS-Protection: 1; mode=block

**Compliance**: OWASP ASVS V14.4, PCI DSS 6.5.9

**Lines**: 161

---

### 2. DistributedRateLimitMiddleware.cs (P1.1)

**Purpose**: Distributed rate limiting using Redis (prevents API abuse across K8s pods)

**Status**: ✅ Production Ready

**Features**:
- Global rate limit: 200 req/min per IP
- Auth rate limit: 5 req/min (brute force protection)
- Redis-backed (survives pod restarts)
- Graceful failover if Redis unavailable
- X-RateLimit-* headers in response

**Compliance**: OWASP API3:2023

**Lines**: 122

---

### 3. RequestValidationMiddleware.cs (P0.4)

**Purpose**: Validate and sanitize incoming requests (SQL injection, XSS, path traversal)

**Status**: ✅ Production Ready

**Features**:
- SQL injection pattern detection (context-aware)
- XSS pattern detection (DOM-based, event handlers, SVG)
- Path traversal detection (null byte, UNC paths, encoding bypass)
- Request size validation (1MB standard, 5MB bulk operations)
- ReDoS protection (100ms regex timeout)
- Whitelisted paths (file upload, health checks)

**Compliance**: OWASP A06:2021, GDPR Data Protection

**Lines**: 298

---

### 4. ModelValidationMiddleware.cs (P3.2) - NEW

**Purpose**: Centralized logging for all DTO validation failures (400 Bad Request)

**Status**: ✅ Production Ready (Phase 3.2)

**Features**:
- Captures response body without modifying response
- Extracts validation errors from JSON response
- Structured logging with detailed context (IP, user ID, endpoint)
- Security pattern detection:
  - Brute force attacks (multiple 400s on auth endpoints)
  - Injection attempts (oversized error messages)
  - Suspicious field names (SQL/XSS keywords)
- Proxy-aware IP extraction (X-Forwarded-For, X-Real-IP)
- JWT claim parsing (user identification)

**Overhead**:
- Non-400 responses: < 0.1ms
- 400 responses: 1-2ms (acceptable for validation logging)

**Compliance**: GDPR Article 30, OWASP A01/A05/A06, PCI DSS 6.5

**Lines**: 311

**Log Entries**:
- [VALIDATION_FAILURE]: Summary per request
- [VALIDATION_ERROR_DETAIL]: Per-field details
- [SECURITY_CONCERN]: Suspicious patterns detected

---

### 5. CsrfProtectionMiddleware.cs (P0.2)

**Purpose**: CSRF token validation (double-submit cookie pattern)

**Status**: ✅ Production Ready

**Features**:
- Cryptographically secure token generation (32 bytes)
- Constant-time comparison (CryptographicOperations.FixedTimeEquals)
- Cookie + header validation
- Configurable exempt paths

**Compliance**: PCI DSS 6.5.9, OWASP A01:2021

**Lines**: 131

---

### 6. AuditLoggingMiddleware.cs (P0.5)

**Purpose**: Audit trail for sensitive operations

**Status**: ✅ Production Ready

**Features**:
- Uses IDbContextFactory (deadlock-safe for concurrent requests)
- Captures user context (ID, email, roles)
- Logs auth, admin, payment operations
- Creates separate DbContext per request (thread-safe)

**Compliance**: GDPR Article 30

**Lines**: 322

---

## Integration Guide

### How to Add a New Middleware

1. **Create middleware file** in this directory with the pattern:
   ```csharp
   namespace InsightLearn.Application.Middleware;

   public class YourMiddleware
   {
       private readonly RequestDelegate _next;
       private readonly ILogger<YourMiddleware> _logger;

       public YourMiddleware(RequestDelegate next, ILogger<YourMiddleware> logger)
       {
           _next = next;
           _logger = logger;
       }

       public async Task InvokeAsync(HttpContext context)
       {
           // Your logic before

           await _next(context);

           // Your logic after
       }
   }
   ```

2. **Register in Program.cs**:
   ```csharp
   app.UseMiddleware<YourMiddleware>();
   Console.WriteLine("[SECURITY] Your middleware registered");
   ```

3. **Position correctly** in the pipeline:
   - Rate limiting MUST be early (before expensive validation)
   - Input validation MUST be before authentication
   - Logging MUST be after auth (to capture user context)

4. **Add documentation** in this README

---

## Pipeline Positioning Rules

### Critical Rules

1. **Rate Limiting First** → Prevent DoS attacks early
2. **Input Validation Second** → Block malicious requests
3. **Validation Logging Third** → Capture failures for monitoring
4. **Authentication Fourth** → Verify user identity
5. **CSRF/Auth Protection Fifth** → Verify request intent
6. **Authorization Last** → Check user permissions
7. **Audit Logging After Auth** → Log with user context

### Why This Order?

```
SecurityHeaders    → All requests need headers
RateLimit          → Reject DoS early (saves resources)
InputValidation    → Reject malicious input early
ValidationLogging  → Log validation failures (before auth overhead)
Authentication     → Verify user (before auth-specific checks)
CsrfProtection     → Verify request intent
Authorization      → Check permissions
AuditLogging       → Log sensitive ops (after auth to capture user)
```

---

## Configuration

### appsettings.json

```json
{
  "Cors": {
    "AllowedOrigins": "https://insightlearn.cloud,https://www.insightlearn.cloud"
  },
  "Security": {
    "RegexTimeoutMs": 100
  },
  "RateLimit": {
    "RequestsPerMinute": 200,
    "AuthRequestsPerMinute": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "InsightLearn.Application.Middleware": "Debug"
    }
  }
}
```

---

## Testing

### Test Each Middleware

```bash
# Security Headers
./test-security-headers.sh

# Rate Limiting
./test-rate-limiting.sh

# Validation Errors
./test-model-validation-middleware.sh

# Complete verification
./security-fixes-verification.sh
```

---

## Troubleshooting

### Issue: Requests Getting 400 Immediately

**Cause**: RequestValidationMiddleware blocking
- Check: Is request body valid JSON?
- Verify: No SQL keywords in natural language text
- Fix: Add path to ContentWhitelistedPaths if legitimate

### Issue: Validation Errors Not Logged

**Cause**: ModelValidationMiddleware not capturing
- Check: Is response status code 400?
- Verify: Is JSON parsing working (check logs for parsing errors)
- Fix: Check logging configuration level

### Issue: Performance Degradation

**Cause**: Rate limiting or validation overhead
- Check: DistributedRateLimitMiddleware is working (redis available)
- Verify: RequestValidationMiddleware regex timeout is 100ms
- Impact: Expected 1-2ms per 400 response

---

## Monitoring

### Log Patterns to Monitor

**Security Alerts**:
- `[SECURITY]` entries in SecurityHeadersMiddleware
- `[SECURITY_CONCERN]` entries in ModelValidationMiddleware
- `[SECURITY]` entries in RequestValidationMiddleware

**Validation Patterns**:
- `[VALIDATION_FAILURE]` entries (400 response rate)
- `[VALIDATION_ERROR_DETAIL]` entries (field failure patterns)

**Performance**:
- Rate limiting 429 responses (DoS detection)
- Request validation rejections (attack patterns)

---

## Performance Metrics

| Middleware | Per-Request Overhead | Conditions |
|------------|-------------------|-----------|
| SecurityHeaders | < 1ms | All requests |
| RateLimit | < 5ms | All requests (Redis RTT) |
| InputValidation | 0-50ms | Only for suspicious requests |
| ModelValidation | < 2ms | Only for 400 responses |
| CsrfProtection | < 2ms | POST/PUT/PATCH only |
| AuditLogging | 1-5ms | Sensitive operations only |

---

## Version History

| Component | Version | Date | Status |
|-----------|---------|------|--------|
| SecurityHeaders | 1.0.0 | 2025-11-12 | Production |
| RateLimit | 1.0.0 | 2025-11-12 | Production |
| InputValidation | 1.1.0 | 2025-11-12 | Production |
| Csrf | 1.0.0 | 2025-11-08 | Production |
| AuditLogging | 1.1.0 | 2025-11-12 | Production |
| ModelValidation | 1.0.0 | 2025-11-16 | Production |

---

## References

- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [RFC 7231 - HTTP Semantics and Content](https://tools.ietf.org/html/rfc7231)
- [OWASP Top 10 2021](https://owasp.org/Top10/)
- [PCI DSS 3.2.1](https://www.pcisecuritystandards.org/)
- [GDPR Compliance](https://gdpr-info.eu/)
- [ASP.NET Core Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware)

---

**Last Updated**: 2025-11-16
**Total Middleware**: 6 components
**Total Lines**: ~1,300 lines
**Status**: ✅ Production Ready
