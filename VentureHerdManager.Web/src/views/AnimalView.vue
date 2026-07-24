<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { getAnimal } from '../api/animals'
import { getAnimalSnapshot } from '../api/animalsSnapshot'
import type { Animal } from '../models/Animal'
import type { AnimalSnapshot, AnimalTimelineEntry } from '../models/AnimalSnapshot'

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
  type CalvingEvent
} from '../api/calvings'

const route = useRoute()
const router = useRouter()

const animal = ref<Animal | null>(null)
const snapshot = ref<AnimalSnapshot | null>(null)

const heatEvents = ref<HeatEvent[]>([])
const breedingEvents = ref<BreedingEvent[]>([])
const calvingEvents = ref<CalvingEvent[]>([])
const dryOffEvents = ref<DryOffEvent[]>([])
const animalNotes = ref<AnimalNote[]>([])
const timelineEntries = ref<AnimalTimelineEntry[]>([])

const loading = ref(true)

const showHeatForm = ref(false)
const heatNotes = ref('')
const heatPictureUrl = ref('')
const hasEmbryoTransfer = ref(false)

const showBreedingForm = ref(false)
const sireUsed = ref('')
const breedingType = ref(0)
const breedingNotes = ref('')

const showPregCheckForm = ref(false)
const selectedBreedingId = ref<number | null>(null)
const pregnancyStatus = ref(1)

const showCalvingForm = ref(false)
const calfSex = ref(0)
const calfBarnName = ref('')
const calfRegisteredName = ref('')
const calvingPictureUrl = ref('')
const calvingEase = ref(0)
const twins = ref(false)
const stillborn = ref(false)
const calvingNotes = ref('')

const showDryOffForm = ref(false)
const dryReason = ref('')
const dryNotes = ref('')

const showNoteForm = ref(false)
const noteText = ref('')

const animalId = computed(() => Number(route.params.animalId))

function goBack() {
  router.push('/')
}

function closeAllForms() {
  showHeatForm.value = false
  showBreedingForm.value = false
  showPregCheckForm.value = false
  showCalvingForm.value = false
  showDryOffForm.value = false
  showNoteForm.value = false
  hasEmbryoTransfer.value = false
}

function openHeatForm() {
  closeAllForms()
  showHeatForm.value = true
}

function openBreedingForm() {
  closeAllForms()
  showBreedingForm.value = true
}

function openPregCheckForm() {
  closeAllForms()
  showPregCheckForm.value = true

  if (
    breedingEvents.value.length > 0 &&
    selectedBreedingId.value === null
  ) {
    selectedBreedingId.value =
      breedingEvents.value[0]!.breedingEventId
  }
}

function openCalvingForm() {
  closeAllForms()
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

function openPendingAction() {
  const pendingAction =
    sessionStorage.getItem('pendingAnimalAction')

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

  sessionStorage.removeItem('pendingAnimalAction')
}

onMounted(async () => {
  try {
    const animalSnapshot = await getAnimalSnapshot(
      animalId.value
    )

    snapshot.value = animalSnapshot
    animal.value = animalSnapshot.animal
    timelineEntries.value = animalSnapshot.timeline

    heatEvents.value = await getHeatEvents(
      animalId.value
    )

    breedingEvents.value = await getBreedings(
      animalId.value
    )

    calvingEvents.value = await getCalvings(
      animalId.value
    )

    dryOffEvents.value = await getDryOffEvents(
      animalId.value
    )

    animalNotes.value = await getAnimalNotes(
      animalId.value
    )

    openPendingAction()
  } catch (error) {
    console.error('Failed to load animal:', error)
  } finally {
    loading.value = false
  }
})

async function saveHeat() {
  if (!animal.value) return

  try {
    await recordHeat(
      animal.value.animalId,
      heatNotes.value,
      heatPictureUrl.value.trim() || null,
      hasEmbryoTransfer.value
    )

    heatNotes.value = ''
    heatPictureUrl.value = ''
    hasEmbryoTransfer.value = false
    showHeatForm.value = false

    heatEvents.value = await getHeatEvents(
      animal.value.animalId
    )
  } catch (error) {
    console.error('Failed to save heat:', error)
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
  if (selectedBreedingId.value === null) return

  try {
    await updatePregnancyStatus(
      selectedBreedingId.value,
      pregnancyStatus.value
    )

    showPregCheckForm.value = false

    breedingEvents.value = await getBreedings(
      animalId.value
    )
  } catch (error) {
    console.error(
      'Failed to save pregnancy check:',
      error
    )
  }
}

async function saveCalving() {
  if (!animal.value) return

  try {
    await recordCalving(
      animal.value.animalId,
      calfSex.value,
      calfBarnName.value.trim(),
      calfRegisteredName.value.trim(),
      calvingEase.value,
      twins.value,
      stillborn.value,
      calvingNotes.value,
      calvingPictureUrl.value.trim() || null
    )

    calfSex.value = 0
    calfBarnName.value = ''
    calfRegisteredName.value = ''
    calvingPictureUrl.value = ''
    calvingEase.value = 0
    twins.value = false
    stillborn.value = false
    calvingNotes.value = ''
    showCalvingForm.value = false

    calvingEvents.value = await getCalvings(
      animal.value.animalId
    )

    animal.value = await getAnimal(
      animal.value.animalId
    )
  } catch (error) {
    console.error('Failed to save calving:', error)
  }
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
</script>

<template>
  <div class="page">
    <button class="back" @click="goBack">
      ← Herd
    </button>

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
            ❤️ Record Heat
          </button>

          <button @click="openBreedingForm">
            🧬 Breed
          </button>

          <button @click="openPregCheckForm">
            🤰 Preg Check
          </button>

          <button @click="openCalvingForm">
            🐄 Calved
          </button>

          <button @click="openDryOffForm">
            🌾 Dry Off
          </button>

          <button @click="openNoteForm">
            📝 Notes
          </button>
        </div>

        <div
          v-if="showHeatForm"
          class="form-card"
        >
          <h3>Record Heat</h3>

          <label>Heat Notes</label>

          <textarea
            v-model="heatNotes"
            placeholder="Standing heat, activity, mucus, etc."
          />

          <label>Photo URL</label>

          <input
            v-model="heatPictureUrl"
            placeholder="Optional image URL"
          >

          <label class="checkbox-label">
            <input
              v-model="hasEmbryoTransfer"
              type="checkbox"
            >
            <span>Plan embryo transfer on day 7</span>
          </label>

          <div class="form-actions">
            <button
              class="save"
              @click="saveHeat"
            >
              Save Heat
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
              @click="savePregCheck"
            >
              Save Preg Check
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

          <label>Photo URL</label>

          <input
            v-model="calvingPictureUrl"
            placeholder="Optional image URL"
          >

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
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.page {
  max-width: 900px;
  margin: auto;
  padding: 24px;
}

.back {
  margin-bottom: 20px;
  padding: 12px 16px;
  border: 1px solid #31572c;
  border-radius: 6px;
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
  border-radius: 24px;
  background: #eef4ef;
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

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 15px;
  margin: 28px 0;
}

.info-card,
.panel {
  padding: 20px;
  border-radius: 18px;
  background: white;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.08);
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
  padding: 18px;
  border: none;
  border-radius: 18px;
  background: #1f2937;
  color: white;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
}

.form-card {
  margin-top: 18px;
  padding: 18px;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  background: #f8fafc;
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
  border-radius: 14px;
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
  border-radius: 14px;
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
  border-radius: 12px;
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
  margin-bottom: 12px;
  padding: 18px;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  background: #f8fafc;
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
  border-radius: 12px;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 16px !important;
  margin-bottom: 0 !important;
  padding: 12px;
  border: 1px solid #dbe2df;
  border-radius: 12px;
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
}

@media (max-width: 700px) {
  .info-grid,
  .pedigree,
  .actions,
  .checkbox-grid {
    grid-template-columns: 1fr;
  }

  .hero h1 {
    font-size: 32px;
  }
}
</style>