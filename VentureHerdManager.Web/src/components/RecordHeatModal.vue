<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Record Heat Event</h2>
        <button class="close-btn" @click="closeModal">✕</button>
      </div>

      <div class="modal-body">
        <div class="form-group">
          <label>Animal:</label>
          <select v-model="selectedAnimalId" class="form-input">
            <option value="">-- Select Animal --</option>
            <option v-for="animal in animals" :key="animal.animalId" :value="animal.animalId">
              {{ animal.barnName }} ({{ animal.stage }})
            </option>
          </select>
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
          <label>Picture URL:</label>
          <input v-model="pictureUrl" type="text" class="form-input" placeholder="https://..." />
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
          <button @click="recordHeat" class="btn-primary">Record Heat</button>
          <button @click="goToBreedingTab" class="btn-secondary">Record Heat & Breed</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'

interface Animal {
  animalId: number
  barnName: string
  stage: string
}

const isOpen = ref(false)
const selectedAnimalId = ref('')
const heatStrength = ref('2')
const standingHeat = ref(false)
const pictureUrl = ref('')
const notes = ref('')
const hasEmbryoTransfer = ref(false)
const animals = ref<Animal[]>([])

const emit = defineEmits<{
  close: []
  recordHeat: [data: any]
  goToBreeding: [animalId: number]
}>()

const openModal = async () => {
  isOpen.value = true
  // Load animals list
  try {
    const response = await fetch('/api/Animals')
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
  heatStrength.value = '2'
  standingHeat.value = false
  pictureUrl.value = ''
  notes.value = ''
  hasEmbryoTransfer.value = false
}

const recordHeat = async () => {
  if (!selectedAnimalId.value) {
    alert('Please select an animal')
    return
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

textarea.form-input {
  resize: vertical;
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
