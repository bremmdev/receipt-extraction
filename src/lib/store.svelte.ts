import type { SplitReceipt } from "./types";

export const splitReceiptStore = $state<{ receipt: SplitReceipt | null }>({
    receipt: null,
});