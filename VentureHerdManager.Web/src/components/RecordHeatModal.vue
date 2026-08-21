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
              {{ animalDisplayName(animal) }}
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
          <small class="photo-help">Camera Roll / Existing Photo</small>
          <input
            type="file"
            accept=".jpg,.jpeg,.png,.heic,.heif,.webp,image/jpeg,image/png,image/heic,image/heif,image/webp"
            aria-label="Choose existing heat photo from camera roll"
            class="form-input file-input"
            @change="selectPhoto"
          />
          <small class="photo-help">Take New Photo</small>
          <input
            type="file"
            accept="image/*"
            capture="environment"
            aria-label="Take a new heat photo"
            class="form-input file-input"
            @change="selectPhoto"
          />
        </div>

        <div class="form-group">
          <label>Notes:</label>
          <textarea v-model="notes" class="form-input" rows="3"></textarea>
        </div>

        <div class="form-group">
          <label>
            <input type="checkbox" v-model="hasEmbryoTransfer" />
            Plan Embryo Transfer
          </label>
        </div>

        <div v-if="hasEmbryoTransfer" class="form-group">
          <label>Reserve an Embryo:</label>
          <select v-model="selectedEmbryoId" class="form-input">
            <option value="">Choose later</option>
            <option
              v-for="embryo in availableEmbryos"
              :key="embryo.embryoRecordId"
              :value="String(embryo.embryoRecordId)"
            >
              {{ embryo.code || `Embryo #${embryo.embryoRecordId}` }}
              <template v-if="embryo.sire"> · {{ embryo.sire }}</template>
            </option>
          </select>
          <small>You can reserve one now or select it when the transfer is recorded.</small>
        </div>

        <div class="form-group">
          <button :disabled="saving" @click="recordHeat" class="btn-primary">
            {{ saveButtonText }}
          </button>
          <button :disabled="saving" @click="goToBreedingTab" class="btn-secondary">Record Heat & Breed</button>
          <small v-if="saveError" class="save-error">{{ saveError }}</small>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { uploadPhoto } from '../api/photos'
import { getAllEmbryos, type EmbryoRecord } from '../api/embryoRecords'
import type { Animal } from '../models/Animal'
import { animalDisplayName, animalSearchText } from '../utils/animalDisplay'

const API_BASE = import.meta.env.VITE_API_URL

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
const saving = ref(false)
const saveError = ref('')
const notes = ref('')
const hasEmbryoTransfer = ref(false)
const selectedEmbryoId = ref('')
const availableEmbryos = ref<EmbryoRecord[]>([])
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
    .filter(a => animalSearchText(a).includes(q))
    .slice(0, 30)
})

function selectAnimal(animal: Animal) {
  selectedAnimalId.value = String(animal.animalId)
  animalSearch.value = animalDisplayName(animal)
}

const openModal = async (animalId?: number, animalName?: string) => {
  isOpen.value = true
  // Load animals list
  try {
    const [animalResponse, embryos] = await Promise.all([
      fetch(`${API_BASE}/Animals`),
      getAllEmbryos()
    ])
    if (animalResponse.ok) animals.value = await animalResponse.json()
    availableEmbryos.value = embryos.filter(embryo => embryo.status === 0)

    if (animalId) {
      selectedAnimalId.value = String(animalId)
      const selected = animals.value.find(animal => animal.animalId === animalId)
      animalSearch.value = selected
        ? animalDisplayName(selected)
        : (animalName || `Animal #${animalId}`)
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
  selectedEmbryoId.value = ''
  saving.value = false
  uploadingPhoto.value = false
  saveError.value = ''
}

function selectPhoto(event: Event) {
  photoFile.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

const saveButtonText = computed(() => {
  if (uploadingPhoto.value) return 'Uploading Photo…'
  if (saving.value) return 'Saving Heat…'
  return 'Record Heat'
})

const submitHeat = async (openBreedingAfterSave = false) => {
  if (saving.value) return

  if (!selectedAnimalId.value) {
    alert('Please select an animal')
    return
  }

  const savedAnimalId = parseInt(selectedAnimalId.value)
  saving.value = true
  saveError.value = ''
  try {
    if (photoFile.value) {
      uploadingPhoto.value = true
      pictureUrl.value = await uploadPhoto(photoFile.value, 'heat-events')
      uploadingPhoto.value = false
    }

    emit('recordHeat', {
      animalId: savedAnimalId,
      heatStrength: parseInt(heatStrength.value),
      standingHeat: standingHeat.value,
      pictureUrl: pictureUrl.value || null,
      notes: notes.value || null,
      hasEmbryoTransfer: hasEmbryoTransfer.value,
      embryoRecordId: selectedEmbryoId.value
        ? parseInt(selectedEmbryoId.value)
        : null,
      complete: (success: boolean, message?: string) => {
        saving.value = false
        if (success) {
          closeModal()
          if (openBreedingAfterSave) {
            emit('goToBreeding', savedAnimalId)
          }
        } else {
          saveError.value = message || 'Heat could not be saved. Please try again.'
        }
      }
    })
  } catch (error) {
    saving.value = false
    saveError.value = error instanceof Error
      ? error.message
      : 'Photo could not be uploaded.'
  } finally {
    uploadingPhoto.value = false
  }
}

const recordHeat = () => submitHeat(false)
const goToBreedingTab = () => submitHeat(true)

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

.btn-primary:disabled,
.btn-secondary:disabled {
  opacity: 0.65;
  cursor: wait;
}

.save-error {
  display: block;
  margin-top: 10px;
  color: #b91c1c;
  font-size: 0.85rem;
}
</style>
