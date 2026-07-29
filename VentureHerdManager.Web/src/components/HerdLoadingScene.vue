<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

const props = withDefaults(defineProps<{
  message?: string
  scene?: 'random' | 'halter' | 'walk' | 'calf' | 'parlor'
  delayMs?: number
}>(), { message: 'Loading herd records...', scene: 'random', delayMs: 350 })

const scenes = ['walk', 'parlor', 'halter', 'calf'] as const
const visible = ref(false)
let timer: number | null = null
onMounted(() => { timer = window.setTimeout(() => { visible.value = true }, props.delayMs) })
onBeforeUnmount(() => { if (timer !== null) window.clearTimeout(timer) })
const selected = computed(() => props.scene === 'random'
  ? scenes[Math.floor(Math.random() * scenes.length)] ?? 'walk'
  : props.scene)
const label = computed(() => ({
  halter: 'Getting the show string in line',
  walk: 'Checking who left the gate open',
  calf: 'Welcoming the newest employee',
  parlor: 'Sending milk to the cloud'
})[selected.value])
</script>

<template>
  <div v-if="visible" class="retro-loader" role="status" aria-live="polite">
    <div class="scene" :class="`scene-${selected}`" aria-hidden="true">
      <span v-if="selected === 'halter'" class="person">▥</span>
      <span class="cow"><i /><b /><em /><small /></span>
      <span v-if="selected === 'calf'" class="baby"><i /><b /></span>
      <span v-if="selected === 'parlor'" class="pail">▣</span>
      <span v-if="selected === 'walk'" class="grass">♒</span>
    </div>
    <div class="copy">
      <strong>{{ message }}</strong>
      <span>{{ label }}</span>
      <i class="dots"><b /><b /><b /></i>
    </div>
  </div>
</template>

<style scoped>
.retro-loader{display:grid;grid-template-columns:minmax(132px,190px) 1fr;align-items:center;gap:22px;width:100%;padding:20px;overflow:hidden;border:3px solid #29241c;border-radius:4px;background:linear-gradient(#d9d4bf 0 58%,#a2ad68 58% 70%,#566b43 70%);box-shadow:5px 5px 0 #29241c;color:#29241c;image-rendering:pixelated}
.scene{position:relative;height:112px;overflow:hidden}.cow{position:absolute;left:28px;bottom:24px;width:84px;height:45px;background:#fffaf0;box-shadow:0 0 0 4px #29241c;animation:cow-step .75s steps(2,end) infinite}
.cow:before{content:"";position:absolute;left:-24px;top:-7px;width:30px;height:32px;background:#fffaf0;box-shadow:0 0 0 4px #29241c}.cow:after{content:"";position:absolute;left:-17px;top:3px;width:5px;height:5px;background:#29241c;box-shadow:16px 0 #29241c,8px 13px #e6a4a8,13px 13px #e6a4a8}
.cow i,.cow b{position:absolute;bottom:-24px;width:7px;height:25px;background:#29241c}.cow i{left:13px}.cow b{right:12px}.cow em{position:absolute;right:14px;top:10px;width:22px;height:20px;background:#29241c}.cow small{position:absolute;right:-15px;top:1px;width:17px;height:4px;background:#29241c}
.baby{position:absolute;right:2px;bottom:22px;width:38px;height:23px;background:#fffaf0;box-shadow:0 0 0 3px #29241c}.baby:before{content:"";position:absolute;left:-13px;top:-5px;width:15px;height:16px;background:#fffaf0;box-shadow:0 0 0 3px #29241c}.baby i,.baby b{position:absolute;bottom:-12px;width:4px;height:13px;background:#29241c}.baby i{left:7px}.baby b{right:7px}
.person{position:absolute;left:0;bottom:28px;font-size:54px;color:#8d5c3b}.pail{position:absolute;right:3px;bottom:8px;font-size:34px;color:#6b93ad}.grass{position:absolute;right:2px;bottom:4px;font-size:38px;color:#31572c}.copy{display:grid;gap:5px;min-width:0;font-family:"Courier New",monospace;text-transform:uppercase}.copy strong{font-size:clamp(.92rem,2vw,1.12rem)}.copy>span{color:#5b513d;font-size:.72rem;font-weight:700;letter-spacing:.08em}.dots{display:flex;gap:7px;margin-top:7px}.dots b{width:8px;height:8px;background:#29241c;animation:blink 1.05s steps(1,end) infinite}.dots b:nth-child(2){animation-delay:.2s}.dots b:nth-child(3){animation-delay:.4s}
@keyframes cow-step{50%{transform:translateY(-4px)}}@keyframes blink{0%,20%{opacity:1}21%,100%{opacity:.18}}
@media(max-width:560px){.retro-loader{grid-template-columns:110px 1fr;gap:14px;padding:14px;box-shadow:3px 3px 0 #29241c}.scene{height:90px;transform:scale(.82);transform-origin:left center}.copy>span{font-size:.64rem}}
@media(prefers-reduced-motion:reduce){.cow,.dots b{animation:none}}
</style>
