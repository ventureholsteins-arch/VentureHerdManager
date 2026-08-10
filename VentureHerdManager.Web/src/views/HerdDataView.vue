<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { createAnimal, getAnimalsBasic } from '../api/animals'
import { applyHerdData, getHerdDataAnalytics, previewHerdData, type HerdDataPreview, type HerdDataSource } from '../api/herdData'
import type { Animal } from '../models/Animal'

const router = useRouter()
const route = useRoute()
const animals = ref<Animal[]>([])
const analytics = ref<any>(null)
const source = ref<HerdDataSource>(1)
type ImportMode = 'pcdartCsv' | 'currentMilkingPdf' | 'cowPagePdf' | 'zoetisCsv'
const importMode = ref<ImportMode>('pcdartCsv')
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
const duplicateDecision = ref<'pending' | 'accepted' | 'declined'>('pending')
const creatingAnimalKey = ref('')
const cowPageDraft = ref<any>(null)
type AnalyticsView = 'attention' | 'milk' | 'genomics' | 'bulls' | 'linear' | 'combined' | 'imports'
const activeView = ref<AnalyticsView>((['attention', 'milk', 'genomics', 'bulls', 'linear', 'combined', 'imports'].includes(String(route.query.view)) ? route.query.view : 'attention') as AnalyticsView)

function selectView(view: AnalyticsView) {
  activeView.value = view
  router.replace({ query: { ...route.query, view } })
}

function chooseSource(nextSource: HerdDataSource) {
  source.value = nextSource
  activeView.value = 'imports'
  nextTick(() => {
    if (importDetails.value) importDetails.value.open = true
  })
}

function chooseImportMode(mode: ImportMode) {
  importMode.value = mode
  chooseSource(mode === 'zoetisCsv' ? 2 : 1)
}

function csvCell(value: unknown) { const text = String(value ?? ''); return `"${text.replace(/"/g, '""')}"` }
function normalizePdfDate(value: string) {
  const match = value.match(/(\d{2})\/(\d{2})\/(\d{4})/)
  return match ? `${match[3]}-${match[1]}-${match[2]}` : value
}
async function extractPdfLines(file: File) {
  const [pdfjs, workerModule] = await Promise.all([import('pdfjs-dist'), import('pdfjs-dist/build/pdf.worker.min.mjs?url')])
  pdfjs.GlobalWorkerOptions.workerSrc = workerModule.default
  const document = await pdfjs.getDocument({ data: new Uint8Array(await file.arrayBuffer()) }).promise
  const lines: string[] = []
  for (let pageNumber = 1; pageNumber <= document.numPages; pageNumber++) {
    const page = await document.getPage(pageNumber)
    const content = await page.getTextContent()
    const rows = new Map<number, Array<{ x: number; text: string }>>()
    for (const raw of content.items as any[]) {
      if (!('str' in raw) || !raw.str.trim()) continue
      const y = Math.round(raw.transform[5] * 2) / 2
      const row = rows.get(y) ?? []
      row.push({ x: raw.transform[4], text: raw.str.trim() }); rows.set(y, row)
    }
    for (const [, row] of [...rows.entries()].sort((a, b) => b[0] - a[0])) lines.push(row.sort((a, b) => a.x - b.x).map(item => item.text).join(' ').replace(/\s+/g, ' ').trim())
  }
  return lines
}
function currentMilkingCsv(lines: string[]) {
  const headers = ['BarnName', 'DHIID', 'Milk', 'DIM', 'LastCalv', 'Previous Milk', 'Milk Deviation', 'Current SCC', 'Lactation', 'Report Type', 'Source Row']
  const rows: string[][] = []
  for (const line of lines) {
    const match = line.match(/^0\s+([A-Z0-9-]+)\s+(.+)$/i); if (!match) continue
    const tokens = match[2].split(/\s+/); const dateIndex = tokens.findIndex(token => /^\d{2}\/\d{2}\/\d{2,4}$/.test(token)); if (dateIndex < 1) continue
    const before = tokens.slice(0, dateIndex); const dim = tokens[dateIndex + 1] ?? ''; const lactation = before.at(-1) ?? ''; const measures = before.slice(0, -1)
    const previousMilk = measures.length >= 3 ? measures[0] : ''; const milk = measures.length >= 3 ? measures[1] : measures[0] ?? ''; const deviation = measures.length >= 3 ? measures[2] : ''; const scc = measures.length >= 4 ? measures.at(-1) ?? '' : ''
    rows.push([match[1], match[1], milk, dim, normalizePdfDate(tokens[dateIndex]), previousMilk, deviation, scc, lactation, 'PC-DART Current Milking PDF', line])
  }
  if (!rows.length) throw new Error('No Current Milking cow rows were found. Choose the PC-DART 005 Production - Milking Cows PDF.')
  return [headers, ...rows].map(row => row.map(csvCell).join(',')).join('\n')
}
function cowPageCsv(lines: string[]) {
  const text = lines.join('\n'); const flat = lines.join(' ')
  const name = flat.match(/Barn Name\s+([A-Z0-9-]+)/i)?.[1] ?? flat.match(/B\s*N\s*a\s*a\s*r\s*m\s*n\s*e\s+([A-Z0-9-]+)/i)?.[1]
  const dhiId = flat.match(/DHI ID\s+(\d{6,})/i)?.[1] ?? ''
  const birth = flat.match(/Date of\s+(\d{2}\/\d{2}\/\d{4})\s+DHI ID/i)?.[1] ?? flat.match(/Birth\s+(\d{2}\/\d{2}\/\d{4})/i)?.[1] ?? lines.find(line => /^\d{2}\/\d{2}\/\d{4}$/.test(line)) ?? ''
  const testDate = flat.match(/Date of Test\s+(\d{2}\/\d{2}\/\d{4})/i)?.[1] ?? ''
  const status = lines.find(line => /^\d{2}\/\d{2}\/\d{4}\s+[\d.]+\s+[\d.]+\s+[\d.]+\s+\d{4,}/.test(line))?.split(/\s+/) ?? []
  if (!name) throw new Error('The cow name was not found. Choose a PC-DART DHI-203 Cow Page PDF.')
  cowPageDraft.value = { name, dhiId: dhiId || name, birthDate: normalizePdfDate(birth), milk: status[1] ?? '', fat: status[2] ?? '', protein: status[3] ?? '', lastCalving: status[0] ? normalizePdfDate(status[0]) : '', testDate: normalizePdfDate(testDate), fullRecord: text }
  return buildCowPageCsv()
}
function buildCowPageCsv() {
  const draft = cowPageDraft.value
  const headers = ['BarnName', 'DHIID', 'BirthDate', 'Milk', 'Fat%', 'Pro%', 'LastCalv', 'Report Type', 'Test Date', 'Full Cow Record']
  const row = [draft.name, draft.dhiId, draft.birthDate, draft.milk, draft.fat, draft.protein, draft.lastCalving, 'PC-DART DHI-203 Individual Cow PDF', draft.testDate, draft.fullRecord]
  return [headers, row].map(values => values.map(csvCell).join(',')).join('\n')
}
async function applyCowPageCorrections() {
  csvText.value = buildCowPageCsv(); fileName.value = `COW-PAGE-${cowPageDraft.value.name || 'UNKNOWN'}::${fileName.value.split('::').at(-1)}`
  reportDate.value = cowPageDraft.value.testDate || reportDate.value; preview.value = null; mappings.value = {}; await previewImport()
}

onMounted(async () => {
  source.value = route.query.source === '2' ? 2 : 1
  const requestedType = String(route.query.type ?? '')
  if (['pcdartCsv', 'currentMilkingPdf', 'cowPagePdf', 'zoetisCsv'].includes(requestedType)) importMode.value = requestedType as ImportMode
  else importMode.value = source.value === 2 ? 'zoetisCsv' : 'pcdartCsv'
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
  cowPageDraft.value = null
  busy.value = true
  status.value = `Reading ${file.name}…`
  fileName.value = file.name
  try {
  if (file.name.toLowerCase().endsWith('.pdf')) {
    const lines = await extractPdfLines(file)
    csvText.value = importMode.value === 'cowPagePdf' ? cowPageCsv(lines) : currentMilkingCsv(lines)
    const parsedName = csvText.value.split('\n')[1]?.match(/^"([^"]+)/)?.[1] ?? 'UNKNOWN'
    fileName.value = importMode.value === 'cowPagePdf' ? `COW-PAGE-${parsedName}::${file.name}` : `CURRENT-MILKING::${file.name}`
    const flatPdf = lines.join(' ')
    const sourceDate = importMode.value === 'cowPagePdf' ? flatPdf.match(/Date of Test\s+(\d{2}\/\d{2}\/\d{4})/i)?.[1] : flatPdf.match(/Printed\s*(\d{1,2}\/\d{1,2}\/\d{4})/i)?.[1]
    if (sourceDate) reportDate.value = normalizePdfDate(sourceDate.padStart(10, '0'))
    source.value = 1
  } else {
    csvText.value = await file.text()
    source.value = file.name.toLowerCase().includes('coretraits') || importMode.value === 'zoetisCsv' ? 2 : 1
  }
  preview.value = null
  duplicateDecision.value = 'pending'
  mappings.value = {}
    await previewImport()
  } catch (error) {
    csvText.value = ''
    preview.value = null
    status.value = error instanceof Error ? error.message : 'The selected report could not be read.'
  } finally {
    busy.value = false
  }
}

function payload(confirmDuplicateReplace = false) { return { source: source.value, fileName: fileName.value, csvText: csvText.value, reportDate: reportDate.value, animalMappings: mappings.value, confirmDuplicateReplace } }
async function previewImport() {
  busy.value = true; status.value = ''
  try {
    preview.value = await previewHerdData(payload())
    for (const row of preview.value.rows) if (row.animalId) mappings.value[row.sourceKey] = row.animalId
    status.value = needsMatch.value.length === 0
      ? preview.value.duplicateImport
        ? `This ${source.value === 2 ? 'Zoetis' : 'PC-DART'} report date is already stored. Nothing needs to be added again.`
        : `${preview.value.rowsRead} rows matched. Tap Save confirmed import.`
      : `${preview.value.rowsRead} rows read. Confirm the ${needsMatch.value.length} highlighted match${needsMatch.value.length === 1 ? '' : 'es'}, then save.`
  } catch (error) { status.value = error instanceof Error ? error.message : 'Preview failed.' }
  finally { busy.value = false }
}
async function applyImport(confirmDuplicateReplace = false) {
  busy.value = true; status.value = ''
  try { await applyHerdData(payload(confirmDuplicateReplace)); analytics.value = await getHerdDataAnalytics(); status.value = confirmDuplicateReplace ? 'Duplicate reviewed. The stored report was replaced safely and only one copy remains.' : 'Import saved. Animal histories and analytics are updated.'; preview.value = null }
  catch (error) { status.value = error instanceof Error ? error.message : 'Import failed.' }
  finally { busy.value = false }
}
function declineDuplicate() {
  duplicateDecision.value = 'declined'
  preview.value = null
  csvText.value = ''
  fileName.value = ''
  mappings.value = {}
  if (fileInput.value) fileInput.value.value = ''
  status.value = 'Duplicate declined. Nothing was changed.'
}
async function acceptDuplicate() {
  duplicateDecision.value = 'accepted'
  await applyImport(true)
}
async function createAnimalFromImport(row: HerdDataPreview['rows'][number], kind: 'cow' | 'heifer' | 'bull') {
  if (row.candidates.length > 0) {
    const candidateNames = row.candidates.slice(0, 4).map(candidate => candidate.animalName).join(', ')
    const accepted = window.confirm(`Possible existing match${row.candidates.length === 1 ? '' : 'es'}: ${candidateNames}.\n\nCreate a separate ${kind} card anyway?`)
    if (!accepted) {
      status.value = 'New card declined. Choose the correct existing animal instead.'
      return
    }
  }
  creatingAnimalKey.value = row.sourceKey
  status.value = ''
  try {
    const officialId = (row.officialId || '').replace(/^HO/i, '') || null
    const isPcdart = source.value === 1
    const created = await createAnimal({
      barnName: isPcdart ? row.sourceName || null : null,
      registeredName: isPcdart ? null : row.sourceName || null,
      registrationNumber: officialId,
      birthDate: row.birthDate || null,
      sex: kind === 'bull' ? 2 : 1,
      animalStage: kind === 'bull' ? 5 : kind === 'heifer' ? 2 : 3,
      animalStatus: 0,
      breed: row.breed || null,
      notes: `Created from ${isPcdart ? 'PC-DART' : 'Zoetis'} import. Source: ${row.sourceName || row.sourceKey}.`,
      isFavorite: false
    })
    animals.value.push(created)
    mappings.value[row.sourceKey] = created.animalId
    row.animalId = created.animalId
    row.animalName = created.barnName || created.registeredName || `Animal #${created.animalId}`
    row.needsConfirmation = false
    status.value = `${row.animalName} card created as ${kind}. It is matched and ready for the confirmed import.`
  } catch (error) {
    status.value = error instanceof Error ? error.message : 'Animal card could not be created.'
  } finally {
    creatingAnimalKey.value = ''
  }
}
const needsMatch = computed(() => preview.value?.rows.filter(row => !mappings.value[row.sourceKey]) ?? [])
const filteredCombined = computed(() => (analytics.value?.combined ?? []).filter((row: any) => !combinedSearch.value || row.animalName?.toLowerCase().includes(combinedSearch.value.toLowerCase())))
const linearRows = computed(() => [...(analytics.value?.genomic ?? [])].sort((a: any, b: any) => (b.typeScore ?? -99) - (a.typeScore ?? -99)))
const genomicRanked = computed(() => [...(analytics.value?.genomic ?? [])].sort((a: any, b: any) => (b.tpi ?? -9999) - (a.tpi ?? -9999)))
const topGenomic = computed(() => genomicRanked.value.slice(0, 5))
const bottomGenomic = computed(() => genomicRanked.value.slice(-5).reverse())
const genomicHistory = computed(() => analytics.value?.genomicHistory ?? [])
const latestGenomicTrend = computed(() => { const rows = genomicHistory.value; if (rows.length < 2) return null; const current = rows.at(-1); const prior = rows.at(-2); return { current, prior, tpi: current.averageTpi - prior.averageTpi, netMerit: current.averageNetMerit - prior.averageNetMerit, type: current.averageType - prior.averageType, udder: current.averageUdder - prior.averageUdder, feetLegs: current.averageFeetLegs - prior.averageFeetLegs, fertility: current.averageFertility - prior.averageFertility } })
const genomicTraitCards = computed(() => [
  { key: 'tpi', label: 'TPI' }, { key: 'netMerit', label: 'Net Merit' }, { key: 'milkPta', label: 'Milk PTA' },
  { key: 'daughterPregnancyRate', label: 'Fertility' }, { key: 'productiveLife', label: 'Productive Life' },
  { key: 'typeScore', label: 'Type' }, { key: 'udderComposite', label: 'UDC' }, { key: 'rearUdderHeight', label: 'Rear Udder Height' },
  { key: 'rearUdderWidth', label: 'Rear Udder Width' }, { key: 'strength', label: 'Strength' }, { key: 'feetLegsComposite', label: 'Feet & Legs' }
].map(trait => { const ranked = [...genomicRanked.value].filter((row: any) => row[trait.key] != null).sort((a: any, b: any) => b[trait.key] - a[trait.key]); const values = ranked.map((row: any) => Number(row[trait.key])); return { ...trait, average: values.length ? values.reduce((sum: number, value: number) => sum + value, 0) / values.length : null, strongest: ranked[0], weakest: ranked.at(-1) } }))
const featuredTraitBoards = computed(() => genomicTraitCards.value.filter(trait => ['typeScore', 'udderComposite', 'rearUdderHeight', 'rearUdderWidth', 'strength'].includes(trait.key)).map(trait => ({ ...trait, leaders: [...genomicRanked.value].filter((row: any) => row[trait.key] != null).sort((a: any, b: any) => b[trait.key] - a[trait.key]).slice(0, 5) })))
const linearTraits = [
  { key: 'typeScore', label: 'Type' }, { key: 'udderComposite', label: 'Udder' },
  { key: 'feetLegsComposite', label: 'Feet & Legs' }, { key: 'daughterPregnancyRate', label: 'Fertility' },
  { key: 'productiveLife', label: 'Productive Life' }
]
function linearWidth(value: unknown) { const number = Number(value); return Number.isFinite(number) ? `${Math.max(4, Math.min(100, 50 + number * 12))}%` : '0%' }
function herdAverage(key: string) { const values = linearRows.value.map((row: any) => Number(row[key])).filter(Number.isFinite); return values.length ? (values.reduce((sum: number, value: number) => sum + value, 0) / values.length).toFixed(2) : '—' }
</script>

<template>
  <main class="data-page">
    <header><button @click="router.push('/reports')">← Reports</button><h1>Herd Analytics</h1><p>Milk, genomics, whole-farm linear comparisons, and combined decisions.</p></header>
    <nav class="analytics-tabs" aria-label="Analytics sections">
      <button :class="{ active: activeView === 'attention' }" @click="selectView('attention')">Attention Lists</button>
      <button :class="{ active: activeView === 'milk' }" @click="selectView('milk')">Milk</button>
      <button :class="{ active: activeView === 'genomics' }" @click="selectView('genomics')">Genomics</button>
      <button :class="{ active: activeView === 'bulls' }" @click="selectView('bulls')">Bulls</button>
      <button :class="{ active: activeView === 'linear' }" @click="selectView('linear')">Farm Linear</button>
      <button :class="{ active: activeView === 'combined' }" @click="selectView('combined')">Combined</button>
      <button :class="{ active: activeView === 'imports' }" @click="selectView('imports')">Imports</button>
    </nav>
      <section v-if="activeView === 'attention'" class="attention-grid">
        <article class="card attention-card"><h2>High-DIM Open Cows <b>{{ analytics?.attention?.highDimOpen?.length ?? 0 }}</b></h2><p>Milking cows at 200+ DIM without a confirmed pregnancy.</p><button v-for="row in analytics?.attention?.highDimOpen ?? []" :key="row.animalId" class="attention-row" @click="router.push(`/animals/${row.animalId}`)"><strong>{{ row.animalName }}</strong><span>{{ row.daysInMilk }} DIM · {{ row.milk ?? '—' }} milk</span></button><small v-if="!(analytics?.attention?.highDimOpen?.length)">Nobody currently meets this rule.</small></article>
        <article class="card attention-card"><h2>Long-Open Heifers <b>{{ analytics?.attention?.longOpenHeifers?.length ?? 0 }}</b></h2><p>Heifers 15+ months old without a confirmed pregnancy.</p><button v-for="row in analytics?.attention?.longOpenHeifers ?? []" :key="row.animalId" class="attention-row" @click="router.push(`/animals/${row.animalId}`)"><strong>{{ row.animalName }}</strong><span>{{ row.ageMonths }} months · {{ row.lastBred ? `bred ${new Date(row.lastBred).toLocaleDateString()}` : 'no breeding stored' }}</span></button><small v-if="!(analytics?.attention?.longOpenHeifers?.length)">Nobody currently meets this rule.</small></article>
        <article class="card attention-card"><h2>Dropping in Milk <b>{{ analytics?.attention?.droppingMilk?.length ?? 0 }}</b></h2><p>Latest PC-DART test fell at least 10% from the previous test.</p><button v-for="row in analytics?.attention?.droppingMilk ?? []" :key="row.animalId" class="attention-row" @click="router.push(`/animals/${row.animalId}`)"><strong>{{ row.animalName }}</strong><span>{{ row.previousMilk }} → {{ row.currentMilk }} · down {{ row.dropPercent }}%</span></button><small v-if="!(analytics?.attention?.droppingMilk?.length)">No 10% drops found in the latest two tests.</small></article>
        <article class="card attention-card"><h2>Pregnant — Dry-Off Watch <b>{{ analytics?.attention?.dryOffWatch?.length ?? 0 }}</b></h2><p>Recommended dry date is due or within the next 60 days.</p><button v-for="row in analytics?.attention?.dryOffWatch ?? []" :key="row.animalId" class="attention-row" @click="router.push(`/animals/${row.animalId}`)"><strong>{{ row.animalName }}</strong><span>{{ row.daysUntilDry < 0 ? `${Math.abs(row.daysUntilDry)} days overdue` : `${row.daysUntilDry} days` }} · dry {{ new Date(row.recommendedDryOffDate).toLocaleDateString() }}</span></button><small v-if="!(analytics?.attention?.dryOffWatch?.length)">No confirmed pregnant cows are inside the 60-day window.</small></article>
      </section>
    <div v-show="activeView === 'imports'" class="import-choice">
      <button type="button" :class="{ active: importMode === 'pcdartCsv' }" @click="chooseImportMode('pcdartCsv')">PC-DART Milk CSV</button>
      <button type="button" :class="{ active: importMode === 'currentMilkingPdf' }" @click="chooseImportMode('currentMilkingPdf')">Current Milking PDF</button>
      <button type="button" :class="{ active: importMode === 'cowPagePdf' }" @click="chooseImportMode('cowPagePdf')">Individual Cow PDF</button>
      <button type="button" :class="{ active: importMode === 'zoetisCsv' }" @click="chooseImportMode('zoetisCsv')">Zoetis Genomics CSV</button>
    </div>
      <details v-show="activeView === 'imports'" ref="importDetails" class="card import-card" open>
        <summary>Import report</summary>
        <p class="import-instruction">{{ importMode === 'currentMilkingPdf' ? 'Choose the PC-DART 005 Production - Milking Cows PDF. Every cow will be audited before saving.' : importMode === 'cowPagePdf' ? 'Choose a PC-DART DHI-203 individual Cow Page PDF. Its complete extracted record will be stored with the matched animal.' : source === 2 ? 'Choose your Zoetis Core Traits CSV, then preview the animal matches.' : 'Choose your PC-DART CSV, then preview the animal matches.' }}</p>
        <label class="choose-file" for="herd-data-file">Choose {{ importMode === 'currentMilkingPdf' ? 'Current Milking PDF' : importMode === 'cowPagePdf' ? 'Individual Cow PDF' : source === 2 ? 'Zoetis Genomics CSV' : 'PC-DART Milk CSV' }}</label>
        <input id="herd-data-file" ref="fileInput" class="file-input" type="file" :accept="importMode === 'currentMilkingPdf' || importMode === 'cowPagePdf' ? '.pdf,application/pdf' : '.csv,text/csv'" @change="loadFile">
        <div class="controls"><select v-model.number="source"><option :value="1">PC-DART milk report</option><option :value="2">Zoetis genomic report</option></select><input v-model="reportDate" type="date"><strong class="selected-file">{{ fileName || 'No file selected yet' }}</strong></div>
        <div class="actions"><button :disabled="busy || !csvText" @click="previewImport">Preview & match</button><button :disabled="busy || !preview || preview.duplicateImport || needsMatch.length > 0" @click="applyImport(false)">Save confirmed import</button></div>
        <p v-if="status" :class="{ error: status.includes('failed') || status.includes('required') }">{{ status }}</p>
        <section v-if="importMode === 'cowPagePdf' && cowPageDraft" class="cow-page-audit"><h3>Review this cow page before saving</h3><p>Correct anything PC-DART shortened or the PDF reader interpreted incorrectly, then recheck the animal match.</p><div><label>Cow name<input v-model="cowPageDraft.name"></label><label>DHI ID<input v-model="cowPageDraft.dhiId"></label><label>Birth date<input v-model="cowPageDraft.birthDate" type="date"></label><label>Test date<input v-model="cowPageDraft.testDate" type="date"></label><label>Last calving<input v-model="cowPageDraft.lastCalving" type="date"></label><label>Milk<input v-model="cowPageDraft.milk" inputmode="decimal"></label><label>Fat %<input v-model="cowPageDraft.fat" inputmode="decimal"></label><label>Protein %<input v-model="cowPageDraft.protein" inputmode="decimal"></label></div><button type="button" :disabled="busy" @click="applyCowPageCorrections">Apply corrections &amp; recheck match</button></section>
        <section v-if="preview?.duplicateImport" class="duplicate-warning">
          <strong>Possible duplicate found — no changes made yet</strong>
          <p>{{ preview.exactDuplicateFile ? 'This is the exact same file already stored.' : 'A report from this source and report date is already stored.' }}</p>
          <dl>
            <div><dt>Already stored file</dt><dd>{{ preview.existingFileName || 'Unknown filename' }}</dd></div>
            <div><dt>Rows already stored</dt><dd>{{ preview.existingRows ?? 'Unknown' }}</dd></div>
            <div><dt>Imported</dt><dd>{{ preview.existingImportedAt ? new Date(preview.existingImportedAt).toLocaleString() : 'Unknown' }}</dd></div>
            <div><dt>New file</dt><dd>{{ fileName }} · {{ preview.rowsRead }} rows</dd></div>
          </dl>
          <div class="duplicate-actions">
            <button type="button" class="decline" :disabled="busy" @click="declineDuplicate">Decline — keep existing</button>
            <button type="button" class="accept" :disabled="busy || needsMatch.length > 0" @click="acceptDuplicate">Accept — replace with reviewed file</button>
          </div>
          <small>Accept replaces the stored report; it does not create a second copy.</small>
        </section>
        <div v-if="preview" class="match-list"><p><strong>{{ preview.rowsRead }}</strong> rows · {{ needsMatch.length }} need confirmation</p><div v-for="row in preview.rows" :key="row.sourceKey" class="match-row" :class="{ unmatched: !mappings[row.sourceKey] }"><span>{{ row.sourceName }} <small>{{ row.officialId }}<template v-if="row.birthDate"> · Born {{ row.birthDate }}</template><template v-if="row.breed"> · {{ row.breed }}</template></small></span><select v-model.number="mappings[row.sourceKey]"><option :value="0">Choose existing animal…</option><option v-for="candidate in row.candidates" :key="candidate.animalId" :value="candidate.animalId">{{ candidate.animalName }} · {{ candidate.registrationNumber }}</option><option v-for="animal in animals" :key="`all-${animal.animalId}`" :value="animal.animalId">{{ animal.barnName || animal.registeredName || `#${animal.animalId}` }}</option></select><div v-if="!mappings[row.sourceKey]" class="create-animal-actions"><strong>Not in herd? Create card:</strong><button type="button" :disabled="creatingAnimalKey === row.sourceKey" @click="createAnimalFromImport(row, 'cow')">Cow</button><button type="button" :disabled="creatingAnimalKey === row.sourceKey" @click="createAnimalFromImport(row, 'heifer')">Heifer</button><button type="button" :disabled="creatingAnimalKey === row.sourceKey" @click="createAnimalFromImport(row, 'bull')">Bull</button></div></div></div>
      </details>
      <details v-if="activeView === 'milk'" class="card" open><summary>Milk Analytics</summary><div class="table-wrap"><table><thead><tr><th>Animal</th><th>Milk</th><th>DIM</th><th>Fat %</th><th>Protein %</th></tr></thead><tbody><tr v-for="row in analytics?.milk ?? []" :key="row.animalId"><td><button class="link" @click="router.push(`/animals/${row.animalId}`)">{{ row.animalName }}</button></td><td>{{ row.milk }}</td><td>{{ row.daysInMilk }}</td><td>{{ row.fatPercent }}</td><td>{{ row.proteinPercent }}</td></tr></tbody></table></div></details>
      <section v-if="activeView === 'genomics'" class="genomic-dashboard"><div class="genomic-score-strip"><article><small>Animals</small><strong>{{ genomicRanked.length }}</strong></article><article><small>Reports stored</small><strong>{{ genomicHistory.length }}</strong></article><article><small>Latest herd TPI change</small><strong :class="{ positive: (latestGenomicTrend?.tpi ?? 0) >= 0, negative: (latestGenomicTrend?.tpi ?? 0) < 0 }">{{ latestGenomicTrend ? `${latestGenomicTrend.tpi >= 0 ? '+' : ''}${latestGenomicTrend.tpi.toFixed(1)}` : 'Need 2 reports' }}</strong></article></div><section class="card"><h2>Top Type &amp; Udder Trait Leaders</h2><div class="featured-traits"><article v-for="trait in featuredTraitBoards" :key="trait.key"><h3>{{ trait.label }}</h3><p v-if="!trait.leaders.length">Not included in the current Zoetis export.</p><button v-for="(row, index) in trait.leaders" :key="row.animalId" @click="router.push(`/animals/${row.animalId}`)"><b>#{{ index + 1 }} {{ row.animalName }}</b><span>{{ row[trait.key] }}</span></button></article></div></section><div class="ranking-grid"><article class="card rank-card"><h2>Top Genomic Animals</h2><button v-for="(row, index) in topGenomic" :key="row.animalId" @click="router.push(`/animals/${row.animalId}`)"><b>#{{ index + 1 }} {{ row.animalName }}</b><span>TPI {{ row.tpi ?? '—' }} · NM$ {{ row.netMerit ?? '—' }}</span></button></article><article class="card rank-card bottom"><h2>Bottom Genomic Animals</h2><p>Review weaknesses and mating fit; low rank does not automatically mean sell.</p><button v-for="row in bottomGenomic" :key="row.animalId" @click="router.push(`/animals/${row.animalId}`)"><b>{{ row.animalName }}</b><span>TPI {{ row.tpi ?? '—' }} · NM$ {{ row.netMerit ?? '—' }}</span></button></article></div><section class="card"><h2>Herd Baselines, Strengths &amp; Weaknesses</h2><div class="trait-card-grid"><article v-for="trait in genomicTraitCards" :key="trait.key"><small>{{ trait.label }} baseline</small><strong>{{ trait.average?.toFixed(1) ?? '—' }}</strong><span class="strength">Strongest: <button @click="router.push(`/animals/${trait.strongest?.animalId}`)">{{ trait.strongest?.animalName ?? '—' }} {{ trait.strongest?.[trait.key] ?? '' }}</button></span><span class="weakness">Needs improvement: <button @click="router.push(`/animals/${trait.weakest?.animalId}`)">{{ trait.weakest?.animalName ?? '—' }} {{ trait.weakest?.[trait.key] ?? '' }}</button></span></article></div></section><section class="card"><h2>Herd Genomic Trends</h2><p v-if="genomicHistory.length < 2">Import a future Zoetis report to establish improvement trends against this baseline.</p><div v-else class="trend-grid"><article v-for="metric in [{label:'TPI',value:latestGenomicTrend?.tpi},{label:'Net Merit',value:latestGenomicTrend?.netMerit},{label:'Type',value:latestGenomicTrend?.type},{label:'Udder',value:latestGenomicTrend?.udder},{label:'Feet & Legs',value:latestGenomicTrend?.feetLegs},{label:'Fertility',value:latestGenomicTrend?.fertility}]" :key="metric.label"><small>{{ metric.label }}</small><strong :class="{ positive: (metric.value ?? 0) >= 0, negative: (metric.value ?? 0) < 0 }">{{ metric.value == null ? '—' : `${metric.value >= 0 ? '+' : ''}${metric.value.toFixed(2)}` }}</strong></article></div></section><details class="card"><summary>Full Genomic Table</summary><div class="table-wrap"><table><thead><tr><th>Animal</th><th>TPI</th><th>NM$</th><th>Milk PTA</th><th>DPR</th><th>PL</th><th>Type</th><th>UDC</th><th>RUH</th><th>RUW</th><th>Strength</th><th>FLC</th></tr></thead><tbody><tr v-for="row in genomicRanked" :key="row.animalId"><td><button class="link" @click="router.push(`/animals/${row.animalId}`)">{{ row.animalName }}</button></td><td>{{ row.tpi }}</td><td>{{ row.netMerit }}</td><td>{{ row.milkPta }}</td><td>{{ row.daughterPregnancyRate }}</td><td>{{ row.productiveLife }}</td><td>{{ row.typeScore }}</td><td>{{ row.udderComposite }}</td><td>{{ row.rearUdderHeight ?? '—' }}</td><td>{{ row.rearUdderWidth ?? '—' }}</td><td>{{ row.strength ?? '—' }}</td><td>{{ row.feetLegsComposite }}</td></tr></tbody></table></div></details></section>
      <section v-if="activeView === 'bulls'" class="card"><h2>Bull Genomic Proofs</h2><p>Bulls stay separate from cow rankings. Open a bull card to review his proof; open a cow card for sire matches based on what that cow needs improved.</p><div v-if="!(analytics?.bulls?.length)" class="empty-note">No imported bull genomic cards yet. During Zoetis matching, choose “Bull” to create one.</div><div class="table-wrap" v-else><table><thead><tr><th>Bull</th><th>TPI</th><th>NM$</th><th>Milk PTA</th><th>DPR</th><th>PL</th><th>Type</th><th>UDC</th><th>FLC</th></tr></thead><tbody><tr v-for="row in analytics.bulls" :key="row.animalId"><td><button class="link" @click="router.push(`/animals/${row.animalId}`)">{{ row.animalName }}</button></td><td>{{ row.tpi }}</td><td>{{ row.netMerit }}</td><td>{{ row.milkPta }}</td><td>{{ row.daughterPregnancyRate }}</td><td>{{ row.productiveLife }}</td><td>{{ row.typeScore }}</td><td>{{ row.udderComposite }}</td><td>{{ row.feetLegsComposite }}</td></tr></tbody></table></div></section>
      <section v-if="activeView === 'linear'" class="card"><h2>Whole-Farm Linear</h2><p>Compare stored genomic composites across the farm. Tap an animal for her own linear and mating view.</p><div class="herd-averages"><span v-for="trait in linearTraits" :key="trait.key"><small>Herd average</small><strong>{{ trait.label }} {{ herdAverage(trait.key) }}</strong></span></div><div class="linear-grid"><article v-for="row in linearRows" :key="row.animalId" class="linear-card"><button class="linear-name" @click="router.push(`/animals/${row.animalId}`)">{{ row.animalName }}</button><div v-for="trait in linearTraits" :key="trait.key" class="linear-trait"><span>{{ trait.label }}</span><div><i :style="{ width: linearWidth(row[trait.key]) }"></i></div><strong>{{ row[trait.key] ?? '—' }}</strong></div></article></div></section>
      <details v-if="activeView === 'combined'" class="card" open><summary>Combined Sale &amp; Breeding Review</summary><input v-model="combinedSearch" type="search" placeholder="Search animal"><div class="table-wrap"><table><thead><tr><th>Animal</th><th>Actual milk</th><th>DIM</th><th>TPI</th><th>NM$</th><th>Milk PTA</th><th>DPR</th><th>PL</th><th>Type</th><th>UDC</th><th>FLC</th></tr></thead><tbody><tr v-for="row in filteredCombined" :key="row.animalId"><td><button class="link" @click="router.push(`/animals/${row.animalId}`)">{{ row.animalName }}</button></td><td>{{ row.milk }}</td><td>{{ row.daysInMilk }}</td><td>{{ row.tpi }}</td><td>{{ row.netMerit }}</td><td>{{ row.milkPta }}</td><td>{{ row.daughterPregnancyRate }}</td><td>{{ row.productiveLife }}</td><td>{{ row.typeScore }}</td><td>{{ row.udderComposite }}</td><td>{{ row.feetLegsComposite }}</td></tr></tbody></table></div></details>
  </main>
</template>

<style scoped>
.import-choice{display:grid;grid-template-columns:repeat(2,1fr);gap:10px;margin:14px 0}.import-choice button{min-height:52px;border:2px solid #31572c;border-radius:9px;background:#fff;color:#31572c;font-weight:900}.import-choice button.active{background:#31572c;color:#fff}
.analytics-tabs{display:grid;grid-template-columns:repeat(3,1fr);gap:8px;margin:14px 0}.analytics-tabs button{min-height:48px;border:1px solid #31572c;border-radius:8px;background:#fff;color:#31572c;font-weight:900}.analytics-tabs button.active{background:#31572c;color:#fff}.herd-averages{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0}.herd-averages span{display:grid;padding:8px 10px;background:#eef5ef;border-radius:8px}.herd-averages small{color:#64746a}.linear-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(290px,1fr));gap:10px}.linear-card{padding:12px;border:1px solid #d8e2da;border-radius:10px}.linear-name{background:transparent!important;color:#173422;padding:0!important;font-size:1rem}.linear-trait{display:grid;grid-template-columns:92px 1fr 42px;gap:7px;align-items:center;margin-top:8px;font-size:.78rem}.linear-trait>div{height:10px;background:#e6ece7;border-radius:10px;overflow:hidden}.linear-trait i{display:block;height:100%;background:#4f772d;border-radius:10px}.linear-trait strong{text-align:right}
.attention-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.attention-card{margin:0}.attention-card h2{display:flex;justify-content:space-between;gap:8px;margin:0}.attention-card h2 b{display:grid;place-items:center;min-width:34px;height:34px;border-radius:50%;background:#d8202d;color:#fff}.attention-card>p{color:#64746a}.attention-row{display:flex!important;justify-content:space-between;align-items:center;width:100%;margin-top:7px;border:1px solid #d8e2da!important;background:#fff!important;color:#173422!important;text-align:left}.attention-row span{font-size:.78rem;color:#64746a}.attention-card>small{display:block;padding:14px;color:#64746a;text-align:center}
.genomic-score-strip{display:grid;grid-template-columns:repeat(3,1fr);gap:9px}.genomic-score-strip article{display:grid;padding:13px;background:#173422;color:#fff;border-radius:9px}.genomic-score-strip small{color:#b9d3c0}.genomic-score-strip strong{font-size:1.45rem}.ranking-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}.rank-card{margin:12px 0 0}.rank-card>button{display:flex;justify-content:space-between;width:100%;margin-top:6px;background:#f5f9f5;color:#173422}.rank-card.bottom{border-top:4px solid #c2410c}.rank-card.bottom>button{background:#fff7ed}.trait-card-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px}.trait-card-grid article{display:grid;gap:5px;padding:11px;border:1px solid #d8e2da;border-radius:8px}.trait-card-grid article>strong{font-size:1.3rem}.trait-card-grid span{font-size:.72rem}.trait-card-grid button{min-height:0;padding:0;background:transparent;color:inherit}.strength{color:#166534}.weakness{color:#9a3412}.trend-grid{display:grid;grid-template-columns:repeat(6,1fr);gap:8px}.trend-grid article{display:grid;padding:10px;background:#f3f5f3;border-radius:7px}.positive{color:#15803d!important}.negative{color:#b91c1c!important}.empty-note{padding:20px;text-align:center;color:#64746a;background:#f5f7f5}
.featured-traits{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:8px}.featured-traits article{padding:9px;border:1px solid #d8e2da;border-radius:8px}.featured-traits h3{margin:0 0 6px}.featured-traits p{font-size:.74rem;color:#9a3412}.featured-traits button{display:flex;justify-content:space-between;width:100%;min-height:36px;margin-top:4px;padding:5px 7px;background:#f5f9f5;color:#173422;font-size:.72rem}
.import-instruction{font-weight:750;color:#31572c}.choose-file{width:100%;min-height:56px;background:#31572c;color:#fff;font-size:1rem;display:grid;place-items:center;border-radius:8px;font-weight:900;cursor:pointer;box-sizing:border-box}.file-input{width:100%;min-height:48px;margin:8px 0;padding:7px;border:1px solid #bdcbbf;border-radius:8px;box-sizing:border-box}.selected-file{display:flex;align-items:center;min-height:44px;padding:0 10px;border:1px solid #bdcbbf;border-radius:7px;box-sizing:border-box;font-size:.85rem}
.duplicate-warning{margin-top:14px;border:2px solid #b45309;border-radius:10px;background:#fff7ed;padding:14px;color:#431407}.duplicate-warning>strong{font-size:1.05rem}.duplicate-warning dl{display:grid;grid-template-columns:1fr 1fr;gap:8px}.duplicate-warning dl div{border:1px solid #fed7aa;border-radius:7px;background:#fff;padding:8px}.duplicate-warning dt{font-size:.72rem;font-weight:900;text-transform:uppercase;color:#9a3412}.duplicate-warning dd{margin:3px 0 0;font-weight:750}.duplicate-actions{display:grid;grid-template-columns:1fr 1fr;gap:8px}.duplicate-actions .decline{background:#fff;color:#7c2d12;border:1px solid #9a3412}.duplicate-actions .accept{background:#9a3412;color:#fff}.duplicate-warning small{display:block;margin-top:8px;font-weight:750}
.cow-page-audit{margin-top:14px;padding:13px;border:2px solid #31572c;border-radius:10px;background:#f2f8f3}.cow-page-audit h3{margin:0}.cow-page-audit p{color:#64746a}.cow-page-audit>div{display:grid;grid-template-columns:repeat(4,1fr);gap:8px}.cow-page-audit label{display:grid;gap:4px;font-size:.75rem;font-weight:850}.cow-page-audit input{min-width:0;min-height:42px;padding:7px;border:1px solid #bdcbbf;border-radius:7px}.cow-page-audit>button{margin-top:10px;background:#31572c;color:#fff}
.data-page{max-width:1240px;margin:auto;padding:16px;background:#f5f7f2;min-height:100vh}header{padding:20px;border-radius:12px;background:#173422;color:#fff}header button,.card button{min-height:44px;border:0;border-radius:7px;padding:0 14px;font-weight:850}.card{margin:14px 0;padding:16px;border:1px solid #d8e2da;border-radius:12px;background:#fff}.card summary{cursor:pointer;font-size:1.2rem;font-weight:900;min-height:34px}.card[open] summary{margin-bottom:14px}.controls input,.controls select,.card>input,.match-list select{min-height:44px;border:1px solid #bdcbbf;border-radius:7px;padding:8px;width:100%;box-sizing:border-box}.controls{display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px}.actions{display:flex;gap:8px;margin-top:12px}.actions button{background:#31572c;color:#fff}.match-list{display:grid;gap:8px;margin-top:14px;max-height:620px;overflow:auto}.match-row{display:grid;grid-template-columns:1fr 1.3fr;gap:10px;align-items:center;padding:9px;border:1px solid #e0e7e1;border-radius:8px}.match-row.unmatched{border:2px solid #b45309;background:#fffaf2}.match-list span{display:grid;font-weight:800}.match-list small{font-weight:400;color:#64746a}.create-animal-actions{grid-column:1/-1;display:grid;grid-template-columns:1fr repeat(3,minmax(82px,.45fr));gap:7px;align-items:center}.create-animal-actions button{background:#31572c;color:#fff}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse;min-width:720px}th,td{padding:9px;border-bottom:1px solid #e1e7e2;text-align:left}th{background:#eef5ef}.link{background:transparent!important;color:#31572c;padding:0!important}.error{color:#991b1b}@media(max-width:640px){.analytics-tabs{grid-template-columns:repeat(2,1fr)}.analytics-tabs button:last-child{grid-column:1/-1}.controls,.match-row,.duplicate-warning dl,.duplicate-actions,.create-animal-actions{grid-template-columns:1fr}.actions{display:grid}.actions button{width:100%}.data-page{padding:8px}.card{padding:12px}}
@media(max-width:640px){.attention-grid{grid-template-columns:1fr}.attention-row{align-items:flex-start;flex-direction:column}.analytics-tabs button:last-child{grid-column:auto}}
@media(max-width:700px){.ranking-grid,.genomic-score-strip{grid-template-columns:1fr}.featured-traits{grid-template-columns:1fr}.trait-card-grid{grid-template-columns:repeat(2,1fr)}.trend-grid{grid-template-columns:repeat(2,1fr)}.cow-page-audit>div{grid-template-columns:1fr 1fr}}
</style>
