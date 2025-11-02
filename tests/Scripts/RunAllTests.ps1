# Comprehensive InsightLearn Registration Test Runner
# This script runs all registration tests and generates a detailed report

param(
    [string]$TestProject = "C:\kubernetes\tests\InsightLearn.Tests.csproj",
    [string]$ReportPath = "C:\kubernetes\tests\TestResults",
    [string]$DatabaseServer = "localhost,1433",
    [string]$DatabaseName = "InsightLearnDb",
    [string]$DatabaseUser = "sa",
    [string]$DatabasePassword = "InsightLearn123@#",
    [switch]$SkipDatabaseTests,
    [switch]$SkipSecurityTests,
    [switch]$GenerateReport = $true
)

# Colors for output
$SuccessColor = "Green"
$WarningColor = "Yellow"
$ErrorColor = "Red"
$InfoColor = "Cyan"

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "InsightLearn Registration Test Suite" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

# Create reports directory
if (-not (Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Path $ReportPath -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $ReportPath "TestReport_$timestamp.html"
$logFile = Join-Path $ReportPath "TestLog_$timestamp.txt"

# Initialize test results
$testResults = @{
    StartTime = Get-Date
    DatabaseConnectionTest = @{ Status = "Not Run"; Details = "" }
    UnitTests = @{ Status = "Not Run"; Details = ""; PassedTests = 0; FailedTests = 0; TotalTests = 0 }
    IntegrationTests = @{ Status = "Not Run"; Details = ""; PassedTests = 0; FailedTests = 0; TotalTests = 0 }
    SecurityTests = @{ Status = "Not Run"; Details = ""; PassedTests = 0; FailedTests = 0; TotalTests = 0 }
    FormValidationTests = @{ Status = "Not Run"; Details = ""; PassedTests = 0; FailedTests = 0; TotalTests = 0 }
    OAuthTests = @{ Status = "Not Run"; Details = ""; PassedTests = 0; FailedTests = 0; TotalTests = 0 }
    DatabaseVerificationTests = @{ Status = "Not Run"; Details = ""; PassedTests = 0; FailedTests = 0; TotalTests = 0 }
    OverallStatus = "In Progress"
    EndTime = $null
    Duration = $null
    TotalTestsRun = 0
    TotalTestsPassed = 0
    TotalTestsFailed = 0
}

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $logMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] $Message"
    Add-Content -Path $logFile -Value $logMessage

    switch ($Level) {
        "ERROR" { Write-Host $Message -ForegroundColor $ErrorColor }
        "WARNING" { Write-Host $Message -ForegroundColor $WarningColor }
        "SUCCESS" { Write-Host $Message -ForegroundColor $SuccessColor }
        default { Write-Host $Message -ForegroundColor White }
    }
}

function Test-DatabaseConnection {
    Write-TestLog "Testing database connection..." "INFO"

    try {
        # Run the database verification script
        $dbTestScript = "C:\kubernetes\tests\Scripts\VerifyRegistrationDatabase.ps1"
        if (Test-Path $dbTestScript) {
            $dbResult = & PowerShell -File $dbTestScript -Server $DatabaseServer -Database $DatabaseName -Username $DatabaseUser -Password $DatabasePassword 2>&1

            if ($LASTEXITCODE -eq 0) {
                $testResults.DatabaseConnectionTest.Status = "Passed"
                $testResults.DatabaseConnectionTest.Details = "Database connection successful"
                Write-TestLog "Database connection test: PASSED" "SUCCESS"
                return $true
            } else {
                $testResults.DatabaseConnectionTest.Status = "Failed"
                $testResults.DatabaseConnectionTest.Details = $dbResult -join "`n"
                Write-TestLog "Database connection test: FAILED" "ERROR"
                return $false
            }
        } else {
            $testResults.DatabaseConnectionTest.Status = "Skipped"
            $testResults.DatabaseConnectionTest.Details = "Database verification script not found"
            Write-TestLog "Database verification script not found" "WARNING"
            return $false
        }
    } catch {
        $testResults.DatabaseConnectionTest.Status = "Failed"
        $testResults.DatabaseConnectionTest.Details = $_.Exception.Message
        Write-TestLog "Database connection test failed: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Run-DotNetTests {
    param(
        [string]$TestCategory,
        [string]$Filter = ""
    )

    Write-TestLog "Running $TestCategory tests..." "INFO"

    try {
        # Build the test project first
        Write-TestLog "Building test project..." "INFO"
        $buildResult = dotnet build $TestProject --no-restore 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-TestLog "Build failed: $buildResult" "ERROR"
            return @{ Status = "Failed"; Details = "Build failed: $buildResult"; PassedTests = 0; FailedTests = 0; TotalTests = 0 }
        }

        # Run tests with coverage
        $testCommand = "dotnet test `"$TestProject`" --no-build --logger `"trx;LogFileName=${TestCategory}_$timestamp.trx`" --results-directory `"$ReportPath`""

        if ($Filter) {
            $testCommand += " --filter `"$Filter`""
        }

        Write-TestLog "Executing: $testCommand" "INFO"
        $testOutput = Invoke-Expression $testCommand 2>&1

        # Parse test results
        $passedCount = 0
        $failedCount = 0
        $totalCount = 0

        if ($testOutput -match "Passed!\s+(\d+)\s+passed,\s+(\d+)\s+failed,\s+(\d+)\s+skipped") {
            $passedCount = [int]$Matches[1]
            $failedCount = [int]$Matches[2]
            $totalCount = $passedCount + $failedCount + [int]$Matches[3]
        } elseif ($testOutput -match "(\d+)\s+passed,\s+(\d+)\s+failed") {
            $passedCount = [int]$Matches[1]
            $failedCount = [int]$Matches[2]
            $totalCount = $passedCount + $failedCount
        }

        $status = if ($failedCount -eq 0 -and $totalCount -gt 0) { "Passed" } elseif ($totalCount -eq 0) { "No Tests Found" } else { "Failed" }

        Write-TestLog "$TestCategory tests: $status ($passedCount passed, $failedCount failed, $totalCount total)" "INFO"

        return @{
            Status = $status
            Details = $testOutput -join "`n"
            PassedTests = $passedCount
            FailedTests = $failedCount
            TotalTests = $totalCount
        }
    } catch {
        Write-TestLog "$TestCategory tests failed with exception: $($_.Exception.Message)" "ERROR"
        return @{
            Status = "Failed"
            Details = "Exception: $($_.Exception.Message)"
            PassedTests = 0
            FailedTests = 0
            TotalTests = 0
        }
    }
}

function Generate-HtmlReport {
    Write-TestLog "Generating HTML test report..." "INFO"

    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>InsightLearn Registration Test Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { text-align: center; margin-bottom: 30px; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 8px; }
        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin-bottom: 30px; }
        .summary-card { background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 15px; text-align: center; }
        .summary-card h3 { margin: 0 0 10px 0; color: #495057; }
        .summary-card .number { font-size: 2em; font-weight: bold; margin: 10px 0; }
        .passed { color: #28a745; }
        .failed { color: #dc3545; }
        .warning { color: #ffc107; }
        .info { color: #17a2b8; }
        .test-section { margin-bottom: 30px; border: 1px solid #dee2e6; border-radius: 8px; overflow: hidden; }
        .test-section-header { background: #343a40; color: white; padding: 15px; font-weight: bold; }
        .test-section-content { padding: 20px; }
        .status-badge { padding: 4px 12px; border-radius: 20px; color: white; font-weight: bold; display: inline-block; margin-left: 10px; }
        .status-passed { background-color: #28a745; }
        .status-failed { background-color: #dc3545; }
        .status-warning { background-color: #ffc107; color: #212529; }
        .status-skipped { background-color: #6c757d; }
        .details { background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; padding: 15px; margin-top: 10px; white-space: pre-wrap; font-family: monospace; font-size: 12px; max-height: 300px; overflow-y: auto; }
        .metrics { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-top: 15px; }
        .metric { text-align: center; padding: 10px; border: 1px solid #dee2e6; border-radius: 4px; }
        .footer { text-align: center; margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 8px; color: #6c757d; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>InsightLearn Registration Test Report</h1>
            <p>Comprehensive testing of registration system functionality</p>
            <p>Generated on $(Get-Date -Format "MMMM dd, yyyy 'at' HH:mm:ss")</p>
        </div>

        <div class="summary">
            <div class="summary-card">
                <h3>Overall Status</h3>
                <div class="number $(if($testResults.OverallStatus -eq 'Passed'){'passed'}elseif($testResults.OverallStatus -eq 'Failed'){'failed'}else{'warning'})">$($testResults.OverallStatus)</div>
            </div>
            <div class="summary-card">
                <h3>Total Tests</h3>
                <div class="number info">$($testResults.TotalTestsRun)</div>
            </div>
            <div class="summary-card">
                <h3>Passed</h3>
                <div class="number passed">$($testResults.TotalTestsPassed)</div>
            </div>
            <div class="summary-card">
                <h3>Failed</h3>
                <div class="number failed">$($testResults.TotalTestsFailed)</div>
            </div>
            <div class="summary-card">
                <h3>Duration</h3>
                <div class="number info">$($testResults.Duration)</div>
            </div>
        </div>
"@

    # Add test sections
    $testSections = @(
        @{ Name = "Database Connection"; Key = "DatabaseConnectionTest"; Description = "Tests database connectivity and basic operations" },
        @{ Name = "Unit Tests"; Key = "UnitTests"; Description = "Tests individual components and services" },
        @{ Name = "Integration Tests"; Key = "IntegrationTests"; Description = "Tests API endpoints and complete workflows" },
        @{ Name = "Security Tests"; Key = "SecurityTests"; Description = "Tests security measures and vulnerability prevention" },
        @{ Name = "Form Validation"; Key = "FormValidationTests"; Description = "Tests user interface form validation" },
        @{ Name = "OAuth Tests"; Key = "OAuthTests"; Description = "Tests Google OAuth integration" },
        @{ Name = "Database Verification"; Key = "DatabaseVerificationTests"; Description = "Tests database schema and data integrity" }
    )

    foreach ($section in $testSections) {
        $result = $testResults[$section.Key]
        $statusClass = switch ($result.Status) {
            "Passed" { "status-passed" }
            "Failed" { "status-failed" }
            "Skipped" { "status-warning" }
            "Not Run" { "status-skipped" }
            default { "status-skipped" }
        }

        $html += @"
        <div class="test-section">
            <div class="test-section-header">
                $($section.Name)
                <span class="status-badge $statusClass">$($result.Status)</span>
            </div>
            <div class="test-section-content">
                <p>$($section.Description)</p>
"@

        if ($result.PSObject.Properties.Name -contains "PassedTests") {
            $html += @"
                <div class="metrics">
                    <div class="metric">
                        <strong>Total Tests</strong><br>
                        $($result.TotalTests)
                    </div>
                    <div class="metric">
                        <strong>Passed</strong><br>
                        <span class="passed">$($result.PassedTests)</span>
                    </div>
                    <div class="metric">
                        <strong>Failed</strong><br>
                        <span class="failed">$($result.FailedTests)</span>
                    </div>
                </div>
"@
        }

        if ($result.Details) {
            $html += @"
                <div class="details">$($result.Details)</div>
"@
        }

        $html += @"
            </div>
        </div>
"@
    }

    $html += @"
        <div class="footer">
            <p><strong>Test Environment Information</strong></p>
            <p>Database Server: $DatabaseServer | Database: $DatabaseName</p>
            <p>Test Project: $TestProject</p>
            <p>Report Generated: $(Get-Date)</p>
        </div>
    </div>
</body>
</html>
"@

    $html | Out-File -FilePath $reportFile -Encoding UTF8
    Write-TestLog "HTML report generated: $reportFile" "SUCCESS"
}

# Main execution flow
try {
    Write-TestLog "Starting InsightLearn registration test suite" "INFO"
    Write-TestLog "Test project: $TestProject" "INFO"
    Write-TestLog "Report path: $ReportPath" "INFO"

    # Step 1: Database Connection Test
    if (-not $SkipDatabaseTests) {
        Test-DatabaseConnection | Out-Null
    } else {
        Write-TestLog "Skipping database tests" "WARNING"
        $testResults.DatabaseConnectionTest.Status = "Skipped"
    }

    # Step 2: Check if test project exists
    if (-not (Test-Path $TestProject)) {
        Write-TestLog "Test project not found: $TestProject" "ERROR"
        Write-TestLog "Please ensure the test project has been created and built" "ERROR"
        exit 1
    }

    # Step 3: Restore packages
    Write-TestLog "Restoring NuGet packages..." "INFO"
    dotnet restore $TestProject | Out-Null

    # Step 4: Run Unit Tests
    Write-TestLog "Running unit tests..." "INFO"
    $testResults.UnitTests = Run-DotNetTests "Unit" "Category=Unit|FullyQualifiedName~AuthServiceTests"

    # Step 5: Run Integration Tests
    Write-TestLog "Running integration tests..." "INFO"
    $testResults.IntegrationTests = Run-DotNetTests "Integration" "Category=Integration|FullyQualifiedName~RegistrationApiTests"

    # Step 6: Run Security Tests
    if (-not $SkipSecurityTests) {
        Write-TestLog "Running security tests..." "INFO"
        $testResults.SecurityTests = Run-DotNetTests "Security" "Category=Security|FullyQualifiedName~SecurityTests"
    } else {
        Write-TestLog "Skipping security tests" "WARNING"
        $testResults.SecurityTests.Status = "Skipped"
    }

    # Step 7: Run Form Validation Tests
    Write-TestLog "Running form validation tests..." "INFO"
    $testResults.FormValidationTests = Run-DotNetTests "FormValidation" "Category=UI|FullyQualifiedName~RegistrationFormValidationTests"

    # Step 8: Run OAuth Tests
    Write-TestLog "Running OAuth tests..." "INFO"
    $testResults.OAuthTests = Run-DotNetTests "OAuth" "Category=OAuth|FullyQualifiedName~GoogleOAuthRegistrationTests"

    # Step 9: Run Database Verification Tests
    if (-not $SkipDatabaseTests) {
        Write-TestLog "Running database verification tests..." "INFO"
        $testResults.DatabaseVerificationTests = Run-DotNetTests "DatabaseVerification" "Category=Database|FullyQualifiedName~DatabaseConnectionTests"
    } else {
        Write-TestLog "Skipping database verification tests" "WARNING"
        $testResults.DatabaseVerificationTests.Status = "Skipped"
    }

    # Calculate overall results
    $testResults.EndTime = Get-Date
    $testResults.Duration = $testResults.EndTime - $testResults.StartTime
    $testResults.Duration = "{0:mm\:ss}" -f $testResults.Duration

    # Sum up all test results
    $allTestResults = @($testResults.UnitTests, $testResults.IntegrationTests, $testResults.SecurityTests, $testResults.FormValidationTests, $testResults.OAuthTests, $testResults.DatabaseVerificationTests)
    $testResults.TotalTestsRun = ($allTestResults | ForEach-Object { $_.TotalTests } | Measure-Object -Sum).Sum
    $testResults.TotalTestsPassed = ($allTestResults | ForEach-Object { $_.PassedTests } | Measure-Object -Sum).Sum
    $testResults.TotalTestsFailed = ($allTestResults | ForEach-Object { $_.FailedTests } | Measure-Object -Sum).Sum

    # Determine overall status
    $failedSections = $allTestResults | Where-Object { $_.Status -eq "Failed" }
    $passedSections = $allTestResults | Where-Object { $_.Status -eq "Passed" }

    if ($failedSections.Count -gt 0) {
        $testResults.OverallStatus = "Failed"
    } elseif ($passedSections.Count -eq $allTestResults.Count -and $testResults.DatabaseConnectionTest.Status -eq "Passed") {
        $testResults.OverallStatus = "Passed"
    } else {
        $testResults.OverallStatus = "Partial"
    }

    # Generate report
    if ($GenerateReport) {
        Generate-HtmlReport
    }

    # Final summary
    Write-Host ""
    Write-Host "========================================" -ForegroundColor $InfoColor
    Write-Host "Test Execution Complete!" -ForegroundColor $InfoColor
    Write-Host "========================================" -ForegroundColor $InfoColor
    Write-Host "Overall Status: " -NoNewline

    switch ($testResults.OverallStatus) {
        "Passed" { Write-Host "PASSED" -ForegroundColor $SuccessColor }
        "Failed" { Write-Host "FAILED" -ForegroundColor $ErrorColor }
        default { Write-Host "PARTIAL" -ForegroundColor $WarningColor }
    }

    Write-Host "Total Tests Run: $($testResults.TotalTestsRun)" -ForegroundColor White
    Write-Host "Tests Passed: $($testResults.TotalTestsPassed)" -ForegroundColor $SuccessColor
    Write-Host "Tests Failed: $($testResults.TotalTestsFailed)" -ForegroundColor $ErrorColor
    Write-Host "Duration: $($testResults.Duration)" -ForegroundColor White
    Write-Host ""
    Write-Host "Reports generated:" -ForegroundColor $InfoColor
    Write-Host "  HTML Report: $reportFile" -ForegroundColor White
    Write-Host "  Log File: $logFile" -ForegroundColor White

    # Exit with appropriate code
    if ($testResults.OverallStatus -eq "Failed") {
        exit 1
    } else {
        exit 0
    }

} catch {
    Write-TestLog "Critical error during test execution: $($_.Exception.Message)" "ERROR"
    Write-TestLog "Stack trace: $($_.ScriptStackTrace)" "ERROR"

    if ($GenerateReport) {
        $testResults.OverallStatus = "Critical Error"
        $testResults.EndTime = Get-Date
        Generate-HtmlReport
    }

    exit 1
}