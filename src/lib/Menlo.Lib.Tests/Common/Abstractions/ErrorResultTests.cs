using CSharpFunctionalExtensions;
using Menlo.Lib.Common.Abstractions;
using Shouldly;

namespace Menlo.Lib.Tests.Common.Abstractions;

/// <summary>
/// Tests for Error and Result pattern.
/// TC-08: Result and Error Guidance
/// </summary>
public sealed class ErrorResultTests
{
    private sealed class TestError : Error
    {
        public TestError(string code, string message) : base(code, message)
        {
        }
    }

    private sealed class InvalidInputError : Error
    {
        public InvalidInputError(string parameterName)
            : base("TEST_001", $"Invalid input for parameter: {parameterName}")
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }

    [Fact]
    public void GivenErrorWithCodeAndMessage_WhenCreated()
    {
        // Arrange & Act
        TestError error = new("ERR_001", "Test error message");

        // Assert
        ItShouldHavePropertiesSet(error);
    }

    private static void ItShouldHavePropertiesSet(TestError error)
    {
        error.Code.ShouldBe("ERR_001");
        error.Message.ShouldBe("Test error message");
    }

    [Fact]
    public void GivenError_WhenConvertingToString()
    {
        // Arrange
        TestError error = new("ERR_001", "Test error message");

        // Act
        string result = error.ToString();

        // Assert
        ItShouldReturnFormattedString(result);
    }

    private static void ItShouldReturnFormattedString(string result)
    {
        result.ShouldBe("[ERR_001] Test error message");
    }

    [Fact]
    public void GivenNullOrWhitespaceCode_WhenCreatingError()
    {
        // Act & Assert
        ItShouldThrowArgumentExceptionForCode();
    }

    private static void ItShouldThrowArgumentExceptionForCode()
    {
        Should.Throw<ArgumentException>(() => new TestError("", "Message"));
        Should.Throw<ArgumentException>(() => new TestError("   ", "Message"));
    }

    [Fact]
    public void GivenNullOrWhitespaceMessage_WhenCreatingError()
    {
        // Act & Assert
        ItShouldThrowArgumentExceptionForMessage();
    }

    private static void ItShouldThrowArgumentExceptionForMessage()
    {
        Should.Throw<ArgumentException>(() => new TestError("CODE", ""));
        Should.Throw<ArgumentException>(() => new TestError("CODE", "   "));
    }

    [Fact]
    public void GivenDomainMethod_WhenProvidingInvalidInput()
    {
        // Arrange
        Result<string, Error> result = ProcessInput("");

        // Act & Assert
        ItShouldReturnFailureWithError(result);
    }

    private static void ItShouldReturnFailureWithError(Result<string, Error> result)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("TEST_001");
        result.Error.Message.ShouldContain("input");
    }

    [Fact]
    public void GivenDomainMethod_WhenProvidingValidInput()
    {
        // Arrange & Act
        Result<string, Error> result = ProcessInput("valid");

        // Assert
        ItShouldReturnSuccess(result);
    }

    private static void ItShouldReturnSuccess(Result<string, Error> result)
    {
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Processed: valid");
    }

    [Fact]
    public void GivenSpecificError_WhenAccessing()
    {
        // Arrange
        InvalidInputError error = new("username");

        // Act & Assert
        ItShouldExposeAdditionalProperties(error);
    }

    private static void ItShouldExposeAdditionalProperties(InvalidInputError error)
    {
        error.Code.ShouldBe("TEST_001");
        error.ParameterName.ShouldBe("username");
        error.Message.ShouldContain("username");
    }

    private static Result<string, Error> ProcessInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new InvalidInputError("input");
        }

        return $"Processed: {input}";
    }
}
