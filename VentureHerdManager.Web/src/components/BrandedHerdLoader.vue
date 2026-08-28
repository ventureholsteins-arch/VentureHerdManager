<script setup lang="ts">
import HerdLoadingScene from './HerdLoadingScene.vue'

withDefaults(defineProps<{ message?: string }>(), {
  message: 'Opening your herd...'
})
</script>

<template>
  <div class="branded-loader" role="status" aria-live="polite">
    <div class="product-brand">
      <img src="/venture-herd-manager-logo.png" alt="Venture Herd Manager">
    </div>

    <HerdLoadingScene :message="message" :delay-ms="0" />

    <div class="company-brand">
      <span>Powered by</span>
      <img src="/venture-ag-marketing-logo.png" alt="Venture Ag Marketing">
      <strong>Custom Ag Application Solutions</strong>
      <p>Your operation shouldn't have to fit somebody else's software.</p>
    </div>
  </div>
</template>

<style scoped>
.branded-loader {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: grid;
  place-content: center;
  justify-items: center;
  gap: 18px;
  padding: 24px 20px;
  box-sizing: border-box;
  background:
    radial-gradient(circle at 50% 42%, rgba(255, 255, 255, 0.74), transparent 34%),
    #e8e2cd;
}

.branded-loader :deep(.retro-loader) {
  width: min(720px, calc(100vw - 40px));
  box-sizing: border-box;
}

.product-brand {
  width: min(430px, 76vw);
  padding: 12px 24px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  background: #0d1a10;
  box-shadow: 0 10px 30px rgba(20, 24, 18, 0.18);
}

.product-brand img {
  display: block;
  width: 100%;
  height: auto;
}

.company-brand {
  display: grid;
  justify-items: center;
  color: #403a2d;
  text-align: center;
}

.company-brand img {
  display: block;
  width: min(210px, 48vw);
  height: auto;
  margin: 2px 0 1px;
}

.company-brand > span,
.company-brand > strong {
  color: #6a624e;
  font-size: 0.58rem;
  font-weight: 800;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.company-brand p {
  margin: 9px 0 0;
  color: #29241c;
  font-family: Georgia, 'Times New Roman', serif;
  font-size: clamp(0.76rem, 1.6vw, 0.94rem);
  font-style: italic;
}

@media (max-width: 560px) {
  .branded-loader { gap: 13px; padding: 18px 14px; }
  .branded-loader :deep(.retro-loader) { width: calc(100vw - 28px); }
  .product-brand { width: min(310px, 78vw); padding: 9px 18px; }
  .company-brand img { width: min(165px, 48vw); }
}
</style>
