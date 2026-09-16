using System.Text;
using AgentGuard.Core.Abstractions;
using AgentGuard.Extensions.Abstractions;
using Azure.AI.Language.Text;

namespace AgentGuard.Extensions.AzurePii;

/// <summary>
/// Detects and redacts PII using Azure.AI.Language.Text (TextAnalysisClient).
/// </summary>
public sealed class AzurePiiRule : IGuardrailRule
{
    private readonly TextAnalysisClient _client;

    private readonly AzurePiiRuleOptions _options;

    private readonly string? _supportedLanguage;

    private readonly double _confidenceThreshold;

    public AzurePiiRule(AzurePiiRuleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _supportedLanguage = options.SupportedLanguage;
        _confidenceThreshold = options.ConfidenceThreshold ?? 0.7;

        var textAnalysisClientOptions = new TextAnalysisClientOptions(options.ApiVersion)
        {
            Diagnostics =
            {
                IsLoggingContentEnabled = false,
                IsLoggingEnabled = false,
                IsDistributedTracingEnabled = false
            }
        };

        if (options.TokenCredential != null)
        {
            _client = new TextAnalysisClient(new Uri(options.Endpoint), options.TokenCredential, textAnalysisClientOptions);
        }
        else if (options.AzureKeyCredential != null)
        {
            _client = new TextAnalysisClient(new Uri(options.Endpoint), options.AzureKeyCredential, textAnalysisClientOptions);
        }
        else
        {
            throw new ArgumentException($"{nameof(AzurePiiRuleOptions)} requires either {nameof(options.AzureKeyCredential)} or {nameof(options.TokenCredential)}.", nameof(options));
        }
    }

    public string Name => "azure-pii-detection";

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
            var analyzeTextInput = new TextPiiEntitiesRecognitionInput
            {
                TextInput = new MultiLanguageTextInput
                {
                    MultiLanguageInputs =
                    {
                        new MultiLanguageInput("1", text)
                        {
                            Language = _supportedLanguage
                        }
                    }
                },
                ActionContent = new PiiActionContent
                {
                    ModelVersion = _options.ModelVersion,
                    LoggingOptOut = _options.LoggingOptOut,
                    ConfidenceScoreThreshold = new ConfidenceScoreThreshold((float)_confidenceThreshold),
                    Domain = _options.Domain
                }
            };

            if (_options.PiiCategories is { } categories && categories.Count > 0)
            {
                foreach (var category in categories)
                {
                    analyzeTextInput.ActionContent.PiiCategories.Add(category);
                }
            }

            var response = await _client
                .AnalyzeTextAsync(analyzeTextInput, showStatistics: false, cancellationToken: timeoutCts.Token)
                .ConfigureAwait(false);

            if (response.Value is not AnalyzeTextPiiResult analyzeTextPiiResult)
            {
                return GuardrailResult.Error(Name, ErrorBehavior.Warn, "Unexpected response type from Azure Text Analysis API.");
            }

            var piiResult = analyzeTextPiiResult.Results;
            if (piiResult.Errors.Count > 0)
            {
                return GuardrailResult.Error(Name, ErrorBehavior.Warn, $"The Azure Text Analysis API returned errors : {string.Join(", ", piiResult.Errors.Select(e => e.Error.Message))}");
            }

            var document = piiResult.Documents.FirstOrDefault();
            if (document == null)
            {
                return GuardrailResult.Error(Name, ErrorBehavior.Warn, "No document results returned from Azure Text Analysis API.");
            }

            if (document.Entities.Count <= 0)
            {
                return GuardrailResult.Passed();
            }

            if (_options.Operation == PiiOperation.Block)
            {
                return new GuardrailResult
                {
                    IsBlocked = true,
                    Reason = "PII detected and blocked.",
                    Severity = GuardrailSeverity.High,
                    Metadata = new Dictionary<string, object>
                    {
                        { "entityValues", document.Entities.Select(m => m.Text).ToArray() },
                        { "entityCount", document.Entities.Count }
                    }
                };
            }

            var redacted = new StringBuilder(text);
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entity in document.Entities.OrderByDescending(e => e.Offset))
            {
                // Guard against a misbehaving service returning offsets outside the analyzed text, which would otherwise throw when the anonymizer/context enhancer slices text[Start..End]
                if (entity.Offset < 0 || entity.Length <= 0 || entity.Offset + entity.Length > text.Length)
                {
                    continue;
                }

                // Actually not needed, since we already set ConfidenceScoreThreshold in the request, but just in case the service doesn't respect it, we filter again here.
                if (entity.ConfidenceScore < _confidenceThreshold)
                {
                    continue;
                }

                var label = $"{Name}-{BuildRedacted(entity)}";
                labels.Add(label);

                if (!_options.PassthroughRedactedText)
                {
                    var replacement = BuildReplacement(entity);
                    redacted.Remove(entity.Offset, entity.Length);
                    redacted.Insert(entity.Offset, replacement);
                }
            }

            var modifiedText = !_options.PassthroughRedactedText ? redacted.ToString() : document.RedactedText;

            var reason = $"PII detected and redacted: {string.Join(", ", labels.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}";
            return GuardrailResult.Modified(modifiedText, reason) with
            {
                RuleName = Name,
                Metadata = new Dictionary<string, object>
                {
                    { "entityValues", document.Entities.Select(m => m.Text).ToArray() },
                    { "entityCount", document.Entities.Count }
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

    private string BuildReplacement(PiiEntity entity)
    {
        if (_options.Operation == PiiOperation.Redact)
        {
            if (!string.IsNullOrWhiteSpace(_options.Replacement))
            {
                return _options.Replacement;
            }

            return BuildRedacted(entity);
        }

        if (_options.Operation == PiiOperation.Mask)
        {
            return new string('*', entity.Length);
        }

        return string.Empty;
    }

    private static string BuildRedacted(PiiEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.Subcategory))
        {
            return $"<{entity.Subcategory.ToUpperInvariant()}>";
        }

        if (!string.IsNullOrWhiteSpace(entity.Type))
        {
            return $"<{entity.Type.ToUpperInvariant()}>";
        }

        return $"<{entity.Category.ToUpperInvariant()}>";
    }
}