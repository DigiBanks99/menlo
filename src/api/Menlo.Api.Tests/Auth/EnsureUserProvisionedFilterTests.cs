using CSharpFunctionalExtensions;
using Menlo.Api.Auth;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Claims;

namespace Menlo.Api.Tests.Auth;

public sealed class EnsureUserProvisionedFilterTests
{
    private readonly IUserProvisioningService _provisioning = Substitute.For<IUserProvisioningService>();

    [Test]
    public async Task GivenClaimsWithoutExternalId_WhenFiltering_Returns403AndDoesNotProvision()
    {
        EnsureUserProvisionedFilter sut = CreateSut();

        (object? result, bool reachedHandler) = await InvokeAsync(sut, new Claim(ClaimTypes.Email, "a@menlo.test"));

        ItShouldHaveReturnedUserNotProvisioned(result);
        reachedHandler.ShouldBeFalse();
        await _provisioning.DidNotReceiveWithAnyArgs().ProvisionOrUpdateAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task GivenClaimsWithBlankEmail_WhenFiltering_Returns403AndDoesNotProvision()
    {
        EnsureUserProvisionedFilter sut = CreateSut();

        (object? result, bool reachedHandler) = await InvokeAsync(
            sut, new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "  "));

        ItShouldHaveReturnedUserNotProvisioned(result);
        reachedHandler.ShouldBeFalse();
        await _provisioning.DidNotReceiveWithAnyArgs().ProvisionOrUpdateAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task GivenProvisioningReturnsAnError_WhenFiltering_Returns403AndSkipsTheHandler()
    {
        _provisioning
            .ProvisionOrUpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User, AuthError>(new InvalidUserDataError("bad data")));
        EnsureUserProvisionedFilter sut = CreateSut();

        (object? result, bool reachedHandler) = await InvokeAsync(
            sut, new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test"));

        ItShouldHaveReturnedUserNotProvisioned(result);
        reachedHandler.ShouldBeFalse();
    }

    [Test]
    public async Task GivenProvisioningThrows_WhenFiltering_TheExceptionIsNotSwallowed()
    {
        _provisioning
            .ProvisionOrUpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Result<User, AuthError>>(_ => throw new InvalidOperationException("database is down"));
        EnsureUserProvisionedFilter sut = CreateSut();

        Func<Task> act = async () => await InvokeAsync(
            sut, new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test"));

        InvalidOperationException error = await act.ShouldThrowAsync<InvalidOperationException>();
        error.Message.ShouldBe("database is down");
    }

    [Test]
    public async Task GivenNoNameClaim_WhenFiltering_ProvisionsWithTheEmailAsDisplayName()
    {
        _provisioning
            .ProvisionOrUpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => User.Create(new(ci.ArgAt<string>(0)), ci.ArgAt<string>(1), ci.ArgAt<string>(2)));
        EnsureUserProvisionedFilter sut = CreateSut();

        (_, bool reachedHandler) = await InvokeAsync(
            sut, new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test"));

        reachedHandler.ShouldBeTrue();
        await _provisioning.Received(1).ProvisionOrUpdateAsync("ext-1", "a@menlo.test", "a@menlo.test", Arg.Any<CancellationToken>());
    }

    private EnsureUserProvisionedFilter CreateSut() =>
        new(_provisioning, NullLogger<EnsureUserProvisionedFilter>.Instance);

    private static async Task<(object? Result, bool ReachedHandler)> InvokeAsync(
        EnsureUserProvisionedFilter sut, params Claim[] claims)
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        bool reachedHandler = false;

        object? result = await sut.InvokeAsync(
            EndpointFilterInvocationContext.Create(httpContext),
            _ =>
            {
                reachedHandler = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        return (result, reachedHandler);
    }

    private static void ItShouldHaveReturnedUserNotProvisioned(object? result)
    {
        ProblemHttpResult problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        problem.ProblemDetails.Extensions["code"].ShouldBe(EnsureUserProvisionedFilter.UserNotProvisionedCode);
    }
}
