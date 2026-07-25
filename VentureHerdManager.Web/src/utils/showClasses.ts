export function parseDateOnly(value: string | null | undefined): Date | null {
  if (!value) {
    return null
  }

  const parts = value.split('-').map(Number)
  if (parts.length !== 3 || parts.some(Number.isNaN)) {
    return null
  }

  const [year, month, day] = parts
  return new Date(year, month - 1, day)
}

function toMonthDiff(start: Date, end: Date): number {
  let months =
    (end.getFullYear() - start.getFullYear()) * 12 +
    (end.getMonth() - start.getMonth())

  if (end.getDate() < start.getDate()) {
    months -= 1
  }

  return Math.max(0, months)
}

function inRange(date: Date, start: Date, end: Date): boolean {
  return date >= start && date <= end
}

function febEnd(year: number): number {
  return new Date(year, 2, 0).getDate()
}

export function formatCurrentAge(birthDate: string | null | undefined): string {
  const birth = parseDateOnly(birthDate)
  if (!birth) {
    return 'Unknown'
  }

  const months = toMonthDiff(birth, new Date())
  const years = Math.floor(months / 12)
  const remainderMonths = months % 12

  if (years <= 0) {
    return `${remainderMonths}m`
  }

  return `${years}y ${remainderMonths}m`
}

export function getShowClassLabel(
  birthDate: string | null | undefined,
  animalStage?: number | null,
  referenceDate = new Date()
): string {
  const birth = parseDateOnly(birthDate)
  if (!birth) {
    return 'Class TBD'
  }

  const year = referenceDate.getFullYear()
  const ageMonthsAtShow = toMonthDiff(birth, referenceDate)
  const isInMilk = animalStage === 3

  const mar1Current = new Date(year, 2, 1)
  const dec1Prev = new Date(year - 1, 11, 1)
  const febPrevEnd = new Date(year, 1, febEnd(year))
  const sep1Prev = new Date(year - 1, 8, 1)
  const nov30Prev = new Date(year - 1, 10, 30)
  const jun1Prev = new Date(year - 1, 5, 1)
  const aug31Prev = new Date(year - 1, 7, 31)
  const mar1Prev = new Date(year - 1, 2, 1)
  const may31Prev = new Date(year - 1, 4, 31)

  const dec1TwoBack = new Date(year - 2, 11, 1)
  const febOneBackEnd = new Date(year - 1, 1, febEnd(year - 1))
  const sep1TwoBack = new Date(year - 2, 8, 1)
  const nov30TwoBack = new Date(year - 2, 10, 30)

  const dec1ThreeBack = new Date(year - 3, 11, 1)
  const febTwoBackEnd = new Date(year - 2, 1, febEnd(year - 2))
  const sep1ThreeBack = new Date(year - 3, 8, 1)
  const nov30ThreeBack = new Date(year - 3, 10, 30)
  const jun1TwoBack = new Date(year - 2, 5, 1)
  const aug31TwoBack = new Date(year - 2, 7, 31)
  const mar1TwoBack = new Date(year - 2, 2, 1)
  const may31TwoBack = new Date(year - 2, 4, 31)

  const mar1ThreeBack = new Date(year - 3, 2, 1)
  const aug31ThreeBack = new Date(year - 3, 7, 31)
  const sep1FourBack = new Date(year - 4, 8, 1)
  const febThreeBackEnd = new Date(year - 3, 1, febEnd(year - 3))

  const sep1FiveBack = new Date(year - 5, 8, 1)
  const aug31FourBack = new Date(year - 4, 7, 31)
  const sep1SixBack = new Date(year - 6, 8, 1)
  const aug31FiveBack = new Date(year - 5, 7, 31)

  if (birth >= mar1Current && ageMonthsAtShow >= 4) {
    return 'Spring Heifer Calf'
  }

  if (inRange(birth, dec1Prev, febPrevEnd)) {
    return 'Winter Heifer Calf'
  }

  if (inRange(birth, sep1Prev, nov30Prev)) {
    return 'Fall Heifer Calf'
  }

  if (isInMilk && inRange(birth, sep1TwoBack, aug31Prev)) {
    return 'Yearling Heifer in Milk'
  }

  if (inRange(birth, jun1Prev, aug31Prev)) {
    return 'Summer Yearling Heifer'
  }

  if (inRange(birth, mar1Prev, may31Prev)) {
    return 'Spring Yearling Heifer'
  }

  if (inRange(birth, dec1TwoBack, febOneBackEnd)) {
    return 'Winter Yearling Heifer'
  }

  if (inRange(birth, sep1TwoBack, nov30TwoBack)) {
    return 'Fall Yearling Heifer'
  }

  if (inRange(birth, jun1TwoBack, aug31TwoBack)) {
    return 'Summer Junior Two-Year-Old Cow'
  }

  if (inRange(birth, mar1TwoBack, may31TwoBack)) {
    return 'Spring Junior Two-Year-Old Cow'
  }

  if (inRange(birth, dec1ThreeBack, febTwoBackEnd)) {
    return 'Winter Senior Two-Year-Old Cow'
  }

  if (inRange(birth, sep1ThreeBack, nov30ThreeBack)) {
    return 'Fall Senior Two-Year-Old Cow'
  }

  if (inRange(birth, mar1ThreeBack, aug31ThreeBack)) {
    return 'Junior Three-Year-Old Cow'
  }

  if (inRange(birth, sep1FourBack, febThreeBackEnd)) {
    return 'Senior Three-Year-Old Cow'
  }

  if (inRange(birth, sep1FiveBack, aug31FourBack)) {
    return 'Four-Year-Old Cow'
  }

  if (inRange(birth, sep1SixBack, aug31FiveBack)) {
    return 'Five-Year-Old Cow'
  }

  if (birth < sep1SixBack) {
    return 'Six-Year-Old & Over Cow'
  }

  return 'Class TBD'
}