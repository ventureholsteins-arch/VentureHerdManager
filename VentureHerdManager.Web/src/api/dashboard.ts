const API_BASE = import.meta.env.VITE_API_URL

export interface DashboardPregCheck {
  breedingEventId: number
  animalId: number
  animalName: string
  sireUsed: string
  breedingDate: string
  pregnancyCheckDueDate: string
  pregnancyStatus: number
  animalStage: number
}

export interface DashboardDueSoon {
  breedingEventId: number
  animalId: number
  animalName: string
  sireUsed: string
  expectedDueDate: string
  daysUntilDue: number
}

export interface DashboardLutTracking {
  lutalyseEventId: number
  animalId: number
  animalName: string
  administrationDate: string
  expectedHeatWatchEnd: string
  heatObserved: boolean
  daysTracked: number
  daysRemaining: number
}

export interface DashboardEmbryoImplant {
  trackingType: 'NeedsImplant' | 'PregCheckUpcoming'
  heatEventId?: number | null
  embryoRecordId?: number | null
  animalId: number
  animalName: string
  heatDateTime?: string | null
  embryoImplantDate?: string | null
  daysTracked: number
  daysUntilImplant: number
  code?: string | null
  donor?: string | null
  sire?: string | null
  mating?: string | null
  breedingEventId?: number | null
}

export interface DashboardHeat {
  heatEventId: number
  animalId: number
  animalName: string
  heatDateTime: string
  notes?: string | null
  pictureUrl?: string | null
}

export interface DashboardBreeding {
  breedingEventId: number
  animalId: number
  animalName: string
  breedingDate: string
  sireUsed: string
  breedingType: number
  pregnancyStatus: number
  pregnancyCheckDueDate: string
  expectedDueDate: string
}

export interface DashboardSummary {
  totalAnimals: number
  milking: number
  dry: number
  heifers: number
  calves: number
  bulls: number

  pregChecksDueCount: number
  dueSoonCount: number
  lutTrackingCount: number
  embryoImplantsCount: number

  herdScoreAverage?: number | null
  herdBaaAverage?: number | null
  animalsWithScore?: number
  animalsWithBaa?: number
  percentExcellent2ndLactationOrHigher?: number | null

  pregChecksDue: DashboardPregCheck[]
  dueSoon: DashboardDueSoon[]
  lutTracking: DashboardLutTracking[]
  embryoImplants: DashboardEmbryoImplant[]
  recentHeats: DashboardHeat[]
  recentBreedings: DashboardBreeding[]
}

function defaultDashboardSummary(): DashboardSummary {
  return {
    totalAnimals: 0,
    milking: 0,
    dry: 0,
    heifers: 0,
    calves: 0,
    bulls: 0,
    pregChecksDueCount: 0,
    dueSoonCount: 0,
    lutTrackingCount: 0,
    embryoImplantsCount: 0,
    herdScoreAverage: null,
    herdBaaAverage: null,
    animalsWithScore: 0,
    animalsWithBaa: 0,
    percentExcellent2ndLactationOrHigher: null,
    pregChecksDue: [],
    dueSoon: [],
    lutTracking: [],
    embryoImplants: [],
    recentHeats: [],
    recentBreedings: []
  }
}

function normalizeDashboardSummary(value: unknown): DashboardSummary {
  const base = defaultDashboardSummary()
  const data = (value && typeof value === 'object') ? (value as Record<string, unknown>) : {}

  return {
    ...base,
    ...data,
    pregChecksDue: Array.isArray(data.pregChecksDue) ? data.pregChecksDue as DashboardPregCheck[] : [],
    dueSoon: Array.isArray(data.dueSoon) ? data.dueSoon as DashboardDueSoon[] : [],
    lutTracking: Array.isArray(data.lutTracking) ? data.lutTracking as DashboardLutTracking[] : [],
    embryoImplants: Array.isArray(data.embryoImplants) ? data.embryoImplants as DashboardEmbryoImplant[] : [],
    recentHeats: Array.isArray(data.recentHeats) ? data.recentHeats as DashboardHeat[] : [],
    recentBreedings: Array.isArray(data.recentBreedings) ? data.recentBreedings as DashboardBreeding[] : []
  }
}

// Keep the last successful summary available long enough that normal app
// navigation never waits on a sleeping Azure database.
const DASHBOARD_CACHE_MS = 15 * 60_000
const DASHBOARD_TIMEOUT_MS = 30_000
const DASHBOARD_STORAGE_KEY = 'venture-herd-summary-cache-v1'
let cachedDashboard: DashboardSummary | null = null
let cachedAt = 0
let dashboardRequest: Promise<DashboardSummary> | null = null

function dashboardStorage() {
  return import.meta.env.VITE_DEMO_ONLY === 'true'
    ? sessionStorage
    : localStorage
}

function restoreDashboardCache() {
  if (cachedDashboard) return
  try {
    const raw = dashboardStorage().getItem(DASHBOARD_STORAGE_KEY)
    if (!raw) return
    const saved = JSON.parse(raw) as {
      value?: unknown
      savedAt?: number
    }
    if (
      typeof saved.savedAt === 'number'
      && Date.now() - saved.savedAt < DASHBOARD_CACHE_MS
    ) {
      cachedDashboard = normalizeDashboardSummary(saved.value)
      cachedAt = saved.savedAt
    }
  } catch {
    dashboardStorage().removeItem(DASHBOARD_STORAGE_KEY)
  }
}

export async function getDashboardSummary(dueDays = 30): Promise<DashboardSummary> {
  if (dueDays !== 30) {
    const response = await fetch(`${API_BASE}/Dashboard?dueDays=${dueDays}`)
    if (!response.ok) throw new Error('Failed to load dashboard')
    return normalizeDashboardSummary(await response.json())
  }
  restoreDashboardCache()
  const now = Date.now()

  if (cachedDashboard && now - cachedAt < DASHBOARD_CACHE_MS) {
    return cachedDashboard
  }

  if (dashboardRequest) {
    return dashboardRequest
  }

  dashboardRequest = (async () => {
    const controller = new AbortController()
    const timeout = window.setTimeout(
      () => controller.abort(),
      DASHBOARD_TIMEOUT_MS
    )

    let response: Response
    try {
      response = await fetch(`${API_BASE}/Dashboard`, {
        signal: controller.signal
      })
    } finally {
      window.clearTimeout(timeout)
    }

    if (!response.ok) {
      throw new Error('Failed to load dashboard')
    }

    const payload = await response.json()
    cachedDashboard = normalizeDashboardSummary(payload)
    cachedAt = Date.now()
    dashboardStorage().setItem(
      DASHBOARD_STORAGE_KEY,
      JSON.stringify({
        value: cachedDashboard,
        savedAt: cachedAt
      })
    )
    return cachedDashboard
  })()

  try {
    return await dashboardRequest
  } finally {
    dashboardRequest = null
  }
}
