const API_BASE = import.meta.env.VITE_API_URL

export interface DryOffEvent {
  dryOffEventId: number
  animalId: number
  dryOffDate: string
  reason?: string | null
  notes?: string | null
  createdBy?: string | null
}

export async function getDryOffEvents(
  animalId: number
): Promise<DryOffEvent[]> {
  const response = await fetch(
    `${API_BASE}/DryOffEvents/animal/${animalId}`
  )

  if (!response.ok) {
    throw new Error('Failed to load dry-off events')
  }

  return await response.json()
}

export async function recordDryOff(
  animalId: number,
  reason: string,
  notes: string
): Promise<void> {
  const response = await fetch(`${API_BASE}/DryOffEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId,
      dryOffDate: new Date().toISOString(),
      reason,
      notes,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record dry off')
  }
}