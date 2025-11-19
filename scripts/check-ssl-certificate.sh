#!/bin/bash

# Script to check SSL certificate status and diagnose issues
# Usage: ./scripts/check-ssl-certificate.sh [certificate-arn]

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

REGION="${AWS_REGION:-us-east-2}"

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  SSL Certificate Diagnostic Tool       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# Get certificate ARN from argument, environment variable, or prompt
if [ -n "$1" ]; then
    CERT_ARN="$1"
elif [ -n "$SSL_CERTIFICATE_ARN" ]; then
    CERT_ARN="$SSL_CERTIFICATE_ARN"
else
    echo -e "${YELLOW}Enter the SSL certificate ARN:${NC}"
    read -p "Certificate ARN: " CERT_ARN
fi

if [ -z "$CERT_ARN" ]; then
    echo -e "${RED}Error: Certificate ARN is required${NC}" >&2
    exit 1
fi

echo -e "${YELLOW}Checking certificate: ${CERT_ARN}${NC}"
echo -e "${YELLOW}Region: ${REGION}${NC}"
echo ""

# Check certificate status
echo -e "${BLUE}Certificate Status:${NC}"
CERT_INFO=$(aws acm describe-certificate \
    --certificate-arn "$CERT_ARN" \
    --region "$REGION" \
    --query 'Certificate.[Status,DomainName,SubjectAlternativeNames,Issuer,NotBefore,NotAfter,KeyAlgorithm]' \
    --output table 2>&1)

if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to retrieve certificate information${NC}" >&2
    echo "$CERT_INFO" >&2
    exit 1
fi

echo "$CERT_INFO"
echo ""

# Check certificate status
STATUS=$(aws acm describe-certificate \
    --certificate-arn "$CERT_ARN" \
    --region "$REGION" \
    --query 'Certificate.Status' \
    --output text 2>&1)

if [ "$STATUS" != "ISSUED" ]; then
    echo -e "${RED}⚠️  WARNING: Certificate status is '${STATUS}'${NC}"
    echo -e "${YELLOW}Certificate must be 'ISSUED' to be used with a load balancer.${NC}"
    echo ""
    
    if [ "$STATUS" == "PENDING_VALIDATION" ]; then
        echo -e "${YELLOW}Certificate is pending validation.${NC}"
        echo -e "${YELLOW}Check DNS validation records:${NC}"
        aws acm describe-certificate \
            --certificate-arn "$CERT_ARN" \
            --region "$REGION" \
            --query 'Certificate.DomainValidationOptions[*].[DomainName,ResourceRecord.Name,ResourceRecord.Value]' \
            --output table
        echo ""
        echo -e "${YELLOW}Add these DNS records to your domain's DNS configuration.${NC}"
    fi
else
    echo -e "${GREEN}✓ Certificate status: ISSUED${NC}"
fi

echo ""

# Check if certificate is in the correct region
echo -e "${BLUE}Region Check:${NC}"
CERT_REGION=$(aws acm describe-certificate \
    --certificate-arn "$CERT_ARN" \
    --region "$REGION" \
    --query 'Certificate.[Region]' \
    --output text 2>&1)

if [ "$CERT_REGION" != "$REGION" ]; then
    echo -e "${RED}⚠️  WARNING: Certificate is in region '${CERT_REGION}' but checking in '${REGION}'${NC}"
    echo -e "${YELLOW}Certificate must be in the same region as the load balancer.${NC}"
    echo -e "${YELLOW}Either:${NC}"
    echo -e "  1. Request a new certificate in region ${REGION}"
    echo -e "  2. Or use region ${CERT_REGION} for your infrastructure"
else
    echo -e "${GREEN}✓ Certificate is in the correct region (${REGION})${NC}"
fi

echo ""

# Check certificate expiration
echo -e "${BLUE}Expiration Check:${NC}"
NOT_AFTER=$(aws acm describe-certificate \
    --certificate-arn "$CERT_ARN" \
    --region "$REGION" \
    --query 'Certificate.NotAfter' \
    --output text 2>&1)

if [ -n "$NOT_AFTER" ]; then
    EXPIRY_DATE=$(date -d "$NOT_AFTER" +%s 2>/dev/null || date -j -f "%Y-%m-%dT%H:%M:%S%z" "$NOT_AFTER" +%s 2>/dev/null || echo "")
    CURRENT_DATE=$(date +%s)
    
    if [ -n "$EXPIRY_DATE" ] && [ "$EXPIRY_DATE" -lt "$CURRENT_DATE" ]; then
        echo -e "${RED}⚠️  WARNING: Certificate has EXPIRED${NC}"
        echo -e "${YELLOW}Expired on: ${NOT_AFTER}${NC}"
        echo -e "${YELLOW}You need to request a new certificate.${NC}"
    else
        DAYS_UNTIL_EXPIRY=$(( ($EXPIRY_DATE - $CURRENT_DATE) / 86400 ))
        if [ "$DAYS_UNTIL_EXPIRY" -lt 30 ]; then
            echo -e "${YELLOW}⚠️  Certificate expires in ${DAYS_UNTIL_EXPIRY} days${NC}"
            echo -e "${YELLOW}Expires on: ${NOT_AFTER}${NC}"
        else
            echo -e "${GREEN}✓ Certificate is valid${NC}"
            echo -e "${GREEN}Expires on: ${NOT_AFTER}${NC}"
        fi
    fi
fi

echo ""

# Check if certificate is associated with any load balancers
echo -e "${BLUE}Load Balancer Association:${NC}"
echo -e "${YELLOW}Checking Elastic Beanstalk environment...${NC}"

# Try to get the Elastic Beanstalk environment
EB_ENV=$(aws elasticbeanstalk describe-environments \
    --application-name smartscheduler-api \
    --environment-names production \
    --region "$REGION" \
    --query 'Environments[0].EnvironmentName' \
    --output text 2>/dev/null || echo "")

if [ -n "$EB_ENV" ] && [ "$EB_ENV" != "None" ]; then
    echo -e "${GREEN}Found Elastic Beanstalk environment: ${EB_ENV}${NC}"
    
    # Get load balancer ARN
    LB_ARN=$(aws elasticbeanstalk describe-environment-resources \
        --environment-name "$EB_ENV" \
        --region "$REGION" \
        --query 'EnvironmentResources.LoadBalancers[0].Name' \
        --output text 2>/dev/null || echo "")
    
    if [ -n "$LB_ARN" ] && [ "$LB_ARN" != "None" ]; then
        echo -e "${GREEN}Load balancer found: ${LB_ARN}${NC}"
        
        # Check listeners
        LISTENERS=$(aws elbv2 describe-listeners \
            --load-balancer-arn "$LB_ARN" \
            --region "$REGION" \
            --query 'Listeners[*].[Port,Protocol,DefaultActions[0].TargetGroupArn]' \
            --output table 2>/dev/null || echo "")
        
        if [ -n "$LISTENERS" ]; then
            echo ""
            echo -e "${BLUE}Load Balancer Listeners:${NC}"
            echo "$LISTENERS"
        fi
    fi
else
    echo -e "${YELLOW}Elastic Beanstalk environment not found or not accessible.${NC}"
fi

echo ""
echo -e "${BLUE}Summary:${NC}"
echo -e "Certificate ARN: ${CERT_ARN}"
echo -e "Status: ${STATUS}"
echo -e "Region: ${REGION}"

if [ "$STATUS" != "ISSUED" ]; then
    echo ""
    echo -e "${RED}Action Required:${NC}"
    echo -e "Certificate must be validated before it can be used."
    echo -e "Run: ${BLUE}aws acm describe-certificate --certificate-arn ${CERT_ARN} --region ${REGION}${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}Certificate appears to be valid and ready for use.${NC}"
echo -e "${YELLOW}If you're still experiencing issues:${NC}"
echo -e "1. Ensure the certificate domain matches your backend URL"
echo -e "2. Verify the SSL policy is up to date (should be ELBSecurityPolicy-TLS13-1-2-2021-06)"
echo -e "3. Check browser console for specific error messages"
echo -e "4. Test with: ${BLUE}curl -v https://your-backend-url${NC}"

