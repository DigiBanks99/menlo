using Menlo.Api.Ui.Shared;

namespace Menlo.Api.Features;

/// <summary>
/// Marks an endpoint as part of a feature that a configuration flag switches on. While the flag is off,
/// <see cref="FeatureFlags.UseMenloFeatureFlags"/> answers 404 before the endpoint runs, so before its body is read.
/// </summary>
/// <param name="Flag">The configuration key of the flag, such as <see cref="PageContext.BudgetFeatureFlag"/>.</param>
internal sealed record RequiresFeatureMetadata(string Flag);

/// <summary>
/// Switches endpoints off with a configuration flag. Mark them with <see cref="RequireFeature{TBuilder}"/> and add
/// <see cref="UseMenloFeatureFlags"/> to the pipeline.
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Answers 404 on these endpoints while the configuration flag <paramref name="flag"/> is off.
    /// </summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string flag)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new RequiresFeatureMetadata(flag));

    /// <summary>Answers 404 on these endpoints while <see cref="PageContext.BudgetFeatureFlag"/> is off.</summary>
    public static TBuilder RequireBudgetFeature<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireFeature(PageContext.BudgetFeatureFlag);

    /// <summary>
    /// Answers 404 for a request routed to an endpoint whose feature is switched off. It runs after routing and
    /// before the endpoint, so a request whose body can't be bound still gets the 404. Call it after
    /// <c>UseAuthorization()</c>, so someone who isn't signed in still gets 401.
    /// </summary>
    public static IApplicationBuilder UseMenloFeatureFlags(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.GetEndpoint()?.Metadata.GetMetadata<RequiresFeatureMetadata>() is { } feature
                && !context.RequestServices.GetRequiredService<IConfiguration>().GetValue(feature.Flag, defaultValue: false))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(context);
        });
}
