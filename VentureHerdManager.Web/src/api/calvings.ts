const API_BASE = import.meta.env.VITE_API_URL

export interface CalvingEvent {
  calvingEventId: number
  animalId: number
  calvingDate: string
  calfSex: number
  calfBarnName?: string | null
  calfRegisteredName?: string | null
  calvingEase: number
  twins: boolean
  stillborn: boolean
  notes?: string | null
  createdBy?: string | null
}

export async function getCalvings(animalId: number): Promise<CalvingEvent[]> {
  const response = await fetch(`${API_BASE}/CalvingEvents/animal/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load calvings')
  }

  return await response.json()
}

export async function recordCalving(
  animalId: number,
  calfSex: number,
  calfBarnName: string,
  calfRegisteredName: string,
  calvingEase: number,
  twins: boolean,
  stillborn: boolean,
  notes: string
): Promise<void> {
  const response = await fetch(`${API_BASE}/CalvingEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId,
      calvingDate: new Date().toISOString(),
      calfSex,
      calfBarnName,
      calfRegisteredName,
      calvingEase,
      twins,
      stillborn,
      notes,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record calving')
  }
}