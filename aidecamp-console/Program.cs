using System.Text;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
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

var builder = Kernel.CreateBuilder();
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName, 
    endpoint,
    apiKey,
    modelId
);

var kernel = builder.Build();

kernel.ImportPluginFromType<TimeInformationPlugin>();
kernel.ImportPluginFromType<ConversationSummaryPlugin>();
var prompts = kernel.ImportPluginFromPromptDirectory("Prompts");

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// OpenAIPromptExecutionSettings openAIPromptExecutionSettings  = new()
// {
//     ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
// };

var history = new ChatHistory();

history.AddSystemMessage("Handle combat action");

string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "TestImages", "test-1.jpg");
var imageBytes = await File.ReadAllBytesAsync(imagePath);
var message = new ChatMessageContentItemCollection
{
    new TextContent("34cm apart"),
    new ImageContent(imageBytes, "image/jpg")
};

history.AddUserMessage(message);

var result = await kernel.InvokeAsync<string>(prompts["HandleCombat"],
    new() { { "input", message } });

// var result = await chatCompletionService.GetChatMessageContentAsync(
//         history,
//         executionSettings: openAIPromptExecutionSettings,
//         kernel: kernel);

// Print the results
Console.WriteLine("Assistant > " + result);

// Add the message from the agent to the chat history
//history.AddMessage(result.Role, result.Content ?? string.Empty);