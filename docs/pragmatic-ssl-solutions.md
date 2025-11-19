# Pragmatic SSL Solutions Without Custom Domain

## The Problem

AWS Elastic Beanstalk load balancers use `*.elb.amazonaws.com` domains, but you can't get SSL certificates for domains you don't own. This creates a certificate mismatch issue.

## Common Pragmatic Solutions

### Solution 1: Remove HTTPS Listener (Most Common)

**When to use:** Internal APIs, development, staging, or when behind a proxy

**How it works:**
- Elastic Beanstalk serves HTTP only (port 80)
- No SSL certificate needed
- Simple and straightforward

**Implementation:**
```bash
# Simply don't set SSL_CERTIFICATE_ARN
unset SSL_CERTIFICATE_ARN

# Redeploy
cd infrastructure
npm run deploy:api
```

**Pros:**
- ✅ No certificate management
- ✅ No domain mismatch issues
- ✅ Works immediately
- ✅ Common for internal APIs

**Cons:**
- ❌ No encryption in transit (HTTP only)
- ❌ Not suitable for public-facing APIs handling sensitive data

**Security Note:** If your API is only accessed by your frontend (Amplify), and Amplify uses HTTPS, the frontend-to-backend connection can still be HTTP if both are in AWS's network. However, for production APIs handling sensitive data, consider Solution 2 or 3.

---

### Solution 2: API Gateway in Front (Recommended for Public APIs)

**When to use:** Public APIs that need HTTPS without custom domain

**How it works:**
- API Gateway provides SSL termination with its own domain (e.g., `https://abc123.execute-api.us-east-2.amazonaws.com`)
- Backend stays HTTP-only
- API Gateway handles SSL/TLS

**Implementation:**
1. Keep Elastic Beanstalk HTTP-only (don't set SSL_CERTIFICATE_ARN)
2. Create API Gateway REST API or HTTP API
3. Point API Gateway to Elastic Beanstalk HTTP endpoint
4. Use API Gateway URL in frontend

**Pros:**
- ✅ HTTPS provided by AWS
- ✅ No certificate management
- ✅ Additional features (rate limiting, API keys, etc.)
- ✅ AWS-managed SSL

**Cons:**
- ❌ Additional service to manage
- ❌ Additional cost (~$3.50 per million requests)
- ❌ API Gateway URL instead of direct backend URL

---

### Solution 3: CloudFront in Front

**When to use:** Need global distribution + HTTPS

**How it works:**
- CloudFront provides SSL termination
- Backend stays HTTP-only
- CloudFront handles SSL/TLS

**Implementation:**
1. Keep Elastic Beanstalk HTTP-only
2. Create CloudFront distribution
3. Point CloudFront to Elastic Beanstalk HTTP endpoint
4. Use CloudFront URL in frontend

**Pros:**
- ✅ HTTPS provided by AWS
- ✅ Global CDN (faster worldwide)
- ✅ AWS-managed SSL
- ✅ DDoS protection

**Cons:**
- ❌ Additional service to manage
- ❌ Additional cost (~$0.085 per GB transferred)
- ❌ More complex setup

---

## Recommendation by Use Case

### Internal/Development API
→ **Solution 1: Remove HTTPS**
- Simplest approach
- No additional services
- Works immediately

### Public API (Production)
→ **Solution 2: API Gateway** or **Solution 3: CloudFront**
- Provides HTTPS
- AWS-managed SSL
- Additional features

### API Behind Frontend Only
→ **Solution 1: Remove HTTPS**
- If frontend uses HTTPS and backend is only accessed by frontend
- Backend can be HTTP (internal AWS network)
- Simplest setup

---

## Current Setup Recommendation

For your SmartScheduler project:

**If the API is only accessed by your Amplify frontend:**
- Use **Solution 1** (HTTP only)
- Frontend uses HTTPS (Amplify provides this)
- Backend uses HTTP (acceptable for internal communication)

**If the API needs to be publicly accessible with HTTPS:**
- Use **Solution 2** (API Gateway) for simplicity
- Or **Solution 3** (CloudFront) if you need CDN benefits

---

## Quick Fix for Current Issue

To remove the problematic SSL certificate:

```bash
# Unset the SSL certificate ARN
unset SSL_CERTIFICATE_ARN

# Redeploy API stack (will remove HTTPS listener)
cd infrastructure
npm run deploy:api
```

After deployment, your API will be accessible via HTTP:
```
http://awseb--AWSEB-q47GuHv7JsO3-1005435555.us-east-2.elb.amazonaws.com
```

Update your frontend to use HTTP instead of HTTPS for the API URL.

