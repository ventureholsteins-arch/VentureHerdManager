const API_BASE = import.meta.env.VITE_API_URL

export interface SavedBaggingSchedule {
  sharedBaggingScheduleId: number
  publicToken: string
  showName: string
  showDate: string
  scheduleJson: string
  updatedAt: string
}

export async function getLatestBaggingSchedule(): Promise<SavedBaggingSchedule | null> {
  const response = await fetch(`${API_BASE}/BaggingSchedules/latest`)
  if (response.status === 204) return null
  if (!response.ok) throw new Error(await response.text() || 'Failed to load bagging schedule')
  return response.json()
}

export async function saveBaggingSchedule(data: { sharedBaggingScheduleId?: number; showName: string; showDate: string; scheduleJson: string }): Promise<SavedBaggingSchedule> {
  const response = await fetch(`${API_BASE}/BaggingSchedules`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })
  if (!response.ok) throw new Error(await response.text() || 'Failed to save bagging schedule')
  return response.json()
}

export async function saveSharedShowString(showStringJson: string): Promise<string> {
  const response = await fetch(`${API_BASE}/BaggingSchedules/show-string`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ showStringJson })
  })
  if (!response.ok) throw new Error(await response.text() || 'Failed to share show string')
  const result = await response.json()
  return result.token
}

export async function getSharedShowString(token: string): Promise<string> {
  const response = await fetch(`${API_BASE}/BaggingSchedules/show-string/${encodeURIComponent(token)}`)
  if (!response.ok) throw new Error(response.status === 404 ? 'This show string link was not found.' : 'Failed to load show string')
  const result = await response.json()
  return result.showStringJson
}
