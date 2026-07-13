<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import {
  getDashboardSummary,
  type DashboardSummary
} from '../api/dashboard'

const router = useRouter()

const dashboard = ref<DashboardSummary | null>(null)
const loading = ref(true)
const errorMessage = ref('')

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

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString()
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

      <div class="summary-grid">
        <div class="summary-card">
          <span class="icon">🐄</span>

          <div>
            <strong>{{ dashboard.totalAnimals }}</strong>
            <small>Total animals</small>
          </div>
        </div>

        <div class="summary-card">
          <span class="icon">🥛</span>

          <div>
            <strong>{{ dashboard.milking }}</strong>
            <small>Milking</small>
          </div>
        </div>

        <div class="summary-card">
          <span class="icon">🌾</span>

          <div>
            <strong>{{ dashboard.dry }}</strong>
            <small>Dry cows</small>
          </div>
        </div>

        <div class="summary-card">
          <span class="icon">🐮</span>

          <div>
            <strong>{{ dashboard.heifers }}</strong>
            <small>Heifers</small>
          </div>
        </div>

        <div class="summary-card">
          <span class="icon">🍼</span>

          <div>
            <strong>{{ dashboard.calves }}</strong>
            <small>Calves</small>
          </div>
        </div>

        <div class="summary-card important">
          <span class="icon">🤰</span>

          <div>
            <strong>{{ dashboard.pregChecksDueCount }}</strong>
            <small>Preg checks due</small>
          </div>
        </div>

        <div class="summary-card important">
          <span class="icon">📅</span>

          <div>
            <strong>{{ dashboard.dueSoonCount }}</strong>
            <small>Due within 30 days</small>
          </div>
        </div>
      </div>

      <section class="dashboard-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">NEEDS ATTENTION</p>
            <h3>Preg checks due</h3>
          </div>

          <span class="count-badge">
            {{ dashboard.pregChecksDueCount }}
          </span>
        </div>

        <div
          v-if="dashboard.pregChecksDue.length === 0"
          class="empty-state"
        >
          <strong>No preg checks due</strong>
          <p>Everything is caught up.</p>
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

      <section class="dashboard-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">UPCOMING</p>
            <h3>Due within 30 days</h3>
          </div>

          <span class="count-badge">
            {{ dashboard.dueSoonCount }}
          </span>
        </div>

        <div
          v-if="dashboard.dueSoon.length === 0"
          class="empty-state"
        >
          <strong>No animals due soon</strong>
          <p>There are no confirmed due dates in the next 30 days.</p>
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

      <section class="dashboard-panel">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">RECENT ACTIVITY</p>
            <h3>Recent heats</h3>
          </div>
        </div>

        <div
          v-if="dashboard.recentHeats.length === 0"
          class="empty-state"
        >
          <strong>No heats recorded</strong>
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
          v-if="dashboard.recentBreedings.length === 0"
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
  display: grid;
  gap: 20px;
  margin-bottom: 24px;
}

.section-heading,
.panel-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.section-heading h2,
.panel-heading h3 {
  margin: 0;
  color: #142033;
}

.eyebrow {
  margin: 0 0 4px;
  color: #31572c;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.summary-card {
  display: flex;
  align-items: center;
  gap: 14px;
  min-height: 88px;
  padding: 16px;
  border: 1px solid #e2e8f0;
  border-radius: 20px;
  background: white;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.07);
}

.summary-card.important {
  border-color: #d9e7d6;
  background: #f6faf5;
}

.icon,
.event-icon {
  display: grid;
  flex-shrink: 0;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 15px;
  background: #eef4ef;
  font-size: 23px;
}

.summary-card strong {
  display: block;
  color: #142033;
  font-size: 28px;
  line-height: 1;
}

.summary-card small {
  display: block;
  margin-top: 6px;
  color: #64748b;
  font-size: 14px;
}

.dashboard-panel {
  padding: 18px;
  border: 1px solid #e2e8f0;
  border-radius: 22px;
  background: white;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.07);
}

.panel-heading {
  margin-bottom: 14px;
}

.count-badge {
  min-width: 34px;
  padding: 7px 10px;
  border-radius: 999px;
  background: #31572c;
  color: white;
  text-align: center;
  font-weight: 800;
}

.event-row {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 13px;
  padding: 14px 4px;
  border: none;
  border-top: 1px solid #edf0f2;
  background: transparent;
  color: inherit;
  text-align: left;
  cursor: pointer;
}

.event-row:first-of-type {
  border-top: none;
}

.event-row:hover {
  background: #f8faf8;
}

.event-content {
  flex: 1;
  min-width: 0;
}

.event-content strong {
  display: block;
  color: #142033;
  font-size: 16px;
}

.event-content small {
  display: block;
  margin-top: 3px;
  color: #64748b;
}

.event-content p {
  margin: 5px 0 0;
  color: #475569;
  font-size: 14px;
}

.arrow {
  color: #94a3b8;
  font-size: 28px;
}

.empty-state {
  padding: 18px;
  border-radius: 16px;
  background: #f8fafc;
  color: #64748b;
}

.empty-state strong {
  display: block;
  color: #334155;
}

.empty-state p {
  margin: 5px 0 0;
  font-size: 14px;
}

.message,
.error-message {
  padding: 18px;
  border-radius: 16px;
  background: white;
}

.error-message {
  color: #b42318;
}

@media (min-width: 700px) {
  .summary-grid {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}
</style>