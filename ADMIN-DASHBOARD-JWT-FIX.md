# Admin Dashboard JWT Authentication Fix

## Problem Summary

The admin dashboard at `/admin/dashboard` was showing **"Failed to load dashboard statistics"** because:

1. **Backend API endpoints existed** (`/api/admin/dashboard/stats` and `/api/admin/dashboard/recent-activity`) in `Program.cs` (lines 997-1098)
2. **Frontend service was calling them correctly** via `AdminDashboardService`
3. **JWT Authentication middleware was MISSING** - the API had no JWT Bearer authentication configured

When the frontend tried to access the protected endpoints, the API returned `302 Redirect` to a login page instead of accepting the JWT token.

## Solution Implemented

### 1. Added JWT Authentication Middleware

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs`

**Changes**:

#### Added using statements (lines 16-18):
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
```

#### Added JWT authentication configuration (lines 127-154):
```csharp
// Get JWT configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? builder.Configuration["JWT_SECRET_KEY"] ?? "your-very-long-and-secure-secret-key-minimum-32-characters-long!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["JWT_ISSUER"] ?? "InsightLearn.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["JWT_AUDIENCE"] ?? "InsightLearn.Users";

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Authorization
builder.Services.AddAuthorization();
```

### 2. Verified Package Reference

The required NuGet package was already present in `InsightLearn.Application.csproj`:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
```

### 3. Built and Deployed

Built new Docker image:
```bash
docker compose build api
docker tag insightlearn_wasm-api:latest localhost/insightlearn/api:latest
docker save localhost/insightlearn/api:latest > /tmp/api-image.tar
```

## Deployment Instructions

To apply this fix to your Kubernetes cluster:

### Step 1: Import Image to K3s
```bash
./import-api-to-k3s.sh
```

This script will:
1. Import the new API image to K3s containerd
2. Restart the `insightlearn-api` deployment
3. Wait for rollout to complete
4. Display pod status

### Step 2: Verify Deployment
```bash
# Check API pod logs
kubectl logs -n insightlearn -l app=insightlearn-api --tail=50

# Test the endpoint with authentication
# (You'll need a valid Admin JWT token)
curl -X GET http://localhost:31081/api/admin/dashboard/stats \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN_HERE" \
  -H "Content-Type: application/json"
```

### Step 3: Test Frontend

1. Navigate to http://localhost:31081 (or your frontend URL)
2. Login with admin credentials:
   - Email: `admin@insightlearn.cloud`
   - Password: (from `ADMIN_PASSWORD` env var or default)
3. Navigate to `/admin/dashboard`
4. You should now see:
   - Total Users count
   - Total Courses count
   - Total Enrollments count
   - Total Revenue
   - Recent Activity list

## Technical Details

### Admin Dashboard Endpoints

Both endpoints are now properly protected with `[RequireAuthorization(policy => policy.RequireRole("Admin"))]`:

#### 1. GET /api/admin/dashboard/stats
**Returns**:
```json
{
  "TotalUsers": 150,
  "TotalActiveUsers": 120,
  "TotalInstructors": 15,
  "TotalStudents": 135,
  "TotalCourses": 25,
  "TotalEnrollments": 450,
  "TotalPayments": 300,
  "TotalRevenue": 12500.50
}
```

#### 2. GET /api/admin/dashboard/recent-activity
**Parameters**: `?limit=10` (default)

**Returns**:
```json
[
  {
    "Type": "Enrollment",
    "Description": "John Doe enrolled in Advanced C# Programming",
    "UserName": "john.doe@example.com",
    "Timestamp": "2025-11-09T22:30:00Z",
    "Icon": "graduation-cap",
    "Severity": "Info"
  }
]
```

### JWT Token Flow

1. **Login**: User authenticates at `/api/auth/login`
2. **Token Issued**: AuthService creates JWT with user claims and roles
3. **Token Storage**: Frontend stores token in localStorage (via TokenService)
4. **Token Usage**: ApiClient attaches token to all requests via `Authorization: Bearer {token}` header
5. **Token Validation**: JWT middleware validates signature, issuer, audience, and expiration
6. **Role Check**: Authorization middleware verifies Admin role claim
7. **Endpoint Access**: If valid, request proceeds to endpoint handler

### Configuration

JWT settings are read from environment variables or appsettings.json:

```json
{
  "Jwt": {
    "Secret": "your-very-long-and-secure-secret-key-minimum-32-characters-long!!",
    "Issuer": "InsightLearn.Api",
    "Audience": "InsightLearn.Users"
  }
}
```

**IMPORTANT**: In production, ensure the JWT secret in the API matches the secret used by AuthService when issuing tokens!

## Files Modified

1. `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs`
   - Added JWT authentication middleware configuration
   - No changes to existing endpoints (they were already implemented)

## Files Created

1. `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/import-api-to-k3s.sh`
   - Deployment script for K3s

2. `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/ADMIN-DASHBOARD-JWT-FIX.md`
   - This documentation

## Testing Checklist

- [ ] API pod restarts successfully after deployment
- [ ] `/api/info` endpoint returns version and features
- [ ] `/api/auth/login` accepts admin credentials and returns JWT token
- [ ] `/api/admin/dashboard/stats` returns 401 without token
- [ ] `/api/admin/dashboard/stats` returns 200 with valid Admin token
- [ ] Frontend admin dashboard loads without errors
- [ ] Dashboard displays correct statistics
- [ ] Recent activity list populates

## Troubleshooting

### Issue: 401 Unauthorized after login

**Cause**: JWT token not being sent or invalid

**Solutions**:
1. Check browser DevTools → Application → Local Storage for `authToken`
2. Verify token is being attached in Network tab (Authorization header)
3. Check API logs for token validation errors

### Issue: 403 Forbidden

**Cause**: User doesn't have Admin role

**Solutions**:
1. Verify user has Admin role in database:
   ```sql
   SELECT u.Email, r.Name
   FROM AspNetUsers u
   JOIN AspNetUserRoles ur ON u.Id = ur.UserId
   JOIN AspNetRoles r ON ur.RoleId = r.Id
   WHERE u.Email = 'admin@insightlearn.cloud';
   ```
2. If missing, assign Admin role via UserManager or SQL

### Issue: Empty dashboard statistics

**Cause**: Database has no data

**Solutions**:
1. Seed sample data via SQL scripts
2. Create test courses and enrollments
3. Check database connectivity

## Security Considerations

1. **JWT Secret**: Must be at least 32 characters and stored securely (environment variables, Kubernetes Secrets)
2. **HTTPS**: Always use HTTPS in production to prevent token interception
3. **Token Expiration**: Configure appropriate token lifetime (default: 7 days from AuthService)
4. **Role-Based Access**: Admin endpoints properly protected with `RequireRole("Admin")`
5. **CORS**: Configured to allow frontend origin (currently `AllowAnyOrigin` - restrict in production!)

## Next Steps

After verifying the admin dashboard works:

1. **Implement remaining admin endpoints** (see CLAUDE.md for full list):
   - User management (GET/PUT/DELETE /api/admin/users/{id})
   - Course management (GET/POST/PUT/DELETE /api/admin/courses/{id})
   - System health monitoring

2. **Add pagination** to dashboard endpoints for large datasets

3. **Implement real-time updates** via SignalR for dashboard statistics

4. **Add audit logging** for all admin actions

## References

- [CLAUDE.md](/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/CLAUDE.md) - Full project documentation
- [Program.cs](/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs) - API endpoints definition
- [AdminDashboardService.cs](/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.WebAssembly/Services/Admin/AdminDashboardService.cs) - Frontend service
- [Dashboard.razor](/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.WebAssembly/Pages/Admin/Dashboard.razor) - Frontend UI

---

**Status**: ✅ JWT authentication middleware implemented and ready for deployment

**Date**: 2025-11-09

**Version**: 1.6.0-dev
