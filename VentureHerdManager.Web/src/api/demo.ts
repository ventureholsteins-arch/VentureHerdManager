const API_BASE = import.meta.env.VITE_API_URL

export interface DemoSeedResult {
  message: string
  animals: number
  heatEvents: number
  breedingEvents: number
  calvingEvents: number
  lutalyseEvents: number
}

export interface DemoStatusResponse {
  enabled: boolean
  counts: {
    animals: number
    activeAnimals: number
    heats: number
    breedings: number
    calvings: number
    lutalyseEvents: number
    notes: number
    photos: number
  }
  stageCounts: Array<{
    stage: string
    count: number
  }>
  previewAnimals: Array<{
    animalId: number
    name: string
    stage: string
    breed: string | null
  }>
}

export async function resetDemo(): Promise<DemoSeedResult> {
  const demoKey = import.meta.env.VITE_DEMO_KEY as string | undefined

  const response = await fetch(`${API_BASE}/demo/reset`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(demoKey ? { 'X-Demo-Key': demoKey } : {})
    }
  })

  if (!response.ok) {
    const body = await response.json().catch(() => ({ message: 'Unknown error' }))
    throw new Error((body as DemoSeedResult).message || `HTTP ${response.status}`)
  }

  return response.json() as Promise<DemoSeedResult>
}

export async function getDemoStatus(): Promise<DemoStatusResponse> {
  const demoKey = import.meta.env.VITE_DEMO_KEY as string | undefined

  const response = await fetch(`${API_BASE}/demo/status`, {
    headers: {
      ...(demoKey ? { 'X-Demo-Key': demoKey } : {})
    }
  })

  if (!response.ok) {
    const body = await response.json().catch(() => ({ message: 'Unknown error' }))
    throw new Error((body as { message?: string }).message || `HTTP ${response.status}`)
  }

  return response.json() as Promise<DemoStatusResponse>
}
