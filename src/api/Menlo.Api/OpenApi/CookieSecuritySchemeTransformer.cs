using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Menlo.Api.OpenApi;

internal sealed class CookieSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        IEnumerable<AuthenticationScheme> authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.Any(scheme => scheme.Name == CookieAuthenticationDefaults.AuthenticationScheme))
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                [CookieAuthenticationDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = ".Menlo.Session",
                    Description = "Authentication via secure HTTP-only cookie. To authenticate, navigate to GET /auth/login in your browser, complete the Microsoft Entra ID login, and the cookie will be set automatically. The cookie is then used for all subsequent API requests.",
                }
            };

            foreach (IOpenApiPathItem pathItem in document.Paths.Values)
            {
                if (pathItem.Operations is null)
                {
                    continue;
                }

                foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItem.Operations)
                {
                    operation.Value.Security ??= [];
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(CookieAuthenticationDefaults.AuthenticationScheme, document)] = []
                    });
                }
            }
        }
    }
}
