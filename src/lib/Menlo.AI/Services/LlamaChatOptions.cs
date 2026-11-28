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

    /// <summary>
    /// Unless thinking is enabled, makes <paramref name="options"/> send <c>chat_template_kwargs.enable_thinking = false</c>,
    /// on top of any request options the caller already builds.
    /// </summary>
    public static void Apply(ChatOptions options, bool enableThinking)
    {
        if (enableThinking)
        {
            return;
        }

        Func<IChatClient, object?>? callerFactory = options.RawRepresentationFactory;
        options.RawRepresentationFactory = client => WithThinkingDisabled(callerFactory?.Invoke(client) switch
        {
            null => null,
            ChatCompletionOptions callerOptions => callerOptions,
            object unexpected => throw new InvalidOperationException(
                $"ChatOptions.RawRepresentationFactory returned {unexpected.GetType().FullName}; " +
                $"the llama.cpp chat client needs {typeof(ChatCompletionOptions).FullName} or null.")
        });
    }

    // The field is not part of the OpenAI schema, so it is added to the request body through the SDK's JSON patch.
#pragma warning disable SCME0001 // JsonPatch is experimental
    private static ChatCompletionOptions WithThinkingDisabled(ChatCompletionOptions? options = null)
    {
        options ??= new ChatCompletionOptions();
        options.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, false);
        return options;
    }
#pragma warning restore SCME0001
}
