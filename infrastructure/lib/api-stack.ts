import * as cdk from 'aws-cdk-lib';
import * as elasticbeanstalk from 'aws-cdk-lib/aws-elasticbeanstalk';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as logs from 'aws-cdk-lib/aws-logs';
import { Construct } from 'constructs';
import { DatabaseStack } from './database-stack';
import { SecretsStack } from './secrets-stack';

export interface ApiStackProps extends cdk.StackProps {
  database: DatabaseStack;
  secrets: SecretsStack;
  
  /**
   * Application name for Elastic Beanstalk
   * @default smartscheduler-api
   */
  applicationName?: string;
  
  /**
   * Environment name for Elastic Beanstalk
   * @default production
   */
  environmentName?: string;
  
  /**
   * Instance type for Elastic Beanstalk
   * @default t3.small
   */
  instanceType?: string;
  
  /**
   * Minimum number of instances
   * @default 1
   */
  minInstances?: number;
  
  /**
   * Maximum number of instances
   * @default 2
   */
  maxInstances?: number;
}

export class ApiStack extends cdk.Stack {
  public readonly application: elasticbeanstalk.CfnApplication;
  public readonly ebEnvironment: elasticbeanstalk.CfnEnvironment;
  // Note: applicationUrl removed - endpoint URL is only available after environment creation
  // Use the EndpointUrl output after stack deployment to get the full URL

  constructor(scope: Construct, id: string, props: ApiStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-Api',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    const applicationName = props.applicationName || 'smartscheduler-api';
    const environmentName = props.environmentName || 'production';
    const instanceType = props.instanceType || 't3.small';
    const minInstances = props.minInstances || 1;
    const maxInstances = props.maxInstances || 2;

    // Create IAM role for Elastic Beanstalk EC2 instances
    const ebInstanceRole = new iam.Role(this, 'ElasticBeanstalkInstanceRole', {
      roleName: 'smartscheduler-eb-instance-role',
      assumedBy: new iam.ServicePrincipal('ec2.amazonaws.com'),
      managedPolicies: [
        iam.ManagedPolicy.fromAwsManagedPolicyName('AWSElasticBeanstalkWebTier'),
        iam.ManagedPolicy.fromAwsManagedPolicyName('AWSElasticBeanstalkWorkerTier'),
        iam.ManagedPolicy.fromAwsManagedPolicyName('AWSElasticBeanstalkMulticontainerDocker'),
      ],
    });
    cdk.Tags.of(ebInstanceRole).add('Project', 'SmartScheduler');
    cdk.Tags.of(ebInstanceRole).add('Environment', 'production');

    // Grant permissions to access Secrets Manager
    props.secrets.databaseConnectionStringSecret.grantRead(ebInstanceRole);
    props.secrets.openRouteServiceApiKeySecret.grantRead(ebInstanceRole);
    props.secrets.googlePlacesApiKeySecret.grantRead(ebInstanceRole);
    
    // Grant permission to read database master credentials secret (for connection string)
    props.database.database.secret!.grantRead(ebInstanceRole);

    // Grant permissions to access RDS
    props.database.database.grantConnect(ebInstanceRole);

    // Create instance profile for Elastic Beanstalk
    const instanceProfile = new iam.CfnInstanceProfile(this, 'ElasticBeanstalkInstanceProfile', {
      instanceProfileName: `${applicationName}-instance-profile`,
      roles: [ebInstanceRole.roleName],
    });

    // Create Elastic Beanstalk application
    this.application = new elasticbeanstalk.CfnApplication(this, 'Application', {
      applicationName,
      description: 'SmartScheduler .NET 8 API Application',
    });

    // Create CloudWatch Log Group for Elastic Beanstalk
    const logGroup = new logs.LogGroup(this, 'ElasticBeanstalkLogGroup', {
      logGroupName: `/aws/elasticbeanstalk/${applicationName}-${environmentName}/var/log/eb-engine.log`,
      retention: logs.RetentionDays.ONE_MONTH,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
    });

    // Elastic Beanstalk environment configuration
    const optionSettings: elasticbeanstalk.CfnEnvironment.OptionSettingProperty[] = [
      // Environment configuration
      {
        namespace: 'aws:elasticbeanstalk:environment',
        optionName: 'EnvironmentType',
        value: 'LoadBalanced',
      },
      {
        namespace: 'aws:elasticbeanstalk:environment',
        optionName: 'LoadBalancerType',
        value: 'application',
      },
      {
        namespace: 'aws:elasticbeanstalk:environment',
        optionName: 'ServiceRole',
        value: 'aws-elasticbeanstalk-service-role',
      },
      
      // Auto Scaling configuration
      {
        namespace: 'aws:autoscaling:asg',
        optionName: 'MinSize',
        value: minInstances.toString(),
      },
      {
        namespace: 'aws:autoscaling:asg',
        optionName: 'MaxSize',
        value: maxInstances.toString(),
      },
      {
        namespace: 'aws:autoscaling:launchconfiguration',
        optionName: 'InstanceType',
        value: instanceType,
      },
      {
        namespace: 'aws:autoscaling:launchconfiguration',
        optionName: 'IamInstanceProfile',
        value: instanceProfile.instanceProfileName!,
      },
      
      // Load Balancer configuration (for SignalR sticky sessions)
      {
        namespace: 'aws:elasticbeanstalk:environment:process:default',
        optionName: 'StickinessEnabled',
        value: 'true',
      },
      {
        namespace: 'aws:elasticbeanstalk:environment:process:default',
        optionName: 'StickinessLBCookieDuration',
        value: '86400', // 24 hours
      },
      {
        namespace: 'aws:elasticbeanstalk:environment:process:default',
        optionName: 'HealthCheckPath',
        value: '/health',
      },
      {
        namespace: 'aws:elasticbeanstalk:environment:process:default',
        optionName: 'HealthCheckInterval',
        value: '30',
      },
      {
        namespace: 'aws:elasticbeanstalk:environment:process:default',
        optionName: 'HealthCheckTimeout',
        value: '5',
      },
      {
        namespace: 'aws:elasticbeanstalk:environment:process:default',
        optionName: 'HealthyThresholdCount',
        value: '2',
      },
      {
        namespace: 'aws:elasticbeanstalk:environment:process:default',
        optionName: 'UnhealthyThresholdCount',
        value: '5',
      },
      
      // CloudWatch Logs configuration
      {
        namespace: 'aws:elasticbeanstalk:cloudwatch:logs',
        optionName: 'StreamLogs',
        value: 'true',
      },
      {
        namespace: 'aws:elasticbeanstalk:cloudwatch:logs',
        optionName: 'DeleteOnTerminate',
        value: 'true',
      },
      {
        namespace: 'aws:elasticbeanstalk:cloudwatch:logs',
        optionName: 'RetentionInDays',
        value: '30',
      },
      
      // VPC configuration (use same VPC as RDS)
      {
        namespace: 'aws:ec2:vpc',
        optionName: 'VPCId',
        value: props.database.vpc.vpcId,
      },
      {
        namespace: 'aws:ec2:vpc',
        optionName: 'Subnets',
        value: props.database.vpc.publicSubnets.map(s => s.subnetId).join(','),
      },
      {
        namespace: 'aws:ec2:vpc',
        optionName: 'ELBSubnets',
        value: props.database.vpc.publicSubnets.map(s => s.subnetId).join(','),
      },
      {
        namespace: 'aws:ec2:vpc',
        optionName: 'ELBScheme',
        value: 'internet-facing',
      },
      
      // Security group configuration
      {
        namespace: 'aws:autoscaling:launchconfiguration',
        optionName: 'SecurityGroups',
        value: props.database.securityGroup.securityGroupId,
      },
      
      // Environment variables (for application configuration)
      {
        namespace: 'aws:elasticbeanstalk:application:environment',
        optionName: 'ASPNETCORE_ENVIRONMENT',
        value: 'Production',
      },
      {
        namespace: 'aws:elasticbeanstalk:application:environment',
        optionName: 'DB_HOST',
        value: props.database.database.dbInstanceEndpointAddress,
      },
      {
        namespace: 'aws:elasticbeanstalk:application:environment',
        optionName: 'DB_PORT',
        value: props.database.database.dbInstanceEndpointPort,
      },
      {
        namespace: 'aws:elasticbeanstalk:application:environment',
        optionName: 'DB_NAME',
        value: 'smartscheduler',
      },
      {
        namespace: 'aws:elasticbeanstalk:application:environment',
        optionName: 'DB_SECRET_ARN',
        value: props.database.database.secret!.secretArn,
      },
    ];

    // Create Elastic Beanstalk environment
    this.ebEnvironment = new elasticbeanstalk.CfnEnvironment(this, 'Environment', {
      applicationName: this.application.applicationName!,
      environmentName,
      solutionStackName: '64bit Amazon Linux 2023 v3.5.8 running .NET 8',
      optionSettings,
      tags: [
        {
          key: 'Environment',
          value: 'Production',
        },
        {
          key: 'Project',
          value: 'SmartScheduler',
        },
      ],
    });

    this.ebEnvironment.addDependency(this.application);
    this.ebEnvironment.addDependency(instanceProfile);

    // Outputs
    new cdk.CfnOutput(this, 'ApplicationName', {
      value: this.application.applicationName!,
      description: 'Elastic Beanstalk application name',
      exportName: 'SmartScheduler-ApplicationName',
    });

    new cdk.CfnOutput(this, 'EnvironmentName', {
      value: this.ebEnvironment.environmentName!,
      description: 'Elastic Beanstalk environment name',
      exportName: 'SmartScheduler-EnvironmentName',
    });

    // Note: Endpoint URL outputs removed
    // The attrEndpointUrl attribute is not available during stack creation
    // To get the endpoint URL after deployment, use:
    // aws elasticbeanstalk describe-environments --environment-names production --query 'Environments[0].EndpointURL' --output text
    // Or retrieve it from the AWS Console: Elastic Beanstalk > Environments > production > Configuration > Load balancer
  }
}

