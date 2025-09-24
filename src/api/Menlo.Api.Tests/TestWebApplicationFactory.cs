using Menlo.AI.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Menlo.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the AI service registrations that depend on Aspire
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IChatService) ||
                            d.ServiceType == typeof(IEmbeddingService) ||
                            d.ServiceType == typeof(IVisionService))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock AI services
            var mockChatService = Substitute.For<IChatService>();
            mockChatService.GetResponseAsync(Arg.Any<string>())
                .Returns(Task.FromResult("AI service is working"));

            var mockEmbeddingService = Substitute.For<IEmbeddingService>();
            var mockVisionService = Substitute.For<IVisionService>();

            services.AddSingleton(mockChatService);
            services.AddSingleton(mockEmbeddingService);
            services.AddSingleton(mockVisionService);
        });
    }
}
