using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.AI.DocumentIntelligence;
using Azure;
using System.Text;
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

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? GetSpanText(IReadOnlyList<DocumentSpan>? spans, string? content)
    {
        if (spans is null || spans.Count == 0 || string.IsNullOrEmpty(content))
        {
            return null;
        }

        StringBuilder builder = new();

        foreach (DocumentSpan span in spans.OrderBy(span => span.Offset))
        {
            if (span.Offset >= content.Length)
            {
                continue;
            }

            int length = Math.Min(span.Length, content.Length - span.Offset);
            if (length <= 0)
            {
                continue;
            }

            builder.Append(content.Substring(span.Offset, length));
            builder.Append(' ');
        }

        return NormalizeText(builder.ToString());
    }

    private static string? GetFieldText(DocumentField? field, AnalyzeResult result)
    {
        return NormalizeText(field?.ValueString)
            ?? NormalizeText(field?.Content)
            ?? GetSpanText(field?.Spans, result.Content);
    }

    // Raw OCR item text often includes quantity and price values; strip those once so the fallback description keeps only the product text.
    private static string RemoveFirstInvariant(string source, string value)
    {
        int index = source.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return source;
        }

        return string.Concat(source.AsSpan(0, index), " ", source.AsSpan(index + value.Length));
    }

    private static string? GetDescription(DocumentField itemField, IReadOnlyDictionary<string, DocumentField> itemValues, AnalyzeResult result)
    {
        if (itemValues.TryGetValue("Description", out DocumentField? descriptionField))
        {
            string? mappedDescription = GetFieldText(descriptionField, result);
            if (!string.IsNullOrWhiteSpace(mappedDescription))
            {
                return mappedDescription;
            }
        }

        string? rawItemText = GetFieldText(itemField, result);
        if (string.IsNullOrWhiteSpace(rawItemText))
        {
            return null;
        }

        string fallbackDescription = rawItemText;
        foreach (string fieldName in new[] { "Quantity", "Price", "TotalPrice" })
        {
            if (!itemValues.TryGetValue(fieldName, out DocumentField? field))
            {
                continue;
            }

            string? fieldText = GetFieldText(field, result);
            if (string.IsNullOrWhiteSpace(fieldText))
            {
                continue;
            }

            fallbackDescription = RemoveFirstInvariant(fallbackDescription, fieldText);
        }

        return NormalizeText(fallbackDescription);
    }

    private async Task<IReadOnlyList<ReceiptItem>> GetReceiptItemsAsync(BinaryData imageData, CancellationToken cancellationToken)
    {
        Operation<AnalyzeResult> operation;

        try
        {
            operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", imageData, cancellationToken: cancellationToken);
        }

        // catch the error and return the appropriate error message and status code
        catch (RequestFailedException ex)
        {
            string message = ex.Status switch
            {
                400 => "Receipt analysis failed. Make sure the uploaded file is a supported receipt image and try again.",
                401 or 403 => "Receipt analysis failed due to an authorization error.",
                404 => "Receipt analysis failed. The analysis model was not found.",
                429 => "Receipt analysis failed due to too many requests. Please try again later.",
                _ => "Receipt analysis failed due to an unexpected error. Please try again later."
            };

            int responseStatus = ex.Status switch
            {
                400 => StatusCodes.Status400BadRequest,
                401 or 403 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status500InternalServerError
            };

            _logger.LogWarning(ex, "Document Intelligence request failed with status {Status}.", ex.Status);

            var ioex = new InvalidOperationException(message, ex);
            ioex.Data["StatusCode"] = responseStatus;
            throw ioex;
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

            decimal? price = null;
            if (itemValues.TryGetValue("TotalPrice", out DocumentField? totalPriceField))
            {
                price = totalPriceField.ValueCurrency is { Amount: double amount } ? (decimal?)amount : null;
            }

            string? description = GetDescription(itemField, itemValues, result);
            if (string.IsNullOrWhiteSpace(description))
            {
                if (price is null)
                {
                    _logger.LogWarning("Skipping receipt item because both the description and price were missing.");
                    continue;
                }

                description = "Unknown item"; // Last resort description
                _logger.LogWarning("Using placeholder description for receipt item with price {Price}. Raw item text: {RawItemText}", price, GetFieldText(itemField, result) ?? "<none>");
            }

            items.Add(new ReceiptItem(description, price));
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
            _logger.LogWarning(ex, "Receipt analysis failed.");

            // carry the intended status code up from GetReceiptItemsAsync
            return ex.Data["StatusCode"] is int statusCode
                ? new ObjectResult(ex.Message) { StatusCode = statusCode }
                : new BadRequestObjectResult(ex.Message);
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