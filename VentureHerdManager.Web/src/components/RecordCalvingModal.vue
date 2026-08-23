<template>
  <div v-if="isOpen" class="modal-backdrop" @click="closeModal">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h2>Record Calving</h2>
        <button class="close-btn" @click="closeModal">✕</button>
      </div>

      <div class="modal-body">
        <div v-if="saving" class="saving-banner" role="status" aria-live="polite">
          <span class="saving-spinner" aria-hidden="true"></span>
          <strong>{{ saveStatus }}</strong>
        </div>
        <div v-if="animalName" class="animal-info">
          Calving for: <strong>{{ animalName }}</strong>
        </div>

        <div class="form-group">
          <label>Calving Date:</label>
          <input v-model="calvingDate" type="date" class="form-input" />
        </div>

        <div class="form-group">
          <label>Upload Calf Photo:</label>
          <small class="upload-hint">Camera Roll / Existing Photo</small>
          <input
            type="file"
            accept=".jpg,.jpeg,.png,.heic,.heif,.webp,image/jpeg,image/png,image/heic,image/heif,image/webp"
            aria-label="Choose existing calving photo from camera roll"
            class="form-input"
            @change="onCalfPhotoSelected"
          >
          <small class="upload-hint">Take New Photo</small>
          <input
            type="file"
            accept="image/*"
            capture="environment"
            aria-label="Take a new calving photo"
            class="form-input"
            @change="onCalfPhotoSelected"
          >
          <small v-if="calfPhotoFile" class="upload-hint">
            Selected: {{ calfPhotoFile.name }}
          </small>
          <small v-if="isUploadingPhoto" class="upload-hint">
            Uploading photo...
          </small>
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
          <label>Calf Sire Name:</label>
          <input v-model="calfSireName" type="text" class="form-input" placeholder="e.g., Master" />
        </div>

        <div class="form-group">
          <label>Calf Dam Name:</label>
          <input v-model="calfDamName" type="text" class="form-input" placeholder="Defaults to selected cow" />
        </div>

        <div class="form-group">
          <label>Calf Sex:</label>
          <select v-model="calfSex" class="form-input">
            <option value="0">Unknown</option>
            <option value="1">Male (Bull)</option>
            <option value="2">Female (Heifer)</option>
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
          <button :disabled="saving" @click="recordCalving" class="btn-primary">
            {{ saveButtonText }}
          </button>
          <small v-if="saveError" class="save-error">{{ saveError }}</small>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

const isOpen = ref(false)
const animalId = ref<number | null>(null)
const animalName = ref('')
const calvingDate = ref(new Date().toISOString().split('T')[0])
const calfBarnName = ref('')
const calfRegisteredName = ref('')
const calfSireName = ref('')
const calfDamName = ref('')
const calfSex = ref('2')
const birthWeight = ref('')
const calvingEase = ref('0')
const twins = ref(false)
const stillborn = ref(false)
const notes = ref('')
const calfPhotoFile = ref<File | null>(null)
const isUploadingPhoto = ref(false)
const saving = ref(false)
const saveError = ref('')
const saveStatus = ref('Saving calving…')

const emit = defineEmits<{
  close: []
  recordCalving: [data: any]
}>()

const openModal = (id: number, name: string) => {
  animalId.value = id
  animalName.value = name
  calvingDate.value = new Date().toISOString().split('T')[0]
  calfBarnName.value = ''
  calfRegisteredName.value = ''
  calfSireName.value = ''
  calfDamName.value = name
  calfSex.value = '2'
  birthWeight.value = ''
  calvingEase.value = '0'
  twins.value = false
  stillborn.value = false
  notes.value = ''
  calfPhotoFile.value = null
  isUploadingPhoto.value = false
  saving.value = false
  saveError.value = ''
  saveStatus.value = 'Saving calving…'
  isOpen.value = true
}

const closeModal = () => {
  isOpen.value = false
  emit('close')
}

const onCalfPhotoSelected = (event: Event) => {
  const input = event.target as HTMLInputElement
  calfPhotoFile.value = input.files?.[0] ?? null
}

const saveButtonText = computed(() => {
  if (isUploadingPhoto.value) return 'Uploading Photo…'
  if (saving.value) return 'Saving Calving…'
  return 'Record Calving (Move to Milking)'
})

const recordCalving = async () => {
  if (saving.value) return

  if (!animalId.value || !calvingDate.value) {
    alert('Please fill in required fields')
    return
  }

  saving.value = true
  saveError.value = ''
  saveStatus.value = 'Saving calving…'

  emit('recordCalving', {
    animalId: animalId.value,
    calvingDate: calvingDate.value,
    pictureUrl: null,
    photoFile: calfPhotoFile.value,
    calfBarnName: calfBarnName.value || null,
    calfRegisteredName: calfRegisteredName.value || null,
    calfSireName: calfSireName.value || null,
    calfDamName: calfDamName.value || null,
    calfSex: parseInt(calfSex.value),
    birthWeight: birthWeight.value ? parseFloat(birthWeight.value) : null,
    calvingEase: parseInt(calvingEase.value),
    twins: twins.value,
    stillborn: stillborn.value,
    notes: notes.value || null,
    setStatus: (status: string) => {
      saveStatus.value = status
      isUploadingPhoto.value = status.toLowerCase().includes('photo')
    },
    complete: (success: boolean, message?: string) => {
      saving.value = false
      if (success) {
        closeModal()
      } else {
        saveError.value = message || 'Calving could not be saved. Please try again.'
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

.saving-banner {
  position: sticky;
  top: 0;
  z-index: 3;
  display: flex;
  align-items: center;
  gap: 10px;
  margin: -8px -8px 14px;
  padding: 12px 14px;
  border: 1px solid #9bb79c;
  border-radius: 8px;
  background: #eff7ef;
  color: #183b20;
  box-shadow: 0 6px 18px rgba(24, 59, 32, .16);
}

.saving-spinner {
  width: 18px;
  height: 18px;
  border: 3px solid #bfd1c0;
  border-top-color: #31572c;
  border-radius: 50%;
  animation: saving-spin .8s linear infinite;
}

@keyframes saving-spin { to { transform: rotate(360deg); } }

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

.btn-primary:disabled {
  opacity: 0.65;
  cursor: wait;
}

.upload-hint {
  display: block;
  margin-top: 6px;
  color: #475569;
  font-size: 0.85rem;
}

.save-error {
  display: block;
  margin-top: 10px;
  color: #b91c1c;
  font-size: 0.85rem;
}
</style>
