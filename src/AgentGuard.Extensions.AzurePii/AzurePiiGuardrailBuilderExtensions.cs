using AgentGuard.Core.Builders;

namespace AgentGuard.Extensions.AzurePii;

/// <summary>
/// Extension methods for adding PII detection/redaction to the policy builder.
/// </summary>
public static class AzurePiiGuardrailBuilderExtensions
{
    /// <summary>
    /// Adds context-aware PII detection and de-identification (order 20) using the built-in
    /// analyzer and anonymizer. By default detects all supported entities and replaces each with a
    /// <c>&lt;ENTITY_TYPE&gt;</c> tag.
    /// </summary>
    /// <param name="builder">The policy builder.</param>
    /// <param name="options">Optional configuration. When null, defaults are used.</param>
    /// <returns>The builder for chaining.</returns>
    public static GuardrailPolicyBuilder RedactAzurePii(this GuardrailPolicyBuilder builder, AzurePiiRuleOptions options)
    {
        return builder.AddRule(new AzurePiiRule(options));
    }
}