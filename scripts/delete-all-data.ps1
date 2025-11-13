# PowerShell script to delete all data from the database
# WARNING: This will delete ALL data! Use with caution.

param(
    [string]$ConnectionString = $env:DB_CONNECTION_STRING
)

# Check if connection string is provided
if ([string]::IsNullOrEmpty($ConnectionString)) {
    Write-Host "Error: Connection string not provided." -ForegroundColor Red
    Write-Host "Usage: .\delete-all-data.ps1 -ConnectionString 'your_connection_string'" -ForegroundColor Yellow
    Write-Host "Or set the DB_CONNECTION_STRING environment variable" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "WARNING: This will delete ALL data from the database!" -ForegroundColor Red
Write-Host "This includes:" -ForegroundColor Yellow
Write-Host "  - All contractors" -ForegroundColor Yellow
Write-Host "  - All jobs" -ForegroundColor Yellow
Write-Host "  - All assignments" -ForegroundColor Yellow
Write-Host "  - All audit records" -ForegroundColor Yellow
Write-Host "  - All event logs" -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Type 'DELETE ALL' to confirm"

if ($confirmation -ne "DELETE ALL") {
    Write-Host "Operation cancelled." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "Connecting to database..." -ForegroundColor Cyan

try {
    # Load Npgsql assembly
    Add-Type -Path "$PSScriptRoot\..\src\SmartScheduler.Api\bin\Debug\net8.0\Npgsql.dll"

    $conn = New-Object Npgsql.NpgsqlConnection($ConnectionString)
    $conn.Open()

    Write-Host "Connected successfully!" -ForegroundColor Green
    Write-Host ""

    # Start transaction
    $transaction = $conn.BeginTransaction()

    try {
        # Delete in order to respect foreign key constraints
        $tables = @(
            "Assignments",
            "AuditRecommendations", 
            "EventLogs",
            "Jobs",
            "Contractors"
        )

        $totalDeleted = 0

        foreach ($table in $tables) {
            Write-Host "Deleting from $table..." -ForegroundColor Cyan
            
            $cmd = $conn.CreateCommand()
            $cmd.Transaction = $transaction
            $cmd.CommandText = "DELETE FROM `"$table`""
            
            $rowsDeleted = $cmd.ExecuteNonQuery()
            Write-Host "  Deleted $rowsDeleted rows from $table" -ForegroundColor Yellow
            $totalDeleted += $rowsDeleted
        }

        # Commit transaction
        $transaction.Commit()
        
        Write-Host ""
        Write-Host "SUCCESS: Deleted $totalDeleted total rows from all tables!" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        $transaction.Rollback()
        throw
    }
    finally {
        $conn.Close()
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor DarkRed
    exit 1
}

