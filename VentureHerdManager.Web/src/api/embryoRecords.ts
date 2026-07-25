const API_BASE = import.meta.env.VITE_API_URL

export interface EmbryoRecord {
  embryoRecordId: number
  code: string | null
  sire: string | null
  donor: string | null
  grade: string | null
  status: 0 | 1 | 2 | 3   // 0=InStorage 1=Assigned 2=Implanted 3=Failed
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
