using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
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

public static class AIdecampRoutes
{
    public static void MapAIdecampRoutes(this IEndpointRouteBuilder endpoints, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        endpoints.MapPost("/combat", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            var file = form.Files.GetFile("image");

            if (file == null || file.Length == 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("No image file uploaded.");
                return;
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Uploaded file is not an image.");
                return;
            }

            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var apiKey = configuration["AzureOpenAI:ApiKey"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"];
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            
            var builder = Kernel.CreateBuilder();
            builder.Services.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName, 
                endpoint: endpoint,
                apiKey: apiKey
                );
            builder.Services.AddSingleton(loggerFactory);
            var kernel = builder.Build();
            
            kernel.ImportPluginFromType<TurnManagerPlugin>();
            KernelFunction calculateOutcome = kernel.Plugins.GetFunction("TurnManagerPlugin", "calculateOutcome");

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
            {
                Temperature = 0.1,
                //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                //FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                FunctionChoiceBehavior = FunctionChoiceBehavior.Required(functions: [calculateOutcome]),
                ResponseFormat = ChatResponseFormat.JsonObject
                //TODO: https://devblogs.microsoft.com/semantic-kernel/using-json-schema-for-structured-output-in-net-for-openai-models/
            };

            // Create a logger using the logger factory
            var logger = loggerFactory.CreateLogger<Program>();
            // Log some information
            logger.LogInformation("Processing image for combat endpoint.");

            var history = new ChatHistory();

            string promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "prompts", "prompt.md");
            string systemPrompt = await File.ReadAllTextAsync(promptFilePath);
            history.AddSystemMessage(systemPrompt);

            var message = new ChatMessageContentItemCollection
            {
                new TextContent(@"Identify the firing and target toy soldiers in this picture, then calculate the outcome of the wargame scenario. Return the scenario and outcome as JSON"),
                new ImageContent(imageBytes, "image/jpg")
            };
            history.AddUserMessage(message);

            var result = await chatCompletionService.GetChatMessageContentAsync( 
                    history,
                    executionSettings: promptExecutionSettings,
                    kernel: kernel);

            // Add the message from the agent to the chat history
            history.AddMessage(result.Role, result.Content ?? string.Empty);

            // Print the results
            logger.LogInformation("Assistant > " + result);

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

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(result.Content);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Outcome is not present in the assistant result.");
                }
            }
        });
    }
}