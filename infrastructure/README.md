# SmartScheduler AWS Infrastructure

This directory contains AWS CDK infrastructure code for deploying SmartScheduler to AWS production environment.

## ⚠️ Shared AWS Account

**This project is deployed to a shared AWS account.** All resources are properly namespaced and tagged:
- Resource names prefixed with `smartscheduler-` or `smartscheduler/`
- All resources tagged with `Project: SmartScheduler`
- IAM permissions scoped to only this project's resources

## Architecture

The infrastructure consists of four main stacks:

1. **Database Stack** (`SmartScheduler-Database`)
   - RDS PostgreSQL 17.6 with PostGIS extension
   - Security groups for database access
   - CloudWatch log groups
   - Uses default VPC (can be customized)

2. **Secrets Stack** (`SmartScheduler-Secrets`)
   - AWS Secrets Manager secrets for:
     - Database connection string
     - OpenRouteService API key
     - Google Places API key

3. **Storage Stack** (`SmartScheduler-Storage`)
   - S3 bucket for deployment artifacts
   - Versioning enabled (keeps deployment history)
   - Lifecycle policies (90-day retention)
   - Encrypted at rest

4. **API Stack** (`SmartScheduler-Api`)
   - Elastic Beanstalk environment for .NET 8 API
   - Application Load Balancer with sticky sessions (for SignalR)
   - Auto-scaling configuration (1-2 instances)
   - IAM roles and policies
   - CloudWatch log groups

5. **Frontend Stack** (`SmartScheduler-Frontend`)
   - AWS Amplify Hosting for Next.js frontend
   - Automatic builds from GitHub
   - Environment variables configuration
   - Custom domain support (optional)

## Prerequisites

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

4. **Bootstrap CDK** in your AWS account (one-time setup)
   ```bash
   # Check with account admin first - may already be done
   cdk bootstrap aws://ACCOUNT-ID/REGION
   # Example: cdk bootstrap aws://123456789012/us-east-1
   ```

## Setup

1. **Install dependencies**
   ```bash
   cd infrastructure
   npm install
   ```

2. **Build the CDK code**
   ```bash
   npm run build
   ```

3. **Synthesize CloudFormation templates** (optional, for review)
   ```bash
   npm run synth
   ```

4. **Review changes** (recommended before deploying)
   ```bash
   npm run diff
   ```

## Deployment

### Deploy All Stacks

Deploy all infrastructure stacks in order:

```bash
npm run deploy
```

This will deploy:
1. Database Stack (RDS PostgreSQL)
2. Secrets Stack (Secrets Manager)
3. Storage Stack (S3 deployment bucket)
4. API Stack (Elastic Beanstalk)
5. Frontend Stack (Amplify Hosting) - Requires Cognito User Pool to be created first

### Deploy Individual Stacks

You can also deploy stacks individually:

```bash
# Deploy database only
npm run deploy:database

# Deploy secrets only
npm run deploy:secrets

# Deploy storage only
npm run deploy:storage

# Deploy API only (requires database, secrets, and storage to be deployed first)
npm run deploy:api

# Deploy frontend only (requires API and Cognito to be deployed first)
npm run deploy:frontend
```

### First-Time Deployment

On first deployment, you'll need to:

1. **Deploy Database Stack** first:
   ```bash
   npm run deploy:database
   ```

2. **Update Database Connection String Secret**:
   After RDS is created, update the connection string secret with the actual RDS endpoint:
   ```bash
   # Get the database endpoint from stack outputs
   aws cloudformation describe-stacks --stack-name SmartScheduler-Database --query "Stacks[0].Outputs"
   
   # Get the database password from the master credentials secret
   aws secretsmanager get-secret-value --secret-id smartscheduler/database/master-credentials --query SecretString --output text
   
   # Update the connection string secret
   aws secretsmanager update-secret \
     --secret-id smartscheduler/database/connection-string \
     --secret-string "Host=<RDS_ENDPOINT>;Port=5432;Database=smartscheduler;Username=dbadmin;Password=<PASSWORD>"
   ```

3. **Set API Keys** (if you have them):
   ```bash
   # OpenRouteService API key
   aws secretsmanager update-secret \
     --secret-id smartscheduler/api-keys/openrouteservice \
     --secret-string '{"ApiKey":"YOUR_API_KEY"}'
   
   # Google Places API key
   aws secretsmanager update-secret \
     --secret-id smartscheduler/api-keys/google-places \
     --secret-string '{"ApiKey":"YOUR_API_KEY"}'
   ```

4. **Deploy Secrets Stack**:
   ```bash
   npm run deploy:secrets
   ```

5. **Create Cognito User Pool** (via AWS Console or CDK):
   - User Pool name: `smartscheduler-users`
   - Sign-in options: Email
   - App client: `smartscheduler-web-client`
   - Note User Pool ID and App Client ID

6. **Deploy API Stack**:
   ```bash
   npm run deploy:api
   ```

7. **Deploy Frontend Stack**:
   ```bash
   # Set Cognito configuration
   export COGNITO_USER_POOL_ID=us-east-1_XXXXXXXXX
   export COGNITO_APP_CLIENT_ID=YOUR_APP_CLIENT_ID
   export GITHUB_REPOSITORY=YOUR_ORG/smart-scheduler1
   export GITHUB_TOKEN=your-github-token  # Optional - can connect manually
   
   npm run deploy:frontend
   ```

## Configuration

### Environment Variables

Set AWS account and region:

```bash
export CDK_DEFAULT_ACCOUNT=123456789012
export CDK_DEFAULT_REGION=us-east-1
```

Or use AWS CLI profile:

```bash
export AWS_PROFILE=your-profile-name
```

### Optional: Set API Keys During Deployment

You can provide API keys as environment variables (they'll be stored in Secrets Manager):

```bash
export OPENROUTESERVICE_API_KEY=your-key
export GOOGLE_PLACES_API_KEY=your-key
npm run deploy
```

### Custom Configuration

You can customize the infrastructure by modifying the stack props in `bin/app.ts`:

- **Database instance type**: Modify `instanceType` in `DatabaseStackProps`
- **Elastic Beanstalk instance type**: Modify `instanceType` in `ApiStackProps`
- **Auto-scaling**: Modify `minInstances` and `maxInstances` in `ApiStackProps`

## Post-Deployment Setup

### 1. Enable PostGIS Extension

After RDS is deployed, connect to the database and enable PostGIS:

```bash
# Get database endpoint
aws rds describe-db-instances --query "DBInstances[?DBInstanceIdentifier=='smartscheduler-database'].Endpoint.Address" --output text

# Connect to database (using psql or pgAdmin)
psql -h <RDS_ENDPOINT> -U dbadmin -d smartscheduler

# Enable PostGIS extension
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;

# Verify
SELECT PostGIS_Version();
```

### 2. Run Database Migrations

After the API is deployed, run EF Core migrations:

```bash
# Get connection string from Secrets Manager
aws secretsmanager get-secret-value --secret-id smartscheduler/database/connection-string --query SecretString --output text

# Run migrations locally (or configure to run automatically on startup)
cd ../src
dotnet ef database update --project SmartScheduler.Infrastructure --startup-project SmartScheduler.Api --connection "YOUR_CONNECTION_STRING"
```

### 3. Deploy Application Code

Deploy your .NET 8 application to Elastic Beanstalk:

```bash
# Build the application
cd src/SmartScheduler.Api
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../SmartScheduler.Api.zip .

# Deploy using AWS CLI
aws elasticbeanstalk create-application-version \
  --application-name smartscheduler-api \
  --version-label v1.0.0 \
  --source-bundle S3Bucket="smartscheduler-deployments-ACCOUNT-REGION",S3Key="SmartScheduler.Api.zip"

aws elasticbeanstalk update-environment \
  --environment-name production \
  --version-label v1.0.0
```

## Monitoring

### CloudWatch Logs

- **RDS Logs**: `/aws/rds/instance/smartscheduler-database/postgresql`
- **Elastic Beanstalk Logs**: `/aws/elasticbeanstalk/smartscheduler-api-production/var/log/eb-engine.log`

### CloudWatch Metrics

Monitor:
- RDS: CPU, memory, connections, storage
- Elastic Beanstalk: Request count, latency, error rate
- Application: Custom metrics via CloudWatch SDK

## Cost Estimation

Approximate monthly costs for production environment:

- **RDS PostgreSQL** (db.t3.micro): ~$15-20/month
- **Elastic Beanstalk** (t3.small, 1-2 instances): ~$15-30/month
- **Amplify Hosting** (Next.js): ~$0.15/GB build + $0.023/GB served (first 10GB free)
- **Secrets Manager** (3 secrets): ~$1.20/month
- **S3 Storage**: ~$0.023/GB/month (minimal for deployment artifacts)
- **CloudWatch Logs**: ~$1-2/month
- **Data Transfer**: Variable

**Total**: ~$30-50/month (excluding data transfer and Amplify usage)

## Cleanup

To destroy all infrastructure:

```bash
npm run destroy
```

**Warning**: This will delete all resources. The database has `removalPolicy: RETAIN` so it will remain and must be deleted manually.

To destroy individual stacks:

```bash
npm run destroy:frontend
npm run destroy:api
npm run destroy:storage
npm run destroy:secrets
npm run destroy:database  # Database will be retained due to removal policy
```

## Troubleshooting

### CDK Bootstrap Issues

If you get bootstrap errors, check with account admin - it may already be done:

```bash
cdk bootstrap aws://ACCOUNT-ID/REGION
```

### Deployment Failures

Check CloudFormation events:

```bash
aws cloudformation describe-stack-events --stack-name SmartScheduler-Database
```

### RDS Connection Issues

1. Verify security group allows inbound PostgreSQL (port 5432) from VPC
2. Check RDS endpoint and credentials
3. Verify VPC configuration

### Elastic Beanstalk Deployment Issues

1. Check Elastic Beanstalk logs in CloudWatch
2. Verify IAM roles have correct permissions
3. Check health check endpoint (`/health`) is accessible

## Next Steps

After infrastructure is deployed:

1. Configure application to use AWS Secrets Manager for connection strings
2. Set up CI/CD pipeline for automated deployments (see `docs/ci-cd-setup.md`)
3. Configure CloudWatch alarms and monitoring
4. Set up backup and disaster recovery procedures
5. Configure custom domain and SSL certificate (if needed)

## References

- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/)
- [Elastic Beanstalk .NET Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.html)
- [RDS PostgreSQL Guide](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html)
- [Secrets Manager Guide](https://docs.aws.amazon.com/secretsmanager/latest/userguide/intro.html)
- [Infrastructure CDK Setup](docs/infrastructure-cdk-setup.md)
- [AWS Deployment Setup](docs/aws-deployment-setup.md)

