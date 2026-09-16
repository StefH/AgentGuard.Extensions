# AgentGuard.Extensions.Presidio

Sample usage based on the `Presidio.SDK.Example` Worker.

```csharp
using AgentGuard.Core;
using AgentGuard.Extensions.Abstractions;
using AgentGuard.Extensions.Presidio;
using Microsoft.Extensions.DependencyInjection;
using Presidio;
using Presidio.DependencyInjection;
using Presidio.Enums;
using Presidio.Models;
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
    Analyzer = analyzer,
    Anonymizer = anonymizer,
    Language = "nl",
    Operation = PiiOperation.Replace,
    Replacement = "<REDACTED>",
    AdHocRecognizers =
    [
        new PatternRecognizer
        {
            Name = "Dutch Postcode recognizer",
            SupportedEntity = "NL_POSTCODE",
            SupportedLanguage = "nl",
            GlobalRegexFlags = RegexFlags.Multiline | RegexFlags.DotAll,
            Patterns =
            [
                new Pattern
                {
                    Name = "Dutch Postcode",
                    Regex = @"\b[1-9][0-9]{3}\s?(?!SA|SD|SS)[A-Z]{2}\b",
                    Score = 1
                }
            ],
            Context = ["postcode"]
        }
    ]
};

var policy = new GuardrailPolicyBuilder()
    .RedactPresidioPii(options)
    .Build();

var input = "Peter Jansen, Dorpsstraat 10, 5678 CD Utrecht, peter.jansen@email.com";
var result = await policy.EvaluateInputAsync(input);

Console.WriteLine(result.Text);
```
