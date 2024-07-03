using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Shouldly;

namespace Menlo.FeatureManagement;

public class ModuleFeatureFilterTests
{
    private readonly IConfiguration _configuration;
    private readonly FeatureFilterEvaluationContext _context;
    private readonly ModuleFeatureFilter _featureFilter;

    public ModuleFeatureFilterTests()
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "TestModule", "true" },
            { "DisabledModule", "false" }
        });
        _configuration = configurationBuilder.Build();

        _context = new FeatureFilterEvaluationContext()
        {
            Parameters = _configuration,
            FeatureName = "Modules"
        };

        _featureFilter = new ModuleFeatureFilter();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnTrue_WhenModuleIsEnabled()
    {
        // Arrange
        string moduleName = "TestModule";

        // Act
        bool isEnabled = await _featureFilter.EvaluateAsync(_context, moduleName);

        // Assert
        isEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnFalse_WhenModuleIsDisabled()
    {
        // Arrange
        string moduleName = "DisabledModule";

        // Act
        bool isEnabled = await _featureFilter.EvaluateAsync(_context, moduleName);

        // Assert
        isEnabled.ShouldBeFalse();
    }
}
