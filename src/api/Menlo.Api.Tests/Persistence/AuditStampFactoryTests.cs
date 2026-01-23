using System.Security.Claims;
using Menlo.Api.Persistence;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace Menlo.Api.Tests.Persistence;

/// <summary>
/// Tests for AuditStampFactory.
/// </summary>
public sealed class AuditStampFactoryTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _timeProvider;

    public AuditStampFactoryTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _timeProvider = Substitute.For<TimeProvider>();
    }

    [Fact]
    public void GivenAuthenticatedUserWithOidClaim_WhenCreatingStamp()
    {
        Guid expectedUserId = Guid.NewGuid();
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        string expectedTraceId = "trace-123";
        SetupAuthenticatedUser(oidClaim: expectedUserId.ToString());
        SetupHttpContext(traceIdentifier: expectedTraceId);
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveActorId(result, expectedUserId);
        ItShouldHaveTimestamp(result, expectedTimestamp);
        ItShouldHaveCorrelationId(result, expectedTraceId);
    }

    [Fact]
    public void GivenAuthenticatedUserWithNameIdentifierClaim_WhenCreatingStamp()
    {
        Guid expectedUserId = Guid.NewGuid();
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        string expectedTraceId = "trace-456";
        SetupAuthenticatedUser(nameIdentifierClaim: expectedUserId.ToString());
        SetupHttpContext(traceIdentifier: expectedTraceId);
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveActorId(result, expectedUserId);
        ItShouldHaveTimestamp(result, expectedTimestamp);
        ItShouldHaveCorrelationId(result, expectedTraceId);
    }

    [Fact]
    public void GivenAuthenticatedUserWithBothClaims_WhenCreatingStamp()
    {
        Guid oidUserId = Guid.NewGuid();
        Guid nameIdentifierUserId = Guid.NewGuid();
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupAuthenticatedUser(oidClaim: oidUserId.ToString(), nameIdentifierClaim: nameIdentifierUserId.ToString());
        SetupHttpContext(traceIdentifier: "trace-789");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveActorId(result, oidUserId);
    }

    [Fact]
    public void GivenUnauthenticatedRequest_WhenCreatingStamp()
    {
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupUnauthenticatedUser();
        SetupHttpContext(traceIdentifier: "trace-unauth");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveSystemUser(result);
        ItShouldHaveTimestamp(result, expectedTimestamp);
    }

    [Fact]
    public void GivenNoHttpContext_WhenCreatingStamp()
    {
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupNoHttpContext();
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveSystemUser(result);
        ItShouldHaveTimestamp(result, expectedTimestamp);
        ItShouldHaveNullCorrelationId(result);
    }

    [Fact]
    public void GivenInvalidGuidInOidClaim_WhenCreatingStamp()
    {
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupAuthenticatedUser(oidClaim: "not-a-valid-guid");
        SetupHttpContext(traceIdentifier: "trace-invalid");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveSystemUser(result);
        ItShouldHaveTimestamp(result, expectedTimestamp);
    }

    [Fact]
    public void GivenInvalidGuidInNameIdentifierClaim_WhenCreatingStamp()
    {
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupAuthenticatedUser(nameIdentifierClaim: "invalid-guid-format");
        SetupHttpContext(traceIdentifier: "trace-invalid-2");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveSystemUser(result);
        ItShouldHaveTimestamp(result, expectedTimestamp);
    }

    [Fact]
    public void GivenEmptyOidClaim_WhenCreatingStamp()
    {
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupAuthenticatedUser(oidClaim: string.Empty);
        SetupHttpContext(traceIdentifier: "trace-empty");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveSystemUser(result);
        ItShouldHaveTimestamp(result, expectedTimestamp);
    }

    [Fact]
    public void GivenTraceIdentifierPresent_WhenCreatingStamp()
    {
        Guid expectedUserId = Guid.NewGuid();
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        string expectedTraceId = "0HN1234567890ABCDEF:00000001";
        SetupAuthenticatedUser(oidClaim: expectedUserId.ToString());
        SetupHttpContext(traceIdentifier: expectedTraceId);
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveCorrelationId(result, expectedTraceId);
    }

    [Fact]
    public void GivenTraceIdentifierNull_WhenCreatingStamp()
    {
        Guid expectedUserId = Guid.NewGuid();
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupAuthenticatedUser(oidClaim: expectedUserId.ToString());
        SetupHttpContext(traceIdentifier: null);
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveActorId(result, expectedUserId);
        // DefaultHttpContext.TraceIdentifier returns empty string, not null
        ItShouldHaveEmptyCorrelationId(result);
    }

    [Fact]
    public void GivenTimeProviderReturnsSpecificTime_WhenCreatingStamp()
    {
        Guid expectedUserId = Guid.NewGuid();
        DateTimeOffset expectedTimestamp = new(2024, 6, 15, 14, 45, 30, TimeSpan.FromHours(-5));
        SetupAuthenticatedUser(oidClaim: expectedUserId.ToString());
        SetupHttpContext(traceIdentifier: "trace-time");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveTimestamp(result, expectedTimestamp);
    }

    [Fact]
    public void GivenMultipleCallsToCreateStamp_WhenCreatingStamp()
    {
        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        DateTimeOffset timestamp1 = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset timestamp2 = new(2024, 1, 15, 11, 0, 0, TimeSpan.Zero);
        SetupAuthenticatedUser(oidClaim: userId1.ToString());
        SetupHttpContext(traceIdentifier: "trace-1");
        SetupTimeProvider(timestamp1);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result1 = factory.CreateStamp();

        ItShouldHaveActorId(result1, userId1);
        ItShouldHaveTimestamp(result1, timestamp1);

        SetupAuthenticatedUser(oidClaim: userId2.ToString());
        SetupHttpContext(traceIdentifier: "trace-2");
        SetupTimeProvider(timestamp2);

        AuditStamp result2 = factory.CreateStamp();

        ItShouldHaveActorId(result2, userId2);
        ItShouldHaveTimestamp(result2, timestamp2);
        ItShouldHaveCorrelationId(result2, "trace-2");
    }

    [Fact]
    public void GivenUserWithNullIdentity_WhenCreatingStamp()
    {
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupAuthenticatedUserWithNullIdentity();
        SetupHttpContext(traceIdentifier: "trace-null-identity");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveSystemUser(result);
        ItShouldHaveTimestamp(result, expectedTimestamp);
    }

    [Fact]
    public void GivenUserWithoutAnyClaims_WhenCreatingStamp()
    {
        DateTimeOffset expectedTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupAuthenticatedUserWithoutClaims();
        SetupHttpContext(traceIdentifier: "trace-no-claims");
        SetupTimeProvider(expectedTimestamp);
        AuditStampFactory factory = new(_httpContextAccessor, _timeProvider);

        AuditStamp result = factory.CreateStamp();

        ItShouldHaveSystemUser(result);
        ItShouldHaveTimestamp(result, expectedTimestamp);
    }

    // Setup Helpers
    private void SetupAuthenticatedUser(string? oidClaim = null, string? nameIdentifierClaim = null)
    {
        List<Claim> claims = new();
        if (oidClaim != null)
        {
            claims.Add(new Claim("oid", oidClaim));
        }
        if (nameIdentifierClaim != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifierClaim));
        }

        ClaimsIdentity identity = new(claims, "TestAuthType");
        ClaimsPrincipal principal = new(identity);

        DefaultHttpContext httpContext = new()
        {
            User = principal
        };

        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private void SetupAuthenticatedUserWithNullIdentity()
    {
        ClaimsPrincipal principal = new();

        DefaultHttpContext httpContext = new()
        {
            User = principal
        };

        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private void SetupAuthenticatedUserWithoutClaims()
    {
        ClaimsIdentity identity = new(Array.Empty<Claim>(), "TestAuthType");
        ClaimsPrincipal principal = new(identity);

        DefaultHttpContext httpContext = new()
        {
            User = principal
        };

        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private void SetupUnauthenticatedUser()
    {
        ClaimsIdentity identity = new();
        ClaimsPrincipal principal = new(identity);

        DefaultHttpContext httpContext = new()
        {
            User = principal
        };

        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private void SetupNoHttpContext()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
    }

    private void SetupHttpContext(string? traceIdentifier)
    {
        HttpContext? currentContext = _httpContextAccessor.HttpContext;
        if (currentContext != null)
        {
            currentContext.TraceIdentifier = traceIdentifier ?? string.Empty;
        }
    }

    private void SetupTimeProvider(DateTimeOffset timestamp)
    {
        _timeProvider.GetUtcNow().Returns(timestamp);
    }

    // Assertion Helpers
    private static void ItShouldHaveActorId(AuditStamp stamp, Guid expectedUserId)
    {
        stamp.ActorId.Value.ShouldBe(expectedUserId);
    }

    private static void ItShouldHaveSystemUser(AuditStamp stamp)
    {
        stamp.ActorId.Value.ShouldBe(Guid.Empty);
    }

    private static void ItShouldHaveTimestamp(AuditStamp stamp, DateTimeOffset expectedTimestamp)
    {
        stamp.Timestamp.ShouldBe(expectedTimestamp);
    }

    private static void ItShouldHaveCorrelationId(AuditStamp stamp, string expectedCorrelationId)
    {
        stamp.CorrelationId.ShouldNotBeNull();
        stamp.CorrelationId.ShouldBe(expectedCorrelationId);
    }

    private static void ItShouldHaveNullCorrelationId(AuditStamp stamp)
    {
        stamp.CorrelationId.ShouldBeNull();
    }

    private static void ItShouldHaveEmptyCorrelationId(AuditStamp stamp)
    {
        stamp.CorrelationId.ShouldBe(string.Empty);
    }
}
