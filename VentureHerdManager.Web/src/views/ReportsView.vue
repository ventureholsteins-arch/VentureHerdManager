<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { getAnimals } from '../api/animals'
import { getHerdActivity, getEmbryoImplants, type HerdActivityResponse, type EmbryoImplantResponse } from '../api/analytics'
import { createAchievement, deleteAchievement, getAllAchievements, updateAchievement, type ShowAchievement } from '../api/showAchievements'
import {
  getAllEmbryos,
  createEmbryo,
  createEmbryoBatch,
  updateEmbryo,
  deleteEmbryo,
  implantEmbryo,
  undoEmbryoImplant,
  recordEmbryoOutcome,
  type EmbryoRecord as ApiEmbryoRecord
} from '../api/embryoRecords'
import {
  applyPcdartImport,
  previewPcdartImport,
  type PcdartImportResult
} from '../api/pcdartImport'
import { ensureDemo } from '../api/demo'
import type { Animal } from '../models/Animal'
import { formatCurrentAge, getShowClassLabel } from '../utils/showClasses'
import HerdLoadingScene from '../components/HerdLoadingScene.vue'
import RetroIcon from '../components/RetroIcon.vue'
import { getHerdDataAnalytics } from '../api/herdData'
import { getLatestBaggingSchedule, saveBaggingSchedule } from '../api/baggingSchedules'

type HubTab = 'analytics' | 'embryos' | 'embryoImplants' | 'showString' | 'showBagging' | 'lists' | 'checklist' | 'pcdartImport' | 'achievements'
type ReportCategory = 'decisions' | 'embryos' | 'shows' | 'data'

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

interface ShowBaggingQuarter {
  key: 'frontLeft' | 'frontRight' | 'rearLeft' | 'rearRight'
  label: string
  hoursBeforeRing: number | null
  milkOutTime?: string
}

interface ShowBaggingRow {
  id: number
  animalId: number | null
  lineupOrder: number
  showName: string
  showDate: string
  wasSuccessful: boolean
  entryTime: string
  notes: string
  quarters: ShowBaggingQuarter[]
  remindersEnabled?: boolean
  showAchievementId?: number
}

interface ChecklistItem {
  id: number
  text: string
  done: boolean
}

export interface EmbryoRecord {
  id: number
  embryoRecordId?: number
  breedingEventId?: number | null
  createdAt?: string
  updatedAt?: string
  code: string
  sire: string
  donor: string
  donorAnimalId?: number | null
  mating: string
  groupName: string
  grade: string
  status: 'In Storage' | 'Assigned' | 'Implanted' | 'Failed' | 'Confirmed Pregnant' | 'Calved / Completed'
  recipientAnimalId: number | null
  recipientName?: string | null
  implantDate: string
  pregnancyStatus?: number | null
  pregnancyCheckDate?: string | null
  pregnancyCheckDueDate?: string | null
  linkedBreedingNote: string
  failureNotes: string
  notes: string
  collectionLocation: string
  storageLocation: string
}

interface AchievementRecord {
  id: number
  animalId: number | null
  showName: string
  showDate: string
  bagged: string
  placed: string
  notes: string
  showAchievementId?: number
}

const router = useRouter()
const route = useRoute()
const isDemoOnly = import.meta.env.VITE_DEMO_ONLY === 'true'
const activeTab = ref<HubTab>('embryos')
const activeCategory = ref<ReportCategory>('embryos')
const loading = ref(true)
const animals = ref<Animal[]>([])
const attentionSummary = ref<any>(null)
const attentionTotal = computed(() => attentionSummary.value ? Object.values(attentionSummary.value).reduce((sum: number, rows: any) => sum + (rows?.length ?? 0), 0) : 0)
const pageMode = computed(() => route.path === '/embryos' ? 'embryos' : route.path === '/shows' ? 'shows' : 'reports')
const pageTitle = computed(() => pageMode.value === 'embryos' ? 'Embryo Hatchery' : pageMode.value === 'shows' ? 'Show Command Center' : 'Reports & Analytics')
const pageSubtitle = computed(() => pageMode.value === 'embryos'
  ? 'Nest inventory · implants · outcomes'
  : pageMode.value === 'shows' ? 'Moo Squadron online · string · bagging · results' : 'Live herd stats · breeding · milk · genomics · decisions')
watch(pageMode, mode => selectReportCategory(mode === 'embryos' ? 'embryos' : mode === 'shows' ? 'shows' : 'decisions'))

function categoryForTab(tab: HubTab): ReportCategory {
  if (tab === 'embryos' || tab === 'embryoImplants') return 'embryos'
  if (tab === 'showString' || tab === 'showBagging' || tab === 'achievements' || tab === 'checklist') return 'shows'
  if (tab === 'pcdartImport') return 'data'
  return 'decisions'
}

function selectReportCategory(category: ReportCategory) {
  activeCategory.value = category
  activeTab.value = category === 'decisions' ? 'analytics'
    : category === 'embryos' ? 'embryos'
      : category === 'shows' ? 'showString'
        : 'pcdartImport'
}

function selectReportTab(tab: HubTab) {
  activeTab.value = tab
  activeCategory.value = categoryForTab(tab)
}

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
const showStringShareStatus = ref('')
const showBaggingSearch = ref('')
const showBaggingShowName = ref('')
const showBaggingShowDate = ref(new Date().toISOString().slice(0, 10))
const showBaggingStartTime = ref(toLocalDateTimeInput(new Date()))
const showBaggingPhoneNumbers = ref('')
const showBaggingRows = ref<ShowBaggingRow[]>([])
const achievementSearch = ref('')
const baggingHistorySearch = ref('')
const baggingHistoryGroupFilter = ref('')
const baggingHistoryGroupOnly = ref(false)
const baggingSimpleMode = ref(true)
const baggingShareStatus = ref('')
const achievementsShareStatus = ref('')
const pcdartRawText = ref('')
const pcdartReportLabel = ref(`PCDART Monthly ${new Date().toLocaleDateString()}`)
const pcdartFileName = ref('')
const pcdartImporting = ref(false)
const pcdartResult = ref<PcdartImportResult | null>(null)
const pcdartError = ref('')
const pcdartMappingKey = 'venture-herd-pcdart-animal-mappings-v1'
const pcdartMappings = ref<Record<string, number>>(
  JSON.parse(localStorage.getItem(pcdartMappingKey) || '{}')
)
const pcdartApplySuggested = ref(true)
const embryoLoadError = ref('')
const embryoActionId = ref<number | null>(null)
const baggingActionStatus = ref('')
const baggingScheduleId = ref<number | undefined>()
const baggingSaving = ref(false)
const reportsLoadError = ref('')
const analyticsLoaded = ref(false)
const embryoImplantsLoaded = ref(false)
const baggingPlanLoaded = ref(false)

const listKey = 'venture-herd-lists-v2'
const showStringKey = 'venture-herd-show-string-v2'
const showBaggingKey = 'venture-herd-show-bagging-v1'
const showBaggingMetaKey = 'venture-herd-show-bagging-meta-v1'
const checklistKey = 'venture-herd-checklist-v1'
const embryoKey = 'venture-herd-embryos-v2'
const achievementsKey = 'venture-herd-achievements-v1'

const defaultLists: AnimalGroupList[] = [
  { key: 'show-string', title: 'Show String 3', animalIds: [], notes: '', searchQuery: '' },
  { key: 'sale-animals', title: 'Sale Animals', animalIds: [], notes: '', searchQuery: '' },
  { key: 'recipients', title: 'Recipients', animalIds: [], notes: '', searchQuery: '' },
  { key: 'flush-candidates', title: 'Flush Candidates', animalIds: [], notes: '', searchQuery: '' },
  { key: 'donor-cows', title: 'Donor Cows', animalIds: [], notes: '', searchQuery: '' },
  { key: 'vet-check', title: 'Vet Check', animalIds: [], notes: '', searchQuery: '' },
  { key: 'health-paper-group', title: 'Health Paper Group', animalIds: [], notes: '', searchQuery: '' },
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
const achievements = ref<AchievementRecord[]>([])

const nextRowId = ref(1)
const nextBaggingRowId = ref(1)
const nextEmbryoId = ref(1)
const showEmbryoCreate = ref(false)
const embryoCreateGroup = ref('')
const embryoCreateDonor = ref('')
const embryoCreateDonorAnimalId = ref<number | null>(null)
const embryoCreateSire = ref('')
const embryoCreateGrade = ref('')
const embryoCreateQuantity = ref(1)
const embryoCreateSaving = ref(false)
const nextAchievementId = ref(1)

const herdListOrder = defaultLists.map(list => list.key)

const quarterTemplates: Omit<ShowBaggingQuarter, 'hoursBeforeRing'>[] = [
  { key: 'frontLeft', label: 'Front Left' },
  { key: 'frontRight', label: 'Front Right' },
  { key: 'rearLeft', label: 'Rear Left' },
  { key: 'rearRight', label: 'Rear Right' }
]

function toLocalDateTimeInput(value: string | Date): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offsetMs = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

function toIsoFromInput(value: string): string | null {
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return null
  return parsed.toISOString()
}

function addHoursToInput(value: string, hours: number): string {
  const parsed = toIsoFromInput(value)
  if (!parsed) return ''
  return toLocalDateTimeInput(new Date(new Date(parsed).getTime() + hours * 3_600_000))
}

function parseHoursDifference(targetIso: string): number | null {
  const target = new Date(targetIso)
  if (Number.isNaN(target.getTime())) return null
  return (target.getTime() - Date.now()) / 3_600_000
}

function formatHoursDifference(hours: number | null): string {
  if (hours === null) return '—'
  const absolute = Math.abs(hours)
  if (absolute < 0.05) return 'now'
  const rounded = absolute < 10 ? absolute.toFixed(1) : Math.round(absolute).toString()
  return hours >= 0 ? `${rounded}h from now` : `${rounded}h ago`
}

function formatTime(value: string | null | undefined): string {
  if (!value) return '—'
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return '—'
  return parsed.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
}

function formatScheduleDateTime(value: string): string {
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return 'Time not set'
  return parsed.toLocaleString([], { weekday: 'short', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' })
}

function createDefaultBaggingQuarters(): ShowBaggingQuarter[] {
  return quarterTemplates.map(template => ({
    key: template.key,
    label: template.label,
    hoursBeforeRing: null
  }))
}

function normalizeBaggingRow(row: Partial<ShowBaggingRow>): ShowBaggingRow {
  const incomingQuarters = Array.isArray(row.quarters) ? row.quarters : []
  return {
    id: row.id ?? nextBaggingRowId.value++,
    animalId: row.animalId ?? null,
    lineupOrder: row.lineupOrder ?? 1,
    showName: row.showName ?? '',
    showDate: row.showDate ?? new Date().toISOString().slice(0, 10),
    wasSuccessful: row.wasSuccessful ?? false,
    entryTime: row.entryTime ?? '',
    notes: row.notes ?? '',
    showAchievementId: row.showAchievementId,
    remindersEnabled: row.remindersEnabled ?? true,
    quarters: quarterTemplates.map(template => {
      const found = incomingQuarters.find(quarter => quarter.key === template.key)
      return {
        key: template.key,
        label: template.label,
        hoursBeforeRing: found?.hoursBeforeRing ?? null,
        milkOutTime: found?.milkOutTime ?? ''
      }
    })
  }
}

function achievementFromApi(record: ShowAchievement): AchievementRecord {
  return {
    id: record.showAchievementId,
    animalId: record.animalId,
    showName: record.showName ?? '',
    showDate: record.showDate ?? '',
    bagged: record.bagged ?? '',
    placed: record.placed ?? '',
    notes: record.notes ?? '',
    showAchievementId: record.showAchievementId
  }
}

function parseStored<T>(k: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(k)
    return raw ? (JSON.parse(raw) as T) : fallback
  } catch {
    return fallback
  }
}

function normalizeGroupLists(stored: AnimalGroupList[]): AnimalGroupList[] {
  const existingByKey = new Map(stored.map(list => [list.key, list]))
  return defaultLists.map(list => {
    const existing = existingByKey.get(list.key)
    return {
      ...list,
      ...existing,
      title: list.title,
      searchQuery: existing?.searchQuery ?? ''
    }
  })
}

function loadData() {
  groupLists.value = normalizeGroupLists(parseStored<AnimalGroupList[]>(listKey, defaultLists.map(l => ({ ...l })))).map(l => ({ ...l, searchQuery: l.searchQuery ?? '' }))
  showStringRows.value = parseStored<ShowStringRow[]>(showStringKey, []).map(r => ({ ...r, feedRation: '' }))
  showBaggingRows.value = parseStored<ShowBaggingRow[]>(showBaggingKey, []).map(r => normalizeBaggingRow(r))
  const baggingMeta = parseStored<{ showName: string; showDate: string; showStartTime: string; phoneNumbers?: string }>(showBaggingMetaKey, {
    showName: '',
    showDate: new Date().toISOString().slice(0, 10),
    showStartTime: toLocalDateTimeInput(new Date())
  })
  showBaggingShowName.value = baggingMeta.showName
  showBaggingShowDate.value = baggingMeta.showDate
  showBaggingStartTime.value = baggingMeta.showStartTime
  showBaggingPhoneNumbers.value = baggingMeta.phoneNumbers ?? ''
  checklistItems.value = parseStored<ChecklistItem[]>(checklistKey, defaultChecklist.map(i => ({ ...i })))
  embryoRecords.value = parseStored<EmbryoRecord[]>(embryoKey, []).map(e => ({
    ...e,
    createdAt: e.createdAt,
    updatedAt: e.updatedAt,
    mating: e.mating || '',
    groupName: e.groupName || '',
    implantDate: e.implantDate || '',
    failureNotes: e.failureNotes || '',
    collectionLocation: e.collectionLocation || '',
    storageLocation: e.storageLocation || ''
  }))
  achievements.value = parseStored<AchievementRecord[]>(achievementsKey, [])
  nextRowId.value = Math.max(1, ...showStringRows.value.map(r => r.id + 1), 1)
  nextBaggingRowId.value = Math.max(1, ...showBaggingRows.value.map(r => r.id + 1), 1)
  nextEmbryoId.value = Math.max(1, ...embryoRecords.value.map(e => e.id + 1), 1)
  nextAchievementId.value = Math.max(1, ...achievements.value.map(a => a.id + 1), 1)
}

function statusFromApi(status: ApiEmbryoRecord['status']): EmbryoRecord['status'] {
  if (status === 1) return 'Assigned'
  if (status === 2) return 'Implanted'
  if (status === 3) return 'Failed'
  if (status === 4) return 'Confirmed Pregnant'
  if (status === 5) return 'Calved / Completed'
  return 'In Storage'
}

function statusToApi(status: EmbryoRecord['status']): ApiEmbryoRecord['status'] {
  if (status === 'Assigned') return 1
  if (status === 'Implanted') return 2
  if (status === 'Failed') return 3
  if (status === 'Confirmed Pregnant') return 4
  if (status === 'Calved / Completed') return 5
  return 0
}

function embryoFromApi(record: ApiEmbryoRecord): EmbryoRecord {
  return {
    id: record.embryoRecordId,
    embryoRecordId: record.embryoRecordId,
    createdAt: record.createdAt,
    updatedAt: record.updatedAt,
    code: record.code ?? '',
    sire: record.sire ?? '',
    donor: record.donor ?? '',
    donorAnimalId: record.donorAnimalId ?? null,
    mating: record.mating ?? '',
    groupName: record.groupName ?? '',
    grade: record.grade ?? '',
    status: statusFromApi(record.status),
    recipientAnimalId: record.recipientAnimalId,
    recipientName: record.recipientName ?? null,
    implantDate: record.implantDate ?? '',
    breedingEventId: record.breedingEventId ?? null,
    pregnancyStatus: record.pregnancyStatus ?? null,
    pregnancyCheckDate: record.pregnancyCheckDate ?? null,
    pregnancyCheckDueDate: record.pregnancyCheckDueDate ?? null,
    linkedBreedingNote: record.linkedBreedingNote ?? '',
    failureNotes: record.failureNotes ?? '',
    notes: record.notes ?? '',
    collectionLocation: record.collectionLocation ?? '',
    storageLocation: record.storageLocation ?? ''
  }
}

function embryoToApi(record: EmbryoRecord): Omit<ApiEmbryoRecord, 'embryoRecordId' | 'createdAt' | 'updatedAt'> {
  return {
    code: record.code || null,
    sire: record.sire || null,
    donor: record.donor || null,
    donorAnimalId: record.donorAnimalId ?? null,
    mating: record.mating || null,
    groupName: record.groupName || null,
    grade: record.grade || null,
    status: statusToApi(record.status),
    recipientAnimalId: record.recipientAnimalId,
    implantDate: record.implantDate || null,
    linkedBreedingNote: record.linkedBreedingNote || null,
    failureNotes: record.failureNotes || null,
    notes: record.notes || null,
    collectionLocation: record.collectionLocation || null,
    storageLocation: record.storageLocation || null
  }
}

async function loadRemoteEmbryos() {
  embryoLoadError.value = ''

  try {
    const remote = await getAllEmbryos()
    embryoRecords.value = remote.map(embryoFromApi)
    nextEmbryoId.value = Math.max(1, ...embryoRecords.value.map(record => record.id + 1), 1)
  } catch (error) {
    console.error('Failed to load embryo records:', error)
    embryoLoadError.value = 'Embryo records could not be loaded from the server. Existing local data is still shown if available.'
  }
}

async function loadRemoteAchievements() {
  try {
    const remote = await getAllAchievements()
    const merged = new Map<number, AchievementRecord>()

    for (const record of achievements.value) {
      merged.set(record.showAchievementId ?? record.id, record)
    }

    for (const record of remote.map(achievementFromApi)) {
      merged.set(record.showAchievementId ?? record.id, record)
    }

    achievements.value = Array.from(merged.values())
  } catch (error) {
    console.error('Failed to load show achievements:', error)
  }
}

function saveData() {
  localStorage.setItem(listKey, JSON.stringify(groupLists.value))
  localStorage.setItem(showStringKey, JSON.stringify(showStringRows.value))
  localStorage.setItem(showBaggingKey, JSON.stringify(showBaggingRows.value))
  localStorage.setItem(showBaggingMetaKey, JSON.stringify({
    showName: showBaggingShowName.value,
    showDate: showBaggingShowDate.value,
    showStartTime: showBaggingStartTime.value,
    phoneNumbers: showBaggingPhoneNumbers.value
  }))
  localStorage.setItem(checklistKey, JSON.stringify(checklistItems.value))
  localStorage.setItem(embryoKey, JSON.stringify(embryoRecords.value))
  localStorage.setItem(achievementsKey, JSON.stringify(achievements.value))
}

let saveDataTimer: number | null = null
function scheduleSaveData() {
  if (saveDataTimer !== null) {
    window.clearTimeout(saveDataTimer)
  }

  saveDataTimer = window.setTimeout(() => {
    saveData()
    saveDataTimer = null
  }, 200)
}

function openHealthPaperReport() {
  saveData()
  router.push('/reports/print?report=healthPapers')
}

watch([groupLists, showStringRows, showBaggingRows, showBaggingShowName, showBaggingShowDate, showBaggingStartTime, showBaggingPhoneNumbers, checklistItems, embryoRecords, achievements], scheduleSaveData, { deep: true })

watch(activeTab, async tab => {
  if (route.query.tab === tab) return
  await router.replace({ query: { ...route.query, tab } })
})

async function ensureTabData(tab: HubTab) {
  if (tab === 'analytics' && !analyticsLoaded.value) {
    await loadAnalytics()
    analyticsLoaded.value = true
    return
  }

  if (tab === 'embryoImplants' && !embryoImplantsLoaded.value) {
    await loadEmbryoImplants()
    embryoImplantsLoaded.value = true
    return
  }

  if (tab === 'showBagging' && !baggingPlanLoaded.value) {
    await loadSavedBaggingPlan()
    baggingPlanLoaded.value = true
  }
}

const animalOptions = computed(() =>
  [...animals.value].sort((a, b) =>
    (a.barnName || a.registeredName || '').localeCompare(b.barnName || b.registeredName || '')
  )
)

const showClassOptions = computed(() => {
  const oldestBirthByClass = new Map<string, string>()
  for (const a of animals.value) {
    const c = getShowClassLabel(a.birthDate, a.animalStage)
    if (!c || c === 'Class TBD') continue

    const birthDate = a.birthDate?.slice(0, 10)
    const oldest = oldestBirthByClass.get(c)
    if (birthDate && (!oldest || birthDate < oldest)) {
      oldestBirthByClass.set(c, birthDate)
    } else if (!oldestBirthByClass.has(c)) {
      oldestBirthByClass.set(c, '9999-12-31')
    }
  }
  return [...oldestBirthByClass.entries()]
    .sort((left, right) => left[1].localeCompare(right[1]) || left[0].localeCompare(right[0]))
    .map(([className]) => className)
})

const showStringBrowseAnimals = computed(() => {
  const query = showStringSearch.value.trim().toLowerCase()
  if (query.length < 2 && showStringClassFilter.value === 'all') {
    return []
  }

  let result = [...animalOptions.value]
  if (showStringClassFilter.value !== 'all') {
    result = result.filter(a => getShowClassLabel(a.birthDate, a.animalStage) === showStringClassFilter.value)
  }
  if (query.length >= 2) {
    result = result.filter(a => {
      const haystack = [
        a.barnName,
        a.registeredName,
        a.registrationNumber,
        a.sireName,
        a.damName,
        a.breed
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()

      return haystack.includes(query)
    })
  }

  return result.sort((a, b) => {
    if (!a.birthDate && !b.birthDate) return (a.barnName || a.registeredName || '').localeCompare(b.barnName || b.registeredName || '')
    if (!a.birthDate) return 1
    if (!b.birthDate) return -1
    return a.birthDate.localeCompare(b.birthDate)
  }).slice(0, showStringClassFilter.value === 'all' ? 12 : undefined)
})

const showBaggingBrowseAnimals = computed(() => {
  const q = showBaggingSearch.value.trim().toLowerCase()
  if (q.length < 2) return []
  return animalOptions.value
    .filter(a => {
      const haystack = [
        a.barnName,
        a.registeredName,
        a.registrationNumber,
        a.sireName,
        a.damName,
        a.breed
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()

      return haystack.includes(q)
    })
    .slice(0, 15)
})

const showBaggingMatchCount = computed(() => showBaggingBrowseAnimals.value.length)

const showBaggingRowsSorted = computed(() => [...showBaggingRows.value].sort((left, right) => left.lineupOrder - right.lineupOrder))

const baggingCowsByShowTime = computed(() => [...showBaggingRows.value].sort((left, right) => {
  const leftTime = new Date(left.entryTime).getTime()
  const rightTime = new Date(right.entryTime).getTime()
  return (Number.isNaN(leftTime) ? Number.MAX_SAFE_INTEGER : leftTime) - (Number.isNaN(rightTime) ? Number.MAX_SAFE_INTEGER : rightTime)
}))

const baggingTimeline = computed(() => {
  const grouped = new Map<string, { time: string; items: Array<{ rowId: number; cow: string; action: string }> }>()
  const addItem = (time: string, rowId: number, cow: string, action: string) => {
    if (!time || Number.isNaN(new Date(time).getTime())) return
    const key = toLocalDateTimeInput(time)
    const group = grouped.get(key) ?? { time: key, items: [] }
    group.items.push({ rowId, cow, action })
    grouped.set(key, group)
  }

  for (const row of showBaggingRowsSorted.value) {
    const cow = getBaggingRowAnimalLabel(row)
    for (const quarter of row.quarters) {
      const milkTime = quarter.milkOutTime || (quarter.hoursBeforeRing !== null ? addHoursToInput(row.entryTime, -quarter.hoursBeforeRing) : '')
      if (milkTime) addItem(milkTime, row.id, cow, `Milk ${quarter.label}`)
    }
    addItem(row.entryTime, row.id, cow, 'Goes into the ring')
  }

  return Array.from(grouped.values()).sort((left, right) => new Date(left.time).getTime() - new Date(right.time).getTime())
})

const baggingRowsGlance = computed(() =>
  showBaggingRowsSorted.value.map(row => ({
    ...row,
    groupLabel: row.showName?.trim() || showBaggingShowName.value.trim() || 'Ungrouped',
    entryLabel: formatTime(row.entryTime)
  }))
)

const showNameOptions = computed(() => {
  const names = new Set<string>()
  for (const record of achievements.value) {
    if (record.showName.trim()) {
      names.add(record.showName.trim())
    }
  }
  return Array.from(names).sort()
})

const baggingGroupOptions = computed(() =>
  showNameOptions.value.filter(name => name.trim().length > 0)
)

const achievementMatches = computed(() => {
  const q = achievementSearch.value.trim().toLowerCase()
  if (!q) return achievements.value
  return achievements.value.filter(record =>
    [record.showName, record.bagged, record.placed, record.notes, getAnimalLabel(record.animalId)]
      .join(' ')
      .toLowerCase()
      .includes(q)
  )
})

const achievementGroups = computed(() => {
  const grouped = new Map<string, AchievementRecord[]>()
  for (const record of achievementMatches.value) {
    const key = record.showName.trim() || 'Ungrouped'
    const list = grouped.get(key)
    if (list) list.push(record)
    else grouped.set(key, [record])
  }

  return Array.from(grouped.entries())
    .sort((left, right) => {
      if (left[0] === 'Ungrouped') return 1
      if (right[0] === 'Ungrouped') return -1
      return left[0].localeCompare(right[0])
    })
    .map(([name, records]) => ({ name, records }))
})

const baggingRowGroups = computed(() => {
  const grouped = new Map<string, ShowBaggingRow[]>()
  for (const record of showBaggingRowsSorted.value) {
    const key = record.showName.trim() || showBaggingShowName.value.trim() || 'Ungrouped'
    const list = grouped.get(key)
    if (list) list.push(record)
    else grouped.set(key, [record])
  }

  return Array.from(grouped.entries())
    .sort((left, right) => {
      if (left[0] === 'Ungrouped') return 1
      if (right[0] === 'Ungrouped') return -1
      return left[0].localeCompare(right[0])
    })
    .map(([name, records]) => ({ name, records }))
})

const baggingHistoryMatches = computed(() => {
  const search = baggingHistorySearch.value.trim().toLowerCase()
  const selectedHistoryGroup = baggingHistoryGroupFilter.value.trim().toLowerCase()

  return achievements.value
    .filter(record => {
      const hasBaggingHistory =
        record.bagged.trim().length > 0
        || record.notes.trim().length > 0

      if (!hasBaggingHistory) {
        return false
      }

      const recordGroup = record.showName.trim()
      const haystack = [
        recordGroup,
        getAnimalLabel(record.animalId),
        record.showDate,
        record.bagged,
        record.placed,
        record.notes
      ]
        .join(' ')
        .toLowerCase()

      const matchesSearch = !search || haystack.includes(search)
      const matchesGroup =
        !baggingHistoryGroupOnly.value
        || !selectedHistoryGroup
        || recordGroup.toLowerCase().includes(selectedHistoryGroup)

      return matchesSearch && matchesGroup
    })
    .sort((left, right) => {
      const byDate = (right.showDate || '').localeCompare(left.showDate || '')
      if (byDate !== 0) return byDate
      return right.id - left.id
    })
})

const showBaggingStartHoursFromNow = computed(() => parseHoursDifference(toIsoFromInput(showBaggingStartTime.value) ?? ''))

const showStringSorted = computed(() => [...showStringRows.value].sort((a, b) => a.lineupOrder - b.lineupOrder))
const showStringTopThreeAnimalIds = computed(() =>
  showStringSorted.value
    .map(row => row.animalId)
    .filter((animalId): animalId is number => animalId !== null)
    .slice(0, 3)
)
const orderedGroupLists = computed(() =>
  [...groupLists.value].sort((left, right) => herdListOrder.indexOf(left.key) - herdListOrder.indexOf(right.key))
)

// Split lineup into Cows (stage 3/4 or show class has "Cow") and Heifers/Youngstock
const showStringCows = computed(() =>
  showStringSorted.value.filter(row => {
    if (!row.animalId) return false
    const a = animals.value.find(x => x.animalId === row.animalId)
    if (!a) return false
    return a.animalStage === 3 || a.animalStage === 4 ||
      getShowClassLabel(a.birthDate, a.animalStage).includes('Cow') ||
      getShowClassLabel(a.birthDate, a.animalStage).includes('Cow')
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
function compareEmbryos(left: EmbryoRecord, right: EmbryoRecord): number {
  const leftGroup = left.groupName.trim().toLowerCase()
  const rightGroup = right.groupName.trim().toLowerCase()
  if (leftGroup !== rightGroup) {
    return leftGroup.localeCompare(rightGroup)
  }

  const leftDate = left.createdAt ? Date.parse(left.createdAt) : 0
  const rightDate = right.createdAt ? Date.parse(right.createdAt) : 0
  if (leftDate !== rightDate) {
    return rightDate - leftDate
  }

  return right.id - left.id
}

const embryosActive = computed(() =>
  embryoRecords.value
    .filter(record => {
      const hasImplantHistory = Boolean(record.implantDate)
        || Boolean(record.breedingEventId)
        || record.status === 'Implanted'
        || record.status === 'Failed'
        || record.status === 'Confirmed Pregnant'
        || record.status === 'Calved / Completed'

      return (record.status === 'In Storage' || record.status === 'Assigned')
        && !hasImplantHistory
    })
    .sort(compareEmbryos)
)

const embryosActiveGroups = computed(() => {
  const grouped = new Map<string, EmbryoRecord[]>()

  for (const record of embryosActive.value) {
    const name = record.groupName.trim() || 'Ungrouped'
    const list = grouped.get(name)
    if (list) {
      list.push(record)
    } else {
      grouped.set(name, [record])
    }
  }

  return Array.from(grouped.entries())
    .sort((left, right) => {
      if (left[0] === 'Ungrouped') return 1
      if (right[0] === 'Ungrouped') return -1
      return left[0].localeCompare(right[0])
    })
    .map(([name, records]) => ({ name, records }))
})

const embryosFailed = computed(() =>
  embryoRecords.value
    .filter(e => e.status === 'Failed')
    .sort(compareEmbryos)
)
const embryosWithImplants = computed(() =>
  embryoRecords.value
    .filter(record =>
      Boolean(record.implantDate)
      || Boolean(record.breedingEventId)
      || record.status === 'Implanted'
      || record.status === 'Failed'
      || record.status === 'Confirmed Pregnant'
      || record.status === 'Calved / Completed')
    .sort((left, right) => (right.implantDate || '').localeCompare(left.implantDate || ''))
)
const hasNoAnimals = computed(() => animals.value.length === 0)

function getAnimalLabel(animalId: number | null): string {
  if (!animalId) return 'Unassigned'
  const a = animals.value.find(x => x.animalId === animalId)
  if (!a) return `Animal #${animalId}`
  return `${a.barnName || a.registeredName || `#${a.animalId}`} · ${formatCurrentAge(a.birthDate)} · ${getShowClassLabel(a.birthDate, a.animalStage)}`
}

function getAnimalById(animalId: number | null): Animal | undefined {
  if (!animalId) return undefined
  return animals.value.find(animal => animal.animalId === animalId)
}

function getCurrentWorkingPen(animalId: number | null): string {
  const animal = getAnimalById(animalId)
  if (!animal) return 'Unknown'
  return animal.herdLocation === 1 ? "At Mueller's" : 'Home herd'
}

function getShowStringPosition(animalId: number | null): number | null {
  if (!animalId) return null
  const index = showStringSorted.value.findIndex(row => row.animalId === animalId)
  return index >= 0 ? index + 1 : null
}

function getListAnimalIds(list: AnimalGroupList): number[] {
  if (list.key === 'show-string') {
    return showStringTopThreeAnimalIds.value
  }

  if (list.key === 'health-paper-group') {
    return [...list.animalIds].sort((leftId, rightId) => {
      const left = getAnimalById(leftId)
      const right = getAnimalById(rightId)
      if (!left?.birthDate && !right?.birthDate) {
        return (left?.barnName || left?.registeredName || '').localeCompare(right?.barnName || right?.registeredName || '')
      }
      if (!left?.birthDate) return 1
      if (!right?.birthDate) return -1
      return left.birthDate.localeCompare(right.birthDate)
    })
  }

  return list.animalIds
}

function isReadOnlyList(list: AnimalGroupList): boolean {
  return list.key === 'show-string'
}

function openAnimalFromReports(animalId: number) {
  router.push({
    name: 'animal',
    params: { animalId },
    query: { returnTo: route.fullPath }
  })
}

function getScoreLabel(score: number | null | undefined): string {
  if (!score) return ''
  if (score >= 90) return `EX ${Math.round(score)}`
  if (score >= 85) return `VG ${Math.round(score)}`
  return `GP ${Math.round(score)}`
}

function formatShowBirthDate(value: string | null | undefined): string {
  if (!value) return 'Birth date missing'
  const [year, month, day] = value.slice(0, 10).split('-').map(Number)
  if (!year || !month || !day) return value
  return `Born ${month}/${day}/${year}`
}

function barPct(value: number, allValues: number[]): number {
  const max = Math.max(...allValues, 1)
  if (max === 0) return 0
  return Math.max(Math.round((value / max) * 100), value > 0 ? 4 : 0)
}

function filteredListAnimals(list: AnimalGroupList): Animal[] {
  if (isReadOnlyList(list)) return []
  const q = (list.searchQuery || '').trim().toLowerCase()
  if (q.length < 2) return []
  return animalOptions.value
    .filter(a => [a.barnName, a.registeredName, a.registrationNumber, a.sireName, a.damName]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
      .includes(q))
    .slice(0, 15)
}

function isAnimalInShowString(animalId: number): boolean {
  return showStringRows.value.some(r => r.animalId === animalId)
}

function addToShowString(animal: Animal) {
  showStringRows.value.push({ id: nextRowId.value++, animalId: animal.animalId, lineupOrder: showStringRows.value.length + 1, feedNotes: '', feedRation: '', ringDirections: '' })
}

function addShowBaggingRow(animal: Animal) {
  const existing = showBaggingRows.value.find(row => row.animalId === animal.animalId)
  if (existing) {
    baggingActionStatus.value = `${animal.barnName || animal.registeredName || `#${animal.animalId}`} is already in bagging rows.`
    nextTick(() => jumpToBaggingRow(existing.id))
    return
  }

  const entryTime = ''
  const quarters = createDefaultBaggingQuarters()
  const rowId = nextBaggingRowId.value++

  showBaggingRows.value.push({
    id: rowId,
    animalId: animal.animalId,
    lineupOrder: showBaggingRows.value.length + 1,
    showName: showBaggingShowName.value,
    showDate: showBaggingShowDate.value,
    wasSuccessful: false,
    entryTime,
    notes: '',
    quarters,
    remindersEnabled: true
  })

  baggingActionStatus.value = `Added ${animal.barnName || animal.registeredName || `#${animal.animalId}`} to bagging rows.`
  showBaggingSearch.value = ''
  nextTick(() => jumpToBaggingRow(rowId))

}

function addBlankBaggingRow() {
  showBaggingRows.value.push({
    id: nextBaggingRowId.value++,
    animalId: null,
    lineupOrder: showBaggingRows.value.length + 1,
    showName: showBaggingShowName.value,
    showDate: showBaggingShowDate.value,
    wasSuccessful: false,
    entryTime: '',
    notes: '',
    quarters: createDefaultBaggingQuarters(),
    remindersEnabled: true
  })
}

function removeShowBaggingRow(id: number) {
  showBaggingRows.value = showBaggingRows.value.filter(row => row.id !== id)
}

function getBaggingRowAnimalLabel(row: ShowBaggingRow): string {
  return row.animalId ? getAnimalLabel(row.animalId) : 'Unassigned'
}

function getQuarterMilkTime(entryTime: string, hoursBeforeRing: number | null): string {
  if (hoursBeforeRing === null) return '—'
  return formatTime(addHoursToInput(entryTime, -hoursBeforeRing))
}

function quarterShortLabel(key: ShowBaggingQuarter['key']): string {
  if (key === 'frontLeft') return 'FL'
  if (key === 'frontRight') return 'FR'
  if (key === 'rearLeft') return 'RL'
  return 'RR'
}

function baggingRowAnchorId(rowId: number): string {
  return `bagging-row-${rowId}`
}

async function jumpToBaggingRow(rowId: number): Promise<void> {
  const element = document.getElementById(baggingRowAnchorId(rowId))
  if (!element) return
  document.querySelectorAll<HTMLDetailsElement>('.cow-bagging-details[open]').forEach(details => {
    if (details !== element) details.open = false
  })
  if (element instanceof HTMLDetailsElement) element.open = true
  await nextTick()
  element.scrollIntoView({ behavior: 'smooth', block: 'start' })
  element.classList.add('quick-edit-opened')
  window.setTimeout(() => element.classList.remove('quick-edit-opened'), 900)
}

function editQuarterHours(row: ShowBaggingRow, quarterKey: ShowBaggingQuarter['key']) {
  const quarter = row.quarters.find(q => q.key === quarterKey)
  if (!quarter) return

  const current = quarter.hoursBeforeRing === null ? '' : String(quarter.hoursBeforeRing)
  const next = window.prompt(`Hours before ring for ${quarter.label}`, current)
  if (next === null) return

  if (next.trim() === '') {
    quarter.hoursBeforeRing = null
    return
  }

  const parsed = Number(next)
  if (Number.isNaN(parsed) || parsed < 0) {
    alert('Enter a valid number of hours.')
    return
  }

  quarter.hoursBeforeRing = parsed
}

function baggingSummary(row: ShowBaggingRow): string {
  const parts = row.quarters
    .filter(quarter => Boolean(quarter.milkOutTime) || quarter.hoursBeforeRing !== null)
    .map(quarter => `${quarter.label}: ${quarter.milkOutTime ? formatTime(quarter.milkOutTime) : getQuarterMilkTime(row.entryTime, quarter.hoursBeforeRing)}`)

  return parts.length > 0 ? parts.join(' · ') : 'No milk-out times entered'
}

function baggingDetailNotes(row: ShowBaggingRow): string {
  const quarterLines = row.quarters
    .map(quarter => {
      const milkTime = quarter.milkOutTime
        ? formatTime(quarter.milkOutTime)
        : getQuarterMilkTime(row.entryTime, quarter.hoursBeforeRing)
      return `${quarter.label}: milk at ${milkTime}`
    })
    .join('\n')

  return [
    `Cow goes into ring: ${formatTime(row.entryTime)}`,
    `15-minute reminders: ${row.remindersEnabled ? 'On' : 'Off'}`,
    quarterLines,
    row.notes.trim() ? `Notes: ${row.notes.trim()}` : ''
  ].filter(Boolean).join('\n')
}

async function saveBaggingRow(row: ShowBaggingRow) {
  if (!row.animalId) {
    alert('Select a cow before saving bagging.')
    return
  }
  if (!row.entryTime || Number.isNaN(new Date(row.entryTime).getTime())) {
    alert(`Enter the separate show time for ${getBaggingRowAnimalLabel(row)} before saving.`)
    return
  }

  await saveWholeBaggingPlan()
}

async function saveWholeBaggingPlan() {
  if (baggingSaving.value) return
  if (!showBaggingRows.value.length) { alert('Add at least one cow first.'); return }
  const invalid = showBaggingRows.value.find(row => !row.animalId || !row.entryTime || Number.isNaN(new Date(row.entryTime).getTime()))
  if (invalid) { alert(`Choose a cow and show time for ${getBaggingRowAnimalLabel(invalid)}.`); return }
  baggingSaving.value = true
  try {
    const saved = await saveBaggingSchedule({
      sharedBaggingScheduleId: baggingScheduleId.value,
      showName: showBaggingShowName.value.trim() || 'Show Bagging',
      showDate: showBaggingShowDate.value || new Date().toISOString().slice(0, 10),
      scheduleJson: JSON.stringify({ showStartTime: showBaggingStartTime.value, phoneNumbers: showBaggingPhoneNumbers.value, rows: showBaggingRows.value })
    })
    baggingScheduleId.value = saved.sharedBaggingScheduleId
    baggingActionStatus.value = `Saved ${showBaggingRows.value.length} cow${showBaggingRows.value.length === 1 ? '' : 's'} together.`
  } catch (error) {
    console.error('Failed to save bagging plan:', error)
    alert(error instanceof Error ? error.message : 'Bagging plan could not be saved.')
  } finally { baggingSaving.value = false }
}

async function loadSavedBaggingPlan() {
  const saved = await getLatestBaggingSchedule()
  if (!saved) return
  baggingScheduleId.value = saved.sharedBaggingScheduleId
  if (showBaggingRows.value.length) return
  const plan = JSON.parse(saved.scheduleJson)
  showBaggingShowName.value = saved.showName
  showBaggingShowDate.value = saved.showDate
  showBaggingStartTime.value = plan.showStartTime || showBaggingStartTime.value
  showBaggingPhoneNumbers.value = plan.phoneNumbers || ''
  showBaggingRows.value = (plan.rows || []).map((row: ShowBaggingRow) => normalizeBaggingRow(row))
}

async function shareShowStringLink() {
  const resolved = router.resolve({
    name: 'shows',
    query: { tab: 'showString' }
  })
  const shareUrl = `${window.location.origin}${resolved.href}`

  try {
    if (navigator.share) {
      await navigator.share({
        title: 'Show String Lineup',
        text: 'Open the show string lineup for Venture Herd Manager.',
        url: shareUrl
      })
      showStringShareStatus.value = 'Share dialog opened.'
      return
    }

    await navigator.clipboard.writeText(shareUrl)
    showStringShareStatus.value = 'Show string link copied.'
  } catch (error) {
    console.error('Failed to share show string link:', error)
    showStringShareStatus.value = shareUrl
  }
}

async function shareBaggingLink() {
  const resolved = router.resolve({
    name: 'shows',
    query: {
      tab: 'showBagging',
      group: showBaggingShowName.value.trim() || undefined,
      baggingSearch: baggingHistorySearch.value.trim() || undefined
    }
  })
  const shareUrl = `${window.location.origin}${resolved.href}`

  try {
    if (navigator.share) {
      await navigator.share({
        title: 'Bagging Group Planner',
        text: 'Open the bagging group planner and history in Venture Herd Manager.',
        url: shareUrl
      })
      baggingShareStatus.value = 'Share dialog opened.'
      return
    }

    await navigator.clipboard.writeText(shareUrl)
    baggingShareStatus.value = 'Bagging planner link copied.'
  } catch (error) {
    console.error('Failed to share bagging link:', error)
    baggingShareStatus.value = shareUrl
  }
}

function textBaggingTeam() {
  const numbers = showBaggingPhoneNumbers.value
    .split(/[;,\n]+/)
    .map(value => value.replace(/[^\d+]/g, ''))
    .filter(Boolean)
  if (numbers.length === 0) {
    baggingShareStatus.value = 'Enter at least one phone number for this show.'
    return
  }
  const resolved = router.resolve({ name: 'shows', query: { tab: 'showBagging', group: showBaggingShowName.value.trim() || undefined } })
  const shareUrl = `${window.location.origin}${resolved.href}`
  const nextCow = showBaggingRowsSorted.value[0]
  const message = [
    showBaggingShowName.value.trim() || 'Show bagging plan',
    nextCow ? `First cow: ${getBaggingRowAnimalLabel(nextCow)} at ${formatTime(nextCow.entryTime)}` : '',
    shareUrl
  ].filter(Boolean).join('\n')
  window.location.href = `sms:${numbers.join(',')}?&body=${encodeURIComponent(message)}`
  baggingShareStatus.value = `Text prepared for ${numbers.length} contact${numbers.length === 1 ? '' : 's'}.`
}

async function shareAchievementsLink() {
  const resolved = router.resolve({
    name: 'shows',
    query: {
      tab: 'achievements',
      q: achievementSearch.value.trim() || undefined
    }
  })
  const shareUrl = `${window.location.origin}${resolved.href}`

  try {
    if (navigator.share) {
      await navigator.share({
        title: 'Bagging History Results',
        text: 'Open filtered bagging and achievement history in Venture Herd Manager.',
        url: shareUrl
      })
      achievementsShareStatus.value = 'Share dialog opened.'
      return
    }

    await navigator.clipboard.writeText(shareUrl)
    achievementsShareStatus.value = 'History link copied.'
  } catch (error) {
    console.error('Failed to share achievements link:', error)
    achievementsShareStatus.value = shareUrl
  }
}

function openBaggingHistoryForGroup(groupName: string) {
  const trimmed = groupName.trim()
  if (!trimmed) {
    return
  }

  baggingSimpleMode.value = false
  baggingHistorySearch.value = trimmed
  baggingHistoryGroupFilter.value = trimmed
  baggingHistoryGroupOnly.value = true
}

function addShowStringRow() {
  showStringRows.value.push({ id: nextRowId.value++, animalId: null, lineupOrder: showStringRows.value.length + 1, feedNotes: '', feedRation: '', ringDirections: '' })
}

function removeShowStringRow(id: number) { showStringRows.value = showStringRows.value.filter(r => r.id !== id) }

function toggleAnimalInList(key: string, animalId: number) {
  const list = groupLists.value.find(l => l.key === key)
  if (!list) return
  if (list.key === 'show-string') return
  if (list.animalIds.includes(animalId)) list.animalIds = list.animalIds.filter(id => id !== animalId)
  else list.animalIds.push(animalId)
}

function addHealthPaperAnimalAndContinue(list: AnimalGroupList, animalId: number, event: MouseEvent) {
  const group = (event.currentTarget as HTMLElement | null)?.closest('.rp-group')
  toggleAnimalInList(list.key, animalId)
  list.searchQuery = ''
  nextTick(() => group?.querySelector<HTMLInputElement>('.rp-list-search')?.focus())
}

function addChecklistItem() { checklistItems.value.push({ id: Date.now(), text: 'New item', done: false }) }

async function addEmbryoRecord() {
  const groupName = embryoCreateGroup.value.trim()
  if (!groupName) {
    alert('Group name is required to create embryos in a batch.')
    return
  }
  const quantity = Number(embryoCreateQuantity.value)
  if (!Number.isFinite(quantity) || quantity < 1 || quantity > 100) {
    alert('Quantity must be a whole number between 1 and 100.')
    return
  }

  const linkedDonor = embryoCreateDonorAnimalId.value
    ? animals.value.find(animal => animal.animalId === embryoCreateDonorAnimalId.value)
    : null
  const donorName = embryoCreateDonor.value.trim()
    || linkedDonor?.barnName
    || linkedDonor?.registeredName
    || ''
  const draft: EmbryoRecord = {
    id: nextEmbryoId.value++,
    code: '',
    sire: embryoCreateSire.value.trim(),
    donor: donorName,
    donorAnimalId: embryoCreateDonorAnimalId.value,
    mating: donorName && embryoCreateSire.value.trim()
      ? `${donorName} x ${embryoCreateSire.value.trim()}`
      : groupName,
    groupName,
    grade: embryoCreateGrade.value.trim(),
    status: 'In Storage',
    recipientAnimalId: null,
    implantDate: '',
    linkedBreedingNote: '',
    failureNotes: '',
    notes: '',
    collectionLocation: '',
    storageLocation: ''
  }

  try {
    embryoCreateSaving.value = true
    const created = await createEmbryoBatch(embryoToApi(draft), quantity)
    embryoRecords.value = [
      ...created.map(item => embryoFromApi(item)),
      ...embryoRecords.value
    ]
    showEmbryoCreate.value = false
    embryoCreateGroup.value = ''
    embryoCreateDonor.value = ''
    embryoCreateDonorAnimalId.value = null
    embryoCreateSire.value = ''
    embryoCreateGrade.value = ''
    embryoCreateQuantity.value = 1
  } catch (error) {
    console.error('Failed to create embryo batch:', error)
    alert('Failed to create embryo batch.')
  } finally {
    embryoCreateSaving.value = false
  }
}

async function saveEmbryoRecord(rec: EmbryoRecord) {
  try {
    if (rec.embryoRecordId) {
      await updateEmbryo(rec.embryoRecordId, embryoToApi(rec))
    } else {
      const created = await createEmbryo(embryoToApi(rec))
      rec.embryoRecordId = created.embryoRecordId
      rec.id = created.embryoRecordId
    }
  } catch (error) {
    console.error('Failed to save embryo record:', error)
    alert('Failed to save embryo record.')
  }
}

function applyEmbryoFromApi(target: EmbryoRecord, source: ApiEmbryoRecord) {
  Object.assign(target, embryoFromApi(source))
}

async function markEmbryoImplanted(rec: EmbryoRecord) {
  if (!rec.recipientAnimalId) {
    alert('Select a recipient first.')
    return
  }

  if (!rec.implantDate) {
    rec.implantDate = new Date().toISOString().slice(0, 10)
  }

  try {
    if (!rec.embryoRecordId) {
      const created = await createEmbryo(embryoToApi(rec))
      rec.embryoRecordId = created.embryoRecordId
      rec.id = created.embryoRecordId
    }

    const implanted = await implantEmbryo(
      rec.embryoRecordId,
      rec.recipientAnimalId,
      rec.implantDate
    )

    applyEmbryoFromApi(rec, implanted)
    await Promise.all([
      loadRemoteEmbryos(),
      loadEmbryoImplants()
    ])
    embryoImplantsLoaded.value = true
    activeTab.value = 'embryoImplants'
    activeCategory.value = 'embryos'
  } catch (error) {
    console.error('Failed to implant embryo:', error)
    alert('Failed to mark embryo as implanted.')
  }
}

async function markEmbryoLost(rec: EmbryoRecord) {
  if (!rec.embryoRecordId) {
    alert('Save this embryo before recording outcome.')
    return
  }

  if (!rec.recipientAnimalId || !rec.implantDate) {
    alert('Implant this embryo first, then record that it did not stick.')
    return
  }

  embryoActionId.value = rec.embryoRecordId

  try {
    if (rec.status !== 'Implanted') {
      const implanted = await implantEmbryo(
        rec.embryoRecordId,
        rec.recipientAnimalId,
        rec.implantDate
      )
      applyEmbryoFromApi(rec, implanted)
    }

    const updated = await recordEmbryoOutcome(
      rec.embryoRecordId,
      false,
      rec.failureNotes || rec.notes || ''
    )
    applyEmbryoFromApi(rec, updated)
    await Promise.all([
      loadRemoteEmbryos(),
      loadEmbryoImplants()
    ])
  } catch (error) {
    console.error('Failed to record embryo loss:', error)
    alert(error instanceof Error
      ? error.message
      : 'Failed to record that this embryo did not stick.')
  } finally {
    embryoActionId.value = null
  }
}

async function markEmbryoSuccessful(rec: EmbryoRecord) {
  if (!rec.embryoRecordId || !rec.recipientAnimalId || !rec.implantDate) {
    alert('A linked recipient and implant date are required before confirming pregnancy.')
    return
  }

  embryoActionId.value = rec.embryoRecordId
  try {
    const updated = await recordEmbryoOutcome(
      rec.embryoRecordId,
      true,
      'Pregnancy confirmed from embryo transfer.'
    )
    applyEmbryoFromApi(rec, updated)
    await Promise.all([loadRemoteEmbryos(), loadEmbryoImplants()])
  } catch (error) {
    console.error('Failed to confirm embryo pregnancy:', error)
    alert(error instanceof Error ? error.message : 'Failed to confirm embryo pregnancy.')
  } finally {
    embryoActionId.value = null
  }
}

async function markEmbryoBackToStorage(rec: EmbryoRecord) {
  if (!rec.embryoRecordId) {
    rec.status = 'In Storage'
    await saveEmbryoRecord(rec)
    return
  }

  try {
    const reset = await undoEmbryoImplant(rec.embryoRecordId)
    applyEmbryoFromApi(rec, reset)
  } catch (error) {
    console.error('Failed to return embryo to storage:', error)
    alert('Failed to return embryo to storage.')
  }
}

async function removeEmbryoRecord(id: number) {
  const record = embryoRecords.value.find(item => item.id === id)
  if (record?.embryoRecordId) {
    try {
      await deleteEmbryo(record.embryoRecordId)
    } catch (error) {
      console.error('Failed to delete embryo record:', error)
      alert('Failed to delete embryo record.')
      return
    }
  }

  embryoRecords.value = embryoRecords.value.filter(e => e.id !== id)
}

async function loadPcdartFile(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  pcdartFileName.value = file.name
  pcdartRawText.value = await file.text()
}

async function runPcdartImport(apply: boolean) {
  if (!pcdartRawText.value.trim()) {
    alert('Paste or upload a PCDART report first.')
    return
  }

  pcdartImporting.value = true
  pcdartError.value = ''
  pcdartResult.value = null

  try {
    const payload = {
      rawText: pcdartRawText.value,
      reportLabel: pcdartReportLabel.value,
      applySuggestedChanges: pcdartApplySuggested.value,
      animalMappings: pcdartMappings.value,
      createMissingAnimals: false
    }

    pcdartResult.value = apply
      ? await applyPcdartImport(payload)
      : await previewPcdartImport(payload)
    if (apply) {
      localStorage.setItem(pcdartMappingKey, JSON.stringify(pcdartMappings.value))
    }
  } catch (error) {
    pcdartError.value = error instanceof Error ? error.message : 'Import failed.'
  } finally {
    pcdartImporting.value = false
  }
}

function pcdartAnimalSuggestions(reportName: string): Animal[] {
  const normalize = (value: string | null | undefined) => (value || '').toLowerCase().replace(/[^a-z0-9]/g, '')
  const source = normalize(reportName)
  return animalOptions.value
    .map(animal => {
      const values = [animal.barnName, animal.registeredName, animal.registrationNumber].map(normalize).filter(Boolean)
      const exact = values.some(value => value === source)
      const partial = values.some(value => value.includes(source) || source.includes(value))
      return { animal, score: exact ? 2 : partial ? 1 : 0 }
    })
    .sort((left, right) => right.score - left.score || getAnimalLabel(left.animal.animalId).localeCompare(getAnimalLabel(right.animal.animalId)))
    .slice(0, 12)
    .map(item => item.animal)
}

function addAchievement() {
  achievements.value.push({ id: nextAchievementId.value++, animalId: null, showName: '', showDate: '', bagged: '', placed: '', notes: '' })
}

async function reloadReportsData() {
  loading.value = true
  embryoLoadError.value = ''
  embryoImplantsError.value = ''
  analyticsError.value = ''
  reportsLoadError.value = ''

  if (isDemoOnly) {
    try {
      await ensureDemo()
    } catch (error) {
      console.error('Demo seed check failed:', error)
      reportsLoadError.value =
        'Demo seed check failed. Your local planner data is still available.'
    }
  }

  try {
    animals.value = await getAnimals()
    const activeIds = new Set(animals.value.map(animal => animal.animalId))
    showBaggingRows.value = showBaggingRows.value.filter(row => !row.animalId || activeIds.has(row.animalId))
  } catch (error) {
    console.error('Failed to load animals during refresh:', error)
    const message = error instanceof Error
      ? error.message
      : 'Herd data could not be loaded from the API.'
    reportsLoadError.value =
      `${message} Local planner data remains available while the API recovers.`
  }

  const results = await Promise.allSettled([
    loadRemoteEmbryos(),
    loadRemoteAchievements()
  ])

  analyticsLoaded.value = false
  embryoImplantsLoaded.value = false
  baggingPlanLoaded.value = false

  const failed = results
    .filter((result): result is PromiseRejectedResult =>
      result.status === 'rejected')

  if (failed.length > 0) {
    console.error('One or more report loaders failed:', failed)
    if (!reportsLoadError.value) {
      reportsLoadError.value =
        'Some live sections failed to refresh. Saved planner data is still loaded.'
    }
  }

  loading.value = false
}

function openAchievementsForShow(showName: string) {
  activeTab.value = 'achievements'
  achievementSearch.value = showName
}

async function saveAchievement(record: AchievementRecord) {
  if (!record.animalId) {
    alert('Select a cow before saving the achievement.')
    return
  }

  const payload = {
    animalId: record.animalId,
    showName: record.showName.trim(),
    showDate: record.showDate || new Date().toISOString().slice(0, 10),
    bagged: record.bagged.trim(),
    placed: record.placed.trim(),
    notes: record.notes.trim()
  }

  try {
    if (record.showAchievementId) {
      await updateAchievement(record.showAchievementId, payload)
    } else {
      const created = await createAchievement(payload)
      record.showAchievementId = created.showAchievementId
      record.id = created.showAchievementId
    }

    const existingIndex = achievements.value.findIndex(item => item.id === record.id)
    if (existingIndex === -1) {
      achievements.value.unshift({ ...record })
    } else {
      achievements.value[existingIndex] = { ...record }
    }

    activeTab.value = 'achievements'
    achievementSearch.value = record.showName
  } catch (error) {
    console.error('Failed to save achievement:', error)
    alert('Failed to save show achievement.')
  }
}

async function removeAchievement(id: number) {
  const record = achievements.value.find(item => item.id === id)
  if (record?.showAchievementId) {
    try {
      await deleteAchievement(record.showAchievementId)
    } catch (error) {
      console.error('Failed to delete achievement:', error)
      alert('Failed to delete show achievement.')
      return
    }
  }

  achievements.value = achievements.value.filter(a => a.id !== id)
}

onMounted(async () => {
  loadData()
  getHerdDataAnalytics().then(value => { attentionSummary.value = value.attention }).catch(() => {})
  if (pageMode.value === 'embryos') selectReportCategory('embryos')
  else if (pageMode.value === 'shows') selectReportCategory('shows')
  else selectReportCategory('decisions')
  const tabParam = route.query.tab as string | undefined
  if (tabParam && ['analytics', 'embryos', 'embryoImplants', 'showString', 'showBagging', 'lists', 'checklist', 'pcdartImport', 'achievements'].includes(tabParam)) {
    activeTab.value = tabParam as HubTab
    activeCategory.value = categoryForTab(activeTab.value)
  }
  const showParam = route.query.show as string | undefined
  const searchParam = route.query.q as string | undefined
  if (searchParam) {
    achievementSearch.value = searchParam
    if (activeTab.value !== 'achievements') {
      activeTab.value = 'achievements'
      activeCategory.value = 'shows'
    }
  } else if (showParam) {
    achievementSearch.value = showParam
    if (activeTab.value !== 'achievements') {
      activeTab.value = 'achievements'
      activeCategory.value = 'shows'
    }
  }
  const groupParam = route.query.group as string | undefined
  if (groupParam) {
    showBaggingShowName.value = groupParam
    if (activeTab.value !== 'showBagging') {
      activeTab.value = 'showBagging'
      activeCategory.value = 'shows'
    }
  }
  const baggingSearchParam = route.query.baggingSearch as string | undefined
  if (baggingSearchParam) {
    baggingHistorySearch.value = baggingSearchParam
    if (activeTab.value !== 'showBagging') {
      activeTab.value = 'showBagging'
      activeCategory.value = 'shows'
    }
  }
  await reloadReportsData()
  await ensureTabData(activeTab.value)
})

watch(activeTab, tab => {
  void ensureTabData(tab)
})
</script>

<template>
  <main class="rp" :class="{ 'show-command': pageMode === 'shows', 'embryo-hatchery': pageMode === 'embryos', 'analytics-sports': pageMode === 'reports' }">
    <header class="rp-hero">
      <div class="rp-hero-top">
        <button class="rp-back" type="button" @click="router.push('/')">← Dashboard</button>
        <div class="rp-hero-actions">
          <span class="rp-brand">Venture Herd Manager</span>
          <button class="rp-back rp-print-link" type="button" @click="router.push('/reports/print')">Print Reports</button>
        </div>
      </div>
      <h1 class="rp-title">{{ pageTitle }}</h1>
      <p class="rp-sub">{{ pageSubtitle }}</p>
      <p class="rp-powered">Powered by <strong>Venture Ag Marketing</strong> · Custom Application Solutions</p>
    </header>

    <nav v-if="pageMode === 'reports'" class="rp-categories" aria-label="Report categories">
      <button :class="{ active: activeCategory === 'decisions' }" @click="selectReportCategory('decisions')">Herd Decisions</button>
      <button :class="{ active: activeCategory === 'data' }" @click="selectReportCategory('data')">Imports &amp; Data</button>
    </nav>

    <nav class="rp-tabs" aria-label="Reports in selected category">
      <template v-if="activeCategory === 'decisions'">
        <button type="button" @click="router.push('/reports/herd-data?view=attention')"><RetroIcon name="note" :size="22" />Attention Lists</button>
        <button :class="{ active: activeTab === 'analytics' }" @click="selectReportTab('analytics')"><RetroIcon name="reports" :size="22" />Breeding Analytics</button>
        <button type="button" @click="router.push('/reports/herd-data?view=milk')"><RetroIcon name="reports" :size="22" />Milk Analytics</button>
        <button type="button" @click="router.push('/reports/herd-data?view=genomics')"><RetroIcon name="reports" :size="22" />Genomic Analytics</button>
        <button type="button" @click="router.push('/reports/herd-data?view=linear')"><RetroIcon name="reports" :size="22" />Farm Linear</button>
        <button :class="{ active: activeTab === 'lists' }" @click="selectReportTab('lists')"><RetroIcon name="note" :size="22" />Herd Lists</button>
      </template>
      <template v-else-if="activeCategory === 'embryos'">
        <button :class="{ active: activeTab === 'embryos' }" @click="selectReportTab('embryos')"><RetroIcon name="embryo" :size="22" />Inventory</button>
        <button :class="{ active: activeTab === 'embryoImplants' }" @click="selectReportTab('embryoImplants')"><RetroIcon name="pregCheck" :size="22" />Implants</button>
      </template>
      <template v-else-if="activeCategory === 'shows'">
        <button :class="{ active: activeTab === 'showString' }" @click="selectReportTab('showString')"><RetroIcon name="calf" :size="22" />Show String</button>
        <button :class="{ active: activeTab === 'showBagging' }" @click="selectReportTab('showBagging')"><RetroIcon name="calving" :size="22" />Bagging</button>
        <button :class="{ active: activeTab === 'checklist' }" @click="selectReportTab('checklist')"><RetroIcon name="note" :size="22" />Checklist</button>
        <button :class="{ active: activeTab === 'achievements' }" @click="selectReportTab('achievements')"><RetroIcon name="calf" :size="22" />Achievements</button>
      </template>
      <template v-else>
        <button type="button" @click="router.push('/reports/audit')"><RetroIcon name="reports" :size="22" />Audit Center</button>
        <button type="button" @click="router.push('/reports/herd-data?source=1')"><RetroIcon name="reports" :size="22" />Import PC-DART</button>
        <button type="button" @click="router.push('/reports/herd-data?view=imports&source=1&type=currentMilkingPdf')"><RetroIcon name="note" :size="22" />Current Milking PDF</button>
        <button type="button" @click="router.push('/reports/herd-data?view=imports&source=1&type=cowPagePdf')"><RetroIcon name="note" :size="22" />Individual Cow PDF</button>
        <button type="button" @click="router.push('/reports/herd-data?source=2')"><RetroIcon name="reports" :size="22" />Import Zoetis</button>
      </template>
    </nav>

    <button v-if="pageMode === 'reports' && attentionSummary" class="attention-ribbon" type="button" @click="router.push('/reports/herd-data?view=attention')"><span>⚠ ATTENTION BOARD</span><strong>{{ attentionTotal }} animals need review</strong><small>{{ attentionSummary.highDimOpen?.length ?? 0 }} high-DIM open · {{ attentionSummary.longOpenHeifers?.length ?? 0 }} long-open heifers · {{ attentionSummary.droppingMilk?.length ?? 0 }} milk drops · {{ attentionSummary.dryOffWatch?.length ?? 0 }} dry-off watch</small><b>Open lists →</b></button>

    <p v-if="reportsLoadError" class="rp-error" style="margin: 12px 16px 0;">{{ reportsLoadError }}</p>

    <section v-if="loading" class="rp-panel">
      <HerdLoadingScene message="Loading reports and herd data..." />
    </section>

    <!-- ANALYTICS -->
    <section v-else-if="activeTab === 'analytics'" class="rp-panel">
      <div class="rp-ph bagging-sticky-head">
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
        <div class="rp-ph-actions">
          <button type="button" class="rp-add-btn" @click="showEmbryoCreate = !showEmbryoCreate">+ Add Embryos</button>
        </div>
      </div>
      <p class="rp-hint">Track storage, assign recipients, and log outcomes. Use Did Not Stick to record a failed implant and move that embryo to the Failed section below.</p>

      <form v-if="showEmbryoCreate" class="emb-create-form" @submit.prevent="addEmbryoRecord">
        <label>Group name<input v-model="embryoCreateGroup" type="text" placeholder="Carissa x Braxton" required></label>
        <label>Donor in herd (optional)<select v-model.number="embryoCreateDonorAnimalId"><option :value="null">Type donor name instead</option><option v-for="a in animalOptions" :key="`donor-${a.animalId}`" :value="a.animalId">{{ a.barnName || a.registeredName || `#${a.animalId}` }}</option></select></label>
        <label>Donor / Dam<input v-model="embryoCreateDonor" type="text" placeholder="Carissa"></label>
        <label>Sire<input v-model="embryoCreateSire" type="text" placeholder="Braxton"></label>
        <label>Grade<input v-model="embryoCreateGrade" type="text" placeholder="Grade 1"></label>
        <label>Quantity<input v-model.number="embryoCreateQuantity" type="number" min="1" max="100" inputmode="numeric" required></label>
        <div class="emb-create-actions"><button class="rp-add-btn" type="submit" :disabled="embryoCreateSaving">{{ embryoCreateSaving ? 'Creating…' : `Create ${embryoCreateQuantity} in group` }}</button><button type="button" class="rp-x" @click="showEmbryoCreate = false">Cancel</button></div>
      </form>

      <div v-if="hasNoAnimals" class="rp-error">
        Herd data is empty right now, so embryos and bagging may look missing. Use Reload Data above.
      </div>

      <p v-if="embryoLoadError" class="rp-error">{{ embryoLoadError }}</p>

      <div v-if="embryosActive.length === 0 && embryosFailed.length === 0" class="rp-empty">No embryos found yet. Add your first record.</div>
      <div v-else-if="embryosActive.length === 0 && embryosFailed.length > 0" class="rp-empty">No embryos currently In Storage or Assigned. Check the Failed/Not Confirmed section below.</div>

      <template v-for="group in embryosActiveGroups" :key="`grp-${group.name}`">
        <details class="emb-group-details">
          <summary class="emb-group-title">{{ group.name }} ({{ group.records.length }})</summary>
          <div class="emb-group-body">
            <div v-for="rec in group.records" :key="rec.id" class="emb-card" :class="`emb-${rec.status.toLowerCase().replace(' ', '-')}`">
              <div class="emb-hd">
                <div class="emb-id">
                  <span class="emb-code">{{ rec.code || 'No Code' }}</span>
                  <span class="emb-badge" :class="`ebadge-${rec.status.toLowerCase().replace(' ', '-')}`">{{ rec.status }}</span>
                  <span v-if="rec.implantDate && rec.status === 'Implanted'" class="emb-date">{{ rec.implantDate }}</span>
                </div>
                <div class="emb-actions">
                  <button v-if="rec.status === 'In Storage' || rec.status === 'Assigned'" type="button" class="rp-add-btn emb-action-btn" @click="markEmbryoImplanted(rec)">Mark Implanted</button>
                  <button v-if="rec.status === 'Implanted'" type="button" class="rp-add-btn emb-action-btn emb-loss" :disabled="embryoActionId === rec.embryoRecordId" @click="markEmbryoLost(rec)">{{ embryoActionId === rec.embryoRecordId ? 'Recording…' : 'Did Not Stick' }}</button>
                  <button type="button" class="rp-add-btn emb-action-btn" @click="saveEmbryoRecord(rec)">Save</button>
                  <button type="button" class="rp-x" @click="removeEmbryoRecord(rec.id)">✕</button>
                </div>
              </div>
              <p class="emb-workflow-hint">Workflow: Save edits → Mark Implanted → Did Not Stick (if failed).</p>
              <div class="emb-grid">
                <label>Code / ID<input v-model="rec.code" type="text" placeholder="ET-2026-001"></label>
                <label>Group<input v-model.lazy="rec.groupName" type="text" placeholder="Donor line, flush, or custom group"></label>
                <label>Sire<input v-model="rec.sire" type="text" placeholder="Sire name"></label>
                <label>Donor Cow<input v-model="rec.donor" type="text" placeholder="Donor name"></label>
                <label>Mating<input v-model="rec.mating" type="text" placeholder="Donor x Sire"></label>
                <label>Grade<input v-model="rec.grade" type="text" placeholder="Grade 1, Excellent…"></label>
                <label>Current Status<input :value="rec.status" type="text" readonly></label>
                <label>Recipient Animal
                  <select v-model.number="rec.recipientAnimalId">
                    <option :value="null">No recipient yet</option>
                    <option v-for="a in animalOptions" :key="`r-${a.animalId}`" :value="a.animalId">{{ a.barnName || a.registeredName || `${a.damName || 'Unknown dam'} × ${a.sireName || 'Unknown sire'} (#${a.animalId})` }}</option>
                  </select>
                </label>
                <label>Implant Date<input v-model="rec.implantDate" type="date"></label>
                <label>Collection Location<input v-model="rec.collectionLocation" type="text" placeholder="Farm, flush date, or facility"></label>
                <label>Storage Location<input v-model="rec.storageLocation" type="text" placeholder="Tank/straw location"></label>
                <label>Breeding Link Note<input v-model="rec.linkedBreedingNote" type="text" placeholder="Breeding date or event ref"></label>
                <label class="emb-full">Notes<textarea v-model="rec.notes" rows="2" placeholder="Tank, straw info, vet notes" /></label>
              </div>
            </div>
          </div>
        </details>
      </template>

      <template v-if="embryosFailed.length > 0">
        <details class="emb-group-details">
          <summary class="emb-group-title">Failed / Not Confirmed ({{ embryosFailed.length }})</summary>
        <div class="rp-divider rp-divider-failed">Failed / Not Confirmed ({{ embryosFailed.length }})</div>
        <p class="rp-hint">Embryos that didn't stick — kept for your records.</p>
        <div v-for="rec in embryosFailed" :key="`f-${rec.id}`" class="emb-card emb-failed">
          <div class="emb-hd">
            <div class="emb-id">
              <span class="emb-code">{{ rec.code || 'No Code' }}</span>
              <span class="emb-badge ebadge-failed">Failed</span>
            </div>
            <div class="emb-actions">
              <button type="button" class="rp-add-btn emb-action-btn" @click="markEmbryoBackToStorage(rec)">Back to Storage</button>
              <button type="button" class="rp-add-btn emb-action-btn" @click="saveEmbryoRecord(rec)">Save</button>
              <button type="button" class="rp-x" @click="removeEmbryoRecord(rec.id)">✕</button>
            </div>
          </div>
          <div class="emb-summary"><span>Group: <strong>{{ rec.groupName || '—' }}</strong></span><span>Sire: <strong>{{ rec.sire || '—' }}</strong></span><span>Donor: <strong>{{ rec.donor || '—' }}</strong></span><span>Recipient: <strong>{{ rec.recipientAnimalId ? getAnimalLabel(rec.recipientAnimalId) : '—' }}</strong></span></div>
          <label class="emb-full mt8">Failure Notes<textarea v-model="rec.failureNotes" rows="2" placeholder="Reason, vet notes, recheck date" /></label>
          <label class="emb-full mt8">Current Status<input :value="rec.status" type="text" readonly></label>
        </div>
        </details>
      </template>
    </section>

    <!-- EMBRYO IMPLANTS -->
    <section v-else-if="activeTab === 'embryoImplants'" class="rp-panel">
      <div class="rp-ph">
        <h2>Embryo Implants vs Results</h2>
        <button type="button" class="rp-add-btn" @click="loadEmbryoImplants" :disabled="embryoImplantsLoading">{{ embryoImplantsLoading ? 'Loading…' : '↻ Refresh' }}</button>
      </div>
      <p class="rp-hint">Monthly breakdown of embryos implanted vs. failed outcomes.</p>

      <template v-if="embryoImplantsError">
        <div class="rp-error">{{ embryoImplantsError }} <button type="button" @click="loadEmbryoImplants">Try Again</button></div>
      </template>

      <template v-else-if="!embryoImplantsLoading">
        <details class="emb-group-details">
          <summary class="emb-group-title">Implant totals, chart &amp; records ({{ embryosWithImplants.length }})</summary>
        <div v-if="embryoImplantsData" class="as-row">
          <div class="as-stat">
            <span class="as-label">Total Implanted</span>
            <span class="as-val">{{ embryoImplantsData.totals.totalImplanted }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Awaiting Pregnancy Check</span>
            <span class="as-val">{{ embryoImplantsData.totals.waitingForPregCheck }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Confirmed Pregnant</span>
            <span class="as-val">{{ embryoImplantsData.totals.totalSuccessful }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Did Not Stick</span>
            <span class="as-val">{{ embryoImplantsData.totals.totalFailed }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Results Recorded</span>
            <span class="as-val">{{ embryoImplantsData.totals.resolvedImplants }} / {{ embryoImplantsData.totals.totalImplanted }}</span>
          </div>
          <div class="as-stat">
            <span class="as-label">Pregnancy Rate (Resolved)</span>
            <span class="as-val">{{ embryoImplantsData.totals.resolvedImplants ? `${embryoImplantsData.totals.successRatePct}%` : '—' }}</span>
          </div>
        </div>

        <p v-else class="rp-hint">Implant summary chart is unavailable right now, but implant records below are still shown.</p>

        <div v-if="embryoImplantsData" class="bc-group">
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

        <div class="implant-list">
          <div class="browse-label">Implant Records ({{ embryosWithImplants.length }})</div>
          <div v-if="embryosWithImplants.length === 0" class="rp-empty">No implant records were returned. Implant an embryo from Inventory, then refresh.</div>
          <article v-for="record in embryosWithImplants" :key="`implant-${record.id}`" class="implant-record-card">
            <div>
              <small>Embryo / mating</small>
              <strong>{{ record.code || record.mating || `${record.donor || 'Unknown donor'} × ${record.sire || 'Unknown sire'}` }}</strong>
              <span>{{ record.implantDate || 'Date not recorded' }} · {{ record.status }}</span>
            </div>
            <div>
              <small>Recipient</small>
              <strong>{{ record.recipientName || (record.recipientAnimalId ? getAnimalLabel(record.recipientAnimalId) : 'Not linked') }}</strong>
            </div>
            <div>
              <small>Donor × sire</small>
              <strong>{{ record.donor || 'Unknown donor' }} × {{ record.sire || 'Unknown sire' }}</strong>
            </div>
            <div>
              <small>Pregnancy follow-up</small>
              <strong v-if="record.pregnancyCheckDate">Checked {{ record.pregnancyCheckDate }}</strong>
              <strong v-else-if="record.pregnancyCheckDueDate">Due {{ record.pregnancyCheckDueDate }}</strong>
              <strong v-else>Not scheduled</strong>
              <span v-if="record.breedingEventId">Breeding record #{{ record.breedingEventId }}</span>
            </div>
            <div>
              <small>Group / grade</small>
              <strong>{{ record.groupName || 'Ungrouped' }} · {{ record.grade || 'No grade' }}</strong>
            </div>
            <div class="implant-record-actions">
              <button v-if="record.status === 'Implanted'" type="button" class="rp-add-btn" :disabled="embryoActionId === record.embryoRecordId" @click="markEmbryoSuccessful(record)">Confirm Pregnant</button>
              <button v-if="record.status === 'Implanted'" type="button" class="rp-add-btn emb-loss" :disabled="embryoActionId === record.embryoRecordId" @click="markEmbryoLost(record)">Did Not Stick</button>
              <button v-if="record.recipientAnimalId" type="button" class="rp-add-btn" @click="openAnimalFromReports(record.recipientAnimalId)">Open Recipient</button>
              <button type="button" class="rp-x" @click="markEmbryoBackToStorage(record)">Undo Implant</button>
            </div>
          </article>
        </div>
        </details>
      </template>
    </section>

    <!-- SHOW STRING -->
    <section v-else-if="activeTab === 'showString'" class="rp-panel">
      <div class="rp-ph">
        <h2>Show String Lineup</h2>
        <div class="rp-ph-actions">
          <button type="button" class="rp-add-btn" @click="shareShowStringLink">Share Link</button>
          <button type="button" class="rp-add-btn" @click="addShowStringRow">+ Blank Row</button>
        </div>
      </div>

      <p v-if="showStringShareStatus" class="rp-hint">{{ showStringShareStatus }}</p>

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
          <p v-if="showStringBrowseAnimals.length === 0" class="rp-empty-sm">
            {{ showStringClassFilter === 'all' && showStringSearch.trim().length < 2
              ? 'Choose a class or type at least two letters to find animals.'
              : 'No animals match this filter.' }}
          </p>
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
              <span class="lineup-age" v-if="row.animalId"><b>SHOW AGE</b> {{ formatCurrentAge(animals.find(a => a.animalId === row.animalId)?.birthDate) }}</span>
              <span class="lineup-birth" v-if="row.animalId">{{ formatShowBirthDate(animals.find(a => a.animalId === row.animalId)?.birthDate) }}</span>
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
              <span class="lineup-age" v-if="row.animalId"><b>SHOW AGE</b> {{ formatCurrentAge(animals.find(a => a.animalId === row.animalId)?.birthDate) }}</span>
              <span class="lineup-birth" v-if="row.animalId">{{ formatShowBirthDate(animals.find(a => a.animalId === row.animalId)?.birthDate) }}</span>
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

    <!-- SHOW BAGGING -->
    <section v-else-if="activeTab === 'showBagging'" class="rp-panel">
      <div class="rp-ph bagging-page-head">
        <h2>Show Bagging</h2>
        <div class="rp-ph-actions">
          <button type="button" class="rp-add-btn bagging-primary-save" :disabled="baggingSaving" @click="saveWholeBaggingPlan">{{ baggingSaving ? 'Saving…' : 'Save Whole Show' }}</button>
          <button type="button" class="rp-add-btn bagging-mode-btn" @click="baggingSimpleMode = !baggingSimpleMode">{{ baggingSimpleMode ? 'More Options' : 'Simple View' }}</button>
          <template v-if="!baggingSimpleMode">
            <button type="button" class="rp-add-btn" @click="shareBaggingLink">Share</button>
            <button type="button" class="rp-add-btn" @click="textBaggingTeam">Text Team</button>
            <button type="button" class="rp-add-btn" @click="reloadReportsData">↻ Reload</button>
          </template>
        </div>
      </div>
      <p v-if="baggingShareStatus" class="rp-hint">{{ baggingShareStatus }}</p>
      <p v-if="baggingSimpleMode" class="rp-hint bagging-simple-hint">Set the show start, add each cow, enter her show and milk-out times, then save.</p>

      <section class="bagging-show-anchor">
        <label>
          <strong><span class="bagging-step-number">1</span> When does the show start?</strong>
          <input v-model="showBaggingStartTime" type="datetime-local" />
        </label>
        <div>
          <small>Show starts</small>
          <strong>{{ formatHoursDifference(parseHoursDifference(showBaggingStartTime)) }}</strong>
          <span>{{ formatScheduleDateTime(showBaggingStartTime) }}</span>
        </div>
      </section>

      <div v-if="hasNoAnimals" class="rp-error" style="margin-bottom: 12px;">
        No cows are loaded in this environment yet. Bagging add/search needs herd animals. Try Reload Data.
      </div>

      <details v-if="!baggingSimpleMode" class="bagging-section-details">
        <summary>Show setup &amp; text contacts</summary>
      <div class="bagging-top-grid">
        <label>
          Bagging Group
          <input v-model="showBaggingShowName" list="show-name-options" type="text" placeholder="Group name (example: County Fair 2026)" />
          <datalist id="show-name-options">
            <option v-for="name in showNameOptions" :key="name" :value="name" />
          </datalist>
        </label>

        <label>
          Group Date
          <input v-model="showBaggingShowDate" type="date" />
        </label>

        <label class="bagging-phone-field">
          Phone numbers for this show
          <textarea v-model="showBaggingPhoneNumbers" rows="2" inputmode="tel" placeholder="Enter numbers separated by commas" />
          <small>Saved on this device with this show plan.</small>
        </label>
      </div>
      </details>

      <details class="bagging-section-details" open>
        <summary><span class="bagging-step-number">2</span> Add a cow</summary>
      <div class="bagging-search-panel">
        <div class="browse-label">Quick Cow Search</div>
        <p class="bagging-search-hint">Type at least 2 letters to find a cow, then tap + Bag.</p>
        <input v-model="showBaggingSearch" type="search" class="rp-list-search" placeholder="Search by barn name, registered name, sire, dam, or breed…" />
        <div class="bagging-search-tools">
          <span>{{ showBaggingSearch.trim().length < 2 ? 'Type 2+ letters' : `${showBaggingMatchCount} matches` }}</span>
          <span class="bagging-tools-note">Tap + Bag on the cow you want</span>
        </div>
        <p v-if="baggingActionStatus" class="rp-hint">{{ baggingActionStatus }}</p>
        <div class="browse-grid">
          <div v-for="animal in showBaggingBrowseAnimals" :key="`bag-${animal.animalId}`" class="browse-row">
            <div class="browse-info">
              <strong>{{ animal.barnName || animal.registeredName || `#${animal.animalId}` }}</strong>
              <span class="browse-age">{{ formatCurrentAge(animal.birthDate) }}</span>
              <span class="browse-cls">{{ getShowClassLabel(animal.birthDate, animal.animalStage) }}</span>
            </div>
            <button type="button" class="add-str-btn" @click="addShowBaggingRow(animal)">+ Bag</button>
          </div>
          <p v-if="showBaggingSearch.trim().length < 2" class="rp-empty-sm">Start typing a cow name to see results.</p>
          <p v-else-if="showBaggingBrowseAnimals.length === 0" class="rp-empty-sm">No cows match this search.</p>
        </div>
      </div>
      </details>

      <details v-if="!baggingSimpleMode" class="bagging-section-details">
        <summary>Past bagging history</summary>
      <div class="bagging-history-panel">
        <div class="browse-label">Bagging History Search</div>
        <div class="browse-filters">
          <input
            v-model="baggingHistorySearch"
            type="search"
            class="rp-sel"
            placeholder="Search by cow, group, notes, or date..."
          />
          <select v-model="baggingHistoryGroupFilter" class="rp-sel">
            <option value="">All Groups</option>
            <option v-for="group in baggingGroupOptions" :key="`bag-group-${group}`" :value="group">{{ group }}</option>
          </select>
          <label class="bagging-group-filter-toggle">
            <input v-model="baggingHistoryGroupOnly" type="checkbox" />
            Filter to selected group only
          </label>
        </div>
        <div class="bagging-history-count">
          {{ baggingHistoryMatches.length }} saved bagging record{{ baggingHistoryMatches.length === 1 ? '' : 's' }}
        </div>
        <div v-if="baggingHistoryMatches.length === 0" class="rp-empty-sm">No saved bagging history matches your filters yet.</div>
        <div v-else class="bagging-history-grid">
          <article v-for="history in baggingHistoryMatches.slice(0, 24)" :key="`hist-${history.id}`" class="bagging-history-card">
            <strong>{{ history.showName || 'Ungrouped' }}</strong>
            <span>{{ getAnimalLabel(history.animalId) }}</span>
            <small>{{ history.showDate || 'No date' }} · {{ history.placed || 'No result' }}</small>
            <p>{{ history.bagged || 'No bagging summary entered.' }}</p>
            <button type="button" class="bagging-show-link" @click="openBaggingHistoryForGroup(history.showName)">Focus Group</button>
          </article>
        </div>
      </div>
      </details>

      <div v-if="false" class="bagging-glance-panel">
        <div class="browse-label">Bagging At A Glance</div>
        <p class="bagging-search-hint">Each group is collapsed into one summary so you can glance and open only the row you need.</p>
        <div v-if="baggingRowGroups.length === 0" class="rp-empty-sm">No bagging rows yet. Add cows above and they will appear here.</div>
        <details v-for="group in baggingRowGroups" :key="`bag-group-${group.name}`" class="bagging-group-details">
          <summary class="bagging-group-summary">
            <strong>{{ group.name }}</strong>
            <span>{{ group.records.length }} cow{{ group.records.length === 1 ? '' : 's' }}</span>
          </summary>
          <div class="bagging-group-glance">
            <article v-for="row in group.records" :key="`glance-${row.id}`" class="bagging-glance-card">
              <div class="bagging-glance-head">
                <strong>{{ getBaggingRowAnimalLabel(row) }}</strong>
                <button type="button" class="bagging-jump-btn" @click="jumpToBaggingRow(row.id)">Jump</button>
              </div>
              <div class="bagging-glance-meta">{{ row.showDate || showBaggingShowDate }} · Entry {{ formatTime(row.entryTime) }}</div>
              <div class="bagging-glance-quarters">
                <div v-for="quarter in row.quarters" :key="`g-${row.id}-${quarter.key}`" class="bagging-glance-quarter">
                  <span>{{ quarterShortLabel(quarter.key) }}</span>
                  <strong>{{ quarter.hoursBeforeRing === null ? '--' : `${quarter.hoursBeforeRing}h` }}</strong>
                  <small>{{ getQuarterMilkTime(row.entryTime, quarter.hoursBeforeRing) }}</small>
                </div>
              </div>
            </article>
          </div>
        </details>
      </div>

      <div v-if="showBaggingRowsSorted.length === 0" class="rp-empty">Add a cow above to start bagging.</div>

      <div id="bagging-rows" />

      <section v-if="!baggingSimpleMode && baggingCowsByShowTime.length > 0" class="bagging-cow-overview">
        <div class="bagging-timeline-heading">
          <strong>All cows at a glance</strong>
          <span>{{ baggingCowsByShowTime.length }} cow{{ baggingCowsByShowTime.length === 1 ? '' : 's' }}</span>
        </div>
        <article v-for="row in baggingCowsByShowTime" :key="`overview-${row.id}`" class="bagging-cow-quick-row">
          <strong>{{ getBaggingRowAnimalLabel(row) }}</strong>
          <span>{{ formatHoursDifference(parseHoursDifference(row.entryTime)) }}</span>
          <input v-model="row.entryTime" type="datetime-local" aria-label="Cow show time" />
          <button type="button" @click="jumpToBaggingRow(row.id)">Edit quarters</button>
        </article>
      </section>

      <section v-if="!baggingSimpleMode && baggingTimeline.length > 0" class="bagging-timeline">
        <div class="bagging-timeline-heading">
          <strong>Milking and ring schedule</strong>
          <span>Earliest first</span>
        </div>
        <div v-for="group in baggingTimeline" :key="group.time" class="bagging-time-group">
          <time :datetime="group.time">{{ formatScheduleDateTime(group.time) }}</time>
          <div class="bagging-time-items">
            <button v-for="item in group.items" :key="`${item.rowId}-${item.action}`" type="button" @click="jumpToBaggingRow(item.rowId)">
              <strong>{{ item.cow }}</strong>
              <span>{{ item.action }}</span>
            </button>
          </div>
          <small v-if="group.items.length > 1">{{ group.items.length }} jobs at this same time</small>
        </div>
      </section>

      <div v-if="showBaggingRowsSorted.length > 0" class="browse-label">Current Cows</div>

      <details v-for="row in showBaggingRowsSorted" :id="baggingRowAnchorId(row.id)" :key="row.id" class="bagging-edit-group cow-bagging-details">
        <summary class="bagging-edit-summary">
          <strong><span class="bagging-step-number">3</span> {{ getBaggingRowAnimalLabel(row) }}</strong>
          <span>{{ formatTime(row.entryTime) }} · {{ formatHoursDifference(parseHoursDifference(row.entryTime)) }}</span>
        </summary>
        <div class="bagging-edit-group-body">
          <div class="bagging-card">
            <div class="bagging-card-hd">
              <div>
                <button type="button" class="bagging-show-link" @click="openBaggingHistoryForGroup(row.showName || showBaggingShowName)">
                  {{ row.showName || showBaggingShowName || 'Select a group' }}
                </button>
                <div class="bagging-cow-line">{{ getBaggingRowAnimalLabel(row) }}</div>
              </div>
              <div class="bagging-card-actions">
                <button type="button" class="rp-x" @click="removeShowBaggingRow(row.id)">✕</button>
              </div>
            </div>

            <div class="bagging-quick-edit">
              <label>
                <strong>{{ getBaggingRowAnimalLabel(row) }} show time</strong>
                <input v-model="row.entryTime" type="datetime-local" required />
              </label>
              <div>
                <small>Goes out</small>
                <strong>{{ formatHoursDifference(parseHoursDifference(row.entryTime)) }}</strong>
                <span>{{ formatScheduleDateTime(row.entryTime) }}</span>
              </div>
            </div>

            <div class="bagging-meta-grid">
              <label>
                Cow
                <select v-model.number="row.animalId">
                  <option :value="null">Select cow</option>
                  <option v-for="animal in animalOptions" :key="`bag-select-${animal.animalId}`" :value="animal.animalId">{{ animal.barnName || animal.registeredName || `#${animal.animalId}` }}</option>
                </select>
              </label>

              <label>
                Bagging Group
                <input v-model.lazy="row.showName" type="text" placeholder="Group name" />
              </label>

              <label>
                Group Date
                <input v-model="row.showDate" type="date" />
              </label>

              <label class="bagging-success-toggle">
                <input v-model="row.wasSuccessful" type="checkbox" />
                Successful bagging / result
              </label>

              <label class="bagging-success-toggle reminder-toggle">
                <input v-model="row.remindersEnabled" type="checkbox" />
                Remind crew 15 minutes before each milk-out
              </label>

            </div>

            <div class="bagging-udder-grid exact-times">
              <label
                v-for="quarter in row.quarters"
                :key="quarter.key"
                class="udder-quarter"
              >
                <span class="quarter-label">{{ quarter.label }}</span>
                <strong class="quarter-hours-label">Hours before cow goes out</strong>
                <input v-model.number="quarter.hoursBeforeRing" type="number" min="0" step="0.5" inputmode="decimal" placeholder="Example: 8" @input="quarter.milkOutTime = ''" />
                <strong class="quarter-time-value">Milk at {{ getQuarterMilkTime(row.entryTime, quarter.hoursBeforeRing) }}</strong>
                <small class="quarter-alert-time">Alert at {{ quarter.hoursBeforeRing === null ? '—' : formatTime(addHoursToInput(row.entryTime, -quarter.hoursBeforeRing - 0.25)) }}</small>
              </label>
            </div>

            <label class="bagging-notes">
              Notes / what happened
              <textarea v-model="row.notes" rows="3" placeholder="Milk letdown, timing adjustments, success notes, problems..." />
            </label>
            <button type="button" class="bagging-save-bottom" @click="saveWholeBaggingPlan">Save Whole Show</button>
          </div>
        </div>
      </details>
    </section>

    <!-- HERD LISTS -->
    <section v-else-if="activeTab === 'lists'" class="rp-panel">
      <div class="rp-ph"><h2>Herd Lists</h2></div>
      <p class="rp-hint">Search by barn name, tap animals to toggle them into a list. Each list is independent.</p>

      <div v-for="list in orderedGroupLists" :key="list.key" class="rp-group">
        <div class="rp-group-hd">
          <h3>{{ list.title }}</h3>
          <span class="rp-group-ct">{{ getListAnimalIds(list).length }}</span>
        </div>
        <button v-if="list.key === 'health-paper-group'" type="button" class="rp-add-btn" @click="openHealthPaperReport">Open Printable Health Papers</button>
        <p v-if="isReadOnlyList(list)" class="rp-hint">Pulled automatically from the first 3 animals in Show String.</p>
        <template v-else>
          <input v-model="list.searchQuery" type="search" class="rp-list-search" :placeholder="`Search ${list.title}…`" />
          <p v-if="(list.searchQuery || '').trim().length < 2" class="rp-hint">Type at least 2 letters. The entire herd will not be shown as buttons.</p>
        </template>
        <div v-if="!isReadOnlyList(list)" class="rp-chips">
          <button v-for="a in filteredListAnimals(list)" :key="`${list.key}-${a.animalId}`" type="button" class="rp-chip" :class="{ 'rp-chip-sel': getListAnimalIds(list).includes(a.animalId) }" @click="list.key === 'health-paper-group' ? addHealthPaperAnimalAndContinue(list, a.animalId, $event) : toggleAnimalInList(list.key, a.animalId)">{{ a.barnName || a.registeredName || `#${a.animalId}` }}</button>
        </div>
        <div v-if="getListAnimalIds(list).length > 0" class="rp-list-members">
          <div class="rp-lm-hd">In this list</div>
          <div v-for="id in getListAnimalIds(list)" :key="`${list.key}-in-${id}`" class="rp-lm-row">
            <button type="button" class="rp-lm-open" @click="openAnimalFromReports(id)">
              <strong>{{ getAnimalLabel(id) }}</strong>
              <small>Current working pen: {{ getCurrentWorkingPen(id) }}</small>
              <small v-if="getShowStringPosition(id)">Show String #{{ getShowStringPosition(id) }}</small>
            </button>
            <button v-if="!isReadOnlyList(list)" type="button" class="rp-lm-rm" @click="toggleAnimalInList(list.key, id)">✕</button>
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

    <!-- PCDART IMPORT -->
    <section v-else-if="activeTab === 'pcdartImport'" class="rp-panel">
      <div class="rp-ph">
        <h2>Legacy PCDART Notes &amp; Audit</h2>
      </div>
      <p class="rp-hint">This older tool creates timeline notes and suggested-change audits. For stored milk values and analytics, use Import PC-DART at the top.</p>

      <div class="rp-row-card">
        <label>Report Label
          <input v-model="pcdartReportLabel" type="text" placeholder="PCDART Monthly Aug 2026">
        </label>
        <label>Upload Report Text File
          <input type="file" accept=".txt,.csv,text/plain,text/csv" @change="loadPcdartFile">
        </label>
        <label>
          <span>Accept Suggested Safe Changes</span>
          <select v-model="pcdartApplySuggested">
            <option :value="true">Yes - auto apply safe fixes</option>
            <option :value="false">No - audit only</option>
          </select>
        </label>
        <label class="rp-full">Raw Report Text
          <textarea v-model="pcdartRawText" rows="10" placeholder="Paste PCDART report text here" />
        </label>
        <p v-if="pcdartFileName" class="rp-hint rp-full">Loaded file: {{ pcdartFileName }}</p>
        <div class="emb-actions rp-full">
          <button type="button" class="rp-add-btn" :disabled="pcdartImporting" @click="runPcdartImport(false)">{{ pcdartImporting ? 'Working…' : 'Preview Import' }}</button>
          <button type="button" class="rp-add-btn" :disabled="pcdartImporting" @click="runPcdartImport(true)">{{ pcdartImporting ? 'Working…' : 'Accept Audit Results & Apply' }}</button>
        </div>
      </div>

      <p v-if="pcdartError" class="rp-error">{{ pcdartError }}</p>

      <div v-if="pcdartResult" class="rp-row-card">
        <p><strong>Mode:</strong> {{ pcdartResult.applied ? 'Applied' : 'Preview only' }}</p>
        <p><strong>Rows read:</strong> {{ pcdartResult.rowsRead }}</p>
        <p><strong>Animals matched:</strong> {{ pcdartResult.animalsMatched }}</p>
        <p><strong>Animals created:</strong> {{ pcdartResult.animalsCreated }}</p>
        <p><strong>Notes created:</strong> {{ pcdartResult.notesCreated }}</p>
        <p><strong>Duplicates skipped:</strong> {{ pcdartResult.duplicateNotesSkipped }}</p>
        <p><strong>Suggested changes applied:</strong> {{ pcdartResult.suggestedChangesApplied }}</p>
        <div class="rp-full" v-if="pcdartResult.missingAnimals.length > 0">
          <strong>Confirm unmatched PCDART names</strong>
          <p class="rp-hint">Choose the correct herd animal for each shortened or slightly different PCDART name. Your confirmed choices are remembered on this device.</p>
          <div class="pcdart-match-list">
            <label v-for="name in pcdartResult.missingAnimals" :key="name" class="pcdart-match-row">
              <span>PCDART: {{ name }}</span>
              <select v-model.number="pcdartMappings[name]">
                <option :value="0">Select the correct animal…</option>
                <option v-for="animal in pcdartAnimalSuggestions(name)" :key="animal.animalId" :value="animal.animalId">{{ getAnimalLabel(animal.animalId) }}</option>
              </select>
            </label>
          </div>
          <p class="rp-hint">After matching the names, tap Apply Monthly Import again. Unmatched rows will not create duplicate animals.</p>
        </div>
        <div class="rp-full" v-if="pcdartResult.alerts.length > 0">
          <strong>Audit Alerts:</strong>
          <ul>
            <li v-for="alert in pcdartResult.alerts" :key="`${alert.code}-${alert.animalId ?? alert.message}`">
              {{ alert.animalLabel }}: {{ alert.message }}
            </li>
          </ul>
        </div>
        <div class="rp-full" v-if="pcdartResult.suggestedChanges.length > 0">
          <strong>Suggested Changes:</strong>
          <ul>
            <li v-for="change in pcdartResult.suggestedChanges" :key="`${change.code}-${change.animalId ?? change.proposedAction}`">
              {{ change.animalLabel }}: {{ change.proposedAction }}
              <span v-if="change.canAutoApply">(auto-apply supported)</span>
              <span v-else>(manual review)</span>
            </li>
          </ul>
        </div>
        <div class="rp-full" v-if="pcdartResult.conflicts.length > 0">
          <strong>Conflicts:</strong>
          <ul>
            <li v-for="msg in pcdartResult.conflicts" :key="msg">{{ msg }}</li>
          </ul>
        </div>
      </div>
    </section>

    <!-- ACHIEVEMENTS -->
    <section v-else class="rp-panel">
      <div class="rp-ph">
        <h2>Bagging History & Achievements</h2>
        <div class="rp-ph-actions">
          <button type="button" class="rp-add-btn" @click="shareAchievementsLink">Share Link</button>
          <button type="button" class="rp-add-btn" @click="addAchievement">+ Record</button>
        </div>
      </div>
      <p v-if="achievementsShareStatus" class="rp-hint">{{ achievementsShareStatus }}</p>
      <input v-model="achievementSearch" type="search" class="rp-list-search" placeholder="Search by show, cow, or notes…" />
      <div v-if="achievementMatches.length === 0" class="rp-empty">No achievements logged yet.</div>
      <details v-for="group in achievementGroups" :key="`ach-${group.name}`" class="achievement-group-details">
        <summary class="achievement-group-summary">
          <strong>{{ group.name }}</strong>
          <span>{{ group.records.length }} record{{ group.records.length === 1 ? '' : 's' }}</span>
        </summary>
        <div class="achievement-group-body">
          <div v-for="rec in group.records" :key="rec.id" class="rp-row-card">
            <label>Animal
              <select v-model.number="rec.animalId">
                <option :value="null">Select animal</option>
                <option v-for="a in animalOptions" :key="`ach-${a.animalId}`" :value="a.animalId">{{ a.barnName || a.registeredName || `#${a.animalId}` }}</option>
              </select>
            </label>
            <label>Bagging Group<input v-model="rec.showName" type="text" placeholder="Spring Classic Group"></label>
            <label>Group Date<input v-model="rec.showDate" type="date"></label>
            <label>Bagged<input v-model="rec.bagged" type="text" placeholder="How she bagged up"></label>
            <label>Result<input v-model="rec.placed" type="text" placeholder="Successful / Not Successful"></label>
            <label class="rp-full">Notes<textarea v-model="rec.notes" rows="2" placeholder="Judge comments, prep notes" /></label>
            <button type="button" class="rp-add-btn" @click="saveAchievement(rec)">Save</button>
            <button type="button" class="rp-danger" @click="removeAchievement(rec.id)">Remove</button>
          </div>
        </div>
      </details>
    </section>
  </main>
</template>

<style scoped>
.rp { max-width: 1240px; margin: 0 auto; padding: 0 0 60px; font-family: 'Bahnschrift', 'Arial Narrow', 'Segoe UI', sans-serif; background: #f5f7f2; min-height: 100vh; }
.rp-hero { background: linear-gradient(135deg, #0f2318 0%, #1a3d22 60%, #244f2f 100%); padding: 22px 24px 18px; border-bottom: 3px solid #31572c; }
.rp-hero-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 10px; gap: 12px; }
.rp-hero-actions { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.rp-brand { color: #7dd3a0; font-size: 0.75rem; font-weight: 900; letter-spacing: 0.12em; text-transform: uppercase; }
.rp-title { margin: 0; font-size: 1.85rem; font-weight: 900; color: #fff; letter-spacing: -0.02em; text-transform: uppercase; }
.rp-sub { margin: 6px 0 0; color: rgba(255,255,255,0.6); font-size: 0.88rem; }
.rp-powered { margin: 10px 0 0; color: rgba(255,255,255,0.3); font-size: 0.72rem; letter-spacing: 0.04em; }
.rp-powered strong { color: rgba(255,255,255,0.55); font-weight: 900; }
.show-command{background:#f3f4f0}.show-command .rp-hero{position:relative;overflow:hidden;background-color:#07111d;background-image:radial-gradient(circle at 14% 24%,rgba(255,255,255,.95) 0 1px,transparent 2px),radial-gradient(circle at 76% 18%,rgba(151,225,255,.9) 0 1px,transparent 2px),radial-gradient(circle at 46% 72%,rgba(255,255,255,.7) 0 1px,transparent 2px),radial-gradient(circle at 90% 68%,rgba(255,217,102,.75) 0 1px,transparent 2px),linear-gradient(135deg,#020617 0%,#0b1f2d 58%,#102f28 100%);background-size:130px 110px,180px 140px,210px 170px,240px 190px,auto;border-bottom:4px solid #f5c84c;box-shadow:inset 0 -1px 0 #66d9ef}.show-command .rp-hero:after{content:'🐄  MOO SQUADRON • ALL SYSTEMS READY';display:block;width:max-content;margin-top:13px;padding:5px 10px;border:1px solid rgba(102,217,239,.55);border-radius:999px;background:rgba(2,6,23,.66);color:#9be8f4;font-size:.67rem;font-weight:900;letter-spacing:.13em}.show-command .rp-title{color:#ffe48a;text-shadow:0 0 14px rgba(245,200,76,.3);letter-spacing:.05em}.show-command .rp-sub{color:#c8eff5}.show-command .rp-brand{color:#9be8f4}.show-command .rp-print-link{border-color:#f5c84c;color:#ffe48a}.show-command .rp-tabs{background:#081521;border-bottom-color:#f5c84c;gap:7px;padding:8px 16px}.show-command .rp-tabs button{border:1px solid #294658;border-radius:7px;color:#b9d8df;background:#0d2130;padding:11px 15px}.show-command .rp-tabs button:hover{color:#fff;border-color:#66d9ef}.show-command .rp-tabs button.active{color:#081521;background:#f5c84c;border-color:#ffe48a;box-shadow:0 0 14px rgba(245,200,76,.24)}
.embryo-hatchery{background:#fffaf0}.embryo-hatchery .rp-hero{position:relative;overflow:hidden;background-color:#6b3e1f;background-image:repeating-linear-gradient(12deg,transparent 0 17px,rgba(255,218,145,.08) 18px 20px),linear-gradient(135deg,#4b2b18 0%,#815027 55%,#a96f32 100%);border-bottom:5px solid #e8b84f;box-shadow:inset 0 -1px 0 #fff0bd}.embryo-hatchery .rp-hero:after{content:'🐔  NEST CHECK • EGGS ACCOUNTED FOR';display:block;width:max-content;margin-top:13px;padding:5px 11px;border:1px solid rgba(255,240,189,.55);border-radius:999px;background:rgba(67,36,16,.55);color:#fff0bd;font-size:.67rem;font-weight:900;letter-spacing:.12em}.embryo-hatchery .rp-title{color:#fff4cf;text-shadow:0 2px 0 rgba(62,31,13,.45)}.embryo-hatchery .rp-sub{color:#ffe4a3}.embryo-hatchery .rp-brand{color:#ffd873}.embryo-hatchery .rp-print-link{border-color:#ffd873;color:#fff0bd}.embryo-hatchery .rp-tabs{gap:8px;padding:9px 16px;background:#fff4d7;border-bottom:2px solid #dfb052}.embryo-hatchery .rp-tabs button{border:1px solid #d5a957;border-radius:999px;background:#fffaf0;color:#71431f;padding:11px 18px}.embryo-hatchery .rp-tabs button:hover{border-color:#9a5e28;color:#4b2b18}.embryo-hatchery .rp-tabs button.active{border-color:#8a521f;background:#f2c768;color:#4b2b18;box-shadow:0 3px 0 #b67830}.embryo-hatchery .rp-panel{border-color:#ead6a5;background:#fffdf7}.embryo-hatchery .emb-card{border-color:#ead6a5;background:#fffdf7}
.analytics-sports{background:#eef0f2}.analytics-sports .rp-hero{position:relative;overflow:hidden;background-color:#111318;background-image:linear-gradient(115deg,transparent 0 62%,rgba(218,32,45,.18) 62% 68%,transparent 68%),repeating-linear-gradient(90deg,rgba(255,255,255,.025) 0 1px,transparent 1px 32px),linear-gradient(135deg,#090a0d 0%,#1b1e24 62%,#2a2d34 100%);border-bottom:6px solid #d8202d;box-shadow:inset 0 -1px 0 #fff}.analytics-sports .rp-hero:after{content:'● LIVE  •  VENTURE HERD STAT DESK  •  UPDATED FROM YOUR RECORDS';display:block;width:max-content;margin-top:13px;padding:5px 11px;border-left:4px solid #d8202d;background:#f7f7f8;color:#17191e;font-size:.67rem;font-weight:950;letter-spacing:.11em}.analytics-sports .rp-title{font-style:italic;letter-spacing:-.03em;text-shadow:3px 3px 0 #a91420}.analytics-sports .rp-sub{color:#e8eaed;font-weight:750}.analytics-sports .rp-brand{color:#ff6b73}.analytics-sports .rp-print-link{border-color:#f04450;color:#fff}.analytics-sports .rp-categories{background:#17191e;border-bottom:1px solid #343841}.analytics-sports .rp-categories button{border-color:#3a3e47;background:#24272e;color:#e9eaec;text-transform:uppercase;letter-spacing:.05em}.analytics-sports .rp-categories button.active{border-color:#ef3d49;background:#d8202d;color:#fff;box-shadow:0 3px 0 #84101a}.analytics-sports .rp-tabs{background:#fff;border-bottom:3px solid #202329;gap:5px;padding:8px 16px}.analytics-sports .rp-tabs button{border:1px solid #d5d8dc;border-radius:5px;background:#f4f5f6;color:#34373d;padding:11px 14px}.analytics-sports .rp-tabs button:hover{border-color:#d8202d;color:#a91420}.analytics-sports .rp-tabs button.active{border-color:#d8202d;border-bottom-color:#d8202d;background:#d8202d;color:#fff;box-shadow:0 3px 0 #84101a}.analytics-sports .rp-panel{border-top:4px solid #d8202d;box-shadow:0 3px 10px rgba(22,24,29,.08)}.analytics-sports .rp-ph h2{font-style:italic}.analytics-sports .rp-add-btn{background:#d8202d}.analytics-sports .rp-add-btn:hover{background:#a91420}
.attention-ribbon{display:grid;grid-template-columns:auto auto 1fr auto;gap:12px;align-items:center;width:calc(100% - 32px);margin:12px 16px 0;padding:11px 14px;border:0;border-left:7px solid #d8202d;background:#24272e;color:#fff;text-align:left;cursor:pointer}.attention-ribbon span{color:#ff6973;font-size:.72rem;font-weight:950;letter-spacing:.1em}.attention-ribbon strong{font-size:1rem}.attention-ribbon small{color:#c9cdd2}.attention-ribbon b{color:#fff}
@media(max-width:700px){.attention-ribbon{grid-template-columns:1fr;width:calc(100% - 16px);margin:8px}.attention-ribbon b{justify-self:start}}
.rp-back { display: inline-flex; align-items: center; gap: 6px; border: 1px solid rgba(255,255,255,0.22); background: rgba(255,255,255,0.07); color: #e2e8f0; font-weight: 800; font-size: 0.85rem; border-radius: 6px; padding: 8px 14px; cursor: pointer; }
.rp-back:hover { background: rgba(255,255,255,0.14); }
.rp-print-link { border-color: rgba(125,211,160,0.45); }
.rp-ph-actions { display: flex; flex-wrap: wrap; gap: 8px; justify-content: flex-end; }

.rp-categories { display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;padding:12px 16px 8px;background:#fff; }
.rp-categories button { min-height:48px;border:1px solid #c8d4cb;border-radius:9px;background:#f5f8f5;color:#31572c;font-weight:900;font-size:.86rem;cursor:pointer; }
.rp-categories button.active { border-color:#31572c;background:#31572c;color:#fff;box-shadow:0 3px 10px rgba(49,87,44,.18); }
.rp-tabs { display: flex; overflow-x: auto; gap: 0; background: #fff; border-bottom: 2px solid #e0e8e1; padding: 0 16px; }
.rp-tabs::-webkit-scrollbar { height: 0; }
.rp-tabs button { display: inline-flex; align-items: center; gap: 7px; flex-shrink: 0; border: none; border-bottom: 3px solid transparent; background: transparent; color: #5d6f63; font-weight: 800; font-size: 0.82rem; letter-spacing: 0.06em; text-transform: uppercase; padding: 14px 16px 11px; cursor: pointer; white-space: nowrap; transition: color 0.15s, border-color 0.15s; }
.rp-tabs button:hover { color: #0f1f16; }
.rp-tabs button.active { color: #31572c; border-bottom-color: #31572c; }

.rp-panel { margin: 20px 16px; background: #fff; border: 1px solid #d9e3dc; border-radius: 12px; padding: 20px; }
.rp-ph { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 16px; padding-bottom: 14px; border-bottom: 1px solid #e0e8e1; }
.rp-ph h2 { margin: 0; color: #0f1f16; font-size: 1.1rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; }
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
.emb-create-form{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:10px;margin:12px 0 18px;padding:14px;border:1px solid #cfdcd2;border-radius:10px;background:#f8fbf8}.emb-create-form label{display:grid;gap:5px;font-size:.78rem;font-weight:850;color:#31572c}.emb-create-form input{min-width:0;padding:10px;border:1px solid #b9cabb;border-radius:7px;background:#fff;font:inherit;color:#17251b}.emb-create-actions{grid-column:1/-1;display:flex;gap:8px;align-items:center}
.emb-confirmed-pregnant { border-left-color: #0f766e; background: #f0fdfa; }
.emb-failed { border-left-color: #dc2626; background: #fff8f8; }
.emb-hd { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
.emb-id { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.emb-code { font-weight: 900; font-size: 1rem; color: #0f1f16; }
.emb-date { font-size: 0.82rem; color: #5d6f63; }
.emb-badge { border-radius: 999px; padding: 2px 10px; font-size: 0.72rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; }
.ebadge-in-storage { background: #dcfce7; color: #14532d; }
.ebadge-assigned { background: #fef3c7; color: #92400e; }
.ebadge-implanted { background: #dbeafe; color: #1d4ed8; }
.ebadge-confirmed-pregnant { background: #ccfbf1; color: #115e59; }
.ebadge-failed { background: #fee2e2; color: #991b1b; }
.emb-group-details,
.bagging-group-details,
.bagging-edit-group,
.achievement-group-details { margin: 14px 0; border: 1px solid #d9e3dc; border-radius: 10px; background: #fff; overflow: hidden; }
.emb-group-details[open],
.bagging-group-details[open],
.bagging-edit-group[open],
.achievement-group-details[open] { box-shadow: 0 1px 0 rgba(0,0,0,0.02); }
.emb-group-details summary,
.bagging-group-details summary,
.bagging-edit-group summary,
.achievement-group-details summary { list-style: none; cursor: pointer; }
.emb-group-details summary::-webkit-details-marker,
.bagging-group-details summary::-webkit-details-marker,
.bagging-edit-group summary::-webkit-details-marker,
.achievement-group-details summary::-webkit-details-marker { display: none; }
.emb-group-title { margin: 0; padding: 10px 12px; background: #f0f7f1; border-left: 4px solid #31572c; color: #1f3a25; font-size: 0.8rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; }
.emb-group-body { padding: 12px; display: grid; gap: 10px; }
.emb-workflow-hint { margin: 0 0 10px; font-size: 0.8rem; font-weight: 700; color: #31572c; }
.emb-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.emb-full { grid-column: 1 / -1; }
.mt8 { margin-top: 8px; }
.emb-summary { display: flex; flex-wrap: wrap; gap: 12px; font-size: 0.9rem; color: #5d6f63; }
.rp-x { border: 1px solid #fca5a5; background: #fff1f2; color: #991b1b; border-radius: 4px; width: 28px; height: 28px; font-size: 0.85rem; font-weight: 900; cursor: pointer; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.rp-x:hover { background: #fee2e2; }
.rp-divider { margin: 20px 0 10px; padding: 8px 14px; background: #f0f7f1; border-left: 4px solid #31572c; border-radius: 4px; font-weight: 900; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.06em; color: #1f3a25; }
.rp-divider-failed { background: #fff8f8; border-left-color: #dc2626; color: #991b1b; }
.implant-list { margin-top: 20px; display: grid; gap: 9px; }
.implant-record-card { display: grid; grid-template-columns: 1.1fr 1fr 1fr; gap: 12px; padding: 13px; border: 1px solid #cfdcd2; border-left: 4px solid #2563eb; border-radius: 9px; background: #f8fbff; }
.implant-record-card > div { display: grid; gap: 3px; min-width: 0; }
.implant-record-card .implant-record-actions { grid-column: 1 / -1; display: flex; flex-wrap: wrap; gap: 7px; }
.implant-record-actions .rp-add-btn { width: auto; }
.implant-record-card span, .implant-record-card small { color: #5d6f63; font-size: .78rem; }
.implant-record-card strong { overflow-wrap: anywhere; }
.pcdart-match-list { display: grid; gap: 10px; margin-top: 10px; }
.pcdart-match-row { padding: 10px; border: 1px solid #d7e2d8; border-radius: 8px; background: #fff; }

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

.lineup-age b {
  margin-right: 3px;
  color: #123b68;
  font-size: 0.68rem;
  letter-spacing: 0.05em;
}

.lineup-birth {
  color: #475569;
  font-size: 0.78rem;
  font-weight: 700;
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
  .emb-create-form{grid-template-columns:1fr 1fr}.emb-create-form label:first-child{grid-column:1/-1}
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
.rp-lm-open { display: grid; gap: 3px; flex: 1; min-width: 0; border: none; background: transparent; padding: 0; text-align: left; color: inherit; cursor: pointer; }
.rp-lm-open strong { overflow-wrap: anywhere; }
.rp-lm-open small { color: #5d6f63; font-size: 0.76rem; }
.rp-lm-rm { border: none; background: transparent; color: #dc2626; font-size: 0.85rem; cursor: pointer; padding: 2px 6px; border-radius: 4px; }
.rp-lm-rm:hover { background: #fee2e2; }
.lbl-notes { display: grid; gap: 6px; margin-top: 10px; color: #5d6f63; font-weight: 700; font-size: 0.8rem; letter-spacing: 0.06em; text-transform: uppercase; }

/* show bagging */
.bagging-top-grid { display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; gap: 12px; margin-bottom: 14px; }
.bagging-simple-hint { margin-top:-5px;padding:10px 12px;border-left:4px solid #31572c;background:#f3f8f3;color:#17331f;font-weight:750; }
.bagging-step-number { display:inline-grid;place-items:center;width:24px;height:24px;margin-right:6px;border-radius:50%;background:#31572c;color:#fff;font-size:.75rem;font-weight:950;vertical-align:middle; }
.bagging-mode-btn { background:#eef5ef;color:#17331f;border:1px solid #9db39f; }
.bagging-mode-btn:hover { background:#dfece1; }
.bagging-section-details { border:1px solid #d9e3dc;border-radius:10px;background:#fff;margin-bottom:10px;overflow:hidden; }
.bagging-section-details>summary { cursor:pointer;padding:13px 14px;font-weight:900;color:#17331f;background:#f5faf6; }
.bagging-section-details[open]>summary { border-bottom:1px solid #d9e3dc; }
.bagging-section-details>.bagging-top-grid,.bagging-section-details>.bagging-search-panel,.bagging-section-details>.bagging-history-panel { margin:0;padding:12px;border:0;border-radius:0; }
.bagging-phone-field { grid-column:1/-1; }
.bagging-phone-field textarea { width:100%;box-sizing:border-box;min-height:54px; }
.bagging-phone-field small { text-transform:none;letter-spacing:0;color:#64748b;font-weight:600; }
.bagging-sticky-head { position:sticky;top:4px;z-index:18;background:#fff;padding:8px;border:1px solid #d9e3dc;border-radius:10px; }
.bagging-summary-card { border: 1px solid #d9e3dc; border-radius: 8px; background: #f8fbf8; padding: 10px 12px; display: grid; gap: 4px; align-content: center; }
.bagging-summary-card strong { font-size: 0.78rem; letter-spacing: 0.06em; text-transform: uppercase; color: #31572c; }
.bagging-summary-card span { font-size: 1rem; font-weight: 900; color: #0f1f16; }
.bagging-summary-card small { font-size: 0.8rem; color: #5d6f63; }
.bagging-search-panel { border: 1px solid #d9e3dc; border-radius: 10px; padding: 12px; background: #f8fbf8; margin-bottom: 14px; }
.bagging-show-anchor { border:2px solid #31572c;border-radius:12px;background:#f0f7f1;padding:12px;margin:12px 0;display:grid;grid-template-columns:minmax(220px,1fr) minmax(190px,.7fr);gap:12px;align-items:end; }
.bagging-show-anchor label,.bagging-show-anchor>div { display:grid;gap:5px; }
.bagging-show-anchor input { min-height:50px;border:1px solid #8ea391;border-radius:8px;padding:8px;font-size:1rem;background:#fff;color:#0f1f16;box-sizing:border-box;width:100%; }
.bagging-show-anchor>div { border-radius:9px;background:#17331f;color:#fff;padding:10px 12px; }
.bagging-show-anchor>div small { color:#d9eadc;font-weight:800;text-transform:uppercase;letter-spacing:.05em; }
.bagging-show-anchor>div strong { font-size:1.2rem; }
.bagging-show-anchor>div span { font-size:.82rem; }
.bagging-cow-overview { border:2px solid #31572c;border-radius:12px;background:#fff;margin:14px 0;padding:12px;display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:8px; }
.bagging-cow-overview .bagging-timeline-heading { grid-column:1/-1; }
.bagging-cow-quick-row { min-width:0;border:1px solid #a9bbaa;border-radius:9px;background:#f8fbf8;padding:9px;display:grid;grid-template-columns:1fr auto;gap:7px; }
.bagging-cow-quick-row strong { font-size:1rem; }
.bagging-cow-quick-row>span { color:#31572c;font-size:.78rem;font-weight:850;text-align:right; }
.bagging-cow-quick-row input { grid-column:1/-1;width:100%;min-height:46px;box-sizing:border-box;border:1px solid #8ea391;border-radius:8px;padding:7px;background:#fff;color:#0f1f16;font-size:.95rem; }
.bagging-cow-quick-row button { grid-column:1/-1;min-height:38px;border:1px solid #31572c;border-radius:8px;background:#eef6ef;color:#17331f;font-weight:900; }
.bagging-primary-save { background:#17331f!important;color:#fff!important;border-color:#17331f!important; }
.cow-show-clock { font-size:1rem;font-weight:950;color:#17331f; }
.bagging-timeline { border:2px solid #31572c;border-radius:12px;background:#fff;margin:14px 0;padding:12px;display:grid;gap:10px; }
.bagging-timeline-heading { display:flex;justify-content:space-between;align-items:center;gap:10px;color:#17331f;font-size:1.05rem; }
.bagging-timeline-heading span { background:#e8f5ea;border-radius:999px;padding:4px 10px;font-size:.8rem;font-weight:900; }
.bagging-time-group { border:1px solid #cbd9ce;border-radius:10px;padding:10px;background:#f8fbf8;display:grid;gap:7px; }
.bagging-time-group time { font-size:1rem;font-weight:950;color:#17331f; }
.bagging-time-items { display:grid;grid-template-columns:repeat(auto-fit,minmax(175px,1fr));gap:7px; }
.bagging-time-items button { min-height:52px;border:1px solid #9fb2a3;border-radius:9px;background:#fff;color:#0f1f16;padding:8px 10px;text-align:left;display:grid;gap:2px;cursor:pointer; }
.bagging-time-items button strong { font-size:.95rem; }
.bagging-time-items button span,.bagging-time-group small { color:#31572c;font-size:.8rem;font-weight:750; }
.bagging-search-hint { margin: 0 0 8px; font-size: 0.84rem; color: #31572c; font-weight: 700; }
.bagging-search-tools { display: flex; align-items: center; justify-content: space-between; gap: 10px; margin-bottom: 8px; font-size: 0.82rem; color: #5d6f63; }
.bagging-tools-note { font-weight: 700; color: #31572c; }
.bagging-glance-panel { border: 1px solid #d9e3dc; border-radius: 10px; padding: 12px; background: #f8fbf8; margin-bottom: 14px; }
.bagging-group-summary,
.bagging-edit-summary,
.achievement-group-summary { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 10px 12px; background: #f0f7f1; color: #17331f; font-weight: 900; text-transform: uppercase; letter-spacing: 0.05em; }
.bagging-group-summary span,
.bagging-edit-summary span,
.achievement-group-summary span { font-size: 0.74rem; color: #31572c; }
.bagging-group-glance,
.bagging-edit-group-body,
.achievement-group-body { padding: 12px; display: grid; gap: 10px; }
.bagging-glance-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 10px; }
.bagging-glance-card { border: 1px solid #d0ddd3; border-radius: 10px; background: #fff; padding: 10px; display: grid; gap: 6px; }
.bagging-glance-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.bagging-glance-head strong { color: #0f1f16; font-size: 0.92rem; }
.bagging-glance-meta { font-size: 0.78rem; color: #31572c; font-weight: 800; }
.bagging-glance-entry { font-size: 0.82rem; color: #0f1f16; font-weight: 700; }
.bagging-jump-btn { border: 1px solid #31572c; background: #f0f7f1; color: #17331f; border-radius: 999px; min-height: 30px; padding: 0 10px; font-size: 0.74rem; font-weight: 900; letter-spacing: 0.04em; cursor: pointer; }
.bagging-jump-btn:hover { background: #e1efe4; }
.bagging-glance-quarters { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 6px; }
.bagging-glance-quarter { border: 1px solid #e0e8e1; border-radius: 8px; padding: 6px; display: grid; gap: 2px; background: #fdfefe; }
.bagging-glance-quarter span { font-size: 0.72rem; color: #5d6f63; font-weight: 800; }
.bagging-glance-quarter strong { font-size: 0.86rem; color: #0f1f16; }
.bagging-glance-quarter small { font-size: 0.75rem; color: #31572c; font-weight: 700; }
.bagging-card { border: 1px solid #d9e3dc; border-radius: 10px; padding: 12px; background: #fff; margin: 10px 0; }
.cow-bagging-details.quick-edit-opened { box-shadow:0 0 0 4px rgba(49,87,44,.22); }
.bagging-quick-edit { display:grid;grid-template-columns:minmax(220px,1fr) minmax(180px,.65fr);gap:10px;margin-bottom:12px;padding:10px;border:2px solid #31572c;border-radius:10px;background:#f0f7f1; }
.bagging-quick-edit label,.bagging-quick-edit>div { display:grid;gap:5px; }
.bagging-quick-edit input { min-height:50px;width:100%;box-sizing:border-box;border:1px solid #8ea391;border-radius:8px;padding:8px;font-size:1rem;background:#fff;color:#0f1f16; }
.bagging-quick-edit>div { border-radius:8px;background:#17331f;color:#fff;padding:9px 11px; }
.bagging-quick-edit>div small { color:#cfe3d2;text-transform:uppercase;font-weight:850;font-size:.7rem; }
.bagging-quick-edit>div strong { font-size:1.05rem; }
.bagging-quick-edit>div span { font-size:.78rem; }
.bagging-save-bottom { width:100%;min-height:52px;margin-top:12px;border:0;border-radius:9px;background:#31572c;color:#fff;font-size:1rem;font-weight:950;cursor:pointer; }
.bagging-card-hd { display: flex; align-items: start; justify-content: space-between; gap: 10px; margin-bottom: 10px; }
.bagging-show-link { border: none; background: transparent; color: #31572c; font-size: 1rem; font-weight: 900; padding: 0; cursor: pointer; text-align: left; }
.bagging-cow-line { font-size: 0.85rem; color: #5d6f63; margin-top: 4px; }
.bagging-card-actions { display: flex; gap: 8px; align-items: center; }
.bagging-meta-grid { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 10px; margin-bottom: 12px; }
.bagging-success-toggle { display: flex; align-items: center; gap: 8px; text-transform: none; letter-spacing: 0; font-size: 0.86rem; color: #0f1f16; }
.reminder-toggle { border: 2px solid #31572c; border-radius: 10px; padding: 10px 12px; background: #f0f7f1; font-weight: 900; }
.reminder-toggle input { width: 22px; height: 22px; accent-color: #31572c; }
.bagging-entry-summary { border: 1px solid #e0e8e1; border-radius: 8px; padding: 10px; background: #f8fbf8; display: grid; gap: 4px; align-content: center; }
.bagging-entry-summary strong { color: #0f1f16; font-size: 0.86rem; }
.bagging-entry-summary span { color: #5d6f63; font-size: 0.82rem; }
.bagging-udder-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-bottom: 10px; }
.udder-quarter { border: 1px solid #c8d4cb; border-radius: 10px; background: #fff; color: #0f1f16; padding: 12px; display: grid; gap: 3px; cursor: pointer; text-align: left; min-height: 132px; }
.udder-quarter:hover { border-color: #31572c; background: #f0f7f1; }
.exact-times .udder-quarter { cursor: default; min-height: 0; gap: 7px; }
.exact-times .udder-quarter input { width: 100%; min-height: 48px; box-sizing: border-box; border: 1px solid #9fb2a3; border-radius: 8px; padding: 8px; font-size: 1rem; background: #fff; color: #0f1f16; }
.quarter-label { font-size: 0.82rem; font-weight: 900; letter-spacing: 0.04em; text-transform: uppercase; color: #31572c; }
.quarter-hours-label { font-size: 0.7rem; color: #5d6f63; letter-spacing: 0.04em; text-transform: uppercase; }
.quarter-hours-value { font-size: 1.08rem; color: #0f1f16; font-weight: 900; }
.quarter-time-label { color: #5d6f63; font-size: 0.7rem; letter-spacing: 0.04em; text-transform: uppercase; }
.quarter-time-value { color: #0f1f16; font-size: 0.9rem; font-weight: 800; }
.bagging-notes { display: grid; gap: 6px; }

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
  .rp-hero { padding: 14px 12px; }
  .rp-hero-top, .rp-ph { align-items: stretch; flex-direction: column; }
  .rp-hero-actions, .rp-ph-actions { display: grid; grid-template-columns: 1fr; width: 100%; }
  .rp-back, .rp-add-btn, .rp-print-link { width: 100%; min-height: 46px; justify-content: center; box-sizing: border-box; }
  .rp-panel { margin: 10px 8px; padding: 14px 12px; }
  .emb-hd { align-items: stretch; flex-direction: column; }
  .emb-actions { display: grid; grid-template-columns: 1fr; gap: 8px; }
  .emb-actions .rp-add-btn { width: 100%; }
  .implant-record-card { grid-template-columns: 1fr; }
  .chart-row-2 { grid-template-columns: 1fr; }
  .bar-chart { overflow-x: auto; overflow-y: hidden; padding: 18px 2px 4px; scroll-snap-type: x proximity; }
  .bar-col { flex: 0 0 40px; min-width: 40px; scroll-snap-align: start; }
  .bar-label { font-size: .68rem; }
  .browse-filters { grid-template-columns: 1fr; }
  .emb-grid { grid-template-columns: 1fr; }
  .emb-full { grid-column: 1; }
  .browse-grid { grid-template-columns: 1fr; max-height: none; }
  .rp-row-card { grid-template-columns: 1fr; }
  .rp-full { grid-column: 1; }
  .rp-tabs button { padding: 12px 12px 9px; font-size: 0.75rem; }
  .rp-categories { grid-template-columns:repeat(2,minmax(0,1fr));padding:9px 8px 5px;gap:6px; }
  .rp-categories button { min-height:46px;font-size:.8rem; }
  .rp-tabs { display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px;padding:8px;overflow:visible; }
  .rp-tabs button { justify-content:center;border:1px solid #d5dfd7;border-radius:8px;padding:10px 6px;white-space:normal;text-align:center; }
  .rp-tabs button.active { border-color:#31572c;background:#eef6ef; }
  .bagging-top-grid { grid-template-columns: 1fr; }
  .bagging-show-anchor { grid-template-columns: 1fr; }
  .bagging-page-head { gap:10px;margin-bottom:10px;padding-bottom:10px; }
  .bagging-page-head .rp-ph-actions { grid-template-columns:1fr 1fr;gap:7px; }
  .bagging-page-head .rp-add-btn { min-height:48px;padding:7px 8px;font-size:.8rem; }
  .bagging-simple-hint { margin-bottom:10px; }
  .bagging-show-anchor { margin:8px 0 10px;padding:10px;gap:8px; }
  .bagging-show-anchor input { min-height:52px;font-size:16px; }
  .bagging-section-details { margin-bottom:10px; }
  .bagging-section-details>summary { min-height:48px;display:flex;align-items:center;padding:10px 12px;box-sizing:border-box; }
  .bagging-section-details>.bagging-search-panel { padding:10px; }
  .bagging-search-panel .rp-list-search { min-height:50px;font-size:16px; }
  .bagging-cow-overview { grid-template-columns:1fr;padding:9px;gap:6px; }
  .bagging-cow-overview>button { min-width:0; }
  .bagging-meta-grid { grid-template-columns: 1fr; }
  .bagging-quick-edit { grid-template-columns:1fr; }
  .bagging-udder-grid { grid-template-columns:1fr;gap:8px; }
  .bagging-edit-summary { min-height:54px;padding:10px 12px;box-sizing:border-box; }
  .bagging-edit-group-body { display:block; }
  .bagging-card { padding:10px;margin:6px 0; }
  .bagging-quick-edit input,.bagging-meta-grid input,.bagging-meta-grid select,.bagging-notes textarea,.udder-quarter input { font-size:16px;min-height:48px;box-sizing:border-box;width:100%; }
  .bagging-card-hd { flex-direction: column; }
  .bagging-card-actions { width: 100%; justify-content: space-between; }
  .bagging-sticky-head { align-items:stretch; }
  .bagging-sticky-head .rp-ph-actions { display:grid;grid-template-columns:repeat(2,minmax(0,1fr)); }
  .bagging-search-tools { flex-direction: column; align-items: stretch; }
  .bagging-glance-grid { grid-template-columns: 1fr; }
  .udder-quarter { min-height:0;padding:12px; }
  .quarter-hours-value { font-size: 1rem; }
}
</style>
