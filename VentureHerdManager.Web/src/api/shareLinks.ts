const API_BASE = import.meta.env.VITE_API_URL

export interface SharedHerdData {
  readOnly: true
  expiresAt: string
  animals: Array<Record<string, unknown>>
  embryos: Array<Record<string, unknown>>
}

export async function createShareLink(input: {
  animalIds: number[]
  includeAnimals: boolean
  includeEmbryos: boolean
  includeOutcomes: boolean
  expiresInDays: number
}): Promise<{ token: string; expiresAt: string }> {
  const response = await fetch(`${API_BASE}/ShareLinks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input)
  })
  if (!response.ok) throw new Error(await response.text() || 'Could not create share link')
  return response.json()
}

export async function getSharedHerd(token: string): Promise<SharedHerdData> {
  const response = await fetch(`${API_BASE}/ShareLinks/${encodeURIComponent(token)}`)
  if (!response.ok) throw new Error(await response.text() || 'Share link unavailable')
  return response.json()
}
