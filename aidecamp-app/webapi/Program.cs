using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add CORS services to the DI container
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpaClient", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "https://dbtest123.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// adopting approach in https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/Demos/TelemetryWithAppInsights
// Configure OpenTelemetry
var connectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
var resourceBuilder = ResourceBuilder.CreateDefault().AddService("AIdecamp");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder => 
        tracerProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = connectionString;
            })
    )
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = connectionString;
            })
    );

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddConsoleExporter();
        options.AddAzureMonitorLogExporter(configure =>
        {
            configure.ConnectionString = connectionString;
        });
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Apply the CORS policy to your endpoints
app.UseCors("AllowSpaClient");

// Add routing middleware
app.UseRouting();

// Register the routes
app.UseEndpoints(endpoints =>
{
    endpoints.MapBasicRoutes();
    endpoints.MapAIdecampRoutes(app.Configuration, loggerFactory);
});

app.Run();
