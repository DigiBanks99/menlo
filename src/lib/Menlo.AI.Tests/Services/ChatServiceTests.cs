using Menlo.AI.Services;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace Menlo.AI.Tests.Services;

public sealed class ChatServiceTests
{
    [Test]
    public void TheKernelIsMissing()
    {
        Exception? exception = Catch.Exception(() => new ChatService(null!));

        ItShouldRefuseToBeCreated();

        void ItShouldRefuseToBeCreated() => exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Test]
    public async Task AChatClientIsProvided()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Sunny in Pretoria")));
        ChatService chatService = new(Kernel.CreateBuilder().Build(), chatClient);

        string response = await chatService.GetResponseAsync("What's the weather?");

        ItShouldAskTheChatClient();
        ItShouldReturnTheChatClientsAnswer();

        void ItShouldAskTheChatClient() => chatClient.Received(1).GetResponseAsync(
            Arg.Is<IEnumerable<ChatMessage>>(messages => messages.Single().Text == "What's the weather?"),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());

        void ItShouldReturnTheChatClientsAnswer() => response.ShouldBe("Sunny in Pretoria");
    }
}
