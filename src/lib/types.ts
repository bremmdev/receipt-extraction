export interface ReceiptItem {
    description: string;
    price: number;
}

export interface ReceiptCategory {
    items: ReceiptItem[];
    totalPrice: number;
}

export interface SplitReceipt {
    mine: ReceiptCategory;
    hers: ReceiptCategory;
    shared: ReceiptCategory;
}