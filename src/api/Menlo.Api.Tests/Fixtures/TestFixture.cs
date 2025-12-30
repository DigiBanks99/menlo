using System.Net;

public abstract class TestFixture
{
    internal static void ItShouldHaveSucceeded(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    internal static void ItShouldHaveBeenUnauthorised(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
