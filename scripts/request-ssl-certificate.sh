#!/bin/bash

# Script to request an SSL certificate from AWS Certificate Manager (ACM) for the API load balancer
# Usage: ./scripts/request-ssl-certificate.sh [domain-name]

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
echo -e "${BLUE}║  Request SSL Certificate from ACM      ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# Get domain name from argument or prompt
if [ -n "$1" ]; then
    DOMAIN_NAME="$1"
else
    echo -e "${YELLOW}Enter the domain name for the SSL certificate:${NC}"
    echo -e "${YELLOW}(e.g., api.smartscheduler.com or *.smartscheduler.com for wildcard)${NC}"
    read -p "Domain name: " DOMAIN_NAME
fi

if [ -z "$DOMAIN_NAME" ]; then
    echo -e "${RED}Error: Domain name is required${NC}" >&2
    exit 1
fi

echo -e "${YELLOW}Requesting certificate for: ${DOMAIN_NAME}${NC}"
echo -e "${YELLOW}Region: ${REGION}${NC}"
echo ""

# Request certificate
echo -e "${YELLOW}Requesting certificate from ACM...${NC}"
CERT_ARN=$(aws acm request-certificate \
    --domain-name "$DOMAIN_NAME" \
    --validation-method DNS \
    --region "$REGION" \
    --query 'CertificateArn' \
    --output text 2>&1)

if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to request certificate${NC}" >&2
    echo "$CERT_ARN" >&2
    exit 1
fi

echo -e "${GREEN}✓ Certificate requested successfully!${NC}"
echo ""
echo -e "${BLUE}Certificate ARN:${NC}"
echo -e "${GREEN}${CERT_ARN}${NC}"
echo ""

# Get validation details
echo -e "${YELLOW}Fetching validation details...${NC}"
sleep 3  # Wait a moment for certificate to be created

VALIDATION_RECORDS=$(aws acm describe-certificate \
    --certificate-arn "$CERT_ARN" \
    --region "$REGION" \
    --query 'Certificate.DomainValidationOptions[*].[DomainName,ResourceRecord.Name,ResourceRecord.Value]' \
    --output table 2>&1)

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${BLUE}DNS Validation Records:${NC}"
    echo "$VALIDATION_RECORDS"
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo -e "1. Add the DNS validation records shown above to your domain's DNS"
    echo -e "2. Wait for validation to complete (usually 5-30 minutes)"
    echo -e "3. Check status: ${BLUE}aws acm describe-certificate --certificate-arn ${CERT_ARN} --region ${REGION}${NC}"
    echo -e "4. Once validated, update the API stack with: ${BLUE}export SSL_CERTIFICATE_ARN=${CERT_ARN}${NC}"
    echo -e "5. Redeploy the API stack: ${BLUE}cd infrastructure && npm run deploy:api${NC}"
else
    echo -e "${YELLOW}Could not fetch validation details. Check certificate status manually:${NC}"
    echo -e "${BLUE}aws acm describe-certificate --certificate-arn ${CERT_ARN} --region ${REGION}${NC}"
fi

echo ""
echo -e "${GREEN}Certificate ARN saved. Use this when deploying the API stack:${NC}"
echo -e "${BLUE}export SSL_CERTIFICATE_ARN=${CERT_ARN}${NC}"

