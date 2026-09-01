<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { formatCurrentAge } from '../utils/showClasses'

interface SharedRow {
  animalId: number
  name: string
  registeredName: string
  birthDate: string
  showClass: string
  stage: number
  feedRation: string
  feedNotes: string
  ringDirections: string
}

const route = useRoute()
const shareStatus = ref('')
const selectedClass = ref('all')

function decodePayload(): { rows: SharedRow[]; available: SharedRow[] } {
  try {
    const encoded = String(route.query.data || '').replace(/-/g, '+').replace(/_/g, '/')
    const binary = atob(encoded.padEnd(Math.ceil(encoded.length / 4) * 4, '='))
    const bytes = Uint8Array.from(binary, character => character.charCodeAt(0))
    const payload = JSON.parse(new TextDecoder().decode(bytes))
    return {
      rows: Array.isArray(payload.rows) ? payload.rows : [],
      available: Array.isArray(payload.available) ? payload.available : []
    }
  } catch {
    return { rows: [], available: [] }
  }
}

const decoded = decodePayload()
const rows = ref(decoded.rows)
const available = ref(decoded.available)
const cows = computed(() => rows.value.filter(row => row.stage === 3 || row.stage === 4 || row.showClass.includes('Cow')))
const youngstock = computed(() => rows.value.filter(row => !cows.value.includes(row)))
const classes = computed(() => [...new Set(available.value.map(row => row.showClass))])
const candidates = computed(() => available.value.filter(row => selectedClass.value === 'all' || row.showClass === selectedClass.value))
const hasNotes = (row: SharedRow) => Boolean(row.feedRation || row.feedNotes || row.ringDirections)

function addAnimal(row: SharedRow) {
  rows.value.push(row)
  rows.value.sort((left, right) => left.birthDate.localeCompare(right.birthDate))
  available.value = available.value.filter(candidate => candidate.animalId !== row.animalId)
  shareStatus.value = `${row.name} added to this copy. Share the updated lineup to send the change back.`
}

function encodePayload(): string {
  const json = JSON.stringify({ version: 2, sharedAt: new Date().toISOString(), rows: rows.value, available: available.value })
  const bytes = new TextEncoder().encode(json)
  let binary = ''
  bytes.forEach(byte => { binary += String.fromCharCode(byte) })
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '')
}

async function shareUpdated() {
  const url = `${window.location.origin}/shows/shared?data=${encodeURIComponent(encodePayload())}`
  try {
    if (navigator.share) {
      await navigator.share({ title: 'Updated Show String', text: 'Here is the updated show string lineup.', url })
      shareStatus.value = 'Updated lineup shared.'
    } else {
      await navigator.clipboard.writeText(url)
      shareStatus.value = 'Updated lineup link copied.'
    }
  } catch {
    shareStatus.value = url
  }
}
</script>

<template>
  <main class="shared-show">
    <header>
      <span class="eyebrow">VENTURE HERD MANAGER</span>
      <h1>Show String</h1>
      <p>Shared lineup · oldest to youngest</p>
    </header>

    <div v-if="rows.length === 0" class="empty">This shared show string is missing or incomplete. Ask the sender to share it again.</div>

    <details v-if="available.length" class="available-panel">
      <summary>Open available animals <span>{{ available.length }}</span></summary>
      <div class="class-picker">
        <button type="button" :class="{ active: selectedClass === 'all' }" @click="selectedClass = 'all'">All</button>
        <button v-for="showClass in classes" :key="showClass" type="button" :class="{ active: selectedClass === showClass }" @click="selectedClass = showClass">{{ showClass }}</button>
      </div>
      <div class="candidate-list">
        <div v-for="animal in candidates" :key="animal.animalId" class="candidate">
          <div><strong>{{ animal.name }}</strong><small>{{ animal.showClass }} · {{ formatCurrentAge(animal.birthDate) }}</small></div>
          <button type="button" @click="addAnimal(animal)">+ Add</button>
        </div>
      </div>
    </details>

    <button v-if="rows.length" type="button" class="share-updated" @click="shareUpdated">Share Updated Lineup</button>
    <p v-if="shareStatus" class="share-status">{{ shareStatus }}</p>

    <template v-for="section in [{ title: 'Cows', rows: cows }, { title: 'Heifers & Youngstock', rows: youngstock }]" :key="section.title">
      <section v-if="section.rows.length">
        <h2>{{ section.title }} <span>{{ section.rows.length }}</span></h2>
        <article v-for="(row, index) in section.rows" :key="row.animalId || `${section.title}-${index}`">
          <b class="position">{{ index + 1 }}</b>
          <div class="animal">
            <h3>{{ row.name }}</h3>
            <p v-if="row.registeredName && row.registeredName !== row.name" class="registered">{{ row.registeredName }}</p>
            <div class="meta"><strong>{{ row.showClass }}</strong><span>{{ formatCurrentAge(row.birthDate) }}</span><span v-if="row.birthDate">Born {{ new Date(`${row.birthDate.slice(0, 10)}T12:00:00`).toLocaleDateString() }}</span></div>
            <details v-if="hasNotes(row)">
              <summary>Feed &amp; show notes</summary>
              <dl>
                <template v-if="row.feedRation"><dt>Feed ration</dt><dd>{{ row.feedRation }}</dd></template>
                <template v-if="row.feedNotes"><dt>Schedule / notes</dt><dd>{{ row.feedNotes }}</dd></template>
                <template v-if="row.ringDirections"><dt>Ring directions</dt><dd>{{ row.ringDirections }}</dd></template>
              </dl>
            </details>
          </div>
        </article>
      </section>
    </template>
  </main>
</template>

<style scoped>
.shared-show{width:min(760px,calc(100% - 24px));margin:0 auto;padding:28px 0 60px;color:#132218}.shared-show header{padding:24px;border-radius:14px;background:#10261a;color:#fff;box-shadow:0 10px 28px #10261a22}.eyebrow{font-size:.7rem;font-weight:900;letter-spacing:.16em;color:#cfa766}.shared-show h1{margin:4px 0 2px;font-size:clamp(2rem,8vw,3.5rem);line-height:1}.shared-show header p{margin:8px 0 0;color:#dce6de}.available-panel{margin:18px 0 10px;padding:14px;border:1px solid #cad8cc;border-radius:11px;background:#f8fbf8}.available-panel>summary{cursor:pointer;font-weight:900;color:#214e2a}.available-panel>summary span{margin-left:6px;padding:2px 8px;border-radius:99px;background:#31572c;color:#fff;font-size:.72rem}.class-picker{display:flex;gap:6px;overflow-x:auto;padding:12px 0 8px}.class-picker button{flex:0 0 auto;border:1px solid #c8d5ca;border-radius:99px;background:#fff;padding:7px 11px;color:#31572c;font-weight:750}.class-picker button.active{background:#31572c;color:#fff}.candidate-list{max-height:330px;overflow:auto}.candidate{display:flex;align-items:center;justify-content:space-between;gap:10px;padding:9px 2px;border-top:1px solid #e1e9e2}.candidate div{display:grid;gap:2px}.candidate small{color:#68766b}.candidate button,.share-updated{border:0;border-radius:8px;background:#173e25;color:#fff;padding:9px 13px;font-weight:850}.share-updated{width:100%;margin:8px 0}.share-status{overflow-wrap:anywhere;padding:9px;border-radius:8px;background:#e8f4e9;color:#214e2a;font-size:.82rem}.shared-show section{margin-top:24px}.shared-show h2{display:flex;align-items:center;gap:10px;margin:0 0 8px;font-size:1rem;text-transform:uppercase;letter-spacing:.08em}.shared-show h2 span{padding:2px 9px;border-radius:99px;background:#31572c;color:#fff;font-size:.72rem}.shared-show article{display:flex;gap:13px;margin:8px 0;padding:14px;border:1px solid #dbe5dc;border-radius:11px;background:#fff;box-shadow:0 3px 12px #183c2420}.position{display:grid;place-items:center;flex:0 0 40px;height:40px;border-radius:50%;background:#31572c;color:#fff;font-size:1.15rem}.animal{min-width:0;flex:1}.animal h3{margin:1px 0 3px;font-size:1.15rem}.registered{margin:0 0 6px;color:#657368;font-size:.82rem}.meta{display:flex;flex-wrap:wrap;gap:6px 10px;font-size:.78rem;color:#5f6f63}.meta strong{padding:2px 8px;border-radius:99px;background:#e3f3e5;color:#214e2a}.shared-show article details{margin-top:10px;padding-top:8px;border-top:1px solid #e4ebe5}.shared-show article summary{cursor:pointer;color:#31572c;font-weight:850;font-size:.82rem}.shared-show dl{display:grid;gap:7px;margin:10px 0 0}.shared-show dt{font-size:.68rem;font-weight:900;text-transform:uppercase;letter-spacing:.08em;color:#7a8b7e}.shared-show dd{margin:0;white-space:pre-wrap}.empty{margin-top:20px;padding:20px;border:1px solid #e2c98f;border-radius:10px;background:#fff8e7}@media(max-width:520px){.shared-show{width:min(100% - 16px,760px);padding-top:8px}.shared-show header{padding:20px 18px}.shared-show article{padding:12px 10px;gap:10px}.position{flex-basis:34px;height:34px}}
</style>
