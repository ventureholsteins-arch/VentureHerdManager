<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import { getAnimals } from '../api/animals'
import { getAppearance, type AppearanceSetting } from '../api/appearance'
import { recordHeat } from '../api/heat'
import { recordBreeding } from '../api/breeding'
import { recordCalving } from '../api/calving'
import { addNote } from '../api/notes'
import { recordLUT } from '../api/lut'
import type { Animal } from '../models/Animal'
import DashboardSummary from '../components/DashboardSummary.vue'
import QuickActions from '../components/QuickActions.vue'
import RecordHeatModal from '../components/RecordHeatModal.vue'
import RecordBreedingModal from '../components/RecordBreedingModal.vue'
import RecordCalvingModal from '../components/RecordCalvingModal.vue'
import AddNoteModal from '../components/AddNoteModal.vue'
import RecordLUTModal from '../components/RecordLUTModal.vue'
import EditAnimalModal from '../components/EditAnimalModal.vue'

const router = useRouter()

const animals = ref<Animal[]>([])
const appearance = ref<AppearanceSetting | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const refreshing = ref(false)
const dashboardRefreshKey = ref(0)

const searchQuery = ref('')
const stageFilter = ref<number | null>(null)
const heatModalRef = ref<InstanceType<typeof RecordHeatModal>>()
const breedingModalRef = ref<InstanceType<typeof RecordBreedingModal>>()
const calvingModalRef = ref<InstanceType<typeof RecordCalvingModal>>()
const noteModalRef = ref<InstanceType<typeof AddNoteModal>>()
const lutModalRef = ref<InstanceType<typeof RecordLUTModal>>()
const editAnimalModalRef = ref<InstanceType<typeof EditAnimalModal>>()
const selectedAnimalForEdit = ref<Animal | null>(null)

// Fuzzy search: match by name or sire
const fuzzyMatch = (query: string, text: string): boolean => {
  const q = query.toLowerCase()
  const t = text.toLowerCase()
  
  if (t.includes(q)) return true
  
  // Simple fuzzy: check if all characters appear in order
  let qIdx = 0
  for (let i = 0; i < t.length && qIdx < q.length; i++) {
    if (t[i] === q[qIdx]) qIdx++
  }
  return qIdx === q.length
}

const animalCounts = computed(() => ({
  total: animals.value.length,
  milking: animals.value.filter(animal => animal.animalStage === 3).length,
  dry: animals.value.filter(animal => animal.animalStage === 4).length,
  heifers: animals.value.filter(animal => animal.animalStage === 2).length,
  calves: animals.value.filter(animal => animal.animalStage === 1).length
}))

const filteredAnimals = computed(() => {
  let result = animals.value

  // Apply stage filter
  if (stageFilter.value !== null) {
    result = result.filter(animal => animal.animalStage === stageFilter.value)
  }

  // Apply search query
  const query = searchQuery.value.trim()
  if (!query) {
    return result
  }

  return result.filter(animal => {
    // Check barn name, registered name, registration number
    if (fuzzyMatch(query, animal.barnName || '')) return true
    if (fuzzyMatch(query, animal.registeredName || '')) return true
    if (fuzzyMatch(query, animal.registrationNumber || '')) return true
    // Check sire
    if (fuzzyMatch(query, animal.sireName || '')) return true
    return false
  })
})

const formattedLastUpdated = computed(() => {
  return null
})

const getStageLabel = (stage: number): string => {
  const stages: { [key: number]: string } = {
    1: 'Calf',
    2: 'Heifer',
    3: 'Milking',
    4: 'Dry',
    5: 'Unknown'
  }
  return stages[stage] || 'Unknown'
}

const getScoreLabel = (score: number | null | undefined): string => {
  if (!score) return '—'
  
  let grade = 'GP'
  if (score >= 90) grade = 'EX'
  else if (score >= 85) grade = 'VG'
  
  return `${grade} ${Math.round(score)}`
}

const getBaaLabel = (baa: number | null | undefined): string => {
  if (!baa) return '—'
  return `BAA ${baa.toFixed(2)}`
}

async function loadAnimals() {
  loading.value = true
  errorMessage.value = ''

  try {
    const [appearanceResponse, animalsResponse] = await Promise.all([
      getAppearance(),
      getAnimals()
    ])

    appearance.value = appearanceResponse
    animals.value = Array.isArray(animalsResponse) ? animalsResponse : []
  } catch (error) {
    console.error('Failed to load dashboard information:', error)
    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'The dashboard could not be loaded.'
  } finally {
    loading.value = false
    refreshing.value = false
  }
}

function openCalendar() {
  router.push('/calendar')
}

async function refreshDashboard() {
  refreshing.value = true
  await loadAnimals()
  dashboardRefreshKey.value += 1
}

// Modal event handlers
const openHeatModal = () => heatModalRef.value?.openModal()
const openBreedingModal = (id: number, name: string) => breedingModalRef.value?.openModal(id, name)
const openCalvingModal = (id: number, name: string) => calvingModalRef.value?.openModal(id, name)
const openNoteModal = (id: number, name: string) => noteModalRef.value?.openModal(id, name)
const openLUTModal = (id: number, name: string) => lutModalRef.value?.openModal(id, name)

// Handle heat recording
const onRecordHeat = async (data: any) => {
  try {
    await recordHeat({
      animalId: data.animalId,
      heatDateTime: new Date().toISOString(),
      heatStrength: data.heatStrength,
      standingHeat: data.standingHeat,
      pictureUrl: data.pictureUrl,
      notes: data.notes,
      hasEmbryoTransfer: data.hasEmbryoTransfer
    })
    
    await refreshDashboard()
    alert('Heat event recorded successfully!')
  } catch (error) {
    alert(`Error recording heat: ${error instanceof Error ? error.message : 'Unknown error'}`)
    console.error('Failed to record heat:', error)
  }
}

// Handle breeding recording
const onRecordBreeding = async (data: any) => {
  try {
    await recordBreeding({
      animalId: data.animalId,
      breedingDate: data.breedingDate,
      sireUsed: data.sireUsed,
      breedingType: data.breedingType,
      pregnancyStatus: data.pregnancyStatus,
      notes: data.notes
    })
    
    await refreshDashboard()
    alert('Breeding event recorded successfully!')
  } catch (error) {
    alert(`Error recording breeding: ${error instanceof Error ? error.message : 'Unknown error'}`)
    console.error('Failed to record breeding:', error)
  }
}

// Handle calving recording
const onRecordCalving = async (data: any) => {
  try {
    await recordCalving({
      animalId: data.animalId,
      calvingDate: data.calvingDate,
      pictureUrl: data.pictureUrl,
      calfBarnName: data.calfBarnName,
      calfRegisteredName: data.calfRegisteredName,
      calfSex: data.calfSex,
      birthWeight: data.birthWeight,
      calvingEase: data.calvingEase,
      twins: data.twins,
      stillborn: data.stillborn,
      notes: data.notes
    })
    
    await refreshDashboard()
    alert('Calving event recorded successfully! Cow moved to Milking status.')
  } catch (error) {
    alert(`Error recording calving: ${error instanceof Error ? error.message : 'Unknown error'}`)
    console.error('Failed to record calving:', error)
  }
}

// Handle note addition
const onAddNote = async (data: any) => {
  try {
    await addNote({
      animalId: data.animalId,
      noteText: data.noteText,
      noteType: data.noteType
    })
    
    await refreshDashboard()
    alert('Note added successfully!')
  } catch (error) {
    alert(`Error adding note: ${error instanceof Error ? error.message : 'Unknown error'}`)
    console.error('Failed to add note:', error)
  }
}

// Handle LUT recording
const onRecordLUT = async (data: any) => {
  try {
    await recordLUT({
      animalId: data.animalId,
      administrationDate: data.administrationDate,
      expectedHeatWatchStart: data.expectedHeatWatchStart,
      expectedHeatWatchEnd: data.expectedHeatWatchEnd,
      notes: data.notes
    })
    
    await refreshDashboard()
    alert('LUT injection recorded! Animal will be monitored for heat.')
  } catch (error) {
    alert(`Error recording LUT: ${error instanceof Error ? error.message : 'Unknown error'}`)
    console.error('Failed to record LUT:', error)
  }
}

// Handle opening edit animal modal
const openEditModal = (animal: Animal) => {
  selectedAnimalForEdit.value = animal
  editAnimalModalRef.value?.openModal()
}

// Handle saving edited animal
const onAnimalEdited = (updatedAnimal: Animal) => {
  const index = animals.value.findIndex(a => a.animalId === updatedAnimal.animalId)
  if (index !== -1) {
    animals.value[index] = updatedAnimal
  }
}

// Handle going to breeding from heat modal
const goToBreedingTab = (animalId: number) => {
  breedingModalRef.value?.openModal(animalId, 'Animal')
}

onMounted(() => {
  loadAnimals()
})
</script>

<template>
  <main class="page">
    <header
      class="hero"
      :style="{
        backgroundImage: `url('${appearance?.backgroundImageUrl || '/2F1D8FDE-C401-4E8F-AA57-0BAC674AD74D.jpg'}')`
      }"
    >
      <div class="hero-overlay" />

      <img
        :src="appearance?.logoUrl || '/farm-logo.png'"
        class="hero-farm-watermark"
        alt=""
        aria-hidden="true"
      >

      <div class="hero-inner">
        <div class="hero-main">
          <div class="hero-brand">
            <img
              src="/app-logo.png"
              class="hero-app-logo"
              alt="Venture Herd Manager"
            >
          </div>

          <div class="hero-stats">
            <div class="hero-stat">
              <span class="hero-stat-value">
                {{ animalCounts.total }}
              </span>

              <span class="hero-stat-label">
                Total
              </span>

              <small>Active animals</small>
            </div>

            <div class="hero-stat">
              <span class="hero-stat-value">
                {{ animalCounts.milking }}
              </span>

              <span class="hero-stat-label">
                Milking
              </span>

              <small>Cows</small>
            </div>

            <div class="hero-stat">
              <span class="hero-stat-value">
                {{ animalCounts.dry }}
              </span>

              <span class="hero-stat-label">
                Dry
              </span>

              <small>Cows</small>
            </div>

            <div class="hero-stat">
              <span class="hero-stat-value">
                {{ animalCounts.heifers + animalCounts.calves }}
              </span>

              <span class="hero-stat-label">
                Youngstock
              </span>

              <small>Heifers & calves</small>
            </div>
          </div>
        </div>

        <div class="hero-side">
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

              <span
                class="button-arrow"
                aria-hidden="true"
              >
                →
              </span>
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

            <span
              v-if="formattedLastUpdated"
              class="hero-updated"
            >
              Last updated {{ formattedLastUpdated }}
            </span>
          </div>
        </div>
      </div>
    </header>

    <section
      v-if="loading"
      class="card"
    >
      <p>Loading dashboard...</p>
    </section>

    <section
      v-else-if="errorMessage"
      class="card error-card"
    >
      <strong>Unable to load dashboard</strong>
      <p>{{ errorMessage }}</p>
    </section>

    <template v-else>
      <DashboardSummary :key="dashboardRefreshKey" />
      
      <section class="quick-actions-bar">
        <button @click="openHeatModal" class="quick-btn heat-btn">💉 Record Heat</button>
        <button @click="openLUTModal(0, 'Select Animal')" class="quick-btn lut-btn">💊 LUT Injection</button>
        <button class="quick-btn add-btn" @click="router.push('/animals/new')">➕ Add Animal</button>
      </section>

      <section class="herd-section">
        <div class="herd-header">
          <div>
            <p class="eyebrow">HERD</p>
            <h2>Animals</h2>
            <p class="herd-subtitle">Search by name or sire • Click to manage</p>
          </div>

          <div class="herd-actions">
            <input
              v-model="searchQuery"
              type="search"
              class="search-input-large"
              placeholder="🔎 Search by animal name, sire, or registration..."
            >
            <div class="filter-buttons">
              <button
                @click="stageFilter = stageFilter === 3 ? null : 3"
                :class="['filter-btn', { active: stageFilter === 3 }]"
              >
                🥛 Milking Cows
              </button>
              <button
                @click="stageFilter = stageFilter === 2 ? null : 2"
                :class="['filter-btn', { active: stageFilter === 2 }]"
              >
                🐮 Heifers
              </button>
            </div>
          </div>
        </div>

        <div v-if="filteredAnimals.length" class="animal-grid">
          <div
            v-for="animal in filteredAnimals"
            :key="animal.animalId"
            class="animal-card-enhanced"
          >
            <button
              class="card-main"
              type="button"
              @click="router.push(`/animals/${animal.animalId}`)"
            >
              <div class="card-header">
                <div class="card-name">
                  <strong>{{ animal.barnName || animal.registeredName || `Animal #${animal.animalId}` }}</strong>
                  <p class="card-stage">{{ getStageLabel(animal.animalStage) }}</p>
                </div>
                <div class="card-status" :class="animal.animalStatus === 0 ? 'active' : 'inactive'">
                  {{ animal.animalStatus === 0 ? '✓' : '✕' }}
                </div>
              </div>

              <div class="card-details">
                <div class="detail-row">
                  <span class="label">Breed:</span>
                  <span>{{ animal.breed || '—' }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Sire:</span>
                  <span>{{ animal.sireName || '—' }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Lactation:</span>
                  <span>{{ animal.currentLactation || '—' }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Score:</span>
                  <span>{{ getScoreLabel(animal.latestScore) }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">BAA:</span>
                  <span>{{ getBaaLabel(animal.latestBaa) }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Reg #:</span>
                  <span class="reg-num">{{ animal.registrationNumber || '—' }}</span>
                </div>
              </div>
            </button>

            <div class="card-actions">
              <button 
                @click.stop="openEditModal(animal)"
                class="action-btn edit"
                title="Edit Animal"
              >✏️ Edit</button>
              <button 
                @click.stop="openHeatModal" 
                class="action-btn heat"
                title="Record Heat"
              >💉 Heat</button>
              <button 
                @click.stop="openBreedingModal(animal.animalId, animal.barnName || animal.registeredName || `#${animal.animalId}`)"
                class="action-btn breed"
                title="Record Breeding"
              >🐂 Breed</button>
              <button 
                @click.stop="openCalvingModal(animal.animalId, animal.barnName || animal.registeredName || `#${animal.animalId}`)"
                class="action-btn calving"
                title="Record Calving"
              >👶 Calving</button>
              <button 
                @click.stop="openNoteModal(animal.animalId, animal.barnName || animal.registeredName || `#${animal.animalId}`)"
                class="action-btn note"
                title="Add Note"
              >📝 Note</button>
            </div>
          </div>
        </div>

        <div v-else class="empty-state">
          <p>🔎 No animals match "{{ searchQuery }}" — try searching by name or sire</p>
        </div>
      </section>

      <!-- Modals -->
      <RecordHeatModal 
        ref="heatModalRef" 
        @record-heat="onRecordHeat"
        @go-to-breeding="goToBreedingTab"
      />
      <RecordBreedingModal 
        ref="breedingModalRef" 
        @record-breeding="onRecordBreeding"
      />
      <RecordCalvingModal 
        ref="calvingModalRef" 
        @record-calving="onRecordCalving"
      />
      <AddNoteModal 
        ref="noteModalRef" 
        @add-note="onAddNote"
      />
      <RecordLUTModal 
        ref="lutModalRef" 
        @record-lut="onRecordLUT"
      />
      <EditAnimalModal 
        ref="editAnimalModalRef"
        :animal="selectedAnimalForEdit"
        @saved="onAnimalEdited"
        @close="selectedAnimalForEdit = null"
      />
    </template>
  </main>
</template>

<style scoped>
:global(body) {
  font-family: 'Inter', 'Segoe UI', 'Arial', sans-serif;
}

.page {
  width: min(100%, 1240px);
  margin: 0 auto;
  padding: 24px 20px 56px;
}

.hero {
  position: relative;
  display: flex;
  align-items: stretch;
  min-height: 180px;
  margin-bottom: 16px;
  padding: 16px;
  border-radius: 22px;
  overflow: hidden;
  background-size: 110% auto;
  background-position: center;
  box-shadow: 0 18px 48px rgba(14, 24, 16, 0.18);
}

.hero-overlay {
  position: absolute;
  inset: 0;
  background:
    linear-gradient(120deg, rgba(5, 12, 9, 0.70), rgba(7, 14, 11, 0.38));
  pointer-events: none;
}

.hero-farm-watermark {
  position: absolute;
  inset: auto 24px 14px auto;
  width: min(280px, 28vw);
  max-width: 300px;
  opacity: 0.24;
  filter: saturate(0.75);
  pointer-events: none;
}

.hero-inner {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: 1.2fr 0.8fr;
  gap: 24px;
  width: 100%;
}

.hero-main,
.hero-side {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.hero-brand {
  max-width: 620px;
}

.hero-app-logo {
  display: block;
  width: min(300px, 100%);
  max-width: 100%;
  height: auto;
  margin-bottom: 0;
  filter: drop-shadow(0 10px 28px rgba(0, 0, 0, 0.38));
}

.hero-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 14px;
  margin-top: 24px;
}

.hero-stat {
  padding: 18px 20px;
  border: 1px solid rgba(255, 255, 255, 0.28);
  border-radius: 18px;
  background: rgba(7, 14, 11, 0.38);
  backdrop-filter: blur(8px);
  color: #fff;
  min-height: 100px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.hero-stat-value {
  display: block;
  font-size: 2.1rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  line-height: 1.1;
}

.hero-stat-label {
  display: block;
  margin-top: 4px;
  font-size: 0.95rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.hero-stat small {
  display: block;
  margin-top: 2px;
  color: rgba(255, 255, 255, 0.78);
}

.hero-side {
  align-items: flex-end;
}

.hero-farm-name {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  border: 1px solid rgba(255, 255, 255, 0.28);
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.12);
  color: #fff;
  backdrop-filter: blur(10px);
}

.hero-farm-monogram {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 42px;
  height: 42px;
  border-radius: 50%;
  font-weight: 800;
  background: rgba(255, 255, 255, 0.25);
}

.hero-farm-name strong,
.hero-farm-name span {
  display: block;
}

.hero-farm-name span {
  color: rgba(255, 255, 255, 0.78);
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 18px;
}

.calendar-button,
.refresh-button {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 14px 18px;
  border: 1px solid rgba(0, 0, 0, 0.22);
  border-radius: 16px;
  background: linear-gradient(180deg, rgba(16, 28, 20, 0.96), rgba(9, 15, 11, 0.94));
  color: #fff;
  font-weight: 900;
  font-size: 1rem;
  cursor: pointer;
  box-shadow: 0 8px 18px rgba(6, 10, 8, 0.28);
}

.calendar-button {
  padding: 20px 28px;
  font-size: 1.2rem;
  gap: 14px;
}

.calendar-button:hover,
.refresh-button:hover {
  background: linear-gradient(180deg, rgba(24, 42, 29, 0.98), rgba(10, 18, 12, 0.94));
}

.refresh-button:disabled {
  opacity: 0.72;
  cursor: wait;
}

.hero-updated {
  width: 100%;
  text-align: right;
  color: rgba(255, 255, 255, 0.9);
  font-size: 0.95rem;
}

.card {
  padding: 16px 18px;
  border: 1px solid rgba(16, 40, 24, 0.08);
  border-radius: 18px;
  background: #fff;
  box-shadow: 0 8px 24px rgba(17, 33, 20, 0.05);
}

.error-card {
  color: #7a2020;
}

@media (max-width: 860px) {
  .hero-inner {
    grid-template-columns: 1fr;
  }

  .hero-side {
    align-items: stretch;
  }

  .hero-actions {
    justify-content: flex-start;
  }
}

@media (max-width: 640px) {
  .hero {
    min-height: 160px;
    padding: 14px;
    background-size: cover;
  }

  .hero-stats {
    grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
    gap: 12px;
  }

  .hero-stat {
    padding: 16px 14px;
    min-height: 90px;
  }

  .hero-stat-value {
    font-size: 1.8rem;
  }

  .hero-app-logo {
    width: min(280px, 100%);
  }
}
.herd-section {
  margin-top: 28px;
  padding: 24px;
  border-radius: 22px;
  background: white;
  box-shadow: 0 12px 32px rgba(15, 23, 42, 0.08);
}

.herd-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
}

.herd-header h2 {
  margin: 4px 0 0;
  color: #0f1f16;
  font-size: 1.6rem;
  font-weight: 900;
  letter-spacing: -0.02em;
}

.herd-subtitle {
  margin: 6px 0 0;
  color: #5d6f63;
  font-size: 0.9rem;
}

.herd-actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
  flex-wrap: wrap;
}

.search {
  display: flex;
  align-items: center;
  gap: 14px;
  flex: 1;
  min-width: 280px;
  padding: 16px 20px;
  border: 2px solid #d8dfd9;
  border-radius: 999px;
  background: #fafbfa;
  box-shadow: 0 4px 12px rgba(13, 30, 18, 0.05);
  transition: all 0.2s ease;
}

.search:focus-within {
  border-color: #75a17b;
  box-shadow: 0 6px 20px rgba(13, 30, 18, 0.1);
  background: #ffffff;
}

.search input {
  flex: 1;
  border: none;
  outline: none;
  background: transparent;
  font-size: 1rem;
  color: #0f1f16;
  font-weight: 500;
}

.search input::placeholder {
  color: #8a9b8e;
}

.add-animal-button {
  padding: 12px 18px;
  border: none;
  border-radius: 999px;
  background: #2d5228;
  color: white;
  font-weight: 900;
  letter-spacing: 0.03em;
  font-size: 0.95rem;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 4px 12px rgba(45, 82, 40, 0.15);
}

.add-animal-button:hover {
  background: #1f3a1c;
  box-shadow: 0 6px 16px rgba(45, 82, 40, 0.25);
}

.animal-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;
}

.animal-card {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 20px 22px;
  border: 1.5px solid #d8dfd9;
  border-radius: 18px;
  background: linear-gradient(180deg, #ffffff 0%, #f8fef9 100%);
  color: #0f1f16;
  text-align: left;
  cursor: pointer;
  box-shadow: 0 6px 18px rgba(13, 30, 18, 0.05);
  transition: all 0.2s ease;
}

.animal-card:hover {
  transform: translateY(-2px);
  border-color: #a0d2a5;
  background: linear-gradient(180deg, #f8fef9 0%, #eef7f1 100%);
  box-shadow: 0 10px 28px rgba(13, 30, 18, 0.1);
}

.animal-card-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
}

.animal-card strong {
  font-size: 1.25rem;
  font-weight: 900;
  letter-spacing: -0.015em;
  line-height: 1.2;
}

.animal-stage {
  color: #2d5228;
  font-size: 0.8rem;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  padding: 6px 10px;
  background: #e0f2e3;
  border-radius: 6px;
}

.animal-subtitle {
  margin: 2px 0 0;
  color: #5d6f63;
  font-size: 0.95rem;
  font-weight: 500;
}

.animal-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.animal-meta span {
  padding: 8px 12px;
  border-radius: 8px;
  background: #f0f7f1;
  color: #2d5228;
  font-size: 0.85rem;
  font-weight: 600;
  border: 1px solid #d8dfd9;
}

.animal-footer {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  margin-top: auto;
  color: #5d6f63;
  font-size: 0.9rem;
}

.empty-state {
  padding: 18px;
  border-radius: 16px;
  background: #f8fafc;
  color: #64748b;
  text-align: center;
}

@media (max-width: 720px) {
  .herd-section {
    padding: 20px;
  }

  .herd-header {
    flex-direction: column;
    align-items: stretch;
    margin-bottom: 18px;
  }

  .herd-header h2 {
    font-size: 1.35rem;
  }

  .herd-actions {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
    gap: 10px;
  }

  .search {
    min-width: 0;
    flex: 1;
    padding: 14px 16px;
    font-size: 1rem;
  }

  .search input {
    font-size: 1rem;
  }

  .add-animal-button {
    width: 100%;
    padding: 14px 16px;
    font-size: 1rem;
  }

  .animal-card {
    padding: 18px 16px;
    gap: 10px;
  }

  .animal-card strong {
    font-size: 1.1rem;
  }

  .animal-grid {
    gap: 12px;
  }

  .animal-meta span {
    padding: 6px 10px;
    font-size: 0.8rem;
  }
}

@media (max-width: 480px) {
  .herd-section {
    padding: 16px;
    margin-top: 20px;
  }

  .herd-header {
    margin-bottom: 14px;
  }

  .herd-header h2 {
    font-size: 1.2rem;
  }

  .herd-subtitle {
    font-size: 0.85rem;
  }

  .search {
    padding: 12px 14px;
    gap: 10px;
  }

  .search input {
    font-size: 0.95rem;
  }

  .add-animal-button {
    padding: 12px 14px;
    font-size: 0.9rem;
  }

  .animal-card {
    padding: 16px 14px;
  }

  .animal-card strong {
    font-size: 1rem;
  }

  .animal-stage {
    font-size: 0.75rem;
    padding: 4px 8px;
  }

  .animal-subtitle {
    font-size: 0.9rem;
  }

  .animal-meta {
    gap: 8px;
  }

  .animal-footer {
    font-size: 0.85rem;
  }
}

/* Quick Actions Bar */
.quick-actions-bar {
  display: flex;
  gap: 12px;
  margin: 16px 20px;
  padding: 16px;
  background: linear-gradient(135deg, #f0f8f3, #f8fbfa);
  border-radius: 12px;
  border: 2px solid #d0e8d8;
  flex-wrap: wrap;
}

.quick-btn {
  padding: 12px 20px;
  border: 2px solid #31572c;
  border-radius: 8px;
  background: white;
  color: #31572c;
  font-weight: 600;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.quick-btn:hover {
  background: #31572c;
  color: white;
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(49, 87, 44, 0.2);
}

.heat-btn:hover { background: #ff6b6b; border-color: #ff6b6b; color: white; }
.lut-btn:hover { background: #f59e0b; border-color: #f59e0b; color: white; }
.add-btn:hover { background: #10b981; border-color: #10b981; color: white; }

/* Enhanced Search Input */
.search-input-large {
  width: 100%;
  padding: 18px 24px;
  border: 2px solid #d8dfd9;
  border-radius: 12px;
  font-size: 1.1rem;
  font-weight: 500;
  color: #0f1f16;
  background: white;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.08);
  transition: all 0.2s ease;
}

.search-input-large:focus {
  outline: none;
  border-color: #31572c;
  box-shadow: 0 8px 24px rgba(49, 87, 44, 0.12);
  background: #f8fbfa;
}

.search-input-large::placeholder {
  color: #9ca8a0;
}

.filter-buttons {
  display: flex;
  gap: 10px;
  margin-top: 12px;
  flex-wrap: wrap;
}

.filter-btn {
  padding: 10px 16px;
  border: 2px solid #d8dfd9;
  border-radius: 8px;
  background: white;
  color: #0f1f16;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.filter-btn:hover {
  border-color: #31572c;
  background: #f8fbfa;
}

.filter-btn.active {
  border-color: #31572c;
  background: #31572c;
  color: white;
  box-shadow: 0 4px 12px rgba(49, 87, 44, 0.2);
}

/* Enhanced Animal Cards */
.animal-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
  margin-top: 16px;
}

.animal-card-enhanced {
  border: 2px solid #e5ebe8;
  border-radius: 12px;
  background: white;
  overflow: hidden;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
}

.animal-card-enhanced:hover {
  border-color: #31572c;
  box-shadow: 0 12px 32px rgba(49, 87, 44, 0.15);
  transform: translateY(-4px);
}

.card-main {
  flex: 1;
  padding: 18px;
  border: none;
  background: white;
  text-align: left;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.card-main:hover {
  background: #f8fbfa;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
}

.card-name strong {
  display: block;
  font-size: 1.35rem;
  font-weight: 700;
  color: #0f1f16;
  margin-bottom: 4px;
}

.card-stage {
  margin: 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: #31572c;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.card-status {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-size: 1.2rem;
  font-weight: bold;
}

.card-status.active {
  background: #d1fae5;
  color: #065f46;
}

.card-status.inactive {
  background: #fee2e2;
  color: #991b1b;
}

.card-details {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  padding: 12px;
  background: #f8fbfa;
  border-radius: 8px;
  font-size: 0.9rem;
}

.detail-row {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.detail-row .label {
  font-size: 0.75rem;
  font-weight: 600;
  color: #5d6f63;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.detail-row span:not(.label) {
  font-weight: 500;
  color: #1f3a25;
}

.reg-num {
  font-family: 'Courier New', monospace;
  font-size: 0.85rem;
}

.card-actions {
  display: flex;
  gap: 8px;
  padding: 12px 18px;
  background: #f8fbfa;
  border-top: 1px solid #e5ebe8;
}

.action-btn {
  flex: 1;
  padding: 10px;
  border: 2px solid #e5ebe8;
  border-radius: 8px;
  background: white;
  font-size: 1.2rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.action-btn:hover {
  border-color: #31572c;
  background: #f0f8f3;
  transform: scale(1.1);
}

.action-btn.heat:hover { background: #ffebee; border-color: #ff6b6b; }
.action-btn.breed:hover { background: #e3f2fd; border-color: #2196f3; }
.action-btn.calving:hover { background: #fce4ec; border-color: #e91e63; }
.action-btn.note:hover { background: #fff3e0; border-color: #ff9800; }

.empty-state {
  text-align: center;
  padding: 48px 24px;
  color: #5d6f63;
  font-size: 1.1rem;
}
</style>