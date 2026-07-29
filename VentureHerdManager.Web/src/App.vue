<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getAppearance, type AppearanceSetting } from './api/appearance'
import { resetDemo } from './api/demo'

const appearance = ref<AppearanceSetting | null>(null)
const isDemoOnly = import.meta.env.VITE_DEMO_ONLY === 'true'
const demoResetEnabled = import.meta.env.VITE_DEMO_RESET_ENABLED === 'true'
const demoResetting = ref(false)

onMounted(async () => {
  if (isDemoOnly) {
    return
  }

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

async function handleDemoReset() {
  demoResetting.value = true
  try {
    if (demoResetEnabled) {
      await resetDemo()
    }
    window.location.href = '/'
  } catch (error) {
    console.warn('Demo reset unavailable, reopening demo without reset:', error)
    window.location.href = '/'
  } finally {
    demoResetting.value = false
  }
}

async function openDemoFast() {
  window.location.href = '/'
}
</script>

<template>
  <div class="app-shell" :style="appStyle">
    <div class="app-background" />

    <div v-if="isDemoOnly" class="demo-banner">
      <span>{{ demoResetEnabled ? 'DEMO MODE - open instantly or reset for fresh seed data' : 'DEMO MODE - running without API reset' }}</span>
      <div class="demo-actions">
        <button type="button" class="secondary" :disabled="demoResetting" @click="openDemoFast">
          Open Fast
        </button>
        <button type="button" :disabled="demoResetting || !demoResetEnabled" @click="handleDemoReset">
          {{ demoResetting ? 'Resetting...' : 'Reset Demo' }}
        </button>
      </div>
    </div>

    <div class="app-content">
      <RouterView />
    </div>

    <footer class="app-footer">
      <span>Powered by <strong>Venture Ag Marketing</strong></span>
      <span class="footer-sep">·</span>
      <span>Custom Application Solutions</span>
      <span class="footer-sep">·</span>
      <span>&copy; {{ new Date().getFullYear() }} All Rights Reserved</span>
    </footer>
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

.demo-banner {
  position: sticky;
  top: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 8px 16px;
  background: #31572c;
  color: #fff;
  font-size: 0.82rem;
  font-weight: 600;
  letter-spacing: 0.04em;
}

.demo-banner button {
  border: 1px solid rgba(255, 255, 255, 0.5);
  border-radius: 5px;
  background: transparent;
  color: #fff;
  padding: 4px 12px;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
}

.demo-banner .demo-actions {
  display: flex;
  gap: 8px;
}

.demo-banner button.secondary {
  background: rgba(5, 10, 18, 0.42);
}

.demo-banner button:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.15);
}

.demo-banner button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.app-footer {
  position: relative;
  z-index: 2;
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 8px;
  padding: 14px 16px;
  background: #0d1a10;
  color: rgba(255,255,255,0.42);
  font-size: 0.76rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-align: center;
}

.app-footer strong {
  color: rgba(255,255,255,0.72);
  font-weight: 900;
}

.footer-sep {
  color: rgba(255,255,255,0.2);
}
</style>
