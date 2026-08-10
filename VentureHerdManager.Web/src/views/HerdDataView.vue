<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getAnimalsBasic } from '../api/animals'
import { applyHerdData, getHerdDataAnalytics, previewHerdData, type HerdDataPreview, type HerdDataSource } from '../api/herdData'
import type { Animal } from '../models/Animal'

const router = useRouter()
const route = useRoute()
const animals = ref<Animal[]>([])
const analytics = ref<any>(null)
const source = ref<HerdDataSource>(1)
const fileName = ref('')
const csvText = ref('')
const reportDate = ref(new Date().toISOString().slice(0, 10))
const preview = ref<HerdDataPreview | null>(null)
const mappings = ref<Record<string, number>>({})
const status = ref('')
const busy = ref(false)
const combinedSearch = ref('')
const importDetails = ref<HTMLDetailsElement | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)

function chooseSource(nextSource: HerdDataSource) {
  source.value = nextSource
  nextTick(() => {
    if (importDetails.value) importDetails.value.open = true
  })
}

onMounted(async () => {
  source.value = route.query.source === '2' ? 2 : 1
  if (route.query.source === '1' || route.query.source === '2') {
    await nextTick()
    if (importDetails.value) importDetails.value.open = true
  }
  try {
    ;[analytics.value, animals.value] = await Promise.all([getHerdDataAnalytics(), getAnimalsBasic()])
  } catch (error) { status.value = error instanceof Error ? error.message : 'Private analytics could not load.' }
})

async function loadFile(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  fileName.value = file.name
  csvText.value = await file.text()
  source.value = file.name.toLowerCase().includes('coretraits') ? 2 : 1
  preview.value = null
  mappings.value = {}
  status.value = `Reading ${file.name}…`
  await previewImport()
}

function payload() { return { source: source.value, fileName: fileName.value, csvText: csvText.value, reportDate: reportDate.value, animalMappings: mappings.value } }
async function previewImport() {
  busy.value = true; status.value = ''
  try {
    preview.value = await previewHerdData(payload())
    for (const row of preview.value.rows) if (row.animalId) mappings.value[row.sourceKey] = row.animalId
    status.value = needsMatch.value.length === 0
      ? `${preview.value.rowsRead} rows matched. Tap Save confirmed import.`
      : `${preview.value.rowsRead} rows read. Confirm the ${needsMatch.value.length} highlighted match${needsMatch.value.length === 1 ? '' : 'es'}, then save.`
  } catch (error) { status.value = error instanceof Error ? error.message : 'Preview failed.' }
  finally { busy.value = false }
}
async function applyImport() {
  busy.value = true; status.value = ''
  try { await applyHerdData(payload()); analytics.value = await getHerdDataAnalytics(); status.value = 'Import saved. Animal histories and analytics are updated.'; preview.value = null }
  catch (error) { status.value = error instanceof Error ? error.message : 'Import failed.' }
  finally { busy.value = false }
}
const needsMatch = computed(() => preview.value?.rows.filter(row => !mappings.value[row.sourceKey]) ?? [])
const filteredCombined = computed(() => (analytics.value?.combined ?? []).filter((row: any) => !combinedSearch.value || row.animalName?.toLowerCase().includes(combinedSearch.value.toLowerCase())))
</script>

<template>
  <main class="data-page">
    <header><button @click="router.push('/reports')">← Reports</button><h1>Milk & Genomic Analytics</h1><p>Private herd production, genomic comparisons, and mating decisions.</p></header>
    <div class="import-choice">
      <button type="button" :class="{ active: source === 1 }" @click="chooseSource(1)">Import PC-DART Milk</button>
      <button type="button" :class="{ active: source === 2 }" @click="chooseSource(2)">Import Zoetis Genomics</button>
    </div>
    <template>
      <details ref="importDetails" class="card import-card" open>
        <summary>Import report</summary>
        <p class="import-instruction">{{ source === 2 ? 'Choose your Zoetis Core Traits CSV, then preview the animal matches.' : 'Choose your PC-DART CSV, then preview the animal matches.' }}</p>
        <label class="choose-file" for="herd-data-file">Choose {{ source === 2 ? 'Zoetis Genomics' : 'PC-DART' }} CSV File</label>
        <input id="herd-data-file" ref="fileInput" class="file-input" type="file" accept=".csv,text/csv" @change="loadFile">
        <div class="controls"><select v-model.number="source"><option :value="1">PC-DART milk report</option><option :value="2">Zoetis genomic report</option></select><input v-model="reportDate" type="date"><strong class="selected-file">{{ fileName || 'No file selected yet' }}</strong></div>
        <div class="actions"><button :disabled="busy || !csvText" @click="previewImport">Preview & match</button><button :disabled="busy || !preview || needsMatch.length > 0" @click="applyImport">Save confirmed import</button></div>
        <p v-if="status" :class="{ error: status.includes('failed') || status.includes('required') }">{{ status }}</p>
        <div v-if="preview" class="match-list"><p><strong>{{ preview.rowsRead }}</strong> rows · {{ needsMatch.length }} need confirmation</p><label v-for="row in preview.rows" :key="row.sourceKey"><span>{{ row.sourceName }} <small>{{ row.officialId }}</small></span><select v-model.number="mappings[row.sourceKey]"><option :value="0">Choose animal…</option><option v-for="candidate in row.candidates" :key="candidate.animalId" :value="candidate.animalId">{{ candidate.animalName }} · {{ candidate.registrationNumber }}</option><option v-for="animal in animals" :key="`all-${animal.animalId}`" :value="animal.animalId">{{ animal.barnName || animal.registeredName || `#${animal.animalId}` }}</option></select></label></div>
      </details>
      <details class="card"><summary>Milk table</summary><div class="table-wrap"><table><thead><tr><th>Animal</th><th>Milk</th><th>DIM</th><th>Fat %</th><th>Protein %</th></tr></thead><tbody><tr v-for="row in analytics?.milk ?? []" :key="row.animalId"><td>{{ row.animalName }}</td><td>{{ row.milk }}</td><td>{{ row.daysInMilk }}</td><td>{{ row.fatPercent }}</td><td>{{ row.proteinPercent }}</td></tr></tbody></table></div></details>
      <details class="card"><summary>Genomic table</summary><div class="table-wrap"><table><thead><tr><th>Animal</th><th>TPI</th><th>NM$</th><th>Milk PTA</th><th>DPR</th><th>PL</th><th>Type</th><th>UDC</th><th>FLC</th></tr></thead><tbody><tr v-for="row in analytics?.genomic ?? []" :key="row.animalId"><td>{{ row.animalName }}</td><td>{{ row.tpi }}</td><td>{{ row.netMerit }}</td><td>{{ row.milkPta }}</td><td>{{ row.daughterPregnancyRate }}</td><td>{{ row.productiveLife }}</td><td>{{ row.typeScore }}</td><td>{{ row.udderComposite }}</td><td>{{ row.feetLegsComposite }}</td></tr></tbody></table></div></details>
      <details class="card"><summary>Combined sale &amp; breeding review</summary><input v-model="combinedSearch" type="search" placeholder="Search animal"><div class="table-wrap"><table><thead><tr><th>Animal</th><th>Actual milk</th><th>DIM</th><th>TPI</th><th>NM$</th><th>Milk PTA</th><th>DPR</th><th>PL</th><th>Type</th><th>UDC</th><th>FLC</th></tr></thead><tbody><tr v-for="row in filteredCombined" :key="row.animalId"><td><button class="link" @click="router.push(`/animals/${row.animalId}`)">{{ row.animalName }}</button></td><td>{{ row.milk }}</td><td>{{ row.daysInMilk }}</td><td>{{ row.tpi }}</td><td>{{ row.netMerit }}</td><td>{{ row.milkPta }}</td><td>{{ row.daughterPregnancyRate }}</td><td>{{ row.productiveLife }}</td><td>{{ row.typeScore }}</td><td>{{ row.udderComposite }}</td><td>{{ row.feetLegsComposite }}</td></tr></tbody></table></div></details>
    </template>
  </main>
</template>

<style scoped>
.import-choice{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:14px 0}.import-choice button{min-height:52px;border:2px solid #31572c;border-radius:9px;background:#fff;color:#31572c;font-weight:900}.import-choice button.active{background:#31572c;color:#fff}
.import-instruction{font-weight:750;color:#31572c}.choose-file{width:100%;min-height:56px;background:#31572c;color:#fff;font-size:1rem;display:grid;place-items:center;border-radius:8px;font-weight:900;cursor:pointer;box-sizing:border-box}.file-input{width:100%;min-height:48px;margin:8px 0;padding:7px;border:1px solid #bdcbbf;border-radius:8px;box-sizing:border-box}.selected-file{display:flex;align-items:center;min-height:44px;padding:0 10px;border:1px solid #bdcbbf;border-radius:7px;box-sizing:border-box;font-size:.85rem}
.data-page{max-width:1240px;margin:auto;padding:16px;background:#f5f7f2;min-height:100vh}header{padding:20px;border-radius:12px;background:#173422;color:#fff}header button,.card button{min-height:44px;border:0;border-radius:7px;padding:0 14px;font-weight:850}.card{margin:14px 0;padding:16px;border:1px solid #d8e2da;border-radius:12px;background:#fff}.card summary{cursor:pointer;font-size:1.2rem;font-weight:900;min-height:34px}.card[open] summary{margin-bottom:14px}.controls input,.controls select,.card>input,.match-list select{min-height:44px;border:1px solid #bdcbbf;border-radius:7px;padding:8px;width:100%;box-sizing:border-box}.controls{display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px}.actions{display:flex;gap:8px;margin-top:12px}.actions button{background:#31572c;color:#fff}.match-list{display:grid;gap:8px;margin-top:14px;max-height:520px;overflow:auto}.match-list label{display:grid;grid-template-columns:1fr 1.3fr;gap:10px;align-items:center;padding:8px;border:1px solid #e0e7e1;border-radius:8px}.match-list span{display:grid;font-weight:800}.match-list small{font-weight:400;color:#64746a}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse;min-width:720px}th,td{padding:9px;border-bottom:1px solid #e1e7e2;text-align:left}th{background:#eef5ef}.link{background:transparent!important;color:#31572c;padding:0!important}.error{color:#991b1b}@media(max-width:640px){.controls,.match-list label{grid-template-columns:1fr}.actions{display:grid}.actions button{width:100%}.data-page{padding:8px}.card{padding:12px}}
</style>
