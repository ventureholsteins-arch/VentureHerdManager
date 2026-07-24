const API_BASE = import.meta.env.VITE_API_URL

export interface DashboardPregCheck {
  breedingEventId: number
  animalId: number
  animalName: string
  sireUsed: string
  breedingDate: string
  pregnancyCheckDueDate: string
  pregnancyStatus: number
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
  heatEventId: number
  animalId: number
  animalName: string
  heatDateTime: string
  embryoImplantDate?: string | null
  daysTracked: number
  daysUntilImplant: number
}

export interface DashboardHeat {
  heatEventId: number
  animalId: number
  animalName: string
  heatDateTime: string
  notes?: string | null
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

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const response = await fetch(`${API_BASE}/Dashboard`)

  if (!response.ok) {
    throw new Error('Failed to load dashboard')
  }

  const payload = await response.json()
  return normalizeDashboardSummary(payload)
}