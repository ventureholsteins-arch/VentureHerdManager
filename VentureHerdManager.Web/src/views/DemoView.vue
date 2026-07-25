<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { resetDemo } from '../api/demo'

const router = useRouter()
const loading = ref(false)
const error = ref<string | null>(null)
const demoResetEnabled = import.meta.env.VITE_DEMO_RESET_ENABLED === 'true'

async function launchDemo() {
  loading.value = true
  error.value = null

  try {
    if (demoResetEnabled) {
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
      <p class="subtitle">
        See the real app in action with a pre-loaded herd of demo animals.
        Demo data is reset fresh each time.
      </p>

      <ul class="feature-list">
        <li>Dashboard with upcoming due dates &amp; LUT tracking</li>
        <li>Animal profiles, notes, photos &amp; classification records</li>
        <li>Heat, breeding, dry-off, and calving event history</li>
        <li>Calendar view of all herd events</li>
      </ul>

      <p v-if="error" class="error-msg">{{ error }}</p>

      <button
        class="launch-btn"
        type="button"
        :disabled="loading"
        @click="launchDemo"
      >
        {{ loading ? 'Loading demo data...' : 'Launch Demo' }}
      </button>

      <p class="powered-by">Powered by Venture Ag Marketing Custom Application Solutions</p>
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
}

.launch-card {
  background: #fff;
  border: 1px solid #d5dde4;
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
  margin: 0 0 12px;
  font-size: 1.75rem;
  color: #1a2e1c;
}

.subtitle {
  margin: 0 0 20px;
  color: #425466;
  line-height: 1.6;
}

.feature-list {
  margin: 0 0 28px;
  padding-left: 20px;
  color: #4a5e4c;
  line-height: 1.8;
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
</style>
