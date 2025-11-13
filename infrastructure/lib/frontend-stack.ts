import * as cdk from 'aws-cdk-lib';
import * as amplify from '@aws-cdk/aws-amplify-alpha';
import * as codebuild from 'aws-cdk-lib/aws-codebuild';
import * as iam from 'aws-cdk-lib/aws-iam';
import { Construct } from 'constructs';
import { ApiStack } from './api-stack';

export interface FrontendStackProps extends cdk.StackProps {
  apiStack: ApiStack;
  
  /**
   * GitHub repository for the frontend
   * Format: owner/repo (e.g., "YOUR_ORG/smart-scheduler1")
   */
  repository: string;
  
  /**
   * GitHub branch to deploy
   * @default main
   */
  branch?: string;
  
  /**
   * GitHub OAuth token for connecting to repository
   * Should be stored in AWS Secrets Manager or provided via environment variable
   * Optional - if not provided, repository must be connected manually via Amplify Console
   */
  githubToken?: string;
  
  /**
   * Cognito User Pool ID
   * Required for authentication configuration
   */
  cognitoUserPoolId: string;
  
  /**
   * Cognito App Client ID
   * Required for authentication configuration
   */
  cognitoAppClientId: string;
  
  /**
   * Cognito Region
   * @default us-east-1
   */
  cognitoRegion?: string;
  
  /**
   * Cognito Auth Domain (optional)
   * If using Cognito Hosted UI
   */
  cognitoAuthDomain?: string;
}

export class FrontendStack extends cdk.Stack {
  public readonly app: amplify.App;
  public readonly branch: amplify.Branch;
  public readonly appUrl: string;

  constructor(scope: Construct, id: string, props: FrontendStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-Frontend',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    const branch = props.branch || 'main';
    const cognitoRegion = props.cognitoRegion || 'us-east-1';
    // Note: API URL must be set manually after Elastic Beanstalk environment is created
    // The attrEndpointUrl attribute is not available during stack creation
    // Set NEXT_PUBLIC_API_URL environment variable in Amplify Console after deployment
    // Or use: aws elasticbeanstalk describe-environments --environment-names production --query 'Environments[0].EndpointURL' --output text
    const apiUrl = process.env.API_URL || 'http://PLACEHOLDER-API-URL'; // Will be updated after API stack deployment

    // Create IAM role for Amplify app
    const amplifyRole = new iam.Role(this, 'AmplifyRole', {
      roleName: 'smartscheduler-amplify-role',
      assumedBy: new iam.ServicePrincipal('amplify.amazonaws.com'),
      managedPolicies: [
        iam.ManagedPolicy.fromAwsManagedPolicyName('AdministratorAccess-Amplify'),
      ],
    });
    cdk.Tags.of(amplifyRole).add('Project', 'SmartScheduler');
    cdk.Tags.of(amplifyRole).add('Environment', 'production');

    // Create Amplify app
    this.app = new amplify.App(this, 'FrontendApp', {
      appName: 'smartscheduler-frontend',
      description: 'SmartScheduler Next.js Frontend Application',
      role: amplifyRole,
      sourceCodeProvider: props.githubToken
        ? new amplify.GitHubSourceCodeProvider({
            owner: props.repository.split('/')[0],
            repository: props.repository.split('/')[1],
            oauthToken: cdk.SecretValue.unsafePlainText(props.githubToken),
          })
        : undefined,
      // If no GitHub token, you'll need to connect the repository manually via Amplify Console
      // Build spec for Next.js - Amplify will use this to build the application
      buildSpec: codebuild.BuildSpec.fromObjectToYaml({
        version: '1.0',
        frontend: {
          phases: {
            preBuild: {
              commands: [
                'cd frontend',
                // Install dependencies
                'npm ci',
                // Rebuild native modules to ensure Linux bindings are available
                // Tailwind CSS v4 requires both lightningcss and @tailwindcss/oxide native bindings
                'npm rebuild lightningcss @tailwindcss/oxide || true',
                // Force reinstall to ensure native bindings are downloaded for Linux
                'npm install @tailwindcss/oxide lightningcss --force || true',
              ],
            },
            build: {
              commands: [
                'npm run build',
              ],
            },
          },
          artifacts: {
            baseDirectory: 'frontend/.next',
            files: ['**/*'],
          },
          cache: {
            paths: ['frontend/node_modules/**/*'],
          },
        },
      }),
      environmentVariables: {
        // Next.js environment variables
        NEXT_PUBLIC_API_URL: apiUrl,
        NEXT_PUBLIC_SIGNALR_URL: `${apiUrl}/hubs`,
        
        // Cognito configuration
        NEXT_PUBLIC_COGNITO_USER_POOL_ID: props.cognitoUserPoolId,
        NEXT_PUBLIC_COGNITO_CLIENT_ID: props.cognitoAppClientId,
        NEXT_PUBLIC_COGNITO_REGION: cognitoRegion,
        ...(props.cognitoAuthDomain && {
          NEXT_PUBLIC_COGNITO_AUTH_DOMAIN: props.cognitoAuthDomain,
        }),
        
        // Next.js build configuration
        NODE_ENV: 'production',
        NODE_OPTIONS: '--max-old-space-size=4096',
      },
      customRules: [
        // Next.js App Router rewrites
        {
          source: '/api/<*>',
          target: `${apiUrl}/api/<*>`,
          status: amplify.RedirectStatus.REWRITE,
        },
        // Next.js catch-all for client-side routing
        {
          source: '/<*>',
          target: '/index.html',
          status: amplify.RedirectStatus.REWRITE,
        },
      ],
    });
    cdk.Tags.of(this.app).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.app).add('Environment', 'production');

    // Create branch for deployment
    this.branch = this.app.addBranch(branch, {
      branchName: branch,
      autoBuild: true,
      environmentVariables: {
        NEXT_PUBLIC_API_URL: apiUrl,
        NEXT_PUBLIC_SIGNALR_URL: `${apiUrl}/hubs`,
        NEXT_PUBLIC_COGNITO_USER_POOL_ID: props.cognitoUserPoolId,
        NEXT_PUBLIC_COGNITO_CLIENT_ID: props.cognitoAppClientId,
        NEXT_PUBLIC_COGNITO_REGION: cognitoRegion,
        ...(props.cognitoAuthDomain && {
          NEXT_PUBLIC_COGNITO_AUTH_DOMAIN: props.cognitoAuthDomain,
        }),
      },
    });

    // Add custom domain (optional - can be configured later)
    // this.app.addDomain('example.com', {
    //   subDomains: [
    //     { branch: this.branch },
    //   ],
    // });

    // Outputs
    this.appUrl = `https://${branch}.${this.app.defaultDomain}`;

    new cdk.CfnOutput(this, 'AppId', {
      value: this.app.appId,
      description: 'Amplify App ID',
      exportName: 'SmartScheduler-FrontendAppId',
    });

    new cdk.CfnOutput(this, 'AppUrl', {
      value: this.appUrl,
      description: 'Amplify App URL',
      exportName: 'SmartScheduler-FrontendAppUrl',
    });

    new cdk.CfnOutput(this, 'DefaultDomain', {
      value: this.app.defaultDomain,
      description: 'Amplify App Default Domain',
      exportName: 'SmartScheduler-FrontendDefaultDomain',
    });

    new cdk.CfnOutput(this, 'BranchName', {
      value: this.branch.branchName,
      description: 'Amplify Branch Name',
      exportName: 'SmartScheduler-FrontendBranchName',
    });
  }
}

