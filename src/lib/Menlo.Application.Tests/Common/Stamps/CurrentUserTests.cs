using Menlo.Application.Auth;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Tests.Common.Stamps;

public sealed class CurrentUserTests
{
    [Test]
    public void Value_BeforeItIsSet_IsNull()
    {
        new CurrentUser().Value.ShouldBeNull();
    }

    [Test]
    public void Set_RecordsTheUserForTheScope()
    {
        CurrentUser sut = new();
        UserContext user = new(UserId.NewId());

        sut.Set(user);

        sut.Value.ShouldBe(user);
    }

    [Test]
    public void Set_WhenAlreadySet_Throws()
    {
        CurrentUser sut = new();
        sut.Set(new UserContext(UserId.NewId()));

        Should.Throw<InvalidOperationException>(() => sut.Set(new UserContext(UserId.NewId())));
    }
}
