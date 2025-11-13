# AWS RDS Connection Diagnostic Script (PowerShell)
# This script helps diagnose connection issues to AWS RDS PostgreSQL

Write-Host "=== AWS RDS Connection Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

# Check AWS CLI
if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Host "❌ AWS CLI not found. Install it first." -ForegroundColor Red
    exit 1
}

# Check if AWS credentials are configured
try {
    $null = aws sts get-caller-identity 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "AWS credentials not configured"
    }
} catch {
    Write-Host "❌ AWS credentials not configured. Run 'aws configure' first." -ForegroundColor Red
    exit 1
}

# Get database endpoint
Write-Host "1. Getting database endpoint..." -ForegroundColor Yellow
$dbEndpoint = aws rds describe-db-instances `
    --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].Endpoint.Address" `
    --output text 2>$null

if ([string]::IsNullOrEmpty($dbEndpoint)) {
    Write-Host "❌ Could not find database instance. Check your AWS credentials and region." -ForegroundColor Red
    Write-Host "   Make sure you're in the correct AWS region where the database was deployed." -ForegroundColor Yellow
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
    Write-Host "   Command: aws rds modify-db-instance --db-instance-identifier <ID> --publicly-accessible --apply-immediately" -ForegroundColor Gray
} else {
    Write-Host "✅ Database is publicly accessible" -ForegroundColor Green
}
Write-Host ""

# Check security group
Write-Host "3. Checking security group rules..." -ForegroundColor Yellow
$sgId = aws ec2 describe-security-groups `
    --filters "Name=tag:Project,Values=SmartScheduler" "Name=description,Values=*database*" `
    --query "SecurityGroups[0].GroupId" `
    --output text 2>$null

if ([string]::IsNullOrEmpty($sgId) -or $sgId -eq "None") {
    Write-Host "⚠️  Could not find security group with expected tags" -ForegroundColor Yellow
    Write-Host "   Trying alternative method..." -ForegroundColor Yellow
    # Try to find security group by RDS instance
    $dbId = aws rds describe-db-instances `
        --query "DBInstances[?contains(DBInstanceIdentifier, 'smartscheduler')].DBInstanceIdentifier" `
        --output text
    if (-not [string]::IsNullOrEmpty($dbId)) {
        $sgId = aws rds describe-db-instances `
            --db-instance-identifier $dbId `
            --query "DBInstances[0].VpcSecurityGroups[0].VpcSecurityGroupId" `
            --output text
    }
}

if (-not [string]::IsNullOrEmpty($sgId) -and $sgId -ne "None") {
    Write-Host "✅ Security group ID: $sgId" -ForegroundColor Green
    
    # Check if port 5432 is open
    $port5432 = aws ec2 describe-security-groups `
        --group-ids $sgId `
        --query "SecurityGroups[0].IpPermissions[?FromPort==\`5432\`]" `
        --output json 2>$null
    
    if ($port5432 -and $port5432 -ne "[]") {
        Write-Host "✅ Port 5432 is open in security group" -ForegroundColor Green
        Write-Host "   Ingress rules for port 5432:" -ForegroundColor Cyan
        $rules = aws ec2 describe-security-groups `
            --group-ids $sgId `
            --query "SecurityGroups[0].IpPermissions[?FromPort==\`5432\`].IpRanges[*].CidrIp" `
            --output text
        $rules -split "`t" | ForEach-Object {
            if ($_) { Write-Host "      - $_" -ForegroundColor Gray }
        }
    } else {
        Write-Host "❌ Port 5432 is NOT open in security group" -ForegroundColor Red
        Write-Host "   Solution: Add ingress rule for port 5432" -ForegroundColor Yellow
        Write-Host "   Command: aws ec2 authorize-security-group-ingress --group-id $sgId --protocol tcp --port 5432 --cidr 0.0.0.0/0" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ Could not find security group" -ForegroundColor Red
}
Write-Host ""

# Test network connectivity
Write-Host "4. Testing network connectivity..." -ForegroundColor Yellow
try {
    $test = Test-NetConnection -ComputerName $dbEndpoint -Port 5432 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($test) {
        Write-Host "✅ Network connection successful" -ForegroundColor Green
    } else {
        Write-Host "❌ Network connection failed" -ForegroundColor Red
        Write-Host "   Check: Security group, firewall, network restrictions" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Could not test connectivity: $_" -ForegroundColor Yellow
}
Write-Host ""

# Check credentials
Write-Host "5. Checking credentials availability..." -ForegroundColor Yellow
$secretCheck = aws secretsmanager get-secret-value --secret-id smartscheduler/database/master-credentials 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Credentials secret exists" -ForegroundColor Green
    Write-Host "   Secret name: smartscheduler/database/master-credentials" -ForegroundColor Gray
    
    # Try to get username (don't show password)
    try {
        $secretJson = aws secretsmanager get-secret-value `
            --secret-id smartscheduler/database/master-credentials `
            --query SecretString `
            --output text 2>$null
        $secretObj = $secretJson | ConvertFrom-Json
        Write-Host "   Username: $($secretObj.username)" -ForegroundColor Gray
    } catch {
        Write-Host "   Username: dbadmin (default)" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ Credentials secret not found" -ForegroundColor Red
    Write-Host "   Secret name: smartscheduler/database/master-credentials" -ForegroundColor Yellow
    Write-Host "   Make sure the Secrets stack has been deployed" -ForegroundColor Yellow
}
Write-Host ""

# Get your public IP
Write-Host "6. Your public IP address:" -ForegroundColor Yellow
try {
    $myIp = (Invoke-WebRequest -Uri "https://ifconfig.me" -UseBasicParsing -TimeoutSec 5).Content.Trim()
    Write-Host "   $myIp" -ForegroundColor Cyan
    Write-Host "   Make sure this IP is allowed in the security group (or use 0.0.0.0/0 for all IPs)" -ForegroundColor Yellow
} catch {
    try {
        $myIp = (Invoke-WebRequest -Uri "https://ipinfo.io/ip" -UseBasicParsing -TimeoutSec 5).Content.Trim()
        Write-Host "   $myIp" -ForegroundColor Cyan
        Write-Host "   Make sure this IP is allowed in the security group (or use 0.0.0.0/0 for all IPs)" -ForegroundColor Yellow
    } catch {
        Write-Host "   Could not determine public IP" -ForegroundColor Yellow
    }
}
Write-Host ""

# Summary and connection string
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Database Endpoint: $dbEndpoint" -ForegroundColor White
Write-Host "Database Port: 5432" -ForegroundColor White
Write-Host "Database Name: smartscheduler" -ForegroundColor White
Write-Host ""
Write-Host "To test connection with psql:" -ForegroundColor Yellow
Write-Host "  psql -h $dbEndpoint -U dbadmin -d smartscheduler" -ForegroundColor Gray
Write-Host ""
Write-Host "To get connection string:" -ForegroundColor Yellow
Write-Host "  aws secretsmanager get-secret-value --secret-id smartscheduler/database/connection-string --query SecretString --output text" -ForegroundColor Gray
Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan


