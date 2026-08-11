export type GenomicTrait = {
  key: string
  csv: string
  label: string
  low: string
  high: string
  note?: string
  group: 'Frame' | 'Feet & Legs' | 'Udder'
}

export const genomicLinearTraits: GenomicTrait[] = [
  { key: 'stature', csv: 'ST', label: 'Stature', low: 'Short', high: 'Tall', group: 'Frame' },
  { key: 'strength', csv: 'SG', label: 'Strength', low: 'Frail', high: 'Strong', group: 'Frame' },
  { key: 'bodyDepth', csv: 'BD', label: 'Body Depth', low: 'Shallow', high: 'Deep', group: 'Frame' },
  { key: 'dairyForm', csv: 'DF', label: 'Dairy Form', low: 'Tight rib', high: 'Open rib', group: 'Frame' },
  { key: 'rumpAngle', csv: 'RA', label: 'Rump Angle', low: 'High pins', high: 'Sloped', group: 'Frame', note: 'Positive values slope more from hooks to pins.' },
  { key: 'rumpWidth', csv: 'RW', label: 'Rump Width', low: 'Narrow', high: 'Wide', group: 'Frame' },
  { key: 'rearLegsSide', csv: 'LS', label: 'Rear Legs—Side', low: 'Posty', high: 'Sickle', group: 'Feet & Legs' },
  { key: 'rearLegsRear', csv: 'LR', label: 'Rear Legs—Rear', low: 'Hock-in', high: 'Straight', group: 'Feet & Legs' },
  { key: 'footAngle', csv: 'FA', label: 'Foot Angle', low: 'Low', high: 'Steep', group: 'Feet & Legs' },
  { key: 'feetLegsScore', csv: 'FLS', label: 'Feet & Legs Score', low: 'Low', high: 'High', group: 'Feet & Legs' },
  { key: 'foreUdderAttachment', csv: 'FU', label: 'Fore Udder Attachment', low: 'Loose', high: 'Strong', group: 'Udder' },
  { key: 'rearUdderHeight', csv: 'UH', label: 'Rear Udder Height', low: 'Low', high: 'High', group: 'Udder' },
  { key: 'rearUdderWidth', csv: 'UW', label: 'Rear Udder Width', low: 'Narrow', high: 'Wide', group: 'Udder' },
  { key: 'udderCleft', csv: 'UC', label: 'Udder Cleft', low: 'Weak', high: 'Strong', group: 'Udder' },
  { key: 'udderDepth', csv: 'UD', label: 'Udder Depth', low: 'Deep', high: 'Shallow', group: 'Udder' },
  { key: 'frontTeatPlacement', csv: 'FT', label: 'Front Teat Placement', low: 'Wide', high: 'Close', group: 'Udder' },
  { key: 'rearTeatPlacement', csv: 'RT', label: 'Rear Teat Placement', low: 'Wide', high: 'Close', group: 'Udder' },
  { key: 'teatLength', csv: 'TL', label: 'Teat Length', low: 'Short', high: 'Long', group: 'Udder' }
]

export const genomicSummaryFields = [
  ['TPI', 'TPI', 'Overall Holstein performance index'], ['NM$', 'NM$', 'Lifetime profit estimate'],
  ['DWP$', 'DWP$', 'Dairy Wellness Profit index'], ['CA$', 'CA$', 'Calf wellness and profit index'],
  ['MILK', 'Milk PTA', 'Predicted transmitting ability for milk pounds'], ['FAT', 'Fat PTA', 'Predicted transmitting ability for fat pounds'],
  ['PROT', 'Protein PTA', 'Predicted transmitting ability for protein pounds'], ['DPR', 'DPR', 'Daughter pregnancy rate'],
  ['SCS', 'SCS', 'Somatic cell score; lower is generally preferred'], ['FE', 'Feed Efficiency', 'Expected production value relative to feed needs'],
  ['RFI', 'Residual Feed Intake', 'Expected feed intake beyond predicted needs; lower is more efficient'], ['MSPD', 'Milking Speed', 'Predicted milking speed'],
  ['TYPE FS', 'Type', 'Final score type PTA'], ['UDC', 'UDC', 'Udder composite'], ['BDC', 'Body Composite', 'Body-size composite'], ['FLC', 'FLC', 'Feet and legs composite']
] as const

export const genomicExtendedFields = [
  ['HCR', 'Heifer Conception', 'Higher values indicate better heifer conception rate'], ['CCR', 'Cow Conception', 'Higher values indicate better cow conception rate'],
  ['FI', 'Fertility Index', 'Combined measure of daughter pregnancy, cow conception, and heifer conception'], ['PL', 'Productive Life', 'Expected productive-life advantage'],
  ['LIV', 'Livability', 'Expected ability of daughters to remain alive in the herd'], ['HCC', 'Health Cost Index', 'Expected health-cost advantage'],
  ['SCE', 'Sire Calving Ease', 'Lower values generally mean fewer difficult births when used as a sire'], ['DCE', 'Daughter Calving Ease', 'Lower values generally mean daughters calve more easily'],
  ['SSB', 'Sire Stillbirth', 'Lower values are preferred'], ['DSB', 'Daughter Stillbirth', 'Lower values are preferred'],
  ['GL', 'Gestation Length', 'Expected gestation-length difference'], ['EFC', 'Early First Calving', 'Expected tendency for daughters to calve earlier']
] as const

export const genomicIdentityFields = [
  ['Official ID', 'Official ID'], ['Breed', 'Breed'], ['Result Type', 'Result type'], ['Evaluation Date', 'Evaluation date'],
  ['Parentage Status', 'Parentage status'], ['Sire of Record Official ID', 'Sire official ID'],
  ['Dam of Record Official ID', 'Dam official ID'], ['Maternal Grandsire Official ID', 'Maternal grandsire official ID']
] as const

export function importedGenomicFields(record: any): Record<string, string> {
  try { return typeof record?.rawDataJson === 'string' ? JSON.parse(record.rawDataJson) : record?.rawDataJson ?? {} } catch { return {} }
}

export function numericTrait(source: any, trait: GenomicTrait) {
  const value = source?.[trait.key] ?? source?.[trait.csv]
  const number = Number(String(value ?? '').replace(/,/g, ''))
  return Number.isFinite(number) ? number : null
}

export function linearPosition(value: unknown) {
  const number = Number(value)
  return Number.isFinite(number) ? `${Math.max(2, Math.min(98, 50 + number * 14))}%` : '50%'
}
