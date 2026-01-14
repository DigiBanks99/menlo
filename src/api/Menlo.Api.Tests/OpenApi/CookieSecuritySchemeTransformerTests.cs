using Menlo.Api.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NSubstitute;
using Shouldly;

namespace Menlo.Api.Tests.OpenApi;

/// <summary>
/// Tests for CookieSecuritySchemeTransformer.
/// </summary>
public sealed class CookieSecuritySchemeTransformerTests
{
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;
    private readonly CookieSecuritySchemeTransformer _transformer;

    public CookieSecuritySchemeTransformerTests()
    {
        _authenticationSchemeProvider = Substitute.For<IAuthenticationSchemeProvider>();
        _transformer = new CookieSecuritySchemeTransformer(_authenticationSchemeProvider);
    }

    [Fact]
    public async Task GivenCookieAuthenticationSchemeIsRegistered_WhenTransforming()
    {
        AuthenticationScheme[] schemes =
        [
            new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookies",
                typeof(CookieAuthenticationHandler))
        ];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateEmptyDocument();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldAddSecurityScheme(document);
        ItShouldHaveCorrectSecuritySchemeConfiguration(document);
    }

    [Fact]
    public async Task GivenCookieAuthenticationSchemeIsRegistered_AndDocumentHasOperations_WhenTransforming()
    {
        AuthenticationScheme[] schemes =
        [
            new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookies",
                typeof(CookieAuthenticationHandler))
        ];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateDocumentWithOperations();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldAddSecurityRequirementsToAllOperations(document);
    }

    [Fact]
    public async Task GivenCookieAuthenticationSchemeIsNotRegistered_WhenTransforming()
    {
        AuthenticationScheme[] schemes = [new AuthenticationScheme("Bearer", "Bearer", typeof(TestAuthHandler))];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateEmptyDocument();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldNotAddSecurityScheme(document);
    }

    [Fact]
    public async Task GivenNoAuthenticationSchemes_WhenTransforming()
    {
        AuthenticationScheme[] schemes = [];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateEmptyDocument();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldNotAddSecurityScheme(document);
    }

    [Fact]
    public async Task GivenCookieAuthenticationSchemeIsRegistered_AndDocumentHasNoOperations_WhenTransforming()
    {
        AuthenticationScheme[] schemes =
        [
            new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookies",
                typeof(CookieAuthenticationHandler))
        ];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateDocumentWithoutOperations();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldAddSecurityScheme(document);
        ItShouldNotFailWhenNoOperationsExist(document);
    }

    [Fact]
    public async Task GivenCookieAuthenticationSchemeIsRegistered_AndDocumentAlreadyHasComponents_WhenTransforming()
    {
        AuthenticationScheme[] schemes =
        [
            new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookies",
                typeof(CookieAuthenticationHandler))
        ];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateDocumentWithExistingComponents();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldAddSecurityScheme(document);
        ItShouldPreserveExistingComponents(document);
    }

    [Fact]
    public async Task GivenCookieAuthenticationSchemeIsRegistered_AndOperationAlreadyHasSecurity_WhenTransforming()
    {
        AuthenticationScheme[] schemes =
        [
            new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookies",
                typeof(CookieAuthenticationHandler))
        ];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateDocumentWithExistingSecurityRequirements();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldAddCookieSecurityRequirementToExistingSecurityList(document);
    }

    [Fact]
    public async Task GivenMultipleAuthenticationSchemes_IncludingCookie_WhenTransforming()
    {
        AuthenticationScheme[] schemes =
        [
            new AuthenticationScheme("Bearer", "Bearer", typeof(TestAuthHandler)),
            new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookies",
                typeof(CookieAuthenticationHandler)),
            new AuthenticationScheme("ApiKey", "ApiKey", typeof(TestAuthHandler))
        ];
        _authenticationSchemeProvider.GetAllSchemesAsync().Returns(schemes);

        OpenApiDocument document = CreateEmptyDocument();
        OpenApiDocumentTransformerContext context = CreateContext();

        await _transformer.TransformAsync(document, context, CancellationToken.None);

        ItShouldAddSecurityScheme(document);
    }

    // Assertion Helpers

    private static void ItShouldAddSecurityScheme(OpenApiDocument document)
    {
        document.Components.ShouldNotBeNull();
        document.Components.SecuritySchemes.ShouldNotBeNull();
        document.Components.SecuritySchemes.ShouldContainKey(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static void ItShouldHaveCorrectSecuritySchemeConfiguration(OpenApiDocument document)
    {
        document.Components.ShouldNotBeNull();
        document.Components.SecuritySchemes.ShouldNotBeNull();
        
        IOpenApiSecurityScheme scheme =
            document.Components.SecuritySchemes[CookieAuthenticationDefaults.AuthenticationScheme];
        scheme.Type.ShouldBe(SecuritySchemeType.ApiKey);
        scheme.In.ShouldBe(ParameterLocation.Cookie);
        scheme.Name.ShouldBe(".Menlo.Session");
        scheme.Description.ShouldNotBeNullOrWhiteSpace();
        scheme.Description.ShouldContain("Authentication via secure HTTP-only cookie");
    }

    private static void ItShouldAddSecurityRequirementsToAllOperations(OpenApiDocument document)
    {
        foreach (IOpenApiPathItem pathItem in document.Paths.Values)
        {
            if (pathItem.Operations is null)
            {
                continue;
            }

            foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItem.Operations)
            {
                operation.Value.Security.ShouldNotBeNull();
                operation.Value.Security.ShouldNotBeEmpty();
                OpenApiSecurityRequirement? cookieRequirement = operation.Value.Security.FirstOrDefault(req =>
                    req.Any(kvp => kvp.Key.Reference.Id == CookieAuthenticationDefaults.AuthenticationScheme));
                cookieRequirement.ShouldNotBeNull();
            }
        }
    }

    private static void ItShouldNotAddSecurityScheme(OpenApiDocument document)
    {
        if (document.Components?.SecuritySchemes != null)
        {
            document.Components.SecuritySchemes.ShouldNotContainKey(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    private static void ItShouldNotFailWhenNoOperationsExist(OpenApiDocument document)
    {
        // This assertion verifies that the transformer handles empty operations gracefully
        document.Paths.ShouldNotBeNull();
    }

    private static void ItShouldPreserveExistingComponents(OpenApiDocument document)
    {
        document.Components.ShouldNotBeNull();
        document.Components.Schemas.ShouldNotBeNull();
        document.Components.Schemas.ShouldContainKey("ExistingSchema");
    }

    private static void ItShouldAddCookieSecurityRequirementToExistingSecurityList(OpenApiDocument document)
    {
        IOpenApiPathItem pathItem = document.Paths["/test"];
        OpenApiOperation operation = pathItem.Operations![HttpMethod.Get];
        
        operation.Security.ShouldNotBeNull();
        operation.Security.Count.ShouldBe(2); // Existing + Cookie
        
        OpenApiSecurityRequirement? cookieRequirement = operation.Security.FirstOrDefault(req =>
            req.Any(kvp => kvp.Key.Reference.Id == CookieAuthenticationDefaults.AuthenticationScheme));
        cookieRequirement.ShouldNotBeNull();
    }

    // Test Setup Helpers

    private static OpenApiDocument CreateEmptyDocument()
    {
        return new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths()
        };
    }

    private static OpenApiDocument CreateDocumentWithOperations()
    {
        OpenApiDocument document = new()
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths
            {
                ["/test"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            OperationId = "GetTest",
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse { Description = "Success" }
                            }
                        },
                        [HttpMethod.Post] = new OpenApiOperation
                        {
                            OperationId = "PostTest",
                            Responses = new OpenApiResponses
                            {
                                ["201"] = new OpenApiResponse { Description = "Created" }
                            }
                        }
                    }
                },
                ["/another"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Put] = new OpenApiOperation
                        {
                            OperationId = "PutAnother",
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse { Description = "Success" }
                            }
                        }
                    }
                }
            }
        };

        return document;
    }

    private static OpenApiDocument CreateDocumentWithoutOperations()
    {
        return new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths
            {
                ["/test"] = new OpenApiPathItem()
            }
        };
    }

    private static OpenApiDocument CreateDocumentWithExistingComponents()
    {
        return new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["ExistingSchema"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["id"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };
    }

    private static OpenApiDocument CreateDocumentWithExistingSecurityRequirements()
    {
        OpenApiDocument document = new()
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths()
        };

        document.Paths["/test"] = new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new OpenApiOperation
                {
                    OperationId = "GetTest",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Success" }
                    }
                }
            }
        };

        return document;
    }

    private static OpenApiDocumentTransformerContext CreateContext()
    {
        return new OpenApiDocumentTransformerContext
        {
            DocumentName = "v1",
            ApplicationServices = Substitute.For<IServiceProvider>(),
            DescriptionGroups = []
        };
    }

    // Test authentication handler for mocking
    private class TestAuthHandler : IAuthenticationHandler
    {
        public Task InitializeAsync(AuthenticationScheme scheme, Microsoft.AspNetCore.Http.HttpContext context)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public Task ChallengeAsync(AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }

        public Task ForbidAsync(AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }
    }
}
