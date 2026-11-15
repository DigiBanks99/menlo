using Menlo.Api.Budget;
using Menlo.Api.Budget.Categories;
using Menlo.Lib.Common;
using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

/// <summary>
/// Runs the real <c>/api</c> pipeline with the app's own stamp factories (no stand-ins), so the user
/// middleware's place in the pipeline is proven end to end.
/// </summary>
[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class StampedByTheSignedInUserTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task GivenASignedInUser_WhenTheyCreateABudget_ThenTheRowIsStampedWithTheirUserId()
    {
        CancellationToken ct = TestContext.Current!.Execution.CancellationToken;
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(useRealStampFactories: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: ct);

        Guid budgetId = await CreateBudgetAsync(client, ct);

        Guid createdBy = await ReadStampAsync(
            "SELECT created_by FROM budget_schema.budgets WHERE id = @id", budgetId, ct);
        Guid signedInUser = await ReadSignedInUserIdAsync(ct);
        createdBy.ShouldBe(signedInUser);
        createdBy.ShouldNotBe(SystemUser.Id.Value);
        createdBy.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task GivenASignedInUser_WhenTheyDeleteACategory_ThenTheRowIsStampedWithTheirUserId()
    {
        CancellationToken ct = TestContext.Current!.Execution.CancellationToken;
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(useRealStampFactories: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: ct);
        Guid budgetId = await CreateBudgetAsync(client, ct);
        HttpResponseMessage created = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", new CreateCategoryRequest("Housing", "Expense"), ct);
        CategoryDto category = (await created.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions, ct))!;

        HttpResponseMessage deleted = await client.DeleteAsync($"/api/budgets/{budgetId}/categories/{category.Id}", ct);

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        Guid deletedBy = await ReadStampAsync(
            "SELECT deleted_by FROM budget_schema.budget_categories WHERE id = @id", category.Id, ct);
        deletedBy.ShouldBe(await ReadSignedInUserIdAsync(ct));
    }

    private static async Task<Guid> CreateBudgetAsync(HttpClient client, CancellationToken ct)
    {
        HttpResponseMessage response = await client.PostAsync($"/api/budgets/{DateTimeOffset.UtcNow.Year}", null, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        BudgetDto budget = (await response.Content.ReadFromJsonAsync<BudgetDto>(JsonOptions, ct))!;
        return budget.Id;
    }

    private async Task<Guid> ReadStampAsync(string sql, Guid id, CancellationToken ct)
    {
        await using NpgsqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await using NpgsqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return (Guid)(await command.ExecuteScalarAsync(ct))!;
    }

    private async Task<Guid> ReadSignedInUserIdAsync(CancellationToken ct)
    {
        await using NpgsqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await using NpgsqlCommand command = new("SELECT id FROM shared.users WHERE external_id = @external", connection);
        command.Parameters.AddWithValue("external", Fixtures.TestAuthHandler.DefaultUserId);
        return (Guid)(await command.ExecuteScalarAsync(ct))!;
    }
}
