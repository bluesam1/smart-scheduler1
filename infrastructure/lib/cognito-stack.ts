import * as cdk from 'aws-cdk-lib';
import * as cognito from 'aws-cdk-lib/aws-cognito';
import { Construct } from 'constructs';

/**
 * Cognito Stack - User Pool and App Client for Authentication
 * 
 * This stack creates or references a Cognito User Pool for SmartScheduler authentication.
 * 
 * Note: If you already have a Cognito User Pool, you can either:
 * 1. Import it using fromUserPoolId() and fromUserPoolClientId()
 * 2. Or create a new one using this stack
 */

export interface CognitoStackProps extends cdk.StackProps {
  /**
   * Existing User Pool ID (optional)
   * If provided, will import the existing User Pool instead of creating a new one
   */
  existingUserPoolId?: string;

  /**
   * Existing App Client ID (optional)
   * Required if existingUserPoolId is provided
   */
  existingAppClientId?: string;

  /**
   * Frontend callback URLs
   * @default ['http://localhost:3000/auth/callback']
   */
  callbackUrls?: string[];

  /**
   * Frontend sign-out URLs
   * @default ['http://localhost:3000/auth/signout']
   */
  signOutUrls?: string[];
}

export class CognitoStack extends cdk.Stack {
  public readonly userPool: cognito.IUserPool;
  public readonly userPoolClient: cognito.IUserPoolClient;
  public readonly userPoolId: string;
  public readonly appClientId: string;

  constructor(scope: Construct, id: string, props?: CognitoStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-Cognito',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    const callbackUrls = props?.callbackUrls || ['http://localhost:3000/auth/callback'];
    const signOutUrls = props?.signOutUrls || ['http://localhost:3000/auth/signout'];

    // If existing User Pool ID is provided, import it
    if (props?.existingUserPoolId) {
      if (!props?.existingAppClientId) {
        throw new Error('existingAppClientId is required when existingUserPoolId is provided');
      }

      this.userPool = cognito.UserPool.fromUserPoolId(
        this,
        'ImportedUserPool',
        props.existingUserPoolId
      );

      // Import the app client
      // Note: fromUserPoolClientId only needs the client ID, not the pool ID
      this.userPoolClient = cognito.UserPoolClient.fromUserPoolClientId(
        this,
        'ImportedUserPoolClient',
        props.existingAppClientId
      );

      this.userPoolId = props.existingUserPoolId;
      this.appClientId = props.existingAppClientId;

      console.log(`Using existing Cognito User Pool: ${props.existingUserPoolId}`);
    } else {
      // Create new User Pool
      this.userPool = new cognito.UserPool(this, 'UserPool', {
        userPoolName: 'smartscheduler-users',
        signInAliases: {
          email: true,
        },
        passwordPolicy: {
          minLength: 8,
          requireLowercase: true,
          requireUppercase: true,
          requireDigits: true,
          requireSymbols: false,
        },
        selfSignUpEnabled: false, // Disable public sign-up for internal use
        autoVerify: {
          email: true,
        },
        standardAttributes: {
          email: {
            required: true,
            mutable: true,
          },
          givenName: {
            required: false,
            mutable: true,
          },
          familyName: {
            required: false,
            mutable: true,
          },
        },
        removalPolicy: cdk.RemovalPolicy.RETAIN, // Important in shared account
      });
      cdk.Tags.of(this.userPool).add('Project', 'SmartScheduler');
      cdk.Tags.of(this.userPool).add('Environment', 'production');

      // Create user groups for role-based access
      const adminGroup = new cognito.CfnUserPoolGroup(this, 'AdminGroup', {
        userPoolId: this.userPool.userPoolId,
        groupName: 'Admin',
        description: 'Administrators with full system access',
        precedence: 1,
      });

      const dispatcherGroup = new cognito.CfnUserPoolGroup(this, 'DispatcherGroup', {
        userPoolId: this.userPool.userPoolId,
        groupName: 'Dispatcher',
        description: 'Dispatchers who can view jobs, request recommendations, and confirm bookings',
        precedence: 2,
      });

      const contractorGroup = new cognito.CfnUserPoolGroup(this, 'ContractorGroup', {
        userPoolId: this.userPool.userPoolId,
        groupName: 'Contractor',
        description: 'Contractors who can view own assignments and schedule',
        precedence: 3,
      });

      // Create App Client (no client secret for SPA)
      this.userPoolClient = new cognito.UserPoolClient(this, 'UserPoolClient', {
        userPool: this.userPool,
        userPoolClientName: 'smartscheduler-web-client',
        generateSecret: false, // No secret for public clients (SPA)
        authFlows: {
          userPassword: false,
          userSrp: false,
          adminUserPassword: false,
          custom: false,
        },
        oAuth: {
          flows: {
            authorizationCodeGrant: true,
            implicitCodeGrant: false,
          },
          scopes: [
            cognito.OAuthScope.OPENID,
            cognito.OAuthScope.EMAIL,
            cognito.OAuthScope.PROFILE,
          ],
          callbackUrls,
          logoutUrls: signOutUrls,
        },
        preventUserExistenceErrors: true,
      });
      cdk.Tags.of(this.userPoolClient).add('Project', 'SmartScheduler');
      cdk.Tags.of(this.userPoolClient).add('Environment', 'production');

      this.userPoolId = this.userPool.userPoolId;
      this.appClientId = this.userPoolClient.userPoolClientId;
    }

    // Outputs
    new cdk.CfnOutput(this, 'UserPoolId', {
      value: this.userPoolId,
      description: 'Cognito User Pool ID',
      exportName: 'SmartScheduler-UserPoolId',
    });

    new cdk.CfnOutput(this, 'AppClientId', {
      value: this.appClientId,
      description: 'Cognito App Client ID',
      exportName: 'SmartScheduler-AppClientId',
    });

    new cdk.CfnOutput(this, 'UserPoolArn', {
      value: this.userPool.userPoolArn,
      description: 'Cognito User Pool ARN',
      exportName: 'SmartScheduler-UserPoolArn',
    });

    // Output the issuer URL for JWT validation
    const issuerUrl = `https://cognito-idp.${this.region}.amazonaws.com/${this.userPoolId}`;
    new cdk.CfnOutput(this, 'IssuerUrl', {
      value: issuerUrl,
      description: 'Cognito JWT Issuer URL',
      exportName: 'SmartScheduler-CognitoIssuerUrl',
    });
  }
}

