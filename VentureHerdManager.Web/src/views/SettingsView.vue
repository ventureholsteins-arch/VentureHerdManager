<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import { getAppearance, updateAppearance } from '../api/appearance'
import type { AppearanceSetting } from '../models/AppearanceSetting'

const router = useRouter()

const loading = ref(true)
const saving = ref(false)
const message = ref('')
const messageType = ref<'success' | 'error'>('success')
const settings = ref<AppearanceSetting | null>(null)

onMounted(async () => {
  try {
    settings.value = await getAppearance()
  } catch (error) {
    console.error('Failed to load appearance settings:', error)
    message.value = 'Could not load branding settings.'
    messageType.value = 'error'
  } finally {
    loading.value = false
  }
})

const previewStyle = computed(() => ({
  backgroundImage: `url('${settings.value?.backgroundImageUrl || '/herd-manager-bg.jpg'}')`,
  backgroundSize: 'cover',
  backgroundPosition: 'center',
  opacity: settings.value?.backgroundOpacity ?? 0.15
}))

async function saveSettings() {
  if (!settings.value) {
    return
  }

  saving.value = true
  message.value = ''

  try {
    settings.value = await updateAppearance(settings.value)
    message.value = 'Branding updated.'
    messageType.value = 'success'
  } catch (error) {
    console.error('Failed to save appearance settings:', error)
    message.value = 'Branding could not be saved.'
    messageType.value = 'error'
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push('/')
}
</script>

<template>
  <main class="settings-page">
    <header class="settings-hero">
      <button class="back-button" type="button" @click="goBack">
        ← Dashboard
      </button>

      <p class="eyebrow">BRANDING</p>
      <h1>Farm Look & Feel</h1>
      <p class="subtext">
        Drop in a logo, set the app background, and tune the feel so the dashboard looks premium.
      </p>
    </header>

    <section v-if="loading" class="card">
      <p>Loading branding settings...</p>
    </section>

    <section v-else-if="settings" class="settings-grid">
      <article class="card form-card">
        <div class="field-grid">
          <label>
            <span>Farm Name</span>
            <input v-model="settings.farmName" placeholder="Venture Holsteins">
          </label>

          <label>
            <span>Logo URL</span>
            <input v-model="settings.logoUrl" placeholder="https://.../logo.png">
          </label>

          <label>
            <span>Background Image URL</span>
            <input v-model="settings.backgroundImageUrl" placeholder="https://.../farm.jpg">
          </label>

          <label>
            <span>Background Opacity</span>
            <input v-model.number="settings.backgroundOpacity" type="number" min="0" max="1" step="0.05">
          </label>

          <label>
            <span>Overlay Opacity</span>
            <input v-model.number="settings.overlayOpacity" type="number" min="0" max="1" step="0.05">
          </label>

          <label>
            <span>Accent Color</span>
            <input v-model="settings.accentColor" placeholder="#31572c">
          </label>
        </div>

        <div class="actions">
          <button class="primary-button" type="button" :disabled="saving" @click="saveSettings">
            {{ saving ? 'Saving...' : 'Save Branding' }}
          </button>
        </div>

        <p v-if="message" class="status" :class="messageType">
          {{ message }}
        </p>
      </article>

      <article class="card preview-card">
        <div class="preview-frame">
          <div class="preview-background" :style="previewStyle" />

          <div class="preview-overlay">
            <img
              v-if="settings.logoUrl"
              :src="settings.logoUrl"
              alt="Farm logo"
              class="preview-logo"
            >
            <div v-else class="preview-logo placeholder-logo">
              VENTURE
            </div>

            <h2>{{ settings.farmName }}</h2>
            <p>Dashboard hero preview with branded farm styling.</p>
          </div>
        </div>
      </article>
    </section>
  </main>
</template>

<style scoped>
.settings-page {
  width: min(100%, 1120px);
  margin: 0 auto;
  padding: 28px 20px 60px;
}

.settings-hero {
  margin-bottom: 18px;
  padding: 28px;
  border-radius: 24px;
  background: linear-gradient(135deg, rgba(49, 87, 44, 0.12), rgba(255, 255, 255, 0.96));
  border: 1px solid #dce5de;
}

.back-button {
  margin-bottom: 16px;
  padding: 12px 16px;
  border: 1px solid #31572c;
  border-radius: 6px;
  background: transparent;
  color: #31572c;
  font-size: 1.1rem;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.2s ease;
}

.back-button:hover {
  background: rgba(49, 87, 44, 0.05);
  border-color: #254520;
  color: #254520;
}

.eyebrow {
  margin: 0;
  color: #31572c;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.12em;
}

.settings-hero h1 {
  margin: 8px 0 6px;
  color: #18351f;
  font-size: clamp(32px, 4vw, 44px);
}

.subtext {
  margin: 0;
  color: #64748b;
  max-width: 760px;
  line-height: 1.6;
}

.settings-grid {
  display: grid;
  grid-template-columns: 1.1fr 0.9fr;
  gap: 18px;
}

.card {
  border-radius: 24px;
  border: 1px solid #dce5de;
  background: rgba(255, 255, 255, 0.94);
  box-shadow: 0 10px 30px rgba(33, 65, 41, 0.08);
}

.form-card,
.preview-card {
  padding: 20px;
}

.field-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

label {
  display: grid;
  gap: 7px;
}

label span {
  color: #31572c;
  font-size: 13px;
  font-weight: 800;
}

input {
  width: 100%;
  min-height: 44px;
  padding: 10px 12px;
  border: 1px solid #cbd5c8;
  border-radius: 12px;
  background: white;
}

.actions {
  margin-top: 18px;
}

.primary-button {
  min-height: 44px;
  padding: 10px 18px;
  border-radius: 999px;
  background: #31572c;
  color: white;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
}

.status {
  margin-top: 14px;
  font-weight: 700;
}

.status.success {
  color: #14532d;
}

.status.error {
  color: #b91c1c;
}

.preview-frame {
  position: relative;
  min-height: 360px;
  border-radius: 20px;
  overflow: hidden;
  background: #e4ecd8;
}

.preview-background {
  position: absolute;
  inset: 0;
  background-size: cover;
  background-position: center;
}

.preview-overlay {
  position: absolute;
  inset: 0;
  display: grid;
  align-content: center;
  justify-items: start;
  gap: 8px;
  padding: 24px;
  background: linear-gradient(135deg, rgba(16, 33, 23, 0.72), rgba(16, 33, 23, 0.42));
  color: white;
}

.preview-logo {
  max-width: 220px;
  height: auto;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.85);
  padding: 8px;
}

.placeholder-logo {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 180px;
  min-height: 80px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.82);
  color: #31572c;
  font-weight: 900;
  letter-spacing: 0.08em;
}

.preview-overlay h2 {
  margin: 0;
  font-size: 30px;
}

.preview-overlay p {
  margin: 0;
  max-width: 360px;
  line-height: 1.5;
}

@media (max-width: 860px) {
  .settings-grid {
    grid-template-columns: 1fr;
  }

  .field-grid {
    grid-template-columns: 1fr;
  }
}
</style>
