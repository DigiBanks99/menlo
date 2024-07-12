namespace Menlo.Common.Errors;

public record MenloError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required string? Details { get; init; }
}
