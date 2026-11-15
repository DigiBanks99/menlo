using CSharpFunctionalExtensions;
using Menlo.Api.Auth;
using Menlo.Application.Auth;
using Menlo.Application.Common;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Common;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Claims;

namespace Menlo.Api.Tests.Auth;

public sealed class UserContextMiddlewareTests
{
    private readonly IUserProvisioningService _provisioning = Substitute.For<IUserProvisioningService>();

    [Test]
    public async Task GivenClaimsWithoutExternalId_WhenInvoked_Returns403AndDoesNotProvision()
    {
        (int status, string body, bool reachedNext, CurrentUser current) =
            await InvokeAsync(new Claim(ClaimTypes.Email, "a@menlo.test"));

        ItShouldHaveReturnedUserNotProvisioned(status, body);
        reachedNext.ShouldBeFalse();
        current.Value.ShouldBeNull();
        await _provisioning.DidNotReceiveWithAnyArgs().ProvisionOrUpdateAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task GivenClaimsWithBlankEmail_WhenInvoked_Returns403AndDoesNotProvision()
    {
        (int status, string body, bool reachedNext, _) = await InvokeAsync(
            new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "  "));

        ItShouldHaveReturnedUserNotProvisioned(status, body);
        reachedNext.ShouldBeFalse();
        await _provisioning.DidNotReceiveWithAnyArgs().ProvisionOrUpdateAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task GivenProvisioningReturnsAnError_WhenInvoked_Returns403AndSkipsTheRestOfThePipeline()
    {
        _provisioning
            .ProvisionOrUpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User, AuthError>(new InvalidUserDataError("bad data")));

        (int status, string body, bool reachedNext, CurrentUser current) = await InvokeAsync(
            new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test"));

        ItShouldHaveReturnedUserNotProvisioned(status, body);
        reachedNext.ShouldBeFalse();
        current.Value.ShouldBeNull();
    }

    [Test]
    public async Task GivenProvisioningThrows_WhenInvoked_TheExceptionIsNotSwallowed()
    {
        _provisioning
            .ProvisionOrUpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Result<User, AuthError>>(_ => throw new InvalidOperationException("database is down"));

        Func<Task> act = async () => await InvokeAsync(
            new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test"));

        InvalidOperationException error = await act.ShouldThrowAsync<InvalidOperationException>();
        error.Message.ShouldBe("database is down");
    }

    [Test]
    public async Task GivenNoNameClaim_WhenInvoked_ProvisionsWithTheEmailAsDisplayName()
    {
        ProvisioningSucceeds();

        (_, _, bool reachedNext, _) = await InvokeAsync(
            new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test"));

        reachedNext.ShouldBeTrue();
        await _provisioning.Received(1).ProvisionOrUpdateAsync("ext-1", "a@menlo.test", "a@menlo.test", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GivenAProvisionedUser_WhenInvoked_RecordsThemAsTheCurrentUser()
    {
        User user = ProvisioningSucceeds();

        (_, _, bool reachedNext, CurrentUser current) = await InvokeAsync(
            new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test"));

        reachedNext.ShouldBeTrue();
        current.Value.ShouldNotBeNull().UserId.ShouldBe(user.Id);
    }

    [Test]
    public async Task GivenNoSignedInUser_WhenInvoked_PassesThroughWithoutProvisioning()
    {
        (_, _, bool reachedNext, CurrentUser current) = await InvokeAsync();

        reachedNext.ShouldBeTrue();
        current.Value.ShouldBeNull();
        await _provisioning.DidNotReceiveWithAnyArgs().ProvisionOrUpdateAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task GivenTheMiddlewareRunsBeforeTheFactoriesAreResolved_WhenStamping_UsesTheSignedInUser()
    {
        User user = ProvisioningSucceeds();

        (AuditStamp audit, SoftDeleteStamp softDelete) = await StampAsync(middlewareFirst: true);

        audit.ActorId.ShouldBe(user.Id);
        softDelete.ActorId.ShouldBe(user.Id);
    }

    [Test]
    public async Task GivenTheFactoriesAreResolvedBeforeTheMiddlewareRuns_WhenStamping_FallsBackToTheSystemUser()
    {
        ProvisioningSucceeds();

        (AuditStamp audit, SoftDeleteStamp softDelete) = await StampAsync(middlewareFirst: false);

        audit.ActorId.ShouldBe(SystemUser.Id);
        softDelete.ActorId.ShouldBe(SystemUser.Id);
    }

    [Test]
    public async Task GivenNoSignedInUser_WhenStamping_UsesTheSystemUser()
    {
        (AuditStamp audit, SoftDeleteStamp softDelete) = await StampAsync(middlewareFirst: true, signedIn: false);

        audit.ActorId.ShouldBe(SystemUser.Id);
        softDelete.ActorId.ShouldBe(SystemUser.Id);
    }

    [Test]
    public async Task GivenARequestOutsideApi_WhenTheMiddlewareIsRegistered_ItDoesNotRun()
    {
        ProvisioningSucceeds();

        (AuditStamp audit, _) = await StampAsync(middlewareFirst: true, path: "/not-api");

        audit.ActorId.ShouldBe(SystemUser.Id);
        await _provisioning.DidNotReceiveWithAnyArgs().ProvisionOrUpdateAsync(default!, default!, default!, default);
    }

    private User ProvisioningSucceeds()
    {
        User user = User.Create(new("ext-1"), "a@menlo.test", "a@menlo.test").Value;
        _provisioning
            .ProvisionOrUpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        return user;
    }

    private ServiceProvider BuildServices()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddMenloApplication();
        builder.Services.AddScoped(_ => _provisioning);
        return builder.Services.BuildServiceProvider();
    }

    private async Task<(int Status, string Body, bool ReachedNext, CurrentUser Current)> InvokeAsync(
        params Claim[] claims)
    {
        await using ServiceProvider services = BuildServices();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        DefaultHttpContext httpContext = CreateHttpContext(scope.ServiceProvider, "/api/budgets", claims);
        bool reachedNext = false;

        UserContextMiddleware sut = new(
            _ =>
            {
                reachedNext = true;
                return Task.CompletedTask;
            },
            NullLogger<UserContextMiddleware>.Instance);
        CurrentUser current = scope.ServiceProvider.GetRequiredService<CurrentUser>();

        await sut.InvokeAsync(httpContext, current, scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body);
        string body = await reader.ReadToEndAsync();

        return (httpContext.Response.StatusCode, body, reachedNext, current);
    }

    /// <summary>
    /// Runs a pipeline that resolves both stamp factories, with <c>UseMenloUserContext</c> either before that step or after it.
    /// </summary>
    private async Task<(AuditStamp Audit, SoftDeleteStamp SoftDelete)> StampAsync(
        bool middlewareFirst,
        bool signedIn = true,
        string path = "/api/budgets")
    {
        await using ServiceProvider services = BuildServices();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AuditStamp audit = default;
        SoftDeleteStamp softDelete = default;

        RequestDelegate Resolve(RequestDelegate next) => async context =>
        {
            audit = context.RequestServices.GetRequiredService<IAuditStampFactory>().CreateStamp();
            softDelete = context.RequestServices.GetRequiredService<ISoftDeleteStampFactory>().CreateStamp();
            await next(context);
        };

        IApplicationBuilder app = new ApplicationBuilder(services);
        if (middlewareFirst)
        {
            app.UseMenloUserContext();
            app.Use(Resolve);
        }
        else
        {
            app.Use(Resolve);
            app.UseMenloUserContext();
        }

        RequestDelegate pipeline = app.Build();

        Claim[] claims = signedIn
            ? [new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(ClaimTypes.Email, "a@menlo.test")]
            : [];
        await pipeline(CreateHttpContext(scope.ServiceProvider, path, claims));

        return (audit, softDelete);
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider requestServices, string path, Claim[] claims)
    {
        DefaultHttpContext httpContext = new()
        {
            RequestServices = requestServices,
            User = claims.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        httpContext.Request.Path = path;
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static void ItShouldHaveReturnedUserNotProvisioned(int status, string body)
    {
        status.ShouldBe(StatusCodes.Status403Forbidden);
        body.ShouldContain(UserContextMiddleware.UserNotProvisionedCode);
    }
}
