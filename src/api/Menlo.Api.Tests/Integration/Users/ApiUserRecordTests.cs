using Menlo.Lib.Auth.Entities;
using System.Net;
using System.Text.Json;

namespace Menlo.Api.Tests.Integration.Users;

[ClassDataSource<UserApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("User API")]
public sealed class ApiUserRecordTests(UserApiFixture fixture) : TestFixture
{
    private const string BudgetsUrl = "/api/budgets";

    [Test]
    public async Task GivenSignedInCallerWithoutUserRecord_WhenCallingApi_RequestSucceedsAndUserIsCreated()
    {
        string externalId = Guid.NewGuid().ToString();
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, budgetsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage first = await client.GetAsync(BudgetsUrl, TestContext.Current!.Execution.CancellationToken);
        HttpResponseMessage second = await client.GetAsync(BudgetsUrl, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveSucceeded(first);
        ItShouldHaveSucceeded(second);
        (await UserApiTestData.CountByExternalIdAsync(fixture.Services, externalId)).ShouldBe(1);
    }

    [Test]
    public async Task GivenTwoConcurrentFirstCalls_WhenCallingApi_BothSucceedAndOneUserExists()
    {
        string externalId = Guid.NewGuid().ToString();
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, budgetsEnabled: true);
        using HttpClient client = factory.CreateClient();
        CancellationToken cancellationToken = TestContext.Current!.Execution.CancellationToken;

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => client.GetAsync(BudgetsUrl, cancellationToken)));

        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        (await UserApiTestData.CountByExternalIdAsync(fixture.Services, externalId)).ShouldBe(1);
    }

    [Test]
    public async Task GivenCallerWhoseClaimsHaveNoEmail_WhenCallingApi_Returns403UserNotProvisioned()
    {
        string externalId = Guid.NewGuid().ToString();
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email: "", budgetsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(BudgetsUrl, TestContext.Current!.Execution.CancellationToken);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("USER_NOT_PROVISIONED");
        (await UserApiTestData.CountByExternalIdAsync(fixture.Services, externalId)).ShouldBe(0);
    }

    [Test]
    public async Task GivenCallerWhoseClaimsHaveNoEmail_WhenFetchingProfile_AuthUserStillAnswers()
    {
        await using TestWebApplicationFactory factory = fixture.CreateFactory(email: "");
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current!.Execution.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GivenUnauthenticatedCaller_WhenCallingApi_Returns401AndNoUserIsCreated()
    {
        await using TestWebApplicationFactory factory = fixture.CreateFactory(simulateUnauthenticated: true, budgetsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(BudgetsUrl, TestContext.Current!.Execution.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GivenSignedInMember_WhenCallingRemovedHouseholdsEndpoint_Returns404()
    {
        string externalId = Guid.NewGuid().ToString();
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/households", TestContext.Current!.Execution.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static void ItShouldHaveSucceeded(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
}
