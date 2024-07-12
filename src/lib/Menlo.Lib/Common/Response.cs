namespace Menlo.Common;

public record Response<TError>
{
    public TError? Error { get; init; }
    public bool IsSuccess => Error == null;
}

public record Response<TResponseModel, TError> : Response<TError>
{
    public TResponseModel? Data { get; init; }
}
