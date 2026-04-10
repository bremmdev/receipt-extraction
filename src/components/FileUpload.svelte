<script lang="ts">
  import { receiptItemsStore } from '../lib/store.svelte';

  /* STATE */
  let isDragging = $state(false);
  let selectedFile: File | null = $state(null);
  let isLoading = $state(false);
  let error = $state<string | null>(null);
  let fileInputEl: HTMLInputElement;

  /* HANDLERS */
  function handleDragOver(e: DragEvent) {
    e.preventDefault();
    isDragging = true;
  }

  function handleDragLeave(e: DragEvent) {
    if (!(e.currentTarget as HTMLElement).contains(e.relatedTarget as Node)) {
      isDragging = false;
    }
  }

  function handleDrop(e: DragEvent) {
    e.preventDefault();
    isDragging = false;
    const file = e.dataTransfer?.files[0];
    if (file && file.type.startsWith('image/')) {
      selectedFile = file;
    }
  }

  function handleFileChange(e: Event) {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) {
      selectedFile = file;
    }
  }

  function openFilePicker() {
    fileInputEl.click();
  }

  function handleClearSelection() {
    selectedFile = null;
    error = null;
    receiptItemsStore.items = [];
  }

  async function handleSubmit(e: Event) {
    e.preventDefault();
    const formData = new FormData(e.target as HTMLFormElement);
    formData.append('file', selectedFile as File);
    error = null;
    receiptItemsStore.items = [];
    isLoading = true;
    try {
      if(!selectedFile) {
        throw new Error('No file selected');
      }

      const response = await fetch('https://receipt-extraction-func.azurewebsites.net/api/analyzeReceipt', {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        throw new Error('Failed to extract receipt');
      }

      const data = await response.json();
      receiptItemsStore.items = data;
    } catch (e) {
      if (e instanceof Error) {
        error = `${e.message}`;
      } else {
        error = 'Extracting receipt failed. Please try again.';
      }
    } finally {
      isLoading = false;
    }
  }

  let previewUrl = $derived(selectedFile ? URL.createObjectURL(selectedFile) : null);
</script>

<form onsubmit={handleSubmit}>
<div
  class="dropzone"
  class:dragging={isDragging}
  class:has-file={selectedFile}
  role="button"
  tabindex="0"
  aria-label="Upload receipt image"
  ondragover={handleDragOver}
  ondragleave={handleDragLeave}
  ondrop={handleDrop}
  onclick={openFilePicker}
  onkeydown={(e) => e.key === 'Enter' || e.key === ' ' ? openFilePicker() : null}
>
  <input
    bind:this={fileInputEl}
    type="file"
    accept="image/*"
    hidden
    onchange={handleFileChange}
  />

  {#if previewUrl}
    <img src={previewUrl} alt="Receipt preview" class="preview" />
    <p class="file-name">{selectedFile?.name}</p>
  {:else}
    <div class="upload-icon" aria-hidden="true">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
        <polyline points="17 8 12 3 7 8"/>
        <line x1="12" y1="3" x2="12" y2="15"/>
      </svg>
    </div>
    <p class="primary-text">Drag & drop your receipt here</p>
    <p class="secondary-text">or <span class="browse-link">browse files</span></p>
    <p class="hint">Supports PNG, JPG, WEBP</p>
  {/if}
</div>
<div class="button-container">
  <button class="btn-primary" type="submit" disabled={isLoading}>{isLoading ? 'Extracting...' : 'Extract Receipt'}</button>
  <button class="btn-secondary" type="button" onclick={handleClearSelection} disabled={isLoading}>Clear Selection</button>
</div>
{#if error}
  <div class="error-container">
    <p class="error">{error}</p>
  </div>
{/if}
</form>

<style>
  form {
    width: 100%;
  }

  .dropzone {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    width: 100%;
    min-height: 20rem;
    border: 2px dashed color-mix(in oklch, var(--color-muted) 40%, transparent);
    border-radius: 1rem;
    background: color-mix(in oklch, var(--color-highlight) 30%, white);
    cursor: pointer;
    padding: 2rem;
    box-sizing: border-box;
    transition: border-color 0.2s, background 0.2s;
    outline: none;
    gap: 0.5rem;
    user-select: none;
  }

  .dropzone:hover,
  .dropzone:focus-visible {
    border-color: var(--color-accent);
    background: color-mix(in oklch, var(--color-highlight) 55%, white);
  }

  .dropzone.dragging {
    border-color: var(--color-accent);
    background: color-mix(in oklch, var(--color-highlight) 70%, white);
  }

  .dropzone.has-file {
    border-style: solid;
    border-color: var(--color-accent);
  }

  .upload-icon {
    width: 48px;
    height: 48px;
    color: var(--color-muted);
    margin-bottom: 0.5rem;
  }

  .upload-icon svg {
    width: 100%;
    height: 100%;
  }

  .dragging .upload-icon {
    color: var(--color-accent);
  }

  .primary-text {
    font-size: 1rem;
    font-weight: 500;
    color: var(--color-dark);
    margin: 0;
  }

  .secondary-text {
    font-size: 0.9rem;
    color: var(--color-muted);
    margin: 0;
  }

  .browse-link {
    color: var(--color-accent);
    font-weight: 500;
    text-decoration: underline;
    text-underline-offset: 2px;
  }

  .hint {
    font-size: 0.9rem;
    color: var(--color-muted);
    font-weight: 500;
    margin: 0.25rem 0 0;
  }

  .preview {
    max-width: 100%;
    max-height: 200px;
    border-radius: 0.5rem;
    object-fit: contain;
  }

  .file-name {
    font-size: 0.85rem;
    color: var(--color-muted);
    margin: 0.5rem 0 0;
    word-break: break-all;
    text-align: center;
  }

  .button-container {
    display: flex;
    gap: 1rem;
    margin-top: 2rem;
    justify-content: center;
    width: 100%;
  }

  button {
    padding: 0.75rem 1.5rem;
    border-radius: 0.5rem;
    font-family: inherit;
    font-size: 1rem;
    font-weight: 500;
    border: none;
    color: white;
    cursor: pointer;
    transition: background 0.2s;

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    &.btn-primary {
      background: var(--color-accent);
      color: white;
      &:hover {
        background: color-mix(in oklch, var(--color-accent) 80%, white);
      }
    }

    &.btn-secondary {
      background: white;
      color: var(--color-dark);
      border: 1px solid var(--color-accent);
      &:hover {
        background: color-mix(in oklch, var(--color-accent) 10%, white);
      }
    }
  }

  .error {
      color: red;
      font-weight: 500;
    }

    .error-container {
      margin-top: 1rem;
      text-align: center;
    }
</style>
