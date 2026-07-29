const API_BASE = import.meta.env.VITE_API_URL

export interface AnimalNote {
  animalNoteId: number
  animalId: number
  noteDate: string
  noteText: string
  createdBy?: string | null
}

export async function getAnimalNotes(
  animalId: number
): Promise<AnimalNote[]> {
  const response = await fetch(
    `${API_BASE}/AnimalNotes/animal/${animalId}`
  )

  if (!response.ok) {
    throw new Error('Failed to load animal notes')
  }

  return await response.json()
}

export async function recordAnimalNote(
  animalId: number,
  noteText: string
): Promise<void> {
  const response = await fetch(`${API_BASE}/AnimalNotes`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId,
      noteDate: new Date().toISOString(),
      noteText,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record animal note')
  }
}