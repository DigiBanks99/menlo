using Menlo.Common.Errors;

namespace Menlo.Common;

public interface ICommandHandler<TRequest, TResponse>
{
    Task<Response<TResponse, MenloError>> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
