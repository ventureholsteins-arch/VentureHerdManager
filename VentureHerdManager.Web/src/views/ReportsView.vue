<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { getAnimals } from '../api/animals'
import { getHerdActivity, getEmbryoImplants, type HerdActivityResponse, type EmbryoImplantResponse } from '../api/analytics'
import {
  createEmbryo,
  deleteEmbryo,
  getAllEmbryos,
  getRecentHeatRecipients,
  implantEmbryo,
  recordEmbryoOutcome,
  updateEmbryo,
  EMBRYO_STATUS_LABELS,
  type EmbryoRecord,
  type RecentHeatRecipient
} from '../api/embryoRecords'
import type { Animal } from '../models/Animal'
import { formatCurrentAge, getShowClassLabel } from '../utils/showClasses'

type HubTab = 'analytics' | 'embryos' | 'embryoImplants' | 'showString' | 'lists' | 'checklist' | 'achievements'

interface AnimalGroupList {
  key: string
  title: string
  animalIds: number[]
  notes: string
  searchQuery: string
}

interface ShowStringRow {
  id: number
  animalId: number | null
  lineupOrder: number
  feedNotes: string
  feedRation: string
  ringDirections: string
}

interface ChecklistItem {
  id: number
  text: string
  done: boolean
}

interface AchievementRecord {
  id: number
  animalId: number | null
  showName: string
  showDate: string
  bagged: string
  placed: string
  notes: string
}

const router = useRouter()
const route = useRoute()
const activeTab = ref<HubTab>('embryos')
const loading = ref(true)
const animals = ref<Animal[]>([])

const analyticsData = ref<HerdActivityResponse | null>(null)
const analyticsLoading = ref(false)
const analyticsError = ref('')

async function loadAnalytics() {
  analyticsLoading.value = true
  analyticsError.value = ''
  try {
    analyticsData.value = await getHerdActivity(12)
  } catch (e) {
    analyticsError.value = 'Could not load analytics. Make sure the API is reachable.'
  } finally {
    analyticsLoading.value = false
  }
}

const embryoImplantsData = ref<EmbryoImplantResponse | null>(null)
const embryoImplantsLoading = ref(false)
const embryoImplantsError = ref('')

async function loadEmbryoImplants() {
  embryoImplantsLoading.value = true
  embryoImplantsError.value = ''
  try {
    embryoImplantsData.value = await getEmbryoImplants(12)
  } catch (e) {
    embryoImplantsError.value = 'Could not load embryo implant data. Make sure the API is reachable.'
  } finally {
    embryoImplantsLoading.value = false
  }
}

const showStringClassFilter = ref<string>('all')
const showStringSearch = ref('')

const listKey = 'venture-herd-lists-v2'
const showStringKey = 'venture-herd-show-string-v2'
const checklistKey = 'venture-herd-checklist-v1'
const achievementsKey = 'venture-herd-achievements-v1'

const defaultLists: AnimalGroupList[] = [
  { key: 'show-string', title: 'Show String', animalIds: [], notes: '', searchQuery: '' },
  { key: 'flush-candidates', title: 'Flush Candidates', animalIds: [], notes: '', searchQuery: '' },
  { key: 'donor-cows', title: 'Donor Cows', animalIds: [], notes: '', searchQuery: '' },
  { key: 'recipients', title: 'Recipients', animalIds: [], notes: '', searchQuery: '' },
  { key: 'sale-animals', title: 'Sale Animals', animalIds: [], notes: '', searchQuery: '' },
  { key: 'vet-check', title: 'Vet Check', animalIds: [], notes: '', searchQuery: '' },
]

const defaultChecklist: ChecklistItem[] = [
  { id: 1, text: 'Show halters', done: false },
  { id: 2, text: 'Feed buckets & water tubs', done: false },
  { id: 3, text: 'Bedding and straw', done: false },
  { id: 4, text: 'Paperwork and registrations', done: false },
  { id: 5, text: 'Clippers, blades, adhesives', done: false },
  { id: 6, text: 'Treatments and first aid', done: false },
]

const groupLists = ref<AnimalGroupList[]>(defaultLists.map(l => ({ ...l })))
const showStringRows = ref<ShowStringRow[]>([])
const checklistItems = ref<ChecklistItem[]>(defaultChecklist.map(i => ({ ...i })))
const embryoRecords = ref<EmbryoRecord[]>([])
const recentHeatRecipients = ref<RecentHeatRecipient[]>([])
const recipientSearch = ref<Record<number, string>>({})
const embryoMessage = ref('')
const embryoSavingId = ref<number | null>(null)
const achievements = ref<AchievementRecord[]>([])

const nextRowId = ref(1)
const nextAchievementId = ref(1)

function parseStored<T>(k: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(k)
    return raw ? (JSON.parse(raw) as T) : fallback
  } catch {
    return fallback
  }
}

function loadData() {
  groupLists.value = parseStored<AnimalGroupList[]>(listKey, defaultLists.map(l => ({ ...l }))).map(l => ({ searchQuery: '', ...l }))
  showStringRows.value = parseStored<ShowStringRow[]>(showStringKey, []).map(r => ({ feedRation: '', ...r }))
  checklistItems.value = parseStored<ChecklistItem[]>(checklistKey, defaultChecklist.map(i => ({ ...i })))
  achievements.value = parseStored<AchievementRecord[]>(achievementsKey, [])
  nextRowId.value = Math.max(1, ...showStringRows.value.map(r => r.id + 1), 1)
  nextAchievementId.value = Math.max(1, ...achievements.value.map(a => a.id + 1), 1)
}

function saveData() {
  localStorage.setItem(listKey, JSON.stringify(groupLists.value))
  localStorage.setItem(showStringKey, JSON.stringify(showStringRows.value))
  localStorage.setItem(checklistKey, JSON.stringify(checklistItems.value))
  localStorage.setItem(achievementsKey, JSON.stringify(achievements.value))
}

watch([groupLists, showStringRows, checklistItems, achievements], saveData, { deep: true })

const animalOptions = computed(() =>
  [...animals.value].sort((a, b) =>
    (a.barnName || a.registeredName || '').localeCompare(b.barnName || b.registeredName || '')
  )
)

const showClassOptions = computed(() => {
  const set = new Set<string>()
  for (const a of animals.value) {
    const c = getShowClassLabel(a.birthDate, a.animalStage)
    if (c && c !== 'Class TBD') set.add(c)
  }
  return Array.from(set).sort()
})

const showStringBrowseAnimals = computed(() => {
  let result = animalOptions.value
  if (showStringClassFilter.value !== 'all') {
    result = result.filter(a => getShowClassLabel(a.birthDate, a.animalStage) === showStringClassFilter.value)
  }
  if (showStringSearch.value.trim()) {
    const q = showStringSearch.value.trim().toLowerCase()
    result = result.filter(a => (a.barnName || a.registeredName || '').toLowerCase().includes(q))
  }
  return result
})

const showStringSorted = computed(() => [...showStringRows.value].sort((a, b) => a.lineupOrder - b.lineupOrder))

// Split lineup into Cows (stage 3/4 or show class has "Cow") and Heifers/Youngstock
const showStringCows = computed(() =>
  showStringSorted.value.filter(row => {
    if (!row.animalId) return false
    const a = animals.value.find(x => x.animalId === row.animalId)
    if (!a) return false
    return a.animalStage === 3 || a.animalStage === 4 ||
      getShowClassLabel(a.birthDate, a.animalStage).includes('Cow') ||
      getShowClassLabel(a.birthDate, a.animalStage).includes('Year-Old')
  })
)

const showStringYoungstock = computed(() =>
  showStringSorted.value.filter(row => {
    if (!row.animalId) return false
    const a = animals.value.find(x => x.animalId === row.animalId)
    if (!a) return false
    return a.animalStage === 1 || a.animalStage === 2 ||
      getShowClassLabel(a.birthDate, a.animalStage).includes('Heifer') ||
      getShowClassLabel(a.birthDate, a.animalStage).includes('Calf')
  })
)

const showStringUnassigned = computed(() =>
  showStringSorted.value.filter(row => !row.animalId)
)
const embryosAvailable = computed(() => embryoRecords.value.filter(e => e.status === 0 || e.status === 1))
const embryosImplanted = computed(() => embryoRecords.value.filter(e => e.status === 2))
const embryosFailed = computed(() => embryoRecords.value.filter(e => e.status === 3))
const embryosSuccessful = computed(() => embryoRecords.value.filter(e => e.status === 4))

function getAnimalLabel(animalId: number | null): string {
  if (!animalId) return 'Unassigned'
  const a = animals.value.find(x => x.animalId === animalId)
  if (!a) return `Animal #${animalId}`
  return `${a.barnName || a.registeredName || `#${a.animalId}`} · ${formatCurrentAge(a.birthDate)} · ${getShowClassLabel(a.birthDate, a.animalStage)}`
}

function getScoreLabel(score: number | null | undefined): string {
  if (!score) return ''
  if (score >= 90) return `EX ${Math.round(score)}`
  if (score >= 85) return `VG ${Math.round(score)}`
  return `GP ${Math.round(score)}`
}

function barPct(value: number, allValues: number[]): number {
  const max = Math.max(...allValues, 1)
  if (max === 0) return 0
  return Math.max(Math.round((value / max) * 100), value > 0 ? 4 : 0)
}

function filteredListAnimals(list: AnimalGroupList): Animal[] {
  const q = (list.searchQuery || '').trim().toLowerCase()
  if (!q) return animalOptions.value
  return animalOptions.value.filter(a => (a.barnName || a.registeredName || '').toLowerCase().includes(q))
}

function isAnimalInShowString(animalId: number): boolean {
  return showStringRows.value.some(r => r.animalId === animalId)
}

function addToShowString(animal: Animal) {
  showStringRows.value.push({ id: nextRowId.value++, animalId: animal.animalId, lineupOrder: showStringRows.value.length + 1, feedNotes: '', feedRation: '', ringDirections: '' })
}

function addShowStringRow() {
  showStringRows.value.push({ id: nextRowId.value++, animalId: null, lineupOrder: showStringRows.value.length + 1, feedNotes: '', feedRation: '', ringDirections: '' })
}

function removeShowStringRow(id: number) { showStringRows.value = showStringRows.value.filter(r => r.id !== id) }

function toggleAnimalInList(key: string, animalId: number) {
  const list = groupLists.value.find(l => l.key === key)
  if (!list) return
  if (list.animalIds.includes(animalId)) list.animalIds = list.animalIds.filter(id => id !== animalId)
  else list.animalIds.push(animalId)
}

function addChecklistItem() { checklistItems.value.push({ id: Date.now(), text: 'New item', done: false }) }

async function loadEmbryoRecords() {
  embryoRecords.value = await getAllEmbryos()
  recentHeatRecipients.value = await getRecentHeatRecipients()
}

async function addEmbryoRecord() {
  const created = await createEmbryo({
    code: null,
    sire: null,
    donor: null,
    grade: null,
    status: 0,
    recipientAnimalId: null,
    implantDate: null,
    linkedBreedingNote: null,
    failureNotes: null,
    notes: null,
    collectionLocation: null,
    storageLocation: null
  })
  embryoRecords.value.unshift(created)
}

async function saveEmbryoRecord(record: EmbryoRecord) {
  embryoSavingId.value = record.embryoRecordId
  embryoMessage.value = ''
  try {
    await updateEmbryo(record.embryoRecordId, record)
    embryoMessage.value = 'Embryo record saved.'
  } finally {
    embryoSavingId.value = null
  }
}

async function removeEmbryoRecord(id: number) {
  if (!window.confirm('Delete this embryo record?')) return
  await deleteEmbryo(id)
  embryoRecords.value = embryoRecords.value.filter(e => e.embryoRecordId !== id)
}

function filteredRecipients(record: EmbryoRecord): Animal[] {
  const query = (recipientSearch.value[record.embryoRecordId] ?? '').trim().toLowerCase()
  if (!query) return animalOptions.value.slice(0, 8)
  return animalOptions.value
    .filter(animal =>
      (animal.barnName || '').toLowerCase().includes(query)
      || (animal.registeredName || '').toLowerCase().includes(query)
      || (animal.registrationNumber || '').toLowerCase().includes(query))
    .slice(0, 12)
}

async function confirmImplant(record: EmbryoRecord, animalId: number) {
  const animalName = getAnimalLabel(animalId)
  if (!window.confirm(`Are you sure embryo ${record.code || `#${record.embryoRecordId}`} was implanted in ${animalName}?`)) return

  embryoSavingId.value = record.embryoRecordId
  try {
    const updated = await implantEmbryo(record.embryoRecordId, animalId)
    embryoRecords.value = embryoRecords.value.map(item =>
      item.embryoRecordId === updated.embryoRecordId ? updated : item)
    embryoMessage.value = 'Implant recorded. The embryo is no longer available.'
  } finally {
    embryoSavingId.value = null
  }
}

async function confirmOutcome(record: EmbryoRecord, successful: boolean) {
  const action = successful ? 'confirm this recipient pregnant' : 'record that the embryo did not stick'
  if (!window.confirm(`Are you sure you want to ${action}?`)) return

  const notes = successful ? '' : (window.prompt('Optional failure or vet notes:') ?? '')
  embryoSavingId.value = record.embryoRecordId
  try {
    const updated = await recordEmbryoOutcome(record.embryoRecordId, successful, notes)
    embryoRecords.value = embryoRecords.value.map(item =>
      item.embryoRecordId === updated.embryoRecordId ? updated : item)
    embryoMessage.value = successful
      ? 'Pregnancy confirmed and the recipient record was updated.'
      : 'Failed implant recorded and the recipient remains open.'
    await loadEmbryoImplants()
  } finally {
    embryoSavingId.value = null
  }
}

function addAchievement() {
  achievements.value.push({ id: nextAchievementId.value++, animalId: null, showName: '', showDate: '', bagged: '', placed: '', notes: '' })
}

function removeAchievement(id: number) { achievements.value = achievements.value.filter(a => a.id !== id) }

onMounted(async () => {
  loadData()
  const tabParam = route.query.tab as string | undefined
  if (tabParam && ['analytics', 'embryos', 'embryoImplants', 'showString', 'lists', 'checklist', 'achievements'].includes(tabParam)) {
    activeTab.value = tabParam as HubTab
  }
  try { animals.value = await getAnimals() } catch (e) { console.error('Failed to load animals:', e) } finally { loading.value = false }
  try { await loadEmbryoRecords() } catch (e) { console.error('Failed to load embryo records:', e) }
  loadAnalytics()
  loadEmbryoImplants()
})
</script>

<template>
  <main class="rp">
    <header class="rp-hero">
      <div class="rp-hero-top">
        <button class="rp-back" type="button" @click="router.push('/')">← Dashboard</button>
        <span class="rp-brand">Venture Herd Manager</span>
      </div>
      <h1 class="rp-title">Reports &amp; Show Planner</h1>
      <p class="rp-sub">Embryo Inventory · Show String · Herd Lists · Checklist · Achievements</p>
      <p class="rp-powered">Powered by <strong>Venture Ag Marketing</strong> · Custom Application Solutions</p>
    </header>

    <nav class="rp-tabs">
      <button :class="{ active: activeTab === 'embryos' }" @click="activeTab = 'embryos'">🧬 Embryos</button>
      <button :class="{ active: activeTab === 'embryoImplants' }" @click="activeTab = 'embryoImplants'">🤰 Implanted</button>
      <button :class="{ active: activeTab === 'showString' }" @click="activeTab = 'showString'">🐄 Show String</button>
      <button :class="{ active: activeTab === 'lists' }" @click="activeTab = 'lists'">📋 Herd Lists</button>
      <button :class="{ active: activeTab === 'checklist' }" @click="activeTab = 'checklist'">✅ Checklist</button>
      <button :class="{ active: activeTab === 'achievements' }" @click="activeTab = 'achievements'">🏆 Achievements</button>
      <button :class="{ active: activeTab === 'analytics' }" @click="activeTab = 'analytics'">📊 Analytics</button>
    </nav>

    <section v-if="loading" class="rp-panel"><p>Loading animals...</p></section>

    <!-- ANALYTICS -->
    <section v-else-if="activeTab === 'analytics'" class="rp-panel">
      <div class="rp-ph">
        <h2>Herd Analytics</h2>
        <button type="button" class="rp-add-btn" @click="loadAnalytics" :disabled="analyticsLoading">{{ analyticsLoading ? 'Loading…' : '↻ Refresh' }}</button>
      </div>

      <div v-if="analyticsError" class="analytics-error">
        <strong>Analytics unavailable right now</strong>
        <p>{{ analyticsError }}</p>
        <p style="font-size:0.82rem;color:#7f1d1d;margin-top:4px">This usually means the API is deploying — try refreshing in a minute.</p>
        <button type="button" class="rp-add-btn" style="margin-top:10px" @click="loadAnalytics">↻ Try Again</button>
      </div>

      <template v-if="analyticsData && !analyticsLoading">
        <!-- Summary stat row -->
        <div class="analytics-stats-row">
          <div class="analytics-stat">
            <span class="as-val">{{ analyticsData.totals.activeAnimals }}</span>
            <span class="as-lbl">Active Animals</span>
          </div>
          <div class="analytics-stat">
            <span class="as-val">{{ analyticsData.totals.calvingsLast12Mo }}</span>
            <span class="as-lbl">Calvings (12 mo)</span>
          </div>
          <div class="analytics-stat">
            <span class="as-val">{{ analyticsData.totals.heatsLast12Mo }}</span>
            <span class="as-lbl">Heats (12 mo)</span>
          </div>
          <div class="analytics-stat">
            <span class="as-val">{{ analyticsData.totals.breedingsLast12Mo }}</span>
            <span class="as-lbl">Breedings (12 mo)</span>
          </div>
          <div class="analytics-stat highlight">
            <span class="as-val">{{ analyticsData.totals.conceptionRatePct }}%</span>
            <span class="as-lbl">Conception Rate</span>
          </div>
        </div>

        <!-- Chart: Calvings per month -->
        <div class="chart-block">
          <div class="chart-title">Calvings per Month</div>
          <div class="bar-chart">
            <div v-for="m in analyticsData.months" :key="`calv-${m.label}`" class="bar-col">
              <div class="bar-wrap">
                <span v-if="m.calvings > 0" class="bar-tip">{{ m.calvings }}</span>
                <div
                  class="bar bar-calving"
                  :style="{ height: barPct(m.calvings, analyticsData.months.map(x => x.calvings)) + '%' }"
                />
              </div>
              <span class="bar-label">{{ m.label.split(' ')[0] }}</span>
            </div>
          </div>
        </div>

        <!-- Chart: Heats per month -->
        <div class="chart-block">
          <div class="chart-title">Heat Events per Month</div>
          <div class="bar-chart">
            <div v-for="m in analyticsData.months" :key="`heat-${m.label}`" class="bar-col">
              <div class="bar-wrap">
                <span v-if="m.heats > 0" class="bar-tip">{{ m.heats }}</span>
                <div
                  class="bar bar-heat"
                  :style="{ height: barPct(m.heats, analyticsData.months.map(x => x.heats)) + '%' }"
                />
              </div>
              <span class="bar-label">{{ m.label.split(' ')[0] }}</span>
            </div>
          </div>
        </div>

        <!-- Chart: Breedings + Confirmed pregnancies overlaid -->
        <div class="chart-block">
          <div class="chart-title">
            Breedings vs Confirmed Pregnancies per Month
            <span class="chart-legend"><span class="legend-dot dot-breed" />Bred <span class="legend-dot dot-preg" style="margin-left:10px"/>Pregnant</span>
          </div>
          <div class="bar-chart">
            <div v-for="m in analyticsData.months" :key="`breed-${m.label}`" class="bar-col">
              <div class="bar-wrap bar-pair">
                <div class="bar-pair-inner">
                  <div class="bar bar-breed" :style="{ height: barPct(m.breedings, analyticsData.months.map(x => x.breedings)) + '%' }" :title="`Bred: ${m.breedings}`" />
                  <div class="bar bar-preg" :style="{ height: barPct(m.confirmedPregnancies, analyticsData.months.map(x => x.breedings)) + '%' }" :title="`Confirmed: ${m.confirmedPregnancies}`" />
                </div>
              </div>
              <span class="bar-label">{{ m.label.split(' ')[0] }}</span>
            </div>
          </div>
        </div>

        <!-- Chart: Dry-offs + Sold side by side -->
        <div class="chart-row-2">
          <div class="chart-block half">
            <div class="chart-title">Dry-Offs per Month</div>
            <div class="bar-chart">
              <div v-for="m in analyticsData.months" :key="`dry-${m.label}`" class="bar-col">
                <div class="bar-wrap">
                  <span v-if="m.dryOffs > 0" class="bar-tip">{{ m.dryOffs }}</span>
                  <div class="bar bar-dry" :style="{ height: barPct(m.dryOffs, analyticsData.months.map(x => x.dryOffs)) + '%' }" />
                </div>
                <span class="bar-label">{{ m.label.split(' ')[0] }}</span>
              </div>
            </div>
          </div>

          <div class="chart-block half">
            <div class="chart-title">Animals Sold per Month</div>
            <div class="bar-chart">
              <div v-for="m in analyticsData.months" :key="`sold-${m.label}`" class="bar-col">
                <div class="bar-wrap">
                  <span v-if="m.soldAnimals > 0" class="bar-tip">{{ m.soldAnimals }}</span>
                  <div class="bar bar-sold" :style="{ height: barPct(m.soldAnimals, analyticsData.months.map(x => x.soldAnimals)) + '%' }" />
                </div>
                <span class="bar-label">{{ m.label.split(' ')[0] }}</span>
              </div>
            </div>
          </div>
        </div>
      </template>

      <div v-else-if="analyticsLoading" class="analytics-loading">
        <div v-for="n in 4" :key="n" class="chart-skeleton" />
      </div>
    </section>

    <!-- EMBRYO INVENTORY -->
    <section v-else-if="activeTab === 'embryos'" class="rp-panel">
      <div class="rp-ph">
        <h2>Embryo Inventory</h2>
        <button type="button" class="rp-add-btn" @click="addEmbryoRecord">+ Add Embryo</button>
      </div>
      <p class="rp-hint">Track storage, assign recipients, log implants. Mark Failed when it didn't stick — those show below.</p>

      <p v-if="embryoMessage" class="rp-success">{{ embryoMessage }}</p>
      <div v-if="embryosAvailable.length === 0" class="rp-empty">No embryos are currently available.</div>

      <div v-for="rec in embryosAvailable" :key="rec.embryoRecordId" class="emb-card">
        <div class="emb-hd">
          <div class="emb-id">
            <span class="emb-code">{{ rec.code || 'No Code' }}</span>
            <span class="emb-badge">{{ EMBRYO_STATUS_LABELS[rec.status] }}</span>
          </div>
          <button type="button" class="rp-x" @click="removeEmbryoRecord(rec.embryoRecordId)">✕</button>
        </div>
        <div class="emb-grid">
          <label>Code / ID<input v-model="rec.code" type="text" placeholder="ET-2026-001"></label>
          <label>Sire<input v-model="rec.sire" type="text" placeholder="Sire name"></label>
          <label>Donor Cow<input v-model="rec.donor" type="text" placeholder="Donor name"></label>
          <label>Grade<input v-model="rec.grade" type="text" placeholder="Grade 1, Excellent…"></label>
          <label>Status
            <select v-model.number="rec.status">
              <option :value="0">In Storage</option>
              <option :value="1">Assigned to Recipient</option>
            </select>
          </label>
          <label>Breeding Link Note<input v-model="rec.linkedBreedingNote" type="text" placeholder="Breeding date or event ref"></label>
          <label class="emb-full">Notes<textarea v-model="rec.notes" rows="2" placeholder="Tank, straw info, vet notes" /></label>
        </div>

        <div class="recipient-picker">
          <strong>Recent heat recipients</strong>
          <small>Animals recorded in heat 6–8 days ago</small>

          <div v-if="recentHeatRecipients.length" class="recipient-suggestions">
            <button
              v-for="candidate in recentHeatRecipients"
              :key="`${rec.embryoRecordId}-${candidate.animalId}`"
              type="button"
              @click="confirmImplant(rec, candidate.animalId)"
            >
              {{ candidate.animalName }} · {{ candidate.daysSinceHeat }} days
            </button>
          </div>
          <p v-else class="rp-hint">No recent-heat candidates right now.</p>

          <label class="recipient-search">
            Search another animal
            <input
              v-model="recipientSearch[rec.embryoRecordId]"
              type="search"
              placeholder="Name or registration number"
            >
          </label>
          <div class="recipient-results">
            <button
              v-for="animalOption in filteredRecipients(rec)"
              :key="`${rec.embryoRecordId}-animal-${animalOption.animalId}`"
              type="button"
              @click="confirmImplant(rec, animalOption.animalId)"
            >
              {{ animalOption.barnName || animalOption.registeredName }}
            </button>
          </div>
        </div>

        <button
          type="button"
          class="rp-add-btn"
          :disabled="embryoSavingId === rec.embryoRecordId"
          @click="saveEmbryoRecord(rec)"
        >
          {{ embryoSavingId === rec.embryoRecordId ? 'Saving…' : 'Save Details' }}
        </button>
      </div>

      <template v-if="embryosFailed.length > 0">
        <div class="rp-divider rp-divider-failed">Failed / Not Confirmed ({{ embryosFailed.length }})</div>
        <p class="rp-hint">Embryos that didn't stick — kept for your records.</p>
        <div v-for="rec in embryosFailed" :key="`f-${rec.embryoRecordId}`" class="emb-card emb-failed">
          <div class="emb-hd">
            <div class="emb-id">
              <span class="emb-code">{{ rec.code || 'No Code' }}</span>
              <span class="emb-badge ebadge-failed">Failed</span>
            </div>
            <button type="button" class="rp-x" @click="removeEmbryoRecord(rec.embryoRecordId)">✕</button>
          </div>
          <div class="emb-summary"><span>Sire: <strong>{{ rec.sire || '—' }}</strong></span><span>Donor: <strong>{{ rec.donor || '—' }}</strong></span><span>Recipient: <strong>{{ rec.recipientAnimalId ? getAnimalLabel(rec.recipientAnimalId) : '—' }}</strong></span></div>
          <label class="emb-full mt8">Failure Notes<textarea v-model="rec.failureNotes" rows="2" placeholder="Reason, vet notes, recheck date" /></label>
          <label class="emb-full mt8">Status
            <select v-model.number="rec.status">
              <option :value="3">Failed / Not Confirmed</option>
              <option :value="0">Back to Storage</option>
            </select>
          </label>
        </div>
      </template>
    </section>

    <!-- EMBRYO IMPLANTS -->
    <section v-else-if="activeTab === 'embryoImplants'" class="rp-panel">
      <div class="rp-ph">
        <h2>Currently Implanted</h2>
        <button type="button" class="rp-add-btn" @click="loadEmbryoImplants" :disabled="embryoImplantsLoading">{{ embryoImplantsLoading ? 'Loading…' : '↻ Refresh' }}</button>
      </div>
      <p class="rp-hint">Record the pregnancy-check result for each implanted embryo.</p>

      <div v-if="embryosImplanted.length === 0" class="rp-empty">No embryos are waiting for an outcome.</div>
      <div v-for="rec in embryosImplanted" :key="`implanted-${rec.embryoRecordId}`" class="emb-card emb-implanted">
        <div class="emb-hd">
          <div class="emb-id">
            <span class="emb-code">{{ rec.code || `Embryo #${rec.embryoRecordId}` }}</span>
            <span class="emb-badge ebadge-implanted">Implanted</span>
          </div>
        </div>
        <div class="emb-summary">
          <span>Recipient: <strong>{{ getAnimalLabel(rec.recipientAnimalId) }}</strong></span>
          <span>Implanted: <strong>{{ rec.implantDate || 'Date not recorded' }}</strong></span>
          <span>Sire: <strong>{{ rec.sire || '—' }}</strong></span>
        </div>
        <div class="outcome-actions">
          <button type="button" class="outcome-success" @click="confirmOutcome(rec, true)">Confirmed Pregnant</button>
          <button type="button" class="outcome-failed" @click="confirmOutcome(rec, false)">Did Not Stick</button>
        </div>
      </div>

      <div v-if="embryosSuccessful.length || embryosFailed.length" class="rp-divider">
        Recorded outcomes: {{ embryosSuccessful.length }} pregnant · {{ embryosFailed.length }} failed
      </div>

      <h3 class="implant-report-title">Implant Report</h3>
      <p class="rp-hint">Monthly implant and outcome history.</p>

      <template v-if="embryoImplantsError">
        <div class="rp-error">{{ embryoImplantsError }} <button type="button" @click="loadEmbryoImplants">Try Again</button></div>
      </template>

      <template v-else-if="embryoImplantsData && !embryoImplantsLoading">
        <div class="as-row">
          <div class="as-stat">
            <span class="as-label">Total Implanted</span>
            <span class="as-val">{{ embryoImplantsData.totals.totalImplanted }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Successful (Pregnant)</span>
            <span class="as-val">{{ embryoImplantsData.totals.totalSuccessful }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Failed / Stuck</span>
            <span class="as-val">{{ embryoImplantsData.totals.totalFailed }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Success Rate</span>
            <span class="as-val">{{ embryoImplantsData.totals.successRatePct }}%</span>
          </div>
        </div>

        <div class="bc-group">
          <div class="bc-title">Implanted vs Failed (Monthly)</div>
          <div class="bar-chart">
            <div class="bar-legend">
              <span><span class="dot" style="background: #00c853;"></span>Implanted</span>
              <span><span class="dot" style="background: #f44336;"></span>Failed</span>
              <span><span class="dot" style="background: #2196f3;"></span>Successful</span>
            </div>
            <div class="bc-wrap">
              <div v-for="m in embryoImplantsData.months" :key="`ei-${m.label}`" class="bar-col">
                <div class="bar-wrap">
                  <div class="bar bar-implanted" :style="{ height: barPct(m.implanted, embryoImplantsData.months.map(x => x.implanted)) + '%' }" :title="`Implanted: ${m.implanted}`" />
                  <div class="bar bar-failed" :style="{ height: barPct(m.failed, embryoImplantsData.months.map(x => x.failed)) + '%' }" :title="`Failed: ${m.failed}`" />
                  <div class="bar bar-successful" :style="{ height: barPct(m.successful, embryoImplantsData.months.map(x => x.successful)) + '%' }" :title="`Successful: ${m.successful}`" />
                </div>
                <div class="bar-tip">{{ m.label }}</div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </section>

    <!-- SHOW STRING -->
    <section v-else-if="activeTab === 'showString'" class="rp-panel">
      <div class="rp-ph">
        <h2>Show String Lineup</h2>
        <button type="button" class="rp-add-btn" @click="addShowStringRow">+ Blank Row</button>
      </div>

      <div class="browse-panel">
        <div class="browse-label">Browse Herd by Show Class</div>
        <div class="browse-filters">
          <select v-model="showStringClassFilter" class="rp-sel">
            <option value="all">All Classes</option>
            <option v-for="cls in showClassOptions" :key="cls" :value="cls">{{ cls }}</option>
          </select>
          <input v-model="showStringSearch" type="search" class="rp-sel" placeholder="Filter by name…" />
        </div>
        <div class="browse-grid">
          <div v-for="a in showStringBrowseAnimals" :key="a.animalId" class="browse-row" :class="{ 'in-str': isAnimalInShowString(a.animalId) }">
            <div class="browse-info">
              <strong>{{ a.barnName || a.registeredName || `#${a.animalId}` }}</strong>
              <span class="browse-age">{{ formatCurrentAge(a.birthDate) }}</span>
              <span class="browse-cls">{{ getShowClassLabel(a.birthDate, a.animalStage) }}</span>
            </div>
            <button v-if="!isAnimalInShowString(a.animalId)" type="button" class="add-str-btn" @click="addToShowString(a)">+ Add</button>
            <span v-else class="in-str-tag">✓ In String</span>
          </div>
          <p v-if="showStringBrowseAnimals.length === 0" class="rp-empty-sm">No animals match this filter.</p>
        </div>
      </div>

      <div v-if="showStringSorted.length > 0" class="lineup-label">Lineup ({{ showStringSorted.length }})</div>
      <div v-else class="rp-empty">Use the browser above or "+ Blank Row" to build your lineup.</div>

      <!-- Section: Cows -->
      <template v-if="showStringCows.length > 0">
        <div class="lineup-section-hd">
          <span class="lineup-section-icon">🐄</span>
          Cows <span class="lineup-section-ct">{{ showStringCows.length }}</span>
        </div>
        <div v-for="(row, idx) in showStringCows" :key="row.id" class="lineup-card">
          <div class="lineup-pos">{{ idx + 1 }}</div>
          <div class="lineup-main">
            <div class="lineup-name">
              {{ row.animalId ? (animals.find(a => a.animalId === row.animalId)?.barnName || animals.find(a => a.animalId === row.animalId)?.registeredName || `#${row.animalId}`) : '—' }}
              <button type="button" class="lineup-remove" @click="removeShowStringRow(row.id)" title="Remove">✕</button>
            </div>
            <div class="lineup-meta-row">
              <span class="lineup-class-pill">{{ row.animalId ? getShowClassLabel(animals.find(a => a.animalId === row.animalId)?.birthDate, animals.find(a => a.animalId === row.animalId)?.animalStage) : '' }}</span>
              <span class="lineup-age" v-if="row.animalId">{{ formatCurrentAge(animals.find(a => a.animalId === row.animalId)?.birthDate) }}</span>
              <span class="lineup-score" v-if="animals.find(a => a.animalId === row.animalId)?.latestScore">{{ getScoreLabel(animals.find(a => a.animalId === row.animalId)?.latestScore) }}</span>
            </div>
            <div class="lineup-notes-row">
              <div class="lineup-note-block">
                <span class="lineup-note-lbl">Feed Ration</span>
                <input v-model="row.feedRation" type="text" class="lineup-input" placeholder="8 lbs grain, 20 lbs hay, top dress X…" />
              </div>
              <div class="lineup-note-block">
                <span class="lineup-note-lbl">Feed Schedule / Notes</span>
                <textarea v-model="row.feedNotes" rows="2" class="lineup-textarea" placeholder="Show-week timing, special instructions" />
              </div>
              <div class="lineup-note-block">
                <span class="lineup-note-lbl">Ring Directions</span>
                <textarea v-model="row.ringDirections" rows="2" class="lineup-textarea" placeholder="Lead side, clipping cues, prep notes, blanketing" />
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Section: Heifers & Youngstock -->
      <template v-if="showStringYoungstock.length > 0">
        <div class="lineup-section-hd">
          <span class="lineup-section-icon">🌱</span>
          Heifers &amp; Youngstock <span class="lineup-section-ct">{{ showStringYoungstock.length }}</span>
        </div>
        <div v-for="(row, idx) in showStringYoungstock" :key="row.id" class="lineup-card">
          <div class="lineup-pos heifer-pos">{{ idx + 1 }}</div>
          <div class="lineup-main">
            <div class="lineup-name">
              {{ row.animalId ? (animals.find(a => a.animalId === row.animalId)?.barnName || animals.find(a => a.animalId === row.animalId)?.registeredName || `#${row.animalId}`) : '—' }}
              <button type="button" class="lineup-remove" @click="removeShowStringRow(row.id)" title="Remove">✕</button>
            </div>
            <div class="lineup-meta-row">
              <span class="lineup-class-pill heifer-pill">{{ row.animalId ? getShowClassLabel(animals.find(a => a.animalId === row.animalId)?.birthDate, animals.find(a => a.animalId === row.animalId)?.animalStage) : '' }}</span>
              <span class="lineup-age" v-if="row.animalId">{{ formatCurrentAge(animals.find(a => a.animalId === row.animalId)?.birthDate) }}</span>
            </div>
            <div class="lineup-notes-row">
              <div class="lineup-note-block">
                <span class="lineup-note-lbl">Feed Ration</span>
                <input v-model="row.feedRation" type="text" class="lineup-input" placeholder="8 lbs grain, 20 lbs hay, top dress X…" />
              </div>
              <div class="lineup-note-block">
                <span class="lineup-note-lbl">Feed Schedule / Notes</span>
                <textarea v-model="row.feedNotes" rows="2" class="lineup-textarea" placeholder="Show-week timing, special instructions" />
              </div>
              <div class="lineup-note-block">
                <span class="lineup-note-lbl">Ring Directions</span>
                <textarea v-model="row.ringDirections" rows="2" class="lineup-textarea" placeholder="Lead side, clipping cues, prep notes, blanketing" />
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Unassigned / blank rows -->
      <template v-if="showStringUnassigned.length > 0">
        <div class="lineup-section-hd">
          <span class="lineup-section-icon">📌</span>
          Unassigned Slots <span class="lineup-section-ct">{{ showStringUnassigned.length }}</span>
        </div>
        <div v-for="row in showStringUnassigned" :key="row.id" class="lineup-card lineup-card-empty">
          <div class="lineup-pos empty-pos">?</div>
          <div class="lineup-main">
            <label class="lineup-note-lbl" style="margin-bottom:6px">Animal
              <select v-model.number="row.animalId" class="lineup-input" style="min-height:42px;margin-top:4px">
                <option :value="null">— Select animal —</option>
                <option v-for="a in animalOptions" :key="a.animalId" :value="a.animalId">{{ getAnimalLabel(a.animalId) }}</option>
              </select>
            </label>
            <button type="button" class="lineup-remove" style="margin-top:8px" @click="removeShowStringRow(row.id)">✕ Remove</button>
          </div>
        </div>
      </template>
    </section>

    <!-- HERD LISTS -->
    <section v-else-if="activeTab === 'lists'" class="rp-panel">
      <div class="rp-ph"><h2>Herd Lists</h2></div>
      <p class="rp-hint">Search by barn name, tap animals to toggle them into a list. Each list is independent.</p>

      <div v-for="list in groupLists" :key="list.key" class="rp-group">
        <div class="rp-group-hd">
          <h3>{{ list.title }}</h3>
          <span class="rp-group-ct">{{ list.animalIds.length }}</span>
        </div>
        <input v-model="list.searchQuery" type="search" class="rp-list-search" :placeholder="`Search ${list.title}…`" />
        <div class="rp-chips">
          <button v-for="a in filteredListAnimals(list)" :key="`${list.key}-${a.animalId}`" type="button" class="rp-chip" :class="{ 'rp-chip-sel': list.animalIds.includes(a.animalId) }" @click="toggleAnimalInList(list.key, a.animalId)">{{ a.barnName || a.registeredName || `#${a.animalId}` }}</button>
        </div>
        <div v-if="list.animalIds.length > 0" class="rp-list-members">
          <div class="rp-lm-hd">In this list</div>
          <div v-for="id in list.animalIds" :key="`in-${id}`" class="rp-lm-row">
            <span>{{ getAnimalLabel(id) }}</span>
            <button type="button" class="rp-lm-rm" @click="toggleAnimalInList(list.key, id)">✕</button>
          </div>
        </div>
        <label class="lbl-notes">Notes<textarea v-model="list.notes" rows="2" placeholder="Instructions, sorting priority, vet orders" class="rp-textarea" /></label>
      </div>
    </section>

    <!-- CHECKLIST -->
    <section v-else-if="activeTab === 'checklist'" class="rp-panel">
      <div class="rp-ph">
        <h2>Show Supplies Checklist</h2>
        <button type="button" class="rp-add-btn" @click="addChecklistItem">+ Item</button>
      </div>
      <div class="rp-checklist">
        <label v-for="item in checklistItems" :key="item.id" class="rp-check-row" :class="{ done: item.done }">
          <input v-model="item.done" type="checkbox">
          <input v-model="item.text" type="text" :class="{ 'done-txt': item.done }">
        </label>
      </div>
    </section>

    <!-- ACHIEVEMENTS -->
    <section v-else class="rp-panel">
      <div class="rp-ph">
        <h2>Show Achievements</h2>
        <button type="button" class="rp-add-btn" @click="addAchievement">+ Achievement</button>
      </div>
      <div v-if="achievements.length === 0" class="rp-empty">No achievements logged yet.</div>
      <div v-for="rec in achievements" :key="rec.id" class="rp-row-card">
        <label>Animal
          <select v-model.number="rec.animalId">
            <option :value="null">Select animal</option>
            <option v-for="a in animalOptions" :key="`ach-${a.animalId}`" :value="a.animalId">{{ a.barnName || a.registeredName || `#${a.animalId}` }}</option>
          </select>
        </label>
        <label>Show Name<input v-model="rec.showName" type="text" placeholder="Spring Classic Show"></label>
        <label>Show Date<input v-model="rec.showDate" type="date"></label>
        <label>Bagged<input v-model="rec.bagged" type="text" placeholder="How she bagged up"></label>
        <label>Placement<input v-model="rec.placed" type="text" placeholder="1st Jr 2 · Reserve Champion"></label>
        <label class="rp-full">Notes<textarea v-model="rec.notes" rows="2" placeholder="Judge comments, prep notes" /></label>
        <button type="button" class="rp-danger" @click="removeAchievement(rec.id)">Remove</button>
      </div>
    </section>
  </main>
</template>

<style scoped>
.rp { max-width: 1240px; margin: 0 auto; padding: 0 0 60px; font-family: 'Bahnschrift', 'Arial Narrow', 'Segoe UI', sans-serif; background: #f5f7f2; min-height: 100vh; }
.rp-hero { background: linear-gradient(135deg, #0f2318 0%, #1a3d22 60%, #244f2f 100%); padding: 22px 24px 18px; border-bottom: 3px solid #31572c; }
.rp-hero-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 10px; }
.rp-brand { color: #7dd3a0; font-size: 0.75rem; font-weight: 900; letter-spacing: 0.12em; text-transform: uppercase; }
.rp-title { margin: 0; font-size: 1.85rem; font-weight: 900; color: #fff; letter-spacing: -0.02em; }
.rp-sub { margin: 6px 0 0; color: rgba(255,255,255,0.6); font-size: 0.88rem; }
.rp-powered { margin: 10px 0 0; color: rgba(255,255,255,0.3); font-size: 0.72rem; letter-spacing: 0.04em; }
.rp-powered strong { color: rgba(255,255,255,0.55); font-weight: 900; }
.rp-back { display: inline-flex; align-items: center; gap: 6px; border: 1px solid rgba(255,255,255,0.22); background: rgba(255,255,255,0.07); color: #e2e8f0; font-weight: 800; font-size: 0.85rem; border-radius: 6px; padding: 8px 14px; cursor: pointer; }
.rp-back:hover { background: rgba(255,255,255,0.14); }

.rp-tabs { display: flex; overflow-x: auto; gap: 0; background: #fff; border-bottom: 2px solid #e0e8e1; padding: 0 16px; }
.rp-tabs::-webkit-scrollbar { height: 0; }
.rp-tabs button { flex-shrink: 0; border: none; border-bottom: 3px solid transparent; background: transparent; color: #5d6f63; font-weight: 800; font-size: 0.82rem; letter-spacing: 0.01em; padding: 14px 16px 11px; cursor: pointer; white-space: nowrap; transition: color 0.15s, border-color 0.15s; }
.rp-tabs button:hover { color: #0f1f16; }
.rp-tabs button.active { color: #31572c; border-bottom-color: #31572c; }

.rp-panel { margin: 20px 16px; background: #fff; border: 1px solid #d9e3dc; border-radius: 12px; padding: 20px; }
.rp-ph { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 16px; padding-bottom: 14px; border-bottom: 1px solid #e0e8e1; }
.rp-ph h2 { margin: 0; color: #0f1f16; font-size: 1.1rem; font-weight: 900; letter-spacing: 0.01em; }
.rp-add-btn { border: none; background: #31572c; color: #fff; border-radius: 6px; min-height: 38px; padding: 0 14px; font-weight: 900; font-size: 0.85rem; letter-spacing: 0.04em; cursor: pointer; }
.rp-add-btn:hover { background: #254520; }
.rp-hint { color: #5d6f63; margin: 0 0 14px; font-size: 0.87rem; line-height: 1.5; }
.rp-empty { border: 1px dashed #c8d4cb; border-radius: 8px; padding: 24px; color: #8a9b8e; text-align: center; }
.rp-empty-sm { padding: 14px; color: #8a9b8e; text-align: center; grid-column: 1 / -1; }

/* embryo cards */
.emb-card { border: 1px solid #d9e3dc; border-left: 4px solid #31572c; border-radius: 10px; padding: 14px; margin: 10px 0; background: #fff; }
.emb-in-storage { border-left-color: #31572c; }
.emb-assigned { border-left-color: #d97706; }
.emb-implanted { border-left-color: #2563eb; background: #f8fbff; }
.emb-failed { border-left-color: #dc2626; background: #fff8f8; }
.emb-hd { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
.emb-id { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.emb-code { font-weight: 900; font-size: 1rem; color: #0f1f16; }
.emb-date { font-size: 0.82rem; color: #5d6f63; }
.emb-badge { border-radius: 999px; padding: 2px 10px; font-size: 0.72rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; }
.ebadge-in-storage { background: #dcfce7; color: #14532d; }
.ebadge-assigned { background: #fef3c7; color: #92400e; }
.ebadge-implanted { background: #dbeafe; color: #1d4ed8; }
.ebadge-failed { background: #fee2e2; color: #991b1b; }
.emb-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.emb-full { grid-column: 1 / -1; }
.rp-success { padding: 10px 12px; border-radius: 7px; background: #ecfdf3; color: #166534; font-weight: 700; }
.recipient-picker { display: grid; gap: 9px; margin: 14px 0; padding: 12px; border-radius: 8px; background: #f5f8f5; }
.recipient-picker > small { color: #64748b; }
.recipient-suggestions, .recipient-results { display: flex; flex-wrap: wrap; gap: 7px; }
.recipient-suggestions button, .recipient-results button { border: 1px solid #b9c9bc; border-radius: 999px; background: #fff; color: #264b2b; padding: 8px 11px; font-weight: 700; cursor: pointer; }
.recipient-suggestions button:hover, .recipient-results button:hover { background: #eaf4eb; }
.recipient-search { display: grid; gap: 5px; color: #405b48; font-weight: 700; }
.recipient-search input { width: 100%; padding: 10px; border: 1px solid #c8d4cb; border-radius: 6px; }
.outcome-actions { display: flex; flex-wrap: wrap; gap: 9px; margin-top: 12px; }
.outcome-actions button { border: 0; border-radius: 7px; padding: 10px 13px; color: #fff; font-weight: 800; cursor: pointer; }
.outcome-success { background: #2f6d3a; }
.outcome-failed { background: #a33a32; }
.implant-report-title { margin: 24px 0 6px; color: #18351f; }
.mt8 { margin-top: 8px; }
.emb-summary { display: flex; flex-wrap: wrap; gap: 12px; font-size: 0.9rem; color: #5d6f63; }
.rp-x { border: 1px solid #fca5a5; background: #fff1f2; color: #991b1b; border-radius: 4px; width: 28px; height: 28px; font-size: 0.85rem; font-weight: 900; cursor: pointer; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.rp-x:hover { background: #fee2e2; }
.rp-divider { margin: 20px 0 10px; padding: 8px 14px; background: #f0f7f1; border-left: 4px solid #31572c; border-radius: 4px; font-weight: 900; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.06em; color: #1f3a25; }
.rp-divider-failed { background: #fff8f8; border-left-color: #dc2626; color: #991b1b; }

/* form controls */
label { display: grid; gap: 6px; color: #5d6f63; font-weight: 700; font-size: 0.8rem; letter-spacing: 0.06em; text-transform: uppercase; }
input, select, textarea { min-height: 44px; border: 1px solid #c8d4cb; border-radius: 6px; padding: 10px 12px; font-size: 0.95rem; font-family: inherit; background: #fff; color: #0f1f16; transition: border-color 0.15s; }
input:focus, select:focus, textarea:focus { outline: none; border-color: #31572c; box-shadow: 0 0 0 3px rgba(49,87,44,0.1); }
input::placeholder, textarea::placeholder { color: #9ca8a0; }
textarea { min-height: 72px; resize: vertical; }
.rp-textarea { min-height: 60px; border: 1px solid #c8d4cb; border-radius: 8px; padding: 8px 10px; font-size: 0.95rem; width: 100%; box-sizing: border-box; resize: vertical; }
.rp-full { grid-column: 1 / -1; }
.rp-danger { border: 1px solid #fca5a5; background: #fff1f2; color: #991b1b; border-radius: 6px; min-height: 36px; padding: 0 12px; font-weight: 800; font-size: 0.8rem; cursor: pointer; justify-self: start; }
.rp-danger:hover { background: #fee2e2; }

/* row cards */
.rp-row-card { background: #f8fbf8; border: 1px solid #e0e8e1; border-left: 3px solid #31572c; border-radius: 8px; padding: 16px; margin: 10px 0; display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }

/* browse panel */
.browse-panel { border: 1px solid #d9e3dc; border-radius: 10px; padding: 14px; margin-bottom: 18px; background: #f8fbf8; }
.browse-label { font-weight: 900; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.08em; color: #31572c; margin-bottom: 10px; }
.browse-filters { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 12px; }
.rp-sel { min-height: 42px; border: 1px solid #c8d4cb; border-radius: 8px; padding: 8px 12px; font-size: 0.95rem; background: #fff; color: #0f1f16; }
.browse-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 8px; max-height: 280px; overflow-y: auto; }
.browse-row { display: flex; justify-content: space-between; align-items: center; padding: 10px 12px; border: 1px solid #e0e8e1; border-radius: 8px; background: #fff; gap: 8px; }
.browse-row.in-str { background: #f0fff4; border-color: #86efac; }
.browse-info { display: grid; gap: 2px; min-width: 0; }
.browse-info strong { font-size: 0.9rem; color: #0f1f16; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.browse-age { font-size: 0.78rem; font-weight: 700; color: #31572c; }
.browse-cls { font-size: 0.74rem; color: #5d6f63; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.add-str-btn { border: 1px solid #31572c; background: #31572c; color: #fff; border-radius: 6px; padding: 4px 10px; font-size: 0.82rem; font-weight: 800; cursor: pointer; white-space: nowrap; flex-shrink: 0; }
.add-str-btn:hover { background: #254520; }
.in-str-tag { font-size: 0.78rem; font-weight: 800; color: #166534; flex-shrink: 0; }
.lineup-label { font-weight: 900; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.08em; color: #5d6f63; margin: 6px 0 8px; }

/* ── Lineup cards ── */
.lineup-section-hd {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  background: #f0f7f1;
  border-left: 4px solid #31572c;
  border-radius: 6px;
  margin: 18px 0 10px;
  font-weight: 900;
  font-size: 0.88rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: #0f2318;
}

.lineup-section-icon { font-size: 1.1rem; }

.lineup-section-ct {
  margin-left: auto;
  background: #31572c;
  color: #fff;
  border-radius: 999px;
  padding: 1px 9px;
  font-size: 0.75rem;
}

.lineup-card {
  display: flex;
  gap: 14px;
  padding: 14px;
  border: 1px solid #e0e8e1;
  border-radius: 10px;
  background: #fff;
  margin: 8px 0;
  align-items: flex-start;
}

.lineup-card-empty {
  background: #fafbfa;
  border-style: dashed;
}

.lineup-pos {
  flex-shrink: 0;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: #31572c;
  color: #fff;
  font-size: 1.4rem;
  font-weight: 900;
  display: flex;
  align-items: center;
  justify-content: center;
}

.heifer-pos {
  background: #6d28d9;
}

.empty-pos {
  background: #9ca3af;
  font-size: 1.2rem;
}

.lineup-main {
  flex: 1;
  min-width: 0;
}

.lineup-name {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1.25rem;
  font-weight: 900;
  color: #0f1f16;
  line-height: 1.2;
  margin-bottom: 6px;
}

.lineup-remove {
  margin-left: auto;
  border: none;
  background: transparent;
  color: #dc2626;
  font-size: 0.85rem;
  cursor: pointer;
  padding: 2px 6px;
  border-radius: 4px;
  flex-shrink: 0;
}

.lineup-remove:hover { background: #fee2e2; }

.lineup-meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  margin-bottom: 10px;
}

.lineup-class-pill {
  background: #dcfce7;
  color: #14532d;
  border-radius: 999px;
  padding: 2px 10px;
  font-size: 0.78rem;
  font-weight: 900;
  letter-spacing: 0.04em;
}

.heifer-pill {
  background: #ede9fe;
  color: #4c1d95;
}

.lineup-age {
  font-size: 0.82rem;
  font-weight: 700;
  color: #5d6f63;
}

.lineup-score {
  font-size: 0.82rem;
  font-weight: 900;
  color: #d97706;
  background: #fef3c7;
  border-radius: 999px;
  padding: 1px 8px;
}

.lineup-notes-row {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 10px;
}

.lineup-note-block {
  display: grid;
  gap: 4px;
}

.lineup-note-lbl {
  font-size: 0.72rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: #8a9b8e;
}

.lineup-input {
  width: 100%;
  min-height: 36px;
  border: 1px solid #e0e8e1;
  border-radius: 6px;
  padding: 6px 10px;
  font-size: 0.9rem;
  font-family: inherit;
  background: #f8fbf8;
  color: #0f1f16;
  box-sizing: border-box;
  transition: border-color 0.15s;
}

.lineup-input:focus {
  outline: none;
  border-color: #31572c;
  background: #fff;
}

.lineup-textarea {
  width: 100%;
  min-height: 58px;
  border: 1px solid #e0e8e1;
  border-radius: 6px;
  padding: 6px 10px;
  font-size: 0.9rem;
  font-family: inherit;
  background: #f8fbf8;
  color: #0f1f16;
  box-sizing: border-box;
  resize: vertical;
  transition: border-color 0.15s;
}

.lineup-textarea:focus {
  outline: none;
  border-color: #31572c;
  background: #fff;
}

.lineup-input::placeholder,
.lineup-textarea::placeholder { color: #b4c2b8; }

@media (max-width: 700px) {
  .lineup-notes-row {
    grid-template-columns: 1fr;
  }
}

/* herd lists */
.rp-group { border: 1px solid #e0e8e1; border-radius: 10px; padding: 16px; margin: 12px 0; background: #f8fbf8; }
.rp-group-hd { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
.rp-group-hd h3 { margin: 0; color: #31572c; font-size: 0.8rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; }
.rp-group-ct { font-size: 0.82rem; font-weight: 700; color: #31572c; background: #dcfce7; border-radius: 999px; padding: 2px 10px; }
.rp-list-search { width: 100%; min-height: 42px; border: 1px solid #c8d4cb; border-radius: 8px; padding: 8px 12px; font-size: 0.95rem; background: #fff; margin-bottom: 10px; box-sizing: border-box; color: #0f1f16; }
.rp-chips { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 12px; }
.rp-chip { border: 1px solid #c8d4cb; border-radius: 999px; background: #fff; color: #5d6f63; min-height: 34px; padding: 0 14px; font-weight: 700; font-size: 0.85rem; cursor: pointer; transition: all 0.15s; }
.rp-chip:hover { border-color: #31572c; color: #0f1f16; }
.rp-chip-sel { border-color: #31572c; background: #e8f5ea; color: #17331f; font-weight: 900; }
.rp-list-members { margin-top: 12px; border: 1px solid #e0e8e1; border-radius: 8px; overflow: hidden; }
.rp-lm-hd { padding: 6px 12px; background: #f4f7f4; font-size: 0.75rem; font-weight: 800; text-transform: uppercase; color: #5d6f63; letter-spacing: 0.06em; }
.rp-lm-row { display: flex; justify-content: space-between; align-items: center; padding: 8px 12px; border-top: 1px solid #e0e8e1; font-size: 0.9rem; color: #0f1f16; }
.rp-lm-rm { border: none; background: transparent; color: #dc2626; font-size: 0.85rem; cursor: pointer; padding: 2px 6px; border-radius: 4px; }
.rp-lm-rm:hover { background: #fee2e2; }
.lbl-notes { display: grid; gap: 6px; margin-top: 10px; color: #5d6f63; font-weight: 700; font-size: 0.8rem; letter-spacing: 0.06em; text-transform: uppercase; }

/* checklist */
.rp-checklist { display: grid; gap: 6px; }
.rp-check-row { display: grid; grid-template-columns: 28px 1fr; align-items: center; gap: 10px; padding: 6px 8px; border-radius: 6px; background: #f8fbf8; border: 1px solid #e0e8e1; }
.rp-check-row.done { opacity: 0.55; }
.rp-check-row input[type='checkbox'] { width: 20px; height: 20px; margin: 0; accent-color: #31572c; }
.rp-check-row input[type='text'] { min-height: 40px; border: none; background: transparent; color: #0f1f16; font-size: 1rem; }
.rp-check-row input[type='text']:focus { outline: none; }
.done-txt { text-decoration: line-through !important; color: #8a9b8e !important; }

/* ── Analytics ── */
.analytics-stats-row { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 10px; margin-bottom: 22px; }
.analytics-stat { display: flex; flex-direction: column; align-items: center; padding: 14px 10px; border: 1px solid #e0e8e1; border-radius: 10px; background: #f8fbf8; text-align: center; }
.analytics-stat.highlight { background: #dcfce7; border-color: #31572c; }
.as-val { font-size: 2rem; font-weight: 900; color: #0f1f16; line-height: 1; }
.analytics-stat.highlight .as-val { color: #14532d; }
.as-lbl { font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #5d6f63; margin-top: 6px; }
.analytics-error { color: #991b1b; background: #fff1f2; border: 1px solid #fca5a5; border-radius: 8px; padding: 12px 14px; margin-bottom: 14px; }
.chart-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.chart-block { margin-bottom: 22px; border: 1px solid #e0e8e1; border-radius: 10px; padding: 16px; background: #fff; }
.chart-block.half { margin-bottom: 0; }
.chart-title { font-size: 0.82rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; color: #0f1f16; margin-bottom: 14px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 8px; }
.chart-legend { display: flex; align-items: center; gap: 4px; font-size: 0.72rem; font-weight: 700; text-transform: none; letter-spacing: 0; color: #5d6f63; }
.legend-dot { display: inline-block; width: 10px; height: 10px; border-radius: 3px; }
.dot-breed { background: #3b82f6; }
.dot-preg { background: #22c55e; }
.bar-chart { display: flex; gap: 4px; align-items: flex-end; height: 140px; }
.bar-col { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 4px; height: 100%; }
.bar-wrap { flex: 1; width: 100%; display: flex; flex-direction: column; justify-content: flex-end; align-items: center; position: relative; }
.bar-tip { font-size: 0.68rem; font-weight: 900; color: #0f1f16; position: absolute; top: -18px; left: 50%; transform: translateX(-50%); white-space: nowrap; }
.bar { width: 100%; border-radius: 4px 4px 0 0; min-height: 2px; transition: height 0.4s ease; }
.bar-calving  { background: #31572c; }
.bar-heat     { background: #ef4444; }
.bar-breed    { background: #3b82f6; }
.bar-preg     { background: #22c55e; }
.bar-dry      { background: #f59e0b; }
.bar-sold     { background: #8b5cf6; }
.bar-implanted { background: #00c853; }
.bar-failed   { background: #f44336; }
.bar-successful { background: #2196f3; }
.bar-pair { flex-direction: column; justify-content: flex-end; }
.bar-pair-inner { display: flex; align-items: flex-end; gap: 2px; height: 100%; width: 100%; justify-content: center; }
.bar-pair-inner .bar { width: calc(50% - 1px); }
.bar-label { font-size: 0.62rem; font-weight: 700; color: #8a9b8e; text-align: center; white-space: nowrap; }
.analytics-loading { display: grid; gap: 14px; }
.chart-skeleton { height: 180px; border-radius: 10px; background: linear-gradient(90deg, #f0f4f1 25%, #e8ede9 50%, #f0f4f1 75%); background-size: 200% 100%; animation: shimmer 1.4s infinite; }
@keyframes shimmer { 0% { background-position: 200% 0; } 100% { background-position: -200% 0; } }

@media (max-width: 640px) {
  .browse-filters { grid-template-columns: 1fr; }
  .emb-grid { grid-template-columns: 1fr; }
  .emb-full { grid-column: 1; }
  .browse-grid { grid-template-columns: 1fr; max-height: none; }
  .rp-row-card { grid-template-columns: 1fr; }
  .rp-full { grid-column: 1; }
  .rp-tabs button { padding: 12px 12px 9px; font-size: 0.75rem; }
}
</style>
