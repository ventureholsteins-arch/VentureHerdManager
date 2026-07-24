const API_BASE = import.meta.env.VITE_API_URL

export interface LutalyseEvent {
  lutalyseEventId: number
  animalId: number
  administrationDate: string
  expectedHeatWatchStart: string
  expectedHeatWatchEnd: string
  heatObserved?: boolean
  notes?: string
  createdBy?: string
}

export async function getLutEvents(animalId: number): Promise<LutalyseEvent[]> {
  const response = await fetch(`${API_BASE}/LutalyseEvents/animal/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load LUT events')
  }

  return response.json()
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

export async function updateLutEvent(
  lutalyseEventId: number,
  data: {
    administrationDate: string
    expectedHeatWatchStart: string
    expectedHeatWatchEnd: string
    heatObserved: boolean
    notes?: string | null
  }
): Promise<void> {
  const response = await fetch(`${API_BASE}/LutalyseEvents/${lutalyseEventId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      administrationDate: data.administrationDate,
      expectedHeatWatchStart: data.expectedHeatWatchStart,
      expectedHeatWatchEnd: data.expectedHeatWatchEnd,
      heatObserved: data.heatObserved,
      notes: data.notes ?? null,
      updatedBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to update LUT event')
  }
}

export async function deleteLutEvent(lutalyseEventId: number): Promise<void> {
  const response = await fetch(`${API_BASE}/LutalyseEvents/${lutalyseEventId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error('Failed to delete LUT event')
  }
}
