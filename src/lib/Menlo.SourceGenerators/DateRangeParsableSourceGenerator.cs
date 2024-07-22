using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Menlo.SourceGenerators;

[Generator]
public class DateRangeParsableSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new DateRangeParsableSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        DateRangeParsableSyntaxReceiver? syntaxReceiver = context.SyntaxReceiver as DateRangeParsableSyntaxReceiver;
        if (syntaxReceiver?.RecordToAugment is null)
        {
            return;
        }
        ProcessClass(context, syntaxReceiver.RecordToAugment);
    }

    private static void ProcessClass(GeneratorExecutionContext context, RecordDeclarationSyntax recordSyntax)
    {
        if (recordSyntax.Identifier.SyntaxTree is null)
        {
            return;
        }

        SemanticModel model = context.Compilation.GetSemanticModel(recordSyntax.Identifier.SyntaxTree);
        ISymbol? recordSymbol = model.GetDeclaredSymbol(recordSyntax);
        if (recordSymbol is null)
        {
            return;
        }

        string source =
           $$"""
            // <auto-generated />
            using Menlo.Common;
            using System;
            using System.Globalization;
            using System.Collections.Specialized;
            using System.Diagnostics.CodeAnalysis;
            using System.Web;

            namespace {{recordSymbol.ContainingNamespace.ToDisplayString()}};

            #nullable enable

            public partial record {{recordSymbol.Name}}(DateOnly StartDate, DateOnly? EndDate = null, TimeSpan TimeZone = default)
            : DateRangeQuery(StartDate, EndDate, TimeZone), IParsable<{{recordSymbol.Name}}>
            {
                public static {{recordSymbol.Name}} Parse(string query, IFormatProvider? provider)
                {
                    NameValueCollection parameters = HttpUtility.ParseQueryString(query);

                    string[]? startDateValues = parameters.GetValues("startDate");
                    DateOnly startDate = startDateValues == null || startDateValues.Length == 0
                        ? new DateOnly()
                        : DateOnly.Parse(startDateValues.Last(), provider);

                    string[]? endDateValues = parameters.GetValues("endDate");
                    DateOnly? endDate = endDateValues == null || endDateValues.Length == 0
                        ? null
                        : DateOnly.Parse(endDateValues.Last(), provider);

                    string[]? timeZoneValues = parameters.GetValues("timeZone");
                    TimeSpan timeZone = timeZoneValues == null
                        ? default
                        : TimeSpan.Parse(timeZoneValues.Last(), provider);

                    return new {{recordSymbol.Name}}(startDate, endDate, timeZone);
                }

                public static bool TryParse(
                    [NotNullWhen(true)] string? query,
                    IFormatProvider? provider,
                    [MaybeNullWhen(false)] out {{recordSymbol.Name}} result)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        result = null;
                        return false;
                    }

                    result = Parse(query, provider);
                    return true;
                }

                public static bool TryParse(
                    [NotNullWhen(true)] string? query,
                    [MaybeNullWhen(false)] out {{recordSymbol.Name}} result)
                {
                    return TryParse(query, CultureInfo.CurrentCulture, out result);
                }
            }
            """;

        context.AddSource($"{recordSymbol.Name}.Parsable.cs", source);
    }

    private class DateRangeParsableSyntaxReceiver : ISyntaxReceiver
    {
        public RecordDeclarationSyntax? RecordToAugment { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not RecordDeclarationSyntax cds)
            {
                return;
            }

            foreach (AttributeListSyntax attributeList in cds.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    if (attribute.Name is not IdentifierNameSyntax identifier)
                    {
                        continue;
                    }

                    if (identifier.Identifier.ValueText == "DateRangeParsable")
                    {
                        RecordToAugment = cds;
                    }
                }
            }
        }
    }
}