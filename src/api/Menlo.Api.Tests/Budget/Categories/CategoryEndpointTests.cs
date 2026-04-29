using Menlo.Api.Budget.Categories;
using Menlo.Lib.Common.ValueObjects;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget.Categories;

[Collection("Budget")]
public sealed class CategoryEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // Each test scenario uses a unique household to avoid cross-test contamination
    private static readonly HouseholdId CreateRootCategoryHousehold =
        new(Guid.Parse("c0c0c0c0-c0c0-c0c0-c0c0-c0c0c0c0c0c0"));

    private static readonly HouseholdId CreateSubcategoryHousehold =
        new(Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"));

    private static readonly HouseholdId DuplicateNameHousehold =
        new(Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2"));

    private static readonly HouseholdId DepthViolationHousehold =
        new(Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3"));

    private static readonly HouseholdId DeletedParentHousehold =
        new(Guid.Parse("c4c4c4c4-c4c4-c4c4-c4c4-c4c4c4c4c4c4"));

    private static readonly HouseholdId EmptyNameHousehold =
        new(Guid.Parse("c5c5c5c5-c5c5-c5c5-c5c5-c5c5c5c5c5c5"));

    private static readonly HouseholdId InvalidBudgetFlowHousehold =
        new(Guid.Parse("c6c6c6c6-c6c6-c6c6-c6c6-c6c6c6c6c6c6"));

    private static readonly HouseholdId ListEmptyHousehold =
        new(Guid.Parse("c7c7c7c7-c7c7-c7c7-c7c7-c7c7c7c7c7c7"));

    private static readonly HouseholdId ListTreeHousehold =
        new(Guid.Parse("c8c8c8c8-c8c8-c8c8-c8c8-c8c8c8c8c8c8"));

    private static readonly HouseholdId ListDeletedHousehold =
        new(Guid.Parse("c9c9c9c9-c9c9-c9c9-c9c9-c9c9c9c9c9c9"));

    private static readonly HouseholdId GetCategoryHousehold =
        new(Guid.Parse("cacacacc-caca-caca-caca-cacacacacaca"));

    private static readonly HouseholdId UpdateCategoryHousehold =
        new(Guid.Parse("cbcbcbcb-cbcb-cbcb-cbcb-cbcbcbcbcbcb"));

    private static readonly HouseholdId UpdateDuplicateHousehold =
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    private static readonly HouseholdId ReparentHousehold =
        new(Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd"));

    private static readonly HouseholdId ReparentDepthHousehold =
        new(Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdce"));

    private static readonly HouseholdId DeleteCategoryHousehold =
        new(Guid.Parse("cececece-cece-cece-cece-cececececece"));

    private static readonly HouseholdId RestoreCategoryHousehold =
        new(Guid.Parse("cfcfcfcf-cfcf-cfcf-cfcf-cfcfcfcfcfcf"));

    private static readonly HouseholdId CloneCanonicalHousehold =
        new(Guid.Parse("d0d0d0d0-d0d0-d0d0-d0d0-d0d0d0d0d0d0"));

    // =========================================================================
    // CREATE CATEGORY
    // =========================================================================

    [Fact]
    public async Task GivenValidRequest_WhenCreateCategory_ThenReturns201WithCategoryDto()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(CreateRootCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CreateCategoryRequest request = new("Housing", "Expense", Description: "Monthly housing costs");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", request, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveANonEmptyId(dto);
        ItShouldBelongToBudget(dto, budgetId);
        ItShouldHaveName(dto, "Housing");
        ItShouldHaveDescription(dto, "Monthly housing costs");
        ItShouldHaveNullParentId(dto);
        ItShouldHaveBudgetFlow(dto, "Expense");
        ItShouldNotBeDeleted(dto);
        ItShouldHaveACanonicalCategoryId(dto);
    }

    [Fact]
    public async Task GivenParentCategory_WhenCreateSubcategory_ThenReturns201WithParentId()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(CreateSubcategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? parent = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Expenses", "Expense"));

        CreateCategoryRequest childRequest = new("Groceries", "Expense", ParentId: parent!.Id);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", childRequest, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveName(dto, "Groceries");
        ItShouldHaveParentId(dto, parent.Id);
    }

    [Fact]
    public async Task GivenDuplicateName_WhenCreateCategory_ThenReturns409Conflict()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DuplicateNameHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        await CreateCategoryAndDeserializeAsync(client, budgetId, new("Housing", "Expense"));

        CreateCategoryRequest duplicateRequest = new("Housing", "Expense");
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", duplicateRequest, TestContext.Current.CancellationToken);

        ItShouldHaveReturned409Conflict(response);
    }

    [Fact]
    public async Task GivenChildOfChild_WhenCreateCategory_ThenReturns400DepthViolation()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DepthViolationHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? root = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Root", "Expense"));
        CategoryDto? child = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Child", "Expense", ParentId: root!.Id));

        CreateCategoryRequest grandChildRequest = new("GrandChild", "Expense", ParentId: child!.Id);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", grandChildRequest, TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenDeletedParent_WhenCreateCategory_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DeletedParentHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? parent = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Deleted", "Expense"));

        // Delete the parent
        await client.DeleteAsync($"/api/budgets/{budgetId}/categories/{parent!.Id}", TestContext.Current.CancellationToken);

        CreateCategoryRequest childRequest = new("Child", "Expense", ParentId: parent.Id);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", childRequest, TestContext.Current.CancellationToken);

        // The API returns 404 because EF's query filter excludes soft-deleted categories
        // from the loaded collection, so the parent is not found
        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenEmptyName_WhenCreateCategory_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(EmptyNameHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CreateCategoryRequest request = new("", "Expense");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", request, TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenInvalidBudgetFlow_WhenCreateCategory_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(InvalidBudgetFlowHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CreateCategoryRequest request = new("Test", "InvalidFlow");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", request, TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenNonExistentBudget_WhenCreateCategory_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(CreateRootCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid fakeBudgetId = Guid.NewGuid();
        CreateCategoryRequest request = new("Test", "Expense");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{fakeBudgetId}/categories", request, TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // LIST CATEGORIES
    // =========================================================================

    [Fact]
    public async Task GivenEmptyBudget_WhenListCategories_ThenReturns200WithEmptyArray()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ListEmptyHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories", TestContext.Current.CancellationToken);

        List<CategoryTreeNode>? tree = await DeserializeCategoryTree(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveEmptyTree(tree);
    }

    [Fact]
    public async Task GivenBudgetWithHierarchy_WhenListCategories_ThenReturns200WithTreeStructure()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ListTreeHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? root = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Expenses", "Expense"));
        await CreateCategoryAndDeserializeAsync(client, budgetId, new("Groceries", "Expense", ParentId: root!.Id));

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories", TestContext.Current.CancellationToken);

        List<CategoryTreeNode>? tree = await DeserializeCategoryTree(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveSingleRootNode(tree, "Expenses");
        ItShouldHaveChildNode(tree, "Expenses", "Groceries");
    }

    [Fact]
    public async Task GivenDeletedCategory_WhenListCategoriesWithoutIncludeDeleted_ThenExcludesDeleted()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ListDeletedHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? active = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Active", "Expense"));
        CategoryDto? toDelete = await CreateCategoryAndDeserializeAsync(client, budgetId, new("ToDelete", "Expense"));
        await client.DeleteAsync($"/api/budgets/{budgetId}/categories/{toDelete!.Id}", TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories", TestContext.Current.CancellationToken);

        List<CategoryTreeNode>? tree = await DeserializeCategoryTree(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveExactlyNRootNodes(tree, 1);
        ItShouldHaveSingleRootNode(tree, "Active");
    }

    [Fact]
    public async Task GivenDeletedCategory_WhenListCategoriesWithIncludeDeleted_ThenIncludesDeleted()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ListDeletedHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Uses the same household as above – the deleted category from the previous test
        // We need to get the budget that already exists or create fresh
        Guid budgetId = await CreateBudgetAsync(client);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories?includeDeleted=true", TestContext.Current.CancellationToken);

        List<CategoryTreeNode>? tree = await DeserializeCategoryTree(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldContainDeletedNode(tree);
    }

    // =========================================================================
    // GET CATEGORY
    // =========================================================================

    [Fact]
    public async Task GivenExistingCategory_WhenGetCategory_ThenReturns200WithCategoryDto()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(GetCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? created = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Transport", "Expense"));

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{created!.Id}", TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveName(dto, "Transport");
        ItShouldBelongToBudget(dto, budgetId);
    }

    [Fact]
    public async Task GivenNonExistentCategory_WhenGetCategory_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(GetCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenCategoryFromWrongBudget_WhenGetCategory_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(GetCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? created = await CreateCategoryAndDeserializeAsync(client, budgetId, new("OnBudget1", "Expense"));

        // Use a non-existent budget ID to simulate "wrong budget"
        Guid wrongBudgetId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{wrongBudgetId}/categories/{created!.Id}", TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // UPDATE CATEGORY
    // =========================================================================

    [Fact]
    public async Task GivenExistingCategory_WhenUpdateName_ThenReturns200WithUpdatedDto()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? created = await CreateCategoryAndDeserializeAsync(client, budgetId, new("OldName", "Expense"));

        UpdateCategoryRequest updateRequest = new("NewName", "Expense");
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{created!.Id}", updateRequest, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveName(dto, "NewName");
    }

    [Fact]
    public async Task GivenExistingCategory_WhenUpdateAllFields_ThenReturns200WithAllFieldsUpdated()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? created = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Original", "Expense"));

        UpdateCategoryRequest updateRequest = new(
            "Updated",
            "Income",
            Description: "Updated description",
            Attribution: "Main",
            IncomeContributor: "Partner",
            ResponsiblePayer: "Self");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{created!.Id}", updateRequest, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveName(dto, "Updated");
        ItShouldHaveBudgetFlow(dto, "Income");
        ItShouldHaveDescription(dto, "Updated description");
        ItShouldHaveAttribution(dto, "Main");
        ItShouldHaveIncomeContributor(dto, "Partner");
        ItShouldHaveResponsiblePayer(dto, "Self");
    }

    [Fact]
    public async Task GivenDuplicateName_WhenUpdateCategory_ThenReturns409Conflict()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateDuplicateHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        await CreateCategoryAndDeserializeAsync(client, budgetId, new("Existing", "Expense"));
        CategoryDto? toUpdate = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Different", "Expense"));

        UpdateCategoryRequest updateRequest = new("Existing", "Expense");
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{toUpdate!.Id}", updateRequest, TestContext.Current.CancellationToken);

        ItShouldHaveReturned409Conflict(response);
    }

    [Fact]
    public async Task GivenNonExistentCategory_WhenUpdateCategory_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        UpdateCategoryRequest updateRequest = new("Whatever", "Expense");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}", updateRequest, TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // REPARENT CATEGORY
    // =========================================================================

    [Fact]
    public async Task GivenChildCategory_WhenReparentToDifferentRoot_ThenReturns200WithNewParent()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ReparentHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? root1 = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Root1", "Expense"));
        CategoryDto? root2 = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Root2", "Expense"));
        CategoryDto? child = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Child", "Expense", ParentId: root1!.Id));

        ReparentCategoryRequest reparentRequest = new(NewParentId: root2!.Id);
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{child!.Id}/reparent", reparentRequest, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveParentId(dto, root2.Id);
    }

    [Fact]
    public async Task GivenChildCategory_WhenPromoteToRoot_ThenReturns200WithNullParent()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ReparentHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? root = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Parent", "Expense"));
        CategoryDto? child = await CreateCategoryAndDeserializeAsync(client, budgetId, new("PromoteMe", "Expense", ParentId: root!.Id));

        ReparentCategoryRequest reparentRequest = new(NewParentId: null);
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{child!.Id}/reparent", reparentRequest, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveNullParentId(dto);
    }

    [Fact]
    public async Task GivenReparentWouldExceedDepth_WhenReparent_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ReparentDepthHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? root = await CreateCategoryAndDeserializeAsync(client, budgetId, new("RootForDepth", "Expense"));
        CategoryDto? child = await CreateCategoryAndDeserializeAsync(client, budgetId, new("ChildForDepth", "Expense", ParentId: root!.Id));
        // Create another root-level category that has a child
        CategoryDto? anotherRoot = await CreateCategoryAndDeserializeAsync(client, budgetId, new("AnotherRoot", "Expense"));
        CategoryDto? anotherChild = await CreateCategoryAndDeserializeAsync(client, budgetId, new("AnotherChild", "Expense", ParentId: anotherRoot!.Id));

        // Try to reparent anotherChild under child → would be depth 3
        ReparentCategoryRequest reparentRequest = new(NewParentId: child!.Id);
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{anotherChild!.Id}/reparent", reparentRequest, TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // =========================================================================
    // DELETE CATEGORY
    // =========================================================================

    [Fact]
    public async Task GivenExistingCategory_WhenDeleteCategory_ThenReturns204NoContent()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DeleteCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? created = await CreateCategoryAndDeserializeAsync(client, budgetId, new("ToDelete", "Expense"));

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{created!.Id}", TestContext.Current.CancellationToken);

        ItShouldHaveReturned204NoContent(response);
    }

    [Fact]
    public async Task GivenParentWithChildren_WhenDeleteParent_ThenChildrenAlsoDeleted()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DeleteCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? parent = await CreateCategoryAndDeserializeAsync(client, budgetId, new("ParentToDel", "Expense"));
        CategoryDto? child = await CreateCategoryAndDeserializeAsync(client, budgetId, new("ChildToDel", "Expense", ParentId: parent!.Id));

        // Delete parent
        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{parent.Id}", TestContext.Current.CancellationToken);
        ItShouldHaveReturned204NoContent(deleteResponse);

        // Verify child is also deleted by listing with includeDeleted
        HttpResponseMessage listResponse = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories?includeDeleted=true", TestContext.Current.CancellationToken);
        List<CategoryTreeNode>? tree = await DeserializeCategoryTree(listResponse);

        ItShouldHaveAllNodesDeleted(tree, "ParentToDel");
    }

    // =========================================================================
    // RESTORE CATEGORY
    // =========================================================================

    [Fact]
    public async Task GivenDeletedCategory_WhenRestoreCategory_ThenReturns200()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(RestoreCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? created = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Restorable", "Expense"));

        await client.DeleteAsync($"/api/budgets/{budgetId}/categories/{created!.Id}", TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.PutAsync(
            $"/api/budgets/{budgetId}/categories/{created.Id}/restore", null, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldNotBeDeleted(dto);
        ItShouldHaveName(dto, "Restorable");
    }

    [Fact]
    public async Task GivenActiveCategory_WhenRestoreCategory_ThenReturns200AsNoOp()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(RestoreCategoryHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? created = await CreateCategoryAndDeserializeAsync(client, budgetId, new("AlreadyActive", "Income"));

        HttpResponseMessage response = await client.PutAsync(
            $"/api/budgets/{budgetId}/categories/{created!.Id}/restore", null, TestContext.Current.CancellationToken);

        CategoryDto? dto = await DeserializeCategoryDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldNotBeDeleted(dto);
    }

    // =========================================================================
    // CLONE — CanonicalCategoryId preserved
    // =========================================================================

    [Fact]
    public async Task GivenBudgetWithCategories_WhenCloneBudget_ThenCanonicalCategoryIdIsPreserved()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(CloneCanonicalHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int currentYear = DateTimeOffset.UtcNow.Year;

        Guid budgetId = await CreateBudgetAsync(client);
        CategoryDto? original = await CreateCategoryAndDeserializeAsync(client, budgetId, new("Recurring", "Expense"));

        // Clone by creating next year's budget
        HttpResponseMessage cloneResponse = await client.PostAsync(
            $"/api/budgets/{currentYear + 1}", null, TestContext.Current.CancellationToken);
        cloneResponse.IsSuccessStatusCode.ShouldBeTrue();

        string cloneContent = await cloneResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonDocument doc = JsonDocument.Parse(cloneContent);
        Guid clonedBudgetId = doc.RootElement.GetProperty("id").GetGuid();

        // Get the cloned categories
        HttpResponseMessage listResponse = await client.GetAsync(
            $"/api/budgets/{clonedBudgetId}/categories", TestContext.Current.CancellationToken);
        List<CategoryTreeNode>? tree = await DeserializeCategoryTree(listResponse);

        // Verify the canonical category ID is the same
        HttpResponseMessage getCatResponse = await client.GetAsync(
            $"/api/budgets/{clonedBudgetId}/categories/{tree![0].Id}", TestContext.Current.CancellationToken);
        CategoryDto? cloned = await DeserializeCategoryDto(getCatResponse);

        ItShouldHaveReturned200Ok(getCatResponse);
        ItShouldShareCanonicalCategoryId(original, cloned);
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    private BudgetTestWebApplicationFactory CreateIsolatedFactory(HouseholdId householdId) =>
        new(householdId)
        {
            MenloConnectionString = fixture.ConnectionString,
            SkipMigration = true,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true"
            }
        };

    private static async Task<Guid> CreateBudgetAsync(HttpClient client)
    {
        int year = DateTimeOffset.UtcNow.Year;
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue();

        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<CategoryDto?> CreateCategoryAndDeserializeAsync(
        HttpClient client, Guid budgetId, CreateCategoryRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", request, TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue($"Failed to create category: {await response.Content.ReadAsStringAsync()}");
        return await DeserializeCategoryDto(response);
    }

    private static async Task<CategoryDto?> DeserializeCategoryDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CategoryDto>(content, JsonOptions);
    }

    private static async Task<List<CategoryTreeNode>?> DeserializeCategoryTree(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CategoryTreeNode>>(content, JsonOptions);
    }

    // =========================================================================
    // ASSERTION HELPERS
    // =========================================================================

    private static void ItShouldHaveReturned201Created(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

    private static void ItShouldHaveReturned200Ok(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    private static void ItShouldHaveReturned204NoContent(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

    private static void ItShouldHaveReturned400BadRequest(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    private static void ItShouldHaveReturned404NotFound(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

    private static void ItShouldHaveReturned409Conflict(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

    private static void ItShouldHaveANonEmptyId(CategoryDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Id.ShouldNotBe(Guid.Empty);
    }

    private static void ItShouldBelongToBudget(CategoryDto? dto, Guid budgetId)
    {
        dto.ShouldNotBeNull();
        dto.BudgetId.ShouldBe(budgetId);
    }

    private static void ItShouldHaveName(CategoryDto? dto, string expectedName)
    {
        dto.ShouldNotBeNull();
        dto.Name.ShouldBe(expectedName);
    }

    private static void ItShouldHaveDescription(CategoryDto? dto, string? expectedDescription)
    {
        dto.ShouldNotBeNull();
        dto.Description.ShouldBe(expectedDescription);
    }

    private static void ItShouldHaveNullParentId(CategoryDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.ParentId.ShouldBeNull();
    }

    private static void ItShouldHaveParentId(CategoryDto? dto, Guid expectedParentId)
    {
        dto.ShouldNotBeNull();
        dto.ParentId.ShouldBe(expectedParentId);
    }

    private static void ItShouldHaveBudgetFlow(CategoryDto? dto, string expectedFlow)
    {
        dto.ShouldNotBeNull();
        dto.BudgetFlow.ShouldBe(expectedFlow);
    }

    private static void ItShouldHaveAttribution(CategoryDto? dto, string expectedAttribution)
    {
        dto.ShouldNotBeNull();
        dto.Attribution.ShouldBe(expectedAttribution);
    }

    private static void ItShouldHaveIncomeContributor(CategoryDto? dto, string expectedContributor)
    {
        dto.ShouldNotBeNull();
        dto.IncomeContributor.ShouldBe(expectedContributor);
    }

    private static void ItShouldHaveResponsiblePayer(CategoryDto? dto, string expectedPayer)
    {
        dto.ShouldNotBeNull();
        dto.ResponsiblePayer.ShouldBe(expectedPayer);
    }

    private static void ItShouldNotBeDeleted(CategoryDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.IsDeleted.ShouldBeFalse();
    }

    private static void ItShouldHaveACanonicalCategoryId(CategoryDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.CanonicalCategoryId.ShouldNotBe(Guid.Empty);
    }

    private static void ItShouldHaveEmptyTree(List<CategoryTreeNode>? tree)
    {
        tree.ShouldNotBeNull();
        tree.ShouldBeEmpty();
    }

    private static void ItShouldHaveSingleRootNode(List<CategoryTreeNode>? tree, string expectedName)
    {
        tree.ShouldNotBeNull();
        tree.ShouldContain(n => n.Name == expectedName);
    }

    private static void ItShouldHaveChildNode(List<CategoryTreeNode>? tree, string parentName, string childName)
    {
        tree.ShouldNotBeNull();
        CategoryTreeNode? parent = tree.FirstOrDefault(n => n.Name == parentName);
        parent.ShouldNotBeNull();
        parent.Children.ShouldContain(c => c.Name == childName);
    }

    private static void ItShouldHaveExactlyNRootNodes(List<CategoryTreeNode>? tree, int count)
    {
        tree.ShouldNotBeNull();
        tree.Count.ShouldBe(count);
    }

    private static void ItShouldContainDeletedNode(List<CategoryTreeNode>? tree)
    {
        tree.ShouldNotBeNull();
        // Flatten all nodes
        static bool HasDeletedNode(IEnumerable<CategoryTreeNode> nodes) =>
            nodes.Any(n => n.IsDeleted || HasDeletedNode(n.Children));
        HasDeletedNode(tree).ShouldBeTrue();
    }

    private static void ItShouldHaveAllNodesDeleted(List<CategoryTreeNode>? tree, string parentName)
    {
        tree.ShouldNotBeNull();
        CategoryTreeNode? parent = FindNode(tree, parentName);
        parent.ShouldNotBeNull();
        parent.IsDeleted.ShouldBeTrue();
        foreach (CategoryTreeNode child in parent.Children)
        {
            child.IsDeleted.ShouldBeTrue();
        }
    }

    private static void ItShouldShareCanonicalCategoryId(CategoryDto? original, CategoryDto? cloned)
    {
        original.ShouldNotBeNull();
        cloned.ShouldNotBeNull();
        cloned.CanonicalCategoryId.ShouldBe(original.CanonicalCategoryId);
        cloned.Id.ShouldNotBe(original.Id); // Different category IDs
    }

    private static CategoryTreeNode? FindNode(IEnumerable<CategoryTreeNode> nodes, string name)
    {
        foreach (CategoryTreeNode node in nodes)
        {
            if (node.Name == name) return node;
            CategoryTreeNode? found = FindNode(node.Children, name);
            if (found is not null) return found;
        }
        return null;
    }
}
