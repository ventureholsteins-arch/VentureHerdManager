import type { CalendarEvent } from '../models/CalendarEvent'

function getApiUrl(): string {
  const apiUrl = import.meta.env.VITE_API_URL

  if (!apiUrl) {
    throw new Error('The API address is not configured.')
  }

  return apiUrl.replace(/\/$/, '')
}

function formatApiDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

export async function getCalendarEvents(
  startDate: Date,
  endDate: Date
): Promise<CalendarEvent[]> {
  const query = new URLSearchParams({
    startDate: formatApiDate(startDate),
    endDate: formatApiDate(endDate)
  })

  const response = await fetch(
    `${getApiUrl()}/Calendar?${query.toString()}`
  )

  if (!response.ok) {
    const errorText = await response.text()

    throw new Error(
      errorText ||
      `Calendar request failed with status ${response.status}.`
    )
  }

  const result: unknown = await response.json()

  return Array.isArray(result)
    ? result as CalendarEvent[]
    : []
}