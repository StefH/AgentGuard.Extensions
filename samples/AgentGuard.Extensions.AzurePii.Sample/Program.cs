using System.Text.Json;
using AgentGuard.Core.Abstractions;
using AgentGuard.Core.Builders;
using AgentGuard.Core.Guardrails;
using AgentGuard.Extensions.Abstractions;
using AgentGuard.Extensions.AzurePii;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .BuildServiceProvider();

var options = new AzurePiiRuleOptions
{
    Endpoint = Environment.GetEnvironmentVariable("AZURE_AI_LANGUAGE_URL")!,
    AzureKeyCredential = new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_AI_LANGUAGE_KEY")!),
    SupportedLanguage = "en",
    Operation = PiiOperation.Redact
};

var rule = new AzurePiiRule(options);

var policy = new GuardrailPolicyBuilder()
    .AddRule(rule)
    .Build();

var logger = serviceProvider.GetRequiredService<ILogger<GuardrailPipeline>>();
var guardrailPipeline = new GuardrailPipeline(policy, logger);

var context = new GuardrailContext { Text = "My postcode is 1234AB and my name is John Doe.", Phase = GuardrailPhase.Input };

var result = await guardrailPipeline.RunAsync(context);
Console.WriteLine($"Guardrail result: {JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })}");