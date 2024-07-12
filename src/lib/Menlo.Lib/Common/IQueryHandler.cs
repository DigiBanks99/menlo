using Menlo.Common.Errors;

namespace Menlo.Common;

public interface IQueryHandler<TRequest, TResponseModel>
{
    Task<Response<TResponseModel, MenloError>> HandleAsync(TRequest query, CancellationToken cancellationToken);
}
