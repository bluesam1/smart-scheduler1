# CI/CD Pipeline Setup Documentation

This document describes the CI/CD pipeline configuration and required setup.

## Overview

The CI pipeline uses GitHub Actions to automate:
- Continuous Integration (CI): Build, test, and code quality checks

**Note:** Deployment (CD) workflows are currently disabled. CI will run on every push/PR, but deployment must be done manually.

## Workflows

### CI Workflow (`.github/workflows/ci.yml`)

Runs on every push and pull request to `main` or `develop` branches.

**Jobs:**
1. **Backend Build & Test**
   - Restores .NET dependencies
   - Builds the solution
   - Runs unit tests with code coverage
   - Generates OpenAPI specification
   - Uploads test results and OpenAPI spec as artifacts

2. **Frontend Build & Test**
   - Installs Node.js dependencies
   - Runs ESLint
   - Builds Next.js application
   - Uploads build artifacts

3. **API Client Generation**
   - Generates TypeScript API client from OpenAPI spec
   - Commits generated client if changed (on push to main/develop)

4. **Code Quality Checks**
   - Runs .NET code analysis
   - Runs ESLint with detailed output

### CD Workflow (Currently Disabled)

**Deployment workflows have been removed for now.** When ready to enable automated deployment:

1. Create `.github/workflows/cd.yml` with deployment jobs
2. Configure AWS credentials and deployment targets
3. See `docs/infrastructure-cdk-setup.md` for AWS Secrets Manager integration

For now, deployment must be done manually after CI passes.

## Secrets Management

**Recommended Approach: AWS Secrets Manager (via CDK)**

For better security and management, use **AWS Secrets Manager** managed via CDK instead of manual GitHub secrets. See `docs/infrastructure-cdk-setup.md` for the complete CDK-based setup.

**Benefits:**
- Centralized secret management
- Automatic versioning and rotation
- IAM-based access control
- Infrastructure as Code
- Cost-effective (~$1.20/month for 3 secrets)

**With CDK approach, you only need 3 GitHub secrets:**
- `AWS_ACCESS_KEY_ID` - For GitHub Actions to access AWS
- `AWS_SECRET_ACCESS_KEY` - For GitHub Actions to access AWS  
- `AWS_REGION` - AWS region (e.g., `us-east-1`)

All other secrets (API keys, database credentials) are managed in AWS Secrets Manager.

---

**Alternative: Manual GitHub Secrets**

If not using CDK, configure the following secrets in GitHub repository settings (Settings → Secrets and variables → Actions):

### Backend Secrets

- `AWS_ACCESS_KEY_ID` - AWS access key for Elastic Beanstalk deployment
- `AWS_SECRET_ACCESS_KEY` - AWS secret key for Elastic Beanstalk deployment
- `AWS_REGION` - AWS region (e.g., `us-east-1`)
- `DATABASE_CONNECTION_STRING` - PostgreSQL connection string for production
- `COGNITO_USER_POOL_ID` - AWS Cognito User Pool ID
- `COGNITO_APP_CLIENT_ID` - AWS Cognito App Client ID
- `COGNITO_REGION` - AWS Cognito region (e.g., `us-east-1`)

### API Keys

- `OPENROUTESERVICE_API_KEY` - OpenRouteService API key for distance/ETA calculations
- `GOOGLE_PLACES_API_KEY` - Google Places API key for address validation

### Frontend Environment Variables

- `NEXT_PUBLIC_API_URL` - Backend API URL (defaults to `http://localhost:5004` if not set)
- `STAGING_API_URL` - Backend API URL for staging environment
- `PRODUCTION_API_URL` - Backend API URL for production environment

### Deployment URLs (Optional)

- `STAGING_URL` - Staging environment URL (for workflow status badges)
- `PRODUCTION_URL` - Production environment URL (for workflow status badges)

---

**Note:** See `docs/infrastructure-cdk-setup.md` for the recommended CDK-based approach that reduces this to only 3 GitHub secrets.

## Environment Setup

### Staging Environment

The staging environment is automatically deployed after successful CI runs on the `main` branch.

**Configuration:**
- Uses `STAGING_API_URL` for frontend API connection
- Uses staging database connection string
- Uses staging AWS resources (Cognito, etc.)

### Production Environment

The production environment requires manual approval via workflow dispatch.

**Configuration:**
- Uses `PRODUCTION_API_URL` for frontend API connection
- Uses production database connection string
- Uses production AWS resources (Cognito, etc.)

## AWS Elastic Beanstalk Deployment

**Note:** The deployment steps for AWS Elastic Beanstalk are currently commented out in the CD workflow. To enable:

1. Install AWS CLI and EB CLI in the workflow
2. Configure AWS credentials
3. Initialize Elastic Beanstalk application and environment
4. Uncomment and configure the deployment steps in `.github/workflows/cd.yml`

**Example deployment steps:**
```yaml
- name: Configure AWS credentials
  uses: aws-actions/configure-aws-credentials@v4
  with:
    aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
    aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
    aws-region: ${{ secrets.AWS_REGION }}

- name: Deploy to Elastic Beanstalk
  run: |
    eb init -p "64bit Amazon Linux 2 v2.5.0 running .NET Core" --region ${{ secrets.AWS_REGION }}
    eb deploy staging-environment
```

## API Client Generation

The API client is automatically generated during CI using NSwag. The generated client is committed back to the repository if changes are detected.

**Process:**
1. Backend builds and generates OpenAPI spec
2. NSwag generates TypeScript client from spec
3. Generated client is checked for changes
4. If changed, commits with message: `chore: regenerate API client from OpenAPI spec [skip ci]`

**Manual Generation:**
See `README-API-CLIENT-GENERATION.md` for local development instructions.

## Code Quality Checks

### .NET Code Analysis

The pipeline runs .NET build with code analysis enabled. Additional analyzers can be added via NuGet packages.

### ESLint

The frontend uses ESLint for code quality. The pipeline runs:
```bash
npm run lint -- --max-warnings=0
```

**Note:** Currently set to not fail on warnings (`|| true`) to allow gradual adoption. Remove `|| true` once codebase is fully linted.

## Testing

### Backend Tests

- **Framework:** xUnit
- **Location:** `src/SmartScheduler.Api.Tests/`
- **Coverage:** Code coverage collected using `coverlet.collector`
- **Results:** Uploaded as artifacts for review

### Frontend Tests

Frontend tests are not yet configured. When added:
- **Framework:** Jest + React Testing Library
- **Location:** `frontend/__tests__/`
- Add test step to CI workflow

## Artifacts

The pipeline generates the following artifacts:

1. **Backend Test Results** - Test output and code coverage reports
2. **OpenAPI Spec** - Generated OpenAPI specification
3. **Frontend Build** - Next.js build output
4. **Deployment Package** - Complete deployment package (backend + frontend)

Artifacts are retained for 7-30 days depending on type.

## Troubleshooting

### CI Failures

1. **Backend Build Fails:**
   - Check .NET SDK version matches (8.0.x)
   - Verify all NuGet packages are available
   - Check for compilation errors

2. **Frontend Build Fails:**
   - Check Node.js version matches (20.x)
   - Verify all npm dependencies are available
   - Check for TypeScript or build errors

3. **Tests Fail:**
   - Review test output in artifacts
   - Check for environment-specific issues
   - Verify test data setup

### CD Failures

1. **Deployment Fails:**
   - Verify AWS credentials are correct
   - Check Elastic Beanstalk environment exists
   - Review deployment logs

2. **API Client Generation Fails:**
   - Verify NSwag is properly configured
   - Check OpenAPI spec generation
   - Review NSwag logs

## Next Steps

1. **Enable Elastic Beanstalk Deployment:**
   - Set up Elastic Beanstalk application and environments
   - Configure deployment steps in CD workflow
   - Test deployment process

2. **Add Frontend Tests:**
   - Set up Jest and React Testing Library
   - Add test step to CI workflow
   - Configure test coverage reporting

3. **Improve Code Quality:**
   - Add additional .NET analyzers
   - Configure ESLint rules
   - Set up code coverage thresholds

4. **Add Integration Tests:**
   - Set up TestContainers for database tests
   - Add integration test project
   - Configure integration test step in CI

