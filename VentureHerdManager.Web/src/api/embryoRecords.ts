const API_BASE = import.meta.env.VITE_API_URL

export interface EmbryoRecord {
  embryoRecordId: number
  code: string | null
  sire: string | null
  donor: string | null
  grade: string | null
  status: 0 | 1 | 2 | 3 | 4
  recipientAnimalId: number | null
  implantDate: string | null
  linkedBreedingNote: string | null
  failureNotes: string | null
  notes: string | null
  collectionLocation: string | null
  storageLocation: string | null
  createdAt?: string
  updatedAt?: string
}

export const EMBRYO_STATUS_LABELS: Record<number, string> = {
  0: 'In Storage',
  1: 'Assigned',
  2: 'Implanted',
  3: 'Failed',
  4: 'Confirmed Pregnant',
}

export async function getAllEmbryos(): Promise<EmbryoRecord[]> {
  const response = await fetch(`${API_BASE}/EmbryoRecords`)
  if (!response.ok) throw new Error('Failed to load embryo records')
  return response.json()
}

export async function getEmbryoById(id: number): Promise<EmbryoRecord> {
  const response = await fetch(`${API_BASE}/EmbryoRecords/${id}`)
  if (!response.ok) throw new Error(`Failed to load embryo record #${id}`)
  return response.json()
}

export async function createEmbryo(data: Omit<EmbryoRecord, 'embryoRecordId' | 'createdAt' | 'updatedAt'>): Promise<EmbryoRecord> {
  const response = await fetch(`${API_BASE}/EmbryoRecords`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  if (!response.ok) throw new Error('Failed to create embryo record')
  return response.json()
}

export async function updateEmbryo(id: number, data: Partial<EmbryoRecord>): Promise<void> {
  const response = await fetch(`${API_BASE}/EmbryoRecords/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ...data, embryoRecordId: id }),
  })
  if (!response.ok) throw new Error('Failed to update embryo record')
}

export async function deleteEmbryo(id: number): Promise<void> {
  const response = await fetch(`${API_BASE}/EmbryoRecords/${id}`, {
    method: 'DELETE',
  })
  if (!response.ok) throw new Error('Failed to delete embryo record')
}

export interface RecentHeatRecipient {
  animalId: number
  animalName: string
  heatDateTime: string
  daysSinceHeat: number
}

export async function getRecentHeatRecipients(): Promise<RecentHeatRecipient[]> {
  const response = await fetch(
    `${API_BASE}/HeatEvents/recent-recipients?minDays=6&maxDays=8`
  )
  if (!response.ok) throw new Error('Failed to load recent heat recipients')
  return response.json()
}

export async function implantEmbryo(
  embryoRecordId: number,
  recipientAnimalId: number
): Promise<EmbryoRecord> {
  const response = await fetch(
    `${API_BASE}/EmbryoRecords/${embryoRecordId}/implant`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ recipientAnimalId })
    }
  )
  if (!response.ok) throw new Error(await response.text() || 'Failed to implant embryo')
  return response.json()
}

export async function recordEmbryoOutcome(
  embryoRecordId: number,
  successful: boolean,
  notes = ''
): Promise<EmbryoRecord> {
  const response = await fetch(
    `${API_BASE}/EmbryoRecords/${embryoRecordId}/outcome`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ successful, notes })
    }
  )
  if (!response.ok) throw new Error(await response.text() || 'Failed to record embryo outcome')
  return response.json()
}
