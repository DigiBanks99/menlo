namespace Menlo.Api.Ui.Shared;

/// <summary>
/// The validation messages of a submitted form: one per field, plus one for the form as a whole.
/// A form's view model carries it, and the form passes each message to its MnlFormField.
/// </summary>
public sealed class FormErrors
{
    private readonly Dictionary<string, string> _fields = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A form with no errors.</summary>
    public static FormErrors None => new();

    /// <summary>A message that isn't about one field, shown above the actions.</summary>
    public string? Form { get; private set; }

    /// <summary>Whether there are no errors.</summary>
    public bool IsValid => _fields.Count == 0 && Form is null;

    /// <summary>Records a message for a field. The first message for a field wins.</summary>
    public FormErrors Add(string field, string message)
    {
        _fields.TryAdd(field, message);
        return this;
    }

    /// <summary>Records a message for the form as a whole.</summary>
    public FormErrors AddForm(string message)
    {
        Form ??= message;
        return this;
    }

    /// <summary>The message for a field, or null when it is valid.</summary>
    public string? For(string field) => _fields.GetValueOrDefault(field);

    /// <summary>Whether a field has a message.</summary>
    public bool Has(string field) => _fields.ContainsKey(field);
}
