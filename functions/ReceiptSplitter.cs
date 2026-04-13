using ReceiptExtraction.Functions;

internal class ReceiptSplitter
{
    internal static SplitReceipt SplitReceipt(IReadOnlyList<ReceiptItem> receiptItems, List<string> myItemRules, List<string> herItemRules)
    {
        var myItemsList = receiptItems
            .Where(item => myItemRules.Any(k => item.Description.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var herItemsList = receiptItems.Except(myItemsList)
            .Where(item => herItemRules.Any(k => item.Description.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var sharedItemsList = receiptItems.Except(myItemsList).Except(herItemsList).ToList();
        return new SplitReceipt(new ReceiptCategory(myItemsList, myItemsList.Sum(item => item.Price ?? 0)), new ReceiptCategory(herItemsList, herItemsList.Sum(item => item.Price ?? 0)), new ReceiptCategory(sharedItemsList, sharedItemsList.Sum(item => item.Price ?? 0)));
    }
}

public record ReceiptCategory(IReadOnlyList<ReceiptItem> Items, decimal TotalPrice);
public record SplitReceipt(ReceiptCategory Mine, ReceiptCategory Hers, ReceiptCategory Shared);