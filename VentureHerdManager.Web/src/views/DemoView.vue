<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { resetDemo } from '../api/demo'
import { getDashboardSummary } from '../api/dashboard'

const router = useRouter()
const loading = ref(false)
const error = ref<string | null>(null)
const demoResetEnabled = import.meta.env.VITE_DEMO_RESET_ENABLED === 'true'

onMounted(() => {
  // Wake the demo API and its database connection while visitors read this page.
  // The dashboard still performs a fresh request after launch.
  void getDashboardSummary().catch(() => undefined)
})

async function launchDemo(withReset: boolean) {
  loading.value = true
  error.value = null

  try {
    if (withReset && demoResetEnabled) {
      await resetDemo()
    }
    sessionStorage.setItem('demo-launched', 'true')
    await router.push('/')
  } catch (err) {
    // If reset API is unavailable, continue launching demo so UI still works.
    console.warn('Demo reset unavailable, launching without reset:', err)
    sessionStorage.setItem('demo-launched', 'true')
    await router.push('/')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="demo-launch">
    <div class="launch-card">
      <p class="demo-tag">DEMO</p>
      <h1>Venture Herd Manager</h1>
      <h2 class="demo-promise">
        <span class="promise-setup">Your operation</span>
        <span class="promise-main">should not have to fit</span>
        <span class="promise-accent">somebody else's software.</span>
      </h2>
      <p class="subtitle">
        Try animal records, breeding, embryos, alerts, and reports in one
        shared place—built around the way a crew actually works.
      </p>

      <div class="capability-row" aria-label="Demo features">
        <span>Animal records</span>
        <span>Breeding &amp; embryos</span>
        <span>Calendar &amp; alerts</span>
      </div>

      <p v-if="error" class="error-msg">{{ error }}</p>

      <div class="launch-buttons">
        <button
          class="launch-btn"
          type="button"
          :disabled="loading"
          @click="launchDemo(false)"
        >
          {{ loading ? 'Opening demo...' : 'Explore the Demo' }}
        </button>

        <button
          v-if="demoResetEnabled"
          class="launch-btn secondary"
          type="button"
          :disabled="loading"
          @click="launchDemo(true)"
        >
          {{ loading ? 'Preparing demo...' : 'Start with Fresh Demo Data' }}
        </button>
      </div>

      <p v-if="demoResetEnabled" class="hint">
        Choose “Explore the Demo” to continue where you left off, or choose
        “Start with Fresh Demo Data” to restore the original sample records.
      </p>

      <p class="powered-by">Custom application solutions by Venture Ag Marketing</p>
      <p class="rights-reserved">All rights reserved.</p>
    </div>
  </main>
</template>

<style scoped>
.demo-launch {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  position: relative;
  background:
    linear-gradient(rgba(10, 28, 17, 0.74), rgba(10, 28, 17, 0.88)),
    url('/candid.jpg') center / cover fixed;
}

.launch-card {
  position: relative;
  background: rgba(255, 255, 255, 0.97);
  border: 1px solid rgba(255, 255, 255, 0.55);
  border-top: 5px solid #31572c;
  border-radius: 12px;
  padding: 40px 48px;
  max-width: 520px;
  width: 100%;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.06);
}

.demo-tag {
  margin: 0 0 10px;
  color: #31572c;
  font-weight: 800;
  letter-spacing: 0.14em;
  font-size: 0.72rem;
}

h1 {
  margin: 0 0 16px;
  font-size: 1.75rem;
  color: #1a2e1c;
}

.demo-promise {
  margin: 0 0 18px;
  color: #17281a;
  font-family: Georgia, 'Times New Roman', serif;
  font-size: clamp(2rem, 6vw, 3rem);
  line-height: 1.02;
  letter-spacing: -0.035em;
  font-weight: 700;
}

.demo-promise span {
  display: block;
}

.promise-setup {
  margin-bottom: 7px;
  color: #6f7d70;
  font-family: system-ui, -apple-system, 'Segoe UI', sans-serif;
  font-size: 0.38em;
  font-style: normal;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.promise-main {
  color: #17281a;
  font-weight: 700;
}

.promise-accent {
  margin-top: 5px;
  color: #31572c;
  font-style: italic;
  font-weight: 500;
}

.subtitle {
  margin: 0 0 20px;
  color: #425466;
  line-height: 1.6;
}

.capability-row {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  margin: 0 0 24px;
}

.capability-row span {
  padding: 7px 10px;
  border: 1px solid #d9e5d5;
  border-radius: 999px;
  background: #f3f7f1;
  color: #31572c;
  font-size: 0.72rem;
  font-weight: 700;
}

.launch-buttons {
  display: grid;
  gap: 10px;
}

.error-msg {
  margin: 0 0 16px;
  padding: 10px 14px;
  background: #fff3f3;
  border: 1px solid #f5c6c6;
  border-radius: 6px;
  color: #b91c1c;
  font-size: 0.9rem;
}

.launch-btn {
  width: 100%;
  padding: 14px;
  background: #31572c;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s;
}

.launch-btn.secondary {
  background: #fff;
  color: #31572c;
  border: 1px solid #31572c;
}

.launch-btn.secondary:hover:not(:disabled) {
  background: #f3f7f1;
}

.launch-btn:hover:not(:disabled) {
  background: #264822;
}

.launch-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.powered-by {
  margin: 20px 0 0;
  text-align: center;
  font-size: 0.75rem;
  color: #8a9ba8;
  letter-spacing: 0.01em;
}

.rights-reserved {
  margin: 4px 0 0;
  text-align: center;
  font-size: 0.7rem;
  color: #9aa8b3;
}

.hint {
  margin: 8px 0 0;
  color: #5f6c7b;
  font-size: 0.85rem;
}

@media (max-width: 560px) {
  .demo-launch {
    padding: 14px;
  }

  .launch-card {
    padding: 30px 22px;
  }

  .demo-promise {
    font-size: clamp(1.85rem, 8.5vw, 2.4rem);
  }

}
</style>
