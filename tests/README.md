# InsightLearn Registration System Test Suite

This comprehensive test suite validates the InsightLearn registration system functionality, security, and performance. The tests cover all aspects of user registration from UI validation to database verification.

## Test Suite Overview

### 🧪 Test Categories

1. **Unit Tests** - Test individual components and services
2. **Integration Tests** - Test API endpoints and complete workflows
3. **Database Tests** - Verify database connectivity and data integrity
4. **Security Tests** - Test password encryption, injection prevention, and security measures
5. **Form Validation Tests** - Test UI form validation and user experience
6. **OAuth Tests** - Test Google OAuth registration flow
7. **Performance Tests** - Test system performance under load

### 📁 Project Structure

```
tests/
├── Unit/                          # Unit tests
│   └── AuthServiceTests.cs        # Authentication service tests
├── Integration/                   # Integration tests
│   └── RegistrationApiTests.cs    # API endpoint tests
├── Database/                      # Database tests
│   └── DatabaseConnectionTests.cs # Database connectivity and verification
├── Security/                      # Security tests
│   └── SecurityTests.cs           # Security and vulnerability tests
├── UI/                           # UI validation tests
│   └── RegistrationFormValidationTests.cs # Form validation tests
├── OAuth/                        # OAuth tests
│   └── GoogleOAuthRegistrationTests.cs # Google OAuth tests
├── Data/                         # Test data generation
│   └── TestDataGenerator.cs       # Automated test data creation
├── Scripts/                      # PowerShell test scripts
│   ├── RunAllTests.ps1            # Main test runner
│   ├── QuickRegistrationTest.ps1  # Quick HTTP-based tests
│   └── VerifyRegistrationDatabase.ps1 # Database verification script
├── InsightLearn.Tests.csproj     # Test project file
├── appsettings.Test.json         # Test configuration
└── README.md                     # This file
```

## 🚀 Quick Start

### Prerequisites

1. **SQL Server** running on `localhost,1433`
2. **InsightLearn database** with credentials:
   - Server: `localhost,1433`
   - Database: `InsightLearnDb`
   - User: `sa`
   - Password: `InsightLearn123@#`
3. **.NET 8 SDK** installed
4. **PowerShell** (for scripts)

### Running Tests

#### Option 1: Quick HTTP-based Test (Recommended for initial verification)

```powershell
cd C:\kubernetes\tests
.\Scripts\QuickRegistrationTest.ps1
```

This script:
- Tests registration endpoints via HTTP calls
- Creates and validates test users
- Tests login and token validation
- Verifies security measures
- Provides immediate feedback

#### Option 2: Comprehensive Test Suite

```powershell
cd C:\kubernetes\tests
.\Scripts\RunAllTests.ps1
```

This runs the complete test suite and generates an HTML report.

#### Option 3: Database Verification Only

```powershell
cd C:\kubernetes\tests
.\Scripts\VerifyRegistrationDatabase.ps1
```

This verifies database connectivity and table structure.

#### Option 4: .NET Test Runner

```bash
cd C:\kubernetes\tests
dotnet test --logger "console;verbosity=detailed"
```

## 📋 Test Details

### Unit Tests (`AuthServiceTests.cs`)

Tests the core authentication service functionality:

- ✅ User registration with valid data
- ✅ Duplicate email prevention
- ✅ Password validation and hashing
- ✅ User login with valid/invalid credentials
- ✅ JWT token generation and validation
- ✅ User lockout protection
- ✅ Google OAuth integration
- ✅ Token refresh functionality

**Sample Test:**
```csharp
[Fact]
public async Task RegisterAsync_ValidInput_ReturnsSuccessResult()
{
    // Tests successful user registration with proper data
}
```

### Integration Tests (`RegistrationApiTests.cs`)

Tests complete API workflows:

- ✅ POST `/api/auth/register` - User registration
- ✅ POST `/api/auth/login` - User authentication
- ✅ GET `/api/auth/me` - Current user info
- ✅ GET `/api/auth/validate` - Token validation
- ✅ POST `/api/auth/refresh` - Token refresh
- ✅ Duplicate email handling
- ✅ Invalid data validation
- ✅ Role assignment (Student/Instructor)

### Database Tests (`DatabaseConnectionTests.cs`)

Verifies database functionality:

- ✅ Database connectivity
- ✅ Table structure validation
- ✅ User creation and retrieval
- ✅ Password hashing verification
- ✅ Role assignment verification
- ✅ Data integrity checks
- ✅ Performance testing

### Security Tests (`SecurityTests.cs`)

Tests security measures:

- ✅ Password hashing (no plain text storage)
- ✅ SQL injection prevention
- ✅ XSS prevention
- ✅ Password complexity enforcement
- ✅ Rate limiting simulation
- ✅ JWT token security
- ✅ Sensitive data exposure prevention
- ✅ Path traversal prevention

**Security Test Examples:**
```csharp
[Theory]
[InlineData("' OR '1'='1")]  // SQL injection attempt
[InlineData("'; DROP TABLE AspNetUsers; --")]  // SQL injection attempt
public async Task SQLInjection_InEmailField_DoesNotCompromiseSecurity(string maliciousEmail)
```

### Form Validation Tests (`RegistrationFormValidationTests.cs`)

Tests UI form validation:

- ✅ Required field validation
- ✅ Email format validation
- ✅ Password strength requirements
- ✅ Terms and conditions checkbox
- ✅ Multi-step form navigation
- ✅ Responsive design elements
- ✅ Accessibility features
- ✅ Google OAuth button presence

### OAuth Tests (`GoogleOAuthRegistrationTests.cs`)

Tests Google OAuth integration:

- ✅ OAuth configuration status
- ✅ OAuth flow initiation
- ✅ Callback handling
- ✅ Error handling
- ✅ Security measures (CSRF protection)
- ✅ Token validation
- ✅ User creation from OAuth

## 🎯 Test Scenarios Covered

### Registration Flow Testing

1. **Valid Registration**
   - Standard student registration
   - Instructor registration
   - Google OAuth registration

2. **Invalid Registration**
   - Empty/invalid email addresses
   - Weak passwords
   - Missing required fields
   - Duplicate email addresses

3. **Security Validation**
   - SQL injection attempts
   - XSS attack prevention
   - Path traversal attempts
   - Password security requirements

### Database Verification

1. **Connection Testing**
   - Basic connectivity
   - Credentials validation
   - Network accessibility

2. **Schema Validation**
   - Required tables exist
   - Column structure verification
   - Index validation
   - Constraint verification

3. **Data Integrity**
   - User creation/retrieval
   - Password hashing
   - Role assignment
   - Audit trail functionality

## 📊 Test Reports

### HTML Report Generation

The comprehensive test runner generates detailed HTML reports:

```
tests/TestResults/TestReport_YYYYMMDD_HHMMSS.html
```

**Report includes:**
- Overall test status and metrics
- Individual test category results
- Detailed pass/fail information
- Performance metrics
- Error details and debugging information

### Console Output

All test scripts provide real-time console feedback:
- ✅ Passed tests (Green)
- ❌ Failed tests (Red)
- ⚠️ Warnings (Yellow)
- ℹ️ Information (Blue)

## 🔧 Configuration

### Test Configuration (`appsettings.Test.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=InsightLearnDb;User Id=sa;Password=InsightLearn123@#;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "SecretKey": "InsightLearnTestSecretKeyThatIsAtLeast32CharactersLongForTestingPurposes",
    "Issuer": "InsightLearn.Tests",
    "Audience": "InsightLearn.TestUsers",
    "ExpirationDays": 7
  }
}
```

### Customizing Tests

#### Running Specific Test Categories

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only security tests
dotnet test --filter "Category=Security"

# Run only database tests
dotnet test --filter "Category=Database"
```

#### Customizing PowerShell Scripts

```powershell
# Custom database connection
.\Scripts\VerifyRegistrationDatabase.ps1 -Server "myserver" -Database "mydb" -Username "myuser" -Password "mypass"

# Custom test user count
.\Scripts\QuickRegistrationTest.ps1 -TestUserCount 5

# Skip cleanup after tests
.\Scripts\QuickRegistrationTest.ps1 -CleanupAfterTest:$false
```

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Failed**
   ```
   Solution: Verify SQL Server is running and credentials are correct
   ```

2. **Test Project Build Failed**
   ```
   Solution: Run 'dotnet restore' to install NuGet packages
   ```

3. **Web Application Not Accessible**
   ```
   Solution: Ensure the InsightLearn web application is running on https://localhost:7003
   ```

4. **API Not Accessible**
   ```
   Solution: Ensure the InsightLearn API is running on https://localhost:7001
   ```

### Debugging Tests

1. **Enable Detailed Logging**
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```

2. **Run Single Test**
   ```bash
   dotnet test --filter "TestMethodName"
   ```

3. **Check Test Output Files**
   ```
   tests/TestResults/TestLog_YYYYMMDD_HHMMSS.txt
   ```

## 📈 Performance Benchmarks

### Expected Performance

- **Database Connection**: < 1 second
- **User Registration**: < 2 seconds
- **User Login**: < 1 second
- **Token Validation**: < 500ms
- **Complete Test Suite**: < 5 minutes

### Load Testing

The test suite can simulate concurrent user registrations:

```csharp
var performanceData = await testDataGenerator.GeneratePerformanceTestData(100);
// Creates 100 test users for load testing
```

## 🛡️ Security Testing Details

### Injection Attack Prevention

The security tests verify protection against:
- SQL Injection
- XSS (Cross-Site Scripting)
- Path Traversal
- Command Injection

### Password Security

Tests verify:
- Minimum length requirements (8+ characters)
- Complexity requirements (uppercase, lowercase, numbers)
- Hash-based storage (no plain text)
- Secure password comparison

### Authentication Security

Tests verify:
- JWT token structure and signing
- Token expiration handling
- Session management
- CSRF protection in OAuth flows

## 📚 Best Practices

### Writing New Tests

1. **Follow AAA Pattern** (Arrange, Act, Assert)
2. **Use descriptive test names** that explain the scenario
3. **Test both positive and negative cases**
4. **Use realistic test data**
5. **Clean up test data** after execution
6. **Mock external dependencies** appropriately

### Test Data Management

1. **Use unique identifiers** (timestamps, GUIDs) for test data
2. **Clean up after tests** to avoid database pollution
3. **Use separate test database** when possible
4. **Generate realistic test data** with Bogus library

## 🤝 Contributing

When adding new tests:

1. Follow the existing project structure
2. Add tests to appropriate category folders
3. Update this README with new test descriptions
4. Include both positive and negative test cases
5. Ensure tests are independent and can run in any order

## 📞 Support

For issues with the test suite:

1. Check the troubleshooting section above
2. Review test output logs in `tests/TestResults/`
3. Verify application configuration matches test expectations
4. Ensure all prerequisites are met

---

**Last Updated**: 2024-12-29
**Test Suite Version**: 1.0
**Compatible with**: InsightLearn v1.0+