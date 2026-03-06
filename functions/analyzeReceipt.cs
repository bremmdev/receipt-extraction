using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.AI.DocumentIntelligence;
using System.Threading.Tasks;
using Azure;

namespace ReceiptExtraction.Functions;

public class analyzeReceipt
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<analyzeReceipt> _logger;

    public analyzeReceipt(DocumentIntelligenceClient client, ILogger<analyzeReceipt> logger)
    {
        _client = client;
        _logger = logger;
    }

    [Function("analyzeReceipt")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        Operation<AnalyzeResult> operation = await _client.AnalyzeDocumentAsync(
        WaitUntil.Completed,
        "prebuilt-receipt",
        new Uri("https://raw.githubusercontent.com/Azure-Samples/cognitive-services-REST-api-samples/master/curl/form-recognizer/rest-api/receipt.png")
        );
        AnalyzeResult result = operation.Value;

        return new OkObjectResult(result);
    }
}