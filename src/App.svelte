<script lang="ts">
  import type { ReceiptItem } from './lib/types';
  import FileUpload from './FileUpload.svelte';

  let receiptItems: ReceiptItem[] = $state([]);
</script>

<main>
  <h1>Receipt Extraction</h1>
  <FileUpload onExtract={(data) => (receiptItems = data)} />
  <ul>
    {#each receiptItems as item}
      <li>{item.description} - {item.price}</li>
    {/each}
  </ul>
</main>

<style>
  main {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 3rem 1rem;
    min-height: 100vh;
  }

  h1 {
    font-size: 1.75rem;
    font-weight: 600;
    margin-bottom: 2.5rem;
    color: var(--color-dark);
  }
</style>