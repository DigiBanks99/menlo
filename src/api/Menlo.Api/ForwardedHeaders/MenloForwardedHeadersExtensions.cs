using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using IPNetwork = System.Net.IPNetwork;

namespace Menlo.Api.ForwardedHeaders;

internal static class MenloForwardedHeadersExtensions
{
    private const string KnownNetworksKey = "ForwardedHeaders:KnownNetworks";
    private const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";

    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Trusts X-Forwarded-For/Proto only from loopback plus the proxies in
        /// <c>ForwardedHeaders:KnownNetworks</c> (CIDR) and <c>ForwardedHeaders:KnownProxies</c> (IP).
        /// </summary>
        public IHostApplicationBuilder AddMenloForwardedHeaders()
        {
            string[] networks = builder.Configuration.GetSection(KnownNetworksKey).Get<string[]>() ?? [];
            string[] proxies = builder.Configuration.GetSection(KnownProxiesKey).Get<string[]>() ?? [];

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                    | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

                foreach (string network in networks.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    if (!IPNetwork.TryParse(network, out IPNetwork parsed))
                    {
                        throw new InvalidOperationException(
                            $"'{KnownNetworksKey}' contains an invalid CIDR network: '{network}'.");
                    }

                    options.KnownIPNetworks.Add(parsed);
                }

                foreach (string proxy in proxies.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    if (!IPAddress.TryParse(proxy, out IPAddress? address))
                    {
                        throw new InvalidOperationException(
                            $"'{KnownProxiesKey}' contains an invalid IP address: '{proxy}'.");
                    }

                    options.KnownProxies.Add(address);
                }
            });

            return builder;
        }
    }
}
