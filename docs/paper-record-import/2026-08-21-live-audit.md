# Live herd reconciliation audit — 2026-08-21

This is a read-only comparison of the handwritten August notes with the production API. No production data was changed.

## Embryo and implant findings

| Recipient | Paper record | Live finding | Reconciliation action |
|---|---|---|---|
| Carmella | 2026-07-15, Seashell x Legend, later marked open/ONS | Embryo 15 is linked and implanted; breeding history exists, but the outcome was not consistently represented | Import file now records **Did not stick** so the embryo becomes Failed and the breeding remains as Open history |
| Bandi | 2026-07-15, Polly x Goldwyn, later marked open/ONS | Embryo 16 exists in storage but is not linked; Bandi has no breeding history | Importer now reuses embryo 16, links it to Bandi, creates the ET history, and records Failed/Open |
| Peach | 2026-07-15, Seashell x Dropbox | Peach is missing; embryo 17 exists in storage but is not linked | Importer now creates Peach without invented details, reuses embryo 17, links the implant, and leaves outcome Unconfirmed |
| Carmella | 2026-08-09, Carissa x Braxton | Live 2026-08-09 record says Seashell x Legend; Carissa x Braxton embryos 19/20 are in storage | Owner confirmed ET. Prepared to reuse one stored Carissa x Braxton embryo and preserve the incorrect live record for audit/correction rather than deleting it |
| Bandi | 2026-08-09, Carissa x Braxton | No live breeding/implant; Carissa x Braxton inventory exists | Owner confirmed ET. Prepared to reuse the other stored Carissa x Braxton embryo |
| Rose | 2026-08-12, Conquor x Master | Not present in live history; matching inventory exists | Owner confirmed ET. Prepared to reuse the stored Conquor x Master embryo |

The importer is idempotent. It first looks for an exact linked implant; otherwise it consumes one matching unlinked inventory embryo. It never deletes old breeding history.

## Animal and breeding findings

- Matches: Azure, Coco, Caddie, Cellie, Snickers, Chico, and Emmy agree with the legible paper entries.
- Missing animals: Peach, Sorrelly, Catty, and Cinnabun/Cinnabar. Peach, Sorrelly, and Cinnabun are prepared for safe creation. Catty is marked sold and is not auto-created.
- Carmella: existing animal matched. The confirmed 2026-08-09 Carissa x Braxton ET is prepared; the conflicting live mating remains visible for correction/history review.
- Bandi: existing animal matched. Both the failed 2026-07-15 Polly x Goldwyn transfer and current 2026-08-09 Carissa x Braxton transfer are prepared.
- Rose: existing animal matched; the confirmed 2026-08-12 Conquor x Master ET is prepared.
- Shine: owner confirmed Open. Import will update the latest breeding to Open without deleting older history.
- Catalina: owner confirmed she is a cow; the import maps her to the app's cow/milking stage while preserving breeding history.
- Chico x Hulu: Chico is the dam and Hulu is the sire of an unnamed heifer. The heifer is not created because no identifying name was supplied; this avoids an unusable duplicate animal.

## UI/workflow changes in this pass

- Animal LUT action is available from the animal card and refreshes the card after saving.
- LUT animal search now searches names and pedigree and prioritizes milking cows in the initial list.
- Mobile animal card places quick actions, genomic/milk information, and pedigree near the top.
- Embryo batch creation now uses a stable form, supports quantity and group creation, and no longer relies on repeated phone prompts.
- Pregnancy Checks Due defaults to Milking; the existing All/Heifer/Dry filters remain available.
- Show-string cow grouping no longer incorrectly includes generic yearling heifer classes.

## Safe production step

Before applying, confirm the three August conflicts above. Then deploy the tested code and run the paper import in preview mode. Apply only after the preview counts and per-animal conflicts are reviewed. The import must not be run against production with an older API build because that build can duplicate stored embryos instead of linking them.
