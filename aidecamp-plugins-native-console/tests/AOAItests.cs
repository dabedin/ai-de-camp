using System.Text;
using System.IO;
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


namespace Tests;
public class AOAItest:BaseTest
{
    public AOAItest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task NoImage_NoPromptNoPlugin()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: DeploymentName, 
            endpoint: Endpoint,
            apiKey: ApiKey
            );
        builder.Services.AddSingleton(this.loggerFactory);
        builder.Services.AddSingleton<IPromptRenderFilter, PromptFilterTrace>();
        var kernel = builder.Build();

        kernel.ImportPluginFromType<TimeInformationPlugin>();
        kernel.ImportPluginFromType<TurnManagerPlugin>();

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
        {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var history = new ChatHistory();

        var message = new ChatMessageContentItemCollection
        {
            new TextContent(@"Describe the sun.")
        };

        history.AddUserMessage(message);

        var result = await chatCompletionService.GetChatMessageContentAsync( 
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel);

        // Add the message from the agent to the chat history
        history.AddMessage(result.Role, result.Content ?? string.Empty);

         // Assert
        Assert.NotNull(result);
        Assert.Equal("Assistant", result.Role.ToString());
        Assert.NotEmpty(result.Content);
        
        // Print the results
        output.WriteLine("Assistant > " + result);
    }

    [Fact]
    public async Task Image_IdentifySoldiers_PromptWithPlugin()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: DeploymentName, 
            endpoint: Endpoint,
            apiKey: ApiKey
            );
        builder.Services.AddSingleton(this.loggerFactory);
        builder.Services.AddSingleton<IPromptRenderFilter, PromptFilterTrace>();
        var kernel = builder.Build();

        kernel.ImportPluginFromType<TimeInformationPlugin>();
        kernel.ImportPluginFromType<TurnManagerPlugin>();

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
        {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var history = new ChatHistory();

        string promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "prompts", "prompt.md");
        string systemPrompt = await File.ReadAllTextAsync(promptFilePath);
        history.AddSystemMessage(systemPrompt);

        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "images", "test-3-20.jpg");
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var message = new ChatMessageContentItemCollection
        {
            new TextContent(@"Identify the firing and target toy soldiers in this picture and return the JSON."),
            new ImageContent(imageBytes, "image/jpg")
        };
        history.AddUserMessage(message);

        var result = await chatCompletionService.GetChatMessageContentAsync( 
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel);

        // Add the message from the agent to the chat history
        history.AddMessage(result.Role, result.Content ?? string.Empty);

         // Assert
        Assert.NotNull(result);
        Assert.Equal("Assistant", result.Role.ToString());
        Assert.NotEmpty(result.Content);

        // Print the results
        output.WriteLine("Assistant > " + result);
    }

    [Fact]
    public async Task Image_IdentifySoldiers_PromptNoPlugin()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: DeploymentName, 
            endpoint: Endpoint,
            apiKey: ApiKey
            );
        builder.Services.AddSingleton(this.loggerFactory);
        //builder.Services.AddSingleton<IPromptRenderFilter, PromptFilterTrace>();
        var kernel = builder.Build();

        // kernel.ImportPluginFromType<TimeInformationPlugin>();
        // kernel.ImportPluginFromType<TurnManagerPlugin>();

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
        {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var history = new ChatHistory();

        string promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "prompts", "prompt.md");
        string systemPrompt = await File.ReadAllTextAsync(promptFilePath);
        history.AddSystemMessage(systemPrompt);
        
        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "images", "test-3-20.jpg");
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var message = new ChatMessageContentItemCollection
        {
            new TextContent(@"Identify the firing and target toy soldiers in this picture and return the JSON."),
            new ImageContent(imageBytes, "image/jpg")
        };
        history.AddUserMessage(message);

        var result = await chatCompletionService.GetChatMessageContentAsync( 
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel);

        // Add the message from the agent to the chat history
        history.AddMessage(result.Role, result.Content ?? string.Empty);

         // Assert
        Assert.NotNull(result);
        Assert.Equal("Assistant", result.Role.ToString());
        Assert.NotEmpty(result.Content);

        // Print the results
        output.WriteLine("Assistant > " + result);
    }

    [Fact]
    public async Task Image_PromptWithPlugin()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: DeploymentName, 
            endpoint: Endpoint,
            apiKey: ApiKey
            );
        builder.Services.AddSingleton(this.loggerFactory);
        //builder.Services.AddSingleton<IPromptRenderFilter, PromptFilterTrace>();
        var kernel = builder.Build();

        kernel.ImportPluginFromType<TurnManagerPlugin>();

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var history = new ChatHistory();

        string promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "prompts", "prompt.md");
        string systemPrompt = await File.ReadAllTextAsync(promptFilePath);
        history.AddSystemMessage(systemPrompt);
        
        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "images", "test-3-20.jpg");
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var message = new ChatMessageContentItemCollection
        {
            new TextContent(@"Identify the firing and target toy soldiers in this picture, then calculate the outcome of the wargame scenario."),
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

         // Assert
        Assert.NotNull(result);
        Assert.Equal("Assistant", result.Role.ToString());
        Assert.NotEmpty(result.Content);

        // Print the results
        output.WriteLine("Assistant > " + result);
    }

    [Fact]
    public async Task DummyImage_PromptWithPlugin()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: DeploymentName, 
            endpoint: Endpoint,
            apiKey: ApiKey
            );
        builder.Services.AddSingleton(this.loggerFactory);
        //builder.Services.AddSingleton<IPromptRenderFilter, PromptFilterTrace>();
        var kernel = builder.Build();

        kernel.ImportPluginFromType<TurnManagerPlugin>();

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
        {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var history = new ChatHistory();

        string promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "prompts", "prompt.md");
        string systemPrompt = await File.ReadAllTextAsync(promptFilePath);
        history.AddSystemMessage(systemPrompt);
        
        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "images", "test-munro.jpg");
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var message = new ChatMessageContentItemCollection
        {
            new TextContent(@"Identify the firing and target toy soldiers in this picture, then calculate the outcome of the wargame scenario."),
            new ImageContent(imageBytes, "image/jpg")
        };
        history.AddUserMessage(message);

        var result = await chatCompletionService.GetChatMessageContentAsync( 
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel);

        // Add the message from the agent to the chat history
        history.AddMessage(result.Role, result.Content ?? string.Empty);

         // Assert
        Assert.NotNull(result);
        Assert.Equal("Assistant", result.Role.ToString());
        Assert.NotEmpty(result.Content);

        // Print the results
        output.WriteLine("Assistant > " + result);
    }

    [Fact]
    public async Task Json_PromptWithPlugin()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: DeploymentName, 
            endpoint: Endpoint,
            apiKey: ApiKey
            );
        builder.Services.AddSingleton(this.loggerFactory);
        builder.Services.AddSingleton<IPromptRenderFilter, PromptFilterTrace>();
        var kernel = builder.Build();

        kernel.ImportPluginFromType<TimeInformationPlugin>();
        kernel.ImportPluginFromType<TurnManagerPlugin>();

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        AzureOpenAIPromptExecutionSettings promptExecutionSettings  = new()
        {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var history = new ChatHistory();
        
        string promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "prompts", "prompt.md");
        string systemPrompt = await File.ReadAllTextAsync(promptFilePath);
        history.AddSystemMessage(systemPrompt);

        var message = new ChatMessageContentItemCollection
        {
            new TextContent(@"Based on this JSON scenario, calculate the outcome of the wargame scenario."),
            new TextContent(@"{
                ""Firing"": {
                    ""Pose"": ""standing"",
                    ""Weapon"": ""rifle""
                },
                ""Target"": {
                    ""Pose"": ""crouched"",
                    ""Weapon"": ""pistol""
                },
                ""Distance"": {
                    ""Value"": 50,
                    ""Unit"": ""cm"",
                    ""Estimated"": ""false""
                }
            }")
        };
        history.AddUserMessage(message);

        var result = await chatCompletionService.GetChatMessageContentAsync( 
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel);

        // Add the message from the agent to the chat history
        history.AddMessage(result.Role, result.Content ?? string.Empty);

         // Assert
        Assert.NotNull(result);
        Assert.Equal("Assistant", result.Role.ToString());
        Assert.NotEmpty(result.Content);

        // Print the results
        output.WriteLine("Assistant > " + result);

    }
}