# ADR-001 - Animal Deletion Policy

## Status
Accepted

## Decision
Animals will never be permanently deleted from Venture Herd Manager.

Instead, animals will be marked with an AnimalStatus such as:

- Active
- Sold
- Deceased

## Search Rules

### Active Animals
Active animals appear normally in search results.

### Recently Sold Animals
Animals sold within the last year may still appear in search results, but they should:

- Display a SOLD badge
- Appear at the bottom of the list
- Show the sold date if available

### Older Sold Animals
Animals sold more than one year ago should not appear in normal searches.

They should still be searchable if:

- The user checks Include Sold Animals
- The user searches by registration number

## Reason
Animal records must stay in the system to preserve:

- Pedigree history
- Calving history
- Classification history
- Breeding history
- Embryo records
- Family tree relationships
- Sale history

Deleting an animal would break historical records and relationships.

## Future Notes
A sale should eventually become an AnimalEvent in the animal timeline.