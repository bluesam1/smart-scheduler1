# Authentication & Authorization Setup

## Overview

SmartScheduler uses Amazon Cognito for authentication and implements role-based authorization with JWT tokens.

## Amazon Cognito Setup

### 1. Create User Pool

1. Go to AWS Cognito Console
2. Create a new User Pool
3. Configure the following:
   - **Sign-in options**: Email
   - **Password policy**: Default or custom
   - **MFA**: Optional for MVP (can be enabled later)
   - **User attributes**: Email (required), Given name and Family name (optional)

### 2. Create App Client

1. In your User Pool, go to "App integration" → "App clients"
2. Create a new app client with:
   - **App client name**: `smartscheduler-web-client`
   - **Client secret**: **Do NOT generate a client secret** (for SPA/public clients)
   - **Allowed OAuth flows**: Authorization code grant
   - **Allowed OAuth scopes**: `openid`, `email`, `profile`
   - **Allowed callback URLs**: 
     - Development: `http://localhost:3000/auth/callback`
     - Production: `https://your-domain.com/auth/callback`
   - **Allowed sign-out URLs**:
     - Development: `http://localhost:3000/auth/signout`
     - Production: `https://your-domain.com/auth/signout`

### 3. Create User Groups

Create the following groups in your User Pool:

1. **Admin** - Full system access
2. **Dispatcher** - Can view jobs, request recommendations, confirm bookings
3. **Contractor** - Can view own assignments and schedule

To create groups:
1. Go to "User groups" in your User Pool
2. Create each group with appropriate name and description
3. Assign users to groups as needed

### 4. Configure Hosted UI (Optional for MVP)

1. In your User Pool, go to "App integration" → "Domain"
2. Create a Cognito domain (e.g., `smartscheduler.auth.us-east-1.amazoncognito.com`)
3. Configure hosted UI settings:
   - **App client**: Select your app client
   - **Identity providers**: Cognito user pool
   - **Callback URLs**: Same as configured in app client

## Backend Configuration

### Environment Variables

Update `appsettings.json` or set environment variables:

```json
{
  "Cognito": {
    "Region": "us-east-1",
    "UserPoolId": "us-east-1_XXXXXXXXX",
    "AppClientId": "YOUR_APP_CLIENT_ID"
  }
}
```

**Important**: For production, use environment variables or AWS Secrets Manager instead of hardcoding in `appsettings.json`.

### JWT Validation

The backend automatically:
- Validates JWT tokens against Cognito's public keys (JWKS)
- Validates token signature, expiration, issuer, and audience
- Maps Cognito `groups` claim to ASP.NET Core roles
- Enforces role-based authorization policies

### Authorization Policies

Three authorization policies are configured:

1. **Admin**: Requires `Admin` role
2. **Dispatcher**: Requires `Dispatcher` or `Admin` role
3. **Contractor**: Requires `Contractor` or `Admin` role

### Protected Endpoints

All API endpoints require authentication except:
- `/health` - Health check endpoint (public)

To protect an endpoint, use:
```csharp
app.MapGet("/api/endpoint", () => Results.Ok())
    .RequireAuthorization(); // Requires any authenticated user
```

To require a specific role:
```csharp
app.MapGet("/api/admin/endpoint", () => Results.Ok())
    .RequireAuthorization("Admin"); // Requires Admin role
```

## Frontend Integration

### Token Flow

1. User logs in via Cognito Hosted UI
2. Cognito redirects to callback URL with authorization code
3. Frontend exchanges code for tokens (access token, ID token, refresh token)
4. Frontend stores tokens (in memory or session storage)
5. Frontend includes access token in API requests: `Authorization: Bearer <token>`

### Token Refresh

- Access tokens expire after 60 minutes (default)
- Refresh tokens expire after 30 days (default)
- Frontend should implement automatic token refresh before expiration
- Use Cognito's token refresh endpoint to get new access token

### Example Frontend Code (Next.js)

```typescript
// Get tokens from Cognito
const response = await fetch('https://cognito-idp.us-east-1.amazonaws.com/', {
  method: 'POST',
  headers: { 'Content-Type': 'application/x-amzn-json-1.1' },
  body: JSON.stringify({
    ClientId: process.env.NEXT_PUBLIC_COGNITO_APP_CLIENT_ID,
    AuthFlow: 'USER_PASSWORD_AUTH',
    AuthParameters: {
      USERNAME: username,
      PASSWORD: password
    }
  })
});

// Include token in API requests
const apiResponse = await fetch('http://localhost:5004/api/endpoint', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});
```

## Testing

### Test Users

Create test users in Cognito User Pool:
1. Go to "Users" in your User Pool
2. Create users with email addresses
3. Assign users to appropriate groups (Admin, Dispatcher, Contractor)
4. Set temporary passwords (users will be required to change on first login)

### Integration Tests

The test suite includes:
- Authorization policy tests for each role
- Unauthorized access tests
- Health check public access test

Run tests:
```bash
cd src
dotnet test
```

## Security Considerations

1. **Never commit Cognito credentials** to version control
2. **Use environment variables** or AWS Secrets Manager for production
3. **Enable HTTPS** in production
4. **Configure CORS** properly (already configured for `http://localhost:3000`)
5. **Token validation** is automatic - backend validates all tokens
6. **Clock skew** is set to zero for strict token expiration validation

## Troubleshooting

### 401 Unauthorized

- Check that token is included in `Authorization` header
- Verify token hasn't expired
- Ensure Cognito configuration matches (UserPoolId, AppClientId, Region)

### 403 Forbidden

- Verify user is in the correct Cognito group
- Check that authorization policy matches the required role
- Ensure `cognito:groups` claim is present in token

### Token Validation Errors

- Verify `Cognito:UserPoolId` and `Cognito:AppClientId` are correct
- Check that token issuer matches Cognito User Pool
- Ensure token audience matches App Client ID

## Next Steps

- Story 0.3: SignalR Real-time Setup (will use same authentication)
- Story 0.4: Connect Frontend to Backend (will implement token handling)

