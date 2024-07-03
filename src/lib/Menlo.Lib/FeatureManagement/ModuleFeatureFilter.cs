using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace Menlo.FeatureManagement;

[FilterAlias(nameof(ModuleFeatureFilter))]
internal class ModuleFeatureFilter : IContextualFeatureFilter<string>
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, string moduleName)
    {
        bool isEnabled = context.Parameters.GetValue(moduleName, false);

        return Task.FromResult(isEnabled);
    }
}
