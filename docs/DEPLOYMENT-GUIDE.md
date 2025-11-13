# SmartScheduler AWS Deployment Guide

This guide walks you through deploying SmartScheduler to AWS step-by-step.

## Prerequisites

Before starting, ensure you have:

1. **AWS CLI** installed and configured
   ```bash
   aws --version
   aws configure
   ```

2. **Node.js** 18+ and npm
   ```bash
   node --version
   npm --version
   ```

3. **AWS CDK CLI** installed globally
   ```bash
   npm install -g aws-cdk
   cdk --version
   ```

4. **.NET 8 SDK** installed
   ```bash
   dotnet --version
   ```

5. **AWS Account Access**
   - AWS Account ID
   - Region (e.g., `us-east-1`)
   - IAM permissions to create resources

6. **API Keys Ready**
   - OpenRouteService API key
   - Google Places API key (optional)

7. **Cognito User Pool** (already exists: `us-east-2_oGumIWt36`)
   - User Pool ID: `us-east-2_oGumIWt36`
   - App Client ID: `4rps8b0oldpuan0qs2dnk37odd`
   - Region: `us-east-2`

## Deployment Overview

The deployment consists of 5 main steps:

1. **Bootstrap CDK** (one-time per account/region)
2. **Deploy Infrastructure** (Database, Secrets, Storage, API, Frontend)
3. **Configure Secrets** (API keys, database connection string)
4. **Deploy Application** (.NET API to Elastic Beanstalk)
5. **Run Database Migrations** (PostGIS extension, schema)

## Step-by-Step Deployment

### Step 1: Set Environment Variables

Set your AWS account and region:

```bash
export CDK_DEFAULT_ACCOUNT=YOUR_AWS_ACCOUNT_ID
export CDK_DEFAULT_REGION=us-east-1  # or your preferred region
export AWS_REGION=us-east-1
```

Or use AWS CLI profile:

```bash
export AWS_PROFILE=your-profile-name
```

### Step 2: Bootstrap CDK (First Time Only)

**⚠️ Important:** Check with your AWS account administrator first - CDK may already be bootstrapped.

```bash
cd infrastructure
npm install
cdk bootstrap aws://${CDK_DEFAULT_ACCOUNT}/${CDK_DEFAULT_REGION}
```

### Step 3: Deploy Infrastructure Stacks

Deploy stacks in order:

#### 3.1 Deploy Database Stack

```bash
cd infrastructure
npm run deploy:database
```

This creates:
- RDS PostgreSQL 17.6 instance
- Security groups
- CloudWatch log groups

**Wait for completion** (~10-15 minutes). Note the database endpoint from the outputs.

#### 3.2 Deploy Secrets Stack

```bash
npm run deploy:secrets
```

This creates Secrets Manager secrets (with placeholder values).

#### 3.3 Update Database Connection String Secret

After the database is created, update the connection string:

```bash
# Get database endpoint
DB_ENDPOINT=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Database \
  --query "Stacks[0].Outputs[?OutputKey=='DatabaseEndpoint'].OutputValue" \
  --output text)

# Get database password from master credentials secret
DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString --output text | jq -r .password)

# Update connection string secret
aws secretsmanager update-secret \
  --secret-id smartscheduler/database/connection-string \
  --secret-string "Host=${DB_ENDPOINT};Port=5432;Database=smartscheduler;Username=dbadmin;Password=${DB_PASSWORD}"
```

#### 3.4 Set API Keys in Secrets Manager

```bash
# OpenRouteService API key
aws secretsmanager update-secret \
  --secret-id smartscheduler/api-keys/openrouteservice \
  --secret-string '{"ApiKey":"YOUR_OPENROUTESERVICE_API_KEY"}'

# Google Places API key (optional)
aws secretsmanager update-secret \
  --secret-id smartscheduler/api-keys/google-places \
  --secret-string '{"ApiKey":"YOUR_GOOGLE_PLACES_API_KEY"}'
```

#### 3.5 Deploy Storage Stack

```bash
npm run deploy:storage
```

This creates an S3 bucket for deployment artifacts.

#### 3.6 Deploy API Stack

```bash
npm run deploy:api
```

This creates:
- Elastic Beanstalk application and environment
- IAM roles and policies
- CloudWatch log groups

**Wait for completion** (~10-15 minutes). Note the Elastic Beanstalk environment URL.

#### 3.7 Get API URL

After API stack deployment, get the API endpoint:

```bash
API_URL=$(aws elasticbeanstalk describe-environments \
  --environment-names production \
  --query 'Environments[0].EndpointURL' \
  --output text)

echo "API URL: ${API_URL}"
```

#### 3.8 Deploy Frontend Stack

Set Cognito configuration and deploy:

```bash
export COGNITO_USER_POOL_ID=us-east-2_oGumIWt36
export COGNITO_APP_CLIENT_ID=4rps8b0oldpuan0qs2dnk37odd
export COGNITO_REGION=us-east-2
export GITHUB_REPOSITORY=YOUR_ORG/smart-scheduler1
export GITHUB_BRANCH=main
export API_URL=${API_URL}  # From previous step

npm run deploy:frontend
```

**Note:** If you don't have a GitHub token, you can connect the repository manually via the Amplify Console after deployment.

### Step 4: Post-Deployment Database Setup

#### 4.1 Enable PostGIS Extension

Connect to the database and enable PostGIS:

```bash
# Get database endpoint
DB_ENDPOINT=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Database \
  --query "Stacks[0].Outputs[?OutputKey=='DatabaseEndpoint'].OutputValue" \
  --output text)

# Get database password
DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString --output text | jq -r .password)

# Connect and enable PostGIS (requires psql)
PGPASSWORD=${DB_PASSWORD} psql -h ${DB_ENDPOINT} -U dbadmin -d smartscheduler -c "CREATE EXTENSION IF NOT EXISTS postgis;"
PGPASSWORD=${DB_PASSWORD} psql -h ${DB_ENDPOINT} -U dbadmin -d smartscheduler -c "CREATE EXTENSION IF NOT EXISTS postgis_topology;"
```

#### 4.2 Run Database Migrations

Run EF Core migrations:

```bash
# Get connection string
CONNECTION_STRING=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/connection-string \
  --query SecretString --output text)

# Run migrations
cd ../src
dotnet ef database update \
  --project SmartScheduler.Infrastructure \
  --startup-project SmartScheduler.Api \
  --connection "${CONNECTION_STRING}"
```

### Step 5: Deploy Application Code

#### 5.1 Build and Package .NET API

```bash
cd src/SmartScheduler.Api
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../SmartScheduler.Api.zip .
cd ..
```

#### 5.2 Upload to S3

```bash
# Get deployment bucket name
BUCKET_NAME=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Storage \
  --query "Stacks[0].Outputs[?OutputKey=='DeploymentBucketName'].OutputValue" \
  --output text)

# Upload to S3
aws s3 cp SmartScheduler.Api.zip s3://${BUCKET_NAME}/SmartScheduler.Api.zip
```

#### 5.3 Deploy to Elastic Beanstalk

```bash
# Get application name
APP_NAME=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Api \
  --query "Stacks[0].Outputs[?OutputKey=='ApplicationName'].OutputValue" \
  --output text)

# Create application version
aws elasticbeanstalk create-application-version \
  --application-name ${APP_NAME} \
  --version-label v1.0.0 \
  --source-bundle S3Bucket=${BUCKET_NAME},S3Key=SmartScheduler.Api.zip

# Deploy to environment
aws elasticbeanstalk update-environment \
  --environment-name production \
  --version-label v1.0.0
```

#### 5.4 Configure Elastic Beanstalk Environment Variables

Set environment variables in Elastic Beanstalk:

```bash
# Get secrets
DB_CONNECTION=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/connection-string \
  --query SecretString --output text)

ORS_API_KEY=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/api-keys/openrouteservice \
  --query SecretString --output text | jq -r .ApiKey)

GOOGLE_PLACES_KEY=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/api-keys/google-places \
  --query SecretString --output text | jq -r .ApiKey)

# Update environment configuration
aws elasticbeanstalk update-environment \
  --environment-name production \
  --option-settings \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=ConnectionStrings__DefaultConnection,Value="${DB_CONNECTION}" \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Cognito__Region,Value="us-east-2" \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Cognito__UserPoolId,Value="us-east-2_oGumIWt36" \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Cognito__AppClientId,Value="4rps8b0oldpuan0qs2dnk37odd" \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=OpenRouteService__ApiKey,Value="${ORS_API_KEY}" \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=GooglePlaces__ApiKey,Value="${GOOGLE_PLACES_KEY}"
```

**Note:** For better security, consider using Elastic Beanstalk's integration with Secrets Manager instead of environment variables.

### Step 6: Update Frontend Environment Variables

After the API is deployed, update the frontend environment variables in Amplify:

1. Go to AWS Amplify Console
2. Select your app (`smartscheduler-frontend`)
3. Go to "Environment variables"
4. Update:
   - `NEXT_PUBLIC_API_URL` = Your API URL (from Step 3.7)
   - `NEXT_PUBLIC_SIGNALR_URL` = `${API_URL}/hubs`
   - `NEXT_PUBLIC_COGNITO_USER_POOL_ID` = `us-east-2_oGumIWt36`
   - `NEXT_PUBLIC_COGNITO_CLIENT_ID` = `4rps8b0oldpuan0qs2dnk37odd`
   - `NEXT_PUBLIC_COGNITO_REGION` = `us-east-2`

5. Redeploy the frontend branch

### Step 7: Verify Deployment

#### 7.1 Check API Health

```bash
curl https://${API_URL}/health
```

Should return: `{"status":"Healthy"}`

#### 7.2 Check API Root

```bash
# Requires authentication token
curl -H "Authorization: Bearer YOUR_TOKEN" https://${API_URL}/
```

#### 7.3 Check Frontend

Visit the Amplify app URL (from frontend stack outputs).

## Quick Deployment Script

For convenience, use the deployment scripts:

```bash
# Full deployment
./scripts/deploy-to-aws.sh

# Or step-by-step
./scripts/deploy-infrastructure.sh
./scripts/configure-secrets.sh
./scripts/deploy-api.sh
./scripts/deploy-frontend.sh
```

## Troubleshooting

### CDK Bootstrap Issues

If you get bootstrap errors:

```bash
# Check if already bootstrapped
aws cloudformation describe-stacks --stack-name CDKToolkit

# Bootstrap if needed (check with admin first)
cdk bootstrap aws://ACCOUNT-ID/REGION
```

### Database Connection Issues

1. Verify security group allows inbound PostgreSQL (port 5432) from Elastic Beanstalk
2. Check RDS endpoint and credentials
3. Verify VPC configuration

### Elastic Beanstalk Deployment Issues

1. Check Elastic Beanstalk logs in CloudWatch
2. Verify IAM roles have correct permissions
3. Check health check endpoint (`/health`) is accessible
4. Review environment variables

### Frontend Build Issues

1. Check Amplify build logs
2. Verify environment variables are set
3. Check Next.js build configuration
4. Ensure GitHub repository is connected

## Cost Estimation

Approximate monthly costs:

- **RDS PostgreSQL** (db.t3.micro): ~$15-20/month
- **Elastic Beanstalk** (t3.small, 1-2 instances): ~$15-30/month
- **Amplify Hosting**: ~$0.15/GB build + $0.023/GB served (first 10GB free)
- **Secrets Manager** (3 secrets): ~$1.20/month
- **S3 Storage**: ~$0.023/GB/month
- **CloudWatch Logs**: ~$1-2/month

**Total**: ~$30-50/month (excluding data transfer)

## Cleanup

To destroy all infrastructure:

```bash
cd infrastructure
npm run destroy:frontend
npm run destroy:api
npm run destroy:storage
npm run destroy:secrets
npm run destroy:database  # Database will be retained due to removal policy
```

**Warning**: This will delete all resources. The database has `removalPolicy: RETAIN` so it will remain and must be deleted manually.

## Next Steps

After deployment:

1. Set up CI/CD pipeline for automated deployments
2. Configure CloudWatch alarms and monitoring
3. Set up backup and disaster recovery procedures
4. Configure custom domain and SSL certificate
5. Enable additional security features (MFA, WAF, etc.)

## References

- [Infrastructure CDK Setup](infrastructure-cdk-setup.md)
- [AWS Deployment Setup](aws-deployment-setup.md)
- [Infrastructure README](../infrastructure/README.md)

