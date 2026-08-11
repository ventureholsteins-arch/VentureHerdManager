<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'

import { getAnimal, markAnimalSold, restoreAnimal, setAnimalLocation } from '../api/animals'
import { getAnimalSnapshot } from '../api/animalsSnapshot'
import type { Animal } from '../models/Animal'
import type { AnimalSnapshot, AnimalTimelineEntry } from '../models/AnimalSnapshot'
import RetroIcon from '../components/RetroIcon.vue'
import EditAnimalModal from '../components/EditAnimalModal.vue'
import { getAnimalHerdData, getAdminKey, getMatingSuggestions } from '../api/herdData'

import {
  getAnimalNotes,
  recordAnimalNote,
  type AnimalNote
} from '../api/animalNotes'

import {
  getDryOffEvents,
  recordDryOff,
  type DryOffEvent
} from '../api/dryOffs'

import {
  getHeatEvents,
  recordHeat,
  type HeatEvent
} from '../api/heats'

import {
  getBreedings,
  recordBreeding,
  updatePregnancyStatus,
  type BreedingEvent
} from '../api/breeding'

import {
  getCalvings,
  recordCalving,
  deleteCalvingEvent,
  updateCalvingEvent,
  type CalvingEvent
} from '../api/calvings'

import {
  deleteLutEvent,
  getLutEvents,
  updateLutEvent,
  type LutalyseEvent
} from '../api/lut'

import {
  deleteBreedingEvent,
  updateBreedingEvent
} from '../api/breeding'

import {
  getAchievementsForAnimal,
  type ShowAchievement
} from '../api/showAchievements'

import {
  deleteHeatEvent,
  updateHeatEvent
} from '../api/heats'

import { uploadPhoto } from '../api/photos'
import { formatCurrentAge, getShowClassLabel } from '../utils/showClasses'
import { genomicLinearTraits, genomicSummaryFields, importedGenomicFields, linearPosition, numericTrait } from '../utils/genomicTraits'

const route = useRoute()
const router = useRouter()

const animal = ref<Animal | null>(null)
const editAnimalModalRef = ref<InstanceType<typeof EditAnimalModal>>()
const snapshot = ref<AnimalSnapshot | null>(null)

const heatEvents = ref<HeatEvent[]>([])
const breedingEvents = ref<BreedingEvent[]>([])
const calvingEvents = ref<CalvingEvent[]>([])
const dryOffEvents = ref<DryOffEvent[]>([])
const lutEvents = ref<LutalyseEvent[]>([])
const animalNotes = ref<AnimalNote[]>([])
const timelineEntries = ref<AnimalTimelineEntry[]>([])
const herdDataRecords = ref<any[]>([])
const matingData = ref<any>(null)
const latestGenomicRecord = computed(() => herdDataRecords.value.find(item => item.source === 2) ?? null)
const latestGenomicFields = computed(() => importedGenomicFields(latestGenomicRecord.value))
const animalLinear = computed(() => genomicLinearTraits
  .map(trait => ({ ...trait, value: numericTrait(latestGenomicFields.value, trait) }))
  .filter(trait => trait.value != null))
const genomicHighlights = computed(() => genomicSummaryFields
  .map(([csv, label, note]) => ({ csv, label, note, value: latestGenomicFields.value[csv] }))
  .filter(field => field.value != null && field.value !== ''))
const animalLinearWidth = linearPosition
function importedFields(record: any): Record<string, string> {
  try { return typeof record.rawDataJson === 'string' ? JSON.parse(record.rawDataJson) : record.rawDataJson ?? {} } catch { return {} }
}
const latestMilkRecord = computed(() => herdDataRecords.value.find(record => record.source === 1) ?? null)
const latestMilkFields = computed(() => latestMilkRecord.value ? importedFields(latestMilkRecord.value) : {})
const showAchievements = ref<ShowAchievement[]>([])

const loading = ref(true)

const showHeatForm = ref(false)
const heatSaving = ref(false)
const heatError = ref('')
const heatNotes = ref('')
const heatPhotoFile = ref<File | null>(null)
const hasEmbryoTransfer = ref(false)

const showBreedingForm = ref(false)
const sireUsed = ref('')
const breedingType = ref(0)
const breedingNotes = ref('')

const showPregCheckForm = ref(false)
const selectedBreedingId = ref<number | null>(null)
const pregnancyStatus = ref(1)
const pregCheckSaving = ref(false)
const pregCheckError = ref('')

const showCalvingForm = ref(false)
const calfSex = ref(0)
const calfBarnName = ref('')
const calfRegisteredName = ref('')
const calfSireName = ref('')
const calfDamName = ref('')
const calvingPhotoFile = ref<File | null>(null)
const calvingEase = ref(0)
const twins = ref(false)
const stillborn = ref(false)
const calvingNotes = ref('')
const isUploadingHeatPhoto = ref(false)
const isUploadingCalvingPhoto = ref(false)

const showDryOffForm = ref(false)
const dryReason = ref('')
const dryNotes = ref('')

const showNoteForm = ref(false)
const showSoldForm = ref(false)
const soldDate = ref(new Date().toISOString().slice(0, 10))
const soldNotes = ref('')
const soldSaving = ref(false)
const soldError = ref('')
const noteText = ref('')

const animalId = computed(() => Number(route.params.animalId))

async function openEditAnimal() {
  if (!animal.value) return
  animal.value.latestScore = snapshot.value?.latestClassificationRecord?.score ?? animal.value.latestScore
  animal.value.latestBaa = snapshot.value?.latestClassificationRecord?.baa ?? animal.value.latestBaa
  await nextTick(); editAnimalModalRef.value?.openModal()
}

async function onAnimalEdited(updated: Animal) {
  animal.value = updated
  await reloadAnimalData()
}

async function reloadAnimalData() {
  const animalSnapshot = await getAnimalSnapshot(animalId.value)

  snapshot.value = animalSnapshot
  animal.value = animalSnapshot.animal
  timelineEntries.value = animalSnapshot.timeline

  heatEvents.value = await getHeatEvents(animalId.value)
  breedingEvents.value = await getBreedings(animalId.value)
  calvingEvents.value = await getCalvings(animalId.value)
  dryOffEvents.value = await getDryOffEvents(animalId.value)
  lutEvents.value = await getLutEvents(animalId.value)
  animalNotes.value = await getAnimalNotes(animalId.value)
  showAchievements.value = await getAchievementsForAnimal(animalId.value)
}

const hasUnsavedFormChanges = computed(() => {
  const anyFormOpen =
    showHeatForm.value ||
    showBreedingForm.value ||
    showPregCheckForm.value ||
    showCalvingForm.value ||
    showDryOffForm.value ||
    showNoteForm.value ||
    showSoldForm.value

  if (!anyFormOpen) {
    return false
  }

  return (
    heatNotes.value.trim().length > 0 ||
    !!heatPhotoFile.value ||
    hasEmbryoTransfer.value ||
    sireUsed.value.trim().length > 0 ||
    breedingType.value !== 0 ||
    breedingNotes.value.trim().length > 0 ||
    selectedBreedingId.value !== null ||
    pregnancyStatus.value !== 1 ||
    calfSex.value !== 0 ||
    calfBarnName.value.trim().length > 0 ||
    calfRegisteredName.value.trim().length > 0 ||
    calfSireName.value.trim().length > 0 ||
    calfDamName.value.trim().length > 0 ||
    !!calvingPhotoFile.value ||
    calvingEase.value !== 0 ||
    twins.value ||
    stillborn.value ||
    calvingNotes.value.trim().length > 0 ||
    dryReason.value.trim().length > 0 ||
    dryNotes.value.trim().length > 0 ||
    noteText.value.trim().length > 0
  )
})

const beforeUnloadHandler = (event: BeforeUnloadEvent) => {
  if (!hasUnsavedFormChanges.value) {
    return
  }

  event.preventDefault()
  event.returnValue = ''
}

function goBack() {
  const returnTo =
    typeof route.query.returnTo === 'string'
      ? route.query.returnTo
      : null

  if (window.history.length > 1) {
    router.back()
    return
  }

  if (returnTo) {
    router.push(returnTo)
    return
  }

  router.push('/')
}

function closeAllForms() {
  showHeatForm.value = false
  showBreedingForm.value = false
  showPregCheckForm.value = false
  showCalvingForm.value = false
  showDryOffForm.value = false
  showNoteForm.value = false
  showSoldForm.value = false
  hasEmbryoTransfer.value = false
}

function openHeatForm() {
  closeAllForms()
  heatError.value = ''
  showHeatForm.value = true
}

function openBreedingForm() {
  closeAllForms()
  showBreedingForm.value = true
}

async function openPregCheckForm(preferredBreedingId?: number) {
  closeAllForms()
  pregCheckError.value = ''
  pregnancyStatus.value = 1
  if (breedingEvents.value.length === 0) {
    pregCheckSaving.value = true
    try {
      breedingEvents.value = await getBreedings(animalId.value)
    } catch (error) {
      pregCheckError.value = error instanceof Error ? error.message : 'Breeding history could not be loaded.'
    } finally {
      pregCheckSaving.value = false
    }
  }
  const preferred = preferredBreedingId == null
    ? undefined
    : breedingEvents.value.find(event => event.breedingEventId === preferredBreedingId)
  const eligible = preferred
    ?? breedingEvents.value.find(event => event.pregnancyStatus === 0 || event.pregnancyStatus === 3)
    ?? breedingEvents.value[0]
  selectedBreedingId.value = eligible?.breedingEventId ?? null
  showPregCheckForm.value = true
}

function openCalvingForm() {
  closeAllForms()
  calfDamName.value = animal.value?.barnName || animal.value?.registeredName || ''
  showCalvingForm.value = true
}

function openDryOffForm() {
  closeAllForms()
  showDryOffForm.value = true
}

function openNoteForm() {
  closeAllForms()
  showNoteForm.value = true
}
function openSoldForm() { closeAllForms(); showSoldForm.value = true; soldError.value = '' }
async function saveSold() {
  if (!animal.value) return
  soldSaving.value = true; soldError.value = ''
  try { animal.value = await markAnimalSold(animalId.value, `${soldDate.value}T12:00:00`, soldNotes.value); showSoldForm.value = false }
  catch (error) { soldError.value = error instanceof Error ? error.message : 'The sold status could not be saved.' }
  finally { soldSaving.value = false }
}
async function undoSold() {
  if (!animal.value || !window.confirm(`Restore ${animal.value.barnName || animal.value.registeredName || 'this animal'} to the active herd?`)) return
  try { animal.value = await restoreAnimal(animalId.value) } catch (error) { alert(error instanceof Error ? error.message : 'Restore failed.') }
}

async function changeHerdLocation(herdLocation: number) {
  if (!animal.value) return
  const destination = herdLocation === 1 ? "Mueller's" : 'the home herd'
  if (!window.confirm(`Move ${animal.value.barnName || animal.value.registeredName || 'this animal'} to ${destination}?`)) return
  try {
    animal.value = await setAnimalLocation(animal.value.animalId, herdLocation)
  } catch (error) {
    alert(error instanceof Error ? error.message : 'Herd location could not be updated.')
  }
}

function openPendingAction() {
  const routeAction = typeof route.query.action === 'string' ? route.query.action : null
  const pendingAction =
    routeAction ?? sessionStorage.getItem('pendingAnimalAction')

  if (pendingAction === 'heat') {
    openHeatForm()
  }

  if (pendingAction === 'breeding') {
    openBreedingForm()
  }

  if (pendingAction === 'calving') {
    openCalvingForm()
  }

  if (pendingAction === 'note') {
    openNoteForm()
  }

  if (pendingAction === 'pregCheck') {
    const routeBreedingId = Number(route.query.breedingId)
    void openPregCheckForm(Number.isInteger(routeBreedingId) && routeBreedingId > 0 ? routeBreedingId : undefined)
  }

  sessionStorage.removeItem('pendingAnimalAction')
}

onMounted(async () => {
  window.addEventListener('beforeunload', beforeUnloadHandler)

  try {
    const animalSnapshot = await getAnimalSnapshot(
      animalId.value
    )

    snapshot.value = animalSnapshot
    animal.value = animalSnapshot.animal
    timelineEntries.value = animalSnapshot.timeline
    loading.value = false
    openPendingAction()
    void Promise.all([
      getHeatEvents(animalId.value).then(value => { heatEvents.value = value }),
      getBreedings(animalId.value).then(value => { breedingEvents.value = value }),
      getCalvings(animalId.value).then(value => { calvingEvents.value = value }),
      getDryOffEvents(animalId.value).then(value => { dryOffEvents.value = value }),
      getLutEvents(animalId.value).then(value => { lutEvents.value = value }),
      getAnimalNotes(animalId.value).then(value => { animalNotes.value = value })
    ]).catch(error => console.warn('Some animal history is still loading:', error))
    if (getAdminKey()) void Promise.all([
      getAnimalHerdData(animalId.value).then(value => { herdDataRecords.value = value }),
      getMatingSuggestions(animalId.value).then(value => { matingData.value = value }).catch(() => null)
    ]).catch(error => console.warn('Private milk/genomic history is unavailable:', error))
  } catch (error) {
    console.error('Failed to load animal:', error)
  } finally {
    loading.value = false
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', beforeUnloadHandler)
})

onBeforeRouteLeave(() => {
  if (!hasUnsavedFormChanges.value) {
    return true
  }

  return window.confirm('You have unsaved form changes. Leave this page?')
})

async function saveHeat() {
  if (!animal.value || heatSaving.value) return
  heatSaving.value = true
  heatError.value = ''

  try {
    let pictureUrl: string | null = null

    if (heatPhotoFile.value) {
      isUploadingHeatPhoto.value = true
      pictureUrl = await uploadPhoto(heatPhotoFile.value, 'heat-events')
    }

    await recordHeat({
      animalId: animal.value.animalId,
      heatDateTime: new Date().toISOString(),
      heatStrength: 2,
      standingHeat: false,
      pictureUrl,
      notes: heatNotes.value,
      hasEmbryoTransfer: hasEmbryoTransfer.value,
      embryoImplantDate: hasEmbryoTransfer.value
        ? new Date(
            new Date().getTime() + 7 * 24 * 60 * 60 * 1000
          ).toISOString()
        : null,
      createdBy: 'Austin'
    })

    heatNotes.value = ''
    heatPhotoFile.value = null
    hasEmbryoTransfer.value = false
    showHeatForm.value = false

    await reloadAnimalData()
  } catch (error) {
    console.error('Failed to save heat:', error)
    heatError.value = error instanceof Error ? error.message : 'Heat event could not be saved. Try again.'
  } finally {
    isUploadingHeatPhoto.value = false
    heatSaving.value = false
  }
}

async function saveBreeding() {
  if (!animal.value || !sireUsed.value.trim()) return

  try {
    await recordBreeding({
      animalId: animal.value.animalId,
      breedingDate: new Date().toISOString(),
      sireUsed: sireUsed.value.trim(),
      breedingType: breedingType.value,
      pregnancyStatus: 0,
      notes: breedingNotes.value
    })

    sireUsed.value = ''
    breedingType.value = 0
    breedingNotes.value = ''
    showBreedingForm.value = false

    breedingEvents.value = await getBreedings(
      animal.value.animalId
    )
  } catch (error) {
    console.error('Failed to save breeding:', error)
  }
}

async function savePregCheck() {
  if (pregCheckSaving.value) return
  if (selectedBreedingId.value === null) {
    pregCheckError.value = 'Record a breeding or embryo transfer before adding a pregnancy check.'
    return
  }

  pregCheckSaving.value = true
  pregCheckError.value = ''

  const breedingEventId = selectedBreedingId.value
  const requestedStatus = pregnancyStatus.value

  try {
    try {
      await updatePregnancyStatus(breedingEventId, requestedStatus)
    } catch (saveError) {
      // Mobile Safari can lose the empty response after the API has committed
      // the update. Verify the stored value before telling the user it failed.
      const refreshed = await getBreedings(animalId.value)
      breedingEvents.value = refreshed
      const saved = refreshed.some(event =>
        event.breedingEventId === breedingEventId
        && event.pregnancyStatus === requestedStatus
      )
      if (!saved) throw saveError
    }

    showPregCheckForm.value = false
    breedingEvents.value = await getBreedings(animalId.value)
  } catch (error) {
    console.error(
      'Failed to save pregnancy check:',
      error
    )
    pregCheckError.value = error instanceof Error ? error.message : 'Pregnancy check could not be saved. Try again.'
  } finally {
    pregCheckSaving.value = false
  }
}

async function saveCalving() {
  if (!animal.value) return

  try {
    let pictureUrl: string | null = null

    if (calvingPhotoFile.value) {
      isUploadingCalvingPhoto.value = true
      pictureUrl = await uploadPhoto(calvingPhotoFile.value, 'calving-events')
    }

    await recordCalving(
      animal.value.animalId,
      calfSex.value,
      calfBarnName.value.trim(),
      calfRegisteredName.value.trim(),
      calfSireName.value.trim(),
      calfDamName.value.trim(),
      calvingEase.value,
      twins.value,
      stillborn.value,
      calvingNotes.value,
      pictureUrl
    )

    calfSex.value = 0
    calfBarnName.value = ''
    calfRegisteredName.value = ''
    calfSireName.value = ''
    calfDamName.value = ''
    calvingPhotoFile.value = null
    calvingEase.value = 0
    twins.value = false
    stillborn.value = false
    calvingNotes.value = ''
    showCalvingForm.value = false

    await reloadAnimalData()
  } catch (error) {
    console.error('Failed to save calving:', error)
    alert('Failed to save calving event.')
  } finally {
    isUploadingCalvingPhoto.value = false
  }
}

function onHeatPhotoSelected(event: Event) {
  const input = event.target as HTMLInputElement
  heatPhotoFile.value = input.files?.[0] ?? null
}

function onCalvingPhotoSelected(event: Event) {
  const input = event.target as HTMLInputElement
  calvingPhotoFile.value = input.files?.[0] ?? null
}

async function saveDryOff() {
  if (!animal.value) return

  try {
    await recordDryOff(
      animal.value.animalId,
      dryReason.value,
      dryNotes.value
    )

    dryReason.value = ''
    dryNotes.value = ''
    showDryOffForm.value = false

    dryOffEvents.value = await getDryOffEvents(
      animal.value.animalId
    )

    animal.value = await getAnimal(
      animal.value.animalId
    )
  } catch (error) {
    console.error('Failed to save dry off:', error)
  }
}

async function saveAnimalNote() {
  if (!animal.value || !noteText.value.trim()) return

  try {
    await recordAnimalNote(
      animal.value.animalId,
      noteText.value.trim()
    )

    noteText.value = ''
    showNoteForm.value = false

    animalNotes.value = await getAnimalNotes(
      animal.value.animalId
    )
  } catch (error) {
    console.error('Failed to save animal note:', error)
  }
}

function breedingTypeLabel(type: number) {
  const types: Record<number, string> = {
    0: 'AI',
    1: 'Natural',
    2: 'Embryo Transfer'
  }

  return types[type] ?? 'Unknown'
}

function pregnancyStatusLabel(status: number) {
  const statuses: Record<number, string> = {
    0: 'Unconfirmed',
    1: 'Pregnant',
    2: 'Open',
    3: 'Recheck',
    4: 'Aborted'
  }

  return statuses[status] ?? 'Unknown'
}

function calfSexLabel(sex: number) {
  const sexes: Record<number, string> = {
    0: 'Unknown',
    1: 'Bull',
    2: 'Heifer'
  }

  return sexes[sex] ?? 'Unknown'
}

function calvingEaseLabel(ease: number) {
  const easeLabels: Record<number, string> = {
    0: 'Unassisted',
    1: 'Easy Pull',
    2: 'Hard Pull',
    3: 'C-Section'
  }

  return easeLabels[ease] ?? 'Unknown'
}

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

async function editHeat(heat: HeatEvent) {
  const nextDate = window.prompt(
    'Edit heat date/time (YYYY-MM-DDTHH:mm)',
    toLocalDateTimeInput(heat.heatDateTime)
  )
  if (!nextDate) return

  const nextIso = toIsoFromInput(nextDate)
  if (!nextIso) {
    alert('Invalid heat date/time format.')
    return
  }

  try {
    await updateHeatEvent(heat.heatEventId, {
      heatDateTime: nextIso,
      notes: heat.notes ?? null,
      pictureUrl: heat.pictureUrl ?? null
    })
    heatEvents.value = await getHeatEvents(animalId.value)
  } catch (error) {
    console.error('Failed to edit heat:', error)
    alert('Failed to edit heat event.')
  }
}

async function deleteHeat(heat: HeatEvent) {
  const confirmed = window.confirm('Are you sure you want to delete this heat event?')
  if (!confirmed) return

  try {
    await deleteHeatEvent(heat.heatEventId)
    heatEvents.value = await getHeatEvents(animalId.value)
  } catch (error) {
    console.error('Failed to delete heat:', error)
    alert('Failed to delete heat event.')
  }
}

async function editBreeding(breeding: BreedingEvent) {
  const nextDate = window.prompt(
    'Edit breeding date/time (YYYY-MM-DDTHH:mm)',
    toLocalDateTimeInput(breeding.breedingDate)
  )
  if (!nextDate) return

  const nextIso = toIsoFromInput(nextDate)
  if (!nextIso) {
    alert('Invalid breeding date/time format.')
    return
  }

  try {
    await updateBreedingEvent(breeding.breedingEventId, {
      breedingDate: nextIso,
      sireUsed: breeding.sireUsed,
      breedingType: breeding.breedingType,
      pregnancyStatus: breeding.pregnancyStatus,
      notes: breeding.notes ?? null
    })
    breedingEvents.value = await getBreedings(animalId.value)
  } catch (error) {
    console.error('Failed to edit breeding:', error)
    alert('Failed to edit breeding event.')
  }
}

async function deleteBreeding(breeding: BreedingEvent) {
  const confirmed = window.confirm('Are you sure you want to delete this breeding event?')
  if (!confirmed) return

  try {
    await deleteBreedingEvent(breeding.breedingEventId)
    breedingEvents.value = await getBreedings(animalId.value)
  } catch (error) {
    console.error('Failed to delete breeding:', error)
    alert('Failed to delete breeding event.')
  }
}

async function editCalving(calving: CalvingEvent) {
  const nextDate = window.prompt(
    'Edit calving date/time (YYYY-MM-DDTHH:mm)',
    toLocalDateTimeInput(calving.calvingDate)
  )
  if (!nextDate) return

  const nextIso = toIsoFromInput(nextDate)
  if (!nextIso) {
    alert('Invalid calving date/time format.')
    return
  }

  try {
    await updateCalvingEvent(calving.calvingEventId, {
      calvingDate: nextIso,
      calfSex: calving.calfSex,
      calfBarnName: calving.calfBarnName ?? null,
      calfRegisteredName: calving.calfRegisteredName ?? null,
      calvingEase: calving.calvingEase,
      twins: calving.twins,
      stillborn: calving.stillborn,
      notes: calving.notes ?? null
    })
    calvingEvents.value = await getCalvings(animalId.value)
  } catch (error) {
    console.error('Failed to edit calving:', error)
    alert('Failed to edit calving event.')
  }
}

async function deleteCalving(calving: CalvingEvent) {
  const confirmed = window.confirm('Are you sure you want to delete this calving event?')
  if (!confirmed) return

  try {
    await deleteCalvingEvent(calving.calvingEventId)
    calvingEvents.value = await getCalvings(animalId.value)
  } catch (error) {
    console.error('Failed to delete calving:', error)
    alert('Failed to delete calving event.')
  }
}

async function editLut(lut: LutalyseEvent) {
  const nextDate = window.prompt(
    'Edit LUT administration date/time (YYYY-MM-DDTHH:mm)',
    toLocalDateTimeInput(lut.administrationDate)
  )
  if (!nextDate) return

  const nextIso = toIsoFromInput(nextDate)
  if (!nextIso) {
    alert('Invalid LUT date/time format.')
    return
  }

  const startIso = toIsoFromInput(toLocalDateTimeInput(lut.expectedHeatWatchStart))
  const endIso = toIsoFromInput(toLocalDateTimeInput(lut.expectedHeatWatchEnd))

  if (!startIso || !endIso) {
    alert('Unable to parse LUT heat-watch dates.')
    return
  }

  try {
    await updateLutEvent(lut.lutalyseEventId, {
      administrationDate: nextIso,
      expectedHeatWatchStart: startIso,
      expectedHeatWatchEnd: endIso,
      heatObserved: lut.heatObserved ?? false,
      notes: lut.notes ?? null
    })
    lutEvents.value = await getLutEvents(animalId.value)
  } catch (error) {
    console.error('Failed to edit LUT event:', error)
    alert('Failed to edit LUT event.')
  }
}

async function deleteLut(lut: LutalyseEvent) {
  const confirmed = window.confirm('Are you sure you want to delete this LUT event?')
  if (!confirmed) return

  try {
    await deleteLutEvent(lut.lutalyseEventId)
    lutEvents.value = await getLutEvents(animalId.value)
  } catch (error) {
    console.error('Failed to delete LUT event:', error)
    alert('Failed to delete LUT event.')
  }
}

const stageLabel = computed(() => {
  if (!animal.value) return 'Unknown'

  const stages: Record<number, string> = {
    0: 'Unknown',
    1: 'Calf',
    2: 'Heifer',
    3: 'Milking',
    4: 'Dry',
    5: 'Bull',
    6: 'Sold',
    7: 'Deceased'
  }

  return stages[animal.value.animalStage] ?? 'Unknown'
})

const sexLabel = computed(() => {
  if (!animal.value) return 'Unknown'

  const sexes: Record<number, string> = {
    0: 'Unknown',
    1: 'Female',
    2: 'Male'
  }

  return sexes[animal.value.sex] ?? 'Unknown'
})

const ageLabel = computed(() => {
  if (!animal.value) return 'Unknown'
  return formatCurrentAge(animal.value.birthDate)
})

const showClassLabel = computed(() => {
  if (!animal.value) return 'Class TBD'
  return getShowClassLabel(animal.value.birthDate, animal.value.animalStage)
})

const scoreLabel = computed(() => {
  const score = snapshot.value?.latestClassificationRecord?.score ?? animal.value?.latestScore
  if (score == null) return 'Not scored'
  const savedLabel = snapshot.value?.latestClassificationRecord?.classificationLabel?.trim()
  if (savedLabel) return /\d/.test(savedLabel) ? savedLabel : `${savedLabel} ${Math.round(score)}`
  if (score >= 90) return `EX ${Math.round(score)}`
  if (score >= 85) return `VG ${Math.round(score)}`
  return `GP ${Math.round(score)}`
})
</script>

<template>
  <div class="page">
    <button class="back" @click="goBack">
      ← Back
    </button>
    <EditAnimalModal ref="editAnimalModalRef" :animal="animal" @saved="onAnimalEdited" />

    <p v-if="loading">
      Loading...
    </p>

    <div v-else-if="animal">
      <section class="hero">
        <div class="avatar">
          🐄
        </div>

        <div>
          <p class="eyebrow">
            {{ stageLabel }} · {{ sexLabel }}
          </p>

          <h1>
            {{ animal.barnName || 'Unnamed Animal' }}
          </h1>

          <p>
            {{ animal.registeredName || 'No registered name' }}
          </p>

          <small>
            Reg #: {{ animal.registrationNumber || 'None' }}
          </small>
        </div>
        <button type="button" class="edit-animal-button" @click="openEditAnimal">Edit Animal</button>
      </section>

      <section class="info-grid">
        <div class="info-card">
          <span>Stage</span>
          <strong>{{ stageLabel }}</strong>
        </div>

        <div class="info-card">
          <span>Breed</span>
          <strong>{{ animal.breed || 'Unknown' }}</strong>
        </div>

        <div class="info-card">
          <span>Current Lactation</span>
          <strong>
            {{ animal.currentLactation ?? 'Not set' }}
          </strong>
        </div>

        <div class="info-card">
          <span>Birth Date</span>
          <strong>{{ animal.birthDate || 'Unknown' }}</strong>
        </div>

        <div class="info-card">
          <span>Age</span>
          <strong>{{ ageLabel }}</strong>
        </div>

        <div class="info-card">
          <span>Show Class</span>
          <strong>{{ showClassLabel }}</strong>
        </div>

        <div class="info-card">
          <span>Score</span>
          <strong>{{ scoreLabel }}</strong>
        </div>
      </section>
      <section v-if="animal.animalStatus === 1" class="sold-banner"><div><strong>SOLD - ARCHIVED</strong><span>{{ animal.soldDate ? new Date(animal.soldDate).toLocaleDateString() : 'Sold animal' }} · All records are retained</span><p v-if="animal.soldNotes">{{ animal.soldNotes }}</p></div><button @click="undoSold">Restore to active herd</button></section>
      <section v-if="animal.animalStatus === 0 && animal.herdLocation === 1" class="sold-banner muellers-banner"><div><strong>ACTIVE AT MUELLER'S</strong><span>Still part of the active herd · All records and reminders continue</span></div><button @click="changeHerdLocation(0)">Return to Home Herd</button></section>

      <section v-if="latestMilkRecord" class="latest-milk-strip">
        <div class="milk-strip-title"><span>LATEST MILK TEST</span><strong>{{ latestMilkRecord.reportDate }}</strong></div>
        <article><small>Current milk</small><strong>{{ latestMilkRecord.milk ?? '—' }}</strong></article>
        <article v-if="latestMilkFields['Previous Milk']"><small>Previous milk</small><strong>{{ latestMilkFields['Previous Milk'] }}</strong></article>
        <article v-if="latestMilkFields['Milk Deviation']"><small>Change</small><strong :class="{ down: String(latestMilkFields['Milk Deviation']).startsWith('-') }">{{ latestMilkFields['Milk Deviation'] }}</strong></article>
        <article><small>DIM</small><strong>{{ latestMilkRecord.daysInMilk ?? '—' }}</strong></article>
        <article v-if="latestMilkFields['Current SCC']"><small>SCC</small><strong>{{ latestMilkFields['Current SCC'] }}</strong></article>
        <article v-if="latestMilkRecord.fatPercent != null"><small>Fat</small><strong>{{ latestMilkRecord.fatPercent }}%</strong></article>
        <article v-if="latestMilkRecord.proteinPercent != null"><small>Protein</small><strong>{{ latestMilkRecord.proteinPercent }}%</strong></article>
        <button @click="router.push('/reports/herd-data?view=milk')">Milk analytics →</button>
      </section>

      <section class="panel">
        <h2>Pedigree</h2>

        <div class="pedigree">
          <div>
            <span>Sire</span>

            <strong>
              {{ animal.sireName || 'Unknown' }}
            </strong>
          </div>

          <div>
            <span>Dam</span>

            <strong>
              {{ animal.damName || 'Unknown' }}
            </strong>
          </div>
        </div>
      </section>

      <section class="panel">
        <h2>Quick Actions</h2>

        <div class="actions">
          <button @click="openHeatForm">
            <RetroIcon name="heat" :size="30" />
            <span>Record Heat</span>
          </button>

          <button @click="openBreedingForm">
            <RetroIcon name="embryo" :size="30" />
            <span>Breed</span>
          </button>

          <button @click="openPregCheckForm">
            <RetroIcon name="pregCheck" :size="30" />
            <span>Preg Check</span>
          </button>

          <button @click="openCalvingForm">
            <RetroIcon name="calving" :size="30" />
            <span>Calved</span>
          </button>

          <button @click="openDryOffForm">
            <RetroIcon name="dryOff" :size="30" />
            <span>Dry Off</span>
          </button>

          <button @click="openNoteForm">
            <RetroIcon name="note" :size="30" />
            <span>Notes</span>
          </button>
          <button v-if="animal.animalStatus !== 1 && animal.sex === 1" class="sold-action" @click="openSoldForm"><span class="sold-icon">$</span><span>Mark Sold</span></button>
          <button v-if="animal.animalStatus === 0 && animal.sex === 1" class="muellers-action" @click="changeHerdLocation(animal.herdLocation === 1 ? 0 : 1)"><span class="sold-icon">M</span><span>{{ animal.herdLocation === 1 ? 'Return Home' : "Move to Mueller's" }}</span></button>
        </div>

        <div v-if="showSoldForm" class="form-card sold-form"><h3>Mark Animal Sold</h3><p>This removes her from the active herd but keeps her card, timeline, milk, genomics, photos, pedigree, embryos, and every other record.</p><p v-if="soldError" class="form-error">{{ soldError }}</p><label>Sold date</label><input v-model="soldDate" type="date"><label>Buyer / sale notes (optional)</label><textarea v-model="soldNotes" rows="3" placeholder="Buyer, sale price, destination, reason, or other notes"></textarea><div class="form-actions"><button class="save sold-confirm" :disabled="soldSaving || !soldDate" @click="saveSold">{{ soldSaving ? 'Saving…' : 'Confirm Sold - Keep All Data' }}</button><button class="cancel" @click="showSoldForm = false">Cancel</button></div></div>

        <div
          v-if="showHeatForm"
          class="form-card"
        >
          <h3>Record Heat</h3>
          <p v-if="heatError" class="form-error">{{ heatError }}</p>

          <label>Heat Notes</label>

          <textarea
            v-model="heatNotes"
            placeholder="Standing heat, activity, mucus, etc."
          />

          <label>Upload Heat Photo</label>

          <input
            type="file"
            accept="image/*"
            capture="environment"
            @change="onHeatPhotoSelected"
          >

          <small
            v-if="heatPhotoFile"
            class="upload-hint"
          >
            Selected: {{ heatPhotoFile.name }}
          </small>

          <small
            v-if="isUploadingHeatPhoto"
            class="upload-hint"
          >
            Uploading photo...
          </small>

          <label class="checkbox-label">
            <input
              v-model="hasEmbryoTransfer"
              type="checkbox"
            >
            <span class="day7-label">Plan embryo transfer<br>on day 7</span>
          </label>

          <div class="form-actions">
            <button
              class="save"
              :disabled="heatSaving"
              @click="saveHeat"
            >
              {{ heatSaving ? 'Saving…' : 'Save Heat' }}
            </button>

            <button
              class="cancel"
              @click="showHeatForm = false"
            >
              Cancel
            </button>
          </div>
        </div>

        <div
          v-if="showBreedingForm"
          class="form-card"
        >
          <h3>Record Breeding</h3>

          <label>Sire Used</label>

          <input
            v-model="sireUsed"
            placeholder="Master, Detective, Unix, etc."
          >

          <label>Breeding Type</label>

          <select v-model.number="breedingType">
            <option :value="0">AI</option>
            <option :value="1">Natural</option>
            <option :value="2">Embryo Transfer</option>
          </select>

          <label>Breeding Notes</label>

          <textarea
            v-model="breedingNotes"
            placeholder="Breeding notes"
          />

          <div class="form-actions">
            <button
              class="save"
              @click="saveBreeding"
            >
              Save Breeding
            </button>

            <button
              class="cancel"
              @click="showBreedingForm = false"
            >
              Cancel
            </button>
          </div>
        </div>

        <div
          v-if="showPregCheckForm"
          class="form-card"
        >
          <h3>Record Pregnancy Check</h3>

          <p v-if="breedingEvents.length === 0" class="form-error">No breeding or embryo transfer is recorded for this animal yet.</p>
          <p v-if="pregCheckError" class="form-error">{{ pregCheckError }}</p>

          <label>Breeding</label>

          <select v-model.number="selectedBreedingId">
            <option
              v-for="breeding in breedingEvents"
              :key="breeding.breedingEventId"
              :value="breeding.breedingEventId"
            >
              {{ breeding.sireUsed }} -
              {{
                new Date(
                  breeding.breedingDate
                ).toLocaleDateString()
              }}
            </option>
          </select>

          <label>Status</label>

          <select v-model.number="pregnancyStatus">
            <option :value="1">Pregnant</option>
            <option :value="2">Open</option>
            <option :value="3">Recheck</option>
            <option :value="4">Aborted</option>
          </select>

          <div class="form-actions">
            <button
              class="save"
              :disabled="pregCheckSaving || selectedBreedingId === null"
              @click="savePregCheck"
            >
              {{ pregCheckSaving ? 'Saving…' : 'Save Preg Check' }}
            </button>

            <button
              class="cancel"
              @click="showPregCheckForm = false"
            >
              Cancel
            </button>
          </div>
        </div>

        <div
          v-if="showCalvingForm"
          class="form-card"
        >
          <h3>Record Calving</h3>

          <label>Calf Sex</label>

          <select v-model.number="calfSex">
            <option :value="0">Unknown</option>
            <option :value="1">Bull</option>
            <option :value="2">Heifer</option>
          </select>

          <label>Calf Barn Name</label>

          <input
            v-model="calfBarnName"
            placeholder="Optional"
          >

          <label>Calf Registered Name</label>

          <input
            v-model="calfRegisteredName"
            placeholder="Optional"
          >

          <label>Calf Sire Name</label>

          <input
            v-model="calfSireName"
            placeholder="Optional"
          >

          <label>Calf Dam Name</label>

          <input
            v-model="calfDamName"
            placeholder="Optional"
          >

          <label>Upload Calving Photo</label>

          <input
            type="file"
            accept="image/*"
            capture="environment"
            @change="onCalvingPhotoSelected"
          >

          <small
            v-if="calvingPhotoFile"
            class="upload-hint"
          >
            Selected: {{ calvingPhotoFile.name }}
          </small>

          <small
            v-if="isUploadingCalvingPhoto"
            class="upload-hint"
          >
            Uploading photo...
          </small>

          <label>Calving Ease</label>

          <select v-model.number="calvingEase">
            <option :value="0">Unassisted</option>
            <option :value="1">Easy Pull</option>
            <option :value="2">Hard Pull</option>
            <option :value="3">C-Section</option>
          </select>

          <div class="checkbox-grid">
            <label class="checkbox-row">
              <input
                v-model="twins"
                type="checkbox"
              >
              Twins
            </label>

            <label class="checkbox-row">
              <input
                v-model="stillborn"
                type="checkbox"
              >
              Stillborn
            </label>
          </div>

          <label>Calving Notes</label>

          <textarea
            v-model="calvingNotes"
            placeholder="Calving details, assistance, calf condition, etc."
          />

          <div class="form-actions">
            <button
              class="save"
              @click="saveCalving"
            >
              Save Calving
            </button>

            <button
              class="cancel"
              @click="showCalvingForm = false"
            >
              Cancel
            </button>
          </div>
        </div>

        <div
          v-if="showDryOffForm"
          class="form-card"
        >
          <h3>Record Dry Off</h3>

          <label>Reason</label>

          <input
            v-model="dryReason"
            placeholder="Scheduled dry off, mastitis, etc."
          >

          <label>Notes</label>

          <textarea
            v-model="dryNotes"
            placeholder="Optional notes..."
          />

          <div class="form-actions">
            <button
              class="save"
              @click="saveDryOff"
            >
              Save Dry Off
            </button>

            <button
              class="cancel"
              @click="showDryOffForm = false"
            >
              Cancel
            </button>
          </div>
        </div>

        <div
          v-if="showNoteForm"
          class="form-card"
        >
          <h3>Add Note</h3>

          <label>Note</label>

          <textarea
            v-model="noteText"
            placeholder="Enter note..."
          />

          <div class="form-actions">
            <button
              class="save"
              @click="saveAnimalNote"
            >
              Save Note
            </button>

            <button
              class="cancel"
              @click="showNoteForm = false"
            >
              Cancel
            </button>
          </div>
        </div>
      </section>

      <section class="panel">
        <div class="private-data-heading">
          <h2>Milk &amp; Genomic History</h2>
          <button class="mini-btn" type="button" @click="router.push('/reports/herd-data')">Open analytics</button>
        </div>
        <p v-if="!getAdminKey()" class="upload-hint">Unlock the private Milk &amp; Genomic Analytics page to display these records.</p>
        <div v-else-if="herdDataRecords.length === 0" class="timeline-card"><strong>No imported milk or genomic records yet.</strong></div>
        <div v-else class="data-history-grid">
          <article v-for="record in herdDataRecords" :key="record.animalDataRecordId" class="data-history-card">
            <strong>{{ record.source === 1 ? 'PC-DART milk test' : 'Zoetis genomic evaluation' }}</strong>
            <small>{{ record.reportDate }}<template v-if="record.source === 1 && record.sourceAnimalName"> · PC-DART name: {{ record.sourceAnimalName }}</template></small>
            <div v-if="record.source === 1"><span>Milk {{ record.milk ?? '—' }}</span><span>DIM {{ record.daysInMilk ?? '—' }}</span><span>Fat {{ record.fatPercent ?? '—' }}%</span><span>Protein {{ record.proteinPercent ?? '—' }}%</span></div>
            <div v-else><span>TPI {{ record.tpi ?? '—' }}</span><span>NM$ {{ record.netMerit ?? '—' }}</span><span>Milk PTA {{ record.milkPta ?? '—' }}</span><span>DPR {{ record.daughterPregnancyRate ?? '—' }}</span><span>Type {{ record.typeScore ?? '—' }}</span><span>UDC {{ record.udderComposite ?? '—' }}</span><span>FLC {{ record.feetLegsComposite ?? '—' }}</span></div>
            <details v-if="importedFields(record)['Report Type']" class="imported-full-record"><summary>{{ importedFields(record)['Report Type'] }}</summary><div class="imported-field-grid"><template v-for="(value, key) in importedFields(record)" :key="key"><span v-if="key !== 'Full Cow Record' && value"><small>{{ key }}</small><strong>{{ value }}</strong></span></template></div><pre v-if="importedFields(record)['Full Cow Record']">{{ importedFields(record)['Full Cow Record'] }}</pre></details>
          </article>
        </div>
        <div v-if="genomicHighlights.length" class="genomic-highlight-grid">
          <article v-for="field in genomicHighlights" :key="field.csv" class="genomic-highlight-card">
            <small>{{ field.csv }}</small>
            <strong>{{ field.value }}</strong>
            <span>{{ field.label }}</span>
          </article>
        </div>
        <div v-if="animalLinear.length" class="animal-linear">
          <div class="private-data-heading"><h3>Linear at a Glance</h3><button class="mini-btn" type="button" @click="router.push('/reports/herd-data?view=linear')">Compare whole farm</button></div>
          <div v-for="trait in animalLinear" :key="trait.label" class="animal-linear-row"><span><b>{{ trait.csv }}</b> {{ trait.label }}</span><div><i :style="{ width: animalLinearWidth(trait.value) }"></i></div><strong>{{ trait.value ?? '—' }}</strong></div>
        </div>
        <div v-if="matingData" class="mating-review">
          <h3>Linear &amp; Mating Suggestions</h3>
          <p class="upload-hint">Suggestions prioritize sires that improve this animal’s weaker genomic composites. Always review pedigree, recessives, inbreeding, calving ease, and your mating goals before breeding.</p>
          <div class="cow-proof-row"><span>TPI {{ matingData.cow.tpi ?? '—' }}</span><span>Milk {{ matingData.cow.milkPta ?? '—' }}</span><span>DPR {{ matingData.cow.daughterPregnancyRate ?? '—' }}</span><span>PL {{ matingData.cow.productiveLife ?? '—' }}</span><span>Type {{ matingData.cow.typeScore ?? '—' }}</span><span>UDC {{ matingData.cow.udderComposite ?? '—' }}</span><span>FLC {{ matingData.cow.feetLegsComposite ?? '—' }}</span></div>
          <details v-for="sire in matingData.suggestions" :key="sire.sireReferenceId" class="sire-suggestion">
            <summary><strong>{{ sire.name }}</strong><span>{{ sire.naabCode || 'No NAAB code' }} · match {{ Number(sire.score).toFixed(1) }}</span></summary>
            <div class="cow-proof-row"><span>NM$ {{ sire.netMerit ?? '—' }}</span><span>Milk {{ sire.ptaMilk ?? '—' }}</span><span>DPR {{ sire.daughterPregnancyRate ?? '—' }}</span><span>PL {{ sire.productiveLife ?? '—' }}</span><span>Type {{ sire.ptaType ?? '—' }}</span><span>UDC {{ sire.udderComposite ?? '—' }}</span><span>FLC {{ sire.feetLegsComposite ?? '—' }}</span></div>
            <p>{{ sire.reasons.join(' · ') || 'Balanced candidate; review full proof.' }}</p>
          </details>
          <details v-if="matingData.avoid?.length" class="avoid-sires"><summary>Sires to avoid for this specific cow ({{ matingData.avoid.length }})</summary><p class="upload-hint">These bulls are not universally bad. They are flagged because their current proof may fail to improve this cow's particular weaknesses.</p><article v-for="sire in matingData.avoid" :key="`avoid-${sire.sireReferenceId}`"><strong>{{ sire.name }}</strong><span>{{ sire.naabCode || 'No NAAB code' }}</span><p>{{ sire.concerns.join(' · ') }}</p></article></details>
        </div>
      </section>

      <section class="panel">
        <h2>Unified Timeline</h2>

        <div
          v-if="timelineEntries.length === 0"
          class="timeline-card"
        >
          <strong>No timeline activity yet</strong>

          <small>
            Use the quick actions above to begin tracking the animal.
          </small>
        </div>

        <div
          v-for="entry in timelineEntries"
          :key="`${entry.eventType}-${entry.eventId}`"
          class="timeline-card"
        >
          <strong>
            {{ entry.title }}
          </strong>

          <small>
            {{ new Date(entry.eventDate).toLocaleString() }}
          </small>

          <p>
            {{ entry.summary }}
          </p>

          <p v-if="entry.notes">
            {{ entry.notes }}
          </p>

          <img
            v-if="entry.photoUrl"
            :src="entry.photoUrl"
            class="timeline-photo"
            alt="Timeline photo"
          >
        </div>
      </section>

      <section class="panel">
        <h2>Show Achievements</h2>

        <div
          v-if="showAchievements.length === 0"
          class="timeline-card"
        >
          <strong>No show achievements recorded</strong>

          <small>
            Save a bagging record in Reports to see it here.
          </small>
        </div>

        <div
          v-for="achievement in showAchievements"
          :key="achievement.showAchievementId"
          class="timeline-card"
        >
          <strong>
            🏆
            {{ achievement.showName || 'Show Achievement' }}
          </strong>

          <small>
            {{ achievement.showDate ? new Date(achievement.showDate).toLocaleDateString() : '—' }}
          </small>

          <p>
            Bagged: {{ achievement.bagged || '—' }}
          </p>

          <p>
            Result: {{ achievement.placed || '—' }}
          </p>

          <p v-if="achievement.notes">
            {{ achievement.notes }}
          </p>
        </div>
      </section>

      <section class="panel">
        <h2>Calving History</h2>

        <div
          v-if="calvingEvents.length === 0"
          class="timeline-card"
        >
          <strong>No calvings recorded</strong>

          <small>
            Use Calved above to record one.
          </small>
        </div>

        <div
          v-for="calving in calvingEvents"
          :key="calving.calvingEventId"
          class="timeline-card"
        >
          <div class="timeline-actions">
            <button class="mini-btn" @click="editCalving(calving)">
              Edit
            </button>
            <button class="mini-btn danger" @click="deleteCalving(calving)">
              Delete
            </button>
          </div>

          <strong>
            🐄 Calved · {{ calfSexLabel(calving.calfSex) }}
          </strong>

          <small>
            {{
              new Date(
                calving.calvingDate
              ).toLocaleString()
            }}
          </small>

          <p>
            Ease:
            {{ calvingEaseLabel(calving.calvingEase) }}
          </p>

          <p v-if="calving.calfBarnName">
            Calf:
            {{ calving.calfBarnName }}
          </p>

          <p v-if="calving.calfRegisteredName">
            Registered name:
            {{ calving.calfRegisteredName }}
          </p>

          <p v-if="calving.twins">
            Twins
          </p>

          <p v-if="calving.stillborn">
            Stillborn
          </p>

          <p v-if="calving.notes">
            {{ calving.notes }}
          </p>

          <img
            v-if="calving.pictureUrl"
            :src="calving.pictureUrl"
            class="timeline-photo"
            alt="Calving event photo"
          >
        </div>
      </section>

      <section class="panel">
        <h2>Dry Off History</h2>

        <div
          v-if="dryOffEvents.length === 0"
          class="timeline-card"
        >
          <strong>No dry offs recorded</strong>

          <small>
            Use Dry Off above to record one.
          </small>
        </div>

        <div
          v-for="dry in dryOffEvents"
          :key="dry.dryOffEventId"
          class="timeline-card"
        >
          <strong>
            🌾 Dry Off
          </strong>

          <small>
            {{
              new Date(
                dry.dryOffDate
              ).toLocaleString()
            }}
          </small>

          <p v-if="dry.reason">
            Reason: {{ dry.reason }}
          </p>

          <p v-if="dry.notes">
            {{ dry.notes }}
          </p>
        </div>
      </section>

      <section class="panel">
        <h2>Breeding History</h2>

        <div
          v-if="breedingEvents.length === 0"
          class="timeline-card"
        >
          <strong>No breedings recorded</strong>

          <small>
            Use Breed above to add one.
          </small>
        </div>

        <div
          v-for="breeding in breedingEvents"
          :key="breeding.breedingEventId"
          class="timeline-card"
        >
          <div class="timeline-actions">
            <button class="mini-btn" @click="editBreeding(breeding)">
              Edit
            </button>
            <button class="mini-btn danger" @click="deleteBreeding(breeding)">
              Delete
            </button>
          </div>

          <strong>
            🧬 Bred to {{ breeding.sireUsed }}
          </strong>

          <small>
            {{
              new Date(
                breeding.breedingDate
              ).toLocaleString()
            }}
            ·
            {{ breedingTypeLabel(breeding.breedingType) }}
            ·
            {{
              pregnancyStatusLabel(
                breeding.pregnancyStatus
              )
            }}
          </small>

          <p>
            Preg check:
            {{
              new Date(
                breeding.pregnancyCheckDueDate
              ).toLocaleDateString()
            }}
          </p>

          <p>
            Due:
            {{
              new Date(
                breeding.expectedDueDate
              ).toLocaleDateString()
            }}
          </p>

          <p v-if="breeding.notes">
            {{ breeding.notes }}
          </p>
        </div>
      </section>

      <section class="panel">
        <h2>LUT History</h2>

        <div
          v-if="lutEvents.length === 0"
          class="timeline-card"
        >
          <strong>No LUT events recorded</strong>

          <small>
            Use LUT injection from dashboard quick actions to add one.
          </small>
        </div>

        <div
          v-for="lut in lutEvents"
          :key="lut.lutalyseEventId"
          class="timeline-card"
        >
          <div class="timeline-actions">
            <button class="mini-btn" @click="editLut(lut)">
              Edit
            </button>
            <button class="mini-btn danger" @click="deleteLut(lut)">
              Delete
            </button>
          </div>

          <strong>
            💉 LUT Injection
          </strong>

          <small>
            {{ new Date(lut.administrationDate).toLocaleString() }}
          </small>

          <p>
            Watch window:
            {{ new Date(lut.expectedHeatWatchStart).toLocaleDateString() }}
            -
            {{ new Date(lut.expectedHeatWatchEnd).toLocaleDateString() }}
          </p>

          <p v-if="lut.notes">
            {{ lut.notes }}
          </p>
        </div>
      </section>

      <section class="panel">
        <h2>Heat History</h2>

        <div
          v-if="heatEvents.length === 0"
          class="timeline-card"
        >
          <strong>No heats recorded</strong>

          <small>
            Use Record Heat above to add one.
          </small>
        </div>

        <div
          v-for="heat in heatEvents"
          :key="heat.heatEventId"
          class="timeline-card"
        >
          <div class="timeline-actions">
            <button class="mini-btn" @click="editHeat(heat)">
              Edit
            </button>
            <button class="mini-btn danger" @click="deleteHeat(heat)">
              Delete
            </button>
          </div>

          <strong>
            ❤️ Heat
          </strong>

          <small>
            {{
              new Date(
                heat.heatDateTime
              ).toLocaleString()
            }}
          </small>

          <p>
            {{ heat.notes || 'No notes' }}
          </p>

          <img
            v-if="heat.pictureUrl"
            :src="heat.pictureUrl"
            class="timeline-photo"
            alt="Heat event photo"
          >
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.page {
  position: relative;
  isolation: isolate;
  max-width: 900px;
  margin: auto;
  padding: 24px;
}

.page::before {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: 24px;
  background-image:
    linear-gradient(160deg, rgba(246, 251, 247, 0.94), rgba(255, 255, 255, 0.94)),
    url('/candid.jpg');
  background-size: cover;
  background-position: center;
  opacity: 0.24;
  pointer-events: none;
  z-index: -1;
}

.back {
  margin-bottom: 20px;
  padding: 12px 16px;
  border: 1px solid #31572c;
  border-radius: 4px;
  background: none;
  color: #31572c;
  font-size: 1.1rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.back:hover {
  background: rgba(49, 87, 44, 0.05);
  border-color: #254520;
  color: #254520;
}

.hero {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 25px;
}

.avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 90px;
  height: 90px;
  border-radius: 8px;
  border: 1px solid #c4d3c4;
  background: linear-gradient(160deg, #f4f8f4, #e7efe7);
  font-size: 42px;
}

.eyebrow {
  margin: 0 0 4px;
  color: #31572c;
  font-weight: 800;
}

.hero h1 {
  margin: 0;
  font-size: 40px;
}

.hero p {
  margin: 6px 0;
  color: #64748b;
}

.edit-animal-button{margin-left:auto;min-height:44px;border:0;border-radius:8px;background:#31572c;color:#fff;padding:9px 16px;font-weight:900;cursor:pointer}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 15px;
  margin: 28px 0;
}

.info-card,
.panel {
  padding: 20px;
  border-radius: 8px;
  border: 1px solid #d7dee8;
  background: linear-gradient(180deg, #ffffff, #f8fafc);
  box-shadow: 0 8px 22px rgba(2, 6, 23, 0.07);
}

.info-card span,
.pedigree span {
  display: block;
  margin-bottom: 6px;
  color: #64748b;
  font-size: 14px;
}

.info-card strong,
.pedigree strong {
  font-size: 20px;
}

.panel {
  margin-bottom: 22px;
}

.panel h2,
.form-card h3 {
  margin-top: 0;
}
.form-error{margin:6px 0;padding:9px 10px;border:1px solid #fecaca;border-radius:7px;background:#fff1f2;color:#991b1b;font-weight:750}.form-actions .save:disabled{opacity:.55;cursor:not-allowed}

.pedigree {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px;
}

.actions {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14px;
}

.actions button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 18px;
  border: 1px solid #111827;
  border-radius: 8px;
  background: linear-gradient(170deg, #232f41, #111827);
  color: white;
  font-size: 16px;
  font-weight: 700;
  letter-spacing: 0.02em;
  cursor: pointer;
}
.actions .sold-action{border-color:#b45309;background:#fff7ed;color:#7c2d12}.sold-icon{display:grid;place-items:center;width:30px;height:30px;border-radius:50%;background:#b45309;color:#fff;font-size:1.1rem;font-weight:950}.sold-form{border:2px solid #b45309;background:#fffaf2}.sold-confirm{background:#b45309!important}.sold-banner{display:flex;justify-content:space-between;align-items:center;gap:12px;margin:12px 0;padding:13px;border:2px solid #b45309;border-radius:9px;background:#fff7ed;color:#7c2d12}.sold-banner>div{display:grid;gap:3px}.sold-banner p{margin:3px 0 0}.sold-banner button{min-height:42px;border:0;border-radius:7px;background:#7c2d12;color:#fff;padding:7px 12px;font-weight:850}

.form-card {
  margin-top: 18px;
  padding: 18px;
  border: 1px solid #ced7e3;
  border-radius: 8px;
  background: linear-gradient(180deg, #f8fafc, #f3f7fb);
}

.form-card label {
  display: block;
  margin: 12px 0 8px;
  font-weight: 700;
}

.form-card textarea,
.form-card input,
.form-card select {
  box-sizing: border-box;
  width: 100%;
  min-height: 44px;
  padding: 12px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  background: #ffffff;
  font-size: 15px;
}

.form-card textarea {
  min-height: 90px;
}

.checkbox-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
  margin-top: 16px;
}

.checkbox-row {
  display: flex !important;
  align-items: center;
  gap: 9px;
  margin: 0 !important;
  padding: 12px;
  border: 1px solid #dbe2df;
  border-radius: 6px;
  background: white;
}

.checkbox-row input {
  width: auto;
  min-height: auto;
}

.form-actions {
  display: flex;
  gap: 10px;
  margin-top: 16px;
}

.form-actions button {
  padding: 12px 16px;
  border: none;
  border-radius: 6px;
  font-weight: 700;
  cursor: pointer;
}

.save {
  background: #31572c;
  color: white;
}

.cancel {
  background: #e5e7eb;
  color: #111827;
}

.timeline-card {
  position: relative;
  margin-bottom: 12px;
  padding: 18px;
  border: 1px solid #d7dde8;
  border-left: 4px solid #31572c;
  border-radius: 8px;
  background: linear-gradient(180deg, #f8fafc, #f1f5f9);
}
.private-data-heading{display:flex;align-items:center;justify-content:space-between;gap:10px}.private-data-heading h2{margin:0}.data-history-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:10px}.data-history-card{display:grid;gap:7px;padding:13px;border:1px solid #d9e3dc;border-left:4px solid #31572c;border-radius:8px;background:#f8fbf8}.data-history-card>div{display:flex;flex-wrap:wrap;gap:7px}.data-history-card span{padding:4px 7px;border-radius:5px;background:#fff;font-size:.82rem;font-weight:700}.data-history-card small{color:#64748b}
.genomic-highlight-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(120px,1fr));gap:8px;margin-top:14px}.genomic-highlight-card{display:grid;gap:3px;padding:10px 11px;border:1px solid #d9e3dc;border-radius:8px;background:#fff}.genomic-highlight-card small{font-size:.68rem;font-weight:900;letter-spacing:.08em;color:#31572c}.genomic-highlight-card strong{font-size:1.15rem;color:#173422}.genomic-highlight-card span{font-size:.76rem;color:#64748b}
.animal-linear{margin-top:14px;padding:13px;border:1px solid #d9e3dc;border-radius:10px;background:#fbfdfb}.animal-linear h3{margin:0}.animal-linear-row{display:grid;grid-template-columns:100px 1fr 48px;gap:8px;align-items:center;margin-top:9px;font-size:.82rem}.animal-linear-row>div{height:12px;background:#e4ebe5;border-radius:10px;overflow:hidden}.animal-linear-row i{display:block;height:100%;border-radius:10px;background:#4f772d}.animal-linear-row strong{text-align:right}
.imported-full-record{margin-top:5px;border-top:1px solid #d9e3dc;padding-top:7px}.imported-full-record summary{cursor:pointer;font-weight:850;color:#31572c}.imported-field-grid{display:grid!important;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px!important;margin-top:8px}.imported-field-grid span{display:grid!important}.imported-field-grid small{font-size:.68rem;text-transform:uppercase;color:#64748b}.imported-full-record pre{max-height:360px;margin:8px 0 0;padding:10px;overflow:auto;white-space:pre-wrap;background:#f4f7f4;border-radius:7px;font:12px/1.45 monospace}
.mating-review{margin-top:18px;padding-top:14px;border-top:1px solid #d9e3dc}.cow-proof-row{display:flex;flex-wrap:wrap;gap:7px;margin:8px 0}.cow-proof-row span{padding:5px 8px;border-radius:6px;background:#eef5ef;font-size:.8rem;font-weight:800}.sire-suggestion{margin:8px 0;border:1px solid #d8e2da;border-radius:8px;background:#fff}.sire-suggestion summary{display:flex;justify-content:space-between;gap:10px;padding:11px;cursor:pointer}.sire-suggestion summary span{color:#64748b;font-size:.8rem}.sire-suggestion>div,.sire-suggestion>p{margin:9px 11px}
.avoid-sires{margin-top:12px;border:1px solid #f0b3b3;border-radius:8px;background:#fff8f8}.avoid-sires>summary{padding:11px;color:#991b1b;font-weight:900;cursor:pointer}.avoid-sires>p,.avoid-sires>article{margin:8px 11px}.avoid-sires article{padding:9px;border-top:1px solid #f5cccc}.avoid-sires article span{margin-left:8px;color:#64748b}.avoid-sires article p{margin:5px 0 0;color:#991b1b;font-size:.82rem}

.timeline-actions {
  position: absolute;
  top: 10px;
  right: 10px;
  display: flex;
  gap: 8px;
}

.mini-btn {
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  background: #ffffff;
  color: #1f2937;
  font-size: 0.8rem;
  font-weight: 700;
  padding: 6px 10px;
  cursor: pointer;
}

.mini-btn:hover {
  background: #f1f5f9;
}

.mini-btn.danger {
  border-color: #ef4444;
  color: #b91c1c;
}

.mini-btn.danger:hover {
  background: #fef2f2;
}

.timeline-card strong {
  display: block;
  margin-bottom: 8px;
}

.timeline-card small {
  color: #64748b;
}

.timeline-card p {
  margin-bottom: 0;
}

.timeline-photo {
  display: block;
  max-width: 100%;
  margin-top: 12px;
  border-radius: 6px;
}

.checkbox-label {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  margin-top: 16px !important;
  margin-bottom: 0 !important;
  padding: 12px;
  border: 1px solid #dbe2df;
  border-radius: 6px;
  background: white;
  cursor: pointer;
  font-weight: 600;
  font-size: 15px;
}

.checkbox-label input {
  width: auto;
  min-height: auto;
  margin: 0;
  cursor: pointer;
}

.checkbox-label span {
  flex: 1;
  line-height: 1.35;
  overflow-wrap: anywhere;
}

.day7-label {
  display: inline-block;
  line-height: 1.2;
}

.upload-hint {
  display: block;
  margin-top: 8px;
  color: #475569;
  font-size: 0.9rem;
}

.latest-milk-strip{display:grid;grid-template-columns:auto repeat(6,minmax(80px,1fr)) auto;gap:7px;align-items:stretch;margin:12px 0;padding:10px;border-left:6px solid #2f80ed;background:#eef6ff;overflow-x:auto}
.latest-milk-strip article,.milk-strip-title{display:grid;align-content:center;min-width:82px;padding:7px;background:#fff}
.milk-strip-title{background:#123b68;color:#fff}
.milk-strip-title span{font-size:.66rem;font-weight:950;letter-spacing:.08em}
.latest-milk-strip small{color:#64748b;font-size:.7rem}
.latest-milk-strip strong{font-size:1rem}
.latest-milk-strip .down{color:#b91c1c}
.latest-milk-strip>button{min-width:120px;border:0;border-radius:6px;background:#1769c2;color:#fff;font-weight:850}

@media (max-width: 700px) {
  .hero{align-items:flex-start;flex-wrap:wrap}
  .edit-animal-button{width:100%;margin-left:0}
  .latest-milk-strip{grid-template-columns:repeat(2,minmax(0,1fr));overflow:visible}
  .milk-strip-title,.latest-milk-strip>button{grid-column:1/-1}

  .info-grid,
  .pedigree,
  .actions,
  .checkbox-grid {
    grid-template-columns: 1fr;
  }

  .checkbox-label {
    font-size: 14px;
    padding: 10px;
  }

  .hero h1 {
    font-size: 32px;
  }
}
</style>
