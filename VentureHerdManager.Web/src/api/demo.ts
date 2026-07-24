const API_BASE = import.meta.env.VITE_API_URL

export interface DemoSeedResult {
  message: string
  animals: number
  heatEvents: number
  breedingEvents: number
  calvingEvents: number
  lutalyseEvents: number
}

export async function resetDemo(): Promise<DemoSeedResult> {
  const demoKey = import.meta.env.VITE_DEMO_KEY as string | undefined

  const response = await fetch(`${API_BASE}/api/demo/reset`, {
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
