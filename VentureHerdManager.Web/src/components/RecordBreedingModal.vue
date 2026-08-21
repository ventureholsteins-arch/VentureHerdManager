<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Record Breeding</h2>
        <button class="close-btn" @click="closeModal">✕</button>
      </div>

      <div class="modal-body">
        <div v-if="animalName" class="animal-info">
          Recording breeding for: <strong>{{ animalName }}</strong>
        </div>

        <div class="form-group">
          <label>Breeding Date:</label>
          <input v-model="breedingDate" type="date" class="form-input" />
        </div>

        <div class="form-group">
          <label>Sire:</label>
          <select v-model="sireUsed" class="form-input">
            <option value="">-- Select Sire --</option>
            <option v-for="sire in recentSires" :key="sire" :value="sire">
              {{ sire }} (recent)
            </option>
            <option value="---">--- Enter Custom ---</option>
          </select>
        </div>

        <div v-if="sireUsed === '---'" class="form-group">
          <input v-model="customSire" type="text" class="form-input" placeholder="Enter sire name" />
        </div>

        <div class="form-group">
          <label>Breeding Type:</label>
          <select v-model="breedingType" class="form-input">
            <option value="0">AI (Artificial Insemination)</option>
            <option value="1">Natural</option>
            <option value="2">Embryo Transfer</option>
          </select>
        </div>

        <div v-if="breedingType === '2'" class="form-group">
          <label>Embryo:</label>
          <select v-model="selectedEmbryoId" class="form-input">
            <option value="">-- Select Inventory Embryo --</option>
            <option
              v-for="embryo in availableEmbryos"
              :key="embryo.embryoRecordId"
              :value="String(embryo.embryoRecordId)"
            >
              {{ embryo.mating || `${embryo.donor || 'Unknown dam'} × ${embryo.sire || 'Unknown sire'}` }}
              · {{ embryo.groupName || 'Ungrouped' }}
              <template v-if="embryo.grade"> · {{ embryo.grade }}</template>
              <template v-if="embryo.code"> · {{ embryo.code }}</template>
            </option>
          </select>
          <small>The transfer will link this embryo to {{ animalName }} and remove it from available inventory.</small>
        </div>

        <div class="form-group">
          <label>Notes:</label>
          <textarea v-model="notes" class="form-input" rows="3"></textarea>
        </div>

        <div class="form-group">
          <label>Pregnancy Status:</label>
          <select v-model="pregnancyStatus" class="form-input">
            <option value="0">Unconfirmed</option>
            <option value="1">Pregnant</option>
            <option value="2">Open / Not Pregnant</option>
            <option value="3">Recheck</option>
            <option value="4">Aborted</option>
          </select>
        </div>

        <div class="form-group">
          <p v-if="saveError" class="save-error">{{ saveError }}</p>
          <button @click="recordBreeding" class="btn-primary" :disabled="saving">{{ saving ? 'Saving…' : 'Record Breeding' }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { getAllEmbryos, type EmbryoRecord } from '../api/embryoRecords'

const isOpen = ref(false)
const animalId = ref<number | null>(null)
const animalName = ref('')
const breedingDate = ref(new Date().toISOString().split('T')[0])
const sireUsed = ref('')
const customSire = ref('')
const breedingType = ref('1')
const selectedEmbryoId = ref('')
const availableEmbryos = ref<EmbryoRecord[]>([])
const notes = ref('')
const pregnancyStatus = ref('0')
const recentSires = ref<string[]>(['Seashore', 'Robust', 'Elevation'])
const saving = ref(false)
const saveError = ref('')

const emit = defineEmits<{
  close: []
  recordBreeding: [data: any]
}>()

const openModal = async (id: number, name: string) => {
  animalId.value = id
  animalName.value = name
  breedingDate.value = new Date().toISOString().split('T')[0]
  sireUsed.value = ''
  customSire.value = ''
  breedingType.value = '0'
  selectedEmbryoId.value = ''
  notes.value = ''
  pregnancyStatus.value = '0'
  saving.value = false
  saveError.value = ''
  isOpen.value = true
  try {
    availableEmbryos.value = (await getAllEmbryos())
      .filter(embryo =>
        embryo.status === 0
        || (embryo.status === 1 && embryo.recipientAnimalId === id))
  } catch (error) {
    console.error('Failed to load embryo inventory:', error)
    availableEmbryos.value = []
  }
}

const closeModal = () => {
  isOpen.value = false
  emit('close')
}

const recordBreeding = async () => {
  if (saving.value) return

  if (!animalId.value || !breedingDate.value) {
    alert('Please fill in all required fields')
    return
  }

  const finalSire = sireUsed.value === '---' ? customSire.value : sireUsed.value
  if (breedingType.value === '2' && !selectedEmbryoId.value) {
    alert('Please select the embryo being transferred')
    return
  }

  saving.value = true
  saveError.value = ''
  emit('recordBreeding', {
    animalId: animalId.value,
    breedingDate: breedingDate.value,
    sireUsed: finalSire || 'Unknown',
    breedingType: parseInt(breedingType.value),
    notes: notes.value || null,
    pregnancyStatus: parseInt(pregnancyStatus.value),
    embryoRecordId: selectedEmbryoId.value
      ? parseInt(selectedEmbryoId.value)
      : null,
    complete: (success: boolean, message?: string) => {
      saving.value = false
      if (success) {
        closeModal()
      } else {
        saveError.value = message || 'The breeding could not be saved.'
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
