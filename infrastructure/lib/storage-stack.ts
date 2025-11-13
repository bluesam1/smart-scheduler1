import * as cdk from 'aws-cdk-lib';
import * as s3 from 'aws-cdk-lib/aws-s3';
import { Construct } from 'constructs';

/**
 * Storage Stack - S3 Bucket for Application Deployments
 * 
 * This stack creates an S3 bucket for storing application deployment artifacts:
 * - .NET API deployment packages (for Elastic Beanstalk)
 * - Frontend build artifacts (if needed)
 * - Other deployment-related files
 */

export interface StorageStackProps extends cdk.StackProps {
  /**
   * Enable versioning for deployment artifacts
   * @default true
   */
  versioned?: boolean;

  /**
   * Number of days to retain old versions
   * @default 90
   */
  lifecycleDays?: number;
}

export class StorageStack extends cdk.Stack {
  public readonly deploymentBucket: s3.Bucket;
  public readonly bucketName: string;

  constructor(scope: Construct, id: string, props?: StorageStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-Storage',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    const versioned = props?.versioned ?? true;
    const lifecycleDays = props?.lifecycleDays || 90;

    // Create S3 bucket for deployment artifacts
    // Bucket name includes account and region for uniqueness in shared account
    this.deploymentBucket = new s3.Bucket(this, 'DeploymentBucket', {
      bucketName: `smartscheduler-deployments-${this.account}-${this.region}`,
      
      // Versioning for deployment history
      versioned,

      // Encryption at rest
      encryption: s3.BucketEncryption.S3_MANAGED,

      // Block public access (deployment artifacts should be private)
      blockPublicAccess: s3.BlockPublicAccess.BLOCK_ALL,

      // Lifecycle policy to clean up old versions
      lifecycleRules: [
        {
          id: 'DeleteOldVersions',
          enabled: true,
          noncurrentVersionExpiration: cdk.Duration.days(lifecycleDays),
        },
        {
          id: 'AbortIncompleteMultipartUploads',
          enabled: true,
          abortIncompleteMultipartUploadAfter: cdk.Duration.days(7),
        },
      ],

      // Retain bucket in shared account (safer)
      removalPolicy: cdk.RemovalPolicy.RETAIN,
      autoDeleteObjects: false,
    });

    cdk.Tags.of(this.deploymentBucket).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.deploymentBucket).add('Environment', 'production');
    cdk.Tags.of(this.deploymentBucket).add('ResourceType', 'Storage');

    this.bucketName = this.deploymentBucket.bucketName;

    // CloudFormation Outputs
    new cdk.CfnOutput(this, 'DeploymentBucketName', {
      value: this.deploymentBucket.bucketName,
      description: 'S3 bucket for deployment artifacts',
      exportName: 'SmartScheduler-DeploymentBucket',
    });

    new cdk.CfnOutput(this, 'DeploymentBucketArn', {
      value: this.deploymentBucket.bucketArn,
      description: 'ARN of the deployment bucket',
      exportName: 'SmartScheduler-DeploymentBucketArn',
    });
  }
}

