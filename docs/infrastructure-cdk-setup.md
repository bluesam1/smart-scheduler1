# Infrastructure Setup with AWS CDK

This document describes how to set up infrastructure and secrets using AWS CDK, which is the recommended approach per the architecture specification.

## Overview

Instead of manually configuring GitHub secrets, we use **AWS Secrets Manager** (managed via CDK) for all application secrets. This provides:

- ✅ **Centralized secret management** - All secrets in one place
- ✅ **Version control** - Secrets are versioned and can be rotated
- ✅ **Better security** - IAM policies control access
- ✅ **Infrastructure as Code** - Secrets defined alongside infrastructure
- ✅ **Automatic rotation** - Can enable automatic secret rotation
- ✅ **Cost-effective** - $0.40/secret/month + $0.05 per 10,000 API calls

## ⚠️ Shared AWS Account Considerations

**This project is deployed to a shared AWS account.** All resources must be properly namespaced and tagged to avoid conflicts and enable cost tracking.

**Key Requirements:**
- ✅ **Resource naming**: All resources prefixed with `smartscheduler-` or `smartscheduler/`
- ✅ **Cost allocation tags**: All resources tagged with `Project: SmartScheduler`
- ✅ **Scoped IAM permissions**: IAM roles/policies scoped to only this project's resources
- ✅ **Stack naming**: Stacks named with project identifier (e.g., `SmartScheduler-Secrets`)
- ✅ **Single environment**: Production only - no environment prefixes needed

## Architecture Alignment

The architecture already specifies:
> **Secrets:** AWS Secrets Manager for API keys (OpenRouteService, Google Places API) and database credentials

This CDK approach implements that specification.

## CDK Stack Structure

```
infrastructure/
├── bin/
│   └── app.ts                      # Main CDK app entry point
├── lib/
│   ├── database-stack.ts          # RDS PostgreSQL database stack
│   ├── secrets-stack.ts            # Secrets Manager stack
│   ├── storage-stack.ts            # S3 deployment bucket stack
│   ├── api-stack.ts                # Elastic Beanstalk API stack
│   └── frontend-stack.ts           # Amplify Hosting frontend stack
├── cdk.json
├── package.json
├── tsconfig.json
└── README.md                       # Infrastructure setup guide
```

**✅ Infrastructure folder created!** See `infrastructure/README.md` for complete setup instructions.

## Setup Steps

### 1. Install Dependencies

The infrastructure folder is already set up. Just install dependencies:

```bash
cd infrastructure
npm install
```

### 2. Verify Dependencies

All required CDK dependencies are already in `package.json`. The `aws-cdk-lib` package includes all AWS services:
- `aws-secretsmanager` (included in aws-cdk-lib)
- `aws-elasticbeanstalk` (included in aws-cdk-lib)
- `aws-rds` (included in aws-cdk-lib)
- `aws-ec2` (included in aws-cdk-lib)
- `aws-iam` (included in aws-cdk-lib)
- `aws-s3` (included in aws-cdk-lib)
- `aws-logs` (included in aws-cdk-lib)

### 3. Review Stacks

All stacks are already created in `infrastructure/lib/`:

- `secrets-stack.ts` - Secrets Manager for API keys and database credentials
- `database-stack.ts` - RDS PostgreSQL with PostGIS
- `storage-stack.ts` - S3 bucket for deployment artifacts
- `api-stack.ts` - Elastic Beanstalk for .NET 8 API
- `frontend-stack.ts` - AWS Amplify Hosting for Next.js frontend

The main app entry point is in `infrastructure/bin/app.ts`.

**Secrets Stack** (`infrastructure/lib/secrets-stack.ts`):

```typescript
import * as cdk from 'aws-cdk-lib';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import { Construct } from 'constructs';

export class SecretsStack extends cdk.Stack {
  public readonly databaseSecret: secretsmanager.Secret;
  public readonly openRouteServiceApiKey: secretsmanager.Secret;
  public readonly googlePlacesApiKey: secretsmanager.Secret;

  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, {
      ...props,
      // Stack name includes project identifier for shared account
      stackName: 'SmartScheduler-Secrets',
      // Add cost allocation tags
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    // Database connection string secret
    this.databaseSecret = new secretsmanager.Secret(this, 'DatabaseSecret', {
      secretName: 'smartscheduler/database/connection-string',
      description: 'SmartScheduler PostgreSQL database connection string',
      generateSecretString: {
        secretStringTemplate: JSON.stringify({
          host: 'localhost',
          port: 5432,
          database: 'smartscheduler',
        }),
        generateStringKey: 'password',
        excludeCharacters: '"@/\\',
      },
      // Important in shared account - don't accidentally delete
      removalPolicy: cdk.RemovalPolicy.RETAIN,
    });
    cdk.Tags.of(this.databaseSecret).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.databaseSecret).add('Environment', 'production');
    cdk.Tags.of(this.databaseSecret).add('ResourceType', 'Secret');

    // OpenRouteService API key
    this.openRouteServiceApiKey = new secretsmanager.Secret(this, 'OpenRouteServiceApiKey', {
      secretName: 'smartscheduler/api-keys/openrouteservice',
      description: 'SmartScheduler OpenRouteService API key for distance/ETA calculations',
      removalPolicy: cdk.RemovalPolicy.RETAIN,
      // You'll need to set this value manually or via CLI after creation
    });
    cdk.Tags.of(this.openRouteServiceApiKey).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.openRouteServiceApiKey).add('Environment', 'production');
    cdk.Tags.of(this.openRouteServiceApiKey).add('ResourceType', 'Secret');

    // Google Places API key
    this.googlePlacesApiKey = new secretsmanager.Secret(this, 'GooglePlacesApiKey', {
      secretName: 'smartscheduler/api-keys/google-places',
      description: 'SmartScheduler Google Places API key for address validation',
      removalPolicy: cdk.RemovalPolicy.RETAIN,
      // You'll need to set this value manually or via CLI after creation
    });
    cdk.Tags.of(this.googlePlacesApiKey).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.googlePlacesApiKey).add('Environment', 'production');
    cdk.Tags.of(this.googlePlacesApiKey).add('ResourceType', 'Secret');
  }
}
```

### 4. Update GitHub Actions to Use AWS Secrets Manager

Update `.github/workflows/cd.yml` to read secrets from AWS:

```yaml
- name: Configure AWS credentials
  uses: aws-actions/configure-aws-credentials@v4
  with:
    aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
    aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
    aws-region: ${{ secrets.AWS_REGION }}

- name: Get secrets from AWS Secrets Manager
  id: get-secrets
  run: |
    DATABASE_CONNECTION=$(aws secretsmanager get-secret-value --secret-id smartscheduler/database/connection-string --query SecretString --output text)
    ORS_API_KEY=$(aws secretsmanager get-secret-value --secret-id smartscheduler/api-keys/openrouteservice --query SecretString --output text)
    GOOGLE_PLACES_KEY=$(aws secretsmanager get-secret-value --secret-id smartscheduler/api-keys/google-places --query SecretString --output text)
    
    echo "::add-mask::$DATABASE_CONNECTION"
    echo "::add-mask::$ORS_API_KEY"
    echo "::add-mask::$GOOGLE_PLACES_KEY"
    
    echo "DATABASE_CONNECTION=$DATABASE_CONNECTION" >> $GITHUB_ENV
    echo "ORS_API_KEY=$ORS_API_KEY" >> $GITHUB_ENV
    echo "GOOGLE_PLACES_KEY=$GOOGLE_PLACES_KEY" >> $GITHUB_ENV
```

### 5. Minimal GitHub Secrets Required

With this approach, you only need **3 GitHub secrets**:

1. `AWS_ACCESS_KEY_ID` - For GitHub Actions to access AWS
2. `AWS_SECRET_ACCESS_KEY` - For GitHub Actions to access AWS
3. `AWS_REGION` - AWS region (e.g., `us-east-1`)

All other secrets are managed in AWS Secrets Manager via CDK.

## Deployment Workflow

### 1. Deploy Infrastructure (First Time)

```bash
cd infrastructure

# Bootstrap CDK (first time only - check with account admin first!)
# Only needed once per account/region combination
cdk bootstrap

# Deploy secrets stack
cdk deploy SmartScheduler-Secrets

# Set secret values (after creation)
aws secretsmanager put-secret-value \
  --secret-id smartscheduler/api-keys/openrouteservice \
  --secret-string "your-api-key-here"

aws secretsmanager put-secret-value \
  --secret-id smartscheduler/api-keys/google-places \
  --secret-string "your-api-key-here"
```

**⚠️ Important for Shared Accounts:**
- Check with account administrator before running `cdk bootstrap` (may already be done)
- Verify you have permissions to create Secrets Manager resources
- Always use `removalPolicy: RETAIN` to prevent accidental deletion

### 2. Application Reads from Secrets Manager

Update your .NET application to read from Secrets Manager:

```csharp
// In Program.cs or Startup.cs
builder.Configuration.AddSecretsManager(configurator: options =>
{
    options.SecretFilter = entry => entry.Name.StartsWith("smartscheduler/");
    options.KeyGenerator = (entry, key) => key
        .Replace("smartscheduler/", "")
        .Replace("/", ":");
});
```

### 3. Update GitHub Actions IAM Role

Create an IAM role for GitHub Actions with **scoped permissions** to only this project's secrets:

```typescript
// In secrets-stack.ts
import * as iam from 'aws-cdk-lib/aws-iam';

// Create IAM role with scoped permissions (important for shared account)
const githubActionsRole = new iam.Role(this, 'GitHubActionsRole', {
  roleName: 'smartscheduler-github-actions', // Explicit name for shared account
  assumedBy: new iam.OpenIdConnectPrincipal(
    new iam.OpenIdConnectProvider(this, 'GitHubOIDC', {
      url: 'https://token.actions.githubusercontent.com',
      clientIds: ['sts.amazonaws.com'],
      // Restrict to specific repository
      conditions: {
        StringEquals: {
          'token.actions.githubusercontent.com:aud': 'sts.amazonaws.com',
        },
        StringLike: {
          'token.actions.githubusercontent.com:sub': 'repo:YOUR_ORG/smart-scheduler1:*',
        },
      },
    })
  ),
});

// Add tags for cost tracking
cdk.Tags.of(githubActionsRole).add('Project', 'SmartScheduler');
cdk.Tags.of(githubActionsRole).add('Environment', 'production');
cdk.Tags.of(githubActionsRole).add('ManagedBy', 'CDK');

// Grant read access ONLY to this project's secrets (scoped by prefix)
this.databaseSecret.grantRead(githubActionsRole);
this.openRouteServiceApiKey.grantRead(githubActionsRole);
this.googlePlacesApiKey.grantRead(githubActionsRole);

// Optional: Add policy to restrict to only smartscheduler/* secrets
githubActionsRole.addToPolicy(new iam.PolicyStatement({
  effect: iam.Effect.ALLOW,
  actions: ['secretsmanager:GetSecretValue', 'secretsmanager:DescribeSecret'],
  resources: [
    `arn:aws:secretsmanager:${this.region}:${this.account}:secret:smartscheduler/*`,
  ],
}));
```

**⚠️ Shared Account Security:**
- Role name includes project identifier to avoid conflicts
- Permissions scoped to only `smartscheduler/*` secrets
- OIDC conditions restrict to specific GitHub repository
- All resources tagged for cost tracking and identification

## Benefits Over Manual GitHub Secrets

| Manual GitHub Secrets | AWS Secrets Manager (CDK) |
|----------------------|---------------------------|
| ❌ Manual setup for each secret | ✅ Defined in code |
| ❌ No versioning | ✅ Automatic versioning |
| ❌ No rotation | ✅ Can enable auto-rotation |
| ❌ Hard to audit | ✅ CloudTrail integration |
| ❌ Per-repository | ✅ Centralized management |
| ❌ No IAM policies | ✅ Fine-grained IAM control |

## Cost

- **AWS Secrets Manager**: $0.40/secret/month + $0.05 per 10,000 API calls
- **For 3 secrets**: ~$1.20/month + minimal API call costs
- **Much cheaper** than managing secrets manually

## Shared Account Best Practices

### Resource Naming Convention
- **Secrets**: `smartscheduler/resource-name` (e.g., `smartscheduler/api-keys/openrouteservice`)
- **Stacks**: `SmartScheduler-{Resource}` (e.g., `SmartScheduler-Secrets`)
- **IAM Roles**: `smartscheduler-{purpose}` (e.g., `smartscheduler-github-actions`)

### Required Tags
All resources must include these tags:
- `Project: SmartScheduler` - For cost allocation
- `Environment: production` - Environment identifier
- `ManagedBy: CDK` - Indicates infrastructure as code
- `ResourceType: {Secret|Role|Stack}` - Resource type for filtering

### IAM Permissions
- **Principle of Least Privilege**: Only grant permissions needed for this project
- **Resource Scoping**: Use ARN patterns to restrict to `smartscheduler/*` resources

### Cost Tracking
- All resources tagged with `Project: SmartScheduler` for cost allocation
- Use AWS Cost Explorer with tag filters to track project costs
- Set up billing alerts for project-specific resources

### Coordination with Account Admin
Before deploying:
1. ✅ Verify CDK bootstrap status (may already be done)
2. ✅ Confirm IAM permissions for Secrets Manager
3. ✅ Check resource limits (secrets per account, etc.)
4. ✅ Agree on naming conventions with other projects
5. ✅ Set up cost allocation tags policy (if not already done)

## Next Steps

1. **Coordinate with account administrator** - Verify permissions and naming conventions
2. **Install dependencies**: `cd infrastructure && npm install`
3. **Build CDK code**: `npm run build`
4. **Review changes**: `npm run diff`
5. **Deploy infrastructure**: See `infrastructure/README.md` for deployment steps
6. **Update application code** to read from Secrets Manager
7. **Set up cost monitoring** with project tags

**See `infrastructure/README.md` for complete deployment instructions.**

## Migration Path

If you've already set up manual GitHub secrets:

1. Deploy CDK secrets stack
2. Copy values from GitHub secrets to AWS Secrets Manager
3. Update workflows to read from AWS
4. Test deployment
5. Remove old GitHub secrets (keep AWS credentials)

