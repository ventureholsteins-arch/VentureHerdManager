<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import { getCalendarEvents } from '../api/calendar'
import type {
  CalendarEvent,
  CalendarEventType
} from '../models/CalendarEvent'

interface CalendarDay {
  key: string
  date: Date
  dayNumber: number
  isCurrentMonth: boolean
  isToday: boolean
  events: CalendarEvent[]
}

const router = useRouter()

const currentMonth = ref(
  new Date(
    new Date().getFullYear(),
    new Date().getMonth(),
    1
  )
)

const events = ref<CalendarEvent[]>([])
const loading = ref(true)
const refreshing = ref(false)
const errorMessage = ref('')
const selectedDateKey = ref<string | null>(null)

const weekDays = [
  'Sun',
  'Mon',
  'Tue',
  'Wed',
  'Thu',
  'Fri',
  'Sat'
]

const eventTypes: Array<{
  type: CalendarEventType
  label: string
  icon: string
}> = [
  {
    type: 'heat',
    label: 'Heat',
    icon: '♥'
  },
  {
    type: 'breeding',
    label: 'Breeding',
    icon: '●'
  },
  {
    type: 'pregnancyCheck',
    label: 'Pregnancy Check',
    icon: '✓'
  },
  {
    type: 'dueDate',
    label: 'Due Date',
    icon: '◆'
  },
  {
    type: 'calving',
    label: 'Calving',
    icon: '★'
  },
  {
    type: 'dryOff',
    label: 'Dry Off',
    icon: '◇'
  }
]

const monthTitle = computed(() => {
  return currentMonth.value.toLocaleDateString('en-US', {
    month: 'long',
    year: 'numeric'
  })
})

const monthStart = computed(() => {
  return new Date(
    currentMonth.value.getFullYear(),
    currentMonth.value.getMonth(),
    1
  )
})

const monthEnd = computed(() => {
  return new Date(
    currentMonth.value.getFullYear(),
    currentMonth.value.getMonth() + 1,
    1
  )
})

const calendarRangeStart = computed(() => {
  const date = new Date(monthStart.value)

  date.setDate(
    date.getDate() - date.getDay()
  )

  return date
})

const calendarRangeEnd = computed(() => {
  const date = new Date(calendarRangeStart.value)

  date.setDate(date.getDate() + 42)

  return date
})

const eventsByDate = computed(() => {
  const grouped = new Map<string, CalendarEvent[]>()

  for (const event of events.value) {
    const key = getDateKey(parseApiDate(event.eventDate))
    const existingEvents = grouped.get(key) ?? []

    existingEvents.push(event)

    existingEvents.sort((first, second) => {
      return parseApiDate(first.eventDate).getTime() -
        parseApiDate(second.eventDate).getTime()
    })

    grouped.set(key, existingEvents)
  }

  return grouped
})

const calendarDays = computed<CalendarDay[]>(() => {
  const days: CalendarDay[] = []
  const todayKey = getDateKey(new Date())

  for (let dayIndex = 0; dayIndex < 42; dayIndex += 1) {
    const date = new Date(calendarRangeStart.value)

    date.setDate(date.getDate() + dayIndex)

    const key = getDateKey(date)

    days.push({
      key,
      date,
      dayNumber: date.getDate(),
      isCurrentMonth:
        date.getMonth() === currentMonth.value.getMonth() &&
        date.getFullYear() === currentMonth.value.getFullYear(),
      isToday: key === todayKey,
      events: eventsByDate.value.get(key) ?? []
    })
  }

  return days
})

const selectedDay = computed(() => {
  if (!selectedDateKey.value) {
    return null
  }

  return calendarDays.value.find(
    day => day.key === selectedDateKey.value
  ) ?? null
})

const selectedDateTitle = computed(() => {
  if (!selectedDay.value) {
    return ''
  }

  return selectedDay.value.date.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  })
})

const monthEventCount = computed(() => {
  return events.value.filter(event => {
    const date = parseApiDate(event.eventDate)

    return date >= monthStart.value && date < monthEnd.value
  }).length
})

function parseApiDate(value: string): Date {
  const dateOnlyMatch = value.match(
    /^(\d{4})-(\d{2})-(\d{2})/
  )

  if (dateOnlyMatch) {
    return new Date(
      Number(dateOnlyMatch[1]),
      Number(dateOnlyMatch[2]) - 1,
      Number(dateOnlyMatch[3])
    )
  }

  return new Date(value)
}

function getDateKey(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

function getEventIcon(eventType: CalendarEventType): string {
  return eventTypes.find(item => item.type === eventType)?.icon ?? '•'
}

function getEventTypeLabel(eventType: CalendarEventType): string {
  return eventTypes.find(item => item.type === eventType)?.label ??
    'Herd Event'
}

function getEventTime(eventDate: string): string | null {
  if (!eventDate.includes('T')) {
    return null
  }

  const date = new Date(eventDate)

  if (Number.isNaN(date.getTime())) {
    return null
  }

  const hours = date.getHours()
  const minutes = date.getMinutes()

  if (hours === 0 && minutes === 0) {
    return null
  }

  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit'
  })
}

async function loadCalendar(showMainLoader = true) {
  if (showMainLoader) {
    loading.value = true
  }

  errorMessage.value = ''

  try {
    events.value = await getCalendarEvents(
      calendarRangeStart.value,
      calendarRangeEnd.value
    )
  } catch (error) {
    console.error('Failed to load calendar:', error)

    events.value = []

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'The herd calendar could not be loaded.'
  } finally {
    if (showMainLoader) {
      loading.value = false
    }
  }
}

async function refreshCalendar() {
  if (refreshing.value) {
    return
  }

  refreshing.value = true

  try {
    await loadCalendar(false)
  } finally {
    refreshing.value = false
  }
}

async function changeMonth(monthDifference: number) {
  currentMonth.value = new Date(
    currentMonth.value.getFullYear(),
    currentMonth.value.getMonth() + monthDifference,
    1
  )

  selectedDateKey.value = null

  await loadCalendar()
}

async function goToToday() {
  const today = new Date()

  currentMonth.value = new Date(
    today.getFullYear(),
    today.getMonth(),
    1
  )

  selectedDateKey.value = getDateKey(today)

  await loadCalendar()
}

function selectDay(day: CalendarDay) {
  selectedDateKey.value = day.key
}

function openAnimal(event: CalendarEvent) {
  router.push(`/animals/${event.animalId}`)
}

function goToDashboard() {
  router.push('/')
}

onMounted(async () => {
  const today = new Date()

  selectedDateKey.value = getDateKey(today)

  await loadCalendar()
})
</script>

<template>
  <main class="calendar-page">
    <header class="calendar-hero">
      <div>
        <button
          class="back-button"
          type="button"
          @click="goToDashboard"
        >
          ← Dashboard
        </button>

        <p class="brand">
          VENTURE HERD MANAGER
        </p>

        <h1>Herd Calendar</h1>

        <p class="hero-description">
          See heats, breedings, pregnancy checks, due dates,
          calvings, and dry-offs in one place.
        </p>
      </div>

      <button
        class="refresh-button"
        type="button"
        :disabled="refreshing"
        @click="refreshCalendar"
      >
        <span
          class="refresh-icon"
          :class="{ spinning: refreshing }"
        >
          ↻
        </span>

        {{ refreshing ? 'Refreshing...' : 'Refresh' }}
      </button>
    </header>

    <section class="calendar-toolbar">
      <div class="month-navigation">
        <button
          class="navigation-button"
          type="button"
          aria-label="Previous month"
          @click="changeMonth(-1)"
        >
          ‹
        </button>

        <div class="month-title">
          <p>HERD SCHEDULE</p>
          <h2>{{ monthTitle }}</h2>
        </div>

        <button
          class="navigation-button"
          type="button"
          aria-label="Next month"
          @click="changeMonth(1)"
        >
          ›
        </button>
      </div>

      <div class="toolbar-actions">
        <span class="event-count">
          {{ monthEventCount }}
          {{ monthEventCount === 1 ? 'event' : 'events' }}
        </span>

        <button
          class="today-button"
          type="button"
          @click="goToToday"
        >
          Today
        </button>
      </div>
    </section>

    <section
      v-if="errorMessage"
      class="error-alert"
    >
      <div>
        <strong>Unable to load the calendar</strong>
        <p>{{ errorMessage }}</p>
      </div>

      <button
        type="button"
        @click="loadCalendar()"
      >
        Try Again
      </button>
    </section>

    <section class="legend">
      <div
        v-for="eventType in eventTypes"
        :key="eventType.type"
        class="legend-item"
      >
        <span
          class="legend-dot"
          :class="`event-${eventType.type}`"
        >
          {{ eventType.icon }}
        </span>

        <span>{{ eventType.label }}</span>
      </div>
    </section>

    <section
      v-if="loading"
      class="calendar-loading"
    >
      <div
        v-for="item in 35"
        :key="item"
        class="day-skeleton"
      />
    </section>

    <section
      v-else
      class="calendar-layout"
    >
      <div class="calendar-card">
        <div class="weekday-row">
          <div
            v-for="weekDay in weekDays"
            :key="weekDay"
            class="weekday"
          >
            {{ weekDay }}
          </div>
        </div>

        <div class="calendar-grid">
          <button
            v-for="day in calendarDays"
            :key="day.key"
            class="calendar-day"
            :class="{
              'other-month': !day.isCurrentMonth,
              'today': day.isToday,
              'selected': selectedDateKey === day.key
            }"
            type="button"
            @click="selectDay(day)"
          >
            <span class="day-number">
              {{ day.dayNumber }}
            </span>

            <div class="day-events">
              <div
                v-for="event in day.events.slice(0, 3)"
                :key="event.eventId"
                class="calendar-event"
                :class="`event-${event.eventType}`"
                :title="`${event.animalName}: ${event.title}`"
                @click.stop="openAnimal(event)"
              >
                <span class="event-icon">
                  {{ getEventIcon(event.eventType) }}
                </span>

                <span class="event-animal">
                  {{ event.animalName }}
                </span>
              </div>

              <span
                v-if="day.events.length > 3"
                class="more-events"
              >
                +{{ day.events.length - 3 }} more
              </span>
            </div>
          </button>
        </div>
      </div>

      <aside class="day-details">
        <template v-if="selectedDay">
          <div class="details-heading">
            <div>
              <p>SELECTED DATE</p>
              <h3>{{ selectedDateTitle }}</h3>
            </div>

            <span class="day-event-count">
              {{ selectedDay.events.length }}
            </span>
          </div>

          <div
            v-if="selectedDay.events.length === 0"
            class="no-events"
          >
            <span>✓</span>
            <strong>No herd events</strong>
            <p>
              Nothing is currently scheduled for this date.
            </p>
          </div>

          <div
            v-else
            class="event-detail-list"
          >
            <button
              v-for="event in selectedDay.events"
              :key="event.eventId"
              class="event-detail-card"
              type="button"
              @click="openAnimal(event)"
            >
              <span
                class="detail-icon"
                :class="`event-${event.eventType}`"
              >
                {{ getEventIcon(event.eventType) }}
              </span>

              <span class="detail-content">
                <span class="detail-header">
                  <strong>{{ event.animalName }}</strong>

                  <span v-if="getEventTime(event.eventDate)">
                    {{ getEventTime(event.eventDate) }}
                  </span>
                </span>

                <span class="detail-type">
                  {{ getEventTypeLabel(event.eventType) }}
                </span>

                <span class="detail-title">
                  {{ event.title }}
                </span>

                <span
                  v-if="event.description"
                  class="detail-description"
                >
                  {{ event.description }}
                </span>

                <span class="view-animal">
                  View animal →
                </span>
              </span>
            </button>
          </div>
        </template>
      </aside>
    </section>
  </main>
</template>

<style scoped>
.calendar-page {
  width: min(100%, 1280px);
  margin: 0 auto;
  padding: 28px 24px 60px;
}

.calendar-hero {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 24px;
  padding: 27px;
  border: 1px solid #dce5de;
  border-radius: 24px;
  background:
    linear-gradient(
      135deg,
      rgba(49, 87, 44, 0.09),
      rgba(255, 255, 255, 0.98)
    );
  box-shadow: 0 10px 30px rgba(33, 65, 41, 0.07);
}

.back-button {
  margin: 0 0 18px;
  padding: 0;
  border: none;
  background: transparent;
  color: #486b45;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
}

.back-button:hover {
  color: #254520;
}

.brand,
.month-title p,
.details-heading p {
  margin: 0;
  color: #31572c;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.1em;
}

.calendar-hero h1 {
  margin: 7px 0 8px;
  color: #18351f;
  font-size: clamp(32px, 5vw, 44px);
  line-height: 1.05;
}

.hero-description {
  max-width: 680px;
  margin: 0;
  color: #64748b;
  line-height: 1.6;
}

.refresh-button,
.today-button {
  display: inline-flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 44px;
  padding: 11px 17px;
  border: none;
  border-radius: 999px;
  background: #31572c;
  color: white;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
}

.refresh-button:hover:not(:disabled),
.today-button:hover {
  background: #254520;
}

.refresh-button:disabled {
  cursor: wait;
  opacity: 0.65;
}

.refresh-icon {
  display: inline-block;
  font-size: 19px;
}

.spinning {
  animation: spin 0.8s linear infinite;
}

.calendar-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 16px;
  padding: 18px 20px;
  border: 1px solid #e0e7e2;
  border-radius: 20px;
  background: white;
}

.month-navigation {
  display: flex;
  align-items: center;
  gap: 16px;
}

.navigation-button {
  display: grid;
  width: 44px;
  height: 44px;
  flex-shrink: 0;
  place-items: center;
  border: 1px solid #d7e1d9;
  border-radius: 50%;
  background: #f8faf8;
  color: #31572c;
  font-size: 30px;
  line-height: 1;
  cursor: pointer;
}

.navigation-button:hover {
  border-color: #aebdaf;
  background: #eef4ef;
}

.month-title {
  min-width: 190px;
  text-align: center;
}

.month-title h2 {
  margin: 4px 0 0;
  color: #203926;
  font-size: 26px;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.event-count,
.day-event-count {
  padding: 9px 12px;
  border-radius: 999px;
  background: #e8f0e9;
  color: #31572c;
  font-size: 13px;
  font-weight: 900;
}

.legend {
  display: flex;
  flex-wrap: wrap;
  gap: 9px;
  margin-bottom: 16px;
  padding: 13px 15px;
  border: 1px solid #e0e7e2;
  border-radius: 16px;
  background: #f8faf8;
}

.legend-item {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  color: #526057;
  font-size: 13px;
  font-weight: 750;
}

.legend-dot {
  display: grid;
  width: 24px;
  height: 24px;
  place-items: center;
  border-radius: 7px;
  font-size: 12px;
  font-weight: 900;
}

.calendar-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 330px;
  align-items: start;
  gap: 18px;
}

.calendar-card,
.day-details {
  border: 1px solid #dfe7e1;
  border-radius: 20px;
  background: white;
  box-shadow: 0 8px 24px rgba(32, 61, 39, 0.05);
  overflow: hidden;
}

.weekday-row,
.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
}

.weekday {
  padding: 13px 6px;
  border-right: 1px solid #e7ece8;
  border-bottom: 1px solid #dfe7e1;
  background: #f4f7f4;
  color: #526057;
  font-size: 12px;
  font-weight: 900;
  text-align: center;
  text-transform: uppercase;
}

.weekday:last-child {
  border-right: none;
}

.calendar-day {
  position: relative;
  min-width: 0;
  min-height: 125px;
  padding: 9px;
  border: none;
  border-right: 1px solid #e7ece8;
  border-bottom: 1px solid #e7ece8;
  background: white;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.calendar-day:nth-child(7n) {
  border-right: none;
}

.calendar-day:hover {
  background: #fafcfa;
}

.calendar-day.other-month {
  background: #f8faf9;
  color: #9ca9a0;
}

.calendar-day.today {
  background: #f3f8f3;
}

.calendar-day.selected {
  z-index: 1;
  box-shadow: inset 0 0 0 2px #31572c;
}

.day-number {
  display: grid;
  width: 28px;
  height: 28px;
  place-items: center;
  border-radius: 50%;
  color: #46554b;
  font-size: 13px;
  font-weight: 900;
}

.today .day-number {
  background: #31572c;
  color: white;
}

.day-events {
  display: grid;
  gap: 4px;
  margin-top: 5px;
}

.calendar-event {
  display: flex;
  align-items: center;
  gap: 5px;
  min-width: 0;
  padding: 5px 6px;
  border-radius: 7px;
  font-size: 11px;
  font-weight: 800;
  cursor: pointer;
}

.event-icon {
  flex-shrink: 0;
}

.event-animal {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.more-events {
  padding: 2px 5px;
  color: #64748b;
  font-size: 10px;
  font-weight: 800;
}

.day-details {
  position: sticky;
  top: 18px;
  padding: 19px;
}

.details-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 16px;
  border-bottom: 1px solid #e4eae5;
}

.details-heading h3 {
  margin: 5px 0 0;
  color: #253c2b;
  font-size: 19px;
  line-height: 1.3;
}

.no-events {
  display: grid;
  justify-items: center;
  padding: 36px 15px;
  color: #64748b;
  text-align: center;
}

.no-events > span {
  display: grid;
  width: 46px;
  height: 46px;
  margin-bottom: 10px;
  place-items: center;
  border-radius: 50%;
  background: #eaf2eb;
  color: #31572c;
  font-size: 21px;
  font-weight: 900;
}

.no-events strong {
  color: #334155;
}

.no-events p {
  margin: 7px 0 0;
  line-height: 1.5;
}

.event-detail-list {
  display: grid;
  gap: 10px;
  margin-top: 15px;
}

.event-detail-card {
  display: flex;
  align-items: flex-start;
  gap: 11px;
  width: 100%;
  padding: 13px;
  border: 1px solid #e0e7e2;
  border-radius: 14px;
  background: #fbfcfb;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.event-detail-card:hover {
  border-color: #b6c4b8;
  background: white;
}

.detail-icon {
  display: grid;
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  place-items: center;
  border-radius: 9px;
  font-size: 14px;
  font-weight: 900;
}

.detail-content {
  display: grid;
  min-width: 0;
  flex: 1;
}

.detail-header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.detail-header strong {
  overflow: hidden;
  color: #263a2b;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.detail-header > span {
  flex-shrink: 0;
  color: #64748b;
  font-size: 12px;
  font-weight: 700;
}

.detail-type {
  margin-top: 2px;
  color: #31572c;
  font-size: 11px;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.detail-title {
  margin-top: 5px;
  color: #475569;
  font-size: 13px;
  font-weight: 750;
}

.detail-description {
  margin-top: 5px;
  color: #64748b;
  font-size: 12px;
  line-height: 1.4;
}

.view-animal {
  margin-top: 8px;
  color: #31572c;
  font-size: 12px;
  font-weight: 900;
}

.error-alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  margin-bottom: 16px;
  padding: 15px 17px;
  border: 1px solid #fecaca;
  border-radius: 15px;
  background: #fff5f5;
  color: #991b1b;
}

.error-alert strong {
  display: block;
  margin-bottom: 3px;
}

.error-alert p {
  margin: 0;
}

.error-alert button {
  flex-shrink: 0;
  padding: 9px 14px;
  border: 1px solid #fecaca;
  border-radius: 999px;
  background: white;
  color: #991b1b;
  font-weight: 800;
  cursor: pointer;
}

.calendar-loading {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  border: 1px solid #e0e7e2;
  border-radius: 20px;
  overflow: hidden;
}

.day-skeleton {
  min-height: 120px;
  border-right: 1px solid #e7ece8;
  border-bottom: 1px solid #e7ece8;
  background: #f2f5f2;
  animation: pulse 1.2s ease-in-out infinite;
}

.event-heat {
  background: #feecef;
  color: #a51d3d;
}

.event-breeding {
  background: #ecebff;
  color: #5144a4;
}

.event-pregnancyCheck {
  background: #fff3d9;
  color: #9a6110;
}

.event-dueDate {
  background: #e5f2ff;
  color: #176398;
}

.event-calving {
  background: #e8f7ed;
  color: #21713c;
}

.event-dryOff {
  background: #f1ede8;
  color: #745337;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes pulse {
  0%,
  100% {
    opacity: 0.55;
  }

  50% {
    opacity: 1;
  }
}

@media (max-width: 980px) {
  .calendar-layout {
    grid-template-columns: 1fr;
  }

  .day-details {
    position: static;
  }
}

@media (max-width: 720px) {
  .calendar-page {
    padding: 16px 12px 42px;
  }

  .calendar-hero {
    flex-direction: column;
    padding: 21px 17px;
  }

  .refresh-button {
    width: 100%;
  }

  .calendar-toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .month-navigation {
    justify-content: space-between;
  }

  .toolbar-actions {
    justify-content: space-between;
  }

  .calendar-card {
    overflow-x: auto;
  }

  .weekday-row,
  .calendar-grid {
    min-width: 720px;
  }

  .legend {
    flex-wrap: nowrap;
    overflow-x: auto;
  }
}

@media (max-width: 430px) {
  .month-title {
    min-width: 0;
  }

  .month-title h2 {
    font-size: 21px;
  }

  .event-count {
    font-size: 12px;
  }
}
</style>