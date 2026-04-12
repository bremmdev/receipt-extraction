<script lang="ts">
  import { splitReceiptStore } from "../lib/store.svelte";

  const emptyReceiptCategory = { items: [], totalPrice: 0 };

  let receipt = $derived(splitReceiptStore.receipt);

  let mine = $derived(receipt?.mine ?? emptyReceiptCategory);
  let hers = $derived(receipt?.hers ?? emptyReceiptCategory);
  let shared = $derived(receipt?.shared ?? emptyReceiptCategory);

  let totalPrice = $derived(
    mine.totalPrice + hers.totalPrice + shared.totalPrice,
  );

  let myTotalPrice = $derived(mine.totalPrice + shared.totalPrice / 2);
</script>

{#if receipt}
  <section class="extracted-items">
    <h2>Extracted Items</h2>

    <h3>Mine (€{mine.totalPrice.toFixed(2)})</h3>
    <ul>
      {#each mine.items as item}
        <li>
          <span>{item.description}</span>
          <span>€{item.price?.toFixed(2)}</span>
        </li>
      {/each}
    </ul>

    <h3>Hers (€{hers.totalPrice.toFixed(2)})</h3>
    <ul>
      {#each hers.items as item}
        <li>
          <span>{item.description}</span>
          <span>€{item.price?.toFixed(2)}</span>
        </li>
      {/each}
    </ul>

    <h3>Shared (€{shared.totalPrice.toFixed(2)})</h3>
    <ul>
      {#each shared.items as item}
        <li>
          <span>{item.description}</span>
          <span>€{item.price?.toFixed(2)}</span>
        </li>
      {/each}
    </ul>

    <p class="total-price">Total price: €{totalPrice.toFixed(2)}</p>
    <p class="total-price">My total price: €{myTotalPrice.toFixed(2)}</p>
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

    ul {
      list-style: none;
      padding: 0;
      margin: 0;
      display: flex;
      flex-direction: column;
      gap: 1rem;
      width: 100%;
      max-width: 30rem;
      margin-block: 1rem;
    }

    li {
      font-size: 1rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-weight: 500;
      color: var(--color-dark);
      width: 100%;
    }
  }

  .total-price {
    font-size: 1.25rem;
    font-weight: 500;
    color: var(--color-dark);
    text-align: center;
    margin-top: 1rem;
  }
</style>
