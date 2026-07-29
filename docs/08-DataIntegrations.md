# Data integrations

## Current release decision

Production login and role-based access are intentionally deferred for the
owner-only phase. They remain a required backlog item before broad client
rollout. The public demo must stay separate from production data.

## VAS / DairyComp

VAS provides a Developer API for partner software. It supports importing and
exporting herd data, including animals, reproduction records and insemination
or embryo-transfer information. It is not a self-service public API.

To enable it:

1. Contact `integrations@vas.com`.
2. Complete VAS partner onboarding and the integration agreement.
3. Receive a client ID, client secret and API key.
4. Confirm which read and write endpoints are included in the agreement.
5. Store all credentials in Azure configuration/Key Vault, never in this
   repository or the browser application.

Official documentation:

- https://developer-api-docs.vas.com/Content/GettingStarted/APIGettingStarted.htm
- https://developer-api-docs.vas.com/Content/GettingStarted/Authentication.htm

## CDCB WebConnect

CDCB has a third-party bearer-token API through WebConnect, but access and data
use are controlled. Private on-farm use does not automatically make every CDCB
dataset or commercial use free. Request the exact fields and use case, obtain
approval, and keep source-provider authorization where required.

Do not add CDCB credentials or retrieved non-public data to the public GitHub
repository. Build the connector only after CDCB confirms the approved scope.

Official documentation:

- https://redmine.uscdcb.com/projects/webconnect-production/wiki
- https://redmine.uscdcb.com/projects/cdcb-policies-and-regulations/wiki/External_Data_Requests

## NAAB sire catalog

NAAB publishes comma-delimited AISS database files and their field layout. The
app now has:

- an idempotent AISS importer;
- a searchable sire catalog;
- core production, fertility, calving and type traits;
- a report of sires actually used in herd breeding history; and
- exact-name/short-name/NAAB-code matching between herd records and catalog
  entries.

The catalog is reference data. Historical `SireUsed` text remains unchanged.
Importing an updated file updates catalog evaluations without duplicating
sires.

The import endpoint is locked unless `SireCatalog:ImportKey` is configured.
Send that value only in the `X-NAAB-Import-Key` request header. Do not commit
the key or the full downloaded database file.

Official files:

- https://www.naab-css.org/database-files
- https://www.naab-css.org/dairy-cross-reference

Before distributing NAAB data outside the farm or bundling the full catalog in
a public client, confirm redistribution terms with NAAB.

## Holstein EASY ID

Holstein EASY ID accepts manual entry and imports from supported herd
management programs, then submits registrations to Holstein. Holstein's public
site does not publish a general REST API or a universal third-party file
contract.

The app therefore creates an **EASY ID preparation CSV** containing calves and
heifers that still need registration work. It includes recorded identity,
birth, sex, sire, dam and source-calving fields, plus a review warning for
missing information. It does not guess official IDs, ET status, ownership or
other unknown values.

This is semi-automatic today:

1. Download the registration preparation CSV.
2. Review and complete the highlighted missing fields.
3. Confirm the current import mapping with Holstein EASY support.
4. Import or enter the reviewed records in EASY ID and submit there.

Automatic direct submission should not be claimed until Holstein provides and
approves a supported integration specification.

Official information:

- https://www.holsteinusa.com/software/easy.html
- https://www.holsteinusa.com/animal_id/register.html
