using Menlo.Api.Receipts;
using Menlo.Api.Receipts.Photos;
using Menlo.Api.Tests.Fixtures;
using Npgsql;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Menlo.Api.Tests.Receipts;

[ClassDataSource<PostgresApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Receipts")]
public sealed class ReceiptEndpointTests(PostgresApiFixture fixture)
{
    private const int Year = 2026;
    private const string Receipts = "/api/budgets/2026/receipts";

    private static CancellationToken CancellationToken => TestContext.Current!.Execution.CancellationToken;

    [Test]
    public async Task AMemberSendsAPhotoOfASlip()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);

        HttpResponseMessage response = await client.PostAsync(
            Receipts, Photos(budgetView: "/budgets/2026?month=11", TestPhotos.Jpeg()), CancellationToken);
        ReceiptDto? sent = await response.Content.ReadFromJsonAsync<ReceiptDto>(CancellationToken);
        ReceiptDto? fetched = await client.GetFromJsonAsync<ReceiptDto>(response.Headers.Location, CancellationToken);

        ItShouldBeAccepted();
        ItShouldWaitToBeRead();
        ItShouldKeepWhereItWasSentFrom();
        ItShouldBeStored();

        void ItShouldBeAccepted()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            response.Headers.Location.ShouldBe(new Uri($"{Receipts}/{sent!.Id}", UriKind.Relative));
        }

        void ItShouldWaitToBeRead() => sent.ShouldNotBeNull().ShouldSatisfyAllConditions(
            r => r.Status.ShouldBe("Waiting"),
            r => r.Year.ShouldBe(Year),
            r => r.Photos.ShouldBe(1),
            r => r.Attempts.ShouldBe(0),
            r => r.Reading.ShouldBeNull(),
            r => r.TakenBy.ShouldNotBe(Guid.Empty));

        void ItShouldKeepWhereItWasSentFrom() => sent!.BudgetView.ShouldBe("/budgets/2026?month=11");

        // PostgreSQL keeps time to the microsecond
        void ItShouldBeStored() => fetched.ShouldNotBeNull().ShouldSatisfyAllConditions(
            r => r.ShouldBe(sent! with { TakenAt = r.TakenAt }),
            r => r.TakenAt.ShouldBe(sent!.TakenAt, TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public async Task AMemberSendsThreePhotosInDifferentFormats()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);

        HttpResponseMessage response = await client.PostAsync(
            Receipts, Photos(null, TestPhotos.Heic(), TestPhotos.Png(), TestPhotos.WebP()), CancellationToken);
        ReceiptDto sent = (await response.Content.ReadFromJsonAsync<ReceiptDto>(CancellationToken))!;
        List<HttpResponseMessage> photos = [];
        for (int n = 1; n <= 3; n++)
        {
            photos.Add(await client.GetAsync($"{Receipts}/{sent.Id}/photos/{n}", CancellationToken));
        }

        byte[] first = await photos[0].Content.ReadAsByteArrayAsync(CancellationToken);

        ItShouldBeAccepted();
        ItShouldKeepEveryPhoto();
        ItShouldServeEachAsAJpeg();
        ItShouldConvertTheHeic();

        void ItShouldBeAccepted() => response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        void ItShouldKeepEveryPhoto() => sent.Photos.ShouldBe(3);

        void ItShouldServeEachAsAJpeg() => photos.ShouldAllBe(photo =>
            photo.StatusCode == HttpStatusCode.OK && photo.Content.Headers.ContentType!.MediaType == "image/jpeg");

        void ItShouldConvertTheHeic()
        {
            ImageSniffer.Sniff(first).ShouldBe(ImageKind.Jpeg);
            TestPhotos.Describe(first).Width.ShouldBe(TestPhotos.HeicWidth);
        }
    }

    [Test]
    public async Task AMemberSendsAFourthPhoto()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);

        HttpResponseMessage response = await client.PostAsync(
            Receipts, Photos(null, TestPhotos.Jpeg(), TestPhotos.Jpeg(), TestPhotos.Jpeg(), TestPhotos.Jpeg()), CancellationToken);
        string problem = await response.Content.ReadAsStringAsync(CancellationToken);
        IReadOnlyList<ReceiptSummaryDto> receipts = await ListAsync(client);

        ItShouldBeRefused(response);
        ItShouldSayAtMostThree();
        ItShouldKeepNothing(receipts);

        void ItShouldSayAtMostThree() => problem.ShouldContain("at most 3 photos");
    }

    [Test]
    public async Task AMemberSendsNoPhoto()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);
        MultipartFormDataContent blank = Photos("/budgets/2026");
        // What a browser sends for a file input left blank
        ByteArrayContent nothing = new([]);
        nothing.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "photos", FileName = "\"\"" };
        nothing.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        blank.Add(nothing);

        HttpResponseMessage response = await client.PostAsync(Receipts, blank, CancellationToken);
        string problem = await response.Content.ReadAsStringAsync(CancellationToken);
        IReadOnlyList<ReceiptSummaryDto> receipts = await ListAsync(client);

        ItShouldBeRefused(response);
        ItShouldAskForAPhoto();
        ItShouldKeepNothing(receipts);

        void ItShouldAskForAPhoto() => problem.ShouldContain("at least one photo");
    }

    [Test]
    [Arguments("%PDF-1.7 a document")]
    [Arguments("")]
    public async Task AMemberSendsAFileThatIsntAPhoto(string content)
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);

        HttpResponseMessage response = await client.PostAsync(
            Receipts, Photos(null, TestPhotos.Jpeg(), Encoding.ASCII.GetBytes(content)), CancellationToken);
        string problem = await response.Content.ReadAsStringAsync(CancellationToken);
        IReadOnlyList<ReceiptSummaryDto> receipts = await ListAsync(client);

        ItShouldBeRefused(response);
        ItShouldSayWhichFile();
        ItShouldKeepNothing(receipts);

        void ItShouldSayWhichFile() => problem.ShouldContain("File 2 isn't a photo");
    }

    [Test]
    public async Task APhotoIsSentAsSomethingOtherThanAForm()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);
        ByteArrayContent raw = new(TestPhotos.Jpeg());
        raw.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        HttpResponseMessage response = await client.PostAsync(Receipts, raw, CancellationToken);

        ItShouldBeRefused(response);
    }

    [Test]
    [Arguments("https://example.com/budgets")]
    [Arguments("//example.com/budgets")]
    [Arguments("budgets/2026")]
    // A browser drops tabs and line breaks from a URL, so these would become //example.com
    [Arguments("/\t/example.com")]
    [Arguments("/\n/example.com")]
    public async Task AReceiptIsSentFromOutsideMenlo(string budgetView)
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);

        HttpResponseMessage response = await client.PostAsync(Receipts, Photos(budgetView, TestPhotos.Jpeg()), CancellationToken);
        ReceiptDto? sent = await response.Content.ReadFromJsonAsync<ReceiptDto>(CancellationToken);

        ItShouldBeAccepted();
        ItShouldNotGoBackThere();

        void ItShouldBeAccepted() => response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        void ItShouldNotGoBackThere() => sent.ShouldNotBeNull().BudgetView.ShouldBeNull();
    }

    [Test]
    public async Task ReceiptsAreListed()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);
        ReceiptDto older = await SendAsync(client, Receipts, TestPhotos.Jpeg(), TestPhotos.Png());
        ReceiptDto newer = await SendAsync(client, Receipts, TestPhotos.Jpeg());
        await SendAsync(client, "/api/budgets/2027/receipts", TestPhotos.Jpeg());

        IReadOnlyList<ReceiptSummaryDto> receipts = await ListAsync(client);

        ItShouldListOnlyTheYearsReceiptsNewestFirst();
        ItShouldShowEachOnesStatusAndPhotos();

        void ItShouldListOnlyTheYearsReceiptsNewestFirst() =>
            receipts.Select(r => r.Id).ShouldBe([newer.Id, older.Id]);

        void ItShouldShowEachOnesStatusAndPhotos() => receipts.ShouldSatisfyAllConditions(
            list => list.ShouldAllBe(r => r.Status == "Waiting" && r.Year == Year),
            list => list.Select(r => r.Photos).ShouldBe([1, 2]));
    }

    [Test]
    public async Task AReceiptIsDiscarded()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);
        ReceiptDto sent = await SendAsync(client, Receipts, TestPhotos.Jpeg());

        HttpResponseMessage response = await client.DeleteAsync($"{Receipts}/{sent.Id}", CancellationToken);
        HttpResponseMessage receipt = await client.GetAsync($"{Receipts}/{sent.Id}", CancellationToken);
        HttpResponseMessage photo = await client.GetAsync($"{Receipts}/{sent.Id}/photos/1", CancellationToken);
        HttpResponseMessage again = await client.DeleteAsync($"{Receipts}/{sent.Id}", CancellationToken);

        ItShouldBeDiscarded();
        ItShouldBeGonePhotosAndAll();
        ItShouldNotBeDiscardedTwice();

        void ItShouldBeDiscarded() => response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        void ItShouldBeGonePhotosAndAll()
        {
            receipt.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            photo.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        void ItShouldNotBeDiscardedTwice() => again.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AReceiptIsAskedForUnderAnotherYear()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);
        ReceiptDto sent = await SendAsync(client, Receipts, TestPhotos.Jpeg());
        const string otherYear = "/api/budgets/2025/receipts";

        HttpStatusCode[] answers =
        [
            (await client.GetAsync($"{otherYear}/{sent.Id}", CancellationToken)).StatusCode,
            (await client.GetAsync($"{otherYear}/{sent.Id}/photos/1", CancellationToken)).StatusCode,
            (await client.DeleteAsync($"{otherYear}/{sent.Id}", CancellationToken)).StatusCode
        ];
        HttpResponseMessage stillThere = await client.GetAsync($"{Receipts}/{sent.Id}", CancellationToken);

        ItShouldNotBeFound();
        ItShouldStillBeThere();

        void ItShouldNotBeFound() => answers.ShouldAllBe(status => status == HttpStatusCode.NotFound);

        void ItShouldStillBeThere() => stillThere.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    [Arguments(0)]
    [Arguments(2)]
    public async Task APhotoThatIsntThereIsAskedFor(int n)
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);
        ReceiptDto sent = await SendAsync(client, Receipts, TestPhotos.Jpeg());

        HttpResponseMessage response = await client.GetAsync($"{Receipts}/{sent.Id}/photos/{n}", CancellationToken);

        ItShouldNotBeFound();

        void ItShouldNotBeFound() => response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AReceiptThatIsntThereIsAskedFor()
    {
        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"{Receipts}/{Guid.NewGuid()}", CancellationToken);

        ItShouldNotBeFound();

        void ItShouldNotBeFound() => response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task TheBudgetIsSwitchedOff()
    {
        await using TestWebApplicationFactory factory = CreateFactory(budgetEnabled: false);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: CancellationToken);
        Guid id = Guid.NewGuid();

        HttpStatusCode[] answers =
        [
            (await client.PostAsync(Receipts, Photos(null, TestPhotos.Jpeg()), CancellationToken)).StatusCode,
            (await client.GetAsync(Receipts, CancellationToken)).StatusCode,
            (await client.GetAsync($"{Receipts}/{id}", CancellationToken)).StatusCode,
            (await client.GetAsync($"{Receipts}/{id}/photos/1", CancellationToken)).StatusCode,
            (await client.DeleteAsync($"{Receipts}/{id}", CancellationToken)).StatusCode
        ];

        ItShouldNotBeFound();

        void ItShouldNotBeFound() => answers.ShouldAllBe(status => status == HttpStatusCode.NotFound);
    }

    [Test]
    public async Task SomeoneWhoIsNotSignedInAsksForTheReceipts()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            ConfigurationOverrides = new Dictionary<string, string?> { ["Features:Budget"] = "true" }
        };
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(Receipts, CancellationToken);

        ItShouldBeUnauthorised();

        void ItShouldBeUnauthorised() => response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static MultipartFormDataContent Photos(string? budgetView, params byte[][] photos)
    {
        MultipartFormDataContent form = [];
        if (budgetView is not null)
        {
            form.Add(new StringContent(budgetView), ReceiptUpload.BudgetViewField);
        }

        for (int index = 0; index < photos.Length; index++)
        {
            ByteArrayContent photo = new(photos[index]);
            // What a browser sends for a phone photo, whatever it really is: neither is trusted
            photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(photo, "photos", $"photo-{index + 1}.jpg");
        }

        return form;
    }

    private static async Task<ReceiptDto> SendAsync(HttpClient client, string receipts, params byte[][] photos)
    {
        HttpResponseMessage response = await client.PostAsync(receipts, Photos(null, photos), CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return (await response.Content.ReadFromJsonAsync<ReceiptDto>(CancellationToken))!;
    }

    private static async Task<IReadOnlyList<ReceiptSummaryDto>> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<ReceiptSummaryDto>>(Receipts, CancellationToken))!;

    private static void ItShouldBeRefused(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    private static void ItShouldKeepNothing(IReadOnlyList<ReceiptSummaryDto> receipts) => receipts.ShouldBeEmpty();

    // An API host on the shared database, with the budget switched on unless asked otherwise. The receipts are emptied
    // first (these tests run one at a time), so no test sees another's.
    private TestWebApplicationFactory CreateFactory(bool budgetEnabled = true)
    {
        using (NpgsqlConnection connection = new(fixture.ConnectionString))
        {
            connection.Open();
            using NpgsqlCommand truncate = connection.CreateCommand();
            truncate.CommandText = "TRUNCATE financial.receipts CASCADE";
            truncate.ExecuteNonQuery();
        }

        return fixture.CreateFactory(new Dictionary<string, string?>
        {
            ["Features:Budget"] = budgetEnabled ? "true" : "false"
        });
    }
}
