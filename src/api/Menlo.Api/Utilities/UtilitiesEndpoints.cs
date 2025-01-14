namespace Menlo.Utilities;

public static class UtilitiesEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGroup("electricity").WithName("Electricity").WithOpenApi().MapElectricityEndpoints();
        routes.MapGroup("water").WithName("Water").WithOpenApi().MapWaterEndpoints();
    }
}
