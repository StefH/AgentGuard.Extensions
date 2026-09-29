using AgentGuard.Core.Abstractions;
using TypeSafeAI.Sdk.Api;
using TypeSafeAI.Sdk.Contracts;

namespace AgentGuard.Extensions.AzurePii;

/// <summary>
/// Detects and redacts PII using TypeSafe AI (Jev).
/// 
/// Using rules from https://github.com/Arcanum-Sec/arc_pi_taxonomy
/// </summary>
public sealed class TypeSafeAiRule : IGuardrailRule
{
    private readonly ITypeSafeClient _client;

    private readonly TypeSafeAiRuleOptions _options;

    public TypeSafeAiRule(ITypeSafeClient client, TypeSafeAiRuleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _client = client;
    }

    /// <inheritdoc />
    public string Name => "prompt-injection-typesafeai";

    /// <inheritdoc />
    public GuardrailPhase Phase => _options.BLockOutput ? GuardrailPhase.Both : GuardrailPhase.Input;

    /// <inheritdoc />
    public int Order => 21;

    /// <inheritdoc />
    public async ValueTask<GuardrailResult> EvaluateAsync(GuardrailContext context, CancellationToken cancellationToken = default)
    {
        var text = context.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return GuardrailResult.Passed();
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.Timeout);

        return await EvaluateAsync(text, timeoutCts.Token);
    }

    private async Task<GuardrailResult> EvaluateAsync(string text, CancellationToken cancellationToken)
    {
        var request = new EvaluateRequest
        {
            State = text,
            Questions = PromptInjectionQuestions.Techniques
        };

        EvaluateResponse response;

        try
        {
            response = await _client.EvaluateAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return GuardrailResult.Error(Name, ErrorBehavior.FailClosed);
        }
        catch (Exception ex)
        {
            return GuardrailResult.Error(Name, ErrorBehavior.FailClosed, ex.Message);
        }

        var categories = new List<string>();
        foreach (var key in request.Questions.Keys)
        {
            if (response.Answers.TryGetValue(key, out var result) && result.Noul > _options.ConfidenceThreshold)
            {
                categories.Add($"{key}_{result.Noul:0.00}");
            }
        }

        if (categories.Count == 0)
        {
            return GuardrailResult.Passed();
        }

        return new GuardrailResult
        {
            Severity = GuardrailSeverity.High,
            IsBlocked = true,
            Reason = "Input matched a injection pattern.",
            Metadata = new Dictionary<string, object>
            {
                { "categories", string.Join(',', categories) },
                { "categoriesCount", categories.Count }
            }
        };
    }
}