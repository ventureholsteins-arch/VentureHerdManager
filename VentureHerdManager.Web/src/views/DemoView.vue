<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ensureDemo } from '../api/demo'
import HerdLoadingScene from '../components/HerdLoadingScene.vue'

const router = useRouter()
const loading = ref(false)
const error = ref<string | null>(null)
let demoReadyPromise: Promise<unknown> | null = null

onMounted(() => {
  // Create this browser's isolated sample herd while visitors read the page.
  demoReadyPromise = ensureDemo()
  void demoReadyPromise.catch(() => undefined)
})

async function launchDemo() {
  loading.value = true
  error.value = null

  try {
    let result
    if (demoReadyPromise) {
      try {
        result = await demoReadyPromise
      } catch {
        // The first request may have coincided with an Azure cold start.
        // Retry before opening so visitors never land on an empty dashboard.
        result = await ensureDemo()
      }
    } else {
      result = await ensureDemo()
    }

    if (!result || typeof result !== 'object' || !('animals' in result)
      || Number(result.animals) < 1) {
      throw new Error('The sample herd is still preparing. Please try again.')
    }

    sessionStorage.setItem('demo-launched', 'true')
    await router.push('/')
  } catch (err) {
    console.error('Demo setup failed:', err)
    error.value =
      err instanceof Error
        ? err.message
        : 'The demo could not be prepared. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="demo-launch">
    <div class="launch-card">
      <p class="demo-tag">DEMO</p>
      <h2 class="demo-promise">
        <span class="promise-setup">Your operation</span>
        <span class="promise-main">should not have to fit</span>
        <span class="promise-accent">somebody else's software.</span>
      </h2>
      <p class="subtitle">
        Try animal records, breeding, embryos, alerts, and reports in one
        shared place—built around the way a crew actually works.
      </p>

      <p v-if="error" class="error-msg">{{ error }}</p>

      <HerdLoadingScene
        v-if="loading"
        message="Preparing your sample herd..."
      />

      <div class="launch-buttons">
        <button
          class="launch-btn"
          type="button"
          :disabled="loading"
          @click="launchDemo"
        >
          {{ loading ? 'Opening demo...' : 'Explore the Demo' }}
        </button>
      </div>

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

.launch-btn:hover:not(:disabled) {
  background: #264822;
}

.launch-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.powered-by {
  margin: 26px 0 0;
  padding-top: 14px;
  border-top: 1px solid #e5ebe3;
  text-align: center;
  font-size: 0.75rem;
  color: #879488;
  letter-spacing: 0.01em;
}

.rights-reserved {
  margin: 4px 0 0;
  text-align: center;
  font-size: 0.7rem;
  color: #9aa8b3;
}

@media (max-width: 560px) {
  .demo-launch {
    padding: 14px;
  }

  .launch-card {
    padding: 28px 22px 24px;
  }

  .demo-promise {
    font-size: clamp(1.72rem, 8vw, 2.25rem);
    line-height: 1.04;
  }

  .promise-setup {
    margin-bottom: 6px;
  }

  .powered-by {
    margin-top: 22px;
    padding-top: 12px;
  }

}
</style>
