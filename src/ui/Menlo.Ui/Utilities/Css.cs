using System.Text;

namespace Menlo.Ui.Utilities;

/// <summary>
/// Helpers for building class names and data-attribute values. Pure functions: no markup, no state.
/// </summary>
public static class Css
{
    /// <summary>
    /// Joins class names, skipping null and blank ones.
    /// </summary>
    public static string Classes(params string?[] names) =>
        string.Join(' ', names.Where(name => !string.IsNullOrWhiteSpace(name)));

    /// <summary>
    /// Returns the kebab-case form of an enum value, for use in a <c>data-*</c> attribute: <c>FullWidth</c> becomes <c>full-width</c>.
    /// </summary>
    public static string Token<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();
        StringBuilder builder = new(name.Length + 4);

        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];
            if (char.IsUpper(current) && i > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns <c>"true"</c> when set and null otherwise, so the attribute is left out when false.
    /// </summary>
    public static string? Flag(bool value) => value ? "true" : null;
}
