namespace Menlo.Api.OpenApi;

internal static  class OpenApiServiceCollectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddMenloOpenApi()
        {
            builder.Services.AddOpenApi(options => options.AddDocumentTransformer<CookieSecuritySchemeTransformer>());
            return builder;
        }
    }
}
