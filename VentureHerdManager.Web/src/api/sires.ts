const API_BASE = import.meta.env.VITE_API_URL

export interface SireCatalogRecord {
  sireReferenceId: number
  name: string
  shortName: string | null
  naabCode: string | null
  registrationNumber: string | null
  breedCode: string | null
  countryCode: string | null
  registryStatus: string | null
  marketingStatus: string | null
  birthDate: string | null
  yieldReliability: number | null
  ptaMilk: number | null
  ptaFat: number | null
  ptaFatPercent: number | null
  ptaProtein: number | null
  ptaProteinPercent: number | null
  somaticCellScore: number | null
  productiveLife: number | null
  daughterPregnancyRate: number | null
  heiferConceptionRate: number | null
  cowConceptionRate: number | null
  livability: number | null
  netMerit: number | null
  sireCalvingEase: number | null
  daughterCalvingEase: number | null
  ptaType: number | null
  totalPerformanceIndex: number | null
  udderComposite: number | null
  feetLegsComposite: number | null
  sourceFileName: string | null
  updatedAt: string
}

export interface SireUsageRecord {
  sire: string
  recordedNames: string[]
  breedings: number
  animals: number
  pregnant: number
  open: number
  unconfirmed: number
  firstUsed: string
  lastUsed: string
  catalogMatch: SireCatalogRecord | null
  catalogMatchStatus: string
}

export interface NaabImportResult {
  rowsRead: number
  added: number
  updated: number
  unchanged: number
  blankRows: number
  errors: number
  totalCatalogRecords: number
  errorMessages: string[]
}

async function fetchJson<T>(
  url: string,
  options?: RequestInit
): Promise<T> {
  const controller = new AbortController()
  const timeout = window.setTimeout(() => controller.abort(), 30_000)
  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal
    })
    if (!response.ok) {
      const detail = await response.text()
      throw new Error(detail || `Request failed (${response.status})`)
    }
    return response.json()
  } finally {
    window.clearTimeout(timeout)
  }
}

export function searchSires(search = '', limit = 40) {
  const query = new URLSearchParams({
    search,
    limit: String(limit)
  })
  return fetchJson<{
    totalCatalogRecords: number
    matches: SireCatalogRecord[]
  }>(`${API_BASE}/Sires?${query}`)
}

export function getUsedSires() {
  return fetchJson<{
    totalBreedings: number
    uniqueSires: number
    sires: SireUsageRecord[]
  }>(`${API_BASE}/Sires/used`)
}

export function importNaabCatalog(
  file: File,
  importKey: string
) {
  const form = new FormData()
  form.append('file', file)
  return fetchJson<NaabImportResult>(
    `${API_BASE}/Sires/import-naab`,
    {
      method: 'POST',
      headers: {
        'X-NAAB-Import-Key': importKey
      },
      body: form
    }
  )
}

export const easyIdPreparationUrl =
  `${API_BASE}/RegistrationExports/easy-id-prep.csv`
