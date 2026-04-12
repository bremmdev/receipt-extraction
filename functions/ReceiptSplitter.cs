using ReceiptExtraction.Functions;

internal class ReceiptSplitter
{
    private static readonly List<string> myItems = ["augurk", "beyond", "blauwe bes", "espress", "fanta", "garnalen", "gyoza", "kuhne", "lay", "lekkerbek", "lindt", "maki", "pokebowl met garnaal", "tuc", "uien", "witte kool", "zalm"];
    private static readonly List<string> herItems = ["cranberry", "hummus", "kaas", "loempia", "olijven", "perla", "pokebowl vegatarisch"];
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