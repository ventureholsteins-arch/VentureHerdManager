const API_BASE = import.meta.env.VITE_API_URL

export interface MonthDataPoint {
  label: string
  calvings: number
  heats: number
  breedings: number
  confirmedPregnancies: number
  soldAnimals: number
  dryOffs: number
}

export interface HerdActivityResponse {
  months: MonthDataPoint[]
  totals: {
    activeAnimals: number
    conceptionRatePct: number
    calvingsLast12Mo: number
    heatsLast12Mo: number
    breedingsLast12Mo: number
  }
}

export async function getHerdActivity(months = 12): Promise<HerdActivityResponse> {
  const response = await fetch(`${API_BASE}/Analytics/herd-activity?months=${months}`)
  if (!response.ok) throw new Error('Failed to load herd activity')
  return response.json()
}

export interface EmbryoImplantMonthData {
  label: string
  implanted: number
  failed: number
  successful: number
}

export interface EmbryoImplantResponse {
  months: EmbryoImplantMonthData[]
  totals: {
    totalImplanted: number
    totalFailed: number
    totalSuccessful: number
    resolvedImplants: number
    waitingForPregCheck: number
    successRatePct: number
    failureRatePct: number
    outcomeRecordedPct: number
  }
}

export async function getEmbryoImplants(months = 12): Promise<EmbryoImplantResponse> {
  const response = await fetch(`${API_BASE}/Analytics/embryo-implants?months=${months}`)
  if (!response.ok) throw new Error('Failed to load embryo implants')
  return response.json()
}
