import * as cdk from 'aws-cdk-lib';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import { Construct } from 'constructs';

export interface SecretsStackProps extends cdk.StackProps {
  /**
   * OpenRouteService API key (optional - can be set manually later)
   */
  openRouteServiceApiKey?: string;

  /**
   * Google Places API key (optional - can be set manually later)
   */
  googlePlacesApiKey?: string;
}

export class SecretsStack extends cdk.Stack {
  public readonly databaseConnectionStringSecret: secretsmanager.ISecret;
  public readonly openRouteServiceApiKeySecret: secretsmanager.ISecret;
  public readonly googlePlacesApiKeySecret: secretsmanager.ISecret;

  constructor(scope: Construct, id: string, props?: SecretsStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-Secrets',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    // Database connection string secret
    // This will be populated after RDS is created
    // Format: Host={endpoint};Port=5432;Database=smartscheduler;Username={username};Password={password}
    // Import existing secret if it exists (from previous deployment), otherwise CDK will create it
    // Note: If secret already exists, you may need to delete it first or use fromSecretNameV2 to import
    this.databaseConnectionStringSecret = new secretsmanager.Secret(
      this,
      'DatabaseConnectionStringSecret',
      {
        secretName: 'smartscheduler/database/connection-string',
        description: 'SmartScheduler PostgreSQL database connection string',
        generateSecretString: {
          secretStringTemplate: JSON.stringify({
            Host: 'PLACEHOLDER', // Will be updated after RDS deployment
            Port: 5432,
            Database: 'smartscheduler',
            Username: 'PLACEHOLDER', // Will be updated after RDS deployment
          }),
          generateStringKey: 'Password',
          excludeCharacters: '"@/\\',
        },
        removalPolicy: cdk.RemovalPolicy.RETAIN, // Important in shared account
      }
    );
    cdk.Tags.of(this.databaseConnectionStringSecret).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.databaseConnectionStringSecret).add('Environment', 'production');
    cdk.Tags.of(this.databaseConnectionStringSecret).add('ResourceType', 'Secret');

    // OpenRouteService API key secret
    if (props?.openRouteServiceApiKey) {
      this.openRouteServiceApiKeySecret = new secretsmanager.Secret(
        this,
        'OpenRouteServiceApiKeySecret',
        {
          secretName: 'smartscheduler/api-keys/openrouteservice',
          description: 'SmartScheduler OpenRouteService API key for distance/ETA calculations',
          secretStringValue: cdk.SecretValue.unsafePlainText(props.openRouteServiceApiKey),
          removalPolicy: cdk.RemovalPolicy.RETAIN,
        }
      );
    } else {
      this.openRouteServiceApiKeySecret = new secretsmanager.Secret(
        this,
        'OpenRouteServiceApiKeySecret',
        {
          secretName: 'smartscheduler/api-keys/openrouteservice',
          description: 'SmartScheduler OpenRouteService API key for distance/ETA calculations',
          generateSecretString: {
            secretStringTemplate: JSON.stringify({
              ApiKey: 'PLACEHOLDER', // Will be updated manually
            }),
            generateStringKey: 'ApiKey',
          },
          removalPolicy: cdk.RemovalPolicy.RETAIN,
        }
      );
    }
    cdk.Tags.of(this.openRouteServiceApiKeySecret).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.openRouteServiceApiKeySecret).add('Environment', 'production');
    cdk.Tags.of(this.openRouteServiceApiKeySecret).add('ResourceType', 'Secret');

    // Google Places API key secret
    if (props?.googlePlacesApiKey) {
      this.googlePlacesApiKeySecret = new secretsmanager.Secret(
        this,
        'GooglePlacesApiKeySecret',
        {
          secretName: 'smartscheduler/api-keys/google-places',
          description: 'SmartScheduler Google Places API key for address validation',
          secretStringValue: cdk.SecretValue.unsafePlainText(props.googlePlacesApiKey),
          removalPolicy: cdk.RemovalPolicy.RETAIN,
        }
      );
    } else {
      this.googlePlacesApiKeySecret = new secretsmanager.Secret(
        this,
        'GooglePlacesApiKeySecret',
        {
          secretName: 'smartscheduler/api-keys/google-places',
          description: 'SmartScheduler Google Places API key for address validation',
          generateSecretString: {
            secretStringTemplate: JSON.stringify({
              ApiKey: 'PLACEHOLDER', // Will be updated manually
            }),
            generateStringKey: 'ApiKey',
          },
          removalPolicy: cdk.RemovalPolicy.RETAIN,
        }
      );
    }
    cdk.Tags.of(this.googlePlacesApiKeySecret).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.googlePlacesApiKeySecret).add('Environment', 'production');
    cdk.Tags.of(this.googlePlacesApiKeySecret).add('ResourceType', 'Secret');

    // Outputs
    new cdk.CfnOutput(this, 'DatabaseConnectionStringSecretArn', {
      value: this.databaseConnectionStringSecret.secretArn,
      description: 'ARN of the database connection string secret',
      exportName: 'SmartScheduler-DatabaseConnectionStringSecretArn',
    });

    new cdk.CfnOutput(this, 'OpenRouteServiceApiKeySecretArn', {
      value: this.openRouteServiceApiKeySecret.secretArn,
      description: 'ARN of the OpenRouteService API key secret',
      exportName: 'SmartScheduler-OpenRouteServiceApiKeySecretArn',
    });

    new cdk.CfnOutput(this, 'GooglePlacesApiKeySecretArn', {
      value: this.googlePlacesApiKeySecret.secretArn,
      description: 'ARN of the Google Places API key secret',
      exportName: 'SmartScheduler-GooglePlacesApiKeySecretArn',
    });
  }
}

