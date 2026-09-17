using AgentGuard.Extensions.Abstractions;
using Presidio;
using Presidio.Models;

namespace AgentGuard.Extensions.Presidio;

public sealed class PresidioRuleOptions
{
    /// <summary>
    /// The configured Presidio analyzer instance.
    /// </summary>
    public required IPresidioAnalyzer Analyzer { get; init; }

    /// <summary>
    /// The configured Presidio anonymizer instance.
    /// </summary>
    public required IPresidioAnonymizer Anonymizer { get; init; }

    /// <summary>
    /// Entity types to request from Presidio.
    /// If <c>null</c>, Presidio chooses the default recognizers for the specified language.
    /// </summary>
    public IReadOnlyList<string>? EntityTypes { get; init; }

    /// <summary>
    /// Optional ad-hoc recognizers to include in the Presidio analyze request.
    /// </summary>
    public IReadOnlyList<PatternRecognizer>? AdHocRecognizers { get; init; }

    /// <summary>
    /// The analysis language sent to Presidio. Defaults to <c>en</c>.
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Minimum score threshold sent to Presidio and enforced again client-side.
    /// Defaults to 0.7.
    /// </summary>
    public double? ConfidenceThreshold { get; init; } = 0.7;

    /// <summary>
    /// When <c>true</c> (default), a remote failure is swallowed and the rule returns no results.
    /// When <c>false</c>, the failure propagates instead. Cancellation of the caller's token always propagates.
    /// </summary>
    public bool FailOpen { get; init; } = true;

    /// <summary>
    /// Per-request timeout enforced via a linked cancellation token. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Optional callback invoked with the exception whenever a Presidio call fails and is swallowed by <see cref="FailOpen"/>.
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
    /// Replacement text when using <see cref="PiiOperation.Replace"/>.
    /// </summary>
    public string? Replacement { get; init; }
}