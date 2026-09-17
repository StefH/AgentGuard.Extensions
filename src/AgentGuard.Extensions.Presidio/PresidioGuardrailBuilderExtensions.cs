using AgentGuard.Core.Builders;

namespace AgentGuard.Extensions.Presidio;

/// <summary>
/// Extension methods for adding PII detection/redaction to the policy builder.
/// </summary>
public static class PresidioGuardrailBuilderExtensions
{
    /// <summary>
    /// Adds Presidio-based PII detection and de-identification (order 21).
    /// </summary>
    /// <param name="builder">The policy builder.</param>
    /// <param name="options">The configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static GuardrailPolicyBuilder RedactPresidioPii(this GuardrailPolicyBuilder builder, PresidioRuleOptions options)
    {
        return builder.AddRule(new PresidioRule(options));
    }
}