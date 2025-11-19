# SSL Certificate Troubleshooting Guide

## Current Issue

The backend SSL certificate is causing browser errors due to:

1. **Domain Mismatch**: Certificate is for `*.elasticbeanstalk.com` but load balancer uses `*.elb.amazonaws.com`
2. **Outdated SSL Policy**: Using `ELBSecurityPolicy-TLS-1-2-2017-01` (should be `ELBSecurityPolicy-TLS13-1-2-2021-06`)

## Root Cause

AWS Elastic Beanstalk load balancers use `*.elb.amazonaws.com` domains, but the certificate is for `*.elasticbeanstalk.com`. These are different domains, causing certificate validation failures.

## Solutions

### Option 1: Use Custom Domain (Recommended for Production)

1. **Request a certificate for your custom domain:**
   ```bash
   ./scripts/request-ssl-certificate.sh api.yourdomain.com
   ```

2. **Add DNS validation records** to your domain's DNS

3. **Wait for certificate validation** (5-30 minutes)

4. **Set the certificate ARN:**
   ```bash
   export SSL_CERTIFICATE_ARN=arn:aws:acm:us-east-2:ACCOUNT:certificate/CERT_ID
   ```

5. **Redeploy the API stack:**
   ```bash
   cd infrastructure
   npm run deploy:api
   ```

6. **Configure custom domain in Elastic Beanstalk:**
   - Go to AWS Console > Elastic Beanstalk > Your Environment > Configuration > Load Balancer
   - Add a custom domain and associate it with your certificate

### Option 2: Update SSL Policy Only (Quick Fix)

This fixes the SSL policy but won't resolve the domain mismatch:

1. **Redeploy with updated SSL policy:**
   ```bash
   cd infrastructure
   npm run deploy:api
   ```

2. **Note**: Browser will still show certificate warnings due to domain mismatch

### Option 3: Remove HTTPS Listener (Not Recommended)

Only use this for development/testing:

1. **Remove SSL certificate ARN from deployment:**
   ```bash
   unset SSL_CERTIFICATE_ARN
   cd infrastructure
   npm run deploy:api
   ```

2. **Access via HTTP only** (not secure for production)

## Verification Steps

After applying a fix:

1. **Check certificate status:**
   ```bash
   ./scripts/check-ssl-certificate.sh [certificate-arn]
   ```

2. **Test SSL connection:**
   ```bash
   curl -vI https://your-backend-url
   ```

3. **Check browser console** for specific error messages

4. **Verify SSL policy:**
   ```bash
   aws elbv2 describe-listeners \
     --load-balancer-arn [load-balancer-arn] \
     --region us-east-2 \
     --query 'Listeners[?Port==`443`].SslPolicy' \
     --output text
   ```

## Common Certificate Errors

### "NET::ERR_CERT_AUTHORITY_INVALID"
- **Cause**: Certificate not trusted or domain mismatch
- **Fix**: Use a valid certificate from ACM that matches your domain

### "NET::ERR_CERT_COMMON_NAME_INVALID"
- **Cause**: Certificate domain doesn't match the URL
- **Fix**: Request certificate for the exact domain or use wildcard certificate

### "SEC_E_UNTRUSTED_ROOT"
- **Cause**: Certificate chain incomplete or self-signed
- **Fix**: Use AWS Certificate Manager (ACM) certificates (automatically trusted)

## Current Configuration

- **Load Balancer**: `awseb--AWSEB-q47GuHv7JsO3-1005435555.us-east-2.elb.amazonaws.com`
- **Certificate**: `arn:aws:acm:us-east-2:971422717446:certificate/bd0ca71a-2812-487d-9cd5-e6dc79a9c000`
- **Certificate Domain**: `*.elasticbeanstalk.com`
- **SSL Policy**: `ELBSecurityPolicy-TLS-1-2-2017-01` (needs update)

## Next Steps

1. Decide on a custom domain for your API
2. Request a certificate for that domain
3. Redeploy with the new certificate ARN
4. Configure custom domain in Elastic Beanstalk

