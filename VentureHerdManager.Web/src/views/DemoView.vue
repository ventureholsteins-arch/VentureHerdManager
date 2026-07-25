<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { resetDemo } from '../api/demo'

const router = useRouter()
const loading = ref(false)
const error = ref<string | null>(null)
const demoResetEnabled = import.meta.env.VITE_DEMO_RESET_ENABLED === 'true'

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
        Know your herd.
        <span>Move with confidence.</span>
      </h2>
      <p class="subtitle">
        Easy View keeps the whole crew on the same page. Record work with one
        hand, keep every event in order, and see what needs to happen next.
      </p>

      <div class="photo-strip" aria-label="Venture Holsteins">
        <img src="/Palace_heifer.jpg" alt="Venture Holsteins heifer in the show ring">
        <img src="/Seashell_cow.jpg" alt="Venture Holsteins cow in the show ring">
      </div>

      <ul class="feature-list">
        <li>One shared animal record for everyone</li>
        <li>Automatic alerts for work coming due</li>
        <li>Heats, breeding, embryos, calvings, and notes in order</li>
        <li>Built to view and update from a phone in the barn</li>
      </ul>

      <div class="custom-software">
        <strong>Built for your operation</strong>
        <p>
          This is custom software. Features, reports, workflows, and branding
          can be tailored to fit your herd and the way you work.
        </p>
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

.photo-strip {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  margin: 0 0 22px;
}

.photo-strip img {
  width: 100%;
  height: 112px;
  display: block;
  object-fit: cover;
  border-radius: 7px;
}

.photo-strip img:first-child {
  object-position: center 42%;
}

.photo-strip img:last-child {
  object-position: center 48%;
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
  font-size: clamp(2rem, 7vw, 3.25rem);
  line-height: 0.95;
  letter-spacing: -0.055em;
  text-transform: uppercase;
  font-weight: 900;
}

.demo-promise span {
  display: block;
  margin-top: 8px;
  color: transparent;
  -webkit-text-stroke: 1.25px #31572c;
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

.custom-software {
  margin: 0 0 24px;
  padding: 16px;
  background: #f3f7f1;
  border: 1px solid #d9e5d5;
  border-radius: 8px;
  color: #31572c;
}

.custom-software p {
  margin: 6px 0 0;
  color: #4a5e4c;
  font-size: 0.92rem;
  line-height: 1.5;
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
    font-size: clamp(2rem, 12vw, 2.75rem);
  }

  .photo-strip img {
    height: 90px;
  }
}
</style>
