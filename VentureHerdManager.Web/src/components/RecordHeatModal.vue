<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Record Heat Event</h2>
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
              {{ animal.barnName }}
              <span class="stage-tag">{{ stageLabel(animal.animalStage) }}</span>
            </button>
            <p v-if="filteredAnimals.length === 0" class="no-results">No animals match &quot;{{ animalSearch }}&quot;</p>
          </div>
        </div>

        <div class="form-group">
          <label>Heat Strength:</label>
          <select v-model="heatStrength" class="form-input">
            <option value="0">Unknown</option>
            <option value="1">Weak</option>
            <option value="2">Normal</option>
            <option value="3">Strong</option>
          </select>
        </div>

        <div class="form-group">
          <label>
            <input type="checkbox" v-model="standingHeat" />
            Standing Heat
          </label>
        </div>

        <div class="form-group">
          <label>Heat Photo:</label>
          <input
            type="file"
            accept="image/*"
            class="form-input file-input"
            @change="selectPhoto"
          />
          <small class="photo-help">Choose from Photos, Files, or Camera on iPhone.</small>
        </div>

        <div class="form-group">
          <label>Notes:</label>
          <textarea v-model="notes" class="form-input" rows="3"></textarea>
        </div>

        <div class="form-group">
          <label>
            <input type="checkbox" v-model="hasEmbryoTransfer" />
            Embryo Transfer
          </label>
        </div>

        <div class="form-group">
          <button :disabled="uploadingPhoto" @click="recordHeat" class="btn-primary">
            {{ uploadingPhoto ? 'Uploading Photo…' : 'Record Heat' }}
          </button>
          <button @click="goToBreedingTab" class="btn-secondary">Record Heat & Breed</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { uploadPhoto } from '../api/photos'

const API_BASE = import.meta.env.VITE_API_URL

interface Animal {
  animalId: number
  barnName: string
  animalStage: number
}

const stageLabel = (stage: number): string => {
  const labels: Record<number, string> = {
    1: 'Calf',
    2: 'Heifer',
    3: 'Milking',
    4: 'Dry',
    5: 'Bull'
  }

  return labels[stage] ?? 'Unknown'
}

const isOpen = ref(false)
const selectedAnimalId = ref('')
const animalSearch = ref('')
const heatStrength = ref('2')
const standingHeat = ref(false)
const pictureUrl = ref('')
const photoFile = ref<File | null>(null)
const uploadingPhoto = ref(false)
const notes = ref('')
const hasEmbryoTransfer = ref(false)
const animals = ref<Animal[]>([])

const emit = defineEmits<{
  close: []
  recordHeat: [data: any]
  goToBreeding: [animalId: number]
}>()

const filteredAnimals = computed(() => {
  const q = animalSearch.value.trim().toLowerCase()
  if (!q) return animals.value.slice(0, 30)
  return animals.value
    .filter(a => (a.barnName || '').toLowerCase().includes(q))
    .slice(0, 30)
})

function selectAnimal(animal: Animal) {
  selectedAnimalId.value = String(animal.animalId)
  animalSearch.value = animal.barnName
}

const openModal = async () => {
  isOpen.value = true
  // Load animals list
  try {
    const response = await fetch(`${API_BASE}/Animals`)
    if (response.ok) {
      animals.value = await response.json()
    }
  } catch (err) {
    console.error('Failed to load animals:', err)
  }
}

const closeModal = () => {
  isOpen.value = false
  resetForm()
  emit('close')
}

const resetForm = () => {
  selectedAnimalId.value = ''
  animalSearch.value = ''
  heatStrength.value = '2'
  standingHeat.value = false
  pictureUrl.value = ''
  photoFile.value = null
  notes.value = ''
  hasEmbryoTransfer.value = false
}

function selectPhoto(event: Event) {
  photoFile.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

const recordHeat = async () => {
  if (!selectedAnimalId.value) {
    alert('Please select an animal')
    return
  }

  uploadingPhoto.value = true
  try {
    if (photoFile.value) {
      pictureUrl.value = await uploadPhoto(photoFile.value, 'heat-events')
    }

    emit('recordHeat', {
    animalId: parseInt(selectedAnimalId.value),
    heatStrength: parseInt(heatStrength.value),
    standingHeat: standingHeat.value,
    pictureUrl: pictureUrl.value || null,
    notes: notes.value || null,
    hasEmbryoTransfer: hasEmbryoTransfer.value
    })

    closeModal()
  } finally {
    uploadingPhoto.value = false
  }
}

const goToBreedingTab = () => {
  if (!selectedAnimalId.value) {
    alert('Please select an animal')
    return
  }

  emit('goToBreeding', parseInt(selectedAnimalId.value))
  closeModal()
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

.file-input {
  background: #fff;
}

.photo-help {
  display: block;
  margin-top: 6px;
  color: #64748b;
}

textarea.form-input {
  resize: vertical;
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

.animal-list-item:last-child { border-bottom: none; }
.animal-list-item:hover { background: #f0f7f1; }
.animal-list-item.selected { background: #dcfce7; color: #166534; }

.stage-tag {
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
  color: #5d6f63;
  background: #f0f7f1;
  padding: 2px 7px;
  border-radius: 999px;
  flex-shrink: 0;
}

.animal-list-item.selected .stage-tag { background: #bbf7d0; color: #14532d; }

.no-results {
  padding: 12px;
  color: #8a9b8e;
  font-size: 0.9rem;
  text-align: center;
}

.btn-primary,
.btn-secondary {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  margin-right: 10px;
  margin-top: 10px;
}

.btn-primary {
  background: #31572c;
  color: white;
}

.btn-primary:hover {
  background: #254520;
}

.btn-secondary {
  background: #e0e0e0;
  color: #333;
}

.btn-secondary:hover {
  background: #d0d0d0;
}
</style>
