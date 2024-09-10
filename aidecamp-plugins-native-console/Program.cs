using System.Text;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using OpenTelemetry.Logs;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

#pragma warning disable SKEXP0050 
#pragma warning disable SKEXP0060

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var apiKey = configuration["AzureOpenAI:ApiKey"];
var deploymentName = configuration["AzureOpenAI:DeploymentName"];
var modelId = configuration["AzureOpenAI:ModelId"];
var endpoint = configuration["AzureOpenAI:Endpoint"];
//var connectionString = configuration["AzureMonitor:ConnectionString"];

using var loggerFactory = LoggerFactory.Create(builder =>
{
  // Add OpenTelemetry as a logging provider
  builder.AddOpenTelemetry(options =>
  {
    // Assuming connectionString is already defined.
    //options.AddAzureMonitorLogExporter(options => options.ConnectionString = connectionString);
    // Format log messages. This is default to false.
    options.IncludeFormattedMessage = true;
    options.AddConsoleExporter();
  });
  builder.AddFilter("Microsoft", LogLevel.Warning);
  builder.AddFilter("Microsoft.SemanticKernel", LogLevel.Information);
});
// Logger for program scope
ILogger logger = NullLogger.Instance;

var builder = Kernel.CreateBuilder();
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: deploymentName, 
    endpoint: endpoint,
    apiKey: apiKey
    );
builder.Services.AddSingleton(loggerFactory);
//builder.Services.AddSingleton<IPromptRenderFilter, PromptFilterTrace>();
var kernel = builder.Build();

kernel.ImportPluginFromType<TurnManagerPlugin>();
//var prompts = kernel.ImportPluginFromPromptDirectory("Prompts");

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
{
  Temperature = 0.5,
  ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
  ResponseFormat = ChatResponseFormat.JsonObject
};  

var history = new ChatHistory();

string promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "prompts", "prompt.md");
string systemPrompt = await File.ReadAllTextAsync(promptFilePath);
history.AddSystemMessage(systemPrompt);

string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "images", "test-3-20.jpg");
var imageBytes = await File.ReadAllBytesAsync(imagePath);
var message = new ChatMessageContentItemCollection
{
    new TextContent(@"Identify the firing and target toy soldiers in this picture, then calculate the outcome of the wargame scenario. Return the scenario and outcome as JSON"),
    new ImageContent(imageBytes, "image/jpg"),
    new TextContent("20 cm")
};
history.AddUserMessage(message);

var result = await chatCompletionService.GetChatMessageContentAsync( 
        history,
        executionSettings: promptExecutionSettings,
        kernel: kernel);

// Add the message from the agent to the chat history
history.AddMessage(result.Role, result.Content ?? string.Empty);

// Print the results
Console.WriteLine("Assistant > " + result);

// given the ResponseFormat = ChatResponseFormat.JsonObject setting, it should be JSON
using (JsonDocument document = JsonDocument.Parse(result.Content))
{
    if (document.RootElement.TryGetProperty("outcome", out JsonElement outcomeElement))
    {
        // Extract the outcome element as a JSON string
        string outcomeJson = outcomeElement.GetRawText();

        // Optionally, deserialize the outcome JSON into an Outcome object
        ScenarioOutcome outcome = JsonSerializer.Deserialize<ScenarioOutcome>(outcomeJson);
        Console.WriteLine($"Hit or Miss: {outcome.HitOrMiss}");
    }
    else
    {
        Console.WriteLine("Outcome is not present in the assistant result.");
    }
}

Console.ReadLine();
