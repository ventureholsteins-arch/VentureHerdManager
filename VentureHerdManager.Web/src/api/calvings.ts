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
  calfSireName: string,
  calfDamName: string,
  calvingEase: number,
  twins: boolean,
  stillborn: boolean,
  notes: string,
  pictureUrl?: string | null
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
      calfSireName,
      calfDamName,
      calvingEase,
      twins,
      stillborn,
      notes,
      pictureUrl: pictureUrl ?? null,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record calving')
  }
}

export async function updateCalvingEvent(
  calvingEventId: number,
  data: {
    calvingDate: string
    calfSex: number
    calfBarnName?: string | null
    calfRegisteredName?: string | null
    calvingEase: number
    twins: boolean
    stillborn: boolean
    notes?: string | null
    pictureUrl?: string | null
  }
): Promise<void> {
  const response = await fetch(`${API_BASE}/CalvingEvents/${calvingEventId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      calvingDate: data.calvingDate,
      calfSex: data.calfSex,
      calfBarnName: data.calfBarnName ?? null,
      calfRegisteredName: data.calfRegisteredName ?? null,
      calvingEase: data.calvingEase,
      twins: data.twins,
      stillborn: data.stillborn,
      notes: data.notes ?? null,
      pictureUrl: data.pictureUrl ?? null,
      updatedBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to update calving')
  }
}

export async function deleteCalvingEvent(calvingEventId: number): Promise<void> {
  const response = await fetch(`${API_BASE}/CalvingEvents/${calvingEventId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error('Failed to delete calving')
  }
}