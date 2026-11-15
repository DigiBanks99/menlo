using Menlo.Api.Auth;
using Menlo.Application.Auth;
using Menlo.Application.Common;
using Menlo.Lib.Common;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace Menlo.Api.Tests.Auth;

public sealed class UserContextMiddlewareTests
{
    private static readonly UserId SignedInUser = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Test]
    public async Task GivenASessionWithoutAUserId_WhenInvoked_Returns403AndSkipsTheRestOfThePipeline()
    {
        (int status, string body, bool reachedNext, CurrentUser current) =
            await InvokeAsync(new Claim(ClaimTypes.NameIdentifier, "ext-1"));

        ItShouldHaveReturnedUserNotProvisioned(status, body);
        reachedNext.ShouldBeFalse();
        current.Value.ShouldBeNull();
    }

    [Test]
    public async Task GivenASessionWhoseUserIdIsNotAGuid_WhenInvoked_Returns403()
    {
        (int status, string body, bool reachedNext, _) = await InvokeAsync(
            new Claim(ClaimTypes.NameIdentifier, "ext-1"), new Claim(UserSessionClaims.UserIdClaimType, "not-a-guid"));

        ItShouldHaveReturnedUserNotProvisioned(status, body);
        reachedNext.ShouldBeFalse();
    }

    [Test]
    public async Task GivenASessionWithAnEmptyUserId_WhenInvoked_Returns403()
    {
        (int status, string body, _, _) = await InvokeAsync(
            new Claim(UserSessionClaims.UserIdClaimType, Guid.Empty.ToString()));

        ItShouldHaveReturnedUserNotProvisioned(status, body);
    }

    [Test]
    public async Task GivenASessionWithAUserId_WhenInvoked_RecordsThemAsTheCurrentUser()
    {
        (_, _, bool reachedNext, CurrentUser current) = await InvokeAsync(
            new Claim(UserSessionClaims.UserIdClaimType, SignedInUser.Value.ToString()));

        reachedNext.ShouldBeTrue();
        current.Value.ShouldNotBeNull().UserId.ShouldBe(SignedInUser);
    }

    [Test]
    public async Task GivenNoSignedInUser_WhenInvoked_PassesThrough()
    {
        (_, _, bool reachedNext, CurrentUser current) = await InvokeAsync();

        reachedNext.ShouldBeTrue();
        current.Value.ShouldBeNull();
    }

    [Test]
    public async Task GivenTheMiddlewareRunsBeforeTheFactoriesAreResolved_WhenStamping_UsesTheSignedInUser()
    {
        (AuditStamp audit, SoftDeleteStamp softDelete) = await StampAsync(middlewareFirst: true);

        audit.ActorId.ShouldBe(SignedInUser);
        softDelete.ActorId.ShouldBe(SignedInUser);
    }

    [Test]
    public async Task GivenTheFactoriesAreResolvedBeforeTheMiddlewareRuns_WhenStamping_FallsBackToTheSystemUser()
    {
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
        (AuditStamp audit, _) = await StampAsync(middlewareFirst: true, path: "/not-api");

        audit.ActorId.ShouldBe(SystemUser.Id);
    }

    private static ServiceProvider BuildServices()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddMenloApplication();
        return builder.Services.BuildServiceProvider();
    }

    private static async Task<(int Status, string Body, bool ReachedNext, CurrentUser Current)> InvokeAsync(
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

        await sut.InvokeAsync(httpContext, current);

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body);
        string body = await reader.ReadToEndAsync();

        return (httpContext.Response.StatusCode, body, reachedNext, current);
    }

    /// <summary>
    /// Runs a pipeline that resolves both stamp factories, with <c>UseMenloUserContext</c> either before that step or after it.
    /// </summary>
    private static async Task<(AuditStamp Audit, SoftDeleteStamp SoftDelete)> StampAsync(
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
            ? [new Claim(UserSessionClaims.UserIdClaimType, SignedInUser.Value.ToString())]
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
