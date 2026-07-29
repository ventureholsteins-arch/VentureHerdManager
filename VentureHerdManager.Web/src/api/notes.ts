const API_BASE = import.meta.env.VITE_API_URL

export interface AnimalNote {
  animalNoteId: number
  animalId: number
  noteText: string
  noteType: number
  noteDate: string
  createdBy?: string
}

export async function addNote(noteData: {
  animalId: number
  noteText: string
  noteType: number
}): Promise<AnimalNote> {
  const response = await fetch(`${API_BASE}/AnimalNotes`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId: noteData.animalId,
      noteText: noteData.noteText,
      noteType: noteData.noteType,
      noteDate: new Date().toISOString(),
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to add note')
  }

  return response.json()
}
