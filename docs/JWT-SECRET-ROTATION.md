# JWT Secret Key Rotation Guide

## Overview

This document provides comprehensive procedures for JWT secret key rotation in the InsightLearn platform. JWT secret rotation is a critical security practice that minimizes the impact of potential key compromise.

**Version**: 1.6.0-dev
**Last Updated**: 2025-11-14
**Security Level**: CRITICAL

---

## Table of Contents

1. [When to Rotate](#when-to-rotate)
2. [Generating New Secrets](#generating-new-secrets)
3. [Rotation Strategies](#rotation-strategies)
4. [Step-by-Step Procedures](#step-by-step-procedures)
5. [Emergency Rotation](#emergency-rotation)
6. [Verification & Testing](#verification--testing)
7. [Rollback Procedure](#rollback-procedure)
8. [Security Best Practices](#security-best-practices)

---

## When to Rotate

### Scheduled Rotation (Recommended)

- **Frequency**: Every 90 days (quarterly)
- **Best Practice**: Rotate during low-traffic maintenance windows
- **Calendar**: Schedule rotations on the same day each quarter (e.g., Jan 1, Apr 1, Jul 1, Oct 1)

### Immediate Rotation Required

Rotate JWT secret immediately if:

1. **Security Breach**: Suspected or confirmed compromise of secret key
2. **Employee Departure**: Team member with secret access leaves organization
3. **Public Exposure**: Secret accidentally committed to public repository
4. **Compliance Requirement**: Audit or regulatory mandate
5. **System Migration**: Moving to new infrastructure/environment

---

## Generating New Secrets

### Using OpenSSL (Recommended)

```bash
# Generate 64-character base64-encoded secret
openssl rand -base64 64 | tr -d '\n'
```

### Using Built-in Script

```bash
# Navigate to project root
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM

# Run secret generator
./scripts/generate-jwt-secret.sh
```

### Requirements

- **Minimum Length**: 32 characters (enforced at startup)
- **Recommended Length**: 64+ characters
- **Character Set**: Base64 (A-Z, a-z, 0-9, +, /)
- **Entropy**: Cryptographically secure random number generator

### Prohibited Values

The following values will be rejected by the API at startup:

- `your-secret-key`
- `changeme`
- `default`
- `secret`
- `password`
- `replace_with`
- `insightlearn`
- `jwt_secret`
- `REPLACE_WITH_JWT_SECRET_KEY_ENV_VAR_MINIMUM_32_CHARS`

---

## Rotation Strategies

### Strategy 1: Zero-Downtime Rolling Update (Kubernetes)

**Best For**: Production environments requiring high availability

**Process**:
1. Deploy new secret to cluster
2. Perform rolling restart of pods
3. Old pods continue serving with old secret
4. New pods start with new secret
5. Gradual transition as old pods terminate

**Downtime**: None (users may need to re-login)

---

### Strategy 2: Scheduled Maintenance Window (Docker Compose)

**Best For**: Development/staging environments

**Process**:
1. Announce maintenance window to users
2. Update secret in .env file
3. Restart all containers simultaneously
4. Verify health checks pass

**Downtime**: 2-5 minutes

---

### Strategy 3: Dual-Key Transition Period (Advanced)

**Best For**: Large-scale production with millions of active sessions

**Process**:
1. Modify code to accept both old and new keys for validation
2. Deploy updated code
3. Start issuing tokens with new key
4. Wait for old tokens to expire (7 days default)
5. Remove old key from validation

**Downtime**: None
**Complexity**: HIGH (requires code changes)

---

## Step-by-Step Procedures

### Kubernetes Deployment

#### Pre-Rotation Checklist

- [ ] Generate new secret using `./scripts/generate-jwt-secret.sh`
- [ ] Schedule maintenance window (recommended, not required)
- [ ] Backup current secret: `kubectl get secret insightlearn-secrets -n insightlearn -o yaml > secret-backup-$(date +%Y%m%d).yaml`
- [ ] Notify users of potential re-login requirement
- [ ] Verify monitoring/alerting is active

#### Rotation Steps

```bash
# 1. Generate new secret
NEW_SECRET=$(openssl rand -base64 64 | tr -d '\n')
echo "New secret generated (length: ${#NEW_SECRET})"

# 2. Encode secret to base64 for Kubernetes
NEW_SECRET_BASE64=$(echo -n "$NEW_SECRET" | base64 -w 0)

# 3. Patch Kubernetes secret
kubectl patch secret insightlearn-secrets \
  -n insightlearn \
  --type='json' \
  -p="[{\"op\": \"replace\", \"path\": \"/data/jwt-secret-key\", \"value\": \"$NEW_SECRET_BASE64\"}]"

# Verify secret updated
kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.jwt-secret-key}' | base64 -d | wc -c

# 4. Perform rolling restart of API pods
kubectl rollout restart deployment/insightlearn-api -n insightlearn

# 5. Monitor rollout status
kubectl rollout status deployment/insightlearn-api -n insightlearn --timeout=300s

# 6. Verify pods are running with new secret
kubectl get pods -n insightlearn -l app=insightlearn-api

# 7. Check pod logs for successful JWT initialization
kubectl logs -n insightlearn -l app=insightlearn-api --tail=50 | grep "JWT configuration validated"
```

#### Post-Rotation Verification

```bash
# Test authentication endpoint
curl -X POST https://insightlearn.cloud/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"TestPassword123!"}'

# Verify JWT token can be validated
TOKEN="<token-from-login-response>"
curl -H "Authorization: Bearer $TOKEN" https://insightlearn.cloud/api/auth/me

# Check API health
curl https://insightlearn.cloud/health
```

---

### Docker Compose Deployment

#### Pre-Rotation Checklist

- [ ] Generate new secret using `./scripts/generate-jwt-secret.sh`
- [ ] Backup current .env file: `cp .env .env.backup-$(date +%Y%m%d)`
- [ ] Schedule maintenance window (2-5 minutes)
- [ ] Notify users of downtime

#### Rotation Steps

```bash
# 1. Navigate to project root
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM

# 2. Generate new secret
NEW_SECRET=$(openssl rand -base64 64 | tr -d '\n')
echo "New secret: $NEW_SECRET"

# 3. Update .env file (manual or automated)
# Manual: Edit .env file and replace JWT_SECRET_KEY value
# Automated:
sed -i.bak "s/^JWT_SECRET_KEY=.*/JWT_SECRET_KEY=$NEW_SECRET/" .env

# Verify update
grep "^JWT_SECRET_KEY=" .env

# 4. Restart API container
docker-compose restart api

# Alternative: Full stack restart (if API doesn't pick up new env vars)
docker-compose down
docker-compose up -d

# 5. Wait for API to be healthy
sleep 10
docker-compose ps | grep api

# 6. Check API logs for successful startup
docker-compose logs api | tail -50 | grep "JWT configuration validated"
```

#### Post-Rotation Verification

```bash
# Test API health
curl http://localhost:7001/health

# Test authentication
curl -X POST http://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"Admin123!Secure"}'
```

---

## Emergency Rotation

**Scenario**: JWT secret has been compromised or publicly exposed.

### Immediate Actions (Execute in Order)

**Step 1: Containment (0-5 minutes)**

```bash
# 1. Generate emergency secret IMMEDIATELY
EMERGENCY_SECRET=$(openssl rand -base64 64 | tr -d '\n')

# 2. Update Kubernetes secret (production)
kubectl patch secret insightlearn-secrets -n insightlearn \
  --type='json' \
  -p="[{\"op\": \"replace\", \"path\": \"/data/jwt-secret-key\", \"value\": \"$(echo -n $EMERGENCY_SECRET | base64 -w 0)\"}]"

# 3. Force immediate pod restart (faster than rolling update)
kubectl delete pods -n insightlearn -l app=insightlearn-api
```

**Step 2: Verification (5-10 minutes)**

```bash
# 1. Verify all pods restarted
kubectl get pods -n insightlearn -l app=insightlearn-api

# 2. Check logs for successful startup
kubectl logs -n insightlearn -l app=insightlearn-api --tail=20

# 3. Test authentication endpoint
curl -X POST https://insightlearn.cloud/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"Admin123!Secure"}'
```

**Step 3: Incident Response (10-60 minutes)**

1. **Revoke Active Sessions** (if session management implemented):
   ```bash
   # Clear Redis session cache
   kubectl exec -it redis-0 -n insightlearn -- redis-cli -a "$REDIS_PASSWORD" FLUSHDB
   ```

2. **Audit Logs Review**:
   ```sql
   -- Check for suspicious authentication attempts
   SELECT * FROM AuditLogs
   WHERE Action = 'Login'
     AND Timestamp > DATEADD(hour, -24, GETUTCDATE())
   ORDER BY Timestamp DESC;
   ```

3. **Notify Stakeholders**:
   - Security team
   - Development team
   - Management
   - Affected users (if breach confirmed)

4. **Document Incident**:
   - Create incident report
   - Timeline of events
   - Root cause analysis
   - Remediation steps taken

---

## Verification & Testing

### Automated Verification Script

```bash
#!/bin/bash
# JWT Secret Rotation Verification Script

API_URL="${API_URL:-https://insightlearn.cloud}"
TEST_EMAIL="test@example.com"
TEST_PASSWORD="TestPassword123!"

echo "Testing JWT secret rotation..."

# 1. Test login endpoint
echo "1. Testing login..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASSWORD\"}")

echo "$LOGIN_RESPONSE" | jq '.'

# Extract token
TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token // .access_token // empty')

if [ -z "$TOKEN" ]; then
  echo "FAIL: Login did not return a token"
  exit 1
fi

echo "SUCCESS: Received JWT token (length: ${#TOKEN})"

# 2. Test token validation
echo "2. Testing token validation..."
ME_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" "$API_URL/api/auth/me")

echo "$ME_RESPONSE" | jq '.'

if echo "$ME_RESPONSE" | jq -e '.id' > /dev/null; then
  echo "SUCCESS: Token validated successfully"
else
  echo "FAIL: Token validation failed"
  exit 1
fi

# 3. Test API health
echo "3. Testing API health..."
HEALTH_RESPONSE=$(curl -s "$API_URL/health")

if echo "$HEALTH_RESPONSE" | jq -e '.status == "Healthy"' > /dev/null; then
  echo "SUCCESS: API is healthy"
else
  echo "WARNING: API health check returned unexpected response"
fi

echo ""
echo "All tests passed! JWT secret rotation verified."
```

### Manual Verification Checklist

After rotation, verify:

- [ ] API pods/containers restarted successfully
- [ ] API health endpoint returns 200 OK
- [ ] Login endpoint returns valid JWT tokens
- [ ] JWT tokens can be validated (call /api/auth/me)
- [ ] Protected endpoints require valid tokens
- [ ] Expired/invalid tokens are rejected (401 Unauthorized)
- [ ] API logs show "JWT configuration validated successfully"
- [ ] No authentication errors in application logs
- [ ] Monitoring dashboards show normal metrics

---

## Rollback Procedure

If rotation causes issues, rollback immediately:

### Kubernetes Rollback

```bash
# Option 1: Restore from backup secret
kubectl apply -f secret-backup-YYYYMMDD.yaml

# Restart pods to pick up old secret
kubectl rollout restart deployment/insightlearn-api -n insightlearn

# Option 2: Patch with old secret value directly
OLD_SECRET="<previous-secret-value>"
kubectl patch secret insightlearn-secrets -n insightlearn \
  --type='json' \
  -p="[{\"op\": \"replace\", \"path\": \"/data/jwt-secret-key\", \"value\": \"$(echo -n $OLD_SECRET | base64 -w 0)\"}]"

kubectl rollout restart deployment/insightlearn-api -n insightlearn
```

### Docker Compose Rollback

```bash
# Restore backup .env file
cp .env.backup-YYYYMMDD .env

# Restart API
docker-compose restart api
```

### Verification After Rollback

```bash
# Test that old tokens work again (if still valid)
curl -H "Authorization: Bearer <old-token>" https://insightlearn.cloud/api/auth/me

# Verify API health
curl https://insightlearn.cloud/health
```

---

## Security Best Practices

### Secret Storage

1. **Kubernetes Secrets**: Use native Kubernetes Secrets (minimum)
2. **External Secret Management** (recommended for production):
   - HashiCorp Vault
   - AWS Secrets Manager
   - Azure Key Vault
   - Google Secret Manager

3. **Never Store Secrets In**:
   - Git repositories
   - Environment variable files committed to source control
   - Plain text documentation
   - Chat messages (Slack, Teams, email)
   - Container images

### Access Control

1. **Limit Secret Access**:
   ```bash
   # Kubernetes RBAC - restrict secret access
   kubectl create role secret-reader \
     --verb=get,list \
     --resource=secrets \
     --namespace=insightlearn

   kubectl create rolebinding secret-reader-binding \
     --role=secret-reader \
     --user=ops-team \
     --namespace=insightlearn
   ```

2. **Audit Secret Access**:
   ```bash
   # Enable Kubernetes audit logging for secret access
   kubectl get events -n insightlearn --field-selector involvedObject.kind=Secret
   ```

### Monitoring & Alerting

1. **Set Up Alerts For**:
   - Failed authentication attempts (potential brute force)
   - Unusual authentication patterns
   - Secret read operations (Kubernetes audit logs)
   - API startup failures (JWT validation errors)

2. **Monitor Metrics**:
   - Authentication success/failure rate
   - Token generation rate
   - Token validation latency
   - Active user sessions

### Compliance

1. **Documentation Requirements**:
   - Maintain secret rotation schedule
   - Document emergency rotation procedures
   - Keep audit trail of all rotations
   - Incident response plan

2. **Regular Audits**:
   - Quarterly secret rotation verification
   - Annual security audits
   - Penetration testing
   - Compliance assessments (PCI DSS, SOC 2, ISO 27001)

---

## Troubleshooting

### Common Issues

**Issue 1: API fails to start after rotation**

```
Error: JWT Secret is too short (0 characters). Minimum length is 32 characters for security.
```

**Solution**:
```bash
# Verify secret exists in Kubernetes
kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.jwt-secret-key}' | base64 -d | wc -c

# Verify secret is not empty
kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.jwt-secret-key}' | base64 -d
```

---

**Issue 2: Users can't login after rotation**

**Cause**: This is expected behavior - all old JWT tokens are invalidated.

**Solution**:
- Users must login again with username/password
- Communicate this to users before rotation
- Consider implementing refresh token rotation for smoother UX

---

**Issue 3: Pods crash loop after rotation**

```bash
# Check pod logs for errors
kubectl logs -n insightlearn <pod-name> --previous

# Common causes:
# - Secret not base64 encoded properly
# - Secret contains weak/default value
# - Environment variable not mounted correctly
```

**Solution**:
```bash
# Verify secret is properly base64 encoded
kubectl get secret insightlearn-secrets -n insightlearn -o yaml

# Check pod environment variables
kubectl exec -it <pod-name> -n insightlearn -- env | grep JWT
```

---

## Contact & Support

**Security Issues**: security@insightlearn.cloud
**Technical Support**: support@insightlearn.cloud
**Emergency Hotline**: +1 (555) 123-4567

---

## References

- [OWASP JWT Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [RFC 7519 - JSON Web Token](https://datatracker.ietf.org/doc/html/rfc7519)
- [NIST SP 800-57 - Key Management](https://csrc.nist.gov/publications/detail/sp/800-57-part-1/rev-5/final)
- InsightLearn CLAUDE.md - Section "Authentication & Authorization"

---

**Document Version**: 1.0
**Effective Date**: 2025-11-14
**Next Review**: 2026-02-14 (90 days)
