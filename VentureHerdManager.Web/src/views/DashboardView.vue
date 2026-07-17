<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import DashboardSummary from '../components/DashboardSummary.vue'
import QuickActions from '../components/QuickActions.vue'
import AnimalCard from '../components/AnimalCard.vue'

import { getAnimals } from '../api/animals'
import type { Animal } from '../models/Animal'

const router = useRouter()

const animals = ref<Animal[]>([])
const searchText = ref('')
const selectedFilter = ref('all')
const loading = ref(true)

const showAddAnimal = ref(false)
const savingAnimal = ref(false)
const saveError = ref('')
const saveSuccess = ref('')

const newAnimal = ref({
  barnName: '',
  registeredName: '',
  registrationNumber: '',
  birthDate: '',
  sex: 1,
  animalStage: 3,
  breed: 'Holstein',
  sireName: '',
  damName: '',
  notes: ''
})

const filters = [
  { key: 'all', label: 'All' },
  { key: 'milking', label: 'Milking' },
  { key: 'dry', label: 'Dry' },
  { key: 'heifer', label: 'Heifers' },
  { key: 'calf', label: 'Calves' },
  { key: 'bull', label: 'Bulls' }
]

async function loadAnimals() {
  loading.value = true

  try {
    const result = await getAnimals()
    animals.value = Array.isArray(result) ? result : []
  } catch (error) {
    console.error('Failed to load animals:', error)
    animals.value = []
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadAnimals()
})

const filteredAnimals = computed(() => {
  const search = searchText.value.toLowerCase().trim()

  return animals.value.filter(animal => {
    const matchesSearch =
      !search ||
      animal.barnName?.toLowerCase().includes(search) ||
      animal.registeredName?.toLowerCase().includes(search) ||
      animal.registrationNumber?.toLowerCase().includes(search)

    const matchesFilter =
      selectedFilter.value === 'all' ||
      (selectedFilter.value === 'milking' && animal.animalStage === 3) ||
      (selectedFilter.value === 'dry' && animal.animalStage === 4) ||
      (selectedFilter.value === 'heifer' && animal.animalStage === 2) ||
      (selectedFilter.value === 'calf' && animal.animalStage === 1) ||
      (selectedFilter.value === 'bull' && animal.animalStage === 5)

    return matchesSearch && matchesFilter
  })
})

function openAnimal(animal: Animal) {
  router.push(`/animals/${animal.animalId}`)
}

function resetAnimalForm() {
  newAnimal.value = {
    barnName: '',
    registeredName: '',
    registrationNumber: '',
    birthDate: '',
    sex: 1,
    animalStage: 3,
    breed: 'Holstein',
    sireName: '',
    damName: '',
    notes: ''
  }

  saveError.value = ''
  saveSuccess.value = ''
}

function closeAddAnimal() {
  showAddAnimal.value = false
  resetAnimalForm()
}

async function addAnimal() {
  saveError.value = ''
  saveSuccess.value = ''

  if (!newAnimal.value.barnName.trim()) {
    saveError.value = 'Barn name is required.'
    return
  }

  savingAnimal.value = true

  try {
    const apiUrl = import.meta.env.VITE_API_URL

    const response = await fetch(`${apiUrl}/Animals`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        animalStatus: 0,
        barnName: newAnimal.value.barnName.trim(),
        registeredName:
          newAnimal.value.registeredName.trim() || null,
        registrationNumber:
          newAnimal.value.registrationNumber.trim() || null,
        birthDate:
          newAnimal.value.birthDate || null,
        sex: Number(newAnimal.value.sex),
        animalStage: Number(newAnimal.value.animalStage),
        breed:
          newAnimal.value.breed.trim() || null,
        sireId: null,
        sireName:
          newAnimal.value.sireName.trim() || null,
        damId: null,
        damName:
          newAnimal.value.damName.trim() || null,
        notes:
          newAnimal.value.notes.trim() || null
      })
    })

    if (!response.ok) {
      const errorText = await response.text()

      throw new Error(
        errorText || `Request failed with status ${response.status}`
      )
    }

    saveSuccess.value = `${newAnimal.value.barnName} was added.`

    await loadAnimals()

    setTimeout(() => {
      closeAddAnimal()
    }, 700)
  } catch (error) {
    console.error('Failed to add animal:', error)

    saveError.value =
      error instanceof Error
        ? error.message
        : 'The animal could not be added.'
  } finally {
    savingAnimal.value = false
  }
}
</script>

<template>
  <div class="page">
    <header class="hero">
      <div>
        <p class="brand">VENTURE HERD MANAGER</p>

        <h1>Good Morning Austin</h1>

        <p>
          {{ animals.length }} animals loaded
        </p>
      </div>
    </header>

    <DashboardSummary />

    <QuickActions />

    <section class="herd-section">
      <div class="herd-heading">
        <div>
          <p class="section-label">HERD</p>
          <h2>Your Animals</h2>
        </div>

        <div class="heading-actions">
          <span class="result-count">
            {{ filteredAnimals.length }}
          </span>

          <button
            class="add-animal-button"
            type="button"
            @click="showAddAnimal ? closeAddAnimal() : showAddAnimal = true"
          >
            {{ showAddAnimal ? 'Cancel' : '+ Add Animal' }}
          </button>
        </div>
      </div>

      <form
        v-if="showAddAnimal"
        class="add-animal-form"
        @submit.prevent="addAnimal"
      >
        <div class="form-heading">
          <div>
            <p class="section-label">NEW RECORD</p>
            <h3>Add Animal</h3>
          </div>

          <button
            class="close-button"
            type="button"
            aria-label="Close form"
            @click="closeAddAnimal"
          >
            ×
          </button>
        </div>

        <div class="form-grid">
          <label>
            <span>Barn Name *</span>

            <input
              v-model="newAnimal.barnName"
              required
              placeholder="Example: Shila"
            >
          </label>

          <label>
            <span>Registered Name</span>

            <input
              v-model="newAnimal.registeredName"
              placeholder="Venture Master Shila"
            >
          </label>

          <label>
            <span>Registration Number</span>

            <input
              v-model="newAnimal.registrationNumber"
              placeholder="840..."
            >
          </label>

          <label>
            <span>Birth Date</span>

            <input
              v-model="newAnimal.birthDate"
              type="date"
            >
          </label>

          <label>
            <span>Sex</span>

            <select v-model.number="newAnimal.sex">
              <option :value="1">Female</option>
              <option :value="2">Male</option>
              <option :value="0">Unknown</option>
            </select>
          </label>

          <label>
            <span>Stage</span>

            <select v-model.number="newAnimal.animalStage">
              <option :value="1">Calf</option>
              <option :value="2">Heifer</option>
              <option :value="3">Cow / Milking</option>
              <option :value="4">Dry Cow</option>
              <option :value="5">Bull</option>
              <option :value="0">Unknown</option>
            </select>
          </label>

          <label>
            <span>Breed</span>

            <input
              v-model="newAnimal.breed"
              placeholder="Holstein"
            >
          </label>

          <label>
            <span>Sire</span>

            <input
              v-model="newAnimal.sireName"
              placeholder="Sire name"
            >
          </label>

          <label>
            <span>Dam</span>

            <input
              v-model="newAnimal.damName"
              placeholder="Dam name"
            >
          </label>

          <label class="full-width">
            <span>Notes</span>

            <textarea
              v-model="newAnimal.notes"
              rows="4"
              placeholder="Optional notes..."
            />
          </label>
        </div>

        <p
          v-if="saveError"
          class="form-message error-message"
        >
          {{ saveError }}
        </p>

        <p
          v-if="saveSuccess"
          class="form-message success-message"
        >
          {{ saveSuccess }}
        </p>

        <div class="form-actions">
          <button
            class="secondary-button"
            type="button"
            :disabled="savingAnimal"
            @click="closeAddAnimal"
          >
            Cancel
          </button>

          <button
            class="save-button"
            type="submit"
            :disabled="savingAnimal"
          >
            {{ savingAnimal ? 'Saving...' : 'Save Animal' }}
          </button>
        </div>
      </form>

      <div class="search">
        <input
          v-model="searchText"
          placeholder="Search barn name, registered name, or reg number..."
        >
      </div>

      <div class="filters">
        <button
          v-for="filter in filters"
          :key="filter.key"
          class="filter-button"
          :class="{ active: selectedFilter === filter.key }"
          type="button"
          @click="selectedFilter = filter.key"
        >
          {{ filter.label }}
        </button>
      </div>

      <p
        v-if="loading"
        class="message"
      >
        Loading herd...
      </p>

      <div
        v-else-if="filteredAnimals.length === 0"
        class="empty-state"
      >
        <strong>No animals found</strong>
        <p>Try another filter or search.</p>
      </div>

      <div
        v-else
        class="animal-list"
      >
        <AnimalCard
          v-for="animal in filteredAnimals"
          :key="animal.animalId"
          :animal="animal"
          @click="openAnimal(animal)"
        />
      </div>
    </section>
  </div>
</template>

<style scoped>
.page {
  max-width: 800px;
  margin: auto;
  padding: 24px;
}

.hero {
  margin-bottom: 25px;
}

.hero h1 {
  margin: 6px 0;
}

.hero p {
  margin-bottom: 0;
  color: #64748b;
}

.brand,
.section-label {
  margin: 0;
  color: #31572c;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.herd-section {
  margin-top: 28px;
}

.herd-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.herd-heading h2 {
  margin: 4px 0 0;
}

.heading-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.result-count {
  min-width: 38px;
  padding: 8px 11px;
  border-radius: 999px;
  background: #31572c;
  color: white;
  text-align: center;
  font-weight: 800;
}

.add-animal-button,
.save-button {
  padding: 11px 17px;
  border: none;
  border-radius: 999px;
  background: #31572c;
  color: white;
  font-weight: 800;
  cursor: pointer;
}

.add-animal-button:hover,
.save-button:hover {
  background: #254520;
}

.add-animal-form {
  margin-top: 18px;
  padding: 20px;
  border: 1px solid #dbe2df;
  border-radius: 18px;
  background: white;
}

.form-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 18px;
}

.form-heading h3 {
  margin: 4px 0 0;
  font-size: 24px;
}

.close-button {
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 50%;
  background: #eef2ef;
  color: #31572c;
  font-size: 24px;
  line-height: 1;
  cursor: pointer;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 15px;
}

.form-grid label {
  display: grid;
  gap: 7px;
}

.form-grid label span {
  color: #334155;
  font-size: 14px;
  font-weight: 800;
}

.form-grid input,
.form-grid select,
.form-grid textarea {
  box-sizing: border-box;
  width: 100%;
  padding: 13px;
  border: 1px solid #dbe2df;
  border-radius: 12px;
  background: white;
  color: #1e293b;
  font: inherit;
  outline: none;
}

.form-grid input:focus,
.form-grid select:focus,
.form-grid textarea:focus {
  border-color: #31572c;
  box-shadow: 0 0 0 3px rgba(49, 87, 44, 0.12);
}

.form-grid textarea {
  resize: vertical;
}

.full-width {
  grid-column: 1 / -1;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 18px;
}

.secondary-button {
  padding: 11px 17px;
  border: 1px solid #dbe2df;
  border-radius: 999px;
  background: white;
  color: #475569;
  font-weight: 800;
  cursor: pointer;
}

.save-button:disabled,
.secondary-button:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

.form-message {
  margin: 16px 0 0;
  padding: 12px 14px;
  border-radius: 12px;
  font-weight: 700;
}

.error-message {
  background: #fef2f2;
  color: #b91c1c;
}

.success-message {
  background: #f0fdf4;
  color: #166534;
}

.search {
  margin: 18px 0 14px;
}

.search input {
  box-sizing: border-box;
  width: 100%;
  padding: 16px;
  border: 1px solid #dbe2df;
  border-radius: 18px;
  background: white;
  font-size: 16px;
  outline: none;
}

.search input:focus {
  border-color: #31572c;
  box-shadow: 0 0 0 3px rgba(49, 87, 44, 0.12);
}

.filters {
  display: flex;
  gap: 9px;
  margin-bottom: 18px;
  padding-bottom: 4px;
  overflow-x: auto;
}

.filter-button {
  flex-shrink: 0;
  padding: 10px 15px;
  border: 1px solid #dbe2df;
  border-radius: 999px;
  background: white;
  color: #475569;
  font-weight: 700;
  cursor: pointer;
}

.filter-button.active {
  border-color: #31572c;
  background: #31572c;
  color: white;
}

.animal-list {
  display: grid;
  gap: 12px;
}

.message,
.empty-state {
  padding: 18px;
  border-radius: 16px;
  background: white;
}

.empty-state {
  color: #64748b;
}

.empty-state strong {
  color: #334155;
}

.empty-state p {
  margin: 5px 0 0;
}

@media (max-width: 600px) {
  .page {
    padding: 16px;
  }

  .hero h1 {
    font-size: 30px;
  }

  .herd-heading {
    align-items: flex-start;
    flex-direction: column;
  }

  .heading-actions {
    width: 100%;
  }

  .add-animal-button {
    flex: 1;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .full-width {
    grid-column: auto;
  }

  .form-actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }
}
</style>