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

const filters = [
  { key: 'all', label: 'All' },
  { key: 'milking', label: 'Milking' },
  { key: 'dry', label: 'Dry' },
  { key: 'heifer', label: 'Heifers' },
  { key: 'calf', label: 'Calves' },
  { key: 'bull', label: 'Bulls' }
]

onMounted(async () => {
  try {
    const result = await getAnimals()
    animals.value = Array.isArray(result) ? result : []
  } catch (error) {
    console.error('Failed to load animals:', error)
    animals.value = []
  } finally {
    loading.value = false
  }
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

        <span class="result-count">
          {{ filteredAnimals.length }}
        </span>
      </div>

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
          @click="selectedFilter = filter.key"
        >
          {{ filter.label }}
        </button>
      </div>

      <p v-if="loading" class="message">
        Loading herd...
      </p>

      <div
        v-else-if="filteredAnimals.length === 0"
        class="empty-state"
      >
        <strong>No animals found</strong>
        <p>Try another filter or search.</p>
      </div>

      <div v-else class="animal-list">
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

.result-count {
  min-width: 38px;
  padding: 8px 11px;
  border-radius: 999px;
  background: #31572c;
  color: white;
  text-align: center;
  font-weight: 800;
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
}
</style>