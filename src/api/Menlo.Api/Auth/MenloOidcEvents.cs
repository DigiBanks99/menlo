using Menlo.Application.Onboarding;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Menlo.Api.Auth;

public sealed class MenloOidcEvents(IServiceProvider serviceProvider)
{
    public async Task OnTokenValidated(TokenValidatedContext context)
    {
        string? externalId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Principal?.FindFirst("oid")?.Value
            ?? context.Principal?.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(externalId))
        {
            return;
        }

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        string email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@tenant.onmicrosoft.com";
        string displayName = context.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? email;

        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, context.HttpContext.RequestAborted);
    }
}
