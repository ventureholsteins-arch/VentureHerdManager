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
