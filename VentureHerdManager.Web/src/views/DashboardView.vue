<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import { getAnimals } from '../api/animals'
import { getAppearance, type AppearanceSetting } from '../api/appearance'
import { recordHeat } from '../api/heat'
import { getLatestPregnancyStatuses, recordBreeding } from '../api/breeding'
import { assignEmbryo, implantEmbryo } from '../api/embryoRecords'
import { recordCalving } from '../api/calving'
import { addNote } from '../api/notes'
import { recordLUT } from '../api/lut'
import type { Animal } from '../models/Animal'
import { formatCurrentAge, getShowClassLabel } from '../utils/showClasses'
import { animalDisplayName } from '../utils/animalDisplay'
import DashboardSummary from '../components/DashboardSummary.vue'
import RecordHeatModal from '../components/RecordHeatModal.vue'
import RecordBreedingModal from '../components/RecordBreedingModal.vue'
import RecordCalvingModal from '../components/RecordCalvingModal.vue'
import AddNoteModal from '../components/AddNoteModal.vue'
import RecordLUTModal from '../components/RecordLUTModal.vue'
import EditAnimalModal from '../components/EditAnimalModal.vue'
import HerdLoadingScene from '../components/HerdLoadingScene.vue'
import RetroIcon from '../components/RetroIcon.vue'

const router = useRouter()

const animals = ref<Animal[]>([])
const appearance = ref<AppearanceSetting | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const warningMessage = ref('')
const refreshing = ref(false)
const dashboardRefreshKey = ref(0)
const lastUpdatedAt = ref<string | null>(null)

const DASHBOARD_CACHE_KEY = 'venture-herd-dashboard-cache-v1'
const dashboardStorage =
  import.meta.env.VITE_DEMO_ONLY === 'true'
    ? sessionStorage
    : localStorage

interface DashboardCachePayload {
  savedAt: string
  animals: Animal[]
  latestPregnancyStatuses: Record<number, number>
}

const searchQuery = ref('')
const stageFilter = ref<number | null>(null)
const statusFilter = ref<number | null>(0)
const pregnancyFilter = ref<number | null>(null)
const favoriteOnly = ref(false)
const latestPregnancyStatuses = ref<Record<number, number>>({})
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

function dashboardAnimalName(animal: Animal): string {
  return animalDisplayName(animal)
}

const filteredAnimals = computed(() => {
  let result = animals.value

  // Apply stage filter
  if (stageFilter.value !== null) {
    result = result.filter(animal => animal.animalStage === stageFilter.value)
  }

  if (statusFilter.value !== null) {
    result = result.filter(animal => (animal.animalStatus ?? 0) === statusFilter.value)
  }

  if (favoriteOnly.value) {
    result = result.filter(animal => !!animal.isFavorite)
  }

  if (pregnancyFilter.value !== null) {
    result = result.filter(animal => {
      const status = latestPregnancyStatuses.value[animal.animalId]
      return status === pregnancyFilter.value
    })
  }

  // Apply search query
  const query = searchQuery.value.trim()
  if (query) {
    result = result.filter(animal => {
      // Check barn name, registered name, registration number
      if (fuzzyMatch(query, animal.barnName || '')) return true
      if (fuzzyMatch(query, animal.registeredName || '')) return true
      if (fuzzyMatch(query, animal.registrationNumber || '')) return true
      // Check sire/dam/breed
      if (fuzzyMatch(query, animal.sireName || '')) return true
      if (fuzzyMatch(query, animal.damName || '')) return true
      if (fuzzyMatch(query, dashboardAnimalName(animal))) return true
      if (fuzzyMatch(query, animal.breed || '')) return true
      return false
    })
  }

  return [...result].sort(
    (a, b) => Number(!!b.isFavorite) - Number(!!a.isFavorite)
  )
})

const formattedLastUpdated = computed(() => {
  if (!lastUpdatedAt.value) {
    return null
  }

  const parsed = new Date(lastUpdatedAt.value)
  if (Number.isNaN(parsed.getTime())) {
    return null
  }

  return parsed.toLocaleTimeString([], {
    hour: 'numeric',
    minute: '2-digit'
  })
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
  loading.value = animals.value.length === 0
  errorMessage.value = ''
  warningMessage.value = ''

  try {
    // The animal list is the required dashboard request. Appearance and
    // pregnancy status are enhancements and should not make live herd data
    // appear stale when either optional request is temporarily unavailable.
    const animalsResponse = await getAnimals()
    animals.value = Array.isArray(animalsResponse) ? animalsResponse : []
    lastUpdatedAt.value = new Date().toISOString()

    const [appearanceResponse, latestStatuses] = await Promise.all([
      getAppearance().catch((error) => {
        console.warn('Appearance settings are temporarily unavailable:', error)
        return undefined
      }),
      getLatestPregnancyStatuses().catch((error) => {
        console.warn('Pregnancy statuses are temporarily unavailable:', error)
        return {}
      })
    ])

    if (appearanceResponse) {
      appearance.value = appearanceResponse
    }
    latestPregnancyStatuses.value = latestStatuses

    const payload: DashboardCachePayload = {
      savedAt: lastUpdatedAt.value,
      animals: animals.value,
      latestPregnancyStatuses: latestPregnancyStatuses.value
    }

    dashboardStorage.setItem(DASHBOARD_CACHE_KEY, JSON.stringify(payload))
  } catch (error) {
    console.error('Failed to load dashboard information:', error)

    if (animals.value.length > 0) {
      warningMessage.value = 'Live refresh failed. Showing last loaded herd data.'
    } else {
      errorMessage.value =
        error instanceof Error
          ? error.message
          : 'The dashboard could not be loaded.'
    }
  } finally {
    loading.value = false
    refreshing.value = false
  }
}

function loadDashboardCache() {
  const cached = dashboardStorage.getItem(DASHBOARD_CACHE_KEY)
  if (!cached) {
    return
  }

  try {
    const payload = JSON.parse(cached) as DashboardCachePayload
    if (Array.isArray(payload.animals)) {
      animals.value = payload.animals
    }
    if (payload.latestPregnancyStatuses && typeof payload.latestPregnancyStatuses === 'object') {
      latestPregnancyStatuses.value = payload.latestPregnancyStatuses
    }
    if (payload.savedAt) {
      lastUpdatedAt.value = payload.savedAt
    }
  } catch (error) {
    console.warn('Invalid dashboard cache payload:', error)
  }
}

function openCalendar() {
  router.push('/calendar')
}

function openReports() {
  router.push('/reports')
}

async function refreshDashboard() {
  refreshing.value = true
  await loadAnimals()
  dashboardRefreshKey.value += 1
}

// Modal event handlers
const openHeatModal = () => {
  heatModalRef.value?.openModal()
}
const openBreedingModal = (id: number, name: string) => breedingModalRef.value?.openModal(id, name)
const openCalvingModal = (id: number, name: string) => calvingModalRef.value?.openModal(id, name)
const openNoteModal = (id: number, name: string) => noteModalRef.value?.openModal(id, name)
const openLUTModal = (id?: number, name?: string) => {
  lutModalRef.value?.openModal(id, name)
}

const openAddAnimal = () => {
  router.push('/animals/new')
}

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
    if (data.hasEmbryoTransfer && data.embryoRecordId) {
      try {
        await assignEmbryo(data.embryoRecordId, data.animalId)
      } catch (error) {
        console.warn('Heat saved, but the embryo could not be reserved:', error)
        alert('Heat was saved, but the embryo could not be reserved. Select it again when recording the transfer.')
      }
    }
    data.complete?.(true)
    alert('Heat event recorded successfully!')
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Unknown error'
    data.complete?.(false, message)
    alert(`Error recording heat: ${message}`)
    console.error('Failed to record heat:', error)
    return
  }

  try {
    await refreshDashboard()
  } catch (error) {
    console.warn('Heat saved, but the dashboard could not be refreshed:', error)
  }
}

// Handle breeding recording
const onRecordBreeding = async (data: any) => {
  try {
    if (data.breedingType === 2 && data.embryoRecordId) {
      await implantEmbryo(
        data.embryoRecordId,
        data.animalId,
        data.breedingDate
      )
    } else {
      await recordBreeding({
        animalId: data.animalId,
        breedingDate: data.breedingDate,
        sireUsed: data.sireUsed,
        breedingType: data.breedingType,
        pregnancyStatus: data.pregnancyStatus,
        notes: data.notes
      })
    }
    
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
      calfSireName: data.calfSireName,
      calfDamName: data.calfDamName,
      calfSex: data.calfSex,
      birthWeight: data.birthWeight,
      calvingEase: data.calvingEase,
      twins: data.twins,
      stillborn: data.stillborn,
      notes: data.notes
    })
    data.complete?.(true)
    alert('Calving event recorded successfully! Cow moved to Milking status.')
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Unknown error'
    data.complete?.(false, message)
    alert(`Error recording calving: ${message}`)
    console.error('Failed to record calving:', error)
    return
  }

  try {
    await refreshDashboard()
  } catch (error) {
    console.warn('Calving saved, but the dashboard could not be refreshed:', error)
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
const openEditModal = async (animal: Animal) => {
  selectedAnimalForEdit.value = animal
  await nextTick()
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
  loadDashboardCache()
  loadAnimals()
})
</script>

<template>
  <main class="page">
    <header
      class="hero"
      :style="{
        backgroundImage: `url('${appearance?.backgroundImageUrl || '/Seashell_cow.jpg'}')`
      }"
    >
      <div class="hero-overlay" />

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

            <span class="hero-updated">
              Powered by Venture Ag Marketing
            </span>
          </div>

          <p
            v-if="formattedLastUpdated"
            class="hero-powered"
          >
            Last updated {{ formattedLastUpdated }}
          </p>
        </div>
      </div>
    </header>

    <section
      v-if="loading"
      class="card dashboard-loader"
    >
      <HerdLoadingScene message="Opening your herd..." />
    </section>

    <section
      v-else-if="errorMessage"
      class="card error-card"
    >
      <strong>Unable to load dashboard</strong>
      <p>{{ errorMessage }}</p>
    </section>

    <template v-else>
      <section v-if="warningMessage" class="card warning-card">
        <strong>Using cached data</strong>
        <p>{{ warningMessage }}</p>
      </section>

      <section class="quick-actions-bar">
        <button @click="openHeatModal" class="quick-btn heat-btn"><RetroIcon name="heat" :size="28" /><span>Record Heat</span></button>
        <button @click="openLUTModal()" class="quick-btn lut-btn"><RetroIcon name="lut" :size="28" /><span>LUT Injection</span></button>
        <button @click="router.push('/reports?tab=embryos')" class="quick-btn embryo-btn"><RetroIcon name="embryo" :size="28" /><span>Embryo Inventory</span></button>
        <button @click="openReports" class="quick-btn report-btn"><RetroIcon name="reports" :size="28" /><span>Reports</span></button>
      </section>

      <DashboardSummary :key="dashboardRefreshKey" :animals="animals" />

      <section class="herd-section">
        <div class="herd-header">
          <div>
            <p class="eyebrow">HERD</p>
            <h2>Animals</h2>
            <p class="herd-subtitle">Search by name, sire, dam, breed, or registration • Click to manage</p>
          </div>

          <div class="herd-actions">
            <button class="add-animal-inline" type="button" @click="openAddAnimal">➕ Add New Animal</button>

            <div class="filter-row">
              <label>
                Stage
                <select v-model.number="stageFilter">
                  <option :value="null">All stages</option>
                  <option :value="1">Calf</option>
                  <option :value="2">Heifer</option>
                  <option :value="3">Milking</option>
                  <option :value="4">Dry</option>
                  <option :value="5">Bull</option>
                </select>
              </label>

              <label>
                Status
                <select v-model.number="statusFilter">
                  <option :value="null">All statuses</option>
                  <option :value="0">Active</option>
                  <option :value="1">Sold</option>
                  <option :value="2">Deceased</option>
                </select>
              </label>

              <label>
                Pregnancy
                <select v-model.number="pregnancyFilter">
                  <option :value="null">Any</option>
                  <option :value="1">Pregnant</option>
                  <option :value="0">Unconfirmed</option>
                  <option :value="2">Open</option>
                  <option :value="3">Recheck</option>
                  <option :value="4">Aborted</option>
                </select>
              </label>

              <label class="favorite-toggle">
                <input v-model="favoriteOnly" type="checkbox">
                Favorites only
              </label>
            </div>

            <input
              v-model="searchQuery"
              type="search"
              class="search-input-large"
              placeholder="🔎 Search name, sire, dam, breed, or registration..."
            >
          </div>
        </div>

        <div v-if="filteredAnimals.length" class="animal-grid">
          <div
            v-for="animal in filteredAnimals"
            :key="animal.animalId"
            class="player-card"
            :class="{ 'card-favorite': animal.isFavorite }"
          >
            <!-- Card top banner -->
            <div class="player-card-banner">
              <div class="banner-left">
                <span class="card-badge" :class="`badge-stage-${animal.animalStage}`">{{ getStageLabel(animal.animalStage) }}</span>
                <span v-if="animal.isFavorite" class="fav-star" title="Favorite">★</span>
              </div>
              <span class="banner-reg">{{ animal.registrationNumber || '' }}</span>
            </div>

            <!-- Main clickable area -->
            <button class="player-card-body" type="button" @click="router.push(`/animals/${animal.animalId}`)">
              <div class="player-name">
                {{ dashboardAnimalName(animal) }}
              </div>

              <div class="player-meta">
                <span>{{ animal.breed || 'Unknown' }}</span>
                <span v-if="animal.sireName" class="meta-sep">·</span>
                <span v-if="animal.sireName" class="player-sire">
                  <span class="player-sire-label">Sire:</span>
                  {{ animal.sireName }}
                </span>
              </div>

              <!-- Stat row -->
              <div class="stat-rail">
                <div class="stat-cell" title="Show Age">
                  <span class="stat-val">{{ formatCurrentAge(animal.birthDate) }}</span>
                  <span class="stat-lbl">Age</span>
                </div>
                <div class="stat-cell" title="Show Class">
                  <span class="stat-val show-class-val">{{ getShowClassLabel(animal.birthDate, animal.animalStage) }}</span>
                  <span class="stat-lbl">Show Class</span>
                </div>
                <div class="stat-cell" v-if="animal.latestScore">
                  <span class="stat-val score-val">{{ getScoreLabel(animal.latestScore) }}</span>
                  <span class="stat-lbl">Score</span>
                </div>
                <div class="stat-cell" v-if="animal.currentLactation">
                  <span class="stat-val">{{ animal.currentLactation }}</span>
                  <span class="stat-lbl">Lac</span>
                </div>
              </div>
            </button>

            <!-- Action row -->
            <div class="player-card-actions">
              <button @click.stop="openHeatModal" class="pca-btn pca-heat" title="Record Heat"><RetroIcon name="heat" :size="20" /><span>Heat</span></button>
              <button @click.stop="openBreedingModal(animal.animalId, dashboardAnimalName(animal))" class="pca-btn pca-breed" title="Record Breeding"><RetroIcon name="embryo" :size="20" /><span>Breed</span></button>
              <button @click.stop="openEditModal(animal)" class="pca-btn pca-edit" title="Edit"><RetroIcon name="note" :size="20" /><span>Edit</span></button>
              <button @click.stop="router.push(`/animals/${animal.animalId}`)" class="pca-btn pca-open" title="Open"><RetroIcon name="calf" :size="20" /><span>Open</span></button>
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
  font-family: 'Bahnschrift', 'Arial Narrow', 'Segoe UI', sans-serif;
}

.page {
  width: min(100%, 1240px);
  margin: 0 auto;
  padding: 14px 16px 44px;
}

.hero {
  position: relative;
  display: flex;
  align-items: stretch;
  min-height: 96px;
  margin-bottom: 10px;
  padding: 8px;
  border-radius: 12px;
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
  z-index: 1;
}

.hero-inner {
  position: relative;
  z-index: 3;
  display: grid;
  grid-template-columns: 1.2fr 0.8fr;
  gap: 10px;
  width: 100%;
}

.hero-main,
.hero-side {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.hero-brand {
  display: flex;
  justify-content: center;
  width: 100%;
  max-width: 620px;
}

.hero-app-logo {
  display: block;
  width: min(420px, 100%);
  max-width: 100%;
  height: auto;
  margin: 0 auto;
  margin-bottom: 0;
  filter: drop-shadow(0 4px 10px rgba(0, 0, 0, 0.26));
}

.hero-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 8px;
  margin-top: 8px;
}

.hero-stat {
  padding: 10px 12px;
  border: 1px solid rgba(255, 255, 255, 0.34);
  border-radius: 8px;
  background: rgba(7, 14, 11, 0.38);
  backdrop-filter: blur(8px);
  color: #fff;
  min-height: 64px;
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
  margin-top: 10px;
}

.calendar-button,
.refresh-button {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 14px 18px;
  border: 1px solid rgba(0, 0, 0, 0.22);
  border-radius: 8px;
  background: linear-gradient(180deg, rgba(16, 28, 20, 0.96), rgba(9, 15, 11, 0.94));
  color: #fff;
  font-weight: 900;
  font-size: 1rem;
  cursor: pointer;
  box-shadow: 0 8px 18px rgba(6, 10, 8, 0.28);
  letter-spacing: 0.02em;
}

.hero-powered {
  margin-top: 12px;
  font-size: 0.75rem;
  color: rgba(255, 255, 255, 0.48);
  font-weight: 500;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  text-align: center;
}

.calendar-button {
  padding: 14px 18px;
  font-size: 1.05rem;
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
  border-radius: 10px;
  background: #fff;
  box-shadow: 0 8px 24px rgba(17, 33, 20, 0.05);
}

.dashboard-loader {
  min-height: 150px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;
  color: #18311f;
  text-align: left;
}

.dashboard-loader h2,
.dashboard-loader p {
  margin: 0;
}

.loader-kicker {
  color: #6b7c6d;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.13em;
}

.dashboard-loader h2 {
  margin-top: 4px;
  font-family: Georgia, 'Times New Roman', serif;
  font-size: clamp(1.55rem, 5vw, 2.1rem);
}

.loader-copy {
  margin-top: 6px !important;
  color: #667369;
  font-size: 0.82rem;
}

.loader-mark {
  display: flex;
  align-items: end;
  gap: 4px;
  width: 38px;
  height: 38px;
  padding: 8px;
  border-radius: 50%;
  background: #e7efe8;
}

.loader-mark span {
  width: 5px;
  border-radius: 999px;
  background: #31572c;
  animation: herd-loading 0.9s ease-in-out infinite alternate;
}

.loader-mark span:nth-child(1) { height: 9px; }
.loader-mark span:nth-child(2) { height: 18px; animation-delay: 0.15s; }
.loader-mark span:nth-child(3) { height: 13px; animation-delay: 0.3s; }

@keyframes herd-loading {
  to { height: 24px; opacity: 0.55; }
}

.error-card {
  color: #7a2020;
}

.warning-card {
  color: #6b4f00;
  background: #fff9e6;
  border-color: #f3d98a;
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

  .hero-powered {
    font-size: 0.7rem;
  }
}

@media (max-width: 640px) {
  .hero {
    min-height: 76px;
    padding: 6px;
    background-size: cover;
    background-position: center center;
  }

  .hero-inner {
    gap: 8px;
  }

  .hero-main,
  .hero-side,
  .hero-brand {
    align-items: center;
    text-align: center;
  }

  .hero-main,
  .hero-side {
    width: 100%;
  }

  .hero-actions {
    width: 100%;
    justify-content: center;
  }

  .hero-updated {
    text-align: center;
    font-size: 0.85rem;
  }

  .hero-powered {
    font-size: 0.65rem;
    margin-top: 6px;
  }

  .hero-stats {
    width: 100%;
    max-width: 420px;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 6px;
  }

  .hero-stat {
    padding: 8px 7px;
    min-height: 50px;
  }

  .hero-stat-value {
    font-size: 1.42rem;
  }

  .hero-app-logo {
    width: min(380px, 98vw);
  }
}
.herd-section {
  margin-top: 14px;
  padding: 20px;
  border-radius: 10px;
  border: 1px solid #d8e0db;
  background: white;
  box-shadow: 0 12px 32px rgba(15, 23, 42, 0.08);
}

.herd-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 14px;
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
  gap: 8px;
  margin: 4px 0 8px;
  padding: 10px;
  background: linear-gradient(165deg, #f6faf7, #eef5f0);
  border-radius: 8px;
  border: 1px solid #d2ddd5;
  border-top: 3px solid #244f2f;
  flex-wrap: wrap;
}

.quick-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 9px;
  flex: 1 1 180px;
  min-height: 54px;
  padding: 10px 14px;
  border: 1px solid #31572c;
  border-bottom: 3px solid #244f2f;
  border-radius: 6px;
  background: white;
  color: #31572c;
  font-weight: 700;
  font-size: 0.92rem;
  letter-spacing: 0.01em;
  cursor: pointer;
  transition: all 0.2s ease;
}

.quick-btn:hover {
  background: #31572c;
  color: white;
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(49, 87, 44, 0.2);
}

.quick-btn > span {
  display: inline-block;
  line-height: 1.18;
  text-align: left;
}

.heat-btn:hover { background: #ff6b6b; border-color: #ff6b6b; color: white; }
.lut-btn:hover { background: #2563eb; border-color: #2563eb; color: white; }
.embryo-btn:hover { background: #7c3aed; border-color: #7c3aed; color: white; }
.report-btn:hover { background: #0284c7; border-color: #0284c7; color: white; }
.add-btn:hover { background: #10b981; border-color: #10b981; color: white; }

.add-animal-inline {
  border: 1px solid #c8d4cb;
  background: white;
  color: #31572c;
  border-radius: 8px;
  min-height: 44px;
  padding: 0 16px;
  font-weight: 800;
  font-size: 0.95rem;
  cursor: pointer;
  align-self: start;
}

.add-animal-inline:hover {
  background: #f0f7f1;
  border-color: #31572c;
}

@media (max-width: 640px) {
  .quick-actions-bar {
    position: sticky;
    top: 4px;
    z-index: 20;
    margin: 2px 0 8px;
    padding: 8px;
    gap: 6px;
    border-width: 1px;
    box-shadow: 0 8px 20px rgba(15, 23, 42, 0.12);
  }

  .quick-btn {
    flex: 1 1 calc(50% - 8px);
    min-height: 52px;
    padding: 8px 12px;
    font-size: 0.84rem;
  }

}

@media (max-width: 480px) {
  .page {
    padding: 10px 10px 24px;
  }

  .quick-actions-bar {
    top: 2px;
  }

  .quick-btn { flex: 1 1 calc(50% - 8px); }
}

/* Enhanced Search Input */
.search-input-large {
  width: 100%;
  padding: 18px 24px;
  border: 1px solid #c8d4cb;
  border-radius: 8px;
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

.filter-row {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
}

.filter-row label {
  display: grid;
  gap: 4px;
  font-size: 0.85rem;
  font-weight: 800;
  color: #1f3a25;
}

.filter-row select {
  min-height: 44px;
  border: 1px solid #c8d4cb;
  border-radius: 8px;
  padding: 8px 10px;
  font-size: 1rem;
  background: #fff;
  color: #1f2937;
}

.favorite-toggle {
  align-self: end;
  display: flex !important;
  align-items: center;
  gap: 8px;
  min-height: 44px;
  border: 1px solid #c8d4cb;
  border-radius: 8px;
  padding: 0 10px;
  background: #fff;
  color: #1f2937;
  font-size: 0.85rem;
  font-weight: 700;
}

.favorite-toggle input {
  width: 18px;
  height: 18px;
}

/* ── Sporty Player Cards ── */
.animal-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 14px;
  margin-top: 16px;
}

.player-card {
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1.5px solid #d8dfd9;
  border-radius: 10px;
  overflow: hidden;
  transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s;
}

.player-card:hover {
  transform: translateY(-3px);
  border-color: #31572c;
  box-shadow: 0 12px 28px rgba(49, 87, 44, 0.14);
}

.card-favorite {
  border-color: #d97706;
  border-left: 3px solid #d97706;
}

.player-card-banner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 12px;
  background: #f4f7f4;
  border-bottom: 1px solid #e0e8e1;
}

.banner-left {
  display: flex;
  align-items: center;
  gap: 6px;
}

.banner-reg {
  color: #8a9b8e;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.06em;
}

.fav-star {
  color: #d97706;
  font-size: 0.95rem;
}

.card-badge {
  border-radius: 4px;
  padding: 2px 8px;
  font-size: 0.7rem;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.badge-stage-1 { background: #dbeafe; color: #1d4ed8; }
.badge-stage-2 { background: #ede9fe; color: #6d28d9; }
.badge-stage-3 { background: #dcfce7; color: #15803d; }
.badge-stage-4 { background: #d1fae5; color: #065f46; }
.badge-stage-5 { background: #ffedd5; color: #c2410c; }

.player-card-body {
  flex: 1;
  padding: 14px 14px 10px;
  background: transparent;
  border: none;
  text-align: left;
  cursor: pointer;
  color: inherit;
}

.player-card-body:hover .player-name {
  color: #31572c;
}

.player-name {
  font-size: 1.2rem;
  font-weight: 900;
  color: #0f1f16;
  letter-spacing: -0.02em;
  line-height: 1.2;
  transition: color 0.15s;
}

.player-meta {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
  margin-top: 4px;
  color: #5d6f63;
  font-size: 0.82rem;
  font-weight: 600;
}

.meta-sep {
  color: #c8d4cc;
}

.player-sire {
  color: #6b7c70;
  font-size: 0.76rem;
  font-weight: 500;
}

.player-sire-label {
  color: #405b48;
  font-weight: 700;
}

/* Stat rail */
.stat-rail {
  display: flex;
  gap: 0;
  margin-top: 12px;
  border-top: 1px solid #e8efe9;
  border-bottom: 1px solid #e8efe9;
}

.stat-cell {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 8px 4px 6px;
  border-right: 1px solid #e8efe9;
}

.stat-cell:last-child {
  border-right: none;
}

.stat-val {
  font-size: 0.92rem;
  font-weight: 900;
  color: #1a3520;
  line-height: 1.2;
  text-align: center;
}

.show-class-val {
  font-size: 0.7rem;
}

.score-val {
  color: #31572c;
}

.stat-lbl {
  font-size: 0.64rem;
  font-weight: 700;
  color: #8a9b8e;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  margin-top: 2px;
}

/* Card action row */
.player-card-actions {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr 1.2fr;
  border-top: 1px solid #e8efe9;
  background: #f8fbf8;
}

.pca-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  border: none;
  border-right: 1px solid #e0e8e1;
  background: transparent;
  color: #5d7a68;
  font-weight: 800;
  font-size: 0.75rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  padding: 10px 6px;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}

.pca-btn:last-child {
  border-right: none;
}

.pca-btn:hover { color: #0f1f16; background: #eef5f0; }
.pca-heat:hover { color: #991b1b; background: #fff1f2; }
.pca-breed:hover { color: #4c1d95; background: #f5f3ff; }
.pca-edit:hover { color: #92400e; background: #fffbeb; }
.pca-open {
  color: #31572c;
  font-weight: 900;
}
.pca-open:hover { background: #dcfce7; }

/* Backwards-compat stub so old card-name strong doesn't conflict */
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
  border-radius: 6px;
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
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 8px;
  padding: 12px 18px;
  background: #f8fbfa;
  border-top: 1px solid #e5ebe8;
}

.action-btn {
  flex: 1;
  padding: 10px;
  border: 2px solid #e5ebe8;
  border-radius: 6px;
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

@media (max-width: 720px) {
  .filter-row {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .player-card-actions {
    grid-template-columns: repeat(4, 1fr);
  }
}

@media (max-width: 480px) {
  .filter-row {
    grid-template-columns: 1fr;
  }

  .player-card-actions {
    grid-template-columns: repeat(2, 1fr);
  }

  .pca-btn {
    border-bottom: 1px solid #1e2a30;
  }
}
</style>
