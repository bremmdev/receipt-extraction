<script lang="ts">
  import type { ReceiptItem } from './lib/types';
  import FileUpload from './components/FileUpload.svelte';
  import ExtractedItems from './components/ExtractedItems.svelte';

  let receiptItems: ReceiptItem[] = $state([]);
</script>

<main>
  <h1>Receipt Extraction</h1>
  <FileUpload onExtract={(data: ReceiptItem[]) => (receiptItems = data)} />
  <ExtractedItems items={receiptItems} />
</main>

<style>
  h1 {
    font-size: 2rem;
    font-weight: 500;
    margin-bottom: 2.5rem;
    color: var(--color-dark);
  }
</style>