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
using System.ClientModel.Primitives;

namespace Tests
{
    public class BaseTest
    {
        protected readonly ITestOutputHelper output;
        private readonly IConfiguration configuration;
        protected readonly ILoggerFactory loggerFactory;

        public BaseTest(ITestOutputHelper output)
        {
            this.output = output;
            
            this.configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            this.loggerFactory = LoggerFactory.Create(builder =>
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
        }

        protected string ApiKey => this.configuration["AzureOpenAI:ApiKey"];
        protected string DeploymentName => this.configuration["AzureOpenAI:DeploymentName"];
        protected string Endpoint => this.configuration["AzureOpenAI:Endpoint"];
    }
}
