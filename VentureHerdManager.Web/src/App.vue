<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import DashboardSummary from './components/DashboardSummary.vue'
import QuickActions from './components/QuickActions.vue'
import AnimalCard from './components/AnimalCard.vue'
import AnimalDetail from './components/AnimalDetail.vue'

type Animal = {
  animalId: number
  barnName: string | null
  registeredName: string | null
  registrationNumber: string | null
}

const animals = ref<Animal[]>([])
const selectedAnimal = ref<Animal | null>(null)
const searchText = ref('')
const loading = ref(true)

onMounted(async () => {
  const response = await fetch('http://localhost:5051/api/Animals')
  animals.value = await response.json()
  loading.value = false
})

const filteredAnimals = computed(() => {
  const search = searchText.value.toLowerCase().trim()
  if (!search) return animals.value

  return animals.value.filter(animal =>
    animal.barnName?.toLowerCase().includes(search) ||
    animal.registeredName?.toLowerCase().includes(search) ||
    animal.registrationNumber?.toLowerCase().includes(search)
  )
})
</script>

<template>
  <main class="app">
    <header class="top">
      <p class="brand">Venture Herd Manager</p>
      <h1>Good morning, Austin</h1>
      <p class="sub">Today’s herd work at a glance.</p>
    </header>

    <DashboardSummary />

    <QuickActions />

    <section class="search">
      <label>Search animals</label>
      <input v-model="searchText" placeholder="Barn name, full name, or registration..." />
    </section>

    <AnimalDetail
      v-if="selectedAnimal"
      :animal="selectedAnimal"
      @close="selectedAnimal = null"
    />

    <section class="animals">
      <div class="section-title">
        <h2>Animals</h2>
        <span>{{ filteredAnimals.length }}</span>
      </div>

      <p v-if="loading" class="loading">Loading animals...</p>

      <AnimalCard
        v-for="animal in filteredAnimals"
        v-else
        :key="animal.animalId"
        :animal="animal"
        @select="selectedAnimal = animal"
      />
    </section>

    <nav class="bottom-nav">
      <button class="active">Home</button>
      <button>Animals</button>
      <button>Reports</button>
      <button>More</button>
    </nav>
  </main>
</template>

<style scoped>
.app {
  min-height: 100vh;
  padding: 22px 18px 92px;
  background: #f3f6fb;
  color: #142033;
  font-family: Inter, Arial, sans-serif;
}

.top { margin-bottom: 20px; }

.brand {
  margin: 0 0 8px;
  color: #31572c;
  font-size: 14px;
  font-weight: 800;
}

h1 {
  margin: 0;
  font-size: 31px;
  line-height: 1.05;
}

.sub {
  margin: 9px 0 0;
  color: #64748b;
  font-size: 16px;
}

.search { margin-bottom: 18px; }

.search label {
  display: block;
  margin-bottom: 8px;
  color: #334155;
  font-size: 15px;
  font-weight: 800;
}

.search input {
  width: 100%;
  padding: 17px 18px;
  border-radius: 18px;
  border: 1px solid #cbd5e1;
  background: white;
  color: #142033;
  font-size: 17px;
  outline: none;
}

.section-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 13px;
}

.section-title h2 {
  margin: 0;
  font-size: 23px;
}

.section-title span {
  min-width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border-radius: 999px;
  background: #31572c;
  color: white;
  font-weight: 800;
}

.bottom-nav {
  position: fixed;
  left: 12px;
  right: 12px;
  bottom: 12px;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px;
  padding: 10px;
  border-radius: 24px;
  background: white;
  border: 1px solid #e2e8f0;
}

.bottom-nav button {
  padding: 12px 4px;
  border-radius: 16px;
  background: transparent;
  color: #64748b;
  font-weight: 800;
  border: none;
}

.bottom-nav .active {
  background: #142033;
  color: white;
}
</style>