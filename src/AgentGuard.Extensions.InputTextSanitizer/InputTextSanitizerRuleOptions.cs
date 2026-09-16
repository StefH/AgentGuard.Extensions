using Microsoft.Extensions.AI;

namespace AgentGuard.Extensions.ChatMessagesHandler;

public sealed class InputTextSanitizerRuleOptions
{
    public required IChatClient ChatClient { get; init; }

    public string? SystemPrompt { get; init; }

    /// <summary>
    /// When <c>true</c> (default), a remote failure (exception, non-success response, or timeout) is swallowed.
    /// When <c>false</c>, the failure propagates instead. Cancellation of the caller's own token always propagates regardless of this setting.
    /// </summary>
    public bool FailOpen { get; init; } = true;

    /// <summary>
    /// Per-request timeout, enforced by <see cref="AzurePiiRule"/> via a linked cancellation
    /// token independent of the caller's token. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Optional callback invoked with the exception whenever an Azure call fails and is swallowed by
    /// <see cref="FailOpen"/>. Use for logging/telemetry without imposing a logging dependency here.
    /// </summary>
    public Action<Exception>? OnError { get; init; }
}