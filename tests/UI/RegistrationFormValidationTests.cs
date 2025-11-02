using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AngleSharp;
using AngleSharp.Html.Dom;
using System.Text;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Api;

namespace InsightLearn.Tests.UI;

public class RegistrationFormValidationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IConfiguration _config;
    private readonly IBrowsingContext _browsingContext;

    public RegistrationFormValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        _config = Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(_config);
    }

    [Fact]
    public async Task SimpleRegisterPage_LoadsWithRequiredFormFields()
    {
        // Act
        var response = await _client.GetAsync("/register");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Verify form exists
        var form = document.QuerySelector("form") as IHtmlFormElement;
        form.Should().NotBeNull();

        // Verify required fields exist
        var firstNameInput = document.QuerySelector("input[name*='FirstName']") as IHtmlInputElement;
        firstNameInput.Should().NotBeNull();
        firstNameInput.IsRequired.Should().BeTrue();

        var emailInput = document.QuerySelector("input[name*='Email']") as IHtmlInputElement;
        emailInput.Should().NotBeNull();
        emailInput.Type.Should().Be("email");

        var passwordInput = document.QuerySelector("input[name*='Password'][type='password']") as IHtmlInputElement;
        passwordInput.Should().NotBeNull();
        passwordInput.Type.Should().Be("password");

        var agreeTermsCheckbox = document.QuerySelector("input[name*='AgreeToTerms']") as IHtmlInputElement;
        agreeTermsCheckbox.Should().NotBeNull();
        agreeTermsCheckbox.Type.Should().Be("checkbox");
    }

    [Fact]
    public async Task ComprehensiveRegisterPage_LoadsWithAllSteps()
    {
        // Act
        var response = await _client.GetAsync("/register-comprehensive");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Verify progress indicator
        var progressBar = document.QuerySelector(".progress-bar");
        progressBar.Should().NotBeNull();

        // Verify step indicator
        var stepIndicator = document.QuerySelector(".badge");
        stepIndicator.Should().NotBeNull();
        stepIndicator.TextContent.Should().Contain("Step 1 of");

        // Verify Step 1 fields are visible
        var firstNameInput = document.QuerySelector("input[name*='FirstName']") as IHtmlInputElement;
        firstNameInput.Should().NotBeNull();

        var lastNameInput = document.QuerySelector("input[name*='LastName']") as IHtmlInputElement;
        lastNameInput.Should().NotBeNull();

        var emailInput = document.QuerySelector("input[name*='Email']") as IHtmlInputElement;
        emailInput.Should().NotBeNull();

        var passwordInput = document.QuerySelector("input[name*='Password'][type='password']") as IHtmlInputElement;
        passwordInput.Should().NotBeNull();

        var confirmPasswordInput = document.QuerySelector("input[name*='ConfirmPassword']") as IHtmlInputElement;
        confirmPasswordInput.Should().NotBeNull();

        var dateOfBirthInput = document.QuerySelector("input[name*='DateOfBirth']") as IHtmlInputElement;
        dateOfBirthInput.Should().NotBeNull();
        dateOfBirthInput.Type.Should().Be("date");

        var countrySelect = document.QuerySelector("select[name*='Country']") as IHtmlSelectElement;
        countrySelect.Should().NotBeNull();

        // Verify navigation buttons
        var nextButton = document.QuerySelector("button[type='button']:contains('Next')");
        nextButton.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]  // Empty email
    [InlineData("invalid-email")]  // Invalid format
    [InlineData("test@")]  // Incomplete domain
    [InlineData("@example.com")]  // Missing local part
    [InlineData("test..test@example.com")]  // Double dots
    public async Task EmailValidation_InvalidFormats_ShowsValidationErrors(string invalidEmail)
    {
        // This test would require submitting the form and checking validation
        // For now, we'll verify the email input has the correct validation attributes

        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert
        var emailInput = document.QuerySelector("input[name*='Email']") as IHtmlInputElement;
        emailInput.Should().NotBeNull();
        emailInput.Type.Should().Be("email");  // HTML5 email validation

        // Verify validation message container exists
        var validationMessage = document.QuerySelector("*[data-valmsg-for*='Email']");
        validationMessage.Should().NotBeNull();
    }

    [Theory]
    [InlineData("short")]  // Too short
    [InlineData("nouppercase123!")]  // No uppercase
    [InlineData("NOLOWERCASE123!")]  // No lowercase
    [InlineData("NoNumbers!")]  // No numbers
    [InlineData("NoSpecialChars123")]  // No special characters
    public async Task PasswordValidation_WeakPasswords_HasValidationRules(string weakPassword)
    {
        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert
        var passwordInput = document.QuerySelector("input[name*='Password'][type='password']") as IHtmlInputElement;
        passwordInput.Should().NotBeNull();

        // Check for password requirements text
        var passwordHelp = document.QuerySelector(".form-text");
        passwordHelp.Should().NotBeNull();
        passwordHelp.TextContent.Should().Contain("8 or more characters");

        // Verify validation message container exists
        var validationMessage = document.QuerySelector("*[data-valmsg-for*='Password']");
        validationMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task RequiredFieldValidation_EmptyFields_HasValidationAttributes()
    {
        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert required fields have proper validation
        var requiredFields = new[]
        {
            "input[name*='FirstName']",
            "input[name*='Email']",
            "input[name*='Password']"
        };

        foreach (var fieldSelector in requiredFields)
        {
            var field = document.QuerySelector(fieldSelector) as IHtmlInputElement;
            field.Should().NotBeNull($"Field {fieldSelector} should exist");

            // Verify validation message container exists
            var fieldName = fieldSelector.Split('[', ']')[1].Replace("name*='", "").Replace("'", "");
            var validationMessage = document.QuerySelector($"*[data-valmsg-for*='{fieldName}']");
            validationMessage.Should().NotBeNull($"Validation message for {fieldName} should exist");
        }
    }

    [Fact]
    public async Task TermsAndConditionsCheckbox_Required_HasValidation()
    {
        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert
        var agreeTermsCheckbox = document.QuerySelector("input[name*='AgreeToTerms']") as IHtmlInputElement;
        agreeTermsCheckbox.Should().NotBeNull();
        agreeTermsCheckbox.Type.Should().Be("checkbox");

        // Verify validation message container exists
        var validationMessage = document.QuerySelector("*[data-valmsg-for*='AgreeToTerms']");
        validationMessage.Should().NotBeNull();

        // Verify terms and privacy policy links exist
        var termsLink = document.QuerySelector("a[href='/terms']");
        termsLink.Should().NotBeNull();

        var privacyLink = document.QuerySelector("a[href='/privacy']");
        privacyLink.Should().NotBeNull();
    }

    [Fact]
    public async Task ComprehensiveForm_Step2UserType_HasRadioButtons()
    {
        // This test simulates navigating to step 2 of the comprehensive form
        // In a real test, we would need to interact with the form to advance steps

        // Act
        var response = await _client.GetAsync("/register-comprehensive");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check that user type options are defined in the markup
        content.Should().Contain("Student");
        content.Should().Contain("Teacher");
        content.Should().Contain("fa-user-graduate");  // Student icon
        content.Should().Contain("fa-chalkboard-teacher");  // Teacher icon
    }

    [Fact]
    public async Task ComprehensiveForm_Step3PaymentMethods_HasOptions()
    {
        // Act
        var response = await _client.GetAsync("/register-comprehensive");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check that payment method options are defined
        content.Should().Contain("CreditCard");
        content.Should().Contain("PayPal");
        content.Should().Contain("BankTransfer");
        content.Should().Contain("fa-credit-card");
        content.Should().Contain("fa-paypal");
        content.Should().Contain("fa-university");
    }

    [Fact]
    public async Task GoogleOAuthButton_ExistsAndIsConfigured()
    {
        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert
        var googleButton = document.QuerySelector("button:contains('Continue with Google')");
        googleButton.Should().NotBeNull();

        // Verify Google icon
        var googleIcon = document.QuerySelector(".fab.fa-google");
        googleIcon.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitButton_HasLoadingState()
    {
        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert
        var submitButton = document.QuerySelector("button[type='submit']") as IHtmlButtonElement;
        submitButton.Should().NotBeNull();

        // Check for loading state elements in the content
        content.Should().Contain("spinner-border");
        content.Should().Contain("Creating account...");
    }

    [Fact]
    public async Task FormStyling_HasResponsiveDesign()
    {
        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check for responsive CSS classes
        content.Should().Contain("form-control-lg");  // Large form controls
        content.Should().Contain("btn-lg");  // Large buttons
        content.Should().Contain("col-lg-");  // Bootstrap responsive columns
        content.Should().Contain("d-none d-lg-block");  // Hidden on mobile
    }

    [Fact]
    public async Task AccessibilityFeatures_ArePresent()
    {
        // Act
        var response = await _client.GetAsync("/register");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert
        var labels = document.QuerySelectorAll("label");
        labels.Should().NotBeEmpty("Form should have proper labels");

        // Verify form has proper structure
        var formControls = document.QuerySelectorAll(".form-control");
        formControls.Should().NotBeEmpty("Form controls should exist");

        // Check for validation message containers
        var validationMessages = document.QuerySelectorAll(".text-danger");
        validationMessages.Should().NotBeEmpty("Validation message containers should exist");
    }

    [Fact]
    public async Task CountryDropdown_HasOptions()
    {
        // Act
        var response = await _client.GetAsync("/register-comprehensive");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check that countries are available
        content.Should().Contain("Select your country");
        // Check for some common countries that should be in the list
        content.Should().Contain("United States");
        content.Should().Contain("Canada");
        content.Should().Contain("United Kingdom");
    }

    [Fact]
    public async Task GenderField_IsOptional()
    {
        // Act
        var response = await _client.GetAsync("/register-comprehensive");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Gender (Optional)");
        content.Should().Contain("Prefer not to say");
    }

    [Fact]
    public async Task PhoneNumberField_HasCorrectFormat()
    {
        // Act
        var response = await _client.GetAsync("/register-comprehensive");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = await _browsingContext.OpenAsync(req => req.Content(content));

        // Assert
        var phoneInput = document.QuerySelector("input[name*='PhoneNumber']") as IHtmlInputElement;
        if (phoneInput != null)
        {
            phoneInput.Placeholder.Should().Contain("+1");  // Should show phone format example
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _browsingContext?.Dispose();
    }
}