const API_BASE = import.meta.env.VITE_API_URL

export interface HeatEvent {
  heatEventId: number
  animalId: number
  heatDateTime: string
  notes?: string | null
  pictureUrl?: string | null
  createdBy?: string | null
}

export async function getHeatEvents(animalId: number): Promise<HeatEvent[]> {
  const response = await fetch(`${API_BASE}/HeatEvents/animal/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load heat events')
  }

  return await response.json()
}

export async function recordHeat(animalId: number, notes: string): Promise<void> {
  const response = await fetch(`${API_BASE}/HeatEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId,
      heatDateTime: new Date().toISOString(),
      notes,
      pictureUrl: null,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record heat')
  }
}