<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { getSharedBaggingSchedule } from '../api/baggingSchedules'

interface Quarter {
  key: 'frontLeft' | 'frontRight' | 'rearLeft' | 'rearRight'
  label: string
  hoursBeforeRing: number | null
}

interface BaggingRow {
  id: number
  animalName?: string
  entryTime: string
  notes: string
  quarters: Quarter[]
}

const route = useRoute()
const loading = ref(true)
const error = ref('')
const showName = ref('Show Bagging')
const showDate = ref('')
const rows = ref<BaggingRow[]>([])

function formatTime(value: string): string {
  if (!value) return 'Not set'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Not set' : date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
}

function formatDate(value: string): string {
  if (!value) return ''
  const date = new Date(`${value.slice(0, 10)}T12:00:00`)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString([], { weekday: 'long', month: 'long', day: 'numeric' })
}

function milkTime(row: BaggingRow, quarter: Quarter): string {
  if (!row.entryTime || quarter.hoursBeforeRing === null) return '—'
  const date = new Date(row.entryTime)
  if (Number.isNaN(date.getTime())) return '—'
  date.setTime(date.getTime() - quarter.hoursBeforeRing * 3_600_000)
  return formatTime(date.toISOString())
}

function shortLabel(key: Quarter['key']): string {
  return key === 'frontLeft' ? 'FL' : key === 'frontRight' ? 'FR' : key === 'rearLeft' ? 'RL' : 'RR'
}

const orderedRows = computed(() => [...rows.value].sort((left, right) => left.entryTime.localeCompare(right.entryTime)))
const nextTask = computed(() => {
  const now = Date.now()
  return orderedRows.value.flatMap(row => row.quarters.map(quarter => {
    const ring = new Date(row.entryTime).getTime()
    const time = quarter.hoursBeforeRing === null ? Number.NaN : ring - quarter.hoursBeforeRing * 3_600_000
    return { row, quarter, time }
  })).filter(task => Number.isFinite(task.time) && task.time >= now).sort((a, b) => a.time - b.time)[0]
})

onMounted(async () => {
  try {
    const saved = await getSharedBaggingSchedule(String(route.params.token || ''))
    const plan = JSON.parse(saved.scheduleJson)
    showName.value = saved.showName || 'Show Bagging'
    showDate.value = saved.showDate || ''
    rows.value = Array.isArray(plan.rows) ? plan.rows : []
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'This bagging plan could not be loaded.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <main class="shared-bagging">
    <header>
      <span>VENTURE HERD MANAGER</span>
      <h1>{{ showName }}</h1>
      <p>{{ formatDate(showDate) }} · Shared bagging plan</p>
    </header>

    <div v-if="loading" class="notice">Opening bagging plan…</div>
    <div v-else-if="error" class="notice error">{{ error }} Ask the sender to share it again.</div>

    <section v-if="nextTask" class="next-task">
      <small>Next milk-out</small>
      <strong>{{ nextTask.row.animalName || 'Cow' }} · {{ nextTask.quarter.label }}</strong>
      <time>{{ formatTime(new Date(nextTask.time).toISOString()) }}</time>
    </section>

    <article v-for="row in orderedRows" :key="row.id" class="cow-card">
      <div class="cow-head">
        <div><small>Cow</small><h2>{{ row.animalName || 'Unnamed cow' }}</h2></div>
        <div class="ring-time"><small>Goes to ring</small><strong>{{ formatTime(row.entryTime) }}</strong></div>
      </div>

      <div class="udder-label"><span>Rear</span><b>Milk-out times</b><span>Front</span></div>
      <div class="udder-grid">
        <div v-for="quarter in row.quarters" :key="quarter.key" class="quarter">
          <div class="quarter-mark"><i /> <b>{{ shortLabel(quarter.key) }}</b></div>
          <span>{{ quarter.label }}</span>
          <strong>{{ milkTime(row, quarter) }}</strong>
          <small>{{ quarter.hoursBeforeRing === null ? 'Hours not set' : `${quarter.hoursBeforeRing}h before ring` }}</small>
        </div>
      </div>

      <details v-if="row.notes">
        <summary>Notes</summary>
        <p>{{ row.notes }}</p>
      </details>
    </article>
  </main>
</template>

<style scoped>
.shared-bagging{width:min(760px,calc(100% - 20px));margin:0 auto;padding:18px 0 60px;color:#132218}.shared-bagging>header{padding:22px;border-radius:15px;background:#10261a;color:#fff;box-shadow:0 10px 28px #10261a25}.shared-bagging>header span{color:#d7af69;font-size:.68rem;font-weight:950;letter-spacing:.16em}.shared-bagging h1{margin:5px 0 3px;font-size:clamp(1.8rem,7vw,3rem)}.shared-bagging>header p{margin:0;color:#d8e4da}.notice{margin:16px 0;padding:18px;border-radius:10px;background:#fff;border:1px solid #d8e1d9}.notice.error{background:#fff5ed;border-color:#e4b98c}.next-task{display:grid;grid-template-columns:1fr auto;gap:3px 12px;margin:14px 0;padding:13px 15px;border-radius:12px;background:#d7ad64;color:#172219}.next-task small{grid-column:1/-1;text-transform:uppercase;letter-spacing:.1em;font-weight:900}.next-task strong{font-size:1rem}.next-task time{font-size:1.2rem;font-weight:950}.cow-card{margin:12px 0;padding:14px;border:1px solid #d5e0d7;border-radius:14px;background:#fff;box-shadow:0 4px 15px #173b2420}.cow-head{display:flex;justify-content:space-between;align-items:start;gap:12px}.cow-head small,.ring-time small{display:block;color:#68766b;font-size:.66rem;text-transform:uppercase;letter-spacing:.08em;font-weight:900}.cow-head h2{margin:2px 0 0;font-size:1.25rem}.ring-time{text-align:right;padding:8px 10px;border-radius:9px;background:#173e25;color:#fff}.ring-time small{color:#ccddcf}.ring-time strong{font-size:1.15rem}.udder-label{display:flex;justify-content:space-between;margin:14px 4px 7px;color:#69776c;font-size:.65rem;text-transform:uppercase;letter-spacing:.08em;font-weight:900}.udder-label b{color:#31572c}.udder-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}.quarter{display:grid;gap:3px;min-width:0;padding:11px;border:2px solid #cad8cc;border-radius:17px;background:linear-gradient(145deg,#fff,#f3f7f3)}.quarter-mark{display:flex;justify-content:space-between;align-items:center;color:#31572c}.quarter-mark i{width:18px;height:18px;border-radius:50%;background:#e7c6bd;border:3px solid #fff;box-shadow:0 0 0 2px #b98477}.quarter>span{font-size:.69rem;color:#657369;font-weight:850}.quarter>strong{font-size:1.08rem}.quarter>small{font-size:.68rem;color:#657369}.cow-card details{margin-top:10px;border-top:1px solid #e2e9e3;padding-top:8px}.cow-card summary{cursor:pointer;color:#31572c;font-size:.8rem;font-weight:900}.cow-card p{white-space:pre-wrap}.shared-bagging h2{overflow-wrap:anywhere}@media(max-width:430px){.shared-bagging{width:calc(100% - 12px);padding-top:6px}.shared-bagging>header{padding:18px}.cow-card{padding:10px}.quarter{padding:9px;border-radius:14px}.quarter>strong{font-size:.96rem}.ring-time{padding:7px 8px}}
</style>
