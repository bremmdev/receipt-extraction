using ReceiptExtraction.Functions;

internal class ReceiptSplitter
{
    private static readonly List<string> myItems = ["tuc"];
    private static readonly List<string> herItems = ["hummus", "kaas"];
    internal static SplitReceipt SplitReceipt(IReadOnlyList<ReceiptItem> receiptItems)
    {
        var myItemsList = receiptItems
            .Where(item => myItems.Any(k => item.Description.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var herItemsList = receiptItems.Except(myItemsList)
            .Where(item => herItems.Any(k => item.Description.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var sharedItemsList = receiptItems.Except(myItemsList).Except(herItemsList).ToList();
        return new SplitReceipt(new ReceiptCategory(myItemsList, myItemsList.Sum(item => item.Price ?? 0)), new ReceiptCategory(herItemsList, herItemsList.Sum(item => item.Price ?? 0)), new ReceiptCategory(sharedItemsList, sharedItemsList.Sum(item => item.Price ?? 0)));
    }
}

public record ReceiptCategory(IReadOnlyList<ReceiptItem> Items, decimal TotalPrice);
public record SplitReceipt(ReceiptCategory Mine, ReceiptCategory Hers, ReceiptCategory Shared);