<template>
  <Teleport to="body">
    <div v-if="isOpen" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h2>Edit Animal: {{ animal ? animalDisplayName(animal) : 'Animal' }}</h2>
          <button type="button" class="close-btn" @click="closeModal">✕</button>
        </div>

        <div class="modal-body">
          <form @submit.prevent="handleSubmit">
            <div class="form-group">
              <label for="barnName">Barn Name</label>
              <input 
                id="barnName"
                v-model="formData.barnName" 
                type="text" 
                placeholder="e.g., Bessie"
                maxlength="100"
              />
            </div>

            <div class="form-group">
              <label for="registeredName">Registered Name</label>
              <input 
                id="registeredName"
                v-model="formData.registeredName" 
                type="text" 
                placeholder="e.g., RDHFS Bessie 35"
                maxlength="200"
              />
            </div>

            <div class="form-group">
              <label for="breed">Breed</label>
              <input 
                id="breed"
                v-model="formData.breed" 
                type="text" 
                placeholder="e.g., Holstein"
                maxlength="100"
              />
            </div>

            <div class="form-group">
              <label for="sireName">Sire Name</label>
              <input 
                id="sireName"
                v-model="formData.sireName" 
                type="text" 
                placeholder="e.g., Seashore"
                maxlength="200"
              />
            </div>

            <div class="form-group">
              <label for="currentLactation">Current Lactation</label>
              <input 
                id="currentLactation"
                v-model.number="formData.currentLactation" 
                type="number" 
                min="0"
                placeholder="e.g., 2"
              />
            </div>

            <div style="border-top: 1px solid #ddd; margin: 20px 0; padding-top: 20px;">
              <h3 style="margin-top: 0;">Classification Score</h3>
              <div class="form-group">
                <label for="scoreValue">Score</label>
                <input 
                  id="scoreValue"
                  v-model.number="formData.scoreValue" 
                  type="number" 
                  min="0"
                  max="100"
                  step="0.1"
                  placeholder="e.g., 92.5"
                />
                <small style="display: block; margin-top: 4px; color: #666;">
                  EX: 90+, VG: 85-89, GP: &lt;85
                </small>
              </div>

              <div class="form-group">
                <label for="baa">BAA</label>
                <input 
                  id="baa"
                  v-model.number="formData.baa" 
                  type="number" 
                  step="0.1"
                  placeholder="Optional - BAA score"
                />
              </div>

              <div class="form-group">
                <label for="classificationNotes">Classification Notes</label>
                <textarea 
                  id="classificationNotes"
                  v-model="formData.classificationNotes" 
                  placeholder="Notes about this classification..."
                  rows="3"
                  maxlength="2000"
                ></textarea>
              </div>
            </div>

            <div class="form-group">
              <label for="notes">Notes</label>
              <textarea 
                id="notes"
                v-model="formData.notes" 
                placeholder="Add any additional notes..."
                rows="4"
                maxlength="4000"
              ></textarea>
            </div>

            <div class="form-group checkbox">
              <input 
                id="isFavorite"
                v-model="formData.isFavorite" 
                type="checkbox"
              />
              <label for="isFavorite">Mark as Favorite</label>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-cancel" @click="closeModal">
                Cancel
              </button>
              <button type="submit" class="btn btn-primary" :disabled="isSaving">
                {{ isSaving ? 'Saving...' : 'Save Changes' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { Animal } from '../models/Animal'
import { updateAnimal } from '../api/animals'
import { addClassification } from '../api/classification'
import { animalDisplayName } from '../utils/animalDisplay'

const props = defineProps<{
  animal: Animal | null
}>()

const emit = defineEmits<{
  close: []
  saved: [animal: Animal]
}>()

const isOpen = ref(false)
const isSaving = ref(false)
const initialScore = ref<number | null>(null)
const initialBaa = ref<number | null>(null)
const formData = ref({
  barnName: '',
  registeredName: '',
  breed: '',
  sireName: '',
  currentLactation: null as number | null,
  notes: '',
  isFavorite: false,
  scoreValue: null as number | null,
  baa: null as number | null,
  classificationNotes: ''
})

const populateFormData = (sourceAnimal: Animal) => {
  initialScore.value = sourceAnimal.latestScore ?? null
  initialBaa.value = sourceAnimal.latestBaa ?? null
  formData.value = {
    barnName: sourceAnimal.barnName || '',
    registeredName: sourceAnimal.registeredName || '',
    breed: sourceAnimal.breed || '',
    sireName: sourceAnimal.sireName || '',
    currentLactation: sourceAnimal.currentLactation || null,
    notes: sourceAnimal.notes || '',
    isFavorite: sourceAnimal.isFavorite || false,
    scoreValue: sourceAnimal.latestScore || null,
    baa: sourceAnimal.latestBaa || null,
    classificationNotes: ''
  }
}

const openModal = () => {
  if (!props.animal) {
    return
  }

  populateFormData(props.animal)
  isOpen.value = true
}

const closeModal = () => {
  isOpen.value = false
}

const handleSubmit = async () => {
  if (!props.animal) {
    return
  }

  isSaving.value = true

  try {
    const updatedAnimal = await updateAnimal(props.animal.animalId, {
      barnName: formData.value.barnName || null,
      registeredName: formData.value.registeredName || null,
      registrationNumber: props.animal.registrationNumber,
      birthDate: props.animal.birthDate,
      sex: props.animal.sex,
      animalStage: props.animal.animalStage,
      animalStatus: props.animal.animalStatus ?? 0,
      breed: formData.value.breed || null,
      sireId: props.animal.sireId,
      sireName: formData.value.sireName || null,
      damId: props.animal.damId,
      damName: props.animal.damName,
      currentLactation: formData.value.currentLactation,
      notes: formData.value.notes || null,
      profilePictureUrl: props.animal.profilePictureUrl,
      isFavorite: formData.value.isFavorite
    })

    const classificationChanged =
      formData.value.scoreValue !== initialScore.value
      || formData.value.baa !== initialBaa.value
      || formData.value.classificationNotes.trim().length > 0

    if (
      classificationChanged
      && formData.value.scoreValue !== null
      && formData.value.scoreValue !== undefined
    ) {
      await addClassification({
        animalId: props.animal.animalId,
        score: formData.value.scoreValue,
        baa: formData.value.baa ?? undefined,
        classificationDate: new Date().toISOString(),
        notes: formData.value.classificationNotes || undefined
      })
    }

    emit('saved', updatedAnimal)
    closeModal()
    alert(
      'Animal updated successfully!'
      + (classificationChanged ? ' Classification saved!' : '')
    )
  } catch (error) {
    alert(`Error updating animal: ${error instanceof Error ? error.message : 'Unknown error'}`)
    console.error('Failed to update animal:', error)
  } finally {
    isSaving.value = false
  }
}

defineExpose({
  openModal,
  closeModal
})
</script>

<style scoped>
.modal-overlay {
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
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  max-width: 500px;
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px;
  border-bottom: 1px solid #eee;
}

.modal-header h2 {
  margin: 0;
  font-size: 1.25rem;
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #666;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.close-btn:hover {
  background-color: #f0f0f0;
}

.modal-body {
  padding: 20px;
}

.form-group {
  margin-bottom: 16px;
}

.form-group label {
  display: block;
  margin-bottom: 6px;
  font-weight: 500;
  color: #333;
  font-size: 0.95rem;
}

.form-group input[type="text"],
.form-group input[type="number"],
.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.95rem;
  font-family: inherit;
  transition: border-color 0.2s;
}

.form-group input[type="text"]:focus,
.form-group input[type="number"]:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #4CAF50;
  box-shadow: 0 0 0 3px rgba(76, 175, 80, 0.1);
}

.form-group textarea {
  resize: vertical;
}

.form-group.checkbox {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 20px;
}

.form-group.checkbox input[type="checkbox"] {
  width: 18px;
  height: 18px;
  cursor: pointer;
}

.form-group.checkbox label {
  margin: 0;
  cursor: pointer;
  font-weight: normal;
}

.modal-footer {
  padding: 20px;
  border-top: 1px solid #eee;
  display: flex;
  gap: 12px;
  justify-content: flex-end;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  font-size: 0.95rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s, opacity 0.2s;
}

.btn-cancel {
  background-color: #f0f0f0;
  color: #333;
}

.btn-cancel:hover {
  background-color: #e0e0e0;
}

.btn-primary {
  background-color: #4CAF50;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: #45a049;
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
