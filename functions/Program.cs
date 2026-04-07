using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // limit the size of the multipart request to 5MB
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton(_ =>
    {
        string endpoint = Environment.GetEnvironmentVariable(
            "DOCUMENT_INTELLIGENCE_ENDPOINT"
        ) ?? throw new InvalidOperationException(
            "Missing env var: DOCUMENT_INTELLIGENCE_ENDPOINT"
        );

        string apiKey = Environment.GetEnvironmentVariable(
            "DOCUMENT_INTELLIGENCE_API_KEY"
        ) ?? throw new InvalidOperationException(
            "Missing env var: DOCUMENT_INTELLIGENCE_API_KEY"
        );

        return new DocumentIntelligenceClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey)
        );
    });

builder.Build().Run();