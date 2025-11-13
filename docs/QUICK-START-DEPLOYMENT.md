# Quick Start: Deploy SmartScheduler to AWS

This is a quick reference guide for deploying SmartScheduler to AWS. For detailed instructions, see [DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md).

## Prerequisites Checklist

- [ ] AWS CLI installed and configured (`aws configure`)
- [ ] Node.js 18+ and npm installed
- [ ] AWS CDK CLI installed (`npm install -g aws-cdk`)
- [ ] .NET 8 SDK installed
- [ ] AWS Account ID and Region
- [ ] OpenRouteService API key
- [ ] Google Places API key (optional)
- [ ] Cognito User Pool ID (if using existing) or will create new one

## Quick Deployment (Automated)

### Option 1: Full Automated Deployment

```bash
# Set environment variables
export CDK_DEFAULT_ACCOUNT=YOUR_AWS_ACCOUNT_ID
export CDK_DEFAULT_REGION=us-east-1
export COGNITO_USER_POOL_ID=us-east-2_oGumIWt36  # If using existing
export COGNITO_APP_CLIENT_ID=4rps8b0oldpuan0qs2dnk37odd  # If using existing
export GITHUB_REPOSITORY=YOUR_ORG/smart-scheduler1

# Run deployment script
./scripts/deploy-to-aws.sh
```

### Option 2: Step-by-Step Deployment

```bash
# 1. Set environment variables
export CDK_DEFAULT_ACCOUNT=YOUR_AWS_ACCOUNT_ID
export CDK_DEFAULT_REGION=us-east-1

# 2. Deploy infrastructure
cd infrastructure
npm install
npm run build
cdk bootstrap aws://${CDK_DEFAULT_ACCOUNT}/${CDK_DEFAULT_REGION}  # First time only
npm run deploy:database
npm run deploy:secrets
npm run deploy:storage
npm run deploy:cognito  # Or set COGNITO_USER_POOL_ID to use existing
npm run deploy:api
npm run deploy:frontend

# 3. Configure secrets
cd ..
./scripts/configure-secrets.sh

# 4. Set up database
./scripts/setup-database.sh

# 5. Deploy application
./scripts/deploy-api.sh
```

## Manual Steps After Infrastructure Deployment

### 1. Update Secrets Manager

```bash
# Database connection string (automated in configure-secrets.sh)
# API keys (prompted in configure-secrets.sh)
./scripts/configure-secrets.sh
```

### 2. Enable PostGIS and Run Migrations

```bash
./scripts/setup-database.sh
```

### 3. Deploy Application Code

```bash
./scripts/deploy-api.sh
```

### 4. Update Frontend Environment Variables

After API is deployed, get the API URL:

```bash
API_URL=$(aws elasticbeanstalk describe-environments \
  --environment-names production \
  --query 'Environments[0].EndpointURL' \
  --output text)
```

Then update in AWS Amplify Console:
- Go to Amplify Console → Your App → Environment variables
- Update `NEXT_PUBLIC_API_URL` = `https://${API_URL}`
- Update `NEXT_PUBLIC_SIGNALR_URL` = `https://${API_URL}/hubs`

## Verify Deployment

```bash
# Check API health
curl https://$(aws elasticbeanstalk describe-environments \
  --environment-names production \
  --query 'Environments[0].EndpointURL' \
  --output text)/health

# Should return: {"status":"Healthy"}
```

## Common Issues

### CDK Bootstrap Error

If you get bootstrap errors, check with your AWS account admin - it may already be bootstrapped:

```bash
aws cloudformation describe-stacks --stack-name CDKToolkit
```

### Database Connection Issues

1. Check security groups allow PostgreSQL (port 5432) from Elastic Beanstalk
2. Verify connection string in Secrets Manager
3. Check RDS endpoint

### Elastic Beanstalk Deployment Fails

1. Check CloudWatch logs: `/aws/elasticbeanstalk/smartscheduler-api-production/var/log/eb-engine.log`
2. Verify IAM roles have correct permissions
3. Check health check endpoint is accessible

## Cost Estimate

Approximate monthly costs:
- RDS PostgreSQL (db.t3.micro): ~$15-20/month
- Elastic Beanstalk (t3.small): ~$15-30/month
- Amplify Hosting: ~$0-5/month (first 10GB free)
- Secrets Manager (3 secrets): ~$1.20/month
- **Total: ~$30-55/month**

## Next Steps

1. Set up CI/CD pipeline for automated deployments
2. Configure CloudWatch alarms
3. Set up custom domain
4. Enable additional security features

## Need Help?

See [DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md) for detailed instructions and troubleshooting.

