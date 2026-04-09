<script lang="ts">
  import FileUpload from './components/FileUpload.svelte';
  import ExtractedItems from './components/ExtractedItems.svelte';
</script>

<main>
  <h1>Receipt Extraction</h1>
  <FileUpload />
  <ExtractedItems />
</main>

<style>
  h1 {
    font-size: 2rem;
    font-weight: 500;
    margin-bottom: 2.5rem;
    color: var(--color-dark);
  }
</style>