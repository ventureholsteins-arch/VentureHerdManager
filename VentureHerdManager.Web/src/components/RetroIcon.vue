<script setup lang="ts">
type IconName =
  | 'heat' | 'pregCheck' | 'calving' | 'lut' | 'embryo'
  | 'dryOff' | 'calf' | 'note' | 'photo' | 'calendar' | 'reports'

withDefaults(defineProps<{ name: IconName; size?: number }>(), { size: 44 })
</script>

<template>
  <span class="retro-icon" :style="{ width: `${size}px`, height: `${size}px` }" aria-hidden="true">
    <svg viewBox="0 0 16 16" role="presentation">
      <g v-if="name === 'heat'" class="red">
        <path d="M8 1h2v3h2v3h2v5h-2v2H5v-2H3V8h2V5h2V2h1v5H7v4h3V8H9V5H8z"/>
      </g>
      <g v-else-if="name === 'pregCheck'">
        <path class="pink" d="M4 2h7v2h2v8h-2v2H4v-2H2V4h2zm4 4H6v2H5v2h2V9h2v1h2V7H9V5H8z"/>
        <path class="dark" d="M5 4h2v2H5zm4 0h2v2H9z"/>
      </g>
      <g v-else-if="name === 'calving' || name === 'calf'">
        <path class="white" d="M2 4h3V2h2v2h5v2h2v5h-2v3h-2v-3H6v3H4v-3H2z"/>
        <path class="dark" d="M3 5h2v2H3zm5 1h3v3H8zm4-1h2v2h-2z"/>
        <path class="pink" d="M1 7h4v3H1z"/>
      </g>
      <g v-else-if="name === 'lut'">
        <path class="blue" d="M2 3h8v2H2zm2 3h8v2H4zm6 3h4v2h-4z"/>
        <path class="dark" d="M1 2h2v4H1zm8 2h2v4H9zm4 4h2v4h-2z"/>
      </g>
      <g v-else-if="name === 'embryo'">
        <path class="cream" d="M5 1h6v2h2v3h2v5h-2v2h-2v2H5v-2H3v-2H1V6h2V3h2z"/>
        <path class="gold" d="M5 4h6v2h2v4h-2v2H5v-2H3V6h2z"/>
        <path class="white" d="M6 5h4v2h2v2h-2v2H6V9H4V7h2z"/>
      </g>
      <g v-else-if="name === 'dryOff'">
        <path class="blue" d="M7 1h2v3h2v2h2v5h-2v3H5v-3H3V6h2V4h2z"/>
        <path class="white" d="M6 7h2v4H6z"/>
      </g>
      <g v-else-if="name === 'note' || name === 'reports'">
        <path class="cream" d="M3 1h8l2 2v12H3z"/>
        <path class="dark" d="M10 1v3h3v1h-3V2H5v11h6V6h2v9H3V1zm-4 6h4v1H6zm0 3h4v1H6z"/>
      </g>
      <g v-else-if="name === 'photo'">
        <path class="green" d="M1 4h4l1-2h4l1 2h4v10H1z"/>
        <path class="white" d="M6 6h4v2h2v3h-2v2H6v-2H4V8h2z"/>
        <path class="dark" d="M7 7h2v2H7z"/>
      </g>
      <g v-else>
        <path class="cream" d="M2 2h12v13H2z"/>
        <path class="red" d="M2 2h12v4H2z"/>
        <path class="dark" d="M4 1h2v3H4zm6 0h2v3h-2zM4 8h2v2H4zm3 0h2v2H7zm3 0h2v2h-2zM4 11h2v2H4zm3 0h2v2H7z"/>
      </g>
    </svg>
  </span>
</template>

<style scoped>
.retro-icon { display:inline-block; flex:0 0 auto; image-rendering:pixelated; filter:drop-shadow(2px 2px 0 rgba(41,36,28,.2)); }
svg { width:100%; height:100%; shape-rendering:crispEdges; }
.dark { fill:#29241c; } .white { fill:#fffaf0; } .pink { fill:#e6a4a8; }
.red { fill:#c84d3f; } .blue { fill:#6b93ad; } .green { fill:#607a4e; }
.cream { fill:#f0e2bd; } .gold { fill:#d5a848; }
</style>
