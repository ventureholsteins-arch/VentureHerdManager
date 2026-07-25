<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { onBeforeRouteLeave, useRouter } from 'vue-router'

import { createAnimal, getAnimals } from '../api/animals'

const router = useRouter()

const isSaving = ref(false)
const saveMessage = ref('')
const errorMessage = ref('')
const existingRegistrationNumbers = ref<string[]>([])

const form = ref({
  barnName: '',
  registeredName: '',
  registrationNumber: '',
  birthDate: '',
  sex: 1,
  animalStage: 2,
  animalStatus: 0,
  breed: 'Holstein',
  sireName: '',
  damName: '',
  notes: '',
  isFavorite: false
})

const breedOptions = [
  'Holstein',
  'Jersey',
  'Ayrshire',
  'Guernsey',
  'Brown Swiss',
  'Milking Shorthorn',
  'Other'
]

const stageOptions = [
  { value: 1, label: 'Calf' },
  { value: 2, label: 'Heifer' },
  { value: 3, label: 'Milking' },
  { value: 4, label: 'Dry' },
  { value: 5, label: 'Bull' }
]

const sexOptions = [
  { value: 1, label: 'Female' },
  { value: 2, label: 'Male' }
]

const statusOptions = [
  { value: 0, label: 'Active' },
  { value: 1, label: 'Sold' },
  { value: 2, label: 'Deceased' }
]

const trimmedBarnName = computed(() => form.value.barnName.trim())
const trimmedRegistration = computed(() => form.value.registrationNumber.trim())

const duplicateRegistrationWarning = computed(() => {
  const registration = trimmedRegistration.value.toUpperCase()
  if (!registration) return ''

  return existingRegistrationNumbers.value.includes(registration)
    ? `Warning: registration number ${trimmedRegistration.value} already exists.`
    : ''
})

const barnNameError = computed(() => {
  if (!trimmedBarnName.value) {
    return 'Barn name is required.'
  }

  if (trimmedBarnName.value.length > 100) {
    return 'Barn name must be 100 characters or less.'
  }

  return ''
})

const canSave = computed(() => {
  return !isSaving.value && !barnNameError.value
})

const hasUnsavedChanges = computed(() => {
  return (
    form.value.barnName.trim().length > 0 ||
    form.value.registeredName.trim().length > 0 ||
    form.value.registrationNumber.trim().length > 0 ||
    form.value.birthDate.trim().length > 0 ||
    form.value.breed.trim().length > 0 ||
    form.value.sireName.trim().length > 0 ||
    form.value.damName.trim().length > 0 ||
    form.value.notes.trim().length > 0 ||
    form.value.isFavorite
  )
})

const beforeUnloadHandler = (event: BeforeUnloadEvent) => {
  if (!hasUnsavedChanges.value || isSaving.value) {
    return
  }

  event.preventDefault()
  event.returnValue = ''
}

onMounted(async () => {
  window.addEventListener('beforeunload', beforeUnloadHandler)

  try {
    const animals = await getAnimals()
    existingRegistrationNumbers.value = animals
      .map(animal => (animal.registrationNumber ?? '').trim().toUpperCase())
      .filter(Boolean)
  } catch (error) {
    console.warn('Could not preload registration numbers:', error)
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', beforeUnloadHandler)
})

onBeforeRouteLeave(() => {
  if (!hasUnsavedChanges.value || isSaving.value) {
    return true
  }

  return window.confirm('You have unsaved changes. Leave without saving?')
})

function cancel() {
  if (hasUnsavedChanges.value && !window.confirm('Discard this new animal?')) {
    return
  }

  router.push('/')
}

async function saveAnimal() {
  errorMessage.value = ''
  saveMessage.value = ''

  if (barnNameError.value) {
    errorMessage.value = barnNameError.value
    return
  }

  isSaving.value = true

  try {
    const created = await createAnimal({
      barnName: trimmedBarnName.value,
      registeredName: form.value.registeredName.trim() || null,
      registrationNumber: trimmedRegistration.value || null,
      birthDate: form.value.birthDate.trim() || null,
      sex: form.value.sex,
      animalStage: form.value.animalStage,
      animalStatus: form.value.animalStatus,
      breed: form.value.breed.trim() || null,
      sireName: form.value.sireName.trim() || null,
      damName: form.value.damName.trim() || null,
      notes: form.value.notes.trim() || null,
      isFavorite: form.value.isFavorite
    })

    saveMessage.value = 'Animal created successfully.'
    router.push(`/animals/${created.animalId}`)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to create animal.'
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <main class="create-page">
    <section class="panel">
      <h1>Add Animal</h1>
      <p class="subtitle">Simple form with large inputs for quick barn-side entry.</p>

      <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
      <p v-if="saveMessage" class="message success">{{ saveMessage }}</p>

      <div class="grid">
        <label>
          Barn Name *
          <input v-model="form.barnName" type="text" maxlength="100" />
          <small v-if="barnNameError" class="error-text">{{ barnNameError }}</small>
        </label>

        <label>
          Registered Name
          <input v-model="form.registeredName" type="text" maxlength="200" />
        </label>

        <label>
          Registration Number
          <input v-model="form.registrationNumber" type="text" maxlength="100" />
          <small v-if="duplicateRegistrationWarning" class="warn-text">{{ duplicateRegistrationWarning }}</small>
        </label>

        <label>
          Birth Date
          <input v-model="form.birthDate" type="date" />
        </label>

        <label>
          Sex
          <select v-model.number="form.sex">
            <option v-for="option in sexOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
          </select>
        </label>

        <label>
          Stage
          <select v-model.number="form.animalStage">
            <option v-for="option in stageOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
          </select>
        </label>

        <label>
          Status
          <select v-model.number="form.animalStatus">
            <option v-for="option in statusOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
          </select>
        </label>

        <label>
          Breed
          <select v-model="form.breed">
            <option v-for="breed in breedOptions" :key="breed" :value="breed">{{ breed }}</option>
          </select>
        </label>

        <label>
          Sire Name
          <input v-model="form.sireName" type="text" maxlength="200" />
        </label>

        <label>
          Dam Name
          <input v-model="form.damName" type="text" maxlength="200" />
        </label>

        <label class="full-width">
          Notes
          <textarea v-model="form.notes" rows="4" maxlength="4000" />
        </label>

        <label class="checkbox full-width">
          <input v-model="form.isFavorite" type="checkbox" />
          Favorite Animal
        </label>
      </div>

      <div class="actions">
        <button class="cancel" type="button" @click="cancel">Cancel</button>
        <button class="save" type="button" :disabled="!canSave" @click="saveAnimal">
          {{ isSaving ? 'Saving...' : 'Save Animal' }}
        </button>
      </div>
    </section>
  </main>
</template>

<style scoped>
.create-page {
  max-width: 920px;
  margin: 0 auto;
  padding: 20px;
}

.panel {
  background: #fff;
  border: 1px solid #dfe7e1;
  border-radius: 14px;
  padding: 22px;
  box-shadow: 0 8px 24px rgba(14, 24, 16, 0.06);
}

h1 {
  margin: 0;
  font-size: 2rem;
}

.subtitle {
  margin: 8px 0 16px;
  color: #54616d;
}

.message {
  border-radius: 10px;
  padding: 10px 12px;
  font-weight: 700;
}

.message.error {
  background: #fff1f2;
  color: #9f1239;
}

.message.success {
  background: #ecfdf3;
  color: #166534;
}

.grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.full-width {
  grid-column: span 2;
}

label {
  display: grid;
  gap: 6px;
  font-weight: 700;
  color: #1f2937;
}

input,
select,
textarea {
  min-height: 46px;
  border: 1px solid #c8d4cc;
  border-radius: 10px;
  padding: 10px 12px;
  font-size: 1.05rem;
}

textarea {
  min-height: 96px;
}

.checkbox {
  display: flex;
  align-items: center;
  gap: 10px;
}

.checkbox input {
  min-height: auto;
}

.error-text {
  color: #b91c1c;
  font-weight: 600;
}

.warn-text {
  color: #92400e;
  font-weight: 600;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 16px;
}

button {
  min-height: 46px;
  border-radius: 10px;
  border: none;
  padding: 0 16px;
  font-size: 1.05rem;
  font-weight: 800;
  cursor: pointer;
}

button.cancel {
  background: #e5e7eb;
  color: #111827;
}

button.save {
  background: #31572c;
  color: #fff;
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 760px) {
  .grid {
    grid-template-columns: 1fr;
  }

  .full-width {
    grid-column: span 1;
  }
}
</style>
