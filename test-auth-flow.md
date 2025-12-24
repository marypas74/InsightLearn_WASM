# Authentication Flow Debug Guide

## Problem Fixed
The login redirect loop was caused by using `Navigation.NavigateTo(redirectUrl, true)` with the `true` parameter forcing a full page reload. This caused:

1. Full page reload after successful login
2. Blazor WebAssembly app restarts from scratch
3. Authentication state temporarily lost during restart
4. Dashboard page checks auth state before it's restored
5. User redirected back to login page
6. Infinite loop

## The Fix Applied

### Files Modified:

1. **src/InsightLearn.WebAssembly/Pages/Login.razor**
   - Line 195: Changed `Navigation.NavigateTo(redirectUrl, true)` to `Navigation.NavigateTo(redirectUrl)`
   - Added console logging for debugging

2. **src/InsightLearn.WebAssembly/Pages/Register.razor**
   - Line 120: Changed `Navigation.NavigateTo("/complete-registration", true)` to `Navigation.NavigateTo("/complete-registration")`

3. **src/InsightLearn.WebAssembly/Services/Auth/AuthService.cs**
   - Added detailed logging for token save operations

4. **src/InsightLearn.WebAssembly/Services/Auth/JwtAuthenticationStateProvider.cs**
   - Added comprehensive logging for authentication state checks

## How to Test the Fix

### 1. Browser Console Debug
Open browser DevTools console (F12) and watch for these log messages:

```javascript
// Expected flow after login:
[LOGIN] Login successful for user
[LOGIN] Token saved to localStorage
[LOGIN] Auth state updated
// Should navigate to dashboard without reload
```

### 2. Manual localStorage Test
In browser console, run:

```javascript
// Check if token is saved after login
localStorage.getItem('InsightLearn.AuthToken')

// Should return a JWT token string like:
// "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

// Check refresh token
localStorage.getItem('InsightLearn.RefreshToken')
```

### 3. Test Login Flow

1. Clear browser cache and localStorage:
```javascript
localStorage.clear();
sessionStorage.clear();
```

2. Navigate to: https://www.insightlearn.cloud/login

3. Login with test credentials:
   - Email: admin@insightlearn.cloud
   - Password: (from .env ADMIN_PASSWORD)

4. **Expected Result**:
   - Login successful toast message
   - Smooth navigation to /dashboard (no page reload)
   - Dashboard checks user role
   - If Admin, redirects to /admin/dashboard
   - Admin dashboard loads successfully
   - **NO redirect back to login**

### 4. Verify Auth Persistence

After successful login:

1. Open new tab with same domain
2. Navigate directly to /admin/dashboard
3. Should load without requiring login

### 5. Debug Auth State

In browser console:

```javascript
// Get current auth state
const token = localStorage.getItem('InsightLearn.AuthToken');
if (token) {
    // Decode JWT (base64)
    const parts = token.split('.');
    const payload = JSON.parse(atob(parts[1]));
    console.log('User:', payload);
    console.log('Expires:', new Date(payload.exp * 1000));
}
```

## Troubleshooting

### If Still Redirecting to Login:

1. **Check token is saved**:
```javascript
console.log('Token:', localStorage.getItem('InsightLearn.AuthToken'));
```

2. **Check browser network tab**:
   - Look for /api/auth/login request
   - Verify it returns 200 with token in response

3. **Check for errors in console**:
   - Look for any JavaScript errors
   - Check for CORS issues

4. **Verify no forced reloads**:
   - Search codebase for `NavigateTo(.*true)`
   - Should only be true for external OAuth redirects

### Common Issues:

1. **Token not saving**: Check browser localStorage is enabled
2. **CORS errors**: Verify API allows origin https://www.insightlearn.cloud
3. **JWT expired**: Token validity is 7 days by default
4. **Wrong localStorage key**: Should be "InsightLearn.AuthToken"

## Deployment

After fixing, rebuild and redeploy:

```bash
# Build Web Assembly
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM
dotnet publish src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj -c Release -o ./publish/web

# Copy to deployment
cp -r ./publish/web/* /path/to/nginx/wwwroot/

# Or if using Docker
docker-compose build web
docker-compose up -d web
```

## Monitoring

Watch application logs for auth flow:

```bash
# If using Kubernetes
kubectl logs -f deployment/insightlearn-web -n insightlearn

# Browser console for client-side logs
# F12 -> Console tab
```

## Success Criteria

✅ User can login without redirect loop
✅ Token saved to localStorage
✅ Navigation is smooth (no page reload)
✅ Dashboard loads after login
✅ Admin users reach /admin/dashboard
✅ Auth state persists across tabs
✅ Logout clears tokens properly