<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import HerdLoadingScene from '../components/HerdLoadingScene.vue'
import RetroIcon from '../components/RetroIcon.vue'
import {
  easyIdPreparationUrl,
  getUsedSires,
  importNaabCatalog,
  searchSires,
  type NaabImportResult,
  type SireCatalogRecord,
  type SireUsageRecord
} from '../api/sires'

const router = useRouter()
const search = ref('')
const catalogCount = ref(0)
const matches = ref<SireCatalogRecord[]>([])
const usedSires = ref<SireUsageRecord[]>([])
const loading = ref(true)
const searching = ref(false)
const error = ref('')
const showImport = ref(false)
const importFile = ref<File | null>(null)
const importKey = ref('')
const importing = ref(false)
const importResult = ref<NaabImportResult | null>(null)

async function loadPage() {
  loading.value = true
  error.value = ''
  try {
    const [catalog, usage] = await Promise.all([
      searchSires('', 20),
      getUsedSires()
    ])
    catalogCount.value = catalog.totalCatalogRecords
    matches.value = catalog.matches
    usedSires.value = usage.sires
  } catch (reason) {
    error.value = reason instanceof Error
      ? reason.message
      : 'Sire information is temporarily unavailable.'
  } finally {
    loading.value = false
  }
}

async function runSearch() {
  searching.value = true
  error.value = ''
  try {
    const result = await searchSires(search.value, 60)
    catalogCount.value = result.totalCatalogRecords
    matches.value = result.matches
  } catch (reason) {
    error.value = reason instanceof Error
      ? reason.message
      : 'The sire search could not be completed.'
  } finally {
    searching.value = false
  }
}

function chooseImportFile(event: Event) {
  const input = event.target as HTMLInputElement
  importFile.value = input.files?.[0] ?? null
}

async function runImport() {
  if (!importFile.value || !importKey.value.trim()) {
    error.value = 'Choose the official NAAB AISS file and enter the import key.'
    return
  }

  importing.value = true
  error.value = ''
  importResult.value = null
  try {
    importResult.value = await importNaabCatalog(
      importFile.value,
      importKey.value.trim()
    )
    importKey.value = ''
    await runSearch()
  } catch (reason) {
    error.value = reason instanceof Error
      ? reason.message
      : 'The NAAB file could not be imported.'
  } finally {
    importing.value = false
  }
}

const fmtDate = (value: string | null) =>
  value ? new Date(value).toLocaleDateString() : '—'
const fmtTrait = (value: number | null) =>
  value == null ? '—' : value.toLocaleString()

onMounted(loadPage)
</script>

<template>
  <main class="sire-page">
    <header class="sire-hero">
      <button class="back" type="button" @click="router.push('/reports')">
        ← Reports
      </button>
      <div>
        <p class="eyebrow">Breeding reference</p>
        <h1>Sires</h1>
        <p>See what has been used in your herd and look up official NAAB reference data.</p>
      </div>
      <a class="export" :href="easyIdPreparationUrl" download>
        <RetroIcon name="reports" :size="22" />
        Registration prep CSV
      </a>
    </header>

    <HerdLoadingScene v-if="loading" message="Gathering sire records..." />
    <p v-else-if="error && !matches.length && !usedSires.length" class="error">
      {{ error }}
    </p>

    <template v-else>
      <p v-if="error" class="error">{{ error }}</p>

      <section class="panel">
        <div class="section-heading">
          <div>
            <p class="eyebrow">Your records</p>
            <h2>Sires used</h2>
          </div>
          <span>{{ usedSires.length }} sire{{ usedSires.length === 1 ? '' : 's' }}</span>
        </div>
        <div v-if="usedSires.length" class="usage-grid">
          <article v-for="sire in usedSires" :key="sire.sire" class="usage-card">
            <div>
              <h3>{{ sire.sire }}</h3>
              <p>{{ sire.animals }} animal{{ sire.animals === 1 ? '' : 's' }} · {{ sire.breedings }} breeding{{ sire.breedings === 1 ? '' : 's' }}</p>
            </div>
            <dl>
              <div><dt>Pregnant</dt><dd>{{ sire.pregnant }}</dd></div>
              <div><dt>Open</dt><dd>{{ sire.open }}</dd></div>
              <div><dt>To check</dt><dd>{{ sire.unconfirmed }}</dd></div>
              <div><dt>Last used</dt><dd>{{ fmtDate(sire.lastUsed) }}</dd></div>
            </dl>
            <p class="match" :class="{ matched: sire.catalogMatch }">
              {{ sire.catalogMatch ? `${sire.catalogMatch.naabCode || 'NAAB'} · ${sire.catalogMatch.name}` : sire.catalogMatchStatus }}
            </p>
          </article>
        </div>
        <p v-else class="empty">No breeding records with a sire have been entered yet.</p>
      </section>

      <section class="panel">
        <div class="section-heading">
          <div>
            <p class="eyebrow">Official reference</p>
            <h2>NAAB sire lookup</h2>
          </div>
          <span>{{ catalogCount.toLocaleString() }} loaded</span>
        </div>
        <form class="search-row" @submit.prevent="runSearch">
          <input
            v-model="search"
            type="search"
            placeholder="Name, short name, NAAB code, or registration number"
            autocomplete="off"
          />
          <button type="submit" :disabled="searching">
            {{ searching ? 'Searching…' : 'Search' }}
          </button>
        </form>

        <p v-if="catalogCount === 0" class="empty">
          The catalog has not been loaded yet. Your herd sire-usage report still works above.
        </p>
        <div v-else class="catalog-grid">
          <article v-for="sire in matches" :key="sire.sireReferenceId" class="catalog-card">
            <header>
              <div>
                <h3>{{ sire.shortName || sire.name }}</h3>
                <p v-if="sire.shortName && sire.shortName !== sire.name">{{ sire.name }}</p>
              </div>
              <strong>{{ sire.naabCode || 'No NAAB code' }}</strong>
            </header>
            <p class="identifiers">
              {{ sire.breedCode || 'Breed —' }} · {{ sire.registrationNumber || 'Registration —' }}
            </p>
            <dl class="traits">
              <div><dt>NM$</dt><dd>{{ fmtTrait(sire.netMerit) }}</dd></div>
              <div><dt>TPI</dt><dd>{{ fmtTrait(sire.totalPerformanceIndex) }}</dd></div>
              <div><dt>Milk</dt><dd>{{ fmtTrait(sire.ptaMilk) }}</dd></div>
              <div><dt>Type</dt><dd>{{ fmtTrait(sire.ptaType) }}</dd></div>
              <div><dt>DPR</dt><dd>{{ fmtTrait(sire.daughterPregnancyRate) }}</dd></div>
              <div><dt>SCE</dt><dd>{{ fmtTrait(sire.sireCalvingEase) }}</dd></div>
            </dl>
          </article>
        </div>

        <details class="import-box" :open="showImport" @toggle="showImport = ($event.target as HTMLDetailsElement).open">
          <summary>Update catalog from an official NAAB AISS file</summary>
          <p>
            Download the current comma-delimited AISS file from NAAB, then import the .txt or .csv here. Re-importing the same file does not create duplicates.
          </p>
          <label>
            Official AISS file
            <input type="file" accept=".txt,.csv,text/plain,text/csv" @change="chooseImportFile" />
          </label>
          <label>
            Import key
            <input v-model="importKey" type="password" autocomplete="off" />
          </label>
          <button type="button" :disabled="importing" @click="runImport">
            {{ importing ? 'Importing…' : 'Import catalog' }}
          </button>
          <p v-if="importResult" class="success">
            {{ importResult.added }} added, {{ importResult.updated }} updated,
            {{ importResult.unchanged }} unchanged, {{ importResult.errors }} errors.
          </p>
        </details>
      </section>
    </template>
  </main>
</template>

<style scoped>
.sire-page{min-height:100vh;padding:18px;background:#edf2ea;color:#17261b}.sire-hero,.panel{max-width:1080px;margin:0 auto 18px;background:#fff;border:1px solid #ccd7ca;border-radius:18px;box-shadow:0 7px 22px rgba(29,58,34,.08)}.sire-hero{display:grid;grid-template-columns:auto 1fr auto;gap:18px;align-items:center;padding:22px}.sire-hero h1,.panel h2,.panel h3{margin:0}.sire-hero p{margin:5px 0 0;color:#5a685d}.eyebrow{text-transform:uppercase;letter-spacing:.14em;font-size:.72rem;font-weight:800;color:#426346!important}.back,.export,.search-row button,.import-box button{min-height:44px;border:1px solid #31572c;border-radius:11px;background:#31572c;color:#fff;padding:10px 14px;font-weight:800;text-decoration:none}.export{display:flex;align-items:center;gap:8px}.panel{padding:22px}.section-heading{display:flex;justify-content:space-between;align-items:end;gap:12px;margin-bottom:16px}.section-heading>span{color:#607164;font-weight:700}.usage-grid,.catalog-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(245px,1fr));gap:12px}.usage-card,.catalog-card{border:1px solid #d5dfd3;border-radius:13px;padding:15px;background:#fbfdfb}.usage-card h3,.catalog-card h3{font-size:1.08rem}.usage-card p,.catalog-card p{margin:4px 0;color:#657068}.usage-card dl,.traits{display:grid;grid-template-columns:repeat(2,1fr);gap:8px;margin:14px 0}.usage-card dl div,.traits div{background:#eef4ed;border-radius:8px;padding:8px}.usage-card dt,.traits dt{font-size:.68rem;text-transform:uppercase;letter-spacing:.08em;color:#667268}.usage-card dd,.traits dd{margin:2px 0 0;font-weight:800}.match{font-size:.78rem}.match.matched{color:#31572c;font-weight:800}.search-row{display:grid;grid-template-columns:1fr auto;gap:10px;margin-bottom:16px}.search-row input,.import-box input{min-height:44px;border:1px solid #b8c6b7;border-radius:10px;padding:10px 12px;font:inherit}.catalog-card header{display:flex;justify-content:space-between;gap:10px}.catalog-card header strong{font-size:.78rem;color:#31572c}.identifiers{font-size:.8rem}.traits{grid-template-columns:repeat(3,1fr)}.empty,.error,.success{padding:12px;border-radius:10px;background:#f3f6f2}.error{background:#fff0ed;color:#8a2e21}.success{background:#eaf5e9;color:#25572b}.import-box{margin-top:18px;border-top:1px solid #d7dfd5;padding-top:16px}.import-box summary{cursor:pointer;font-weight:800}.import-box label{display:grid;gap:6px;margin:12px 0;font-weight:700;max-width:520px}
@media(max-width:680px){.sire-page{padding:8px}.sire-hero{grid-template-columns:1fr;padding:16px}.sire-hero .back,.sire-hero .export{width:100%;justify-content:center}.panel{padding:16px}.search-row{grid-template-columns:1fr}.search-row button{width:100%}.traits{grid-template-columns:repeat(2,1fr)}.section-heading{align-items:start;flex-direction:column}.usage-grid,.catalog-grid{grid-template-columns:1fr}}
</style>
