# Troubleshooting AWS RDS Database Connection from Dev Machine

This guide helps diagnose and fix connection issues when connecting to your AWS RDS PostgreSQL database from your local development machine.

## Quick Diagnostic Checklist

### 1. Verify Database Endpoint and Credentials

First, get the database endpoint and credentials:

```bash
# Get database endpoint
aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].Endpoint.Address" \
  --output text

# Get database credentials from Secrets Manager
aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString \
  --output text | jq -r '.password'

# Get username (should be 'dbadmin')
aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString \
  --output text | jq -r '.username'
```

### 2. Test Basic Connectivity

Test if you can reach the database endpoint:

```bash
# Replace with your actual endpoint
DB_ENDPOINT=$(aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].Endpoint.Address" \
  --output text)

# Test TCP connection (should succeed)
nc -zv $DB_ENDPOINT 5432

# On Windows PowerShell:
Test-NetConnection -ComputerName $DB_ENDPOINT -Port 5432
```

### 3. Test PostgreSQL Connection

Try connecting with psql:

```bash
# Get credentials
DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString \
  --output text | jq -r '.password')

DB_USERNAME=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString \
  --output text | jq -r '.username')

# Connect
psql -h $DB_ENDPOINT -U $DB_USERNAME -d smartscheduler
# Enter password when prompted
```

## Common Issues and Solutions

### Issue 1: Security Group Not Allowing Your IP

**Symptoms:**
- Connection timeout
- "Connection refused" error
- `nc` or `Test-NetConnection` fails

**Diagnosis:**
```bash
# Check your public IP
curl ifconfig.me
# or
curl ipinfo.io/ip

# Check security group rules
aws ec2 describe-security-groups \
  --filters "Name=tag:Project,Values=SmartScheduler" \
  --query "SecurityGroups[*].{GroupId:GroupId,GroupName:GroupName,IngressRules:IpPermissions}" \
  --output json
```

**Solution:**
The security group should allow `0.0.0.0/0` (all IPs) for development. If it doesn't, update it:

```bash
# Get security group ID
SG_ID=$(aws ec2 describe-security-groups \
  --filters "Name=tag:Project,Values=SmartScheduler" "Name=description,Values=*database*" \
  --query "SecurityGroups[0].GroupId" \
  --output text)

# Add rule to allow your IP (replace YOUR_IP with your actual IP)
aws ec2 authorize-security-group-ingress \
  --group-id $SG_ID \
  --protocol tcp \
  --port 5432 \
  --cidr YOUR_IP/32

# Or allow all IPs (development only!)
aws ec2 authorize-security-group-ingress \
  --group-id $SG_ID \
  --protocol tcp \
  --port 5432 \
  --cidr 0.0.0.0/0
```

### Issue 2: RDS Instance Not Publicly Accessible

**Symptoms:**
- Connection timeout
- Can't resolve endpoint

**Diagnosis:**
```bash
# Check if RDS is publicly accessible
aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].PubliclyAccessible" \
  --output text
```

**Solution:**
If it returns `False`, you need to modify the RDS instance (this requires downtime):

```bash
# Get DB instance identifier
DB_ID=$(aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].DBInstanceIdentifier" \
  --output text)

# Modify to enable public access
aws rds modify-db-instance \
  --db-instance-identifier $DB_ID \
  --publicly-accessible \
  --apply-immediately
```

**Note:** If the instance is in a private subnet, you'll need to either:
1. Move it to a public subnet, OR
2. Use a VPN or bastion host to connect

### Issue 3: RDS Instance in Private Subnet

**Symptoms:**
- Endpoint resolves but connection times out
- Instance is publicly accessible but still can't connect

**Diagnosis:**
```bash
# Check subnet configuration
aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].{SubnetGroup:DBSubnetGroup.DBSubnetGroupName,Subnets:DBSubnetGroup.Subnets[*].SubnetIdentifier}" \
  --output json

# Check if subnets have internet gateway
aws ec2 describe-subnets \
  --subnet-ids <SUBNET_ID> \
  --query "Subnets[*].{SubnetId:SubnetId,MapPublicIpOnLaunch:MapPublicIpOnLaunch}" \
  --output json
```

**Solution:**
If the RDS is in a private subnet without internet gateway, you have two options:

1. **Use AWS Systems Manager Session Manager** (recommended for production)
2. **Create a bastion host** in a public subnet
3. **Modify CDK to use public subnet** (development only)

### Issue 4: Wrong Connection String Format

**Symptoms:**
- Authentication errors
- "Database does not exist" errors

**Diagnosis:**
Check your connection string format. For PostgreSQL, it should be:

```
Host=<ENDPOINT>;Port=5432;Database=smartscheduler;Username=dbadmin;Password=<PASSWORD>
```

**Solution:**
Get the correct connection string:

```bash
# Get all connection details
DB_ENDPOINT=$(aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].Endpoint.Address" \
  --output text)

DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString \
  --output text | jq -r '.password')

# Build connection string
echo "Host=$DB_ENDPOINT;Port=5432;Database=smartscheduler;Username=dbadmin;Password=$DB_PASSWORD"
```

### Issue 5: Firewall or Network Restrictions

**Symptoms:**
- Connection works from some networks but not others
- Works on VPN but not without

**Diagnosis:**
- Check if your corporate firewall blocks port 5432
- Check if your ISP blocks outbound connections
- Try from a different network (mobile hotspot)

**Solution:**
- Use a VPN
- Configure firewall to allow outbound port 5432
- Use AWS Systems Manager Session Manager for secure tunneling

### Issue 6: SSL/TLS Requirements

**Symptoms:**
- Connection works but requires SSL
- SSL errors

**Diagnosis:**
RDS PostgreSQL requires SSL by default. Your connection string should include SSL parameters.

**Solution:**
Add SSL parameters to connection string:

```
Host=<ENDPOINT>;Port=5432;Database=smartscheduler;Username=dbadmin;Password=<PASSWORD>;SSL Mode=Require
```

Or for .NET EF Core:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<ENDPOINT>;Port=5432;Database=smartscheduler;Username=dbadmin;Password=<PASSWORD>;SSL Mode=Require"
  }
}
```

## Automated Diagnostic Script

Run this script to check all common issues:

```bash
#!/bin/bash
# save as diagnose-db-connection.sh

echo "=== AWS RDS Connection Diagnostic ==="
echo ""

# Check AWS CLI
if ! command -v aws &> /dev/null; then
    echo "❌ AWS CLI not found. Install it first."
    exit 1
fi

# Get database endpoint
echo "1. Getting database endpoint..."
DB_ENDPOINT=$(aws rds describe-db-instances \
  --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].Endpoint.Address" \
  --output text 2>/dev/null)

if [ -z "$DB_ENDPOINT" ]; then
    echo "❌ Could not find database instance. Check your AWS credentials and region."
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

if [ -z "$SG_ID" ]; then
    echo "⚠️  Could not find security group"
else
    echo "✅ Security group ID: $SG_ID"
    # Check if port 5432 is open
    PORT_OPEN=$(aws ec2 describe-security-groups \
      --group-ids $SG_ID \
      --query "SecurityGroups[0].IpPermissions[?FromPort==\`5432\`]" \
      --output json)
    
    if [ "$PORT_OPEN" != "[]" ]; then
        echo "✅ Port 5432 is open in security group"
    else
        echo "❌ Port 5432 is NOT open in security group"
        echo "   Solution: Add ingress rule for port 5432"
    fi
fi
echo ""

# Test network connectivity
echo "4. Testing network connectivity..."
if command -v nc &> /dev/null; then
    if nc -zv -w 5 $DB_ENDPOINT 5432 2>&1 | grep -q "succeeded"; then
        echo "✅ Network connection successful"
    else
        echo "❌ Network connection failed"
        echo "   Check: Security group, firewall, network restrictions"
    fi
elif command -v Test-NetConnection &> /dev/null; then
    # PowerShell on Windows
    if Test-NetConnection -ComputerName $DB_ENDPOINT -Port 5432 -InformationLevel Quiet; then
        echo "✅ Network connection successful"
    else
        echo "❌ Network connection failed"
    fi
else
    echo "⚠️  Cannot test connectivity (nc or Test-NetConnection not available)"
fi
echo ""

# Check credentials
echo "5. Checking credentials availability..."
if aws secretsmanager get-secret-value --secret-id smartscheduler/database/master-credentials &>/dev/null; then
    echo "✅ Credentials secret exists"
else
    echo "❌ Credentials secret not found"
    echo "   Secret name: smartscheduler/database/master-credentials"
fi
echo ""

# Get your public IP
echo "6. Your public IP address:"
MY_IP=$(curl -s ifconfig.me 2>/dev/null || curl -s ipinfo.io/ip 2>/dev/null)
if [ -n "$MY_IP" ]; then
    echo "   $MY_IP"
    echo "   Make sure this IP is allowed in the security group"
else
    echo "   Could not determine public IP"
fi
echo ""

echo "=== Diagnostic Complete ==="
```

## Windows PowerShell Diagnostic Script

```powershell
# save as diagnose-db-connection.ps1

Write-Host "=== AWS RDS Connection Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

# Check AWS CLI
if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Host "❌ AWS CLI not found. Install it first." -ForegroundColor Red
    exit 1
}

# Get database endpoint
Write-Host "1. Getting database endpoint..." -ForegroundColor Yellow
$dbEndpoint = aws rds describe-db-instances `
    --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].Endpoint.Address" `
    --output text 2>$null

if ([string]::IsNullOrEmpty($dbEndpoint)) {
    Write-Host "❌ Could not find database instance. Check your AWS credentials and region." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Database endpoint: $dbEndpoint" -ForegroundColor Green
Write-Host ""

# Check public accessibility
Write-Host "2. Checking public accessibility..." -ForegroundColor Yellow
$public = aws rds describe-db-instances `
    --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].PubliclyAccessible" `
    --output text

if ($public -ne "True") {
    Write-Host "❌ Database is NOT publicly accessible" -ForegroundColor Red
    Write-Host "   Solution: Modify RDS instance to enable public access" -ForegroundColor Yellow
} else {
    Write-Host "✅ Database is publicly accessible" -ForegroundColor Green
}
Write-Host ""

# Test network connectivity
Write-Host "3. Testing network connectivity..." -ForegroundColor Yellow
$test = Test-NetConnection -ComputerName $dbEndpoint -Port 5432 -InformationLevel Quiet -WarningAction SilentlyContinue
if ($test) {
    Write-Host "✅ Network connection successful" -ForegroundColor Green
} else {
    Write-Host "❌ Network connection failed" -ForegroundColor Red
    Write-Host "   Check: Security group, firewall, network restrictions" -ForegroundColor Yellow
}
Write-Host ""

# Check credentials
Write-Host "4. Checking credentials availability..." -ForegroundColor Yellow
$secretCheck = aws secretsmanager get-secret-value --secret-id smartscheduler/database/master-credentials 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Credentials secret exists" -ForegroundColor Green
} else {
    Write-Host "❌ Credentials secret not found" -ForegroundColor Red
    Write-Host "   Secret name: smartscheduler/database/master-credentials" -ForegroundColor Yellow
}
Write-Host ""

# Get your public IP
Write-Host "5. Your public IP address:" -ForegroundColor Yellow
try {
    $myIp = (Invoke-WebRequest -Uri "https://ifconfig.me" -UseBasicParsing).Content.Trim()
    Write-Host "   $myIp" -ForegroundColor Cyan
    Write-Host "   Make sure this IP is allowed in the security group" -ForegroundColor Yellow
} catch {
    Write-Host "   Could not determine public IP" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
```

## Next Steps

1. **Run the diagnostic script** to identify the issue
2. **Fix the identified issue** using the solutions above
3. **Test the connection** using psql or your application
4. **Update your connection string** in `appsettings.json` or environment variables

## Security Recommendations

⚠️ **Important:** The current setup allows connections from anywhere (0.0.0.0/0) which is **NOT secure for production**.

For production, you should:
1. Restrict security group to specific IPs or VPC
2. Set `publiclyAccessible: false` in CDK
3. Use VPN or AWS Systems Manager Session Manager for secure access
4. Enable SSL/TLS encryption (already enabled by default on RDS)

## Getting Help

If you're still having issues after trying these solutions:

1. Check AWS CloudWatch Logs for RDS errors
2. Review security group rules in AWS Console
3. Verify VPC and subnet configuration
4. Check AWS Service Health Dashboard for regional issues


