const API_BASE = import.meta.env.VITE_API_URL

export interface AppearanceSetting {
  appearanceSettingId: number
  farmName: string
  logoUrl?: string | null
  backgroundImageUrl?: string | null
  backgroundOpacity: number
  overlayOpacity: number
  theme: string
  accentColor: string
  updatedAt: string
}

export async function getAppearance(): Promise<AppearanceSetting> {
  const response = await fetch(`${API_BASE}/Appearance`)

  if (!response.ok) {
    throw new Error(
      `Appearance request failed with status ${response.status}`
    )
  }

  return response.json()
}

export async function updateAppearance(
  appearance: AppearanceSetting
): Promise<AppearanceSetting> {
  const response = await fetch(`${API_BASE}/Appearance`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(appearance)
  })

  if (!response.ok) {
    throw new Error(
      `Appearance update failed with status ${response.status}`
    )
  }

  return response.json()
}
