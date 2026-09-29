using AgentGuard.Core.Builders;
using TypeSafeAI.Sdk.Api;

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
    public static GuardrailPolicyBuilder BlockPromptInjectionWithTypeSafeAi(
        this GuardrailPolicyBuilder builder, 
        ITypeSafeClient client, 
        TypeSafeAiRuleOptions options)
    {
        return builder.AddRule(new TypeSafeAiRule(client, options));
    }
}