using System.ComponentModel.DataAnnotations;

namespace AgentGuard.Extensions.AzurePii;

public sealed class TypeSafeAiRuleOptions
{
    [Url]
    public required Uri BaseAddress { get; set; } = new("https://api.typesafe.ai");

    [Required]
    public required string ApiKey { get; set; }

    /// <summary>
    /// The model that handles the request. Use <c>"jev-latest"</c>, TypeSafe’s flagship model.
    /// </summary>
    public string Model { get; set; } = "jev-latest";

    /// <summary>
    /// Minimum <c>confidenceScore</c> (applied client-side, after the response is received) for a
    /// returned entity to be kept. When null, every entity Azure returns is kept (subject to the
    /// analyzer's own overall score threshold downstream).
    /// Defaults to 0.7, which is a reasonable balance between false positives and false negatives for most PII detection scenarios.
    /// </summary>
    public float ConfidenceThreshold { get; init; } = 0.7f;

    /// <summary>
    /// When <c>true</c>, the rule also blocks model output, not just input.
    /// </summary>
    public bool BLockOutput { get; init; }

    /// <summary>
    /// Per-request timeout, enforced by <see cref="TypeSafeAiRule"/> via a linked cancellation
    /// token independent of the caller's token. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}