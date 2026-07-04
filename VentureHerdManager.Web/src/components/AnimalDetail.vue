<script setup lang="ts">
import { ref } from 'vue'

type Animal = {
  animalId: number
  barnName: string | null
  registeredName: string | null
  registrationNumber: string | null
}

const props = defineProps<{ animal: Animal }>()
defineEmits<{ close: [] }>()

const showHeatForm = ref(false)
const heatNotes = ref('')
const heatSaving = ref(false)

async function recordHeat() {
  heatSaving.value = true

  try {
    const response = await fetch(
      `http://localhost:5051/api/animals/${props.animal.animalId}/heats`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          heatDateTime: new Date().toISOString(),
          heatNotes: heatNotes.value,
          pictureUrl: null,
          createdBy: 'Austin',
          wasBred: false,
          sireUsed: null,
          breedingType: null,
          breedingNotes: null
        })
      }
    )

    if (!response.ok) throw new Error('Failed to record heat.')

    alert('Heat recorded successfully!')
    heatNotes.value = ''
    showHeatForm.value = false
  } catch {
    alert('Unable to save heat.')
  } finally {
    heatSaving.value = false
  }
}
</script>

<template>
  <section class="selected">
    <div class="selected-header">
      <div>
        <small>Selected animal</small>
        <h2>{{ animal.barnName || 'Unnamed Animal' }}</h2>
        <p>{{ animal.registeredName || 'No registered name yet' }}</p>
        <span>{{ animal.registrationNumber || 'Pending registration' }}</span>
      </div>

      <button class="close" @click="$emit('close')">×</button>
    </div>

    <div class="selected-actions">
      <button @click="showHeatForm = true">❤️ Heat</button>
      <button>🧬 Breed</button>
      <button>📝 Note</button>
      <button>🐄 Calved</button>
    </div>

    <div v-if="showHeatForm" class="heat-form">
      <h3>Record Heat</h3>
      <textarea v-model="heatNotes" placeholder="Standing heat, mounting, mucus, etc..."></textarea>
      <button @click="recordHeat" :disabled="heatSaving">
        {{ heatSaving ? 'Saving...' : 'Save Heat' }}
      </button>
    </div>
  </section>
</template>

<style scoped>
.selected {
  margin-bottom: 18px;
  padding: 18px;
  border-radius: 24px;
  background: #142033;
  color: white;
}

.selected-header {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.selected small {
  color: #cbd5e1;
  text-transform: uppercase;
  font-weight: 800;
  font-size: 11px;
}

.selected h2 {
  margin: 6px 0 4px;
  font-size: 28px;
}

.selected p {
  margin: 0 0 6px;
  color: #e2e8f0;
}

.selected span {
  color: #cbd5e1;
}

.close {
  width: 38px;
  height: 38px;
  border-radius: 14px;
  background: rgba(255,255,255,0.12);
  color: white;
  font-size: 25px;
  border: none;
}

.selected-actions {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 11px;
  margin-top: 16px;
}

.selected-actions button {
  min-height: 58px;
  border-radius: 18px;
  background: white;
  color: #142033;
  font-size: 16px;
  font-weight: 800;
  border: none;
}

.heat-form {
  margin-top: 18px;
  background: white;
  color: #142033;
  border-radius: 18px;
  padding: 16px;
}

.heat-form h3 {
  margin-top: 0;
}

.heat-form textarea {
  width: 100%;
  min-height: 100px;
  padding: 12px;
  border-radius: 12px;
  border: 1px solid #cbd5e1;
  font-size: 16px;
  resize: vertical;
}

.heat-form button {
  width: 100%;
  margin-top: 12px;
  min-height: 50px;
  border-radius: 14px;
  background: #31572c;
  color: white;
  font-size: 16px;
  font-weight: 800;
  border: none;
}
</style>