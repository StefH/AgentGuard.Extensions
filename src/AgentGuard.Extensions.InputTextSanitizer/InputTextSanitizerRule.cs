using AgentGuard.Core.Abstractions;
using Microsoft.Extensions.AI;

namespace AgentGuard.Extensions.ChatMessagesHandler;

/// <summary>
/// ...
/// </summary>
public sealed class InputTextSanitizerRule : IGuardrailRule
{
    private readonly InputTextSanitizerRuleOptions _options;

    public InputTextSanitizerRule(InputTextSanitizerRuleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    public string Name => "user-message-sanitizer";

    public GuardrailPhase Phase => GuardrailPhase.Input;

    public int Order => 30; //ChatMessage

    public async ValueTask<GuardrailResult> EvaluateAsync(GuardrailContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.Text) || string.IsNullOrEmpty(_options.SystemPrompt))
        {
            return GuardrailResult.Passed();
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.Timeout);

        try
        {
            var modifiedText = await GetSanitizedTextAsync(context.Text, timeoutCts.Token).ConfigureAwait(false);

            return GuardrailResult.Modified(modifiedText, "Only the user question is extracted.") with
            {
                RuleName = Name,
                Severity = GuardrailSeverity.Low
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (_options.FailOpen)
        {
            _options.OnError?.Invoke(ex);
            return GuardrailResult.Passed();
        }
    }

    private async Task<string> GetSanitizedTextAsync(string text, CancellationToken cancellationToken)
    {
        var systemMessage = new ChatMessage(ChatRole.System, _options.SystemPrompt);
        var userMessage = new ChatMessage(ChatRole.User, text);

        var safeChatResponse = await _options.ChatClient.GetResponseAsync([systemMessage, userMessage], cancellationToken: cancellationToken);
        return safeChatResponse.Text.Trim();
    }
}