<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

const props = withDefaults(defineProps<{
  message?: string
  scene?: 'random' | 'halter' | 'walk' | 'calf' | 'parlor'
  delayMs?: number
  branded?: boolean
}>(), {
  message: 'Loading herd records...',
  scene: 'random',
  delayMs: 350,
  branded: false
})

const scenes = ['walk', 'parlor', 'halter', 'calf'] as const
const visible = ref(false)
let timer: number | null = null

onMounted(() => {
  timer = window.setTimeout(() => {
    visible.value = true
  }, props.delayMs)
})

onBeforeUnmount(() => {
  if (timer !== null) window.clearTimeout(timer)
})

const selected = computed(() =>
  props.scene === 'random'
    ? scenes[Math.floor(Math.random() * scenes.length)] ?? 'walk'
    : props.scene
)

const label = computed(() => ({
  halter: "Negotiating with the one who won't lead",
  walk: 'Counting cows. One moved. Starting over.',
  calf: 'Calf is up. Coffee can wait.',
  parlor: "Waiting on the cow who knows she's last"
})[selected.value])

const scenePosition = computed(() => ({
  walk: '0% 50%',
  parlor: '33.333% 50%',
  halter: '66.667% 50%',
  calf: '100% 50%'
})[selected.value])
</script>

<template>
  <div v-if="visible" class="retro-loader" :class="{ branded }" role="status" aria-live="polite">
    <header v-if="branded" class="loader-brand">
      <img src="/app-logo.png" alt="Venture Herd Manager">
    </header>

    <div class="loader-main">
      <div
        class="scene"
        :style="{ backgroundPosition: scenePosition }"
        aria-hidden="true"
      />

      <div class="copy">
        <strong>{{ message }}</strong>
        <span>{{ label }}</span>
        <i class="dots"><b /><b /><b /></i>
      </div>
    </div>

    <footer v-if="branded" class="loader-footer">
      <p class="loader-tagline">Your operation shouldn't have to fit somebody else's software.</p>
      <div class="loader-maker">
        <span>Powered by</span>
        <img src="/venture-ag-marketing-logo.png" alt="Venture Ag Marketing">
        <small>Custom Ag Application Solutions</small>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.retro-loader {
  width: 100%;
  padding: 16px 18px;
  overflow: hidden;
  border: 3px solid #29241c;
  border-radius: 4px;
  background: linear-gradient(#e5dfca 0 63%, #b6bf7b 63% 72%, #65774d 72%);
  box-shadow: 5px 5px 0 #29241c;
  color: #29241c;
  image-rendering: pixelated;
}

.loader-main {
  display: grid;
  grid-template-columns: minmax(150px, 210px) 1fr;
  align-items: center;
  gap: 20px;
}

.loader-brand {
  display: flex;
  justify-content: center;
  padding: 4px 12px 14px;
  border-bottom: 1px solid rgba(41, 36, 28, 0.26);
}

.loader-brand img {
  display: block;
  width: min(330px, 78%);
  max-height: 76px;
  object-fit: contain;
}

.branded .loader-main { padding: 14px 0 10px; }

.loader-footer {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: end;
  gap: 20px;
  padding-top: 12px;
  border-top: 1px solid rgba(41, 36, 28, 0.26);
}

.loader-tagline {
  max-width: 430px;
  margin: 0;
  font-family: Georgia, serif;
  font-size: clamp(0.78rem, 1.8vw, 1rem);
  font-weight: 700;
  line-height: 1.25;
}

.loader-maker {
  display: grid;
  justify-items: end;
  color: #5b513d;
  font-family: Arial, sans-serif;
  font-size: 0.58rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.loader-maker img {
  width: 145px;
  max-height: 42px;
  object-fit: contain;
}

.loader-maker small { font-size: 0.52rem; }

.scene {
  height: 128px;
  background-image: url('/retro/herd-scenes.png');
  background-repeat: no-repeat;
  background-size: 400% auto;
  image-rendering: pixelated;
  filter: drop-shadow(3px 3px 0 rgba(41, 36, 28, 0.18));
  animation: scene-bob 1.4s steps(2, end) infinite;
}

.copy {
  display: grid;
  gap: 5px;
  min-width: 0;
  font-family: "Courier New", monospace;
  text-transform: uppercase;
}

.copy strong {
  font-size: clamp(0.92rem, 2vw, 1.12rem);
}

.copy > span {
  color: #5b513d;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.dots {
  display: flex;
  gap: 7px;
  margin-top: 7px;
}

.dots b {
  width: 8px;
  height: 8px;
  background: #29241c;
  animation: blink 1.05s steps(1, end) infinite;
}

.dots b:nth-child(2) { animation-delay: 0.2s; }
.dots b:nth-child(3) { animation-delay: 0.4s; }

@keyframes scene-bob {
  50% { transform: translateY(-2px); }
}

@keyframes blink {
  0%, 20% { opacity: 1; }
  21%, 100% { opacity: 0.18; }
}

@media (max-width: 560px) {
  .retro-loader {
    padding: 12px;
    box-shadow: 3px 3px 0 #29241c;
  }

  .loader-main {
    grid-template-columns: 116px 1fr;
    gap: 12px;
  }

  .loader-brand { padding-bottom: 9px; }
  .loader-brand img { max-height: 56px; }

  .loader-footer {
    grid-template-columns: 1fr;
    gap: 9px;
  }

  .loader-maker {
    width: 100%;
    grid-template-columns: auto 105px;
    align-items: center;
    justify-content: start;
    justify-items: start;
    gap: 0 7px;
  }

  .loader-maker img { width: 105px; }
  .loader-maker small { grid-column: 1 / -1; }

  .scene {
    height: 94px;
  }

  .copy > span {
    font-size: 0.64rem;
    letter-spacing: 0.04em;
  }
}

@media (prefers-reduced-motion: reduce) {
  .scene,
  .dots b {
    animation: none;
  }
}
</style>
