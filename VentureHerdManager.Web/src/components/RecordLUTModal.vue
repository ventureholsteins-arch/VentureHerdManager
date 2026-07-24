<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Record LUT (Lutalyse) Injection</h2>
        <button class="close-btn" @click="closeModal">✕</button>
      </div>

      <div class="modal-body">
        <div v-if="animalName" class="animal-info">
          Recording LUT for: <strong>{{ animalName }}</strong>
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
          <button @click="recordLUT" class="btn-primary">Record LUT Injection</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

const isOpen = ref(false)
const animalId = ref<number | null>(null)
const animalName = ref('')
const administrationDate = ref(new Date().toISOString().split('T')[0])
const notes = ref('')

const expectedHeatStart = computed(() => {
  if (!administrationDate.value) return ''
  const date = new Date(administrationDate.value)
  date.setHours(date.getHours() + 36)
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString()
})

const expectedHeatEnd = computed(() => {
  if (!administrationDate.value) return ''
  const date = new Date(administrationDate.value)
  date.setHours(date.getHours() + 96) // 4 days
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString()
})

const emit = defineEmits<{
  close: []
  recordLUT: [data: any]
}>()

const openModal = (id: number, name: string) => {
  animalId.value = id
  animalName.value = name
  administrationDate.value = new Date().toISOString().split('T')[0]
  notes.value = ''
  isOpen.value = true
}

const closeModal = () => {
  isOpen.value = false
  emit('close')
}

const recordLUT = () => {
  if (!animalId.value || !administrationDate.value) {
    alert('Please fill in required fields')
    return
  }

  const adminDate = new Date(administrationDate.value)
  const heatStart = new Date(adminDate)
  heatStart.setHours(heatStart.getHours() + 36)
  const heatEnd = new Date(adminDate)
  heatEnd.setHours(heatEnd.getHours() + 96)

  emit('recordLUT', {
    animalId: animalId.value,
    administrationDate: administrationDate.value,
    expectedHeatWatchStart: heatStart.toISOString(),
    expectedHeatWatchEnd: heatEnd.toISOString(),
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

.info-detail {
  font-size: 0.85rem;
  color: #666;
  margin-top: 5px;
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
</style>
