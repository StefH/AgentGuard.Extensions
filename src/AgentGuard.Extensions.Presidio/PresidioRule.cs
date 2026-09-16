using AgentGuard.Core.Abstractions;
using AgentGuard.Extensions.Abstractions;
using Presidio;
using Presidio.Models;

namespace AgentGuard.Extensions.Presidio;

/// <summary>
/// Detects and redacts PII using Presidio.SDK.
/// </summary>
public sealed class PresidioRule : IGuardrailRule
{
    private readonly IPresidioAnalyzer _analyzer;

    private readonly IPresidioAnonymizer _anonymizerService;

    private readonly PresidioRuleOptions _options;

    private readonly double _confidenceThreshold;

    public PresidioRule(PresidioRuleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Analyzer);
        ArgumentNullException.ThrowIfNull(options.Anonymizer);

        _analyzer = options.Analyzer;
        _anonymizerService = options.Anonymizer;
        _options = options;
        _confidenceThreshold = options.ConfidenceThreshold ?? 0.7;
    }

    public string Name => "presidio-pii-detection";

    public GuardrailPhase Phase => _options.RedactOutput ? GuardrailPhase.Both : GuardrailPhase.Input;

    public int Order => 21;

    public async ValueTask<GuardrailResult> EvaluateAsync(GuardrailContext context, CancellationToken cancellationToken = default)
    {
        var text = context.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return GuardrailResult.Passed();
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.Timeout);

        try
        {
            var analyzeRequest = new AnalyzeRequest
            {
                Text = text,
                Language = _options.Language,
                CorrelationId = Guid.NewGuid().ToString("N"),
                ScoreThreshold = _confidenceThreshold,
                Entities = _options.EntityTypes is { Count: > 0 } ? [.. _options.EntityTypes] : null,
                AdHocRecognizers = _options.AdHocRecognizers is { Count: > 0 } ? [.. _options.AdHocRecognizers] : null
            };

            var analysisResults = await _analyzer
                .AnalyzeAsync(analyzeRequest, timeoutCts.Token)
                .ConfigureAwait(false);

            var entities = analysisResults
                .Where(entity => entity.Score >= _confidenceThreshold)
                .Where(entity => TryGetSpan(entity, text.Length, out _, out _))
                .ToArray();

            if (entities.Length == 0)
            {
                return GuardrailResult.Passed();
            }

            var entityTypes = entities
                .Select(entity => entity.EntityType)
                .Where(entityType => !string.IsNullOrWhiteSpace(entityType))
                .ToArray();

            if (_options.Operation == PiiOperation.Block)
            {
                return new GuardrailResult
                {
                    IsBlocked = true,
                    Reason = "PII detected and blocked.",
                    Severity = GuardrailSeverity.High,
                    Metadata = new Dictionary<string, object>
                    {
                        { "entityTypes", entityTypes },
                        { "entityCount", entities.Length }
                    }
                };
            }

            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entity in entities)
            {
                labels.Add($"{Name}-{BuildRedacted(entity)}");
            }

            var modifiedText = await AnonymizeTextAsync(text, entities, timeoutCts.Token).ConfigureAwait(false);
            var reason = $"PII detected and redacted: {string.Join(", ", labels.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}";
            return GuardrailResult.Modified(modifiedText, reason) with
            {
                RuleName = Name,
                Metadata = new Dictionary<string, object>
                {
                    { "entityTypes", entityTypes },
                    { "entityCount", entities.Length }
                }
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

    private async Task<string> AnonymizeTextAsync(string text, IReadOnlyCollection<RecognizerResult> entities, CancellationToken cancellationToken)
    {
        var anonymizeRequest = new AnonymizeRequest
        {
            Text = text,
            AnalyzerResults = [.. entities],
            Anonymizers = BuildAnonymizers()
        };

        var response = await _anonymizerService.AnonymizeAsync(anonymizeRequest, cancellationToken).ConfigureAwait(false);
        return response.Text;
    }

    private Dictionary<string, IAnonymizer> BuildAnonymizers()
    {
        IAnonymizer anonymizer = _options.Operation switch
        {
            PiiOperation.Mask => new Mask
            {
                MaskingChar = "*",
                CharsToMask = int.MaxValue,
                FromEnd = false
            },
            PiiOperation.Replace => new Replace
            {
                NewValue = !string.IsNullOrWhiteSpace(_options.Replacement) ? _options.Replacement : "<PII>"
            },
            PiiOperation.Hash => new Hash(),
            _ => new Redact()
        };

        return new Dictionary<string, IAnonymizer>(StringComparer.OrdinalIgnoreCase)
        {
            ["DEFAULT"] = anonymizer
        };
    }

    private static string BuildRedacted(RecognizerResult entity)
    {
        return !string.IsNullOrWhiteSpace(entity.EntityType) ? $"<{entity.EntityType.ToUpperInvariant()}>" : "<PII>";
    }

    private static bool TryGetSpan(RecognizerResult entity, int textLength, out int start, out int length)
    {
        start = entity.Start;
        length = GetLength(entity);

        return start >= 0 && length > 0 && start + length <= textLength;
    }

    private static int GetLength(RecognizerResult entity)
    {
        return entity.Length > 0 ? entity.Length : entity.End - entity.Start;
    }
}