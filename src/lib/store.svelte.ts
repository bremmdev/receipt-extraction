import type { ReceiptItem } from "./types";

export const receiptItemsStore = $state<{ items: ReceiptItem[] }>({
    items: [],
});