using CSharpFunctionalExtensions;
using Menlo.Application.Auth;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Menlo.Application.Users;

/// <summary>
/// Makes sure a <see cref="User"/> record exists for an external identity.
/// </summary>
public sealed class UserProvisioningService(IUserContext userContext) : IUserProvisioningService
{
    /// <summary>
    /// Finds the user for the external id, or creates it. Safe when two first requests race:
    /// the unique <c>ExternalId</c> index rejects the second insert and the existing row is read again.
    /// </summary>
    /// <returns>The user, or an <see cref="InvalidUserDataError"/> when the identity can't make a valid user.</returns>
    public async Task<Result<User, AuthError>> ProvisionOrUpdateAsync(
        string externalId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        ExternalUserId normalizedExternalId = new(externalId);

        User? existingUser = await FindAsync(normalizedExternalId, cancellationToken);
        if (existingUser is not null)
        {
            return existingUser;
        }

        Result<User, AuthError> userResult = User.Create(normalizedExternalId, email, displayName);
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        User newUser = userResult.Value;

        userContext.Users.Add(newUser);
        try
        {
            await userContext.SaveChangesAsync(cancellationToken);
            return newUser;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            userContext.Users.Entry(newUser).State = EntityState.Detached;
            User? winner = await FindAsync(normalizedExternalId, cancellationToken);
            return winner ?? throw new InvalidOperationException("User vanished after a unique violation.", ex);
        }
    }

    private Task<User?> FindAsync(ExternalUserId externalId, CancellationToken cancellationToken) =>
        userContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.ExternalId == externalId, cancellationToken);
}

/// <summary>
/// Finds or creates the <see cref="User"/> for an external identity.
/// </summary>
public interface IUserProvisioningService
{
    /// <returns>The user, or an error when the identity can't make a valid user.</returns>
    Task<Result<User, AuthError>> ProvisionOrUpdateAsync(
        string externalId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);
}
