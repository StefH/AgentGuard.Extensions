using AgentGuard.Core.Builders;

namespace AgentGuard.Extensions.AzurePii;

/// <summary>
/// Extension methods for ... to the policy builder.
/// </summary>
public static class TypeSafeAiGuardrailBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder">The policy builder.</param>
    /// <param name="options">The configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static GuardrailPolicyBuilder RedactAzurePii(this GuardrailPolicyBuilder builder, AzurePiiRuleOptions options)
    {
        return builder.AddRule(new AzurePiiRule(options));
    }
}