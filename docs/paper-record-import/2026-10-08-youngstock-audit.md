# Calf and heifer paper audit — 2026-10-08

This audit compared the two owner-supplied handwritten calf/heifer sheets with the live production herd list. Horn and RFID notes were intentionally ignored. No birth dates, registration numbers, RFID values, or pedigree details were invented.

## Production changes made

| Paper record | Result |
|---|---|
| Chico x Detective | Missing. Created female calf `Unnamed - Chico x Detective` (animal 3137). |
| Cinnabun x Major | Missing. Created female calf `Unnamed - Cinnabun x Major` (animal 3138). |
| Swish x Energy | Missing. Created female calf `Unnamed - Swish x Energy` (animal 3139). |
| Leddy | Missing. Initially transcribed as Lady; owner corrected the name to `Leddy`. Created female heifer (animal 3140) and corrected the card. |
| Cashin x unknown sire | Missing. Owner confirmed the sire is not known yet. Created female calf with dam name `Cashin`, no sire, and no invented identifiers (animal 3141). |
| Shila x unknown sire | Missing. Created female calf with dam name `Shila`, no sire, and no invented identifiers (animal 3142). |
| Clover x Master | Existing `Unnamed - Clover 1` matched by dam. Added sire `Master`; did not create a second animal when the same pairing appeared twice on the paper. |

## Existing records matched — no duplicate created

| Paper entry | Existing live record used |
|---|---|
| Seashell x Dropbox — Seabreeze | Seabreeze; sire Dropbox |
| Sophie/Sophia x Image | Unnamed - Sophia x Image |
| Capri x Image | Unnamed - Carri x Image (spelling conflict retained for correction) |
| Sunset x Image | Unnamed - Sunset x Image |
| Pike x Image | Unnamed - Pike x Image |
| Pella x McCutchen | Pella x McCutchen |
| Sammi/Sammie x Detective | Sammie x Detective |
| Seashore x Major | Seashore x Major |
| Conquor x Master | Unnamed - Conquor x Master |
| Summer x Master | Unnamed - Summer x Master |
| Cardi B | Cardi B |
| Conquor x Jinx | Unnamed - Conquor x Jinx |
| Seashell x Master | Unnamed - Seashell x Master |
| Polly x Goldwyn | Pyrra; verified dam Polly and sire Goldwyn |
| Chico x Hulu | Chatter; verified dam Chico and sire Hulu |
| Alia x Salute | Unnamed - Alia x Salute |
| Colleen | Colleen |
| Sarah x Master — Starlet | Starlet; sire Master |
| Shaylee x Tattoo — Solo/Sola | Sola; verified dam Shaylee and sire Tattoo |
| Pixie x Master — Prada | Prada; sire Master |
| Seashell x Lambda | Unnamed - Seashell x Lambda |
| Cade x Energy | Unnamed - Cade x Energy |
| Savoy | Savoy |
| Crown | Crown Jewel |
| Cadence | Cadence |
| Status | Existing Status card; dam remains Seashell. |
| Crayola | Crayola |
| Solara Jet | Solara Jet |
| Palace | Palace |
| Shila | Shila |
| Cassia | Cassia |
| Crush | Crush |
| Charm | Charm |
| Cenza | Cenza |
| Chanel | Chanel |

## Intentionally unresolved

- `Cashin x unknown sire`: owner confirmed there is no sire information yet. The calf was created without a sire; Cashin is not currently an animal card and therefore cannot yet have a live dam relationship.
- `Shila x unknown sire`: created as animal 3142. Production currently retains the exact dam name; the accompanying pedigree-link code resolves a unique existing animal named Shila into a durable `DamId` when the record is saved after deployment.
- The standalone word `Bull` beneath the Pixie x Master / Prada entry may be a sex note, but its target is not unambiguous. Prada was not changed.
- `Capri x Image` appears to be the existing `Unnamed - Carri x Image`. The likely `Carri`/`Capri` typo was recorded as a conflict rather than silently changing pedigree data.
- `Mystery Calf` remains separate because it has no identifying pedigree and cannot safely be merged with any paper calf.

## Duplicate protection decisions

- Exact and close name matches were checked across all herd locations, not only the home-herd filter.
- Pedigree was opened and verified for Chatter, Pyrra, Sola, and the Clover calf.
- The repeated `Clover x Master` line was treated as one animal because the sheet provided no second name, date, or identifier.
- Existing unnamed pedigree animals were retained instead of creating better-spelled duplicates.
