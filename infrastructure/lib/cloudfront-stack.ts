import * as cdk from 'aws-cdk-lib';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as origins from 'aws-cdk-lib/aws-cloudfront-origins';
import { Construct } from 'constructs';

export interface CloudFrontStackProps extends cdk.StackProps {
  /**
   * Elastic Beanstalk endpoint URL (HTTP)
   * Format: production.eba-sw37cpef.us-east-2.elasticbeanstalk.com
   */
  backendEndpoint: string;
}

export class CloudFrontStack extends cdk.Stack {
  public readonly distribution: cloudfront.Distribution;
  public readonly cloudfrontUrl: string;

  constructor(scope: Construct, id: string, props: CloudFrontStackProps) {
    super(scope, id, {
      ...props,
      stackName: 'SmartScheduler-CloudFront',
      tags: {
        Project: 'SmartScheduler',
        Environment: 'production',
        ManagedBy: 'CDK',
        ...props?.tags,
      },
    });

    // Strip http:// prefix if present
    const backendHost = props.backendEndpoint.replace(/^https?:\/\//, '');

    // Create CORS response headers policy
    const corsResponseHeadersPolicy = new cloudfront.ResponseHeadersPolicy(this, 'CorsResponseHeadersPolicy', {
      comment: 'CORS policy for SmartScheduler API',
      corsBehavior: {
        accessControlAllowOrigins: [
          'https://main.dea48cmln6qtz.amplifyapp.com',
          'http://localhost:3000',
        ],
        accessControlAllowMethods: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'OPTIONS', 'HEAD'],
        accessControlAllowHeaders: [
          'Content-Type',
          'Authorization',
          'X-Requested-With',
          'Accept',
          'Origin',
          'X-SignalR-User-Agent',
          'Cache-Control',
          'Pragma',
        ],
        accessControlAllowCredentials: true,
        accessControlMaxAge: cdk.Duration.days(1),
        originOverride: false, // Don't override if backend sends CORS headers
      },
    });

    // Create CloudFront distribution with ELB as origin
    this.distribution = new cloudfront.Distribution(this, 'Distribution', {
      comment: 'SmartScheduler API - HTTPS proxy for Elastic Beanstalk',
      defaultBehavior: {
        origin: new origins.HttpOrigin(backendHost, {
          protocolPolicy: cloudfront.OriginProtocolPolicy.HTTP_ONLY,
          httpPort: 80,
        }),
        // Allow all HTTP methods (GET, POST, PUT, DELETE, PATCH, OPTIONS, HEAD)
        allowedMethods: cloudfront.AllowedMethods.ALLOW_ALL,
        // Cache policy: forward all headers, query strings, and cookies to backend
        cachePolicy: cloudfront.CachePolicy.CACHING_DISABLED,
        // Origin request policy: forward all query strings, headers, and cookies
        originRequestPolicy: cloudfront.OriginRequestPolicy.ALL_VIEWER,
        // Response headers policy: add CORS headers
        responseHeadersPolicy: corsResponseHeadersPolicy,
        // Viewer protocol policy: redirect HTTP to HTTPS
        viewerProtocolPolicy: cloudfront.ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
        // Compress responses
        compress: true,
      },
      // Enable HTTP/2 and HTTP/3
      httpVersion: cloudfront.HttpVersion.HTTP2_AND_3,
      // Price class: use only North America and Europe (cheapest option)
      priceClass: cloudfront.PriceClass.PRICE_CLASS_100,
    });

    this.cloudfrontUrl = `https://${this.distribution.distributionDomainName}`;

    // Outputs
    new cdk.CfnOutput(this, 'CloudFrontUrl', {
      value: this.cloudfrontUrl,
      description: 'CloudFront URL (HTTPS) - Use this in your frontend',
      exportName: 'SmartScheduler-CloudFrontUrl',
    });

    new cdk.CfnOutput(this, 'CloudFrontDistributionId', {
      value: this.distribution.distributionId,
      description: 'CloudFront Distribution ID',
      exportName: 'SmartScheduler-CloudFrontDistributionId',
    });
  }
}

