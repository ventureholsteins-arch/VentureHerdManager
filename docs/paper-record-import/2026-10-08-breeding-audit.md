# Follow-up breeding paper audit — 2026-10-08

Every date below is treated as a **bred/implant date**. Earlier services are history and must not be replaced by a newer service. The importer matches animal + exact date + normalized sire/mating and skips an exact duplicate.

## High-confidence ordinary breedings

| Date | Animal | Sire | Import decision |
|---|---|---|---|
| 2026-07-05 | Catalina | Eye Candy | Already represented in the original paper file; duplicate-safe. |
| 2026-07-08 | Emmy | Image | Added to `breedings.csv`. |
| 2026-07-30 | Sarah | Master | Added to `breedings.csv`. |
| 2026-07-30 | Catalina | Image | Added to `breedings.csv`. |
| 2026-08-15 | Pike | Flash | Already represented; duplicate-safe. |
| 2026-08-18 | Pixie | Flash | Already represented; duplicate-safe. |
| 2026-08-20 | Sarah | Hellion | Already represented; duplicate-safe. |
| 2026-08-23 | Summer | Image | Added to `breedings.csv`. |
| 2026-08-24 | Cookie | Tattoo | Added to `breedings.csv`; visible on the production dashboard and expected to be skipped as an exact duplicate. |
| 2026-08-24 | Bandi | Tattoo | Added to `breedings.csv`; visible on the production dashboard and expected to be skipped as an exact duplicate. |
| 2026-08-25 | Ace | Image | Added to `breedings.csv`; visible on the production dashboard and expected to be skipped as an exact duplicate. |
| 2026-08-26 | Chatter | Venmo | Added to `breedings.csv`; paper also identifies Chatter as Chico x Hulu. |
| 2026-09-10 | Shine | Tattoo | Added to `breedings.csv`. |
| 2026-09-15 | Alia | Venmo | Added to `breedings.csv`. |
| 2026-09-24 | Summer | Image | Added to `breedings.csv`; the 08-23 service remains history. |

## Embryo transfers

These lines are already represented in `embryos.csv` and must be reconciled as embryo inventory/implant records rather than plain AI breedings:

- 2026-07-15 Peach — Seashell × Dropbox
- 2026-07-15 Bandi — Polly × Goldwyn
- 2026-07-15 Carmella — Seashell × Legend
- 2026-08-09 Carmella — Carissa × Braxton
- 2026-08-09 Bandi — Carissa × Braxton
- 2026-08-12 Rose — Conquor × Master
- 2026-08-22 Peach — Polly × Goldwyn

Production may render an implant one calendar day earlier on some screens because the stored midnight value is converted from UTC to local time. That display defect must not be mistaken for a separate paper implant or used to create a duplicate.

## Held for review — not imported

- `2026-08-23 Serenity/Sorrelly — Seashell`: the handwriting/name is not reliable enough to choose an animal automatically.
- `2026-08-28 Carri/Carry — Image`: likely conflicts with the existing Capri/Carri spelling issue; do not guess.
- `2026-09-03 929-959 — Flash`: numeric identifier has not yet been matched to one animal card.
- Cropped lines above the visible portion of the sheet were not transcribed.

## Safe application

Run the paper-import preview first. Apply only while `PaperRecordImport:AllowApply` is deliberately enabled. The preview/apply report must be retained so duplicates skipped, missing animals, and conflicts remain auditable.
