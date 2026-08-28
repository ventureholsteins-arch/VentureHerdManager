<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { getAppearance, type AppearanceSetting } from './api/appearance'
import { resetDemo } from './api/demo'
import { clearAdminKey, getAdminKey, setAdminKey, validateAdminKey } from './api/herdData'
import BrandedHerdLoader from './components/BrandedHerdLoader.vue'

const appearance = ref<AppearanceSetting | null>(null)
const isDemoOnly = import.meta.env.VITE_DEMO_ONLY === 'true'
const demoResetEnabled = import.meta.env.VITE_DEMO_RESET_ENABLED === 'true'
const demoResetting = ref(false)
const appUnlocked = ref(isDemoOnly || Boolean(getAdminKey()))
const unlockKey = ref('')
const unlockBusy = ref(false)
const unlockError = ref('')
const startupVisible = ref(!isDemoOnly && Boolean(getAdminKey()))
let startupTimer: number | null = null
let startupShownAt = 0

function beginStartup() {
  startupVisible.value = true
  startupShownAt = Date.now()
  if (startupTimer !== null) window.clearTimeout(startupTimer)
  startupTimer = null
}

function finishStartup() {
  const remaining = Math.max(0, 2200 - (Date.now() - startupShownAt))
  if (startupTimer !== null) window.clearTimeout(startupTimer)
  startupTimer = window.setTimeout(() => {
    startupVisible.value = false
    startupTimer = null
  }, remaining)
}

function showStartup() {
  beginStartup()
  finishStartup()
}

async function refreshAppearance() {
  appearance.value = await getAppearance()
}

onMounted(async () => {
  if (appUnlocked.value && !isDemoOnly) showStartup()
  try {
    await refreshAppearance()
  } catch (error) {
    console.error('Failed to load app appearance:', error)
  }

  window.addEventListener('appearance-updated', refreshAppearance)
  window.addEventListener('herd-startup-begin', beginStartup)
  window.addEventListener('herd-startup-finish', finishStartup)
})

onBeforeUnmount(() => {
  if (startupTimer !== null) window.clearTimeout(startupTimer)
  window.removeEventListener('appearance-updated', refreshAppearance)
  window.removeEventListener('herd-startup-begin', beginStartup)
  window.removeEventListener('herd-startup-finish', finishStartup)
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

async function unlockApp() {
  unlockBusy.value = true
  unlockError.value = ''
  setAdminKey(unlockKey.value.trim())
  try {
    await validateAdminKey()
    appUnlocked.value = true
    showStartup()
    unlockKey.value = ''
  } catch {
    clearAdminKey()
    unlockError.value = 'That key did not work. Check it and try again.'
  } finally {
    unlockBusy.value = false
  }
}
</script>

<template>
  <div class="app-shell" :style="appStyle">
    <div class="app-background" />

    <div v-if="appUnlocked && isDemoOnly" class="demo-banner">
      <span>{{ demoResetEnabled ? 'DEMO MODE · PRIVATE 24-HOUR SAMPLE HERD' : 'DEMO MODE' }}</span>
      <div class="demo-actions">
        <button type="button" class="secondary" :disabled="demoResetting" @click="openDemoFast">
          Enter Demo
        </button>
        <button type="button" :disabled="demoResetting || !demoResetEnabled" @click="handleDemoReset">
          {{ demoResetting ? 'Refreshing...' : 'Refresh Demo Data' }}
        </button>
      </div>
    </div>

    <main v-if="!appUnlocked" class="unlock-screen">
      <form class="unlock-card" @submit.prevent="unlockApp">
        <span class="unlock-mark">VH</span>
        <h1>Venture Herd Manager</h1>
        <p>Enter your private access key. This device will remain unlocked for 24 hours.</p>
        <p class="temporary-key">Temporary access key: <strong>ventureripp</strong></p>
        <label for="app-key">Access key</label>
        <input id="app-key" v-model="unlockKey" type="password" autocomplete="current-password" autofocus>
        <button type="submit" :disabled="unlockBusy || !unlockKey.trim()">{{ unlockBusy ? 'Opening…' : 'Open herd manager' }}</button>
        <p v-if="unlockError" class="unlock-error">{{ unlockError }}</p>
      </form>
    </main>

    <div v-else class="app-content">
      <RouterView />
    </div>

    <BrandedHerdLoader v-if="startupVisible && appUnlocked" />

    <footer v-if="appUnlocked" class="app-footer">
      <span class="footer-brand">
        <span>Powered by</span>
        <i class="footer-venture-logo" aria-label="Venture Ag Marketing" />
      </span>
      <span class="footer-sep">·</span>
      <span>Custom Application Solutions</span>
      <span class="footer-sep">·</span>
      <RouterLink class="footer-customize" to="/settings">Customize App</RouterLink>
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

.unlock-screen { position:relative;z-index:3;min-height:100vh;display:grid;place-items:center;padding:20px;box-sizing:border-box; }
.unlock-card { width:min(420px,100%);display:grid;gap:12px;padding:28px;border:1px solid rgba(49,87,44,.22);border-radius:16px;background:rgba(255,255,255,.96);box-shadow:0 18px 55px rgba(13,26,16,.18); }
.unlock-mark { width:52px;height:52px;display:grid;place-items:center;border-radius:12px;background:#31572c;color:#fff;font-weight:950;font-size:1.15rem; }
.unlock-card h1,.unlock-card p { margin:0; }
.unlock-card .temporary-key { padding:10px 12px;border:1px solid #d7dfd2;border-radius:8px;background:#f3f7f1;color:#31572c; }
.unlock-card label { font-weight:850; }
.unlock-card input,.unlock-card button { min-height:48px;border-radius:8px;font:inherit;box-sizing:border-box; }
.unlock-card input { width:100%;padding:10px 12px;border:1px solid #aebdaf; }
.unlock-card button { border:0;background:#31572c;color:#fff;font-weight:900;cursor:pointer; }
.unlock-card button:disabled { opacity:.58;cursor:wait; }
.unlock-card .unlock-error { color:#991b1b;font-weight:750; }

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
  flex-wrap: wrap;
}

.app-footer strong {
  color: rgba(255,255,255,0.72);
  font-weight: 900;
}

.footer-brand {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.footer-venture-logo {
  display: inline-block;
  width: 130px;
  height: 38px;
  background-image: url('/venture-ag-marketing-logo.png');
  background-repeat: no-repeat;
  background-position: center;
  background-size: contain;
  opacity: 0.82;
}

.footer-sep {
  color: rgba(255,255,255,0.2);
}

.footer-customize {
  color: rgba(255,255,255,0.72);
  font-weight: 800;
  text-decoration: underline;
  text-decoration-color: rgba(211,170,103,0.72);
  text-underline-offset: 3px;
}

.footer-customize:hover { color: #fff; }

@media (max-width: 640px) {
  .app-footer {
    gap: 4px 10px;
    padding-bottom: calc(14px + env(safe-area-inset-bottom));
    line-height: 1.4;
  }

  .footer-sep {
    display: none;
  }

  .app-footer > span:not(.footer-sep) {
    flex: 1 1 100%;
  }

  .footer-customize {
    flex: 1 1 100%;
  }

  .footer-brand {
    justify-content: center;
  }
}
</style>
