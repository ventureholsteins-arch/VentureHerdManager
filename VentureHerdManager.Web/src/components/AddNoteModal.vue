<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Add Note</h2>
        <button class="close-btn" @click="closeModal">✕</button>
      </div>

      <div class="modal-body">
        <div v-if="animalName" class="animal-info">
          Adding note for: <strong>{{ animalName }}</strong>
        </div>

        <div class="form-group">
          <label>Note Type:</label>
          <select v-model="noteType" class="form-input">
            <option value="0">General</option>
            <option value="1">Health</option>
            <option value="2">Behavior</option>
            <option value="3">Nutrition</option>
            <option value="4">Management</option>
          </select>
        </div>

        <div class="form-group">
          <label>Note:</label>
          <textarea 
            v-model="noteText" 
            class="form-input" 
            rows="6"
            placeholder="Enter your note here..."
          ></textarea>
        </div>

        <div class="form-group">
          <button @click="addNote" class="btn-primary">Add Note</button>
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
const noteType = ref('0')
const noteText = ref('')

const emit = defineEmits<{
  close: []
  addNote: [data: any]
}>()

const openModal = (id: number, name: string) => {
  animalId.value = id
  animalName.value = name
  noteType.value = '0'
  noteText.value = ''
  isOpen.value = true
}

const closeModal = () => {
  isOpen.value = false
  emit('close')
}

const addNote = () => {
  if (!animalId.value || !noteText.value.trim()) {
    alert('Please enter a note')
    return
  }

  emit('addNote', {
    animalId: animalId.value,
    noteType: parseInt(noteType.value),
    noteText: noteText.value
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
