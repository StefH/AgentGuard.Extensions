using AgentGuard.Core.Builders;
using AgentGuard.Extensions.ChatMessagesHandler;

namespace AgentGuard.Extensions.AzurePii;

/// <summary>
/// Extension methods for adding PII detection/redaction to the policy builder.
/// </summary>
public static class InputTextSanitizerRuleGuardrailBuilderExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="builder">The policy builder.</param>
    /// <param name="options">Configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static GuardrailPolicyBuilder SanitizeMessage(this GuardrailPolicyBuilder builder, InputTextSanitizerRuleOptions options)
    {
        return builder.AddRule(new InputTextSanitizerRule(options));
    }
}