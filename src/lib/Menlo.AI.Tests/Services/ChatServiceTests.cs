using Menlo.AI.Interfaces;
using Menlo.AI.Services;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using NSubstitute;
using Shouldly;

namespace Menlo.AI.Tests.Services;

public sealed class ChatServiceTests
{
    [Fact]
    public void GivenNullKernel_WhenConstructed_ThenShouldThrowArgumentNullException()
    {
        // Given & When & Then
        Should.Throw<ArgumentNullException>(() => new ChatService(null!));
    }

    [Fact]
    public void GivenChatServiceWithValidParameters_WhenConstructed_ThenShouldNotBeNull()
    {
        // Given
        Kernel kernel = Kernel.CreateBuilder().Build();
        IChatClient chatClient = Substitute.For<IChatClient>();

        // When
        var chatService = new ChatService(kernel, chatClient);

        // Then
        chatService.ShouldNotBeNull();
        chatService.ShouldBeAssignableTo<IChatService>();
    }
}
