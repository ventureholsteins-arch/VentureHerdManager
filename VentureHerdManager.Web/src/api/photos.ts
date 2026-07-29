const API_BASE = import.meta.env.VITE_API_URL

export interface PhotoUploadResponse {
  url: string
}

export async function uploadPhoto(
  file: File,
  folder: string
): Promise<string> {
  const controller = new AbortController()
  const timeoutId = window.setTimeout(() => controller.abort(), 45_000)
  const formData = new FormData()
  formData.append('file', file)
  formData.append('folder', folder)

  try {
    const response = await fetch(`${API_BASE}/Photos/upload`, {
      method: 'POST',
      body: formData,
      signal: controller.signal
    })

    if (!response.ok) {
      const details = await response.text()
      throw new Error(details || `Failed to upload photo (${response.status})`)
    }

    const payload = (await response.json()) as PhotoUploadResponse
    return payload.url
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw new Error('Photo upload timed out. Please try again.')
    }
    throw error
  } finally {
    window.clearTimeout(timeoutId)
  }
}
