# JWT Authentication Fix - IDX10517 Error

## Problem Summary

JWT Bearer authentication was failing with error:
```
IDX10517: Signature validation failed. Unable to match key
SecurityTokenSignatureKeyNotFoundException: The token's kid is missing
```

## Root Cause

**Configuration mismatch between Development and Production environments:**

### Before Fix:

**appsettings.Development.json:**
```json
"Jwt": {
  "Key": "dev-local-key-change-me-please-32charsmin",
  "Issuer": "KatanaDev",
  "Audience": "KatanaDev"
}
```

**appsettings.json (Production):**
```json
"Jwt": {
  "Key": "katana-super-secret-jwt-key-2025-minimum-32-characters-required",
  "Issuer": "KatanaAPI",
  "Audience": "KatanaWebApp"
}
```

### The Issue:
1. **Issuer mismatch**: Token generated with "KatanaDev" but validated against "KatanaAPI"
2. **Audience mismatch**: Token generated with "KatanaDev" but validated against "KatanaWebApp"
3. **Key mismatch**: Different signing keys between environments
4. **Missing kid**: HS256 (symmetric) doesn't use kid, but error suggests validation was looking for it

## Applied Fixes

### 1. Unified JWT Configuration
✅ Updated `publish_new/appsettings.Development.json` to match production settings:
```json
"Jwt": {
  "Key": "katana-super-secret-jwt-key-2025-minimum-32-characters-required",
  "Issuer": "KatanaAPI",
  "Audience": "KatanaWebApp",
  "ExpiryMinutes": 20
}
```

### 2. Enhanced JWT Validation Logging
✅ Added diagnostic event handlers in `Program.cs`:
- `OnAuthenticationFailed`: Logs detailed error information
- `OnTokenValidated`: Confirms successful validation
- `ClockSkew`: Added 5-minute tolerance for time differences

### 3. Diagnostic Script
✅ Created `test-jwt-auth.ps1` to test authentication flow:
- Login and token generation
- Token decoding and inspection
- Protected endpoint testing

## How to Test

### 1. Rebuild and restart the backend:
```powershell
# Quick rebuild
.\QUICK-REBUILD-TEST.ps1

# Or full rebuild
docker-compose down
docker-compose up --build -d
```

### 2. Run diagnostic script:
```powershell
.\test-jwt-auth.ps1
```

Expected output:
```
✓ Login successful
✓ Token decoded: iss=KatanaAPI, aud=KatanaWebApp
✓ All endpoints return 200 or expected status
```

### 3. Test from frontend:
```bash
cd frontend/katana-web
npm start
```

Login with:
- Username: `admin`
- Password: `Katana2025!`

Navigate to protected pages and verify no 401 errors.

## Verification Checklist

- [ ] Backend starts without errors
- [ ] Login returns valid JWT token
- [ ] Token contains correct issuer (`KatanaAPI`)
- [ ] Token contains correct audience (`KatanaWebApp`)
- [ ] Protected endpoints accept the token
- [ ] SignalR connection works with query string token
- [ ] Frontend can access all protected routes
- [ ] No IDX10517 errors in backend logs

## Common Issues & Solutions

### Issue: Still getting 401 errors
**Solution**: Clear browser localStorage and login again
```javascript
// In browser console:
localStorage.clear();
// Then refresh and login
```

### Issue: Token expired immediately
**Solution**: Check system time synchronization
```powershell
# Windows
w32tm /resync

# Check current time
Get-Date
```

### Issue: Different error (IDX10205, IDX10214)
**Solution**: Check specific validation parameter:
- IDX10205: Token lifetime validation failed (expired)
- IDX10214: Audience validation failed
- IDX10211: Issuer validation failed

## Backend Configuration Reference

### JWT Settings (appsettings.json):
```json
{
  "Jwt": {
    "Key": "minimum-32-characters-required-for-hs256",
    "Issuer": "KatanaAPI",
    "Audience": "KatanaWebApp",
    "ExpiryMinutes": 20
  }
}
```

### Token Generation (AuthController.cs):
- Algorithm: HS256 (HMAC-SHA256)
- Claims: sub, name, jti, role
- Expiry: Configurable (default 20 minutes)

### Token Validation (Program.cs):
- ValidateIssuer: true
- ValidateAudience: true
- ValidateLifetime: true
- ValidateIssuerSigningKey: true
- ClockSkew: 5 minutes

## Security Notes

⚠️ **Production Recommendations:**

1. **Change JWT Key**: Use a strong, unique key (minimum 32 characters)
   ```bash
   # Generate secure key:
   openssl rand -base64 32
   ```

2. **Use Environment Variables**: Don't commit secrets to git
   ```json
   "Jwt": {
     "Key": "${JWT_SECRET_KEY}",
     "Issuer": "KatanaAPI",
     "Audience": "KatanaWebApp"
   }
   ```

3. **HTTPS Only**: Ensure production uses HTTPS
4. **Short Expiry**: Keep token expiry short (15-30 minutes)
5. **Refresh Tokens**: Implement refresh token mechanism for better UX

## Monitoring

Check backend logs for JWT-related messages:
```bash
docker logs katana-backend | grep -i "jwt\|authentication\|401"
```

Look for:
- ✅ "JWT Token validated successfully"
- ❌ "JWT Authentication failed"
- ❌ "IDX10517" or other IDX errors

## Related Files

- `src/Katana.API/Program.cs` - JWT configuration
- `src/Katana.API/Controllers/AuthController.cs` - Token generation
- `publish_new/appsettings.json` - Production config
- `publish_new/appsettings.Development.json` - Development config
- `frontend/katana-web/src/services/api.ts` - Frontend token handling
- `test-jwt-auth.ps1` - Diagnostic script
