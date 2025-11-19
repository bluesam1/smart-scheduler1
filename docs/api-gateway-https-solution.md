# API Gateway HTTPS Solution (Without Custom Domain)

## Problem

- Frontend (Amplify) uses HTTPS
- Backend (Elastic Beanstalk) needs HTTPS to avoid mixed content errors
- Can't get SSL certificates for `*.elb.amazonaws.com` domains
- Don't want to use a custom domain

## Solution: API Gateway

API Gateway provides HTTPS automatically via AWS-managed domain (e.g., `https://abc123.execute-api.us-east-2.amazonaws.com`).

**How it works:**
- API Gateway handles HTTPS termination
- Backend stays HTTP-only (no certificate needed)
- API Gateway proxies all requests to Elastic Beanstalk backend
- Frontend uses API Gateway URL instead of direct backend URL

## Implementation Steps

### Step 1: Remove HTTPS from Elastic Beanstalk

First, ensure Elastic Beanstalk is HTTP-only:

```bash
# Make sure SSL_CERTIFICATE_ARN is not set
unset SSL_CERTIFICATE_ARN

# Redeploy API stack to remove HTTPS listener
cd infrastructure
npm run deploy:api
```

### Step 2: Get Backend HTTP Endpoint

After the API stack is deployed, get the HTTP endpoint:

```bash
aws elasticbeanstalk describe-environments \
  --environment-names production \
  --region us-east-2 \
  --query 'Environments[0].EndpointURL' \
  --output text
```

This will return something like: `awseb--AWSEB-q47GuHv7JsO3-1005435555.us-east-2.elb.amazonaws.com`

### Step 3: Deploy API Gateway Stack

Set the backend endpoint and deploy:

```bash
export BACKEND_ENDPOINT=http://awseb--AWSEB-q47GuHv7JsO3-1005435555.us-east-2.elb.amazonaws.com
cd infrastructure
npm run deploy:api-gateway
```

Or deploy all stacks (API Gateway will only be created if BACKEND_ENDPOINT is set):

```bash
export BACKEND_ENDPOINT=http://awseb--AWSEB-q47GuHv7JsO3-1005435555.us-east-2.elb.amazonaws.com
npm run deploy
```

### Step 4: Get API Gateway URL

After deployment, get the API Gateway URL:

```bash
aws cloudformation describe-stacks \
  --stack-name SmartScheduler-ApiGateway \
  --query 'Stacks[0].Outputs[?OutputKey==`ApiGatewayUrl`].OutputValue' \
  --output text
```

Or check the CloudFormation outputs in AWS Console.

### Step 5: Update Frontend Configuration

Update your Amplify environment variables to use the API Gateway URL:

1. Go to AWS Amplify Console → Your App → Environment variables
2. Update:
   ```
   NEXT_PUBLIC_API_URL=https://abc123.execute-api.us-east-2.amazonaws.com
   NEXT_PUBLIC_SIGNALR_URL=https://abc123.execute-api.us-east-2.amazonaws.com/hubs
   ```

**Note:** SignalR WebSocket connections may need special handling. API Gateway HTTP API supports WebSockets, but you may need to configure it differently.

## Architecture

```
Frontend (HTTPS) → API Gateway (HTTPS) → Elastic Beanstalk (HTTP)
   Amplify              AWS Managed          Backend API
```

## Benefits

- ✅ HTTPS provided automatically by AWS
- ✅ No certificate management
- ✅ No custom domain needed
- ✅ Works immediately
- ✅ Additional features (rate limiting, API keys, etc.)

## Costs

- API Gateway HTTP API: ~$1.00 per million requests
- Data transfer: Standard AWS data transfer rates
- Very cost-effective for most applications

## Limitations

- API Gateway URL instead of custom domain
- WebSocket support may need additional configuration
- Slight latency increase (API Gateway hop)

## Troubleshooting

### CORS Issues

If you encounter CORS errors, update the CORS configuration in `infrastructure/lib/api-gateway-stack.ts`:

```typescript
corsPreflight: {
  allowOrigins: ['https://your-amplify-domain.amplifyapp.com'],
  allowMethods: [apigatewayv2.CorsHttpMethod.ANY],
  allowHeaders: ['*'],
  allowCredentials: true,
  maxAge: cdk.Duration.days(1),
},
```

### WebSocket/SignalR Issues

API Gateway HTTP API supports WebSockets, but SignalR may need special configuration. Consider:
1. Using API Gateway WebSocket API (separate stack)
2. Or keeping direct connection to Elastic Beanstalk for SignalR (if you can accept the certificate warning)

## Alternative: CloudFront

If you need global CDN benefits, you can use CloudFront instead:

1. Create CloudFront distribution
2. Point to Elastic Beanstalk HTTP endpoint
3. CloudFront provides HTTPS automatically

See `docs/pragmatic-ssl-solutions.md` for details.

