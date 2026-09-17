# AgentGuard.Extensions.AzurePii

## Example

``` csharp
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

mainLogger.LogWarning("{Endpoint} --> Guardrail result: {Result}", endpoint, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
```

### Sponsors

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=StefH) and [Dapper Plus](https://dapper-plus.net/?utm_source=StefH) are major sponsors and proud to contribute to the development of **RamlToOpenApiConverter**.

[![Entity Framework Extensions](https://raw.githubusercontent.com/StefH/resources/main/sponsor/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert?utm_source=StefH)

[![Dapper Plus](https://raw.githubusercontent.com/StefH/resources/main/sponsor/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert?utm_source=StefH)