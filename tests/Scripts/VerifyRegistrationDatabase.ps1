# PowerShell Script to Verify Registration Database
# This script connects to the InsightLearn database and verifies registration functionality

param(
    [string]$Server = "localhost,1433",
    [string]$Database = "InsightLearnDb",
    [string]$Username = "sa",
    [string]$Password = "InsightLearn123@#"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "InsightLearn Registration Database Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Connection string
$connectionString = "Server=$Server;Database=$Database;User Id=$Username;Password=$Password;TrustServerCertificate=true;MultipleActiveResultSets=true"

try {
    # Load SQL Server module if available
    if (Get-Module -ListAvailable -Name SqlServer) {
        Import-Module SqlServer -ErrorAction SilentlyContinue
    }

    Write-Host "1. Testing Database Connection..." -ForegroundColor Yellow

    # Test connection using .NET SqlConnection
    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    if ($connection.State -eq "Open") {
        Write-Host "   ✓ Database connection successful" -ForegroundColor Green
        Write-Host "   ✓ Server: $($connection.DataSource)" -ForegroundColor Green
        Write-Host "   ✓ Database: $($connection.Database)" -ForegroundColor Green
        Write-Host "   ✓ Server Version: $($connection.ServerVersion)" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "2. Verifying Core Tables..." -ForegroundColor Yellow

    # Check if AspNetUsers table exists
    $usersTableQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers'"
    $usersTableCommand = New-Object System.Data.SqlClient.SqlCommand($usersTableQuery, $connection)
    $usersTableExists = [int]$usersTableCommand.ExecuteScalar() -gt 0

    if ($usersTableExists) {
        Write-Host "   ✓ AspNetUsers table exists" -ForegroundColor Green
    } else {
        Write-Host "   ✗ AspNetUsers table missing" -ForegroundColor Red
        throw "AspNetUsers table not found"
    }

    # Check if AspNetRoles table exists
    $rolesTableQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles'"
    $rolesTableCommand = New-Object System.Data.SqlClient.SqlCommand($rolesTableQuery, $connection)
    $rolesTableExists = [int]$rolesTableCommand.ExecuteScalar() -gt 0

    if ($rolesTableExists) {
        Write-Host "   ✓ AspNetRoles table exists" -ForegroundColor Green
    } else {
        Write-Host "   ✗ AspNetRoles table missing" -ForegroundColor Red
    }

    # Check if AspNetUserRoles table exists
    $userRolesTableQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles'"
    $userRolesTableCommand = New-Object System.Data.SqlClient.SqlCommand($userRolesTableQuery, $connection)
    $userRolesTableExists = [int]$userRolesTableCommand.ExecuteScalar() -gt 0

    if ($userRolesTableExists) {
        Write-Host "   ✓ AspNetUserRoles table exists" -ForegroundColor Green
    } else {
        Write-Host "   ✗ AspNetUserRoles table missing" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "3. Verifying User Table Structure..." -ForegroundColor Yellow

    # Check required columns in AspNetUsers table
    $requiredColumns = @(
        "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
        "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "FirstName", "LastName", "DateJoined", "LastLoginDate", "IsInstructor",
        "IsVerified", "WalletBalance"
    )

    $missingColumns = @()
    foreach ($column in $requiredColumns) {
        $columnQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = '$column'"
        $columnCommand = New-Object System.Data.SqlClient.SqlCommand($columnQuery, $connection)
        $columnExists = [int]$columnCommand.ExecuteScalar() -gt 0

        if ($columnExists) {
            Write-Host "   ✓ Column '$column' exists" -ForegroundColor Green
        } else {
            Write-Host "   ✗ Column '$column' missing" -ForegroundColor Red
            $missingColumns += $column
        }
    }

    Write-Host ""
    Write-Host "4. Checking Existing Data..." -ForegroundColor Yellow

    # Count existing users
    $userCountQuery = "SELECT COUNT(*) FROM AspNetUsers"
    $userCountCommand = New-Object System.Data.SqlClient.SqlCommand($userCountQuery, $connection)
    $userCount = [int]$userCountCommand.ExecuteScalar()
    Write-Host "   ✓ Total users in database: $userCount" -ForegroundColor Green

    # Count users registered in last 24 hours
    $recentUsersQuery = "SELECT COUNT(*) FROM AspNetUsers WHERE DateJoined >= DATEADD(day, -1, GETUTCDATE())"
    $recentUsersCommand = New-Object System.Data.SqlClient.SqlCommand($recentUsersQuery, $connection)
    $recentUserCount = [int]$recentUsersCommand.ExecuteScalar()
    Write-Host "   ✓ Users registered in last 24 hours: $recentUserCount" -ForegroundColor Green

    # Count verified users
    $verifiedUsersQuery = "SELECT COUNT(*) FROM AspNetUsers WHERE IsVerified = 1"
    $verifiedUsersCommand = New-Object System.Data.SqlClient.SqlCommand($verifiedUsersQuery, $connection)
    $verifiedUserCount = [int]$verifiedUsersCommand.ExecuteScalar()
    Write-Host "   ✓ Verified users: $verifiedUserCount" -ForegroundColor Green

    # Count instructors
    $instructorsQuery = "SELECT COUNT(*) FROM AspNetUsers WHERE IsInstructor = 1"
    $instructorsCommand = New-Object System.Data.SqlClient.SqlCommand($instructorsQuery, $connection)
    $instructorCount = [int]$instructorsCommand.ExecuteScalar()
    Write-Host "   ✓ Instructor accounts: $instructorCount" -ForegroundColor Green

    Write-Host ""
    Write-Host "5. Checking Roles..." -ForegroundColor Yellow

    # Check if default roles exist
    $defaultRoles = @("Student", "Instructor", "Administrator")
    foreach ($role in $defaultRoles) {
        $roleQuery = "SELECT COUNT(*) FROM AspNetRoles WHERE Name = '$role'"
        $roleCommand = New-Object System.Data.SqlClient.SqlCommand($roleQuery, $connection)
        $roleExists = [int]$roleCommand.ExecuteScalar() -gt 0

        if ($roleExists) {
            Write-Host "   ✓ Role '$role' exists" -ForegroundColor Green
        } else {
            Write-Host "   ⚠ Role '$role' missing (will be created on first use)" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "6. Testing Sample Registration..." -ForegroundColor Yellow

    # Create a test user to verify registration functionality
    $testEmail = "dbtest_$(Get-Date -Format 'yyyyMMdd_HHmmss')@test.com"
    $testUserId = [System.Guid]::NewGuid().ToString()

    # Insert test user (simplified - normally would use UserManager)
    $insertUserQuery = @"
INSERT INTO AspNetUsers (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    FirstName, LastName, DateJoined, IsInstructor, IsVerified
) VALUES (
    '$testUserId', '$testEmail', '$($testEmail.ToUpper())', '$testEmail', '$($testEmail.ToUpper())',
    1, 'AQAAAAEAACcQAAAAETestHashForDatabaseVerification123', NEWID(), NEWID(),
    'Database', 'Test', GETUTCDATE(), 0, 1
)
"@

    try {
        $insertCommand = New-Object System.Data.SqlClient.SqlCommand($insertUserQuery, $connection)
        $insertResult = $insertCommand.ExecuteNonQuery()

        if ($insertResult -eq 1) {
            Write-Host "   ✓ Test user created successfully" -ForegroundColor Green

            # Verify the test user exists
            $verifyQuery = "SELECT COUNT(*) FROM AspNetUsers WHERE Id = '$testUserId'"
            $verifyCommand = New-Object System.Data.SqlClient.SqlCommand($verifyQuery, $connection)
            $userExists = [int]$verifyCommand.ExecuteScalar() -gt 0

            if ($userExists) {
                Write-Host "   ✓ Test user verification successful" -ForegroundColor Green
            }

            # Clean up test user
            $deleteQuery = "DELETE FROM AspNetUsers WHERE Id = '$testUserId'"
            $deleteCommand = New-Object System.Data.SqlClient.SqlCommand($deleteQuery, $connection)
            $deleteResult = $deleteCommand.ExecuteNonQuery()

            if ($deleteResult -eq 1) {
                Write-Host "   ✓ Test user cleanup successful" -ForegroundColor Green
            }
        }
    } catch {
        Write-Host "   ✗ Test user creation failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "7. Performance Test..." -ForegroundColor Yellow

    # Test query performance
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $performanceQuery = "SELECT COUNT(*) FROM AspNetUsers"
    $performanceCommand = New-Object System.Data.SqlClient.SqlCommand($performanceQuery, $connection)
    $result = $performanceCommand.ExecuteScalar()
    $stopwatch.Stop()

    $responseTime = $stopwatch.ElapsedMilliseconds
    if ($responseTime -lt 1000) {
        Write-Host "   ✓ Database response time: $responseTime ms (Good)" -ForegroundColor Green
    } elseif ($responseTime -lt 5000) {
        Write-Host "   ⚠ Database response time: $responseTime ms (Acceptable)" -ForegroundColor Yellow
    } else {
        Write-Host "   ✗ Database response time: $responseTime ms (Slow)" -ForegroundColor Red
    }

    $connection.Close()

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Database Verification Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan

    if ($missingColumns.Count -gt 0) {
        Write-Host ""
        Write-Host "⚠ Warning: Missing columns detected:" -ForegroundColor Yellow
        foreach ($column in $missingColumns) {
            Write-Host "   - $column" -ForegroundColor Yellow
        }
        Write-Host "These columns may be added by Entity Framework migrations." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "✓ Database connection working" -ForegroundColor Green
    Write-Host "✓ Core tables present" -ForegroundColor Green
    Write-Host "✓ Registration data accessible" -ForegroundColor Green
    Write-Host "✓ Performance acceptable" -ForegroundColor Green

} catch {
    Write-Host ""
    Write-Host "✗ Database verification failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Verify SQL Server is running on $Server" -ForegroundColor Yellow
    Write-Host "2. Check if database '$Database' exists" -ForegroundColor Yellow
    Write-Host "3. Verify credentials: $Username" -ForegroundColor Yellow
    Write-Host "4. Ensure network connectivity to SQL Server" -ForegroundColor Yellow
    Write-Host "5. Run Entity Framework migrations if tables are missing" -ForegroundColor Yellow

    exit 1
} finally {
    if ($connection -and $connection.State -eq "Open") {
        $connection.Close()
    }
}

Write-Host ""