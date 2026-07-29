const API_BASE = import.meta.env.VITE_API_URL

export async function getPrintReports(): Promise<any> {
  const controller = new AbortController()
  const timeout = window.setTimeout(() => controller.abort(), 30_000)
  let response: Response
  try {
    response = await fetch(`${API_BASE}/PrintReports`, {
      signal: controller.signal
    })
  } finally {
    window.clearTimeout(timeout)
  }
  if (!response.ok) throw new Error('Failed to load printable reports')
  return response.json()
}
