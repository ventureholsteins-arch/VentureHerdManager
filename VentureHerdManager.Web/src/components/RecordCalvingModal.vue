<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Record Calving</h2>
        <button class="close-btn" @click="closeModal">✕</button>
      </div>

      <div class="modal-body">
        <div v-if="animalName" class="animal-info">
          Calving for: <strong>{{ animalName }}</strong>
        </div>

        <div class="form-group">
          <label>Calving Date:</label>
          <input v-model="calvingDate" type="date" class="form-input" />
        </div>

        <div class="form-group">
          <label>Picture URL:</label>
          <input v-model="pictureUrl" type="text" class="form-input" placeholder="https://..." />
        </div>

        <div class="form-group">
          <label>Calf Barn Name:</label>
          <input v-model="calfBarnName" type="text" class="form-input" placeholder="e.g., Daisy" />
        </div>

        <div class="form-group">
          <label>Calf Registered Name:</label>
          <input v-model="calfRegisteredName" type="text" class="form-input" placeholder="Full registered name" />
        </div>

        <div class="form-group">
          <label>Calf Sex:</label>
          <select v-model="calfSex" class="form-input">
            <option value="0">Unknown</option>
            <option value="1">Female</option>
            <option value="2">Male</option>
          </select>
        </div>

        <div class="form-group">
          <label>Birth Weight (lbs):</label>
          <input v-model="birthWeight" type="number" step="0.1" class="form-input" />
        </div>

        <div class="form-group">
          <label>Calving Ease:</label>
          <select v-model="calvingEase" class="form-input">
            <option value="0">Unassisted</option>
            <option value="1">Slight Assistance</option>
            <option value="2">Moderate Assistance</option>
            <option value="3">Difficult/Surgical</option>
          </select>
        </div>

        <div class="form-group">
          <label>
            <input type="checkbox" v-model="twins" />
            Twins
          </label>
        </div>

        <div class="form-group">
          <label>
            <input type="checkbox" v-model="stillborn" />
            Stillborn
          </label>
        </div>

        <div class="form-group">
          <label>Notes:</label>
          <textarea v-model="notes" class="form-input" rows="3"></textarea>
        </div>

        <div class="form-group">
          <button @click="recordCalving" class="btn-primary">
            Record Calving (Move to Milking)
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const isOpen = ref(false)
const animalId = ref<number | null>(null)
const animalName = ref('')
const calvingDate = ref(new Date().toISOString().split('T')[0])
const pictureUrl = ref('')
const calfBarnName = ref('')
const calfRegisteredName = ref('')
const calfSex = ref('1')
const birthWeight = ref('')
const calvingEase = ref('0')
const twins = ref(false)
const stillborn = ref(false)
const notes = ref('')

const emit = defineEmits<{
  close: []
  recordCalving: [data: any]
}>()

const openModal = (id: number, name: string) => {
  animalId.value = id
  animalName.value = name
  calvingDate.value = new Date().toISOString().split('T')[0]
  pictureUrl.value = ''
  calfBarnName.value = ''
  calfRegisteredName.value = ''
  calfSex.value = '1'
  birthWeight.value = ''
  calvingEase.value = '0'
  twins.value = false
  stillborn.value = false
  notes.value = ''
  isOpen.value = true
}

const closeModal = () => {
  isOpen.value = false
  emit('close')
}

const recordCalving = async () => {
  if (!animalId.value || !calvingDate.value) {
    alert('Please fill in required fields')
    return
  }

  emit('recordCalving', {
    animalId: animalId.value,
    calvingDate: calvingDate.value,
    pictureUrl: pictureUrl.value || null,
    calfBarnName: calfBarnName.value || null,
    calfRegisteredName: calfRegisteredName.value || null,
    calfSex: parseInt(calfSex.value),
    birthWeight: birthWeight.value ? parseFloat(birthWeight.value) : null,
    calvingEase: parseInt(calvingEase.value),
    twins: twins.value,
    stillborn: stillborn.value,
    notes: notes.value || null
  })

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
</style>
