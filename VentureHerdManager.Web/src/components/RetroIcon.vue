<script setup lang="ts">
import { computed } from 'vue'

type IconName =
  | 'heat'
  | 'pregCheck'
  | 'calving'
  | 'lut'
  | 'embryo'
  | 'dryOff'
  | 'calf'
  | 'note'
  | 'photo'
  | 'calendar'
  | 'reports'

const props = withDefaults(
  defineProps<{ name: IconName; size?: number }>(),
  { size: 44 }
)

const spritePosition = computed(() => {
  const positions: Record<IconName, string> = {
    heat: '0% 0%',
    pregCheck: '25% 0%',
    calving: '50% 0%',
    lut: '75% 0%',
    embryo: '100% 0%',
    dryOff: '0% 100%',
    calf: '25% 100%',
    note: '50% 100%',
    photo: '75% 100%',
    calendar: '100% 100%',
    reports: '50% 100%'
  }

  return positions[props.name]
})
</script>

<template>
  <span
    class="retro-icon"
    :style="{
      width: `${size}px`,
      height: `${size}px`,
      backgroundPosition: spritePosition
    }"
    aria-hidden="true"
  />
</template>

<style scoped>
.retro-icon {
  display: inline-block;
  flex: 0 0 auto;
  vertical-align: middle;
  background-image: url('/retro/herd-icons.png');
  background-repeat: no-repeat;
  background-size: 500% 200%;
  image-rendering: pixelated;
  filter: drop-shadow(2px 2px 0 rgba(41, 36, 28, 0.16));
}
</style>
