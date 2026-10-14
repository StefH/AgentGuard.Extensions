using System.Text.Json;
using AgentGuard.Core.Abstractions;
using AgentGuard.Core.Builders;
using AgentGuard.Core.Guardrails;
using AgentGuard.Extensions.Abstractions;
using AgentGuard.Extensions.AzurePii;
using Azure;
using Azure.AI.Language.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .BuildServiceProvider();

foreach (var endpoint in new[] { Environment.GetEnvironmentVariable("AZURE_AI_LANGUAGE_URL")!, "http://localhost:5004" } )
{
    var mainLogger = serviceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var options = new AzurePiiRuleOptions
        {
            Endpoint = endpoint,
            AzureKeyCredential = new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_AI_LANGUAGE_KEY")!),
            SupportedLanguage = "en",
            Operation = PiiOperation.Redact,
            ExcludePiiCategories = [PiiCategoriesExclude.Airport],
            ValueExclusionPolicy = new ValueExclusionPolicy(false, [ "vto", "aaa" ])
        };

        var rule = new AzurePiiRule(options);

        var policy = new GuardrailPolicyBuilder()
            .AddRule(rule)
            .Build();

        var logger = serviceProvider.GetRequiredService<ILogger<GuardrailPipeline>>();
        var guardrailPipeline = new GuardrailPipeline(policy, logger);

        var context = new GuardrailContext { Text = "My postcode is 1234AB and my name is John Doe. And abc and vto.", Phase = GuardrailPhase.Input };

        var result = await guardrailPipeline.RunAsync(context);

        mainLogger.LogWarning("{Endpoint} --> Guardrail result: {Result}", endpoint, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
    catch (Exception ex)
    {
        mainLogger.LogError(ex, "{Endpoint} --> Error: {Message}", endpoint, ex.Message);
    }
}