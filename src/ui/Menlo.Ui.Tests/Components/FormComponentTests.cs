namespace Menlo.Ui.Tests.Components;

public sealed class FormComponentTests
{
    [Test]
    public async Task GivenAFieldWithAnError_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlFormField>(new Dictionary<string, object?>
        {
            ["Label"] = "Name",
            ["For"] = "name",
            ["Required"] = true,
            ["Hint"] = "Shown in the tree.",
            ["Error"] = "Name is required"
        });

        ItShouldLabelTheControl();
        ItShouldShowTheErrorWhereTheControlPointsTo();
        ItShouldHideTheHint();

        void ItShouldLabelTheControl()
        {
            IElement label = document.Find("label");
            label.GetAttribute("for").ShouldBe("name");
            label.TextContent.ShouldContain("Name");
            label.TextContent.ShouldContain("*");
        }

        void ItShouldShowTheErrorWhereTheControlPointsTo()
        {
            IElement error = document.Find("#name-error");
            error.TextContent.ShouldBe("Name is required");
            error.GetAttribute("data-testid").ShouldBe("error-name");
        }

        void ItShouldHideTheHint() => document.QuerySelector(".mnl-form-field__hint").ShouldBeNull();
    }

    [Test]
    public async Task GivenAValidField_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlFormField>(new Dictionary<string, object?>
        {
            ["Label"] = "Name",
            ["For"] = "name",
            ["Hint"] = "Shown in the tree."
        });

        ItShouldShowTheHintAndNoError();

        void ItShouldShowTheHintAndNoError()
        {
            document.Find(".mnl-form-field__hint").TextContent.ShouldBe("Shown in the tree.");
            document.QuerySelector(".mnl-form-field__error").ShouldBeNull();
            document.Find(".mnl-form-field").HasAttribute("data-has-error").ShouldBeFalse();
        }
    }

    [Test]
    public async Task GivenAnInvalidInput_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlInput>(new Dictionary<string, object?>
        {
            ["Name"] = "name",
            ["Value"] = "Groceries",
            ["Invalid"] = true,
            ["min"] = "1"
        });

        IElement input = document.Find("input.mnl-input");

        ItShouldUseItsNameAsItsId();
        ItShouldAnnounceTheErrorMessage();
        ItShouldPassExtraAttributesToTheInput();

        void ItShouldUseItsNameAsItsId()
        {
            input.GetAttribute("id").ShouldBe("name");
            input.GetAttribute("name").ShouldBe("name");
            input.GetAttribute("value").ShouldBe("Groceries");
        }

        void ItShouldAnnounceTheErrorMessage()
        {
            input.GetAttribute("aria-invalid").ShouldBe("true");
            input.GetAttribute("aria-describedby").ShouldBe("name-error");
        }

        void ItShouldPassExtraAttributesToTheInput() => input.GetAttribute("min").ShouldBe("1");
    }

    [Test]
    public async Task GivenASelectWithAPlaceholderAndAValue_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlSelect>(new Dictionary<string, object?>
        {
            ["Name"] = "budgetFlow",
            ["Value"] = "Expense",
            ["Placeholder"] = "-- Select --",
            ["Options"] = new MnlOption[] { new("Income", "Income"), new("Expense", "Expense") }
        });

        ItShouldOfferAnEmptyChoiceFirst();
        ItShouldSelectTheValue();

        void ItShouldOfferAnEmptyChoiceFirst()
        {
            IElement first = document.QuerySelectorAll("option")[0];
            first.GetAttribute("value").ShouldBe(string.Empty);
            first.TextContent.ShouldBe("-- Select --");
            first.HasAttribute("selected").ShouldBeFalse();
        }

        void ItShouldSelectTheValue() =>
            document.Find("option[selected]").GetAttribute("value").ShouldBe("Expense");
    }

    [Test]
    public async Task GivenAnAmount_WhenTheAmountInputIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlAmountInput>(new Dictionary<string, object?>
        {
            ["Name"] = "plannedAmount",
            ["Value"] = 1250.5m
        });

        ItShouldShowTheRandSymbol();
        ItShouldShowAnEditableNumber();

        void ItShouldShowTheRandSymbol() => document.Find(".mnl-amount-input__prefix").TextContent.ShouldBe("R");

        void ItShouldShowAnEditableNumber()
        {
            IElement input = document.Find("input");
            input.GetAttribute("value").ShouldBe("1250.5");
            input.GetAttribute("inputmode").ShouldBe("decimal");
        }
    }

    [Test]
    public async Task GivenASwitch_WhenTheCheckboxIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlCheckbox>(new Dictionary<string, object?>
        {
            ["Name"] = "deleted",
            ["Label"] = "Show deleted",
            ["Checked"] = true,
            ["Switch"] = true
        });

        ItShouldBeACheckedSwitchWithALabel();

        void ItShouldBeACheckedSwitchWithALabel()
        {
            IElement input = document.Find("label.mnl-checkbox input[type='checkbox']");
            input.GetAttribute("role").ShouldBe("switch");
            input.HasAttribute("checked").ShouldBeTrue();
            document.Find("label").TextContent.ShouldContain("Show deleted");
        }
    }

    [Test]
    public async Task GivenHtmxAttributes_WhenTheSegmentedControlIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlSegmented>(new Dictionary<string, object?>
        {
            ["Label"] = "Summary period",
            ["Value"] = "monthly",
            ["Segments"] = new MnlSegment[] { new("yearly", "Yearly", "/b?view=yearly"), new("monthly", "Monthly", "/b?view=monthly") },
            ["hx-target"] = "#region",
            ["data-testid"] = "views"
        });

        IHtmlCollection<IElement> links = document.QuerySelectorAll("a");

        ItShouldMarkTheSelectedChoice();
        ItShouldMakeEachLinkAnHtmxRequestToItsOwnAddress();
        ItShouldKeepTheTestIdOnTheGroup();

        void ItShouldMarkTheSelectedChoice()
        {
            links[0].HasAttribute("aria-current").ShouldBeFalse();
            links[1].GetAttribute("aria-current").ShouldBe("true");
        }

        void ItShouldMakeEachLinkAnHtmxRequestToItsOwnAddress()
        {
            links[0].GetAttribute("hx-get").ShouldBe("/b?view=yearly");
            links[0].GetAttribute("hx-target").ShouldBe("#region");
            links[0].GetAttribute("href").ShouldBe("/b?view=yearly");
        }

        void ItShouldKeepTheTestIdOnTheGroup()
        {
            document.Find("[role='group']").GetAttribute("data-testid").ShouldBe("views");
            links[0].HasAttribute("data-testid").ShouldBeFalse();
        }
    }
}
