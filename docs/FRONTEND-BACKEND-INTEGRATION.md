# Frontend-Backend Integration Guide

This guide covers the specific steps needed to connect your frontend (deployed on AWS Amplify) with your backend API (deployed on Elastic Beanstalk).

## Required Configuration

### 1. Backend CORS Configuration

The backend currently only allows requests from `http://localhost:3000`. You need to add your Amplify frontend domain.

**Update `src/SmartScheduler.Api/Program.cs`:**

```csharp
// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

**Update `src/SmartScheduler.Api/appsettings.json`:**

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://main.d3jy9zozo7c113.amplifyapp.com",
      "https://*.amplifyapp.com"
    ]
  },
  // ... rest of config
}
```

**Note:** For production, you may want to use a more specific pattern or environment variables.

### 2. Frontend Environment Variables

The frontend needs these environment variables set in AWS Amplify:

1. **Go to AWS Amplify Console** → Your App → Environment variables
2. **Add/Update these variables:**

```
NEXT_PUBLIC_API_URL=https://your-elastic-beanstalk-url.elasticbeanstalk.com
NEXT_PUBLIC_SIGNALR_URL=https://your-elastic-beanstalk-url.elasticbeanstalk.com/hubs
NEXT_PUBLIC_COGNITO_USER_POOL_ID=us-east-2_oGumIWt36
NEXT_PUBLIC_COGNITO_CLIENT_ID=4rps8b0oldpuan0qs2dnk37odd
NEXT_PUBLIC_COGNITO_REGION=us-east-2
```

**To get your Elastic Beanstalk URL:**

```bash
aws elasticbeanstalk describe-environments \
  --environment-names production \
  --query 'Environments[0].EndpointURL' \
  --output text
```

### 3. Backend Environment Variables (Elastic Beanstalk)

The backend needs these environment variables in Elastic Beanstalk:

1. **Go to AWS Elastic Beanstalk Console** → Your Environment → Configuration → Software
2. **Add/Update these environment variables:**

```
Cognito__Region=us-east-2
Cognito__UserPoolId=us-east-2_oGumIWt36
Cognito__AppClientId=4rps8b0oldpuan0qs2dnk37odd
ConnectionStrings__DefaultConnection=<from Secrets Manager>
OpenRouteService__ApiKey=<from Secrets Manager>
GooglePlaces__ApiKey=<from Secrets Manager>
```

**To get secrets from Secrets Manager:**

```bash
# Database connection string
aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/connection-string \
  --query SecretString --output text

# OpenRouteService API key
aws secretsmanager get-secret-value \
  --secret-id smartscheduler/api-keys/openrouteservice \
  --query SecretString --output text | jq -r .ApiKey

# Google Places API key
aws secretsmanager get-secret-value \
  --secret-id smartscheduler/api-keys/google-places \
  --query SecretString --output text | jq -r .ApiKey
```

### 4. Cognito App Client Configuration

Make sure your Cognito App Client has the correct callback URLs:

1. **Go to AWS Cognito Console** → User Pools → Your Pool → App integration → App clients
2. **Edit your app client** (`4rps8b0oldpuan0qs2dnk37odd`)
3. **Update Callback URLs:**
   - `http://localhost:3000/auth/callback` (for local dev)
   - `https://main.d3jy9zozo7c113.amplifyapp.com/auth/callback` (your Amplify URL)
   - `https://*.amplifyapp.com/auth/callback` (wildcard for all Amplify branches)
4. **Update Sign-out URLs:**
   - `http://localhost:3000/auth/signout`
   - `https://main.d3jy9zozo7c113.amplifyapp.com/auth/signout`
   - `https://*.amplifyapp.com/auth/signout`

## Quick Setup Script

Here's a script to help you set everything up:

```bash
#!/bin/bash

# Get API URL
API_URL=$(aws elasticbeanstalk describe-environments \
  --environment-names production \
  --query 'Environments[0].EndpointURL' \
  --output text)

echo "API URL: ${API_URL}"

# Get Amplify App URL
AMPLIFY_URL=$(aws amplify get-app \
  --app-id $(aws cloudformation describe-stacks \
    --stack-name SmartScheduler-Frontend \
    --query 'Stacks[0].Outputs[?OutputKey==`AppUrl`].OutputValue' \
    --output text | cut -d'/' -f3) \
  --query 'app.defaultDomain' \
  --output text 2>/dev/null || echo "main.d3jy9zozo7c113.amplifyapp.com")

echo "Amplify URL: https://${AMPLIFY_URL}"

echo ""
echo "1. Update Amplify Environment Variables:"
echo "   NEXT_PUBLIC_API_URL=https://${API_URL}"
echo "   NEXT_PUBLIC_SIGNALR_URL=https://${API_URL}/hubs"
echo "   NEXT_PUBLIC_COGNITO_USER_POOL_ID=us-east-2_oGumIWt36"
echo "   NEXT_PUBLIC_COGNITO_CLIENT_ID=4rps8b0oldpuan0qs2dnk37odd"
echo "   NEXT_PUBLIC_COGNITO_REGION=us-east-2"
echo ""
echo "2. Update Cognito App Client Callback URLs:"
echo "   https://${AMPLIFY_URL}/auth/callback"
echo "   https://${AMPLIFY_URL}/auth/signout"
echo ""
echo "3. Update backend CORS to allow:"
echo "   https://${AMPLIFY_URL}"
```

## Testing the Integration

### 1. Test API Health Endpoint

```bash
# Should return {"status":"Healthy"}
curl https://your-api-url.elasticbeanstalk.com/health
```

### 2. Test Frontend API Connection

1. Open your Amplify app in a browser
2. Open browser DevTools → Network tab
3. Try to log in or make an API call
4. Check for CORS errors in the console

### 3. Test Authentication Flow

1. Navigate to the login page
2. Log in with a test user
3. Verify the JWT token is stored
4. Verify API calls include the Authorization header

## Troubleshooting

### CORS Errors

**Error:** `Access to fetch at '...' from origin '...' has been blocked by CORS policy`

**Solution:**
1. Verify the frontend URL is in the backend's CORS allowed origins
2. Check that `AllowCredentials()` is enabled in CORS config
3. Ensure the backend is using HTTPS (Amplify requires HTTPS)

### 401 Unauthorized Errors

**Error:** `401 Unauthorized` when making API calls

**Solution:**
1. Verify the JWT token is being sent in the Authorization header
2. Check that Cognito User Pool ID and App Client ID are correct
3. Verify the token hasn't expired
4. Check backend logs for JWT validation errors

### SignalR Connection Issues

**Error:** SignalR connection fails

**Solution:**
1. Verify `NEXT_PUBLIC_SIGNALR_URL` is set correctly
2. Check that the SignalR hub endpoint is accessible
3. Verify WebSocket support in Elastic Beanstalk load balancer
4. Check that sticky sessions are enabled (required for SignalR)

### Environment Variables Not Working

**Issue:** Frontend can't read environment variables

**Solution:**
1. Ensure variables start with `NEXT_PUBLIC_` prefix
2. Redeploy the Amplify app after changing environment variables
3. Clear browser cache and hard refresh
4. Check build logs in Amplify Console

## Next Steps

After completing these steps:

1. **Redeploy the frontend** in Amplify Console to pick up new environment variables
2. **Redeploy the backend** if you updated CORS configuration
3. **Test the full authentication flow** end-to-end
4. **Monitor CloudWatch logs** for any errors

## References

- [AWS Amplify Environment Variables](https://docs.aws.amazon.com/amplify/latest/userguide/environment-variables.html)
- [Elastic Beanstalk Environment Variables](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/environments-cfg-softwaresettings.html)
- [Cognito App Client Configuration](https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-client-apps.html)

