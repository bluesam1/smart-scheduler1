import * as cdk from 'aws-cdk-lib';
import * as rds from 'aws-cdk-lib/aws-rds';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as logs from 'aws-cdk-lib/aws-logs';
import { Construct } from 'constructs';

export interface DatabaseStackProps extends cdk.StackProps {
  /**
   * Database instance type
   * @default t3.micro (for MVP, can be upgraded later)
   */
  instanceType?: ec2.InstanceType;
  
  /**
   * Database name
   * @default smartscheduler
   */
  databaseName?: string;
  
  /**
   * Master username
   * @default dbadmin (admin is reserved in PostgreSQL)
   */
  masterUsername?: string;
  
  /**
   * Allocated storage in GB
   * @default 20
   */
  allocatedStorage?: number;
  
  /**
   * Enable automated backups
   * @default true
   */
  enableBackups?: boolean;
  
  /**
   * Backup retention days
   * @default 7
   */
  backupRetentionDays?: number;
}

export class DatabaseStack extends cdk.Stack {
  public readonly vpc: ec2.IVpc;
  public readonly database: rds.DatabaseInstance;
  public readonly databaseEndpoint: string;
  public readonly databasePort: number;
  public readonly databaseName: string;
  public readonly securityGroup: ec2.SecurityGroup;

  constructor(scope: Construct, id: string, props?: DatabaseStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-Database',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    const databaseName = props?.databaseName || 'smartscheduler';
    // Note: 'admin' is reserved in PostgreSQL, so we use 'dbadmin' as default
    const masterUsername = props?.masterUsername || 'dbadmin';
    const allocatedStorage = props?.allocatedStorage || 20;
    const enableBackups = props?.enableBackups !== false;
    const backupRetentionDays = props?.backupRetentionDays || 7;

    // Create VPC for RDS (or use default VPC)
    // For MVP, we'll use the default VPC to simplify setup
    // In production, you may want to create a dedicated VPC
    this.vpc = ec2.Vpc.fromLookup(this, 'DefaultVPC', {
      isDefault: true,
    });

    // Create security group for RDS
    this.securityGroup = new ec2.SecurityGroup(this, 'DatabaseSecurityGroup', {
      vpc: this.vpc,
      description: 'Security group for SmartScheduler RDS PostgreSQL database',
      allowAllOutbound: true,
    });
    cdk.Tags.of(this.securityGroup).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.securityGroup).add('Environment', 'production');

    // ⚠️ WARNING: Allow inbound PostgreSQL traffic from anywhere (for development only)
    // TODO: Restrict this to specific IPs or VPC for production
    this.securityGroup.addIngressRule(
      ec2.Peer.anyIpv4(),
      ec2.Port.tcp(5432),
      'Allow PostgreSQL access from anywhere (DEVELOPMENT ONLY)'
    );

    // Create RDS PostgreSQL instance with PostGIS
    // Using PostgreSQL 17.6 (latest available on AWS)
    this.database = new rds.DatabaseInstance(this, 'Database', {
      engine: rds.DatabaseInstanceEngine.postgres({
        version: rds.PostgresEngineVersion.VER_17_6,
      }),
      instanceType: props?.instanceType || ec2.InstanceType.of(ec2.InstanceClass.T3, ec2.InstanceSize.MICRO),
      vpc: this.vpc,
      vpcSubnets: {
        // Use public subnets when publiclyAccessible is true (required for internet access)
        // Use private subnets only when publiclyAccessible is false
        subnetType: ec2.SubnetType.PUBLIC, // Public subnets required for publicly accessible RDS
      },
      securityGroups: [this.securityGroup],
      publiclyAccessible: true, // WARNING: For development only, set to false for production
      databaseName,
      credentials: rds.Credentials.fromGeneratedSecret(masterUsername, {
        secretName: 'smartscheduler/database/master-credentials',
        excludeCharacters: '"@/\\',
      }),
      allocatedStorage,
      storageType: rds.StorageType.GP3,
      storageEncrypted: true,
      multiAz: false, // Single AZ for MVP (can enable Multi-AZ later)
      autoMinorVersionUpgrade: true,
      backupRetention: enableBackups ? cdk.Duration.days(backupRetentionDays) : undefined,
      deleteAutomatedBackups: !enableBackups,
      deletionProtection: false, // Set to true in production
      removalPolicy: cdk.RemovalPolicy.RETAIN, // Retain database on stack deletion (important in shared account)
      enablePerformanceInsights: false, // Enable for production if needed
      parameterGroup: new rds.ParameterGroup(this, 'DatabaseParameterGroup', {
        engine: rds.DatabaseInstanceEngine.postgres({
          version: rds.PostgresEngineVersion.VER_17_6,
        }),
        description: 'Parameter group for SmartScheduler PostgreSQL with PostGIS',
      }),
    });
    cdk.Tags.of(this.database).add('Project', 'SmartScheduler');
    cdk.Tags.of(this.database).add('Environment', 'production');

    // CloudWatch Logs for RDS
    const logGroup = new logs.LogGroup(this, 'DatabaseLogGroup', {
      logGroupName: `/aws/rds/instance/${this.database.instanceIdentifier}/postgresql`,
      retention: logs.RetentionDays.ONE_MONTH,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
    });

    // Outputs
    this.databaseEndpoint = this.database.instanceEndpoint.hostname;
    this.databasePort = 5432;
    this.databaseName = databaseName;

    new cdk.CfnOutput(this, 'DatabaseEndpoint', {
      value: this.databaseEndpoint,
      description: 'RDS PostgreSQL endpoint',
      exportName: 'SmartScheduler-DatabaseEndpoint',
    });

    new cdk.CfnOutput(this, 'DatabasePort', {
      value: this.databasePort.toString(),
      description: 'RDS PostgreSQL port',
      exportName: 'SmartScheduler-DatabasePort',
    });

    new cdk.CfnOutput(this, 'DatabaseName', {
      value: this.databaseName,
      description: 'RDS PostgreSQL database name',
      exportName: 'SmartScheduler-DatabaseName',
    });

    new cdk.CfnOutput(this, 'DatabaseSecretArn', {
      value: this.database.secret?.secretArn || 'N/A',
      description: 'ARN of the secret containing database credentials',
      exportName: 'SmartScheduler-DatabaseSecretArn',
    });

    new cdk.CfnOutput(this, 'SecurityGroupId', {
      value: this.securityGroup.securityGroupId,
      description: 'Security group ID for database access',
      exportName: 'SmartScheduler-DatabaseSecurityGroupId',
    });

    new cdk.CfnOutput(this, 'VpcId', {
      value: this.vpc.vpcId,
      description: 'VPC ID for database',
      exportName: 'SmartScheduler-VpcId',
    });
  }
}

