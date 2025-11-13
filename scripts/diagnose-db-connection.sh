#!/bin/bash
# AWS RDS Connection Diagnostic Script
# This script helps diagnose connection issues to AWS RDS PostgreSQL

set -e

echo "=== AWS RDS Connection Diagnostic ==="
echo ""

# Check AWS CLI
if ! command -v aws &> /dev/null; then
    echo "❌ AWS CLI not found. Install it first."
    exit 1
fi

# Check if AWS credentials are configured
if ! aws sts get-caller-identity &>/dev/null; then
    echo "❌ AWS credentials not configured. Run 'aws configure' first."
    exit 1
fi

# Get database endpoint
echo "1. Getting database endpoint..."
DB_ENDPOINT=$(aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].Endpoint.Address" \
  --output text 2>/dev/null)

if [ -z "$DB_ENDPOINT" ]; then
    echo "❌ Could not find database instance. Check your AWS credentials and region."
    echo "   Make sure you're in the correct AWS region where the database was deployed."
    exit 1
fi

echo "✅ Database endpoint: $DB_ENDPOINT"
echo ""

# Check public accessibility
echo "2. Checking public accessibility..."
PUBLIC=$(aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].PubliclyAccessible" \
  --output text)

if [ "$PUBLIC" != "True" ]; then
    echo "❌ Database is NOT publicly accessible"
    echo "   Solution: Modify RDS instance to enable public access"
    echo "   Command: aws rds modify-db-instance --db-instance-identifier <ID> --publicly-accessible --apply-immediately"
else
    echo "✅ Database is publicly accessible"
fi
echo ""

# Check security group
echo "3. Checking security group rules..."
SG_ID=$(aws ec2 describe-security-groups \
  --filters "Name=tag:Project,Values=SmartScheduler" "Name=description,Values=*database*" \
  --query "SecurityGroups[0].GroupId" \
  --output text 2>/dev/null)

if [ -z "$SG_ID" ] || [ "$SG_ID" == "None" ]; then
    echo "⚠️  Could not find security group with expected tags"
    echo "   Trying alternative method..."
    # Try to find security group by RDS instance
    DB_ID=$(aws rds describe-db-instances \
      --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].DBInstanceIdentifier" \
      --output text)
    if [ -n "$DB_ID" ]; then
        SG_ID=$(aws rds describe-db-instances \
          --db-instance-identifier "$DB_ID" \
          --query "DBInstances[0].VpcSecurityGroups[0].VpcSecurityGroupId" \
          --output text)
    fi
fi

if [ -n "$SG_ID" ] && [ "$SG_ID" != "None" ]; then
    echo "✅ Security group ID: $SG_ID"
    # Check if port 5432 is open
    PORT_5432=$(aws ec2 describe-security-groups \
      --group-ids "$SG_ID" \
      --query "SecurityGroups[0].IpPermissions[?FromPort==\`5432\`]" \
      --output json 2>/dev/null)
    
    if [ -n "$PORT_5432" ] && [ "$PORT_5432" != "[]" ]; then
        echo "✅ Port 5432 is open in security group"
        # Show the rules
        echo "   Ingress rules for port 5432:"
        aws ec2 describe-security-groups \
          --group-ids "$SG_ID" \
          --query "SecurityGroups[0].IpPermissions[?FromPort==\`5432\`].IpRanges[*].CidrIp" \
          --output text | tr '\t' '\n' | sed 's/^/      - /'
    else
        echo "❌ Port 5432 is NOT open in security group"
        echo "   Solution: Add ingress rule for port 5432"
        echo "   Command: aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 5432 --cidr 0.0.0.0/0"
    fi
else
    echo "❌ Could not find security group"
fi
echo ""

# Test network connectivity
echo "4. Testing network connectivity..."
if command -v nc &> /dev/null; then
    if timeout 5 nc -zv "$DB_ENDPOINT" 5432 2>&1 | grep -q "succeeded\|open"; then
        echo "✅ Network connection successful"
    else
        echo "❌ Network connection failed"
        echo "   Check: Security group, firewall, network restrictions"
    fi
elif command -v telnet &> /dev/null; then
    if timeout 5 bash -c "echo > /dev/tcp/$DB_ENDPOINT/5432" 2>/dev/null; then
        echo "✅ Network connection successful"
    else
        echo "❌ Network connection failed"
    fi
else
    echo "⚠️  Cannot test connectivity (nc or telnet not available)"
    echo "   You can test manually with: nc -zv $DB_ENDPOINT 5432"
fi
echo ""

# Check credentials
echo "5. Checking credentials availability..."
if aws secretsmanager get-secret-value --secret-id smartscheduler/database/master-credentials &>/dev/null; then
    echo "✅ Credentials secret exists"
    echo "   Secret name: smartscheduler/database/master-credentials"
    
    # Try to get username (don't show password)
    USERNAME=$(aws secretsmanager get-secret-value \
      --secret-id smartscheduler/database/master-credentials \
      --query SecretString \
      --output text 2>/dev/null | jq -r '.username' 2>/dev/null || echo "dbadmin")
    echo "   Username: $USERNAME"
else
    echo "❌ Credentials secret not found"
    echo "   Secret name: smartscheduler/database/master-credentials"
    echo "   Make sure the Secrets stack has been deployed"
fi
echo ""

# Get your public IP
echo "6. Your public IP address:"
MY_IP=$(curl -s --max-time 5 ifconfig.me 2>/dev/null || curl -s --max-time 5 ipinfo.io/ip 2>/dev/null || echo "Could not determine")
if [ -n "$MY_IP" ] && [ "$MY_IP" != "Could not determine" ]; then
    echo "   $MY_IP"
    echo "   Make sure this IP is allowed in the security group (or use 0.0.0.0/0 for all IPs)"
else
    echo "   Could not determine public IP"
fi
echo ""

# Summary and connection string
echo "=== Summary ==="
echo ""
echo "Database Endpoint: $DB_ENDPOINT"
echo "Database Port: 5432"
echo "Database Name: smartscheduler"
echo ""
echo "To test connection with psql:"
echo "  psql -h $DB_ENDPOINT -U dbadmin -d smartscheduler"
echo ""
echo "To get connection string:"
echo "  aws secretsmanager get-secret-value --secret-id smartscheduler/database/connection-string --query SecretString --output text"
echo ""
echo "=== Diagnostic Complete ==="

