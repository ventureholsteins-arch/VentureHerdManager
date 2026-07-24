<script setup lang="ts">
import { nextTick, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import {
  getDashboardSummary,
  type DashboardSummary
} from '../api/dashboard'

const router = useRouter()

const dashboard = ref<DashboardSummary | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const pregChecksSectionRef = ref<HTMLElement | null>(null)
const dueSoonSectionRef = ref<HTMLElement | null>(null)
const lutTrackingSectionRef = ref<HTMLElement | null>(null)
const embryoSectionRef = ref<HTMLElement | null>(null)

type ReportSection =
  | 'pregChecks'
  | 'dueSoon'
  | 'lutTracking'
  | 'embryo'

onMounted(async () => {
  try {
    dashboard.value = await getDashboardSummary()
  } catch (error) {
    console.error('Failed to load dashboard:', error)
    errorMessage.value = 'Dashboard could not be loaded.'
  } finally {
    loading.value = false
  }
})

function openAnimal(animalId: number) {
  router.push(`/animals/${animalId}`)
}

function formatDate(value?: string | null) {
  if (!value) {
    return '—'
  }

  const parsed = new Date(value)

  return Number.isNaN(parsed.getTime())
    ? '—'
    : parsed.toLocaleDateString()
}

function formatDateTime(value?: string | null) {
  if (!value) {
    return '—'
  }

  const parsed = new Date(value)

  return Number.isNaN(parsed.getTime())
    ? '—'
    : parsed.toLocaleString()
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

async function openReportSection(section: ReportSection) {
  await nextTick()

  const sectionRefMap: Record<ReportSection, HTMLElement | null> = {
    pregChecks: pregChecksSectionRef.value,
    dueSoon: dueSoonSectionRef.value,
    lutTracking: lutTrackingSectionRef.value,
    embryo: embryoSectionRef.value
  }

  const target = sectionRefMap[section]
  if (!target) return

  target.scrollIntoView({ behavior: 'smooth', block: 'start' })
}
</script>

<template>
  <section class="dashboard-summary">
    <p v-if="loading" class="message">
      Loading dashboard...
    </p>

    <p v-else-if="errorMessage" class="error-message">
      {{ errorMessage }}
    </p>

    <template v-else-if="dashboard">
      <div class="section-heading">
        <div>
          <p class="eyebrow">HERD OVERVIEW</p>
          <h2>Today at a glance</h2>
        </div>
      </div>

      <div class="herd-metrics-row">
        <div class="metric-mini">
          <small>Avg Score</small>
          <span class="metric-value">{{ dashboard.herdScoreAverage ? dashboard.herdScoreAverage.toFixed(1) : '—' }}</span>
        </div>
        <div class="metric-mini">
          <small>EX (2nd+ Lac)</small>
          <span class="metric-value">{{ dashboard.percentExcellent2ndLactationOrHigher ? dashboard.percentExcellent2ndLactationOrHigher.toFixed(0) : '—' }}%</span>
        </div>
        <div class="metric-mini">
          <small>Avg BAA</small>
          <span class="metric-value">{{ dashboard.herdBaaAverage ? dashboard.herdBaaAverage.toFixed(1) : '—' }}</span>
        </div>
      </div>

      <div class="summary-grid">
        <button
          type="button"
          class="summary-card summary-card-button"
          @click="openReportSection('pregChecks')"
        >
          <span class="icon">🤰</span>

          <div>
            <strong>{{ dashboard.pregChecksDueCount }}</strong>
            <small>Preg checks due</small>
          </div>
        </button>

        <button
          type="button"
          class="summary-card summary-card-button"
          @click="openReportSection('dueSoon')"
        >
          <span class="icon">📅</span>

          <div>
            <strong>{{ dashboard.dueSoonCount }}</strong>
            <small>Due within 30 days</small>
          </div>
        </button>

        <button
          type="button"
          class="summary-card summary-card-button"
          @click="openReportSection('lutTracking')"
        >
          <span class="icon">💉</span>

          <div>
            <strong>{{ dashboard.lutTrackingCount }}</strong>
            <small>LUT tracking</small>
          </div>
        </button>

        <button
          type="button"
          class="summary-card summary-card-button important"
          @click="openReportSection('embryo')"
        >
          <span class="icon">🧬</span>

          <div>
            <strong>{{ dashboard.embryoImplantsCount }}</strong>
            <small>Embryo implants</small>
          </div>
        </button>
      </div>

      <section
        v-if="(dashboard.pregChecksDue?.length ?? 0) > 0"
        class="dashboard-panel"
        ref="pregChecksSectionRef"
      >
        <div class="panel-heading">
          <div>
            <p class="eyebrow">NEEDS ATTENTION</p>
            <h3>Preg checks due</h3>
          </div>

          <span class="count-badge">
            {{ dashboard.pregChecksDueCount }}
          </span>
        </div>

        <button
          v-for="item in dashboard.pregChecksDue"
          :key="item.breedingEventId"
          class="event-row"
          @click="openAnimal(item.animalId)"
        >
          <span class="event-icon">🤰</span>

          <div class="event-content">
            <strong>{{ item.animalName }}</strong>

            <small>
              Bred to {{ item.sireUsed }}
            </small>

            <p>
              Due for check:
              {{ formatDate(item.pregnancyCheckDueDate) }}
            </p>
          </div>

          <span class="arrow">›</span>
        </button>
      </section>

      <section
        v-if="(dashboard.dueSoon?.length ?? 0) > 0"
        class="dashboard-panel"
        ref="dueSoonSectionRef"
      >
        <div class="panel-heading">
          <div>
            <p class="eyebrow">UPCOMING</p>
            <h3>Due within 30 days</h3>
          </div>

          <span class="count-badge">
            {{ dashboard.dueSoonCount }}
          </span>
        </div>

        <button
          v-for="item in dashboard.dueSoon"
          :key="item.breedingEventId"
          class="event-row"
          @click="openAnimal(item.animalId)"
        >
          <span class="event-icon">📅</span>

          <div class="event-content">
            <strong>{{ item.animalName }}</strong>

            <small>
              Bred to {{ item.sireUsed }}
            </small>

            <p>
              Due {{ formatDate(item.expectedDueDate) }}
              · {{ item.daysUntilDue }} days
            </p>
          </div>

          <span class="arrow">›</span>
        </button>
      </section>

      <section
        v-if="(dashboard.lutTracking?.length ?? 0) > 0"
        class="dashboard-panel"
        ref="lutTrackingSectionRef"
      >
        <div class="panel-heading">
          <div>
            <p class="eyebrow">NEEDS ATTENTION</p>
            <h3>LUT tracking</h3>
          </div>

          <span class="count-badge">
            {{ dashboard.lutTrackingCount }}
          </span>
        </div>

        <button
          v-for="item in dashboard.lutTracking"
          :key="item.lutalyseEventId"
          class="event-row"
          @click="openAnimal(item.animalId)"
        >
          <span class="event-icon">💉</span>

          <div class="event-content">
            <strong>{{ item.animalName }}</strong>

            <small>
              LUT on {{ formatDate(item.administrationDate) }}
            </small>

            <p>
              Heat watch through {{ formatDate(item.expectedHeatWatchEnd) }}
              · {{ item.daysRemaining }} days remaining
            </p>
          </div>

          <span class="arrow">›</span>
        </button>
      </section>

      <section
        v-if="(dashboard.embryoImplants?.length ?? 0) > 0"
        class="dashboard-panel"
        ref="embryoSectionRef"
      >
        <div class="panel-heading">
          <div>
            <p class="eyebrow">NEEDS ATTENTION</p>
            <h3>Embryo implants</h3>
          </div>

          <span class="count-badge">
            {{ dashboard.embryoImplantsCount }}
          </span>
        </div>

        <button
          v-for="item in dashboard.embryoImplants"
          :key="item.heatEventId"
          class="event-row"
          @click="openAnimal(item.animalId)"
        >
          <span class="event-icon">🧬</span>

          <div class="event-content">
            <strong>{{ item.animalName }}</strong>

            <small>
              Heat on {{ formatDate(item.heatDateTime) }}
            </small>

            <p>
              Implant day {{ 7 - item.daysTracked }} of 7
              · {{ item.daysUntilImplant }} days to implant
            </p>
          </div>

          <span class="arrow">›</span>
        </button>
      </section>

      <section
        v-if="(dashboard.recentHeats?.length ?? 0) > 0"
        class="dashboard-panel"
      >
        <div class="panel-heading">
          <div>
            <p class="eyebrow">RECENT ACTIVITY</p>
            <h3>Recent heats</h3>
          </div>
        </div>

        <button
          v-for="heat in dashboard.recentHeats"
          :key="heat.heatEventId"
          class="event-row"
          @click="openAnimal(heat.animalId)"
        >
          <span class="event-icon">❤️</span>

          <div class="event-content">
            <strong>{{ heat.animalName }}</strong>

            <small>
              {{ formatDateTime(heat.heatDateTime) }}
            </small>

            <p>{{ heat.notes || 'No notes' }}</p>
          </div>

          <span class="arrow">›</span>
        </button>
      </section>

      <section class="dashboard-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">RECENT ACTIVITY</p>
            <h3>Recent breedings</h3>
          </div>
        </div>

        <div
          v-if="(dashboard.recentBreedings?.length ?? 0) === 0"
          class="empty-state"
        >
          <strong>No breedings recorded</strong>
        </div>

        <button
          v-for="breeding in dashboard.recentBreedings"
          :key="breeding.breedingEventId"
          class="event-row"
          @click="openAnimal(breeding.animalId)"
        >
          <span class="event-icon">🧬</span>

          <div class="event-content">
            <strong>
              {{ breeding.animalName }}
            </strong>

            <small>
              Bred to {{ breeding.sireUsed }}
              · {{ pregnancyStatusLabel(breeding.pregnancyStatus) }}
            </small>

            <p>
              {{ formatDateTime(breeding.breedingDate) }}
            </p>
          </div>

          <span class="arrow">›</span>
        </button>
      </section>
    </template>
  </section>
</template>

<style scoped>
.dashboard-summary {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.message,
.error-message {
  padding: 18px 20px;
  border-radius: 10px;
  background: #ffffff;
  border: 1px solid #d6ddd7;
  color: #1f2d23;
  font-size: 0.95rem;
}

.error-message {
  color: #b42318;
  border-color: #f5c2c2;
  background: #fef7f7;
}

.section-heading {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 4px;
}

.eyebrow {
  margin: 0 0 6px;
  color: #4f7a53;
  font-size: 0.80rem;
  font-weight: 900;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  display: block;
}

.section-heading h2,
.panel-heading h3 {
  margin: 0;
  color: #0f1f16;
  font-size: 1.45rem;
  font-weight: 900;
  font-family: 'Bahnschrift', 'Arial Narrow', 'Segoe UI', sans-serif;
  letter-spacing: -0.015em;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.summary-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 12px;
  border: 1px solid #cfd7d0;
  border-bottom: 3px solid #2f5d38;
  border-radius: 10px;
  background: #ffffff;
  box-shadow: 0 5px 14px rgba(13, 30, 18, 0.08);
  transition: all 0.2s ease;
}

.summary-card-button {
  width: 100%;
  text-align: left;
  cursor: pointer;
}

.summary-card-button:focus-visible {
  outline: 2px solid #31572c;
  outline-offset: 2px;
}

.summary-card:hover {
  border-color: #7aa484;
  box-shadow: 0 10px 22px rgba(13, 30, 18, 0.12);
}

.summary-card.important {
  border-color: #87b590;
  border-bottom-color: #20502c;
  background: #f7fcf8;
  box-shadow: 0 8px 20px rgba(35, 73, 42, 0.11);
}

.icon,
.event-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: 8px;
  border: 1px solid #dbe4dc;
  background: linear-gradient(180deg, #f7faf8, #edf3ee);
  font-size: 1.25rem;
  flex-shrink: 0;
}

.summary-card strong {
  display: block;
  color: #0f1f16;
  font-size: 1.35rem;
  font-weight: 900;
  line-height: 1.1;
}

.summary-card small {
  display: block;
  margin-top: 3px;
  color: #5d6f63;
  font-size: 0.85rem;
  font-weight: 600;
}

.herd-metrics-row {
  display: flex;
  gap: 12px;
  margin-top: 6px;
  padding: 10px 12px;
  background: linear-gradient(180deg, #fbfdfb, #f4f9f5);
  border-radius: 8px;
  border: 1px solid #dbe5dd;
  border-top: 3px solid #2d5a35;
}

.metric-mini {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  flex: 1;
  min-width: 0;
}

.metric-mini small {
  font-size: 0.7rem;
  color: #7a8d7f;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.4px;
  margin-bottom: 4px;
}

.metric-value {
  font-size: 0.95rem;
  font-weight: 800;
  color: #0f1f16;
}

.dashboard-panel {
  padding: 16px;
  border: 1px solid #d2dad3;
  border-left: 4px solid #2f5d38;
  border-radius: 10px;
  background: #ffffff;
  box-shadow: 0 8px 18px rgba(13, 30, 18, 0.07);
}

.panel-heading {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
  padding-bottom: 10px;
  border-bottom: 2px solid #f0f7f1;
}

.panel-heading h3 {
  font-size: 1.3rem;
}

.count-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 38px;
  height: 38px;
  padding: 0 12px;
  border-radius: 8px;
  background: #e0f2e3;
  color: #1a5e35;
  font-weight: 900;
  font-size: 1.05rem;
}

.empty-state {
  padding: 16px 0 8px;
  color: #5d6f63;
}

.empty-state strong {
  display: block;
  margin-bottom: 6px;
  color: #163022;
  font-size: 0.95rem;
}

.event-row {
  display: flex;
  align-items: center;
  gap: 14px;
  width: 100%;
  padding: 10px 0;
  border: 0;
  background: transparent;
  text-align: left;
  cursor: pointer;
  transition: background 0.15s ease;
  border-radius: 6px;
  padding-left: 8px;
  padding-right: 8px;
  margin-left: -8px;
  margin-right: -8px;
}

.event-row:hover {
  background: #f5fbf6;
  transform: translateX(2px);
}

.event-row + .event-row {
  border-top: 1px solid #ede8ed;
  padding-top: 10px;
  margin-top: 0;
}

.event-content {
  flex: 1;
  min-width: 0;
}

.event-content strong {
  display: block;
  color: #0f1f16;
  font-size: 1.05rem;
  font-weight: 700;
}

.event-content small,
.event-content p {
  display: block;
  margin-top: 4px;
  color: #5d6f63;
  font-size: 0.9rem;
}

.event-content p {
  margin-bottom: 0;
}

.arrow {
  color: #3e6e47;
  font-size: 1.3rem;
  font-weight: 800;
  flex-shrink: 0;
}

@media (max-width: 700px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 8px;
  }

  .summary-card {
    padding: 12px 10px;
    flex-direction: column;
    text-align: center;
  }

  .summary-card strong {
    font-size: 1.2rem;
  }

  .summary-card small {
    font-size: 0.8rem;
  }

  .dashboard-panel {
    padding: 14px;
  }

  .panel-heading {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .panel-heading h3 {
    font-size: 1.1rem;
  }
}

@media (max-width: 480px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .dashboard-summary {
    gap: 10px;
  }

  .section-heading h2,
  .panel-heading h3 {
    font-size: 1.2rem;
  }

  .eyebrow {
    font-size: 0.75rem;
  }

  .icon,
  .event-icon {
    width: 40px;
    height: 40px;
    font-size: 1.1rem;
  }

  .event-row {
    padding: 9px 0;
  }
}
</style>