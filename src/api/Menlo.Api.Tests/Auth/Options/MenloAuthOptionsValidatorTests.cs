using Menlo.Api.Auth.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Shouldly;
using System.Diagnostics.CodeAnalysis;

namespace Menlo.Api.Tests.Auth.Options;

/// <summary>
/// Tests for MenloAuthOptionsValidator.
/// </summary>
public sealed class MenloAuthOptionsValidatorTests
{
    private readonly MenloAuthOptionsValidator _validator = new();

    [Fact]
    public void GivenValidOptionsWithClientSecret_WhenValidating()
    {
        MenloAuthOptions options = CreateValidOptions();

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenValidOptionsWithCertificate_WhenValidating()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientSecret = null;
        options.ClientCertificates =
        [
            new CertificateDescription
            {
                SourceType = CertificateSource.Path,
                CertificateDiskPath = "/path/to/cert.pfx"
            }
        ];

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenValidOptionsWithABase64Certificate_WhenValidating()
    {
        ConfigurationBuilder configBuilder = new();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{MenloAuthOptions.SectionName}:ClientId"] = "client-guid",
            [$"{MenloAuthOptions.SectionName}:ClientCertificates:0:SourceType"] = "Base64Encoded",
            [$"{MenloAuthOptions.SectionName}:ClientCertificates:0:CertificateBase64Value"] = "base64-encoded-cert",
            [$"{MenloAuthOptions.SectionName}:Instance"] = "https://login.microsoftonline.com",
            [$"{MenloAuthOptions.SectionName}:TenantId"] = "tenant-guid"
        });

        MenloAuthOptions? options = configBuilder.Build()
            .GetSection(MenloAuthOptions.SectionName)
            .Get<MenloAuthOptions>();

        ItShouldDeserializeCorrectly(options);

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenValidOptionsWithBothSecretAndCertificate_WhenValidating()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientCertificates =
        [
            new CertificateDescription
            {
                SourceType = CertificateSource.Path,
                CertificateDiskPath = "/path/to/cert.pfx"
            }
        ];

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidInstance_WhenValidating(string value)
    {
        MenloAuthOptions options = CreateValidOptions();
        options.Instance = value;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "Instance");
    }

    [Fact]
    public void GivenNullInstance_WhenValidating()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.Instance = null!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "Instance");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidTenantId_WhenValidating(string value)
    {
        MenloAuthOptions options = CreateValidOptions();
        options.TenantId = value;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "TenantId");
    }

    [Fact]
    public void GivenNullTenantId_WhenValidating()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.TenantId = null!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "TenantId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidClientId_WhenValidating(string value)
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientId = value;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientId");
    }

    [Fact]
    public void GivenNullClientId_WhenValidating()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientId = null!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidClientSecret_WhenValidating_AndNoCertificates(string value)
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientSecret = value;
        options.ClientCertificates = null;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenNullClientSecret_WhenValidating_AndNoCertificates()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientSecret = null!;
        options.ClientCertificates = null;

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenEmptyClientSecret_WhenValidating_AndHasCertificates()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientSecret = string.Empty;
        options.ClientCertificates =
        [
            new CertificateDescription
            {
                SourceType = CertificateSource.Path,
                CertificateDiskPath = "/path/to/cert.pfx"
            }
        ];

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenEmptyCertificates_WhenValidating_AndNoClientSecret()
    {
        MenloAuthOptions options = CreateValidOptions();
        options.ClientSecret = null;
        options.ClientCertificates = [];

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldContainFailure(result, "ClientSecret or ClientCertificates");
    }

    [Fact]
    public void GivenMultipleInvalidFields_WhenValidating()
    {
        MenloAuthOptions options = new()
        {
            Instance = "",
            TenantId = "",
            ClientId = "",
            ClientSecret = ""
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        ItShouldFail(result);
        ItShouldHaveMultipleFailures(result, 4);
    }

    private static MenloAuthOptions CreateValidOptions() =>
        new()
        {
            Instance = "https://login.microsoftonline.com",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ClientSecret = "secret-value"
        };

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
