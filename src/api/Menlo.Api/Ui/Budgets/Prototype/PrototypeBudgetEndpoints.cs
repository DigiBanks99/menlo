using Menlo.Api.Ui.Shared;

namespace Menlo.Api.Ui.Budgets.Prototype;

// PROTOTYPE (#596): throwaway route for judging the budget views. Mapped only where the design gallery is
// (Development), and it shows made-up data, so it needs no sign-in.
internal static class PrototypeBudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetPrototype(this IEndpointRouteBuilder app)
    {
        app.MapGet("/budgets/prototype", (HttpContext context) =>
                Render.Page<PrototypeBudgetPage>(context, ProtoSheet.Build(ProtoQuery.From(context.Request))))
            .AllowAnonymous();
        return app;
    }
}
