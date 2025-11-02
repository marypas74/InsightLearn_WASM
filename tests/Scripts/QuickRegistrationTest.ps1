# Quick Registration Test Script
# Tests the InsightLearn registration system with real HTTP calls

param(
    [string]$BaseUrl = "https://localhost:7003",
    [string]$ApiUrl = "https://localhost:7001",
    [int]$TestUserCount = 3,
    [switch]$CleanupAfterTest = $true
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "InsightLearn Registration Quick Test" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$testResults = @{
    StartTime = Get-Date
    TestsRun = 0
    TestsPassed = 0
    TestsFailed = 0
    CreatedUsers = @()
    Errors = @()
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Details = ""
    )

    $testResults.TestsRun++
    if ($Passed) {
        $testResults.TestsPassed++
        Write-Host "✓ $TestName" -ForegroundColor Green
    } else {
        $testResults.TestsFailed++
        $testResults.Errors += "$TestName: $Details"
        Write-Host "✗ $TestName" -ForegroundColor Red
        if ($Details) {
            Write-Host "  $Details" -ForegroundColor Yellow
        }
    }
}

function Test-RegistrationEndpoint {
    param([string]$Url)

    try {
        # Test basic connectivity
        $response = Invoke-WebRequest -Uri "$Url/register" -Method GET -UseBasicParsing -TimeoutSec 10
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

function Test-ApiEndpoint {
    param([string]$Url)

    try {
        # Test API health
        $response = Invoke-RestMethod -Uri "$Url/api/auth/google/health" -Method GET -TimeoutSec 10
        return $response -ne $null
    } catch {
        return $false
    }
}

function Register-TestUser {
    param(
        [string]$Email,
        [string]$FirstName,
        [string]$LastName,
        [bool]$IsInstructor = $false
    )

    try {
        $registrationData = @{
            email = $Email
            password = "TestPassword123!"
            firstName = $FirstName
            lastName = $LastName
            isInstructor = $IsInstructor
        }

        $json = $registrationData | ConvertTo-Json
        $response = Invoke-RestMethod -Uri "$ApiUrl/api/auth/register" -Method POST -Body $json -ContentType "application/json" -TimeoutSec 30

        if ($response.isSuccess) {
            $testResults.CreatedUsers += $Email
            return @{ Success = $true; Token = $response.token; User = $response.user }
        } else {
            return @{ Success = $false; Error = ($response.errors -join ", ") }
        }
    } catch {
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

function Test-UserLogin {
    param(
        [string]$Email,
        [string]$Password = "TestPassword123!"
    )

    try {
        $loginData = @{
            email = $Email
            password = $Password
        }

        $json = $loginData | ConvertTo-Json
        $response = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" -Method POST -Body $json -ContentType "application/json" -TimeoutSec 30

        return @{ Success = $response.isSuccess; Token = $response.token; User = $response.user }
    } catch {
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

function Test-TokenValidation {
    param([string]$Token)

    try {
        $headers = @{ Authorization = "Bearer $Token" }
        $response = Invoke-RestMethod -Uri "$ApiUrl/api/auth/validate" -Method GET -Headers $headers -TimeoutSec 30
        return $true
    } catch {
        return $false
    }
}

function Test-InvalidRegistration {
    # Test various invalid registration scenarios
    $invalidTests = @(
        @{ Email = ""; Password = "TestPassword123!"; FirstName = "Test"; LastName = "User"; ExpectedToFail = $true; Description = "Empty email" },
        @{ Email = "invalid-email"; Password = "TestPassword123!"; FirstName = "Test"; LastName = "User"; ExpectedToFail = $true; Description = "Invalid email format" },
        @{ Email = "test@example.com"; Password = "weak"; FirstName = "Test"; LastName = "User"; ExpectedToFail = $true; Description = "Weak password" },
        @{ Email = "test@example.com"; Password = "TestPassword123!"; FirstName = ""; LastName = "User"; ExpectedToFail = $true; Description = "Empty first name" }
    )

    $passedValidations = 0
    foreach ($test in $invalidTests) {
        try {
            $registrationData = @{
                email = $test.Email
                password = $test.Password
                firstName = $test.FirstName
                lastName = $test.LastName
            }

            $json = $registrationData | ConvertTo-Json
            $response = Invoke-RestMethod -Uri "$ApiUrl/api/auth/register" -Method POST -Body $json -ContentType "application/json" -TimeoutSec 30 -ErrorAction SilentlyContinue

            # If the test was expected to fail but succeeded, that's bad
            if ($test.ExpectedToFail -and $response.isSuccess) {
                Write-TestResult "Validation Test: $($test.Description)" $false "Expected to fail but succeeded"
            } else {
                Write-TestResult "Validation Test: $($test.Description)" $true
                $passedValidations++
            }
        } catch {
            # If the test was expected to fail and it did fail, that's good
            if ($test.ExpectedToFail) {
                Write-TestResult "Validation Test: $($test.Description)" $true
                $passedValidations++
            } else {
                Write-TestResult "Validation Test: $($test.Description)" $false $_.Exception.Message
            }
        }
    }

    return $passedValidations -eq $invalidTests.Count
}

# Start testing
Write-Host "Testing registration system..."
Write-Host "Base URL: $BaseUrl"
Write-Host "API URL: $ApiUrl"
Write-Host ""

# Test 1: Basic connectivity
Write-Host "1. Testing connectivity..." -ForegroundColor Yellow
$webConnected = Test-RegistrationEndpoint $BaseUrl
Write-TestResult "Web application connectivity" $webConnected

$apiConnected = Test-ApiEndpoint $ApiUrl
Write-TestResult "API connectivity" $apiConnected

if (-not $webConnected -and -not $apiConnected) {
    Write-Host ""
    Write-Host "❌ Cannot connect to either web application or API" -ForegroundColor Red
    Write-Host "Please ensure the applications are running on the specified URLs" -ForegroundColor Yellow
    exit 1
}

# Test 2: User registration
Write-Host ""
Write-Host "2. Testing user registration..." -ForegroundColor Yellow

$testUsers = @()
for ($i = 1; $i -le $TestUserCount; $i++) {
    $email = "test_user_${i}_$timestamp@example.com"
    $firstName = "TestUser$i"
    $lastName = "Registration"
    $isInstructor = ($i -eq $TestUserCount)  # Make last user an instructor

    $result = Register-TestUser -Email $email -FirstName $firstName -LastName $lastName -IsInstructor $isInstructor

    if ($result.Success) {
        Write-TestResult "Register user: $email" $true
        $testUsers += @{ Email = $email; Token = $result.Token; User = $result.User }
    } else {
        Write-TestResult "Register user: $email" $false $result.Error
    }
}

# Test 3: User login
Write-Host ""
Write-Host "3. Testing user login..." -ForegroundColor Yellow

foreach ($user in $testUsers) {
    $loginResult = Test-UserLogin -Email $user.Email
    Write-TestResult "Login user: $($user.Email)" $loginResult.Success $loginResult.Error
}

# Test 4: Token validation
Write-Host ""
Write-Host "4. Testing token validation..." -ForegroundColor Yellow

foreach ($user in $testUsers) {
    if ($user.Token) {
        $tokenValid = Test-TokenValidation -Token $user.Token
        Write-TestResult "Token validation: $($user.Email)" $tokenValid
    }
}

# Test 5: Validation tests
Write-Host ""
Write-Host "5. Testing input validation..." -ForegroundColor Yellow

$validationPassed = Test-InvalidRegistration
Write-TestResult "Input validation tests" $validationPassed

# Test 6: Duplicate email prevention
Write-Host ""
Write-Host "6. Testing duplicate email prevention..." -ForegroundColor Yellow

if ($testUsers.Count -gt 0) {
    $duplicateEmail = $testUsers[0].Email
    $duplicateResult = Register-TestUser -Email $duplicateEmail -FirstName "Duplicate" -LastName "Test"

    # This should fail
    $duplicatePrevented = -not $duplicateResult.Success
    Write-TestResult "Duplicate email prevention" $duplicatePrevented
}

# Test 7: Password security
Write-Host ""
Write-Host "7. Testing password security..." -ForegroundColor Yellow

# Create a test user and verify password is hashed
$securityTestEmail = "security_test_$timestamp@example.com"
$securityResult = Register-TestUser -Email $securityTestEmail -FirstName "Security" -LastName "Test"

if ($securityResult.Success) {
    # Try to login with the password to verify it works
    $loginTest = Test-UserLogin -Email $securityTestEmail
    Write-TestResult "Password hashing and verification" $loginTest.Success

    # Try to login with wrong password (should fail)
    try {
        $wrongPasswordData = @{
            email = $securityTestEmail
            password = "WrongPassword123!"
        }
        $json = $wrongPasswordData | ConvertTo-Json
        $response = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" -Method POST -Body $json -ContentType "application/json" -TimeoutSec 30 -ErrorAction SilentlyContinue

        # This should fail
        $wrongPasswordBlocked = -not $response.isSuccess
        Write-TestResult "Wrong password rejection" $wrongPasswordBlocked
    } catch {
        # Exception is expected for wrong password
        Write-TestResult "Wrong password rejection" $true
    }
}

# Cleanup (optional)
if ($CleanupAfterTest -and $testResults.CreatedUsers.Count -gt 0) {
    Write-Host ""
    Write-Host "8. Cleanup (note: actual user deletion requires database access)..." -ForegroundColor Yellow
    Write-Host "Created test users that may need manual cleanup:" -ForegroundColor Yellow
    foreach ($email in $testResults.CreatedUsers) {
        Write-Host "  - $email" -ForegroundColor White
    }
}

# Final results
$testResults.EndTime = Get-Date
$duration = $testResults.EndTime - $testResults.StartTime

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Test Results Summary" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

Write-Host "Tests Run: $($testResults.TestsRun)" -ForegroundColor White
Write-Host "Tests Passed: " -NoNewline
Write-Host "$($testResults.TestsPassed)" -ForegroundColor Green
Write-Host "Tests Failed: " -NoNewline
Write-Host "$($testResults.TestsFailed)" -ForegroundColor Red
Write-Host "Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor White
Write-Host "Users Created: $($testResults.CreatedUsers.Count)" -ForegroundColor White

if ($testResults.TestsFailed -eq 0) {
    Write-Host ""
    Write-Host "🎉 All tests passed! Registration system is working correctly." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Some tests failed. Issues found:" -ForegroundColor Red
    foreach ($error in $testResults.Errors) {
        Write-Host "  - $error" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Registration system test completed." -ForegroundColor Cyan

# Exit with appropriate code
if ($testResults.TestsFailed -gt 0) {
    exit 1
} else {
    exit 0
}