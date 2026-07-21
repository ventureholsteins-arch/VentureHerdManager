<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import DashboardSummary from '../components/DashboardSummary.vue'
import QuickActions from '../components/QuickActions.vue'
import AnimalCard from '../components/AnimalCard.vue'

import { getAnimals } from '../api/animals'
import type { Animal } from '../Models/Animal'

const router = useRouter()

const animals = ref<Animal[]>([])
const searchText = ref('')
const selectedFilter = ref('all')

const loading = ref(true)
const refreshing = ref(false)
const loadError = ref('')
const lastUpdated = ref<Date | null>(null)

const dashboardRefreshKey = ref(0)

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

const animalCounts = computed(() => ({
  total: animals.value.length,
  milking: animals.value.filter(
    animal => animal.animalStage === 3
  ).length,
  dry: animals.value.filter(
    animal => animal.animalStage === 4
  ).length,
  heifers: animals.value.filter(
    animal => animal.animalStage === 2
  ).length,
  calves: animals.value.filter(
    animal => animal.animalStage === 1
  ).length,
  bulls: animals.value.filter(
    animal => animal.animalStage === 5
  ).length
}))

const formattedLastUpdated = computed(() => {
  if (!lastUpdated.value) {
    return ''
  }

  return lastUpdated.value.toLocaleTimeString([], {
    hour: 'numeric',
    minute: '2-digit'
  })
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
      (
        selectedFilter.value === 'milking' &&
        animal.animalStage === 3
      ) ||
      (
        selectedFilter.value === 'dry' &&
        animal.animalStage === 4
      ) ||
      (
        selectedFilter.value === 'heifer' &&
        animal.animalStage === 2
      ) ||
      (
        selectedFilter.value === 'calf' &&
        animal.animalStage === 1
      ) ||
      (
        selectedFilter.value === 'bull' &&
        animal.animalStage === 5
      )

    return matchesSearch && matchesFilter
  })
})

async function loadAnimals(showMainLoader = true) {
  if (showMainLoader) {
    loading.value = true
  }

  loadError.value = ''

  try {
    const result = await getAnimals()

    animals.value = Array.isArray(result)
      ? result
      : []

    lastUpdated.value = new Date()
  } catch (error) {
    console.error('Failed to load animals:', error)

    animals.value = []

    loadError.value =
      error instanceof Error
        ? error.message
        : 'The herd could not be loaded.'
  } finally {
    if (showMainLoader) {
      loading.value = false
    }
  }
}

async function refreshDashboard() {
  if (refreshing.value) {
    return
  }

  refreshing.value = true
  loadError.value = ''

  try {
    await loadAnimals(false)

    dashboardRefreshKey.value += 1
    lastUpdated.value = new Date()
  } catch (error) {
    console.error('Failed to refresh dashboard:', error)
  } finally {
    refreshing.value = false
  }
}

onMounted(async () => {
  await loadAnimals()
})

function openCalendar() {
  router.push('/calendar')
}

function openAnimal(animal: Animal) {
  router.push(`/animals/${animal.animalId}`)
}

function selectFilter(filterKey: string) {
  selectedFilter.value = filterKey
}

function clearSearchAndFilters() {
  searchText.value = ''
  selectedFilter.value = 'all'
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

function openAddAnimal() {
  resetAnimalForm()
  showAddAnimal.value = true
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

    if (!apiUrl) {
      throw new Error('The API address is not configured.')
    }

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
        errorText ||
        `Request failed with status ${response.status}`
      )
    }

    const addedAnimalName =
      newAnimal.value.barnName.trim()

    saveSuccess.value =
      `${addedAnimalName} was added successfully.`

    await loadAnimals(false)

    dashboardRefreshKey.value += 1

    window.setTimeout(() => {
      closeAddAnimal()
    }, 900)
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
  <main class="page">
    <header class="hero">
      <div class="hero-content">
        <p class="brand">
          VENTURE HERD MANAGER
        </p>

        <h1>Good Morning, Austin</h1>

        <p class="hero-description">
          Keep track of your herd, breeding activity,
          pregnancy checks, calvings, and daily records.
        </p>

        <div class="hero-stats">
          <span>
            <strong>{{ animalCounts.total }}</strong>
            total
          </span>

          <span>
            <strong>{{ animalCounts.milking }}</strong>
            milking
          </span>

          <span>
            <strong>{{ animalCounts.dry }}</strong>
            dry
          </span>

          <span v-if="formattedLastUpdated">
            Updated {{ formattedLastUpdated }}
          </span>
        </div>
      </div>

      <div class="hero-actions">
        <button
          class="calendar-button"
          type="button"
          @click="openCalendar"
        >
          <span
            class="calendar-icon"
            aria-hidden="true"
          >
            ▦
          </span>

          <span>Herd Calendar</span>
        </button>

        <button
          class="refresh-button"
          type="button"
          :disabled="refreshing"
          @click="refreshDashboard"
        >
          <span
            class="refresh-icon"
            :class="{ spinning: refreshing }"
            aria-hidden="true"
          >
            ↻
          </span>

          <span>
            {{
              refreshing
                ? 'Refreshing...'
                : 'Refresh Dashboard'
            }}
          </span>
        </button>
      </div>
    </header>

    <section
      v-if="loadError"
      class="alert error-alert"
    >
      <div>
        <strong>Unable to load the dashboard</strong>
        <p>{{ loadError }}</p>
      </div>

      <button
        type="button"
        @click="refreshDashboard"
      >
        Try Again
      </button>
    </section>

    <DashboardSummary
      :key="dashboardRefreshKey"
    />

    <QuickActions />

    <section class="herd-section">
      <div class="herd-heading">
        <div>
          <p class="section-label">
            HERD
          </p>

          <h2>Your Animals</h2>

          <p class="section-description">
            Search, filter, view, or add animals to the herd.
          </p>
        </div>

        <div class="heading-actions">
          <span class="result-count">
            {{ filteredAnimals.length }}
          </span>

          <button
            class="add-animal-button"
            type="button"
            @click="
              showAddAnimal
                ? closeAddAnimal()
                : openAddAnimal()
            "
          >
            {{
              showAddAnimal
                ? 'Close Form'
                : '+ Add Animal'
            }}
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
            <p class="section-label">
              NEW RECORD
            </p>

            <h3>Add Animal</h3>

            <p>
              Enter the animal information below.
              Only the barn name is required.
            </p>
          </div>

          <button
            class="close-button"
            type="button"
            aria-label="Close add animal form"
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
              autocomplete="off"
            >
          </label>

          <label>
            <span>Registered Name</span>

            <input
              v-model="newAnimal.registeredName"
              placeholder="Venture Master Shila"
              autocomplete="off"
            >
          </label>

          <label>
            <span>Registration Number</span>

            <input
              v-model="newAnimal.registrationNumber"
              placeholder="840..."
              autocomplete="off"
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
              <option :value="1">
                Female
              </option>

              <option :value="2">
                Male
              </option>

              <option :value="0">
                Unknown
              </option>
            </select>
          </label>

          <label>
            <span>Stage</span>

            <select v-model.number="newAnimal.animalStage">
              <option :value="1">
                Calf
              </option>

              <option :value="2">
                Heifer
              </option>

              <option :value="3">
                Cow / Milking
              </option>

              <option :value="4">
                Dry Cow
              </option>

              <option :value="5">
                Bull
              </option>

              <option :value="0">
                Unknown
              </option>
            </select>
          </label>

          <label>
            <span>Breed</span>

            <input
              v-model="newAnimal.breed"
              placeholder="Holstein"
              autocomplete="off"
            >
          </label>

          <label>
            <span>Sire</span>

            <input
              v-model="newAnimal.sireName"
              placeholder="Sire name"
              autocomplete="off"
            >
          </label>

          <label>
            <span>Dam</span>

            <input
              v-model="newAnimal.damName"
              placeholder="Dam name"
              autocomplete="off"
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
            {{
              savingAnimal
                ? 'Saving Animal...'
                : 'Save Animal'
            }}
          </button>
        </div>
      </form>

      <div class="search-panel">
        <label class="search-field">
          <span class="sr-only">
            Search animals
          </span>

          <span
            class="search-icon"
            aria-hidden="true"
          >
            ⌕
          </span>

          <input
            v-model="searchText"
            placeholder="Search barn name, registered name, or registration number"
          >

          <button
            v-if="searchText"
            class="clear-search-button"
            type="button"
            aria-label="Clear search"
            @click="searchText = ''"
          >
            ×
          </button>
        </label>

        <div class="filters">
          <button
            v-for="filter in filters"
            :key="filter.key"
            class="filter-button"
            :class="{
              active: selectedFilter === filter.key
            }"
            type="button"
            @click="selectFilter(filter.key)"
          >
            {{ filter.label }}
          </button>
        </div>
      </div>

      <div
        v-if="loading"
        class="loading-state"
      >
        <div
          v-for="item in 4"
          :key="item"
          class="animal-skeleton"
        >
          <div class="skeleton-circle" />

          <div class="skeleton-content">
            <div
              class="skeleton-line skeleton-title"
            />

            <div
              class="skeleton-line skeleton-subtitle"
            />
          </div>
        </div>
      </div>

      <div
        v-else-if="filteredAnimals.length === 0"
        class="empty-state"
      >
        <div class="empty-icon">
          🐄
        </div>

        <strong>No animals found</strong>

        <p>
          Try another search or select a different herd filter.
        </p>

        <button
          class="secondary-button"
          type="button"
          @click="clearSearchAndFilters"
        >
          Clear Search and Filters
        </button>
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
  </main>
</template>

<style scoped>
.page {
  width: min(100%, 980px);
  margin: 0 auto;
  padding: 28px 24px 60px;
}

.hero {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 28px;
  margin-bottom: 26px;
  padding: 28px;
  border: 1px solid #dce5de;
  border-radius: 24px;
  background:
    linear-gradient(
      135deg,
      rgba(49, 87, 44, 0.08),
      rgba(255, 255, 255, 0.96)
    );
  box-shadow: 0 10px 30px rgba(33, 65, 41, 0.07);
}

.hero-content {
  min-width: 0;
}

.hero-actions {
  display: grid;
  flex-shrink: 0;
  gap: 10px;
}

.brand,
.section-label {
  margin: 0;
  color: #31572c;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.1em;
}

.hero h1 {
  margin: 7px 0 8px;
  color: #18351f;
  font-size: clamp(30px, 5vw, 44px);
  line-height: 1.05;
}

.hero-description {
  max-width: 620px;
  margin: 0;
  color: #64748b;
  font-size: 16px;
  line-height: 1.6;
}

.hero-stats {
  display: flex;
  flex-wrap: wrap;
  gap: 9px;
  margin-top: 18px;
}

.hero-stats span {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 8px 11px;
  border: 1px solid rgba(49, 87, 44, 0.12);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.78);
  color: #526057;
  font-size: 13px;
  font-weight: 700;
}

.hero-stats strong {
  color: #254520;
}

.calendar-button,
.refresh-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 9px;
  min-height: 46px;
  padding: 12px 18px;
  border-radius: 999px;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    background 0.15s ease,
    border-color 0.15s ease,
    box-shadow 0.15s ease;
}

.calendar-button {
  border: 1px solid #bfd0c1;
  background: white;
  color: #31572c;
}

.calendar-button:hover {
  transform: translateY(-1px);
  border-color: #31572c;
  background: #f5f9f5;
}

.refresh-button {
  border: none;
  background: #31572c;
  color: white;
  box-shadow: 0 6px 16px rgba(49, 87, 44, 0.2);
}

.refresh-button:hover:not(:disabled) {
  transform: translateY(-1px);
  background: #254520;
  box-shadow: 0 9px 20px rgba(49, 87, 44, 0.24);
}

.refresh-button:disabled {
  cursor: wait;
  opacity: 0.7;
}

.calendar-icon,
.refresh-icon {
  display: inline-block;
  font-size: 20px;
  line-height: 1;
}

.spinning {
  animation: spin 0.8s linear infinite;
}

.alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  margin-bottom: 20px;
  padding: 16px 18px;
  border-radius: 16px;
}

.error-alert {
  border: 1px solid #fecaca;
  background: #fff5f5;
  color: #991b1b;
}

.alert strong {
  display: block;
  margin-bottom: 3px;
}

.alert p {
  margin: 0;
  line-height: 1.45;
}

.alert button {
  flex-shrink: 0;
  padding: 9px 14px;
  border: 1px solid #fecaca;
  border-radius: 999px;
  background: white;
  color: #991b1b;
  font-weight: 800;
  cursor: pointer;
}

.herd-section {
  margin-top: 30px;
  padding: 24px;
  border: 1px solid #e0e7e2;
  border-radius: 24px;
  background: #f8faf8;
}

.herd-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
}

.herd-heading h2 {
  margin: 5px 0;
  color: #1f3525;
  font-size: 28px;
}

.section-description {
  margin: 0;
  color: #64748b;
  line-height: 1.5;
}

.heading-actions {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 10px;
}

.result-count {
  min-width: 42px;
  padding: 10px 12px;
  border-radius: 999px;
  background: #e4eee5;
  color: #31572c;
  text-align: center;
  font-weight: 900;
}

.add-animal-button,
.save-button {
  min-height: 44px;
  padding: 11px 18px;
  border: none;
  border-radius: 999px;
  background: #31572c;
  color: white;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    background 0.15s ease;
}

.add-animal-button:hover,
.save-button:hover:not(:disabled) {
  transform: translateY(-1px);
  background: #254520;
}

.add-animal-form {
  margin-top: 22px;
  padding: 22px;
  border: 1px solid #dbe4dd;
  border-radius: 20px;
  background: white;
  box-shadow: 0 8px 24px rgba(32, 61, 39, 0.06);
}

.form-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
  margin-bottom: 20px;
}

.form-heading h3 {
  margin: 5px 0;
  color: #1f3525;
  font-size: 26px;
}

.form-heading p:not(.section-label) {
  margin: 0;
  color: #64748b;
}

.close-button {
  display: grid;
  flex-shrink: 0;
  width: 38px;
  height: 38px;
  place-items: center;
  border: none;
  border-radius: 50%;
  background: #eef3ef;
  color: #31572c;
  font-size: 25px;
  line-height: 1;
  cursor: pointer;
}

.close-button:hover {
  background: #e0e9e2;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
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
  min-height: 46px;
  padding: 13px 14px;
  border: 1px solid #d6e0d8;
  border-radius: 12px;
  background: white;
  color: #1e293b;
  font: inherit;
  outline: none;
  transition:
    border-color 0.15s ease,
    box-shadow 0.15s ease;
}

.form-grid input:focus,
.form-grid select:focus,
.form-grid textarea:focus {
  border-color: #4f7849;
  box-shadow: 0 0 0 3px rgba(49, 87, 44, 0.12);
}

.form-grid textarea {
  min-height: 110px;
  resize: vertical;
}

.full-width {
  grid-column: 1 / -1;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.secondary-button {
  min-height: 44px;
  padding: 11px 17px;
  border: 1px solid #d6e0d8;
  border-radius: 999px;
  background: white;
  color: #475569;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
}

.secondary-button:hover:not(:disabled) {
  border-color: #aebdaf;
  background: #f8faf8;
}

.save-button:disabled,
.secondary-button:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

.form-message {
  margin: 17px 0 0;
  padding: 13px 15px;
  border-radius: 12px;
  font-weight: 700;
}

.error-message {
  border: 1px solid #fecaca;
  background: #fef2f2;
  color: #b91c1c;
}

.success-message {
  border: 1px solid #bbf7d0;
  background: #f0fdf4;
  color: #166534;
}

.search-panel {
  margin: 22px 0 18px;
  padding: 16px;
  border: 1px solid #e0e7e2;
  border-radius: 18px;
  background: white;
}

.search-field {
  position: relative;
  display: block;
}

.search-field input {
  box-sizing: border-box;
  width: 100%;
  min-height: 50px;
  padding: 14px 46px 14px 44px;
  border: 1px solid #d6e0d8;
  border-radius: 14px;
  background: #fbfcfb;
  color: #1e293b;
  font: inherit;
  font-size: 16px;
  outline: none;
}

.search-field input:focus {
  border-color: #4f7849;
  background: white;
  box-shadow: 0 0 0 3px rgba(49, 87, 44, 0.12);
}

.search-icon {
  position: absolute;
  top: 50%;
  left: 16px;
  color: #6b7a70;
  font-size: 21px;
  transform: translateY(-50%);
}

.clear-search-button {
  position: absolute;
  top: 50%;
  right: 10px;
  display: grid;
  width: 32px;
  height: 32px;
  place-items: center;
  border: none;
  border-radius: 50%;
  background: #e9efea;
  color: #48604d;
  font-size: 20px;
  cursor: pointer;
  transform: translateY(-50%);
}

.filters {
  display: flex;
  gap: 8px;
  margin-top: 13px;
  padding-bottom: 2px;
  overflow-x: auto;
}

.filter-button {
  flex-shrink: 0;
  min-height: 40px;
  padding: 9px 15px;
  border: 1px solid #d6e0d8;
  border-radius: 999px;
  background: white;
  color: #475569;
  font: inherit;
  font-weight: 750;
  cursor: pointer;
  transition:
    border-color 0.15s ease,
    background 0.15s ease,
    color 0.15s ease;
}

.filter-button:hover:not(.active) {
  border-color: #aab9ad;
  background: #f5f8f5;
}

.filter-button.active {
  border-color: #31572c;
  background: #31572c;
  color: white;
}

.animal-list,
.loading-state {
  display: grid;
  gap: 12px;
}

.animal-skeleton {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 17px;
  border: 1px solid #e2e8e3;
  border-radius: 16px;
  background: white;
}

.skeleton-circle {
  width: 46px;
  height: 46px;
  flex-shrink: 0;
  border-radius: 50%;
  background: #edf1ed;
  animation: pulse 1.25s ease-in-out infinite;
}

.skeleton-content {
  width: 100%;
}

.skeleton-line {
  border-radius: 999px;
  background: #edf1ed;
  animation: pulse 1.25s ease-in-out infinite;
}

.skeleton-title {
  width: 42%;
  height: 14px;
}

.skeleton-subtitle {
  width: 65%;
  height: 11px;
  margin-top: 9px;
}

.empty-state {
  display: grid;
  justify-items: center;
  padding: 36px 20px;
  border: 1px dashed #cfdacf;
  border-radius: 18px;
  background: white;
  color: #64748b;
  text-align: center;
}

.empty-icon {
  margin-bottom: 8px;
  font-size: 38px;
}

.empty-state strong {
  color: #334155;
  font-size: 19px;
}

.empty-state p {
  margin: 7px 0 18px;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  border: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes pulse {
  0%,
  100% {
    opacity: 0.55;
  }

  50% {
    opacity: 1;
  }
}

@media (max-width: 720px) {
  .page {
    padding: 16px 14px 42px;
  }

  .hero {
    flex-direction: column;
    padding: 22px 18px;
    border-radius: 20px;
  }

  .hero h1 {
    font-size: 32px;
  }

  .hero-actions {
    width: 100%;
  }

  .calendar-button,
  .refresh-button {
    width: 100%;
  }

  .herd-section {
    padding: 18px 14px;
    border-radius: 20px;
  }

  .herd-heading {
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

  .alert {
    align-items: stretch;
    flex-direction: column;
  }

  .alert button {
    width: 100%;
  }
}

@media (max-width: 420px) {
  .hero-stats {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .hero-stats span {
    justify-content: center;
  }

  .heading-actions {
    display: grid;
    grid-template-columns: auto 1fr;
  }

  .form-actions {
    grid-template-columns: 1fr;
  }
}
</style>