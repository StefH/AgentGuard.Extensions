using System.Text.Json;
using AgentGuard.Core.Abstractions;
using AgentGuard.Core.Builders;
using AgentGuard.Core.Guardrails;
using AgentGuard.Extensions.Abstractions;
using AgentGuard.Extensions.Presidio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Presidio;
using Presidio.DependencyInjection;
using Presidio.Options;

var services = new ServiceCollection();
services.AddPresidioSDK(new PresidioSDKOptions
{
    AnalyzerBaseAddress = new Uri("http://localhost:5002"),
    AnonymizerBaseAddress = new Uri("http://localhost:5001")
});

var serviceProvider = services.BuildServiceProvider();

var analyzer = serviceProvider.GetRequiredService<IPresidioAnalyzer>();
var anonymizer = serviceProvider.GetRequiredService<IPresidioAnonymizer>();

var options = new PresidioRuleOptions
{
    Timeout = TimeSpan.FromSeconds(999),
    Analyzer = analyzer,
    Anonymizer = anonymizer,
    Language = "nl",
    Operation = PiiOperation.Replace
};

var rule = new PresidioRule(options);
Console.WriteLine($"Created rule: {rule.Name}");

var policy = new GuardrailPolicyBuilder()
    .AddRule(rule)
    .Build();

var logger = serviceProvider.GetRequiredService<ILogger<GuardrailPipeline>>();
var guardrailPipeline = new GuardrailPipeline(policy, logger);

var context = new GuardrailContext { Text = "My postcode is 1234AB and my name is John Doe.", Phase = GuardrailPhase.Input };

var result = await guardrailPipeline.RunAsync(context);
Console.WriteLine($"Guardrail result: {JsonSerializer.Serialize(result)}");