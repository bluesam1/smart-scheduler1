import * as cdk from 'aws-cdk-lib';
import * as apigatewayv2 from 'aws-cdk-lib/aws-apigatewayv2';
import * as integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import { Construct } from 'constructs';
import { ApiStack } from './api-stack';

export interface ApiGatewayStackProps extends cdk.StackProps {
  /**
   * Reference to the API stack to get the Elastic Beanstalk endpoint
   */
  apiStack: ApiStack;
  
  /**
   * Elastic Beanstalk endpoint URL (HTTP)
   * This should be the HTTP endpoint from Elastic Beanstalk
   * Format: http://awseb-xxx-xxx.us-east-2.elb.amazonaws.com
   */
  backendEndpoint: string;
}

export class ApiGatewayStack extends cdk.Stack {
  public readonly apiUrl: string;
  public readonly httpApi: apigatewayv2.HttpApi;

  constructor(scope: Construct, id: string, props: ApiGatewayStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-ApiGateway',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    // Ensure backend endpoint is HTTP (API Gateway will handle HTTPS)
    const backendUrl = props.backendEndpoint.startsWith('http://')
      ? props.backendEndpoint
      : `http://${props.backendEndpoint}`;

    // Create HTTP API (API Gateway v2) - provides HTTPS automatically
    // Note: When corsPreflight is configured, API Gateway automatically handles OPTIONS requests
    // Do NOT include OPTIONS in route methods - it will be handled by CORS preflight
    this.httpApi = new apigatewayv2.HttpApi(this, 'HttpApi', {
      description: 'SmartScheduler API Gateway - Provides HTTPS for Elastic Beanstalk backend',
      corsPreflight: {
        // Allow Amplify domains and localhost for development
        // Note: API Gateway HTTP API v2 doesn't support wildcard origins with credentials
        // Add additional Amplify app URLs here as needed
        allowOrigins: [
          'https://main.dea48cmln6qtz.amplifyapp.com',
          'http://localhost:3000',
        ],
        allowMethods: [apigatewayv2.CorsHttpMethod.ANY],
        // Explicitly list headers - required when allowCredentials is true
        // API Gateway HTTP API v2 may not accept ['*'] with credentials enabled
        // Include SignalR-specific headers
        allowHeaders: [
          'Content-Type',
          'Authorization',
          'X-Requested-With',
          'Accept',
          'Origin',
          'X-SignalR-User-Agent',
          'Cache-Control',
          'Pragma',
        ],
        allowCredentials: true, // Required for authentication cookies/headers
        maxAge: cdk.Duration.days(1),
      },
    });

    // Go back to using the high-level HttpUrlIntegration construct
    // It should handle path variables and query strings correctly
    const backendIntegration = new integrations.HttpUrlIntegration('BackendIntegration', backendUrl);

    // Add catch-all route that proxies to backend
    // NOTE: Do NOT include OPTIONS here - API Gateway handles it via corsPreflight
    this.httpApi.addRoutes({
      path: '/{proxy+}',
      methods: [apigatewayv2.HttpMethod.ANY],
      integration: backendIntegration,
    });

    // Also add root route
    // NOTE: Do NOT include OPTIONS here - API Gateway handles it via corsPreflight
    this.httpApi.addRoutes({
      path: '/',
      methods: [apigatewayv2.HttpMethod.ANY],
      integration: backendIntegration,
    });

    // Get the API URL (automatically HTTPS)
    this.apiUrl = `https://${this.httpApi.apiId}.execute-api.${this.region}.amazonaws.com`;

    // Outputs
    new cdk.CfnOutput(this, 'ApiGatewayUrl', {
      value: this.apiUrl,
      description: 'API Gateway URL (HTTPS) - Use this in your frontend',
      exportName: 'SmartScheduler-ApiGatewayUrl',
    });

    new cdk.CfnOutput(this, 'ApiGatewayId', {
      value: this.httpApi.apiId,
      description: 'API Gateway ID',
      exportName: 'SmartScheduler-ApiGatewayId',
    });
  }
}

