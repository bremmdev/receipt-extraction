using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.AI.DocumentIntelligence;
using Azure;

namespace ReceiptExtraction.Functions;


public record ReceiptItem(string Description, double? Price);

public class analyzeReceipt
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<analyzeReceipt> _logger;

    public analyzeReceipt(DocumentIntelligenceClient client, ILogger<analyzeReceipt> logger)
    {
        _client = client;
        _logger = logger;
    }


    private async Task<ReceiptItem[]> GetReceiptItemsAsync(string imageUrl)
    {
        Operation<AnalyzeResult> operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", new Uri(imageUrl));
        AnalyzeResult result = operation.Value;
        DocumentField itemsField = result.Documents[0].Fields["Items"];
        return itemsField.ValueList.Select(item => new ReceiptItem(item.ValueDictionary["Description"].ValueString, item.ValueDictionary["TotalPrice"].ValueCurrency?.Amount)).ToArray();
    }

    [Function("analyzeReceipt")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        string imageUrl = req.Query["imageUrl"].ToString() ?? "";

        if (string.IsNullOrEmpty(imageUrl))
        {
            return new BadRequestObjectResult("Image URL is required");
        }

        ReceiptItem[] items = await GetReceiptItemsAsync(imageUrl);

        return new OkObjectResult(items);
    }
}