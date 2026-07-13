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

  pregChecksDue: DashboardPregCheck[]
  dueSoon: DashboardDueSoon[]
  recentHeats: DashboardHeat[]
  recentBreedings: DashboardBreeding[]
}

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const response = await fetch(`${API_BASE}/Dashboard`)

  if (!response.ok) {
    throw new Error('Failed to load dashboard')
  }

  return await response.json()
}