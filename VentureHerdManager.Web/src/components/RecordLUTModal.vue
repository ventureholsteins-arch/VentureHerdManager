<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Record LUT (Lutalyse) Injection</h2>
        <button class="close-btn" @click="closeModal">✕</button>
      </div>

      <div class="modal-body">
        <div class="form-group">
          <label>Search Animal:</label>
          <input
            v-model="animalSearch"
            type="search"
            class="form-input"
            placeholder="Type name to filter..."
            autofocus
          />
        </div>

        <div class="form-group" v-if="filteredAnimals.length > 0 || animalSearch">
          <label>Select Animal:</label>
          <div class="animal-list">
            <button
              v-for="animal in filteredAnimals"
              :key="animal.animalId"
              type="button"
              class="animal-list-item"
              :class="{ selected: selectedAnimalId === String(animal.animalId) }"
              @click="selectAnimal(animal)"
            >
              {{ animalLabel(animal) }}
              <span v-if="animal.animalStage" class="stage-tag">{{ stageLabel(animal.animalStage) }}</span>
            </button>
            <p v-if="filteredAnimals.length === 0" class="no-results">No animals match "{{ animalSearch }}"</p>
          </div>
        </div>

        <div v-if="selectedAnimalId" class="animal-info">
          Recording LUT for: <strong>{{ selectedAnimalLabel }}</strong>
          <div class="info-detail">Animal will be monitored for heat for 4 days. Day 3 will trigger an alert.</div>
        </div>

        <div class="form-group">
          <label>Administration Date:</label>
          <input v-model="administrationDate" type="date" class="form-input" />
        </div>

        <div class="info-box">
          <div><strong>Expected Heat Watch Window:</strong></div>
          <div>Starts: {{ expectedHeatStart }}</div>
          <div>Ends: {{ expectedHeatEnd }}</div>
        </div>

        <div class="form-group">
          <label>Notes:</label>
          <textarea v-model="notes" class="form-input" rows="3"></textarea>
        </div>

        <div class="form-group">
          <p v-if="saveError" class="save-error">{{ saveError }}</p>
          <button @click="recordLUT" class="btn-primary" :disabled="saving">{{ saving ? 'Saving…' : 'Record LUT Injection' }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

const API_BASE = import.meta.env.VITE_API_URL

interface AnimalOption {
  animalId: number
  barnName?: string | null
  registeredName?: string | null
  displayName?: string | null
  sireName?: string | null
  damName?: string | null
  animalStage?: number
}

const animalLabel = (animal: AnimalOption) =>
  animal.barnName || animal.registeredName || animal.displayName || `Animal #${animal.animalId}`

const isOpen = ref(false)
const animalId = ref<number | null>(null)
const selectedAnimalId = ref('')
const animalSearch = ref('')
const animalName = ref('')
const administrationDate = ref(new Date().toISOString().split('T')[0])
const notes = ref('')
const animals = ref<AnimalOption[]>([])
const saving = ref(false)
const saveError = ref('')

const filteredAnimals = computed(() => {
  const q = animalSearch.value.trim().toLowerCase()
  if (!q) return [...animals.value]
    .sort((a, b) => (a.animalStage === 3 ? -1 : 0) - (b.animalStage === 3 ? -1 : 0)
      || animalLabel(a).localeCompare(animalLabel(b)))
    .slice(0, 30)
  return animals.value
    .filter(a => [a.barnName, a.registeredName, a.displayName, a.sireName, a.damName]
      .some(value => (value || '').toLowerCase().includes(q)))
    .sort((a, b) => animalLabel(a).localeCompare(animalLabel(b)))
    .slice(0, 30)
})

const selectedAnimalLabel = computed(() => {
  if (!selectedAnimalId.value) return ''
  const animal = animals.value.find(a => String(a.animalId) === selectedAnimalId.value)
  return animal ? animalLabel(animal) : `Animal #${selectedAnimalId.value}`
})

const stageLabel = (stage: number): string => {
  const labels: Record<number, string> = {
    1: 'Calf', 2: 'Heifer', 3: 'Milking', 4: 'Dry', 5: 'Bull'
  }
  return labels[stage] ?? ''
}

function selectAnimal(animal: AnimalOption) {
  selectedAnimalId.value = String(animal.animalId)
  animalSearch.value = animalLabel(animal)
}

const expectedHeatStart = computed(() => {
  if (!administrationDate.value) return ''
  const date = new Date(administrationDate.value)
  date.setHours(date.getHours() + 36)
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString()
})

const expectedHeatEnd = computed(() => {
  if (!administrationDate.value) return ''
  const date = new Date(administrationDate.value)
  date.setHours(date.getHours() + 96)
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString()
})

const emit = defineEmits<{
  close: []
  recordLUT: [data: any]
}>()

const openModal = async (id?: number, name?: string) => {
  try {
    const response = await fetch(`${API_BASE}/Animals`)
    if (response.ok) {
      animals.value = await response.json()
    }
  } catch (err) {
    console.error('Failed to load animals for LUT modal:', err)
  }

  animalId.value = typeof id === 'number' && id > 0 ? id : null
  selectedAnimalId.value = animalId.value ? String(animalId.value) : ''
  animalSearch.value = animalId.value
    ? (animals.value.find(a => a.animalId === animalId.value)
        ? animalLabel(animals.value.find(a => a.animalId === animalId.value)!)
        : name ?? '')
    : ''
  animalName.value = name ?? ''
  administrationDate.value = new Date().toISOString().split('T')[0]
  notes.value = ''
  saving.value = false
  saveError.value = ''
  isOpen.value = true
}

const closeModal = () => {
  isOpen.value = false
  emit('close')
}

const recordLUT = () => {
  if (saving.value) return

  const resolvedAnimalId = selectedAnimalId.value
    ? parseInt(selectedAnimalId.value, 10)
    : animalId.value

  if (!resolvedAnimalId || !administrationDate.value) {
    alert('Please select an animal and fill in the date.')
    return
  }

  const adminDate = new Date(administrationDate.value)
  const heatStart = new Date(adminDate)
  heatStart.setHours(heatStart.getHours() + 36)
  const heatEnd = new Date(adminDate)
  heatEnd.setHours(heatEnd.getHours() + 96)

  saving.value = true
  saveError.value = ''
  emit('recordLUT', {
    animalId: resolvedAnimalId,
    administrationDate: administrationDate.value,
    expectedHeatWatchStart: heatStart.toISOString(),
    expectedHeatWatchEnd: heatEnd.toISOString(),
    notes: notes.value || null,
    complete: (success: boolean, message?: string) => {
      saving.value = false
      if (success) {
        closeModal()
      } else {
        saveError.value = message || 'The LUT injection could not be saved.'
      }
    }
  })
}

defineExpose({
  openModal,
  closeModal
})
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 8px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  max-width: 500px;
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #e0e0e0;
}

.modal-header h2 {
  margin: 0;
  font-size: 1.5rem;
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #666;
}

.modal-body {
  padding: 20px;
}

.animal-info {
  background: #f5f5f5;
  padding: 12px;
  border-radius: 4px;
  margin-bottom: 15px;
  font-size: 0.95rem;
}

.info-detail {
  font-size: 0.85rem;
  color: #666;
  margin-top: 5px;
}

.animal-list {
  max-height: 200px;
  overflow-y: auto;
  border: 1px solid #ddd;
  border-radius: 6px;
  display: grid;
  gap: 0;
}

.animal-list-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  padding: 10px 12px;
  border: none;
  border-bottom: 1px solid #eee;
  background: #fff;
  text-align: left;
  font-size: 1rem;
  font-weight: 600;
  color: #1f2937;
  cursor: pointer;
  transition: background 0.12s;
}

.animal-list-item:last-child {
  border-bottom: none;
}

.animal-list-item:hover {
  background: #f0f7f1;
}

.animal-list-item.selected {
  background: #dcfce7;
  color: #166534;
}

.stage-tag {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: #5d6f63;
  background: #f0f7f1;
  padding: 2px 7px;
  border-radius: 999px;
  flex-shrink: 0;
}

.animal-list-item.selected .stage-tag {
  background: #bbf7d0;
  color: #14532d;
}

.no-results {
  padding: 12px;
  color: #8a9b8e;
  font-size: 0.9rem;
  text-align: center;
}

.info-box {
  background: #e8f5e9;
  border-left: 4px solid #31572c;
  padding: 12px;
  margin-bottom: 15px;
  border-radius: 4px;
  font-size: 0.9rem;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
  color: #333;
}

.form-input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
  font-family: inherit;
}

.form-input:focus {
  outline: none;
  border-color: #31572c;
  box-shadow: 0 0 0 3px rgba(49, 87, 44, 0.1);
}

textarea.form-input {
  resize: vertical;
}

.btn-primary {
  width: 100%;
  padding: 12px;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  background: #31572c;
  color: white;
  margin-top: 10px;
}

.btn-primary:hover {
  background: #254520;
}
.save-error { color: #991b1b; background: #fff1f2; border: 1px solid #fecaca; border-radius: 6px; padding: 8px; }
</style>
