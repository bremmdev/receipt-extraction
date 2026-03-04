using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ReceiptExtraction.Functions;

public class analyzeReceipt
{
    private readonly ILogger<analyzeReceipt> _logger;

    public analyzeReceipt(ILogger<analyzeReceipt> logger)
    {
        _logger = logger;
    }

    [Function("analyzeReceipt")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}