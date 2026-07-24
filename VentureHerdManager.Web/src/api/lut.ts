const API_BASE = import.meta.env.VITE_API_URL

export interface LutalyseEvent {
  lutalyseEventId: number
  animalId: number
  administrationDate: string
  expectedHeatWatchStart: string
  expectedHeatWatchEnd: string
  notes?: string
  createdBy?: string
}

export async function recordLUT(lutData: {
  animalId: number
  administrationDate: string
  expectedHeatWatchStart: string
  expectedHeatWatchEnd: string
  notes?: string
}): Promise<LutalyseEvent> {
  const response = await fetch(`${API_BASE}/LutalyseEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId: lutData.animalId,
      administrationDate: lutData.administrationDate,
      expectedHeatWatchStart: lutData.expectedHeatWatchStart,
      expectedHeatWatchEnd: lutData.expectedHeatWatchEnd,
      notes: lutData.notes,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record LUT injection')
  }

  return response.json()
}
