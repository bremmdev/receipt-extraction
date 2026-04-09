<script lang="ts">
    import { receiptItemsStore } from '../lib/store.svelte';
</script>

{#if receiptItemsStore.items.length > 0}
<section class="extracted-items">
<h2>Extracted Items</h2>
  <ul>
    {#each receiptItemsStore.items as item}
      <li><span>{item.description}</span> <span>€{item.price?.toFixed(2)}</span></li>
    {/each}
  </ul>
  </section>
{/if}

<style>

.extracted-items {
    width: 100%;
    max-width: 30rem;
    margin-top: 4rem;

    h2 {
    font-size: 1.5rem;
    font-weight: 500;
    margin-bottom: 1rem;
    text-align: center;
    color: var(--color-dark);
  }

  ul{
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    margin-top: 2rem;
    width: 100%;
    max-width: 30rem;
  }

  li{
    font-size: 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-weight: 500;
    color: var(--color-dark);
    width: 100%;
  }
}
</style>