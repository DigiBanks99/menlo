using Menlo.Common.Errors;
using Menlo.Utilities.Models;

namespace Menlo.Utilities.Handlers.Electricity;

[Trait("Category", "Unit")]
[Trait("Module", "Utilities")]
public class ElectricityUsageQueryTests
{
    private readonly ElectricityUsageQueryHandler _handler;
    private readonly IRepository<ElectricityUsage> _repo;
    private readonly IRepository<ElectricityPurchase> _repoPurchases;

    public ElectricityUsageQueryTests()
    {
        _repo = SetupUsageRepo();
        _repoPurchases = SetupPurchaseRepo();

        _handler = new ElectricityUsageQueryHandler(NullLogger<ElectricityUsageQueryHandler>.Instance, _repo, _repoPurchases);
    }

    [Theory]
    [InlineData("2024-07-01", "2024-07-31", 31)]
    [InlineData("2024-07-01", "2024-07-01", 1)]
    [InlineData("2024-07-01", "2024-07-10", 10)]
    public async Task HandleAsync_Should_ReturnElectricityUsagesForTheDates(string from, string to, int expectedCount)
    {
        ElectricityUsageQuery query = new(DateOnly.Parse(from), DateOnly.Parse(to), TimeSpan.FromHours(2));

        Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError> response = await _handler.HandleAsync(query, CancellationToken.None);

        response.Error.ShouldBeNull();
        ElectricityUsageQueryResponse[] data = response.Data.ShouldNotBeNull().ToArray();
        data.Length.ShouldBe(expectedCount);
    }

    [Fact]
    public async Task HandleAsync_Should_AccountForPurchasesInTheUsage()
    {
        ElectricityUsageQuery query = new(DateOnly.Parse("2024-07-01"), DateOnly.Parse("2024-07-03"), TimeSpan.FromHours(2));

        Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError> response = await _handler.HandleAsync(query, CancellationToken.None);

        response.Error.ShouldBeNull();
        ElectricityUsageQueryResponse[] data = response.Data.ShouldNotBeNull().ToArray();
        ElectricityUsageQueryResponse firstUsage = data[0];
        firstUsage.Units.ShouldBe(138.22m);
        firstUsage.Usage.ShouldBe(155.85m - 138.22m);

        ElectricityUsageQueryResponse secondUsage = data[1];
        secondUsage.Units.ShouldBe(795.98m);
        secondUsage.Usage.ShouldBe(138.22m + 685.00m - 795.98m);
    }

    private static IRepository<ElectricityUsage> SetupUsageRepo()
    {
        ElectricityUsage[] data =
        [
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 6, 30, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 155.85m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 138.22m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 2, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 795.98m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 3, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 739.28m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 4, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 713.44m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 5, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 687.87m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 6, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 664.44m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 7, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 644.59m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 8, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 626.26m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 9, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 607.41m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 10, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 572.43m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 11, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 550.61m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 12, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 535.31m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 13, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 512.49m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 14, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 495.56m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 15, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 478.38m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 16, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 458.73m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 17, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 441.98m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 18, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 424.17m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 19, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 401.59m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 20, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 379.61m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 21, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 351.68m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 22, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 335.55m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 23, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 296.15m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 24, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 279.99m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 25, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 258.90m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 26, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 239.11m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 27, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 222.43m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 28, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 202.39m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 29, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 188.45m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 30, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 166.43m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 7, 31, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 136.36m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 8, 1, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 786.00m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 8, 2, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 767.86m
            },
            new ElectricityUsage
            {
                Date = new DateTimeOffset(2024, 8, 3, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 748.66m
            }
        ];
        IRepository<ElectricityUsage> repo = Substitute.For<IRepository<ElectricityUsage>>();
        repo.GetByQueryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            string[] queryParts = callInfo.ArgAt<string>(0).Split('\n');
            string fromPart = queryParts[5].Split('\'')[1];
            string toPart = queryParts[6].Split('\'')[1];

            DateTimeOffset from = DateTimeOffset.Parse(fromPart);
            DateTimeOffset to = DateTimeOffset.Parse(toPart);
            return data.Where(item => item.Date >= from && item.Date <= to);
        });

        return repo;
    }

    private static IRepository<ElectricityPurchase> SetupPurchaseRepo()
    {
        IRepository<ElectricityPurchase> repo = Substitute.For<IRepository<ElectricityPurchase>>();
        ElectricityPurchase[] data =
        [
            new ElectricityPurchase
            {
                Date = new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 685.00m,
                Cost = 2500.00m
            },
            new ElectricityPurchase
            {
                Date = new DateTimeOffset(2024, 8, 1, 0, 0, 0, TimeSpan.FromHours(2)),
                Units = 685.00m,
                Cost = 2500.00m
            }
        ];

        repo.GetByQueryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            string[] queryParts = callInfo.ArgAt<string>(0).Split('\n');
            string fromPart = queryParts[5].Split('\'')[1];
            string toPart = queryParts[6].Split('\'')[1];

            DateTimeOffset from = DateTimeOffset.Parse(fromPart);
            DateTimeOffset to = DateTimeOffset.Parse(toPart);
            return data.Where(item => item.Date >= from && item.Date <= to);
        });

        return repo;
    }
}
