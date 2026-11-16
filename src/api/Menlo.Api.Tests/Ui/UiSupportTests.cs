using Menlo.Api.Ui.Budgets;
using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Menlo.Api.Tests.Ui;

/// <summary>
/// The small classes behind the pages: reading handler results, the page state in the URL, toasts
/// across redirects, htmx response headers and presentation rules.
/// </summary>
public sealed class UiSupportTests
{
    [Test]
    public void GivenAnOkResult_WhenReadAsAnOutcome()
    {
        Outcome<string> outcome = Outcome<string>.From(Results.Ok("value"));

        ItShouldSucceedWithTheValue();

        void ItShouldSucceedWithTheValue()
        {
            outcome.IsSuccess.ShouldBeTrue();
            outcome.Value.ShouldBe("value");
            outcome.Error.ShouldBeNull();
        }
    }

    [Test]
    public void GivenACreatedResultOfAnotherType_WhenReadAsAnOutcome()
    {
        Outcome<string> outcome = Outcome<string>.From(Results.Created("/somewhere", 42));

        ItShouldSucceedWithoutAValue();

        void ItShouldSucceedWithoutAValue()
        {
            outcome.IsSuccess.ShouldBeTrue();
            outcome.Status.ShouldBe(StatusCodes.Status201Created);
            outcome.Value.ShouldBeNull();
        }
    }

    [Test]
    public void GivenAProblem_WhenReadAsAnOutcome()
    {
        Outcome<string> withDetail = Outcome<string>.From(Results.Problem(detail: "The split must total 100%.", statusCode: 400, title: "Invalid payer split"));
        Outcome<string> titleOnly = Outcome<string>.From(Results.Problem(statusCode: 400, title: "Invalid month"));

        ItShouldPreferTheDetailThenTheTitle();

        void ItShouldPreferTheDetailThenTheTitle()
        {
            withDetail.IsSuccess.ShouldBeFalse();
            withDetail.Error.ShouldBe("The split must total 100%.");
            titleOnly.Error.ShouldBe("Invalid month");
        }
    }

    [Test]
    public void GivenAConflict_WhenReadAsAnOutcome()
    {
        Outcome<string> withMessage = Outcome<string>.From(Results.Conflict(new { Code = "Budget.DuplicateCategoryName", Message = "A category named 'Rent' already exists." }));
        Outcome<string> withProblem = Outcome<string>.From(Results.Conflict(new ProblemDetails { Detail = "Already there." }));
        Outcome<string> bare = Outcome<string>.From(Results.Conflict());

        ItShouldBeAConflictWithTheHandlersMessage();

        void ItShouldBeAConflictWithTheHandlersMessage()
        {
            withMessage.IsConflict.ShouldBeTrue();
            withMessage.Error.ShouldBe("A category named 'Rent' already exists.");
            withProblem.Error.ShouldBe("Already there.");
            bare.Error.ShouldBe("That conflicts with something that already exists.");
        }
    }

    [Test]
    [Arguments(401, "You need to sign in again.")]
    [Arguments(403, "You don't have permission to do that.")]
    [Arguments(404, "That wasn't found. It may have been removed, or the feature isn't switched on.")]
    [Arguments(418, "Something went wrong. Please try again.")]
    public void GivenABareFailure_WhenReadAsAnOutcome_ThenTheMessageFitsTheStatus(int status, string message)
    {
        Outcome<string> outcome = Outcome<string>.From(Results.StatusCode(status));

        outcome.IsSuccess.ShouldBeFalse();
        outcome.IsNotFound.ShouldBe(status == 404);
        outcome.Error.ShouldBe(message);
    }

    [Test]
    public void GivenAnEmptyQueryString_WhenThePageStateIsRead()
    {
        BudgetDetailQuery query = BudgetDetailQuery.From(new QueryCollection(), currentMonth: 7);

        ItShouldDefaultToThisMonthWithNoFilters();

        void ItShouldDefaultToThisMonthWithNoFilters()
        {
            query.Category.ShouldBeNull();
            query.IsMonthly.ShouldBeTrue();
            query.Month.ShouldBe(7);
            query.HasFilter.ShouldBeFalse();
            query.Deleted.ShouldBeFalse();
        }
    }

    [Test]
    public void GivenAFullQueryString_WhenThePageStateIsReadAndWrittenBack()
    {
        Guid budget = Guid.NewGuid();
        Guid category = Guid.NewGuid();
        BudgetDetailQuery query = BudgetDetailQuery.From(
            new QueryCollection(new Dictionary<string, StringValues>
            {
                ["category"] = category.ToString(),
                ["q"] = "  rent & rates ",
                ["flow"] = "Expense",
                ["attribution"] = "Rental",
                ["deleted"] = "true",
                ["view"] = "YEARLY",
                ["month"] = "3"
            }),
            currentMonth: 7);

        string url = query.Url(budget);

        ItShouldReadEveryPart();
        ItShouldWriteAnAddressThatReadsBackTheSame();

        void ItShouldReadEveryPart()
        {
            query.Category.ShouldBe(category);
            query.Search.ShouldBe("rent & rates");
            query.Flow.ShouldBe("Expense");
            query.Attribution.ShouldBe("Rental");
            query.Deleted.ShouldBeTrue();
            query.IsMonthly.ShouldBeFalse();
            query.Month.ShouldBe(3);
            query.HasFilter.ShouldBeTrue();
        }

        void ItShouldWriteAnAddressThatReadsBackTheSame()
        {
            url.ShouldStartWith($"/budgets/{budget}?category={category}&q=rent%20%26%20rates&flow=Expense&attribution=Rental&deleted=true");
            url.ShouldEndWith("&view=yearly&month=3");
        }
    }

    [Test]
    [Arguments("0")]
    [Arguments("13")]
    [Arguments("march")]
    [Arguments("-2")]
    public void GivenAMonthThatIsNotAMonth_WhenThePageStateIsRead_ThenThisMonthIsUsed(string month)
    {
        BudgetDetailQuery query = BudgetDetailQuery.From(
            new QueryCollection(new Dictionary<string, StringValues> { ["month"] = month, ["category"] = "not-a-guid", ["deleted"] = "false" }),
            currentMonth: 7);

        query.Month.ShouldBe(7);
        query.Category.ShouldBeNull();
        query.Deleted.ShouldBeFalse();
    }

    [Test]
    public void GivenAToastSetBeforeARedirect_WhenTheNextPageTakesIt()
    {
        DefaultHttpContext first = new();
        Flash.Set(first, "Budget 2027 created.", MnlTone.Success);
        string cookie = first.Response.Headers.SetCookie.ToString();
        string value = Uri.UnescapeDataString(cookie.Split(';')[0].Split('=', 2)[1]);

        DefaultHttpContext next = new();
        next.Request.Headers.Cookie = $".Menlo.Flash={Uri.EscapeDataString(value)}";
        IReadOnlyList<MnlToastMessage> toasts = Flash.Take(next);

        ItShouldBeStoredInAShortLivedHttpOnlyCookie();
        ItShouldBeShownOnceAndCleared();

        void ItShouldBeStoredInAShortLivedHttpOnlyCookie()
        {
            cookie.ShouldContain("httponly");
            cookie.ShouldContain("secure");
            cookie.ShouldContain("samesite=strict");
            cookie.ShouldContain("max-age=60");
        }

        void ItShouldBeShownOnceAndCleared()
        {
            toasts.ShouldBe([new MnlToastMessage("Budget 2027 created.", MnlTone.Success)]);
            next.Response.Headers.SetCookie.ToString().ShouldContain(".Menlo.Flash=;");
        }
    }

    [Test]
    [Arguments("")]
    [Arguments("not json")]
    [Arguments("{\"Message\":\"\",\"Tone\":0}")]
    [Arguments("null")]
    public void GivenAFlashCookieThatHoldsNoMessage_WhenTaken_ThenThereIsNoToast(string value)
    {
        DefaultHttpContext context = new();
        if (value.Length > 0)
        {
            context.Request.Headers.Cookie = $".Menlo.Flash={Uri.EscapeDataString(value)}";
        }

        Flash.Take(context).ShouldBeEmpty();
    }

    [Test]
    public void GivenAnHtmxRequest_WhenNavigatingAfterAnAction()
    {
        DefaultHttpContext context = new();
        context.Request.Headers["HX-Request"] = "true";

        IResult result = Navigate.To(context, "/budgets/123");

        ItShouldTellHtmxWhereToGoWithoutAFullReload();

        void ItShouldTellHtmxWhereToGoWithoutAFullReload()
        {
            result.ShouldBeOfType<NoContent>();
            context.Response.Headers["HX-Location"].ToString().ShouldBe("{\"path\":\"/budgets/123\"}");
        }
    }

    [Test]
    public void GivenAPlainFormPost_WhenNavigatingAfterAnAction()
    {
        DefaultHttpContext context = new();

        IResult result = Navigate.To(context, "/budgets/123");

        ItShouldRedirect();

        void ItShouldRedirect()
        {
            result.ShouldBeOfType<RedirectHttpResult>().Url.ShouldBe("/budgets/123");
            context.Response.Headers.ContainsKey("HX-Location").ShouldBeFalse();
        }
    }

    [Test]
    public void GivenEvents_WhenWrittenToAResponse()
    {
        DefaultHttpContext context = new();
        DefaultHttpContext quiet = new();

        IResult result = new HxResponse()
            .Toast("Category created.")
            .ClosePanel()
            .Raise(UiEvents.CategoriesChanged)
            .NoContent(context);
        new HxResponse().Apply(quiet.Response);

        ItShouldSendThemInOneHeader();
        ItShouldSendNoHeaderWhenThereAreNoEvents();

        void ItShouldSendThemInOneHeader()
        {
            result.ShouldBeOfType<NoContent>();
            context.Response.Headers["HX-Trigger"].ToString().ShouldBe(
                "{\"mnl:toast\":{\"message\":\"Category created.\",\"tone\":\"success\"},\"mnl:close-panel\":true,\"categories-changed\":true}");
        }

        void ItShouldSendNoHeaderWhenThereAreNoEvents() => quiet.Response.Headers.ContainsKey("HX-Trigger").ShouldBeFalse();
    }

    [Test]
    public void GivenRequestHeaders_WhenAskedWhetherAFragmentIsWanted()
    {
        DefaultHttpContext plain = new();
        DefaultHttpContext fragment = new();
        fragment.Request.Headers["HX-Request"] = "true";
        DefaultHttpContext boosted = new();
        boosted.Request.Headers["HX-Request"] = "true";
        boosted.Request.Headers["HX-Boosted"] = "true";

        ItShouldBeAFragmentOnlyForAnUnboostedHtmxRequest();

        void ItShouldBeAFragmentOnlyForAnUnboostedHtmxRequest()
        {
            HxRequest.IsFragment(plain.Request).ShouldBeFalse();
            HxRequest.IsFragment(fragment.Request).ShouldBeTrue();
            HxRequest.IsFragment(boosted.Request).ShouldBeFalse();
        }
    }

    [Test]
    public void GivenFormErrors_WhenAddedAndRead()
    {
        FormErrors errors = new FormErrors()
            .Add("name", "Name is required")
            .Add("NAME", "A second message")
            .AddForm("The category couldn't be saved.")
            .AddForm("A second form message");

        ItShouldKeepTheFirstMessageForEachFieldAndTheForm();
        ItShouldStartValid();

        void ItShouldKeepTheFirstMessageForEachFieldAndTheForm()
        {
            errors.IsValid.ShouldBeFalse();
            errors.For("name").ShouldBe("Name is required");
            errors.Has("Name").ShouldBeTrue();
            errors.For("description").ShouldBeNull();
            errors.Form.ShouldBe("The category couldn't be saved.");
        }

        void ItShouldStartValid() => FormErrors.None.IsValid.ShouldBeTrue();
    }

    [Test]
    public void GivenStatusesPercentagesAndMonths_WhenPresented()
    {
        ItShouldToneAStatus();
        ItShouldToneSpending();
        ItShouldToneAFlow();
        ItShouldNameAMonth();
        ItShouldCountCategories();
        ItShouldLabelAnAttribution();

        void ItShouldToneAStatus()
        {
            BudgetPresentation.StatusTone("Active").ShouldBe(MnlTone.Success);
            BudgetPresentation.StatusTone("Closed").ShouldBe(MnlTone.Neutral);
            BudgetPresentation.StatusTone("Draft").ShouldBe(MnlTone.Warning);
        }

        void ItShouldToneSpending()
        {
            BudgetPresentation.SpendTone(79.9m).ShouldBe(MnlTone.Success);
            BudgetPresentation.SpendTone(80m).ShouldBe(MnlTone.Warning);
            BudgetPresentation.SpendTone(95m).ShouldBe(MnlTone.Error);
        }

        void ItShouldToneAFlow()
        {
            BudgetPresentation.FlowTone("Income").ShouldBe(MnlTone.Success);
            BudgetPresentation.FlowTone("Expense").ShouldBe(MnlTone.Info);
        }

        void ItShouldNameAMonth()
        {
            BudgetPresentation.MonthName(1).ShouldBe("January");
            BudgetPresentation.MonthName(12).ShouldBe("December");
            BudgetPresentation.MonthName(13).ShouldBe("Month 13");
        }

        void ItShouldCountCategories()
        {
            BudgetPresentation.CategoryCount(0).ShouldBe("0 categories");
            BudgetPresentation.CategoryCount(1).ShouldBe("1 category");
            BudgetPresentation.CategoryCount(6).ShouldBe("6 categories");
        }

        void ItShouldLabelAnAttribution()
        {
            CategoryOptions.AttributionLabel("ServiceProvider").ShouldBe("Service Provider");
            CategoryOptions.AttributionLabel("Unknown").ShouldBe("Unknown");
        }
    }
}
