using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Menlo.AI.Services;

/// <summary>
/// Chat options for llama.cpp's OpenAI-compatible API.
/// </summary>
internal static class LlamaChatOptions
{
    /// <summary>
    /// Creates chat options that, unless thinking is enabled, send <c>chat_template_kwargs.enable_thinking = false</c>.
    /// </summary>
    public static ChatOptions Create(bool enableThinking) =>
        enableThinking
            ? new ChatOptions()
            : new ChatOptions { RawRepresentationFactory = _ => WithThinkingDisabled() };

    // The field is not part of the OpenAI schema, so it is added to the request body through the SDK's JSON patch.
#pragma warning disable SCME0001 // JsonPatch is experimental
    private static ChatCompletionOptions WithThinkingDisabled()
    {
        ChatCompletionOptions options = new();
        options.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, false);
        return options;
    }
#pragma warning restore SCME0001
}
