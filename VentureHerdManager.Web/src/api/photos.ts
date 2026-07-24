const API_BASE = import.meta.env.VITE_API_URL

export interface PhotoUploadResponse {
  url: string
}

export async function uploadPhoto(
  file: File,
  folder: string
): Promise<string> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('folder', folder)

  const response = await fetch(`${API_BASE}/Photos/upload`, {
    method: 'POST',
    body: formData
  })

  if (!response.ok) {
    throw new Error('Failed to upload photo')
  }

  const payload = (await response.json()) as PhotoUploadResponse
  return payload.url
}
