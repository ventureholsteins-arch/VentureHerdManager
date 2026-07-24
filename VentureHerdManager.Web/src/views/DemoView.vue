<script setup lang="ts">
import { computed, ref } from 'vue'

interface DemoAnimal {
  id: number
  barnName: string
  stage: 'Milking' | 'Dry' | 'Heifer' | 'Calf'
  sireName: string
  damName: string
  lastHeatDate?: string
  dueDate?: string
  notes: string[]
  photoUrl?: string
}

interface DemoState {
  animals: DemoAnimal[]
  activity: string[]
}

const STORAGE_KEY = 'venture-demo-state-v1'

function initialDemoState(): DemoState {
  return {
    animals: [
      {
        id: 1,
        barnName: 'Demo Aurora',
        stage: 'Milking',
        sireName: 'Master',
        damName: 'Evergreen',
        lastHeatDate: new Date(Date.now() - 6 * 24 * 60 * 60 * 1000).toISOString(),
        dueDate: new Date(Date.now() + 24 * 24 * 60 * 60 * 1000).toISOString(),
        notes: ['Strong appetite', 'Clean gait'],
        photoUrl: 'https://picsum.photos/seed/demo-aurora/600/400'
      },
      {
        id: 2,
        barnName: 'Demo Nova',
        stage: 'Dry',
        sireName: 'Detective',
        damName: 'Maple',
        dueDate: new Date(Date.now() + 11 * 24 * 60 * 60 * 1000).toISOString(),
        notes: ['Close-up ration started'],
        photoUrl: 'https://picsum.photos/seed/demo-nova/600/400'
      },
      {
        id: 3,
        barnName: 'Demo Clover',
        stage: 'Heifer',
        sireName: 'Unix',
        damName: 'Seashell',
        notes: ['Growing well']
      }
    ],
    activity: ['Demo data loaded']
  }
}

function loadState(): DemoState {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) {
    return initialDemoState()
  }

  try {
    const parsed = JSON.parse(raw) as DemoState
    if (!Array.isArray(parsed.animals) || !Array.isArray(parsed.activity)) {
      return initialDemoState()
    }
    return parsed
  } catch {
    return initialDemoState()
  }
}

const state = ref<DemoState>(loadState())

function persist() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state.value))
}

function logActivity(message: string) {
  state.value.activity.unshift(`${new Date().toLocaleString()} - ${message}`)
  state.value.activity = state.value.activity.slice(0, 20)
}

function resetDemoData() {
  state.value = initialDemoState()
  persist()
}

function recordHeat(animalId: number) {
  const animal = state.value.animals.find(item => item.id === animalId)
  if (!animal) return

  animal.lastHeatDate = new Date().toISOString()
  logActivity(`Recorded heat for ${animal.barnName}`)
  persist()
}

function addNote(animalId: number) {
  const animal = state.value.animals.find(item => item.id === animalId)
  if (!animal) return

  animal.notes.unshift('Demo note added by visitor')
  animal.notes = animal.notes.slice(0, 5)
  logActivity(`Added note to ${animal.barnName}`)
  persist()
}

function recordCalving(animalId: number) {
  const dam = state.value.animals.find(item => item.id === animalId)
  if (!dam) return

  const nextId = Math.max(...state.value.animals.map(item => item.id), 0) + 1
  const calfName = `Demo Calf ${nextId}`

  state.value.animals.unshift({
    id: nextId,
    barnName: calfName,
    stage: 'Calf',
    sireName: dam.sireName || 'Demo Sire',
    damName: dam.barnName,
    notes: ['Created from demo calving action'],
    photoUrl: dam.photoUrl
  })

  logActivity(`Recorded calving for ${dam.barnName}; created ${calfName}`)
  persist()
}

const totalAnimals = computed(() => state.value.animals.length)
const milkingCount = computed(() => state.value.animals.filter(item => item.stage === 'Milking').length)
</script>

<template>
  <main class="demo-page">
    <header class="demo-hero">
      <div>
        <p class="demo-tag">DEMO MODE</p>
        <h1>Venture Herd Demo Sandbox</h1>
        <p>Safe fake-data environment. Nothing here touches production records.</p>
      </div>

      <button class="reset-btn" type="button" @click="resetDemoData">
        Reset Demo Data
      </button>
    </header>

    <section class="demo-stats">
      <div class="stat-card">
        <small>Total Animals</small>
        <strong>{{ totalAnimals }}</strong>
      </div>
      <div class="stat-card">
        <small>Milking</small>
        <strong>{{ milkingCount }}</strong>
      </div>
    </section>

    <section class="demo-grid">
      <article v-for="animal in state.animals" :key="animal.id" class="animal-card">
        <img v-if="animal.photoUrl" :src="animal.photoUrl" :alt="animal.barnName" class="animal-photo">

        <div class="animal-head">
          <h2>{{ animal.barnName }}</h2>
          <span class="stage-pill">{{ animal.stage }}</span>
        </div>

        <p>Sire: {{ animal.sireName || 'Unknown' }}</p>
        <p>Dam: {{ animal.damName || 'Unknown' }}</p>

        <p v-if="animal.lastHeatDate">Last heat: {{ new Date(animal.lastHeatDate).toLocaleDateString() }}</p>
        <p v-if="animal.dueDate">Due: {{ new Date(animal.dueDate).toLocaleDateString() }}</p>

        <div class="actions">
          <button type="button" @click="recordHeat(animal.id)">Record Heat</button>
          <button type="button" @click="recordCalving(animal.id)">Record Calving</button>
          <button type="button" @click="addNote(animal.id)">Add Note</button>
        </div>
      </article>
    </section>

    <section class="activity-panel">
      <h3>Recent Demo Activity</h3>
      <ul>
        <li v-for="entry in state.activity" :key="entry">{{ entry }}</li>
      </ul>
    </section>
  </main>
</template>

<style scoped>
.demo-page {
  max-width: 1100px;
  margin: 0 auto;
  padding: 20px;
}

.demo-hero {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  padding: 18px;
  border: 1px solid #d5dde4;
  border-left: 5px solid #1f6f3a;
  border-radius: 10px;
  background: linear-gradient(180deg, #ffffff, #f5f9f6);
}

.demo-tag {
  margin: 0 0 6px;
  color: #1f6f3a;
  font-weight: 800;
  letter-spacing: 0.12em;
  font-size: 0.75rem;
}

.demo-hero h1 {
  margin: 0;
}

.demo-hero p {
  margin: 8px 0 0;
  color: #425466;
}

.reset-btn {
  border: 1px solid #1f6f3a;
  border-radius: 6px;
  background: #1f6f3a;
  color: #fff;
  padding: 10px 12px;
  font-weight: 700;
  cursor: pointer;
}

.demo-stats {
  margin-top: 14px;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.stat-card {
  background: #fff;
  border: 1px solid #dce4dd;
  border-radius: 8px;
  padding: 12px;
}

.stat-card small {
  color: #5a6b61;
}

.stat-card strong {
  display: block;
  font-size: 1.5rem;
}

.demo-grid {
  margin-top: 16px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 12px;
}

.animal-card {
  background: #fff;
  border: 1px solid #d6dde7;
  border-radius: 8px;
  padding: 12px;
}

.animal-photo {
  width: 100%;
  height: 150px;
  object-fit: cover;
  border-radius: 6px;
  margin-bottom: 8px;
}

.animal-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}

.animal-head h2 {
  margin: 0;
  font-size: 1.1rem;
}

.stage-pill {
  border: 1px solid #cad4d9;
  border-radius: 999px;
  padding: 2px 8px;
  font-size: 0.75rem;
  font-weight: 700;
}

.actions {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 6px;
  margin-top: 10px;
}

.actions button {
  border: 1px solid #c9d5ca;
  border-radius: 6px;
  background: #f6faf7;
  padding: 8px 6px;
  font-size: 0.8rem;
  cursor: pointer;
}

.activity-panel {
  margin-top: 16px;
  background: #fff;
  border: 1px solid #dce4eb;
  border-radius: 8px;
  padding: 12px;
}

.activity-panel h3 {
  margin-top: 0;
}

.activity-panel ul {
  margin: 0;
  padding-left: 18px;
}

@media (max-width: 640px) {
  .demo-hero {
    flex-direction: column;
  }

  .actions {
    grid-template-columns: 1fr;
  }
}
</style>
