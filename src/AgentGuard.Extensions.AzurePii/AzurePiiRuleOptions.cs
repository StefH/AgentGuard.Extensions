using AgentGuard.Extensions.Abstractions;
using Azure;
using Azure.AI.Language.Text;
using Azure.Core;
using static Azure.AI.Language.Text.TextAnalysisClientOptions;

namespace AgentGuard.Extensions.AzurePii;

public sealed class AzurePiiRuleOptions
{
    /// <summary>
    /// The Azure AI Language resource endpoint, e.g. <c>https://my-resource.cognitiveservices.azure.com</c>.
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// A credential used to authenticate to the Azure PII Service.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }

    /// <summary>
    /// The AzureKeyCredential used to authenticate to the Azure PII Service. This is an alternative to <see cref="TokenCredential"/>.
    /// </summary>
    public AzureKeyCredential? AzureKeyCredential { get; set; }

    /// <summary>
    /// PII categories to be returned in the response.
    /// If <c>null</c>, the categories are returned which are default for the defined ApiVersion.
    /// Set to <c>[PiiCategory.All]</c> to return ALL categories supported by the defined ApiVersion.
    /// </summary>
    public required IReadOnlyList<PiiCategory>? PiiCategories { get; init; }

    /// <summary>
    /// The analysis language sent to Azure. Defaults to <c>en</c>.
    /// </summary>
    public string SupportedLanguage { get; init; } = "en";

    /// <summary>
    /// The <c>:analyze-text</c> REST API version. Defaults to <c>V2025-11-15-Preview</c>.
    /// </summary>
    public ServiceVersion ApiVersion { get; init; } = ServiceVersion.V2025_11_15_Preview;

    /// <summary>
    /// The PII model version to request. Defaults to <c>latest</c>.
    /// </summary>
    public string ModelVersion { get; init; } = "latest";

    /// <summary>
    /// The processing domain. Defaults to <see cref="PiiDomain.None"/> (general PII, not PHI).
    /// </summary>
    public PiiDomain Domain { get; init; } = PiiDomain.None;

    /// <summary>
    /// Minimum <c>confidenceScore</c> (applied client-side, after the response is received) for a
    /// returned entity to be kept. When null, every entity Azure returns is kept (subject to the
    /// analyzer's own overall score threshold downstream).
    /// Defaults to 0.7, which is a reasonable balance between false positives and false negatives for most PII detection scenarios.
    /// </summary>
    public double? ConfidenceThreshold { get; init; } = 0.7;

    /// <summary>
    /// When <c>true</c> (default), tells Azure not to log the submitted text. A PII detector should not
    /// let the request text be retained server-side, so this defaults to <c>true</c> rather than the
    /// service default.
    /// </summary>
    public bool LoggingOptOut { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, <see cref="TextAnalysisClient.AnalyzeTextAsync"/> surfaces the <c>redactedText</c>
    /// field Azure includes in its response.
    /// When <c>false</c> (default) it is dropped even though Azure still computes and returns it on the
    /// wire - detection stays remote, redaction stays local, so nothing in the recognizer path reads
    /// this value; it is purely a passthrough for callers using <see cref="TextAnalysisClient"/> directly.
    /// </summary>
    public bool PassthroughRedactedText { get; init; }

    /// <summary>
    /// When <c>true</c> (default), a remote failure (exception, non-success response, or timeout) is
    /// swallowed and the recognizer returns no results for this request, so local-only recognizers
    /// still redact structured PII. When <c>false</c>, the failure propagates instead. Cancellation of
    /// the caller's own token always propagates regardless of this setting.
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

    /// <summary>
    /// When <c>true</c>, the rule also redacts model output, not just input.
    /// </summary>
    public bool RedactOutput { get; init; }

    /// <summary>
    /// Action to take when a PII is detected. Default: Redact.
    /// </summary>
    public PiiOperation Operation { get; init; } = PiiOperation.Redact;

    /// <summary>
    /// Replacement text when redacting PII.
    /// </summary>
    public string? Replacement { get; init; }
}