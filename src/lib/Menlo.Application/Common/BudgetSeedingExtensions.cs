using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Common;

/// <summary>
/// Extension methods to seed a demo household budget for the current calendar year.
/// Uses obfuscated names — safe for version control.
/// </summary>
public static class BudgetSeedingExtensions
{
    /// <summary>
    /// Seeds a demo budget for the current year if no budget exists for the seed household.
    /// Idempotent: skips if the household already has a budget for the current year.
    /// </summary>
    public static async Task SeedDemoBudgetAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!config.GetValue<bool>("Menlo:SeedDemoBudget") || config.GetValue<bool>("Menlo:SkipMigration"))
        {
            return;
        }

        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Menlo.Seeding");

        int year = DateTime.UtcNow.Year;
        SeedAuditStampFactory auditFactory = new();

        // 1. Ensure household exists
        Household household = await EnsureHouseholdAsync(db, auditFactory, logger, cancellationToken);

        // 2. Ensure users exist
        UserId primaryUserId = await EnsureUserAsync(
            db, household.Id, auditFactory,
            externalId: "seed-primary-user",
            email: "primary@demo.menlo.local",
            displayName: "Person A",
            logger, cancellationToken);

        UserId secondaryUserId = await EnsureUserAsync(
            db, household.Id, auditFactory,
            externalId: "seed-secondary-user",
            email: "secondary@demo.menlo.local",
            displayName: "Person B",
            logger, cancellationToken);

        // 3. Check if budget already exists
        bool forceReseed = config.GetValue<bool>("Menlo:ReseedDemoBudget");
        BudgetAggregate? existingBudget = await db.Budgets
            .FirstOrDefaultAsync(b => b.HouseholdId == household.Id && b.Year == year, cancellationToken);

        if (existingBudget is not null)
        {
            if (!forceReseed)
            {
                logger.LogInformation("Demo budget for {Year} already exists, skipping seed", year);
                return;
            }

            // Collect canonical IDs in-memory before cascade removes the FK references
            Guid budgetId = existingBudget.Id.Value;
            List<Guid> canonicalIds = await db.Database
                .SqlQuery<Guid>($"SELECT canonical_category_id AS \"Value\" FROM budget_schema.budget_categories WHERE budget_id = {budgetId}")
                .ToListAsync(cancellationToken);

            // Delete budget (cascade removes items + categories)
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM budget_schema.budgets WHERE id = {budgetId}", cancellationToken);

            // Clean up orphaned canonical categories
            if (canonicalIds.Count > 0)
            {
                FormattableString deleteSql = $"DELETE FROM budget_schema.canonical_categories WHERE id = ANY({canonicalIds.ToArray()})";
                await db.Database.ExecuteSqlAsync(deleteSql, cancellationToken);
            }

            logger.LogInformation("Deleted existing demo budget for {Year} (force reseed)", year);
        }

        // 4. Create the budget
        Result<BudgetAggregate, BudgetError> budgetResult = BudgetAggregate.Create(household.Id, year, auditFactory);
        if (budgetResult.IsFailure)
        {
            logger.LogError("Failed to create demo budget: {Error}", budgetResult.Error);
            return;
        }

        BudgetAggregate budget = budgetResult.Value;

        // 5. Add categories and items
        PayerSplit primaryPayerSplit = CreatePayerSplit(primaryUserId, 100);
        PayerSplit secondaryPayerSplit = CreatePayerSplit(secondaryUserId, 100);
        PayerSplit sharedPayerSplit = CreateSharedPayerSplit(primaryUserId, secondaryUserId);
        AttributionSplit mainAttribution = CreateAttributionSplit(Attribution.Main, 100);
        AttributionSplit rentalAttribution = CreateAttributionSplit(Attribution.Rental, 100);

        // === Person A (primary earner) — BudgetFlow.Both ===
        CategoryNode personARoot = AddCategoryOrThrow(budget, "Person A", BudgetFlow.Both, attribution: Attribution.Main);

        // Income
        CategoryNode personASalary = AddChildOrThrow(budget, "Salary", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personASalary.Id, BudgetFlow.Income, 121_000m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, personASalary.Id, 6, BudgetFlow.Income, 125_000m, primaryPayerSplit, mainAttribution);

        // Deductions
        CategoryNode personATax = AddChildOrThrow(budget, "Tax", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personATax.Id, BudgetFlow.Expense, 36_300m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, personATax.Id, 6, BudgetFlow.Expense, 37_500m, primaryPayerSplit, mainAttribution);

        CategoryNode personAUif = AddChildOrThrow(budget, "UIF", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAUif.Id, BudgetFlow.Expense, 177.12m, primaryPayerSplit, mainAttribution);

        CategoryNode personAMedical = AddChildOrThrow(budget, "Medical Aid", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAMedical.Id, BudgetFlow.Expense, 7_500m, primaryPayerSplit, mainAttribution);

        CategoryNode personAPension = AddChildOrThrow(budget, "Pension Fund", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAPension.Id, BudgetFlow.Expense, 9_075m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, personAPension.Id, 6, BudgetFlow.Expense, 9_375m, primaryPayerSplit, mainAttribution);

        // Expenses (only items unique to Person A — cross-sheet references
        // like Mortgage, School Fees, and Security live in their own categories)
        CategoryNode personAGroceries = AddChildOrThrow(budget, "Groceries", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAGroceries.Id, BudgetFlow.Expense, 8_000m, primaryPayerSplit, mainAttribution);

        CategoryNode personALoan = AddChildOrThrow(budget, "Loan", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personALoan.Id, BudgetFlow.Expense, 5_000m, primaryPayerSplit, mainAttribution);

        CategoryNode personACash = AddChildOrThrow(budget, "Cash", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personACash.Id, BudgetFlow.Expense, 2_000m, primaryPayerSplit, mainAttribution);

        CategoryNode personAFuel = AddChildOrThrow(budget, "Fuel", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAFuel.Id, BudgetFlow.Expense, 4_000m, primaryPayerSplit, mainAttribution);

        CategoryNode personATelecom = AddChildOrThrow(budget, "Telecom", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personATelecom.Id, BudgetFlow.Expense, 1_599m, primaryPayerSplit, mainAttribution);

        CategoryNode personAMobile = AddChildOrThrow(budget, "Mobile", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAMobile.Id, BudgetFlow.Expense, 1_200m, primaryPayerSplit, mainAttribution);

        CategoryNode personAStreaming1 = AddChildOrThrow(budget, "Streaming A", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAStreaming1.Id, BudgetFlow.Expense, 249m, primaryPayerSplit, mainAttribution);

        CategoryNode personAStreaming2 = AddChildOrThrow(budget, "Streaming B", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAStreaming2.Id, BudgetFlow.Expense, 140m, primaryPayerSplit, mainAttribution);

        CategoryNode personAAiSub = AddChildOrThrow(budget, "AI Subscription", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAAiSub.Id, BudgetFlow.Expense, 370m, primaryPayerSplit, mainAttribution);

        CategoryNode personACloud = AddChildOrThrow(budget, "Cloud Storage", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personACloud.Id, BudgetFlow.Expense, 22.99m, primaryPayerSplit, mainAttribution);

        CategoryNode personAInsurance = AddChildOrThrow(budget, "Insurance", personARoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personAInsurance.Id, BudgetFlow.Expense, 4_200m, primaryPayerSplit, mainAttribution);

        // === Person B (secondary earner) — BudgetFlow.Both ===
        CategoryNode personBRoot = AddCategoryOrThrow(budget, "Person B", BudgetFlow.Both, attribution: Attribution.Main);

        CategoryNode personBSalary = AddChildOrThrow(budget, "Salary", personBRoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personBSalary.Id, BudgetFlow.Income, 9_240m, secondaryPayerSplit, mainAttribution);

        // Flat Income is tracked under House — Person B's sheet references it for visibility only
        CategoryNode personBTax = AddChildOrThrow(budget, "Tax", personBRoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personBTax.Id, BudgetFlow.Expense, 524m, secondaryPayerSplit, mainAttribution);

        CategoryNode personBUif = AddChildOrThrow(budget, "UIF", personBRoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, personBUif.Id, BudgetFlow.Expense, 177.12m, secondaryPayerSplit, mainAttribution);

        // === House — BudgetFlow.Both ===
        CategoryNode houseRoot = AddCategoryOrThrow(budget, "House", BudgetFlow.Both, attribution: Attribution.Main);

        CategoryNode houseRental = AddChildOrThrow(budget, "Flat Income", houseRoot.Id, BudgetFlow.Both, Attribution.Rental);
        BulkCreateOrThrow(budget, houseRental.Id, BudgetFlow.Income, 0m, sharedPayerSplit, rentalAttribution);
        FillForwardOrThrow(budget, houseRental.Id, 7, BudgetFlow.Income, 10_000m, sharedPayerSplit, rentalAttribution);

        CategoryNode houseBond = AddChildOrThrow(budget, "Bond", houseRoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, houseBond.Id, BudgetFlow.Expense, 18_232.74m, primaryPayerSplit, mainAttribution);

        CategoryNode houseWater = AddChildOrThrow(budget, "Water", houseRoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, houseWater.Id, BudgetFlow.Expense, 1_500m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, houseWater.Id, 7, BudgetFlow.Expense, 1_650m, primaryPayerSplit, mainAttribution);

        CategoryNode houseRates = AddChildOrThrow(budget, "Municipal Rates", houseRoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, houseRates.Id, BudgetFlow.Expense, 2_000m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, houseRates.Id, 7, BudgetFlow.Expense, 2_140m, primaryPayerSplit, mainAttribution);

        CategoryNode houseElectricity = AddChildOrThrow(budget, "Electricity", houseRoot.Id, BudgetFlow.Both, Attribution.Main);
        BulkCreateOrThrow(budget, houseElectricity.Id, BudgetFlow.Expense, 3_000m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, houseElectricity.Id, 7, BudgetFlow.Expense, 3_210m, primaryPayerSplit, mainAttribution);

        // === School Fees — Expense only ===
        CategoryNode schoolRoot = AddCategoryOrThrow(budget, "School Fees", BudgetFlow.Expense, attribution: Attribution.Main);

        CategoryNode schoolChildA = AddChildOrThrow(budget, "Child A", schoolRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, schoolChildA.Id, BudgetFlow.Expense, 3_420m, primaryPayerSplit, mainAttribution);

        CategoryNode schoolChildB = AddChildOrThrow(budget, "Child B", schoolRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, schoolChildB.Id, BudgetFlow.Expense, 3_526m, primaryPayerSplit, mainAttribution);

        CategoryNode schoolChildC = AddChildOrThrow(budget, "Child C", schoolRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, schoolChildC.Id, BudgetFlow.Expense, 0m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, schoolChildC.Id, 7, BudgetFlow.Expense, 3_526m, primaryPayerSplit, mainAttribution);

        CategoryNode schoolActivities = AddChildOrThrow(budget, "Activities", schoolRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, schoolActivities.Id, BudgetFlow.Expense, 500m, primaryPayerSplit, mainAttribution);

        CategoryNode schoolRegistration = AddChildOrThrow(budget, "Registration", schoolRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, schoolRegistration.Id, BudgetFlow.Expense, 0m, primaryPayerSplit, mainAttribution);
        // Registration is a one-time R3,500 in January
        BudgetItem? janRegistration = budget.Items
            .FirstOrDefault(i => i.CategoryId == schoolRegistration.Id && i.Month == 1);
        if (janRegistration is not null)
        {
            budget.UpdateItem(janRegistration.Id, plannedAmount: Money.Create(3_500m, "ZAR").Value);
        }

        // === Security — Expense only ===
        CategoryNode securityRoot = AddCategoryOrThrow(budget, "Security", BudgetFlow.Expense, attribution: Attribution.Main);

        CategoryNode securityAlarm = AddChildOrThrow(budget, "Alarm", securityRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, securityAlarm.Id, BudgetFlow.Expense, 580m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, securityAlarm.Id, 5, BudgetFlow.Expense, 604.99m, primaryPayerSplit, mainAttribution);

        CategoryNode securityFencing = AddChildOrThrow(budget, "Fencing", securityRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, securityFencing.Id, BudgetFlow.Expense, 200m, primaryPayerSplit, mainAttribution);
        FillForwardOrThrow(budget, securityFencing.Id, 7, BudgetFlow.Expense, 220m, primaryPayerSplit, mainAttribution);

        CategoryNode securityCameras = AddChildOrThrow(budget, "Cameras", securityRoot.Id, BudgetFlow.Expense, Attribution.Main);
        BulkCreateOrThrow(budget, securityCameras.Id, BudgetFlow.Expense, 0m, primaryPayerSplit, mainAttribution);

        // 6. Create canonical categories for each category node
        foreach (CategoryNode category in budget.Categories)
        {
            CanonicalCategory canonical = CanonicalCategory.Create(category.CanonicalCategoryId, category.Name.Value);
            canonical.Audit(auditFactory, Menlo.Lib.Common.Enums.AuditOperation.Create);
            db.CanonicalCategories.Add(canonical);
        }

        // 7. Persist
        db.Budgets.Add(budget);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded demo budget for {Year} with {CategoryCount} categories and {ItemCount} items",
            year, budget.Categories.Count, budget.Items.Count);
    }

    private static async Task<Household> EnsureHouseholdAsync(
        MenloDbContext db,
        IAuditStampFactory auditFactory,
        ILogger logger,
        CancellationToken ct)
    {
        const string seedHouseholdName = "Demo Household";
        Household? existing = await db.Households
            .FirstOrDefaultAsync(h => h.Name == seedHouseholdName, ct);

        if (existing is not null)
        {
            return existing;
        }

        Result<Household, Menlo.Lib.Auth.Errors.HouseholdError> result = Household.Create(seedHouseholdName);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create seed household: {result.Error}");
        }

        Household household = result.Value;
        household.Audit(auditFactory, Menlo.Lib.Common.Enums.AuditOperation.Create);
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created seed household: {Id}", household.Id);
        return household;
    }

    private static async Task<UserId> EnsureUserAsync(
        MenloDbContext db,
        HouseholdId householdId,
        IAuditStampFactory auditFactory,
        string externalId,
        string email,
        string displayName,
        ILogger logger,
        CancellationToken ct)
    {
        User? existing = await db.Users
            .FirstOrDefaultAsync(u => u.ExternalId == new ExternalUserId(externalId), ct);

        if (existing is not null)
        {
            return existing.Id;
        }

        // Use raw SQL to insert with household_id since the domain model
        // doesn't expose a public method to assign a household after creation.
        UserId userId = UserId.NewId();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO shared.users
                (id, external_id, email, display_name, last_login_at, household_id,
                 created_by, created_at, modified_by, modified_at,
                 is_deleted, deleted_at, deleted_by)
            VALUES
                ({userId.Value}, {externalId}, {email}, {displayName}, {now}, {householdId.Value},
                 {userId.Value}, {now}, {userId.Value}, {now},
                 {false}, {(DateTimeOffset?)null}, {(Guid?)null})", ct);

        logger.LogInformation("Created seed user: {DisplayName} ({Id})", displayName, userId);
        return userId;
    }

    private static CategoryNode AddCategoryOrThrow(
        BudgetAggregate budget,
        string name,
        BudgetFlow flow,
        Attribution? attribution = null)
    {
        Result<CategoryNode, BudgetError> result = budget.AddCategory(name, flow, attribution: attribution);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to add category '{name}': {result.Error}");
        }
        return result.Value;
    }

    private static CategoryNode AddChildOrThrow(
        BudgetAggregate budget,
        string name,
        BudgetCategoryId parentId,
        BudgetFlow flow,
        Attribution? attribution = null)
    {
        Result<CategoryNode, BudgetError> result = budget.AddCategory(name, flow, parentId: parentId, attribution: attribution);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to add child category '{name}': {result.Error}");
        }
        return result.Value;
    }

    private static void BulkCreateOrThrow(
        BudgetAggregate budget,
        BudgetCategoryId categoryId,
        BudgetFlow flow,
        decimal amount,
        PayerSplit payerSplit,
        AttributionSplit attributionSplit)
    {
        Money money = Money.Create(amount, "ZAR").Value;
        Result<IReadOnlyList<BudgetItem>, BudgetError> result =
            budget.BulkCreateItems(categoryId, flow, money, payerSplit, attributionSplit);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to bulk create items: {result.Error}");
        }
    }

    private static void FillForwardOrThrow(
        BudgetAggregate budget,
        BudgetCategoryId categoryId,
        int fromMonth,
        BudgetFlow flow,
        decimal amount,
        PayerSplit payerSplit,
        AttributionSplit attributionSplit)
    {
        Money money = Money.Create(amount, "ZAR").Value;
        Result<IReadOnlyList<BudgetItem>, BudgetError> result =
            budget.FillForward(categoryId, fromMonth, flow, money, payerSplit, attributionSplit);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to fill forward from month {fromMonth}: {result.Error}");
        }
    }

    private static PayerSplit CreatePayerSplit(UserId userId, int percent)
    {
        Result<PayerSplit, BudgetError> result = PayerSplit.Create([new PayerAllocation(userId, percent)]);
        return result.Value;
    }

    private static PayerSplit CreateSharedPayerSplit(UserId primaryId, UserId secondaryId)
    {
        Result<PayerSplit, BudgetError> result = PayerSplit.Create([
            new PayerAllocation(primaryId, 50),
            new PayerAllocation(secondaryId, 50)
        ]);
        return result.Value;
    }

    private static AttributionSplit CreateAttributionSplit(Attribution attribution, int percent)
    {
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create([new AttributionAllocation(attribution, percent)]);
        return result.Value;
    }

    /// <summary>
    /// Simple audit stamp factory for seeding operations (no HTTP context available).
    /// </summary>
    private sealed class SeedAuditStampFactory : IAuditStampFactory, ISoftDeleteStampFactory
    {
        private static readonly UserId SystemUserId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        public AuditStamp CreateStamp() => new(SystemUserId, DateTimeOffset.UtcNow, "budget-seed");
        SoftDeleteStamp ISoftDeleteStampFactory.CreateStamp() => new(SystemUserId, DateTimeOffset.UtcNow);
    }
}
