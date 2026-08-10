const API_BASE = import.meta.env.VITE_API_URL

export interface PcdartImportRequest {
  rawText: string
  reportLabel?: string | null
  applySuggestedChanges?: boolean
  animalMappings?: Record<string, number>
  createMissingAnimals?: boolean
}

export interface PcdartAuditAlert {
  severity: string
  code: string
  animalId?: number | null
  animalLabel: string
  message: string
}

export interface PcdartSuggestedChange {
  code: string
  animalId?: number | null
  animalLabel: string
  proposedAction: string
  canAutoApply: boolean
}

export interface PcdartImportResult {
  applied: boolean
  reportLabel: string
  rowsRead: number
  animalsMatched: number
  animalsCreated: number
  notesCreated: number
  duplicateNotesSkipped: number
  suggestedChangesApplied: number
  missingAnimals: string[]
  conflicts: string[]
  alerts: PcdartAuditAlert[]
  suggestedChanges: PcdartSuggestedChange[]
}

async function postImport(path: 'preview' | 'apply', payload: PcdartImportRequest): Promise<PcdartImportResult> {
  const response = await fetch(`${API_BASE}/PcdartImport/${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(errorText || `Failed to ${path} PCDART import`)
  }

  return response.json()
}

export function previewPcdartImport(payload: PcdartImportRequest): Promise<PcdartImportResult> {
  return postImport('preview', payload)
}

export function applyPcdartImport(payload: PcdartImportRequest): Promise<PcdartImportResult> {
  return postImport('apply', payload)
}
