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
