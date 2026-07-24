<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import { getAppearance, type AppearanceSetting } from './api/appearance'

const appearance = ref<AppearanceSetting | null>(null)

onMounted(async () => {
  try {
    appearance.value = await getAppearance()
  } catch (error) {
    console.error('Failed to load app appearance:', error)
  }
})

const appStyle = computed(() => ({
  '--brand-bg': `url('${appearance.value?.backgroundImageUrl || '/herd-manager-bg.jpg'}')`,
  '--brand-bg-opacity': `${appearance.value?.backgroundOpacity ?? 0.15}`,
  '--brand-overlay-opacity': `${appearance.value?.overlayOpacity ?? 0.85}`,
  '--brand-accent': appearance.value?.accentColor || '#31572c'
}))
</script>

<template>
  <div class="app-shell" :style="appStyle">
    <div class="app-background" />

    <div class="app-content">
      <RouterView />
    </div>
  </div>
</template>

<style scoped>
.app-shell {
  position: relative;
  min-height: 100vh;
  background:
    radial-gradient(circle at top left, rgba(49, 87, 44, 0.18), transparent 24%),
    linear-gradient(180deg, #f5f7f2 0%, #eef4ec 100%);
}

.app-background {
  position: fixed;
  inset: 0;
  background-image: var(--brand-bg);
  background-size: cover;
  background-position: center;
  opacity: var(--brand-bg-opacity);
  pointer-events: none;
  filter: saturate(1.02);
}

.app-content {
  position: relative;
  z-index: 1;
}
</style>