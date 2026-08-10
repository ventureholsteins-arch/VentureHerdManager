<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getPrintReports } from '../api/printReports'
import HerdLoadingScene from '../components/HerdLoadingScene.vue'
import RetroIcon from '../components/RetroIcon.vue'
import { easyIdPreparationUrl } from '../api/sires'

const router = useRouter()
const data = ref<any>(null)
const loading = ref(true)
const error = ref('')
const report = ref('missingRegistration')

const options = [
  ['missingRegistration', 'Missing registration numbers'],
  ['missingAnimalIdentification', 'Missing barn names or registration numbers'],
  ['oldEnoughNotBred', '7 months+ and not bred'],
  ['milkingNotBred', 'Milking cows not bred'],
  ['sellAnimals', 'My sale report'],
  ['suggestedSell', 'Suggested sale review'],
  ['pregnancyChecksDue', 'All pregnancy checks due'],
  ['animals', 'All active animals'],
  ['calves', 'Calves'],
  ['heifers', 'Heifers'],
  ['cows', 'Milking and dry cows'],
  ['dueWithinEightMonths', 'Due within 8 months'],
  ['heiferPregChecks', 'Heifer pregnancy checks due'],
  ['cowPregChecks', 'Cow pregnancy checks due'],
  ['lastMonthHeats', 'Heats in the last month'],
  ['implants', 'All embryo implants'],
  ['availableEmbryos', 'Available embryo inventory'],
  ['breedings', 'All breedings'],
  ['siresUsed', 'Sires used'],
  ['embryos', 'Embryo inventory & statistics']
]

const rows = computed(() => {
  if (!data.value) return []
  if (report.value === 'sellAnimals') {
    let ids: number[] = []
    try {
      const lists = JSON.parse(localStorage.getItem('venture-herd-lists-v2') || '[]') as Array<{ key: string; animalIds: number[] }>
      ids = lists.find(list => list.key === 'sale-animals')?.animalIds ?? []
    } catch { ids = [] }
    return data.value.animals.filter((animal: any) => ids.includes(animal.animalId))
  }
  if (report.value === 'suggestedSell') return data.value.suggestedSell ?? []
  if (report.value === 'calves') return data.value.animals.filter((a: any) => a.animalStage === 1)
  if (report.value === 'heifers') return data.value.animals.filter((a: any) => a.animalStage === 2)
  if (report.value === 'cows') return data.value.animals.filter((a: any) => [3, 4].includes(a.animalStage))
  return data.value[report.value] ?? []
})

const title = computed(() => options.find(x => x[0] === report.value)?.[1] ?? 'Report')
const isAnimalReport = computed(() => [
  'missingRegistration',
  'missingAnimalIdentification',
  'oldEnoughNotBred',
  'milkingNotBred',
  'sellAnimals',
  'animals',
  'calves',
  'heifers',
  'cows'
].includes(report.value))
const isSireReport = computed(() => report.value === 'siresUsed')
const fmt = (value: string | null) => value ? new Date(value).toLocaleDateString() : '—'
const printReport = () => window.print()

onMounted(async () => {
  try { data.value = await getPrintReports() }
  catch { error.value = 'Reports are temporarily unavailable.' }
  finally { loading.value = false }
})
</script>

<template>
  <main class="print-page">
    <header class="report-toolbar no-print">
      <button @click="router.push('/reports')">← Reports</button>
      <label>Report<select v-model="report"><option v-for="item in options" :key="item[0]" :value="item[0]">{{ item[1] }}</option></select></label>
      <a class="export-link" :href="easyIdPreparationUrl" download>Registration prep CSV</a>
      <button class="print-button" @click="printReport"><RetroIcon name="reports" :size="24" /> Print Report</button>
    </header>
    <HerdLoadingScene v-if="loading" message="Preparing printable reports..." />
    <p v-else-if="error">{{ error }}</p>
    <article v-else class="paper">
      <header><h1>{{ title }}</h1><p>Venture Herd Manager · {{ new Date(data.generatedAt).toLocaleString() }}</p></header>
      <div v-if="report === 'embryos'" class="stats">
        <span>Total <b>{{ data.embryoStatistics.total }}</b></span><span>Stored <b>{{ data.embryoStatistics.inStorage }}</b></span>
        <span>Implanted <b>{{ data.embryoStatistics.implanted }}</b></span><span>Successful <b>{{ data.embryoStatistics.successful }}</b></span><span>Failed <b>{{ data.embryoStatistics.failed }}</b></span>
      </div>
      <p v-if="report === 'suggestedSell'" class="decision-note">Decision aid only: review the reasons and keep-strengths before making a sale decision. The score becomes more useful after both PC-DART and Zoetis imports are matched.</p>
      <table>
        <thead><tr v-if="report === 'suggestedSell'"><th>Rank</th><th>Cow</th><th>Milk / DIM</th><th>Reproduction</th><th>Genomics</th><th>Why review</th><th>Reasons to keep</th></tr>
        <tr v-else-if="isAnimalReport"><th>Animal</th><th>Registered name</th><th>Registration #</th><th>Birth date</th><th>Sire</th><th>Dam</th></tr>
        <tr v-else-if="isSireReport"><th>Sire</th><th>Animals</th><th>Breedings</th><th>Pregnant</th><th>Open</th><th>To check</th><th>Last used</th></tr>
        <tr v-else-if="report === 'lastMonthHeats'"><th>Animal</th><th>Heat date</th><th>Notes</th></tr>
        <tr v-else-if="['dueWithinEightMonths','breedings','pregnancyChecksDue','heiferPregChecks','cowPregChecks'].includes(report)"><th>Animal</th><th>Bred</th><th>Sire</th><th>Due / Check</th><th>Status</th><th>Working notes</th></tr>
        <tr v-else><th>Code</th><th>Donor × Sire</th><th>Grade</th><th>Recipient</th><th>Implant date</th><th>Status</th></tr></thead>
        <tbody>
          <tr v-for="row in rows" :key="row.animalId ?? row.heatEventId ?? row.breedingEventId ?? row.embryoRecordId ?? row.sire">
            <template v-if="report === 'suggestedSell'"><td><strong>{{ row.score }}</strong><br><small>{{ row.reviewLevel }}</small></td><td>{{ row.barnName || row.registeredName || `Animal #${row.animalId}` }}</td><td>{{ row.milk ?? 'Missing' }}<br><small>DIM {{ row.daysInMilk ?? '—' }}</small></td><td>{{ row.reproStatus }}</td><td>NM$ {{ row.netMerit ?? '—' }}<br><small>TPI {{ row.tpi ?? '—' }}</small></td><td>{{ row.concerns.join(' · ') || 'No major concerns' }}</td><td>{{ row.strengths.join(' · ') || 'No recorded strengths yet' }}</td></template>
            <template v-else-if="isAnimalReport"><td>{{ row.barnName || row.registeredName || [row.damName, row.sireName].filter(Boolean).join(' × ') || `Animal #${row.animalId}` }}</td><td>{{ row.registeredName || '—' }}</td><td>{{ row.registrationNumber || 'MISSING' }}</td><td>{{ fmt(row.birthDate) }}</td><td>{{ row.sireName || '—' }}</td><td>{{ row.damName || '—' }}</td></template>
            <template v-else-if="isSireReport"><td>{{ row.sire }}</td><td>{{ row.animals }}</td><td>{{ row.breedings }}</td><td>{{ row.pregnant }}</td><td>{{ row.open }}</td><td>{{ row.toCheck }}</td><td>{{ fmt(row.lastUsed) }}</td></template>
            <template v-else-if="report === 'lastMonthHeats'"><td>{{ row.animalName }}</td><td>{{ fmt(row.heatDateTime) }}</td><td>{{ row.notes || '—' }}</td></template>
            <template v-else-if="['dueWithinEightMonths','breedings','pregnancyChecksDue','heiferPregChecks','cowPregChecks'].includes(report)"><td>{{ row.animalName }}</td><td>{{ fmt(row.breedingDate) }}</td><td>{{ row.sireUsed }}</td><td>{{ fmt(row.expectedDueDate || row.pregnancyCheckDueDate) }}</td><td>{{ row.pregnancyStatus }}</td><td class="write-field"></td></template>
            <template v-else><td>{{ row.code || `#${row.embryoRecordId}` }}</td><td>{{ row.donor || '—' }} × {{ row.sire || '—' }}</td><td>{{ row.grade || '—' }}</td><td>{{ row.recipientName || '—' }}</td><td>{{ fmt(row.implantDate) }}</td><td>{{ row.status }}</td></template>
          </tr>
          <tr v-if="rows.length === 0"><td colspan="6">No records in this report.</td></tr>
        </tbody>
      </table>
    </article>
  </main>
</template>

<style scoped>
.print-page{max-width:1100px;margin:auto;padding:18px}.report-toolbar{display:flex;gap:12px;align-items:end;flex-wrap:wrap;margin-bottom:18px}.report-toolbar label{display:grid;gap:5px;flex:1;min-width:220px}.report-toolbar select,.report-toolbar button,.export-link{min-height:44px;padding:8px 12px}.export-link{display:flex;align-items:center;border:1px solid #31572c;border-radius:3px;color:#31572c;text-decoration:none}.print-button{display:flex;align-items:center;gap:8px}.paper{background:#fff;color:#111;padding:28px;border:1px solid #ccd5ce}.paper header{border-bottom:3px solid #31572c;margin-bottom:18px}.paper h1{margin:0 0 4px}.stats{display:flex;gap:18px;flex-wrap:wrap;margin-bottom:18px}.stats span{border:1px solid #bbb;padding:8px 12px}table{width:100%;border-collapse:collapse;font-size:13px}th,td{text-align:left;padding:8px;border-bottom:1px solid #ccc;vertical-align:top}th{background:#eef4ef}.write-field{min-width:140px;height:34px}
.decision-note{border:1px solid #d2a829;background:#fff9df;padding:10px 12px;font-weight:700}
@media(max-width:600px){.print-page{padding:8px}.paper{padding:14px;overflow-x:auto}.report-toolbar>*{width:100%}table{min-width:720px}}
@media print{.no-print{display:none!important}.print-page{max-width:none;padding:0}.paper{border:0;padding:0}table{font-size:10pt}tr{break-inside:avoid}@page{size:letter portrait;margin:.5in}}
</style>
