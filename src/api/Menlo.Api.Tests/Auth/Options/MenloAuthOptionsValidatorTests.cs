using Menlo.Api.Auth.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Shouldly;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Menlo.Api.Tests.Auth.Options;

/// <summary>
/// Tests for MenloAuthOptionsValidator.
/// </summary>
public sealed class MenloAuthOptionsValidatorTests
{
    private readonly MenloAuthOptionsValidator _validator;

    public MenloAuthOptionsValidatorTests()
    {
        _validator = new MenloAuthOptionsValidator();
    }

    [Fact]
    public void GivenValidOptionsWithClientSecret_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenValidOptionsWithCertificate_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientCertificates =
            [
                new CertificateDescription
                {
                    SourceType = CertificateSource.Path, CertificateDiskPath = "/path/to/cert.pfx"
                }
            ],
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenValidOptionsWithABase64Certificate_WhenValidating()
    {
        ConfigurationBuilder configBuilder = new();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
        {
            [$"{MenloAuthOptions.SectionName}:ClientId"] = "client-guid",
            [$"{MenloAuthOptions.SectionName}:ClientCertificates:0:SourceType"] = "Base64Encoded",
            [$"{MenloAuthOptions.SectionName}:ClientCertificates:0:CertificateBase64Value"] = "base64-encoded-cert",
            [$"{MenloAuthOptions.SectionName}:CookieDomain"] = "menlo.example.com",
            [$"{MenloAuthOptions.SectionName}:Instance"] = "https://login.microsoftonline.com",
            [$"{MenloAuthOptions.SectionName}:TenantId"] = "tenant-guid"
        }!);
        MenloAuthOptions? options = configBuilder.Build().GetSection(MenloAuthOptions.SectionName).Get<MenloAuthOptions>();
        ItShouldDeserializeCorrectly(options);

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenValidOptionsWithBothSecretAndCertificate_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            ClientCertificates =
            [
                new CertificateDescription
                {
                    SourceType = CertificateSource.Path, CertificateDiskPath = "/path/to/cert.pfx"
                }
            ],
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenEmptyInstance_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "Instance");
    }

    [Fact]
    public void GivenWhitespaceInstance_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "   ",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "Instance");
    }

    [Fact]
    public void GivenNullInstance_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = null!,
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "Instance");
    }

    [Fact]
    public void GivenEmptyTenantId_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "TenantId");
    }

    [Fact]
    public void GivenWhitespaceTenantId_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "   ",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "TenantId");
    }

    [Fact]
    public void GivenNullTenantId_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = null!,
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "TenantId");
    }

    [Fact]
    public void GivenEmptyClientId_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientId");
    }

    [Fact]
    public void GivenWhitespaceClientId_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "   ",
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientId");
    }

    [Fact]
    public void GivenNullClientId_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = null!,
            ClientSecret = "secret-value",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientId");
    }

    [Fact]
    public void GivenEmptyClientSecret_WhenValidating_AndNoCertificates()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenWhitespaceClientSecret_WhenValidating_AndNoCertificates()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "   ",
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenNullClientSecret_WhenValidating_AndNoCertificates()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = null!,
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenEmptyClientSecret_WhenValidating_AndHasCertificates()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "",
            ClientCertificates =
            [
                new CertificateDescription
                {
                    SourceType = CertificateSource.Path, CertificateDiskPath = "/path/to/cert.pfx"
                }
            ],
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenEmptyCertificates_WhenValidating_AndNoClientSecret()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientCertificates = [],
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenNullCertificates_WhenValidating_AndNoClientSecret()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientCertificates = null,
            CookieDomain = "menlo.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenEmptyCookieDomain_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = ""
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "CookieDomain");
    }

    [Fact]
    public void GivenWhitespaceCookieDomain_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = "   "
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "CookieDomain");
    }

    [Fact]
    public void GivenNullCookieDomain_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value",
            CookieDomain = null!
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "CookieDomain");
    }

    [Fact]
    public void GivenMultipleInvalidFields_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "",
            TenantId = "",
            ClientId = "",
            ClientSecret = "",
            CookieDomain = ""
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldHaveMultipleFailures(result, 5);
    }

    // Assertion Helpers
    private static void ItShouldSucceed(ValidateOptionsResult result)
    {
        result.Succeeded.ShouldBeTrue();
    }

    private static void ItShouldFail(ValidateOptionsResult result)
    {
        result.Failed.ShouldBeTrue();
    }

    private static void ItShouldContainFailure(ValidateOptionsResult result, string expectedFieldName)
    {
        result.Failures.ShouldNotBeNull();
        result.Failures.ShouldContain(failure => failure.Contains(expectedFieldName));
    }

    private static void ItShouldHaveMultipleFailures(ValidateOptionsResult result, int expectedCount)
    {
        result.Failures.ShouldNotBeNull();
        result.Failures.Count().ShouldBe(expectedCount);
    }

    private static void ItShouldDeserializeCorrectly([NotNull] MenloAuthOptions? options)
    {
        options.ShouldNotBeNull();
        options.ClientCertificates.ShouldNotBeEmpty();
    }
}
