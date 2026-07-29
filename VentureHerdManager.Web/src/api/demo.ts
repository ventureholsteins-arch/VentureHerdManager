const API_BASE = import.meta.env.VITE_API_URL
// A fresh isolated demo session may need to wake Azure SQL and seed all sample
// records. Do not cancel that one-time setup while the server is still working.
const DEMO_REQUEST_TIMEOUT_MS = 180_000

async function fetchDemo(
  path: string,
  options: RequestInit
): Promise<Response> {
  const controller = new AbortController()
  const timeout = window.setTimeout(
    () => controller.abort(),
    DEMO_REQUEST_TIMEOUT_MS
  )

  try {
    return await fetch(`${API_BASE}${path}`, {
      ...options,
      signal: controller.signal
    })
  } finally {
    window.clearTimeout(timeout)
  }
}

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

  const response = await fetchDemo('/demo/reset', {
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

export async function ensureDemo(): Promise<DemoSeedResult> {
  const demoKey = import.meta.env.VITE_DEMO_KEY as string | undefined

  const response = await fetchDemo('/demo/ensure', {
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

  const response = await fetchDemo('/demo/status', {
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
