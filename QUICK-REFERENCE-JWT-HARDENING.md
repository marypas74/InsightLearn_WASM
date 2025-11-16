# JWT Secret Hardening - Quick Reference Card

## What Changed?

**OLD CODE**:
```csharp
var jwtSecret = builder.Configuration["JWT_SECRET_KEY"] ?? builder.Configuration["Jwt:Secret"];
```
Problem: No explicit env var check, no logging, no warning

**NEW CODE**:
```csharp
var jwtSecretFromEnv = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var jwtSecretFromConfig = builder.Configuration["Jwt:Secret"];
var jwtSecret = jwtSecretFromEnv ?? jwtSecretFromConfig;

if (jwtSecretFromEnv == null) {
    Console.WriteLine("[SECURITY WARNING] JWT Secret is loaded from appsettings.json...");
} else {
    Console.WriteLine("[SECURITY] JWT Secret loaded from JWT_SECRET_KEY environment variable (RECOMMENDED)");
}
```

---

## 4 Security Requirements

1. **No Fallback**: App fails if JWT secret not configured (no default values)
2. **Minimum 32 Characters**: Cryptographically secure length requirement
3. **Block Insecure Values**: 14 common defaults blocked (test, dev, changeme, etc.)
4. **Warn on Config File**: Log warning if secret loaded from appsettings.json

---

## Production Setup

```bash
# Generate secure key
openssl rand -base64 64

# Kubernetes Secret
kubectl create secret generic insightlearn-secrets \
  --from-literal=jwt-secret-key="YOUR_GENERATED_KEY" \
  --namespace insightlearn

# Docker Compose
# .env file:
JWT_SECRET_KEY=YOUR_GENERATED_KEY
```

---

## Testing Commands

```bash
# Should FAIL (missing secret)
unset JWT_SECRET_KEY
dotnet run --project src/InsightLearn.Application

# Should FAIL (too short)
export JWT_SECRET_KEY="test"
dotnet run --project src/InsightLearn.Application

# Should FAIL (insecure value)
export JWT_SECRET_KEY="changeme123456789012345678901234"
dotnet run --project src/InsightLearn.Application

# Should SUCCEED
export JWT_SECRET_KEY="$(openssl rand -base64 64)"
dotnet run --project src/InsightLearn.Application
```

---

## Files Modified

1. `/src/InsightLearn.Application/Program.cs` (lines 261-329)
2. `/CLAUDE.md` (new section lines 109-168)

---

## Blocked Insecure Values (14 total)

- changeme
- your-secret-key
- insecure
- test
- dev
- password
- secret
- default
- replace_with
- insightlearn
- jwt_secret
- your_secret
- my_secret
- REPLACE_WITH_JWT_SECRET_KEY_ENV_VAR_MINIMUM_32_CHARS

---

## Startup Logs

**Good (env var)**:
```
[SECURITY] JWT Secret loaded from JWT_SECRET_KEY environment variable (RECOMMENDED)
```

**Warning (appsettings.json)**:
```
[SECURITY WARNING] JWT Secret is loaded from appsettings.json. 
For production deployments, use JWT_SECRET_KEY environment variable instead.
```

**Error (misconfigured)**:
```
Unhandled exception. System.InvalidOperationException: JWT Secret Key is not configured.
```

---

## Version

- **Phase**: 2.1
- **Version**: v1.6.7-dev
- **Priority**: CRITICAL
- **Status**: ✅ COMPLETE
- **Date**: 2025-11-16
