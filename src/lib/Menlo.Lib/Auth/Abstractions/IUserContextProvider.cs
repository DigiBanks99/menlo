using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;

namespace Menlo.Lib.Auth.Abstractions;

public interface IUserContextProvider
{
    Task<Result<UserContext, AuthError>> GetUserContextAsync(CancellationToken cancellationToken = default);
}
