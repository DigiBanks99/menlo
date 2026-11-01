using System.Net;

namespace Menlo.Api.Tests;

[ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerClass)]
// The shared factory starts its server lazily without a lock, so this class's tests run one at a time.
[NotInParallel(nameof(ApiSmokeTests))]
public class ApiSmokeTests(TestWebApplicationFactory factory)
{
    [Test]
    public async Task AiHealth_Endpoint_Returns_Ok()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}


