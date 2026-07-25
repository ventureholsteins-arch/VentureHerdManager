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
      <button class="back" type="button" @click="router.push('/')">← Dashboard</button>
      <h1>Reports & Show Planner</h1>
      <p>Build lineups, working lists, show checklists, embryo inventory, and achievements in one place.</p>
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
.reports-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 18px;
}

.hero {
  border: 1px solid #d9e3dc;
  background: linear-gradient(165deg, #f6fbf7, #edf5ef);
  border-radius: 12px;
  padding: 18px;
  margin-bottom: 12px;
}

.hero h1 {
  margin: 8px 0;
  font-size: 2rem;
  color: #163322;
}

.hero p {
  margin: 0;
  color: #4d6171;
}

.back {
  border: 1px solid #31572c;
  background: #fff;
  color: #31572c;
  font-weight: 800;
  border-radius: 8px;
  padding: 8px 12px;
}

.tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}

.tabs button {
  border: 1px solid #c7d4ca;
  background: #fff;
  color: #1f2937;
  border-radius: 8px;
  min-height: 42px;
  padding: 0 14px;
  font-weight: 800;
}

.tabs button.active {
  border-color: #31572c;
  background: #31572c;
  color: #fff;
}

.panel {
  border: 1px solid #d9e3dc;
  border-radius: 12px;
  background: #fff;
  padding: 16px;
}

.panel h2 {
  margin: 0;
  color: #163322;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.panel-header button {
  border: none;
  background: #31572c;
  color: #fff;
  border-radius: 8px;
  min-height: 40px;
  padding: 0 12px;
  font-weight: 800;
}

.row-card,
.group-card {
  border: 1px solid #e0e7e2;
  border-radius: 10px;
  padding: 12px;
  margin: 10px 0;
  display: grid;
  gap: 10px;
}

label {
  display: grid;
  gap: 6px;
  color: #1f2937;
  font-weight: 700;
}

input,
select,
textarea {
  min-height: 42px;
  border: 1px solid #c8d4cb;
  border-radius: 8px;
  padding: 8px 10px;
  font-size: 1rem;
}

textarea {
  min-height: 78px;
}

.chip-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.chip {
  border: 1px solid #c8d4cb;
  border-radius: 999px;
  background: #fff;
  color: #1f2937;
  min-height: 36px;
  padding: 0 12px;
  font-weight: 700;
}

.chip.selected {
  border-color: #31572c;
  background: #e8f5ea;
  color: #17331f;
}

.checklist {
  display: grid;
  gap: 8px;
}

.check-item {
  display: grid;
  grid-template-columns: 30px 1fr;
  align-items: center;
  gap: 8px;
}

.check-item input[type='checkbox'] {
  width: 22px;
  height: 22px;
  margin: 0;
}

.check-item input[type='text'] {
  min-height: 42px;
}

.danger {
  border: none;
  background: #991b1b;
  color: #fff;
  border-radius: 8px;
  min-height: 40px;
  padding: 0 12px;
  font-weight: 800;
  justify-self: start;
}

.empty {
  border: 1px dashed #c8d4cb;
  border-radius: 8px;
  padding: 14px;
  color: #5b6b78;
}

.hint {
  color: #5b6b78;
  margin: 6px 0 10px;
}

@media (max-width: 760px) {
  .tabs button {
    flex: 1 1 calc(50% - 8px);
  }
}
</style>
