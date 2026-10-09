<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getPrintReports } from '../api/printReports'
import HerdLoadingScene from '../components/HerdLoadingScene.vue'
import RetroIcon from '../components/RetroIcon.vue'
import { easyIdPreparationUrl } from '../api/sires'

const router = useRouter()
const route = useRoute()
const data = ref<any>(null)
const loading = ref(true)
const error = ref('')
const report = ref(typeof route.query.report === 'string' ? route.query.report : 'missingRegistration')
const phoneFit = ref(false)

function uniqueAnimals(items: any[]): any[] {
  const seen = new Set<number>()
  return items.filter(item => {
    if (typeof item?.animalId !== 'number') return true
    if (seen.has(item.animalId)) return false
    seen.add(item.animalId)
    return true
  })
}

const options = [
  ['missingRegistration', 'Missing registration numbers'],
  ['missingAnimalIdentification', 'Missing barn names or registration numbers'],
  ['healthPapers', 'Health papers — Health Paper Group'],
  ['oldEnoughNotBred', '7 months+ and not bred'],
  ['milkingNotBred', 'Milking cows not bred'],
  ['sellAnimals', 'My sale report'],
  ['suggestedSell', 'Suggested sale review'],
  ['classificationScores', 'Classification scores and history'],
  ['genomicResults', 'Genomic results'],
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
  if (report.value === 'healthPapers') {
    let ids: number[] = []
    try {
      const lists = JSON.parse(localStorage.getItem('venture-herd-lists-v2') || '[]') as Array<{ key: string; title?: string; animalIds: number[] }>
      ids = lists.find(list => list.key === 'health-paper-group'
        || list.title?.trim().toLowerCase() === 'health paper group')?.animalIds ?? []
    } catch { ids = [] }
    return (data.value.animals ?? [])
      .filter((animal: any) => ids.includes(animal.animalId))
      .sort((a: any, b: any) => {
        if (!a.birthDate && !b.birthDate) return String(a.barnName || a.registeredName || '').localeCompare(String(b.barnName || b.registeredName || ''))
        if (!a.birthDate) return 1
        if (!b.birthDate) return -1
        return String(a.birthDate).localeCompare(String(b.birthDate))
          || String(a.barnName || a.registeredName || '').localeCompare(String(b.barnName || b.registeredName || ''))
      })
  }
  if (report.value === 'sellAnimals') {
    let ids: number[] = []
    try {
      const lists = JSON.parse(localStorage.getItem('venture-herd-lists-v2') || '[]') as Array<{ key: string; animalIds: number[] }>
      ids = lists.find(list => list.key === 'sale-animals')?.animalIds ?? []
    } catch { ids = [] }
    return (data.value.saleAnimals ?? []).filter((animal: any) => ids.includes(animal.animalId))
  }
  if (report.value === 'suggestedSell') return data.value.suggestedSell ?? []
  if (report.value === 'calves') return uniqueAnimals(data.value.animals.filter((a: any) => a.animalStage === 1))
  if (report.value === 'heifers') return uniqueAnimals(data.value.animals.filter((a: any) => a.animalStage === 2))
  if (report.value === 'cows') return uniqueAnimals(data.value.animals.filter((a: any) => [3, 4].includes(a.animalStage)))
  const result = data.value[report.value] ?? []
  return isAnimalReport.value ? uniqueAnimals(result) : result
})

const title = computed(() => options.find(x => x[0] === report.value)?.[1] ?? 'Report')
const isAnimalReport = computed(() => [
  'missingRegistration',
  'missingAnimalIdentification',
  'oldEnoughNotBred',
  'milkingNotBred',
  'animals',
  'calves',
  'heifers',
  'cows'
].includes(report.value))
const isSireReport = computed(() => report.value === 'siresUsed')
const isHealthPaperReport = computed(() => report.value === 'healthPapers')
const isTimelineReport = computed(() => ['breedings', 'pregnancyChecksDue', 'heiferPregChecks', 'cowPregChecks'].includes(report.value))
const isDueWithinEightMonthsReport = computed(() => report.value === 'dueWithinEightMonths')
const isClassificationReport = computed(() => report.value === 'classificationScores')
const isGenomicReport = computed(() => report.value === 'genomicResults')
const fmt = (value: string | null) => value ? new Date(value).toLocaleDateString() : '—'
const printReport = () => window.print()

function healthPaperText(): string {
  const animals = rows.value.map((animal: any, index: number) => [
    `${index + 1}. ${animal.barnName || animal.registeredName || 'Unnamed animal'}`,
    `Full registered name: ${animal.registeredName || '—'}`,
    `Birthdate: ${fmt(animal.birthDate)}`,
    `Registration number: ${animal.registrationNumber || 'MISSING'}`
  ].join('\n'))

  return ['HEALTH PAPER GROUP', ...animals].join('\n\n')
}

async function copyHealthPaperDetails() {
  await navigator.clipboard.writeText(healthPaperText())
  alert('Health paper details copied. They are ready to paste into an email or document.')
}

function emailHealthPaperDetails() {
  const subject = encodeURIComponent('Health paper animal information')
  const body = encodeURIComponent(`Here is the animal information for health papers.\n\n${healthPaperText()}`)
  window.location.href = `mailto:?subject=${subject}&body=${body}`
}

async function shareReportView() {
  const payload = {
    title: `Venture Herd Manager - ${title.value}`,
    text: `Report view: ${title.value}`,
    url: window.location.href
  }

  try {
    if (navigator.share) {
      await navigator.share(payload)
      return
    }
  } catch {
    // Ignore and fall back to clipboard.
  }

  await navigator.clipboard.writeText(window.location.href)
  alert('Report link copied to clipboard.')
}

onMounted(async () => {
  try { data.value = await getPrintReports() }
  catch { error.value = 'Reports are temporarily unavailable.' }
  finally { loading.value = false }
})
</script>

<template>
  <main class="print-page" :class="{ 'phone-fit': phoneFit }">
    <header class="report-toolbar no-print">
      <button @click="router.push('/reports')">← Reports</button>
      <label>Report<select v-model="report"><option v-for="item in options" :key="item[0]" :value="item[0]">{{ item[1] }}</option></select></label>
      <a class="export-link" :href="easyIdPreparationUrl" download>Registration prep CSV</a>
      <button class="print-button" @click="phoneFit = !phoneFit">{{ phoneFit ? 'Desktop fit' : 'Phone fit' }}</button>
      <button class="print-button" @click="shareReportView">Share view</button>
      <button v-if="isHealthPaperReport" class="print-button" @click="copyHealthPaperDetails">Copy Details</button>
      <button v-if="isHealthPaperReport" class="print-button" @click="emailHealthPaperDetails">Email Report</button>
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
      <p v-if="isHealthPaperReport" class="health-paper-note">Animals are included from the <strong>Health Paper Group</strong> herd list. Add or remove animals there, then return to this report.</p>
      <table>
        <thead><tr v-if="report === 'suggestedSell'"><th>Rank</th><th>Cow</th><th>Milk / DIM</th><th>Reproduction</th><th>Genomics</th><th>Why review</th><th>Reasons to keep</th></tr>
        <tr v-else-if="report === 'sellAnimals'"><th>Animal</th><th>Open status</th><th>Months old</th><th>Times bred</th><th>Registration #</th><th>Sire</th><th>Dam</th></tr>
        <tr v-else-if="isHealthPaperReport"><th>Name</th><th>Full registered name</th><th>Birthdate</th><th>Registration number</th></tr>
        <tr v-else-if="isAnimalReport"><th>Animal</th><th>Registered name</th><th>Registration #</th><th>Birth date</th><th>Sire</th><th>Dam</th></tr>
        <tr v-else-if="isSireReport"><th>Sire</th><th>Animals</th><th>Breedings</th><th>Pregnant</th><th>Open</th><th>To check</th><th>Last used</th></tr>
        <tr v-else-if="report === 'lastMonthHeats'"><th>Animal</th><th>Heat date</th><th>Notes</th></tr>
        <tr v-else-if="isDueWithinEightMonthsReport"><th>Animal</th><th>Sire</th><th>Due</th><th>Working notes</th></tr>
        <tr v-else-if="isClassificationReport"><th>Animal</th><th>Current score</th><th>Current BAA</th><th>Scored</th><th>Previous scores / BAA</th></tr>
        <tr v-else-if="isGenomicReport"><th>Animal</th><th>Report</th><th>TPI</th><th>NM$</th><th>Milk PTA</th><th>DPR</th><th>PL</th><th>Type</th><th>UDC</th><th>FLC</th></tr>
        <tr v-else-if="isTimelineReport"><th>Animal</th><th>Bred</th><th>Sire</th><th>Due / Check</th><th>Status</th><th>Working notes</th></tr>
        <tr v-else><th>Code</th><th>Donor × Sire</th><th>Grade</th><th>Recipient</th><th>Implant date</th><th>Status</th></tr></thead>
        <tbody>
          <tr v-for="row in rows" :key="row.animalId ?? row.heatEventId ?? row.breedingEventId ?? row.embryoRecordId ?? row.sire">
            <template v-if="report === 'suggestedSell'"><td><strong>{{ row.score }}</strong><br><small>{{ row.reviewLevel }}</small></td><td>{{ row.barnName || row.registeredName || `Animal #${row.animalId}` }}</td><td>{{ row.milk ?? 'Missing' }}<br><small>DIM {{ row.daysInMilk ?? '—' }}</small></td><td>{{ row.reproStatus }}</td><td>NM$ {{ row.netMerit ?? '—' }}<br><small>TPI {{ row.tpi ?? '—' }}</small></td><td>{{ row.concerns.join(' · ') || 'No major concerns' }}</td><td>{{ row.strengths.join(' · ') || 'No recorded strengths yet' }}</td></template>
            <template v-else-if="report === 'sellAnimals'"><td>{{ row.barnName || row.registeredName || [row.damName, row.sireName].filter(Boolean).join(' × ') || `Animal #${row.animalId}` }}</td><td>{{ row.openStatus }}</td><td>{{ row.monthsOld ?? '—' }}</td><td>{{ row.timesBred }}</td><td>{{ row.registrationNumber || 'MISSING' }}</td><td>{{ row.sireName || '—' }}</td><td>{{ row.damName || '—' }}</td></template>
            <template v-else-if="isHealthPaperReport"><td>{{ row.barnName || '—' }}</td><td>{{ row.registeredName || '—' }}</td><td>{{ fmt(row.birthDate) }}</td><td>{{ row.registrationNumber || 'MISSING' }}</td></template>
            <template v-else-if="isAnimalReport"><td>{{ row.barnName || row.registeredName || [row.damName, row.sireName].filter(Boolean).join(' × ') || `Animal #${row.animalId}` }}</td><td>{{ row.registeredName || '—' }}</td><td>{{ row.registrationNumber || 'MISSING' }}</td><td>{{ fmt(row.birthDate) }}</td><td>{{ row.sireName || '—' }}</td><td>{{ row.damName || '—' }}</td></template>
            <template v-else-if="isSireReport"><td>{{ row.sire }}</td><td>{{ row.animals }}</td><td>{{ row.breedings }}</td><td>{{ row.pregnant }}</td><td>{{ row.open }}</td><td>{{ row.toCheck }}</td><td>{{ fmt(row.lastUsed) }}</td></template>
            <template v-else-if="report === 'lastMonthHeats'"><td>{{ row.animalName }}</td><td>{{ fmt(row.heatDateTime) }}</td><td>{{ row.notes || '—' }}</td></template>
            <template v-else-if="isDueWithinEightMonthsReport"><td>{{ row.animalName }}</td><td>{{ row.sireUsed }}</td><td>{{ fmt(row.expectedDueDate || row.pregnancyCheckDueDate) }}</td><td class="write-field"></td></template>
            <template v-else-if="isClassificationReport"><td><strong>{{ row.animalName }}</strong><br><small>{{ row.registeredName || '' }}</small></td><td>{{ row.currentLabel || '' }} {{ row.currentScore }}</td><td>{{ row.currentBaa ?? '—' }}</td><td>{{ fmt(row.currentDate) }}</td><td class="history-cell"><span v-if="!row.previousScores?.length">No previous score</span><span v-for="prior in row.previousScores" :key="`${prior.date}-${prior.score}`">{{ fmt(prior.date) }}: {{ prior.classificationLabel || '' }} {{ prior.score }} · BAA {{ prior.baa ?? '—' }}</span></td></template>
            <template v-else-if="isGenomicReport"><td><strong>{{ row.animalName }}</strong><br><small>{{ row.registeredName || '' }}</small></td><td>{{ fmt(row.reportDate) }}</td><td>{{ row.tpi ?? '—' }}</td><td>{{ row.netMerit ?? '—' }}</td><td>{{ row.milkPta ?? '—' }}</td><td>{{ row.daughterPregnancyRate ?? '—' }}</td><td>{{ row.productiveLife ?? '—' }}</td><td>{{ row.typeScore ?? '—' }}</td><td>{{ row.udderComposite ?? '—' }}</td><td>{{ row.feetLegsComposite ?? '—' }}</td></template>
            <template v-else-if="isTimelineReport"><td>{{ row.animalName }}</td><td>{{ fmt(row.breedingDate) }}</td><td>{{ row.sireUsed }}</td><td>{{ fmt(row.expectedDueDate || row.pregnancyCheckDueDate) }}</td><td>{{ row.pregnancyStatus }}</td><td class="write-field"></td></template>
            <template v-else><td>{{ row.code || `#${row.embryoRecordId}` }}</td><td>{{ row.donor || '—' }} × {{ row.sire || '—' }}</td><td>{{ row.grade || '—' }}</td><td>{{ row.recipientName || '—' }}</td><td>{{ fmt(row.implantDate) }}</td><td>{{ row.status }}</td></template>
          </tr>
          <tr v-if="rows.length === 0"><td colspan="6">No records in this report.</td></tr>
        </tbody>
      </table>
    </article>
  </main>
</template>

<style scoped>
.print-page{max-width:1100px;margin:auto;padding:12px}.report-toolbar{display:flex;gap:8px;align-items:end;flex-wrap:wrap;margin-bottom:10px}.report-toolbar label{display:grid;gap:4px;flex:1;min-width:220px}.report-toolbar select,.report-toolbar button,.export-link{min-height:40px;padding:6px 10px}.export-link{display:flex;align-items:center;border:1px solid #31572c;border-radius:3px;color:#31572c;text-decoration:none}.print-button{display:flex;align-items:center;gap:6px}.paper{background:#fff;color:#111;padding:18px;border:1px solid #ccd5ce}.paper header{border-bottom:1px solid #31572c;margin-bottom:9px}.paper header p{margin:2px 0 6px;font-size:11px}.paper h1{margin:0;font-size:20px;line-height:1.15}.stats{display:flex;gap:5px;flex-wrap:wrap;margin-bottom:8px}.stats span{border:1px solid #bbb;padding:3px 6px;font-size:11px}table{width:100%;border-collapse:collapse;font-size:11px;line-height:1.25}th,td{text-align:left;padding:4px 5px;border-bottom:1px solid #ccc;vertical-align:top}th{background:#f5f7f5}.write-field{min-width:100px;height:22px}.history-cell span{display:block;white-space:nowrap}.history-cell span+span{margin-top:2px}
.decision-note{border:1px solid #d2a829;background:#fff9df;padding:10px 12px;font-weight:700}
.health-paper-note{padding:10px 12px;border-left:4px solid #31572c;background:#f4f7f2}
.phone-fit .paper{padding:12px}
.phone-fit table{font-size:12px}
.phone-fit .write-field{min-width:100px}
@media(max-width:600px){.print-page{padding:8px}.paper{padding:14px;overflow-x:auto}.report-toolbar>*{width:100%}table{min-width:720px}.phone-fit table{min-width:560px}.phone-fit th,.phone-fit td{padding:6px}}
@media print{.no-print{display:none!important}.print-page{max-width:none;padding:0}.paper{border:0;padding:0}.paper h1{font-size:14pt}.paper header p{font-size:7.5pt}.stats span{padding:2px 4px;font-size:7.5pt;background:transparent}table{font-size:7.5pt;line-height:1.15}th,td{padding:2.5px 3px}th{background:transparent!important;border-top:1px solid #999}.decision-note,.health-paper-note{padding:4px 6px;background:transparent!important}.write-field{height:16px}tr{break-inside:avoid}@page{size:letter portrait;margin:.35in}}
</style>
