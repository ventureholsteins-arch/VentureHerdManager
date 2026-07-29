# Codex task: reconcile paper herd records with Venture Herd Manager

The CSV files in this directory are transcribed from current handwritten herd records. Treat the dates in `breedings.csv` as **bred dates**.

## Goal
Reconcile the existing Venture Herd Manager database/model with these records and implement a safe way to create missing animals, breeding history, and embryo/ET records while preserving existing correct data.

## Source files
- `animals.csv` — animals/statuses visible in the handwritten records.
- `breedings.csv` — breeding history. Multiple rows for one animal are intentional and must remain historical records.
- `embryos.csv` — embryo/ET records that must be created and linked to the recipient animal.

## Required behavior
1. Inspect the existing .NET API, EF Core models/migrations, seed/import code, and Vue UI before changing schema or behavior. Reuse the current domain model where possible.
2. Reconcile by normalized animal name, but do **not** silently merge questionable names. Produce/report unmatched and ambiguous animals.
3. Do not delete or overwrite valid historical breeding events. Add missing events and update only records that can be matched safely.
4. Dates in `breedings.csv` are bred dates. Preserve them exactly.
5. For `embryos.csv`, each row represents an embryo/ET record that needs to be associated with the listed recipient/linked animal.
6. If an embryo recipient/heifer does not exist, create that animal first, then create the embryo/implant record and link it to that animal. Use sensible existing defaults for a newly created heifer and do not invent registration numbers, birth dates, pedigrees, or other unknown values.
7. The embryo record must retain embryo dam, embryo sire, mating, implant/bred date, recipient, and outcome/status if the current model supports them. If the current model cannot represent these relationships safely, extend it with an EF Core migration and API/UI changes.
8. Prevent duplicate imports. Running the reconciliation/import twice must not create duplicate animals, breeding events, or embryo implants.
9. Preserve paper status notes such as PG, Dry, Calved, and conflicting notes for review. Do not automatically resolve conflicts like Pixie without evidence from the existing database/domain logic.
10. Where a paper row is ambiguous, flag it for review rather than guessing.

## Deliverables
- Implement the reconciliation/import in a maintainable way appropriate for this project (seed/import command, admin endpoint, script, or equivalent).
- Add any required EF Core migration.
- Add/update API DTOs/services/controllers and Vue UI if necessary so embryo records are linked and viewable on the animal.
- Add edit/delete support only where consistent with existing application patterns; do not create destructive behavior without confirmation.
- Add tests for duplicate prevention, missing-recipient creation, breeding-history preservation, and embryo-to-recipient linking.
- Produce a reconciliation summary: matched animals, newly created animals, breeding events added, embryo records added, conflicts/ambiguous rows, and skipped duplicates.

## Important
Do not run destructive production database operations. Build and test the changes first. If live Azure SQL changes require credentials or an explicit production action, stop after producing the tested migration/import and clearly state what command/action remains.
