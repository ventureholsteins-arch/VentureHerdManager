<script setup lang="ts">
type Animal = {
  animalId: number
  barnName: string | null
  registeredName: string | null
  registrationNumber: string | null
}

defineProps<{ animal: Animal }>()
defineEmits<{ select: [] }>()
</script>

<template>
  <button class="animal-card" @click="$emit('select')">
    <div class="animal-icon">
      {{ animal.barnName?.charAt(0) || '?' }}
    </div>

    <div class="animal-text">
      <h3>{{ animal.barnName || 'Unnamed Animal' }}</h3>
      <p>{{ animal.registeredName || 'No registered name yet' }}</p>
      <small>{{ animal.registrationNumber || 'Pending registration' }}</small>
    </div>

    <div class="chevron">›</div>
  </button>
</template>

<style scoped>
.animal-card {
  width: 100%;
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
}
</style>