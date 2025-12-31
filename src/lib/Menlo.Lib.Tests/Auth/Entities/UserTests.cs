using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Events;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Auth.Entities;

/// <summary>
/// Tests for User aggregate root.
/// </summary>
public sealed class UserTests
{
    [Fact]
    public void GivenValidUserData_WhenCreatingUser()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "Test User";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldSucceed(result);
        ItShouldHaveExternalId(result, externalId);
        ItShouldHaveEmail(result, email);
        ItShouldHaveDisplayName(result, displayName);
        ItShouldHaveValidId(result);
        ItShouldHaveInitialLoginTimestamp(result);
    }

    [Fact]
    public void GivenEmailWithWhitespace_WhenCreatingUser()
    {
        ExternalUserId externalId = new("external-123");
        string email = "  test@example.com  ";
        string displayName = "Test User";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldSucceed(result);
        ItShouldHaveTrimmedEmail(result, "test@example.com");
    }

    [Fact]
    public void GivenDisplayNameWithWhitespace_WhenCreatingUser()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "  Test User  ";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldSucceed(result);
        ItShouldHaveTrimmedDisplayName(result, "Test User");
    }

    [Fact]
    public void GivenEmptyExternalId_WhenCreatingUser()
    {
        ExternalUserId externalId = new("");
        string email = "test@example.com";
        string displayName = "Test User";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldFail(result);
        ItShouldBeInvalidUserDataError(result);
        ItShouldHaveReasonContaining(result, "External ID");
    }

    [Fact]
    public void GivenWhitespaceExternalId_WhenCreatingUser()
    {
        ExternalUserId externalId = new("   ");
        string email = "test@example.com";
        string displayName = "Test User";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldFail(result);
        ItShouldBeInvalidUserDataError(result);
        ItShouldHaveReasonContaining(result, "External ID");
    }

    [Fact]
    public void GivenEmptyEmail_WhenCreatingUser()
    {
        ExternalUserId externalId = new("external-123");
        string email = "";
        string displayName = "Test User";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldFail(result);
        ItShouldBeInvalidUserDataError(result);
        ItShouldHaveReasonContaining(result, "Email");
    }

    [Fact]
    public void GivenWhitespaceEmail_WhenCreatingUser()
    {
        ExternalUserId externalId = new("external-123");
        string email = "   ";
        string displayName = "Test User";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldFail(result);
        ItShouldBeInvalidUserDataError(result);
        ItShouldHaveReasonContaining(result, "Email");
    }

    [Fact]
    public void GivenEmptyDisplayName_WhenCreatingUser()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldFail(result);
        ItShouldBeInvalidUserDataError(result);
        ItShouldHaveReasonContaining(result, "Display name");
    }

    [Fact]
    public void GivenWhitespaceDisplayName_WhenCreatingUser()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "   ";

        Result<User, AuthError> result = User.Create(externalId, email, displayName);

        ItShouldFail(result);
        ItShouldBeInvalidUserDataError(result);
        ItShouldHaveReasonContaining(result, "Display name");
    }

    [Fact]
    public void GivenValidUser_WhenRecordingLogin()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "Test User";
        Result<User, AuthError> createResult = User.Create(externalId, email, displayName);
        User user = createResult.Value;
        DateTimeOffset loginTime = DateTimeOffset.UtcNow;

        user.RecordLogin(loginTime);

        ItShouldHaveLastLoginAt(user, loginTime);
        ItShouldHaveDomainEvent(user);
        ItShouldHaveUserLoggedInEvent(user, loginTime);
    }

    [Fact]
    public void GivenValidUser_WhenRecordingMultipleLogins()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "Test User";
        Result<User, AuthError> createResult = User.Create(externalId, email, displayName);
        User user = createResult.Value;
        DateTimeOffset firstLogin = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset secondLogin = DateTimeOffset.UtcNow;

        user.RecordLogin(firstLogin);
        user.RecordLogin(secondLogin);

        ItShouldHaveLastLoginAt(user, secondLogin);
        ItShouldHaveMultipleDomainEvents(user, 2);
    }

    [Fact]
    public void GivenValidUser_WhenClearingDomainEvents()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "Test User";
        Result<User, AuthError> createResult = User.Create(externalId, email, displayName);
        User user = createResult.Value;
        DateTimeOffset loginTime = DateTimeOffset.UtcNow;
        user.RecordLogin(loginTime);

        user.ClearDomainEvents();

        ItShouldHaveNoDomainEvents(user);
    }

    [Fact]
    public void GivenValidUser_WhenAuditingForCreate()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "Test User";
        Result<User, AuthError> createResult = User.Create(externalId, email, displayName);
        User user = createResult.Value;
        IAuditStampFactory factory = CreateAuditStampFactory();

        user.Audit(factory, AuditOperation.Create);

        ItShouldHaveCreatedBy(user);
        ItShouldHaveCreatedAt(user);
        ItShouldHaveModifiedBy(user);
        ItShouldHaveModifiedAt(user);
    }

    [Fact]
    public void GivenValidUser_WhenAuditingForUpdate()
    {
        ExternalUserId externalId = new("external-123");
        string email = "test@example.com";
        string displayName = "Test User";
        Result<User, AuthError> createResult = User.Create(externalId, email, displayName);
        User user = createResult.Value;
        IAuditStampFactory factory = CreateAuditStampFactory();

        user.Audit(factory, AuditOperation.Update);

        ItShouldNotHaveCreatedBy(user);
        ItShouldNotHaveCreatedAt(user);
        ItShouldHaveModifiedBy(user);
        ItShouldHaveModifiedAt(user);
    }

    // Assertion Helpers
    private static void ItShouldSucceed(Result<User, AuthError> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldFail(Result<User, AuthError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldHaveExternalId(Result<User, AuthError> result, ExternalUserId expectedId)
    {
        result.Value.ExternalId.ShouldBe(expectedId);
    }

    private static void ItShouldHaveEmail(Result<User, AuthError> result, string expectedEmail)
    {
        result.Value.Email.ShouldBe(expectedEmail);
    }

    private static void ItShouldHaveTrimmedEmail(Result<User, AuthError> result, string expectedEmail)
    {
        result.Value.Email.ShouldBe(expectedEmail);
    }

    private static void ItShouldHaveDisplayName(Result<User, AuthError> result, string expectedDisplayName)
    {
        result.Value.DisplayName.ShouldBe(expectedDisplayName);
    }

    private static void ItShouldHaveTrimmedDisplayName(Result<User, AuthError> result, string expectedDisplayName)
    {
        result.Value.DisplayName.ShouldBe(expectedDisplayName);
    }

    private static void ItShouldHaveValidId(Result<User, AuthError> result)
    {
        result.Value.Id.Value.ShouldNotBe(Guid.Empty);
    }

    private static void ItShouldHaveInitialLoginTimestamp(Result<User, AuthError> result)
    {
        result.Value.LastLoginAt.ShouldNotBeNull();
    }

    private static void ItShouldBeInvalidUserDataError(Result<User, AuthError> result)
    {
        result.Error.ShouldBeOfType<InvalidUserDataError>();
    }

    private static void ItShouldHaveReasonContaining(Result<User, AuthError> result, string expectedSubstring)
    {
        InvalidUserDataError error = (InvalidUserDataError)result.Error;
        error.Reason.ShouldContain(expectedSubstring);
    }

    private static void ItShouldHaveLastLoginAt(User user, DateTimeOffset expectedTime)
    {
        user.LastLoginAt.ShouldNotBeNull();
        user.LastLoginAt.Value.ShouldBe(expectedTime);
    }

    private static void ItShouldHaveDomainEvent(User user)
    {
        user.DomainEvents.Count.ShouldBe(1);
    }

    private static void ItShouldHaveUserLoggedInEvent(User user, DateTimeOffset expectedTime)
    {
        IDomainEvent domainEvent = user.DomainEvents.First();
        domainEvent.ShouldBeOfType<UserLoggedInEvent>();
        UserLoggedInEvent loginEvent = (UserLoggedInEvent)domainEvent;
        loginEvent.UserId.ShouldBe(user.Id);
        loginEvent.Timestamp.ShouldBe(expectedTime);
    }

    private static void ItShouldHaveMultipleDomainEvents(User user, int expectedCount)
    {
        user.DomainEvents.Count.ShouldBe(expectedCount);
    }

    private static void ItShouldHaveNoDomainEvents(User user)
    {
        user.DomainEvents.ShouldBeEmpty();
    }

    private static void ItShouldHaveCreatedBy(User user)
    {
        user.CreatedBy.ShouldNotBeNull();
    }

    private static void ItShouldHaveCreatedAt(User user)
    {
        user.CreatedAt.ShouldNotBeNull();
    }

    private static void ItShouldHaveModifiedBy(User user)
    {
        user.ModifiedBy.ShouldNotBeNull();
    }

    private static void ItShouldHaveModifiedAt(User user)
    {
        user.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldNotHaveCreatedBy(User user)
    {
        user.CreatedBy.ShouldBeNull();
    }

    private static void ItShouldNotHaveCreatedAt(User user)
    {
        user.CreatedAt.ShouldBeNull();
    }

    // Test Setup Helpers
    private sealed class FakeAuditStampFactory : IAuditStampFactory
    {
        private readonly AuditStamp _stamp;

        public FakeAuditStampFactory(AuditStamp stamp)
        {
            _stamp = stamp;
        }

        public AuditStamp CreateStamp() => _stamp;
    }

    private static IAuditStampFactory CreateAuditStampFactory()
    {
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        return new FakeAuditStampFactory(new AuditStamp(actorId, timestamp));
    }
}
