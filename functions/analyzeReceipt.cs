using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.AI.DocumentIntelligence;
using Azure;

namespace ReceiptExtraction.Functions;

public class AnalyzeReceipt
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<AnalyzeReceipt> _logger;

    public AnalyzeReceipt(DocumentIntelligenceClient client, ILogger<AnalyzeReceipt> logger)
    {
        _client = client;
        _logger = logger;
    }

    private async Task<IReadOnlyList<ReceiptItem>> GetReceiptItemsAsync(BinaryData imageData)
    {
        Operation<AnalyzeResult> operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", imageData);
        AnalyzeResult result = operation.Value;
        DocumentField itemsField = result.Documents[0].Fields["Items"];
        return itemsField.ValueList.Select(item => new ReceiptItem(item.ValueDictionary["Description"].ValueString, item.ValueDictionary["TotalPrice"].ValueCurrency?.Amount)).ToList();
    }

    private async Task<(string? Error, BinaryData? ImageData)> ProcessFormData(HttpRequest req, CancellationToken cancellationToken)
    {
        if (!req.HasFormContentType)
        {
            return ("Content-Type must be multipart/form-data (e.g. use form field \"file\" for the image).", null);
        }

        // Allow the multipart parser to read/seek the body reliably (needed on some hosts).
        req.EnableBuffering();

        IFormCollection form = await req.ReadFormAsync(cancellationToken);
        IFormFile? file = form.Files.GetFile("file");
        if (file is null)
        {
            return ("No file part found in the request.", null);
        }

        await using MemoryStream buffer = new();
        await file.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
        {
            return ("The \"file\" part was empty. Please try again with a valid image file.", null);
        }

        BinaryData imageData = BinaryData.FromBytes(buffer.ToArray());
        return (null, imageData);
    }

    [Function("AnalyzeReceipt")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        (string? Error, BinaryData? ImageData) result = await ProcessFormData(req, cancellationToken);

        if (result.Error is not null)
        {
            return new BadRequestObjectResult(result.Error);
        }

        if (result.ImageData is null)
        {
            return new BadRequestObjectResult("No image data found in the request.");
        }

        IReadOnlyList<ReceiptItem> items = await GetReceiptItemsAsync(result.ImageData);
        return new OkObjectResult(items);
    }
}

public record ReceiptItem(string Description, double? Price);