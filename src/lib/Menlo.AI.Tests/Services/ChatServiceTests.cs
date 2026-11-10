using Menlo.AI.Configuration;
using Menlo.AI.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Menlo.AI.Tests.Services;

public sealed class ChatServiceTests
{
    private readonly IChatClient _chatClient = Substitute.For<IChatClient>();
    private readonly ChatService _chatService;

    public ChatServiceTests()
    {
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Sunny in Pretoria")));
        AIAgent agent = _chatClient.AsAIAgent();
        _chatService = new ChatService(agent, Options.Create(new AiSettings { Endpoint = new Uri("http://llama/v1") }));
    }

    [Test]
    public async Task SomeoneAsksAQuestion()
    {
        string response = await _chatService.GetResponseAsync("What's the weather?");

        ItShouldAskTheChatClient();
        ItShouldReturnTheChatClientsAnswer();

        void ItShouldAskTheChatClient() => _chatClient.Received(1).GetResponseAsync(
            Arg.Is<IEnumerable<ChatMessage>>(messages => messages.Single().Text == "What's the weather?"),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());

        void ItShouldReturnTheChatClientsAnswer() => response.ShouldBe("Sunny in Pretoria");
    }
}
