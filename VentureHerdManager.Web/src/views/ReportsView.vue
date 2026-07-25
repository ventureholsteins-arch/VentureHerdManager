<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import { getAnimals } from '../api/animals'
import type { Animal } from '../models/Animal'
import { formatCurrentAge, getShowClassLabel } from '../utils/showClasses'

type HubTab =
  | 'showString'
  | 'lists'
  | 'checklist'
  | 'embryos'
  | 'achievements'

interface AnimalGroupList {
  key: string
  title: string
  animalIds: number[]
  notes: string
}

interface ShowStringRow {
  id: number
  animalId: number | null
  lineupOrder: number
  feedNotes: string
  ringDirections: string
}

interface ChecklistItem {
  id: number
  text: string
  done: boolean
}

interface EmbryoRecord {
  id: number
  code: string
  sire: string
  donor: string
  grade: string
  status: 'In Storage' | 'Assigned' | 'Implanted'
  recipientAnimalId: number | null
  linkedBreedingNote: string
  notes: string
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
const activeTab = ref<HubTab>('showString')
const loading = ref(true)
const animals = ref<Animal[]>([])

const listStorageKey = 'venture-herd-reports-lists-v1'
const showStringStorageKey = 'venture-herd-reports-show-string-v1'
const checklistStorageKey = 'venture-herd-reports-checklist-v1'
const embryoStorageKey = 'venture-herd-reports-embryos-v1'
const achievementsStorageKey = 'venture-herd-reports-achievements-v1'

const defaultLists: AnimalGroupList[] = [
  { key: 'show-string', title: 'Show String', animalIds: [], notes: '' },
  { key: 'flush-candidates', title: 'Flush Candidates', animalIds: [], notes: '' },
  { key: 'donor-cows', title: 'Donor Cows', animalIds: [], notes: '' },
  { key: 'recipients', title: 'Recipients', animalIds: [], notes: '' },
  { key: 'sale-animals', title: 'Sale Animals', animalIds: [], notes: '' },
  { key: 'vet-check', title: 'Vet Check', animalIds: [], notes: '' }
]

const defaultChecklist: ChecklistItem[] = [
  { id: 1, text: 'Show halters', done: false },
  { id: 2, text: 'Feed buckets & water tubs', done: false },
  { id: 3, text: 'Bedding and straw', done: false },
  { id: 4, text: 'Paperwork and registrations', done: false },
  { id: 5, text: 'Clippers, blades, adhesives', done: false },
  { id: 6, text: 'Treatments and first aid', done: false }
]

const groupLists = ref<AnimalGroupList[]>([...defaultLists])
const showStringRows = ref<ShowStringRow[]>([])
const checklistItems = ref<ChecklistItem[]>([...defaultChecklist])
const embryoRecords = ref<EmbryoRecord[]>([])
const achievements = ref<AchievementRecord[]>([])

const nextRowId = ref(1)
const nextEmbryoId = ref(1)
const nextAchievementId = ref(1)

const animalOptions = computed(() => {
  return [...animals.value].sort((a, b) => {
    const aName = (a.barnName || a.registeredName || '').toLowerCase()
    const bName = (b.barnName || b.registeredName || '').toLowerCase()
    return aName.localeCompare(bName)
  })
})

const showStringSorted = computed(() => {
  return [...showStringRows.value].sort((a, b) => a.lineupOrder - b.lineupOrder)
})

function getAnimalLabel(animalId: number | null): string {
  if (!animalId) return 'Unassigned'

  const animal = animals.value.find(item => item.animalId === animalId)
  if (!animal) return `Animal #${animalId}`

  const name = animal.barnName || animal.registeredName || `Animal #${animal.animalId}`
  const age = formatCurrentAge(animal.birthDate)
  const showClass = getShowClassLabel(animal.birthDate, animal.animalStage)

  return `${name} · ${age} · ${showClass}`
}

function parseStoredData<T>(value: string | null, fallback: T): T {
  if (!value) return fallback

  try {
    return JSON.parse(value) as T
  } catch {
    return fallback
  }
}

function loadLocalData() {
  groupLists.value = parseStoredData(localStorage.getItem(listStorageKey), [...defaultLists])
  showStringRows.value = parseStoredData(localStorage.getItem(showStringStorageKey), [])
  checklistItems.value = parseStoredData(localStorage.getItem(checklistStorageKey), [...defaultChecklist])
  embryoRecords.value = parseStoredData(localStorage.getItem(embryoStorageKey), [])
  achievements.value = parseStoredData(localStorage.getItem(achievementsStorageKey), [])

  nextRowId.value = Math.max(1, ...showStringRows.value.map(item => item.id + 1), 1)
  nextEmbryoId.value = Math.max(1, ...embryoRecords.value.map(item => item.id + 1), 1)
  nextAchievementId.value = Math.max(1, ...achievements.value.map(item => item.id + 1), 1)
}

function saveLocalData() {
  localStorage.setItem(listStorageKey, JSON.stringify(groupLists.value))
  localStorage.setItem(showStringStorageKey, JSON.stringify(showStringRows.value))
  localStorage.setItem(checklistStorageKey, JSON.stringify(checklistItems.value))
  localStorage.setItem(embryoStorageKey, JSON.stringify(embryoRecords.value))
  localStorage.setItem(achievementsStorageKey, JSON.stringify(achievements.value))
}

watch(
  [groupLists, showStringRows, checklistItems, embryoRecords, achievements],
  saveLocalData,
  { deep: true }
)

function toggleAnimalInList(listKey: string, animalId: number) {
  const list = groupLists.value.find(item => item.key === listKey)
  if (!list) return

  if (list.animalIds.includes(animalId)) {
    list.animalIds = list.animalIds.filter(id => id !== animalId)
  } else {
    list.animalIds.push(animalId)
  }
}

function addShowStringRow() {
  showStringRows.value.push({
    id: nextRowId.value++,
    animalId: null,
    lineupOrder: showStringRows.value.length + 1,
    feedNotes: '',
    ringDirections: ''
  })
}

function removeShowStringRow(id: number) {
  showStringRows.value = showStringRows.value.filter(item => item.id !== id)
}

function addChecklistItem() {
  checklistItems.value.push({
    id: Date.now(),
    text: 'New checklist item',
    done: false
  })
}

function addEmbryoRecord() {
  embryoRecords.value.push({
    id: nextEmbryoId.value++,
    code: '',
    sire: '',
    donor: '',
    grade: '',
    status: 'In Storage',
    recipientAnimalId: null,
    linkedBreedingNote: '',
    notes: ''
  })
}

function removeEmbryoRecord(id: number) {
  embryoRecords.value = embryoRecords.value.filter(item => item.id !== id)
}

function addAchievement() {
  achievements.value.push({
    id: nextAchievementId.value++,
    animalId: null,
    showName: '',
    showDate: '',
    bagged: '',
    placed: '',
    notes: ''
  })
}

function removeAchievement(id: number) {
  achievements.value = achievements.value.filter(item => item.id !== id)
}

onMounted(async () => {
  loadLocalData()

  try {
    animals.value = await getAnimals()
  } catch (error) {
    console.error('Failed to load animals for reports hub:', error)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <main class="reports-page">
    <header class="hero">
      <div class="hero-top">
        <button class="back" type="button" @click="router.push('/')">← Dashboard</button>
        <span style="color:#22c55e;font-size:0.75rem;font-weight:900;letter-spacing:0.12em;text-transform:uppercase;">Venture Herd Manager</span>
      </div>
      <h1>Reports &amp; Show Planner</h1>
      <p class="hero-sub">Lineups · Working Lists · Show Checklist · Embryo Inventory · Achievements</p>
    </header>

    <section class="tabs">
      <button type="button" :class="{ active: activeTab === 'showString' }" @click="activeTab = 'showString'">Show String</button>
      <button type="button" :class="{ active: activeTab === 'lists' }" @click="activeTab = 'lists'">Herd Lists</button>
      <button type="button" :class="{ active: activeTab === 'checklist' }" @click="activeTab = 'checklist'">Show Checklist</button>
      <button type="button" :class="{ active: activeTab === 'embryos' }" @click="activeTab = 'embryos'">Embryo Inventory</button>
      <button type="button" :class="{ active: activeTab === 'achievements' }" @click="activeTab = 'achievements'">Achievements</button>
    </section>

    <section v-if="loading" class="panel"><p>Loading animals...</p></section>

    <section v-else-if="activeTab === 'showString'" class="panel">
      <div class="panel-header">
        <h2>Show String Lineup</h2>
        <button type="button" @click="addShowStringRow">+ Add Row</button>
      </div>

      <div v-if="showStringSorted.length === 0" class="empty">No lineup yet. Add your first row.</div>

      <div v-for="row in showStringSorted" :key="row.id" class="row-card">
        <label>
          Lineup Order
          <input v-model.number="row.lineupOrder" type="number" min="1">
        </label>

        <label>
          Animal (show age emphasized)
          <select v-model.number="row.animalId">
            <option :value="null">Select animal</option>
            <option v-for="animal in animalOptions" :key="animal.animalId" :value="animal.animalId">
              {{ getAnimalLabel(animal.animalId) }}
            </option>
          </select>
        </label>

        <label>
          Feeding Notes
          <textarea v-model="row.feedNotes" rows="2" placeholder="Show-week feed setup" />
        </label>

        <label>
          Ring Directions
          <textarea v-model="row.ringDirections" rows="2" placeholder="Clipping, lead side, prep cues" />
        </label>

        <button type="button" class="danger" @click="removeShowStringRow(row.id)">Remove Row</button>
      </div>
    </section>

    <section v-else-if="activeTab === 'lists'" class="panel">
      <h2>Working Herd Lists</h2>
      <p class="hint">Create and maintain list groups: show string, flush candidates, donor cows, recipients, sale animals, vet check.</p>

      <div v-for="list in groupLists" :key="list.key" class="group-card">
        <h3>{{ list.title }}</h3>

        <div class="chip-row">
          <button
            v-for="animal in animalOptions"
            :key="`${list.key}-${animal.animalId}`"
            type="button"
            class="chip"
            :class="{ selected: list.animalIds.includes(animal.animalId) }"
            @click="toggleAnimalInList(list.key, animal.animalId)"
          >
            {{ animal.barnName || animal.registeredName || `#${animal.animalId}` }}
          </button>
        </div>

        <label>
          Notes
          <textarea v-model="list.notes" rows="2" placeholder="Extra details, sorting notes, instructions" />
        </label>
      </div>
    </section>

    <section v-else-if="activeTab === 'checklist'" class="panel">
      <div class="panel-header">
        <h2>Show Supplies Checklist</h2>
        <button type="button" @click="addChecklistItem">+ Item</button>
      </div>

      <div class="checklist">
        <label v-for="item in checklistItems" :key="item.id" class="check-item">
          <input v-model="item.done" type="checkbox">
          <input v-model="item.text" type="text">
        </label>
      </div>
    </section>

    <section v-else-if="activeTab === 'embryos'" class="panel">
      <div class="panel-header">
        <h2>Embryo Inventory</h2>
        <button type="button" @click="addEmbryoRecord">+ Embryo Record</button>
      </div>
      <p class="hint">Track recipient assignment and link embryo movement notes to breeding workflows.</p>

      <div v-if="embryoRecords.length === 0" class="empty">No embryo records yet.</div>

      <div v-for="record in embryoRecords" :key="record.id" class="row-card">
        <label>
          Embryo Code
          <input v-model="record.code" type="text" placeholder="ET-2026-001">
        </label>

        <label>
          Sire
          <input v-model="record.sire" type="text" placeholder="Sire name">
        </label>

        <label>
          Donor Cow
          <input v-model="record.donor" type="text" placeholder="Donor animal">
        </label>

        <label>
          Grade
          <input v-model="record.grade" type="text" placeholder="Grade 1, 2, etc.">
        </label>

        <label>
          Status
          <select v-model="record.status">
            <option value="In Storage">In Storage</option>
            <option value="Assigned">Assigned</option>
            <option value="Implanted">Implanted</option>
          </select>
        </label>

        <label>
          Recipient
          <select v-model.number="record.recipientAnimalId">
            <option :value="null">No recipient yet</option>
            <option v-for="animal in animalOptions" :key="`recipient-${animal.animalId}`" :value="animal.animalId">
              {{ getAnimalLabel(animal.animalId) }}
            </option>
          </select>
        </label>

        <label>
          Linked Breeding Note
          <input v-model="record.linkedBreedingNote" type="text" placeholder="Link to breeding event note/date">
        </label>

        <label>
          Notes
          <textarea v-model="record.notes" rows="2" placeholder="Storage tank, transfer notes, vet notes" />
        </label>

        <button type="button" class="danger" @click="removeEmbryoRecord(record.id)">Remove Record</button>
      </div>
    </section>

    <section v-else class="panel">
      <div class="panel-header">
        <h2>Show Achievements</h2>
        <button type="button" @click="addAchievement">+ Achievement</button>
      </div>
      <p class="hint">Keep this at the bottom and out of the way, but available when needed.</p>

      <div v-if="achievements.length === 0" class="empty">No achievements logged yet.</div>

      <div v-for="record in achievements" :key="record.id" class="row-card">
        <label>
          Animal
          <select v-model.number="record.animalId">
            <option :value="null">Select animal</option>
            <option v-for="animal in animalOptions" :key="`achievement-${animal.animalId}`" :value="animal.animalId">
              {{ getAnimalLabel(animal.animalId) }}
            </option>
          </select>
        </label>

        <label>
          Show Name
          <input v-model="record.showName" type="text" placeholder="Spring Classic Show">
        </label>

        <label>
          Show Date
          <input v-model="record.showDate" type="date">
        </label>

        <label>
          Bagged
          <input v-model="record.bagged" type="text" placeholder="How she bagged up">
        </label>

        <label>
          Placement
          <input v-model="record.placed" type="text" placeholder="1st Jr 2 and Reserve Champion">
        </label>

        <label>
          Notes
          <textarea v-model="record.notes" rows="2" placeholder="Judge comments, prep notes" />
        </label>

        <button type="button" class="danger" @click="removeAchievement(record.id)">Remove</button>
      </div>
    </section>
  </main>
</template>

<style scoped>
/* ── Base ── */
.reports-page {
  max-width: 1240px;
  margin: 0 auto;
  padding: 0 0 60px;
  font-family: 'Bahnschrift', 'Arial Narrow', 'Segoe UI', sans-serif;
  background: #f5f7f2;
  min-height: 100vh;
}

/* ── Hero header ── */
.hero {
  position: relative;
  background: linear-gradient(135deg, #0f2318 0%, #1a3d22 60%, #244f2f 100%);
  padding: 22px 24px 18px;
  border-bottom: 3px solid #31572c;
}

.hero-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.hero h1 {
  margin: 0;
  font-size: 1.85rem;
  font-weight: 900;
  color: #fff;
  letter-spacing: -0.02em;
  text-transform: uppercase;
}

.hero-sub {
  margin: 6px 0 0;
  color: rgba(255,255,255,0.6);
  font-size: 0.88rem;
  letter-spacing: 0.03em;
}

.back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 1px solid rgba(255,255,255,0.22);
  background: rgba(255,255,255,0.07);
  color: #e2e8f0;
  font-weight: 800;
  font-size: 0.85rem;
  letter-spacing: 0.04em;
  border-radius: 6px;
  padding: 8px 14px;
  cursor: pointer;
  transition: background 0.15s;
}

.back:hover {
  background: rgba(255,255,255,0.14);
}

/* ── Tab rail ── */
.tabs {
  display: flex;
  overflow-x: auto;
  gap: 0;
  background: #fff;
  border-bottom: 2px solid #e0e8e1;
  padding: 0 16px;
}

.tabs::-webkit-scrollbar { height: 0; }

.tabs button {
  flex-shrink: 0;
  border: none;
  border-bottom: 3px solid transparent;
  background: transparent;
  color: #5d6f63;
  font-weight: 800;
  font-size: 0.82rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  padding: 14px 18px 11px;
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s;
  white-space: nowrap;
}

.tabs button:hover {
  color: #0f1f16;
}

.tabs button.active {
  color: #31572c;
  border-bottom-color: #31572c;
}

/* ── Panel wrapper ── */
.panel {
  margin: 20px 16px;
  background: #fff;
  border: 1px solid #d9e3dc;
  border-radius: 12px;
  padding: 20px;
}

/* ── Panel header ── */
.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
  padding-bottom: 14px;
  border-bottom: 1px solid #e0e8e1;
}

.panel-header h2 {
  margin: 0;
  color: #0f1f16;
  font-size: 1.1rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.panel-header button,
.add-btn {
  border: none;
  background: #31572c;
  color: #fff;
  border-radius: 6px;
  min-height: 38px;
  padding: 0 14px;
  font-weight: 900;
  font-size: 0.85rem;
  letter-spacing: 0.04em;
  cursor: pointer;
  transition: background 0.15s;
}

.panel-header button:hover,
.add-btn:hover {
  background: #254520;
}

/* ── Row card (show string / embryo / achievement) ── */
.row-card {
  background: #f8fbf8;
  border: 1px solid #e0e8e1;
  border-left: 3px solid #31572c;
  border-radius: 8px;
  padding: 16px;
  margin: 10px 0;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.row-card label:first-child {
  grid-column: 1;
}

.row-card .danger {
  grid-column: 1 / -1;
}

/* full-width fields */
.row-card label:has(textarea),
.row-card label:has([placeholder*='Note']),
.row-card label:has([placeholder*='Direction']),
.row-card label:has([placeholder*='Feeding']) {
  grid-column: 1 / -1;
}

/* ── Group card (herd lists) ── */
.group-card {
  background: #f8fbf8;
  border: 1px solid #e0e8e1;
  border-radius: 8px;
  padding: 16px;
  margin: 12px 0;
}

.group-card h3 {
  margin: 0 0 12px;
  color: #31572c;
  font-size: 0.8rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.1em;
}

/* ── Labels / inputs ── */
label {
  display: grid;
  gap: 6px;
  color: #5d6f63;
  font-weight: 700;
  font-size: 0.8rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

input,
select,
textarea {
  min-height: 44px;
  border: 1px solid #c8d4cb;
  border-radius: 6px;
  padding: 10px 12px;
  font-size: 0.98rem;
  font-family: inherit;
  background: #fff;
  color: #0f1f16;
  transition: border-color 0.15s;
}

input:focus,
select:focus,
textarea:focus {
  outline: none;
  border-color: #31572c;
  box-shadow: 0 0 0 3px rgba(49, 87, 44, 0.1);
}

input::placeholder,
textarea::placeholder {
  color: #9caba2;
}

textarea {
  min-height: 82px;
  resize: vertical;
}

/* ── Chip buttons ── */
.chip-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}

.chip {
  border: 1px solid #c8d4cb;
  border-radius: 999px;
  background: #fff;
  color: #5d6f63;
  min-height: 34px;
  padding: 0 14px;
  font-weight: 700;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all 0.15s;
}

.chip:hover {
  border-color: #31572c;
  color: #0f1f16;
}

.chip.selected {
  border-color: #31572c;
  background: #e8f5ea;
  color: #17331f;
  font-weight: 900;
}

/* ── Checklist ── */
.checklist {
  display: grid;
  gap: 6px;
}

.check-item {
  display: grid;
  grid-template-columns: 28px 1fr;
  align-items: center;
  gap: 10px;
  padding: 6px 8px;
  border-radius: 6px;
  background: #f8fbf8;
  border: 1px solid #e0e8e1;
}

.check-item input[type='checkbox'] {
  width: 20px;
  height: 20px;
  margin: 0;
  accent-color: #31572c;
}

.check-item input[type='text'] {
  min-height: 40px;
  border: none;
  background: transparent;
  color: #0f1f16;
}

.check-item input[type='text']:focus {
  outline: none;
  border-bottom: 1px solid #31572c;
  border-radius: 0;
}

/* ── Danger button ── */
.danger {
  border: 1px solid #fca5a5;
  background: #fff1f2;
  color: #991b1b;
  border-radius: 6px;
  min-height: 36px;
  padding: 0 12px;
  font-weight: 800;
  font-size: 0.8rem;
  letter-spacing: 0.04em;
  cursor: pointer;
  justify-self: start;
  transition: background 0.15s;
}

.danger:hover {
  background: #fee2e2;
  border-color: #f87171;
}

/* ── Empty / hint ── */
.empty {
  border: 1px dashed #c8d4cb;
  border-radius: 8px;
  padding: 24px;
  color: #8a9b8e;
  text-align: center;
  font-size: 0.92rem;
}

.hint {
  color: #5d6f63;
  margin: 0 0 14px;
  font-size: 0.87rem;
  line-height: 1.5;
}

/* ── Status badge ── */
.status-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 900;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.status-badge.storage { background: #1e3a5f; color: #60a5fa; }
.status-badge.assigned { background: #3b1d7f; color: #a78bfa; }
.status-badge.implanted { background: #14331e; color: #22c55e; }

/* ── Responsive ── */
@media (max-width: 760px) {
  .hero {
    padding: 16px;
  }

  .hero h1 {
    font-size: 1.35rem;
  }

  .tabs button {
    padding: 12px 14px 9px;
    font-size: 0.75rem;
  }

  .row-card {
    grid-template-columns: 1fr;
  }

  .row-card label:first-child,
  .row-card .danger {
    grid-column: 1;
  }

  .panel {
    margin: 12px 10px;
    padding: 14px;
  }
}
</style>
