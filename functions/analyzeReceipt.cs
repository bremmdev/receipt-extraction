using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.AI.DocumentIntelligence;
using Azure;
using System.Text.Json;

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

    private async Task<IReadOnlyList<ReceiptItem>> GetReceiptItemsAsync(BinaryData imageData, CancellationToken cancellationToken)
    {
        Operation<AnalyzeResult> operation;

        try
        {
            operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", imageData, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Document Intelligence rejected the receipt analysis request.");
            throw new InvalidOperationException("Receipt analysis failed. Make sure the uploaded file is a supported receipt image and try again.", ex);
        }

        AnalyzeResult result = operation.Value;
        if (result.Documents.Count == 0)
        {
            _logger.LogInformation("Receipt analysis completed without detecting any receipt documents.");
            throw new InvalidOperationException("No receipt could be detected in the uploaded image.");
        }

        AnalyzedDocument receiptDocument = result.Documents[0];
        if (!receiptDocument.Fields.TryGetValue("Items", out DocumentField? itemsField) || itemsField.ValueList is null)
        {
            _logger.LogInformation("Receipt analysis found a document but no line items.");
            return Array.Empty<ReceiptItem>();
        }

        List<ReceiptItem> items = new();
        foreach (DocumentField itemField in itemsField.ValueList)
        {
            IReadOnlyDictionary<string, DocumentField>? itemValues = itemField.ValueDictionary;
            if (itemValues is null)
            {
                _logger.LogWarning("Skipping receipt item because the item payload was not a dictionary.");
                continue;
            }

            if (!itemValues.TryGetValue("Description", out DocumentField? descriptionField) ||
                string.IsNullOrWhiteSpace(descriptionField.ValueString))
            {
                _logger.LogWarning("Skipping receipt item because it is missing a description.");
                continue;
            }

            decimal? price = null;
            if (itemValues.TryGetValue("TotalPrice", out DocumentField? totalPriceField))
            {
                price = totalPriceField.ValueCurrency is { Amount: double amount } ? (decimal?)amount : null;
            }

            items.Add(new ReceiptItem(descriptionField.ValueString, price));
        }

        return items;
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
        [BlobInput("rules/rules.json")] string rules,
        CancellationToken cancellationToken)
    {
        (string? Error, BinaryData? ImageData) = await ProcessFormData(req, cancellationToken);

        if (Error is not null)
        {
            return new BadRequestObjectResult(Error);
        }

        if (ImageData is null)
        {
            return new BadRequestObjectResult("No image data found in the request.");
        }

        try
        {
            IReadOnlyList<ReceiptItem> items = await GetReceiptItemsAsync(ImageData, cancellationToken);

            // we get the 'rules' from a rules.json in blob storage
            var rulesDict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(rules);
            var myItemRules = rulesDict?["myItems"] ?? new List<string>();
            var herItemRules = rulesDict?["herItems"] ?? new List<string>();
            SplitReceipt splitReceipt = ReceiptSplitter.SplitReceipt(items, myItemRules, herItemRules);
            return new OkObjectResult(splitReceipt);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Receipt analysis failed validation.");
            return new BadRequestObjectResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing receipt.");
            return new ObjectResult("An unexpected error occurred while processing the receipt.")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}

public record ReceiptItem(string Description, decimal? Price);