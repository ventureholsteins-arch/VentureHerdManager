const API_BASE = import.meta.env.VITE_API_URL

export interface CalvingEvent {
  calvingEventId: number
  animalId: number
  calvingDate: string
  calvingSex?: number
  calvingEase?: number
  calfCount?: number
  notes?: string
  createdBy?: string
}

export async function recordCalving(calvingData: {
  animalId: number
  calvingDate: string
  pictureUrl?: string
  calfBarnName: string
  calfRegisteredName?: string
  calfSex: number
  birthWeight?: number
  calvingEase: number
  twins: boolean
  stillborn: boolean
  notes?: string
}): Promise<{ calvingEventId: number; calfAnimalId?: number }> {
  const response = await fetch(`${API_BASE}/CalvingEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId: calvingData.animalId,
      calvingDate: calvingData.calvingDate,
      pictureUrl: calvingData.pictureUrl,
      calfBarnName: calvingData.calfBarnName,
      calfRegisteredName: calvingData.calfRegisteredName,
      calvingSex: calvingData.calfSex,
      birthWeight: calvingData.birthWeight,
      calvingEase: calvingData.calvingEase,
      twins: calvingData.twins,
      stillborn: calvingData.stillborn,
      notes: calvingData.notes,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record calving')
  }

  return response.json()
}
