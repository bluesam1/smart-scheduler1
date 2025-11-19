# AWS Deployment Setup Guide

This guide outlines what needs to be set up before you can manually deploy SmartScheduler to AWS.

## Prerequisites Checklist

Before deploying, you need the following AWS infrastructure:

- [ ] **AWS Account Access** - Credentials configured (AWS CLI or IAM user)
- [ ] **Elastic Beanstalk** - Application and environment for .NET 8 backend
- [ ] **RDS PostgreSQL** - Database instance with PostGIS extension
- [ ] **Cognito** - User Pool and App Client for authentication
- [ ] **Secrets Manager** - API keys and database credentials
- [ ] **S3 Bucket** (optional) - For static frontend assets
- [ ] **CloudWatch** (automatic) - Logs and metrics

## Current Status

**What's Ready:**
- ✅ CI pipeline builds and tests the application
- ✅ Application code is ready for deployment
- ✅ CDK documentation for Secrets Manager setup

**What's Missing:**
- ❌ Elastic Beanstalk application/environment
- ❌ RDS PostgreSQL database
- ❌ Cognito User Pool
- ❌ Secrets Manager secrets (can be set up via CDK)
- ❌ Application configuration for AWS services

## Setup Options

### Option 1: Manual Setup via AWS Console (Quick Start)

**Pros:** Fast, visual, good for learning  
**Cons:** Not repeatable, harder to version control

**Steps:**
1. Create RDS PostgreSQL instance via AWS Console
2. Create Cognito User Pool via AWS Console
3. Create Elastic Beanstalk application via AWS Console
4. Set up Secrets Manager secrets manually
5. Configure application settings

### Option 2: CDK Infrastructure as Code (Recommended)

**Pros:** Repeatable, version controlled, easier to manage  
**Cons:** Requires CDK setup, more initial setup time

**Steps:**
1. Set up CDK project (see `docs/infrastructure-cdk-setup.md`)
2. Create CDK stacks for all infrastructure
3. Deploy infrastructure via `cdk deploy`
4. Configure application settings

## Required AWS Services

### 1. Elastic Beanstalk (.NET 8 Backend)

**What you need:**
- Application name: `smartscheduler-api`
- Environment name: `smartscheduler-api-prod`
- Platform: `.NET Core on Linux`
- Platform version: `.NET 8 running on 64bit Amazon Linux 2`
- Instance type: `t3.small` (minimum) or `t3.medium` (recommended)
- Load balancer: Application Load Balancer (required for SignalR sticky sessions)

**Configuration:**
- Health check endpoint: `/health`
- Environment variables (from Secrets Manager):
  - `ConnectionStrings__DefaultConnection`
  - `Cognito__Region`
  - `Cognito__UserPoolId`
  - `Cognito__AppClientId`
  - `OpenRouteService__ApiKey`
  - `GooglePlaces__ApiKey`

### 2. RDS PostgreSQL Database

**What you need:**
- DB instance identifier: `smartscheduler-db`
- Engine: PostgreSQL 15+
- Instance class: `db.t3.micro` (dev) or `db.t3.small` (production)
- Storage: 20 GB minimum
- Master username: `smartscheduler_admin`
- Master password: Store in Secrets Manager
- Database name: `smartscheduler`
- VPC: Same VPC as Elastic Beanstalk
- Security group: Allow inbound from Elastic Beanstalk security group
- PostGIS extension: Enable after creation

**PostGIS Setup:**
```sql
-- Connect to database and run:
CREATE EXTENSION IF NOT EXISTS postgis;
```

### 3. Cognito User Pool

**What you need:**
- User Pool name: `smartscheduler-users`
- Sign-in options: Email
- Password policy: Default or custom
- App client: `smartscheduler-web-client`
- App client settings:
  - Allowed OAuth flows: Authorization code grant
  - Allowed OAuth scopes: `openid`, `email`, `profile`
  - Callback URLs: Your frontend URL (e.g., `https://yourdomain.com/auth/callback`)
  - Sign-out URLs: Your frontend URL

### 4. Secrets Manager

**Required secrets:**
- `smartscheduler/database/connection-string` - PostgreSQL connection string
- `smartscheduler/api-keys/openrouteservice` - OpenRouteService API key
- `smartscheduler/api-keys/google-places` - Google Places API key

**Setup via CDK:** See `docs/infrastructure-cdk-setup.md`

### 5. Frontend Hosting (Optional)

**Options:**
- **Vercel** (recommended for Next.js) - Easiest, free tier available
- **AWS Amplify** - AWS-native, integrates with Cognito
- **S3 + CloudFront** - Static hosting, requires build step
- **Elastic Beanstalk** - Can host Next.js, but more complex

## Manual Deployment Steps

Once infrastructure is set up:

### 1. Build Backend

```bash
cd src/SmartScheduler.Api
dotnet publish -c Release -o ./publish
```

### 2. Create Deployment Package

```bash
# Create ZIP file for Elastic Beanstalk
cd publish
zip -r ../smartscheduler-api.zip .
```

### 3. Deploy to Elastic Beanstalk

**Via AWS Console:**
1. Go to Elastic Beanstalk console
2. Select your environment
3. Click "Upload and deploy"
4. Upload `smartscheduler-api.zip`
5. Deploy

**Via AWS CLI:**
```bash
aws elasticbeanstalk create-application-version \
  --application-name smartscheduler-api \
  --version-label v1.0.0 \
  --source-bundle S3Bucket=your-bucket,S3Key=smartscheduler-api.zip

aws elasticbeanstalk update-environment \
  --environment-name smartscheduler-api-prod \
  --version-label v1.0.0
```

### 4. Configure Environment Variables

In Elastic Beanstalk environment configuration, set:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection` (from Secrets Manager)
- `Cognito__Region=us-east-1`
- `Cognito__UserPoolId` (from Cognito)
- `Cognito__AppClientId` (from Cognito)
- `OpenRouteService__ApiKey` (from Secrets Manager)
- `GooglePlaces__ApiKey` (from Secrets Manager)

### 5. Run Database Migrations

```bash
# SSH into Elastic Beanstalk instance or run locally with production connection string
cd src/SmartScheduler.Infrastructure
dotnet ef database update --connection "YOUR_PRODUCTION_CONNECTION_STRING"
```

### 6. Deploy Frontend

**If using Vercel:**
```bash
cd frontend
vercel deploy --prod
```

**If using AWS Amplify:**
1. Connect GitHub repository
2. Configure build settings
3. Set environment variables
4. Deploy

## Quick Start: Minimum Viable Setup

For the fastest path to deployment:

1. **Set up Secrets Manager** (via CDK or manual):
   ```bash
   # See docs/infrastructure-cdk-setup.md
   ```

2. **Create RDS PostgreSQL** (via AWS Console):
   - Use defaults, enable PostGIS after creation
   - Note connection string for Secrets Manager

3. **Create Cognito User Pool** (via AWS Console):
   - Use email sign-in
   - Create app client
   - Note User Pool ID and App Client ID

4. **Create Elastic Beanstalk** (via AWS Console):
   - Use .NET 8 on Linux platform
   - Configure environment variables
   - Deploy application ZIP

5. **Run migrations**:
   ```bash
   dotnet ef database update --connection "YOUR_CONNECTION_STRING"
   ```

## Next Steps

1. **Choose setup method** (Manual vs CDK)
2. **Set up infrastructure** (RDS, Cognito, Elastic Beanstalk)
3. **Configure secrets** (Secrets Manager)
4. **Build and deploy** (Backend + Frontend)
5. **Run migrations** (Database schema)
6. **Test deployment** (Verify health checks, authentication)

## Troubleshooting

### Common Issues

1. **Connection refused to database:**
   - Check security groups allow inbound from Elastic Beanstalk
   - Verify connection string in Secrets Manager
   - Check RDS is in same VPC as Elastic Beanstalk

2. **Cognito authentication fails:**
   - Verify User Pool ID and App Client ID
   - Check callback URLs match frontend URL
   - Verify OAuth scopes are configured

3. **SignalR not working:**
   - Ensure Application Load Balancer (not Classic)
   - Enable sticky sessions on load balancer
   - Check WebSocket support in security groups

4. **Health check fails:**
   - Verify `/health` endpoint is accessible
   - Check application logs in CloudWatch
   - Verify environment variables are set

## Cost Estimate

**Monthly costs (approximate):**
- RDS PostgreSQL (db.t3.small): ~$15-20/month
- Elastic Beanstalk (t3.small): ~$15-20/month
- Secrets Manager (3 secrets): ~$1.20/month
- Cognito: Free tier (up to 50,000 MAU)
- **Total: ~$30-40/month** (plus data transfer)

## Resources

- [Elastic Beanstalk .NET Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.html)
- [RDS PostgreSQL Setup](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_GettingStarted.CreatingConnecting.PostgreSQL.html)
- [Cognito User Pool Setup](https://docs.aws.amazon.com/cognito/latest/developerguide/cognito-user-pools-as-user-directory.html)
- [Secrets Manager CDK Setup](docs/infrastructure-cdk-setup.md)




