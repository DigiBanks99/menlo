namespace Menlo.Lib.Budget.Enums;

/// <summary>
/// Tags a category as belonging to the main household, rental property, or a service provider.
/// </summary>
public enum Attribution
{
    /// <summary>Main household expense/income.</summary>
    Main,

    /// <summary>Rental property expense/income.</summary>
    Rental,

    /// <summary>Service provider (e.g., sub-meter intermediary).</summary>
    ServiceProvider,
}
