using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Forwarder;

namespace Menlo.Api.SpaHosting;

/// <summary>
/// Strips credentials before forwarding to the SPA development server. The dev server only serves
/// static assets and HMR, and the session cookie (which carries saved OIDC tokens) easily exceeds
/// Node's default header limit, causing 431 responses.
/// </summary>
internal sealed class SpaDevelopmentServerTransformer : HttpTransformer
{
    public static readonly SpaDevelopmentServerTransformer Instance = new();

    public override async ValueTask TransformRequestAsync(
        HttpContext httpContext,
        HttpRequestMessage proxyRequest,
        string destinationPrefix,
        CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        proxyRequest.Headers.Remove(HeaderNames.Cookie);
        proxyRequest.Headers.Remove(HeaderNames.Authorization);
    }
}
