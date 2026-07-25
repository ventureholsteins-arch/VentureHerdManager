<script setup lang="ts">
import { formatCurrentAge, getShowClassLabel } from '../utils/showClasses'

type Animal = {
  animalId: number
  barnName: string | null
  registeredName: string | null
  registrationNumber: string | null
  birthDate?: string | null
  animalStage?: number
  sireName?: string | null
  damName?: string | null
  latestScore?: number | null
}

const { animal } = defineProps<{ animal: Animal }>()
defineEmits<{ select: [] }>()

const scoreLabel = (score: number | null | undefined): string => {
  if (!score) return 'Not scored'
  if (score >= 90) return `EX ${Math.round(score)}`
  if (score >= 85) return `VG ${Math.round(score)}`
  return `GP ${Math.round(score)}`
}
</script>

<template>
  <button class="animal-card" type="button" @click="$emit('select')">
    <div class="animal-card-photo" aria-hidden="true" />

    <div class="animal-icon">
      {{ animal.barnName?.charAt(0) || '?' }}
    </div>

    <div class="animal-text">
      <h3>{{ animal.barnName || 'Unnamed Animal' }}</h3>
      <p>{{ animal.registeredName || 'No registered name yet' }}</p>
      <small>{{ animal.registrationNumber || 'Pending registration' }}</small>

      <div class="meta-grid">
        <span><strong>Sire:</strong> {{ animal.sireName || '—' }}</span>
        <span><strong>Dam:</strong> {{ animal.damName || '—' }}</span>
        <span><strong>Age:</strong> {{ formatCurrentAge(animal.birthDate) }}</span>
        <span class="meta-wide"><strong>Show Class:</strong> {{ getShowClassLabel(animal.birthDate, animal.animalStage) }}</span>
        <span><strong>Score:</strong> {{ scoreLabel(animal.latestScore) }}</span>
      </div>
    </div>

    <div class="chevron">›</div>
  </button>
</template>

<style scoped>
.animal-card {
  width: 100%;
  position: relative;
  isolation: isolate;
  display: grid;
  grid-template-columns: 52px 1fr 18px;
  gap: 14px;
  align-items: center;
  padding: 15px;
  margin-bottom: 12px;
  border-radius: 22px;
  background: white;
  color: #142033;
  text-align: left;
  border: 1px solid #e2e8f0;
  overflow: hidden;
}

.animal-card-photo {
  position: absolute;
  inset: 0;
  z-index: -1;
  background:
    linear-gradient(135deg, rgba(255, 255, 255, 0.94), rgba(246, 251, 247, 0.82)),
    url('/Palace_heifer.jpg');
  background-size: cover;
  background-position: center;
  opacity: 0.26;
}

.animal-icon {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(20, 32, 51, 0.1);
  font-weight: 700;
}

.animal-text h3 {
  margin: 0;
}

.animal-text p {
  margin: 4px 0;
}

.animal-text small {
  color: #3f4c5f;
}

.meta-grid {
  margin-top: 8px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4px 10px;
  font-size: 0.8rem;
}

.meta-wide {
  grid-column: span 2;
}

.chevron {
  font-size: 1.2rem;
  color: #425167;
}
</style>