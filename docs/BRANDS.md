# BigDaySorted and LiveDaysSorted

One maintained organiser source, two branded websites and native app builds.

- BigDaySorted: weddings, birthdays, anniversaries and other celebrations. Teal/amber B/check identity; invitations, seating, gifts and thank-you workflows.
- LiveDaysSorted: concerts and festivals. Midnight/violet/lime L/check identity; band line-ups, stage timings, soundchecks, changeovers and daily production paths.

`brands/config.mjs` owns event-type choices and brand names. Each brand has a landing page, theme and vector logo under `brands/`. The selected deployment is `.openai/brand.json`; the hosting project identity remains independently in `.openai/hosting.json`.

The event API, organiser frontend, privacy rules, timelines, critical paths, gallery and database schema are shared. Do not edit shared files separately in deployment checkouts. Make changes in the canonical BigDaySorted repository and prepare the LiveDaysSorted checkout with:

```
node scripts/export-brand.mjs live /absolute/path/to/LiveDaysSorted
```

This preserves the destination hosting manifest and Git repository. It does not copy signing keys, local event data or native app projects. Each deployed Site receives a source snapshot for reproducibility; these are deployment copies, not independent product implementations.

Build selected brand with `npm run build`. To test a LiveDaysSorted build inside the canonical repository without changing the active Site, use `npm run build:live`. `dist-live` is test output, not the BigDaySorted deployment artifact. Native apps now use the same C# implementation with separate build identities. See NATIVE-APPS.md for preparing and building both brands.

## Data and access

Each private Site has its own event database and file storage in this edition. This is shared code, not automatic cross-brand event synchronisation or a unified paid account system. Website access uses the platform's signed-in identity. Neither site has live payments or paid entitlement enforcement. External contributors need appropriately shared/public access before their application invitation links work.

Existing concert/festival plans in BigDaySorted remain accessible; new creation there is celebration-specific. Export a .bigday planning file and import it into LiveDaysSorted to move a plan. Invitations, photos, media, purchaser records and gift pledges are not migrated by a planning export. No existing records are silently deleted or moved.

Proposed prices and hosting limits remain proposals. Marketing mentions only implemented browser workflows; native downloads, ticketing, automatic emails, approved retailer purchase tracking, commercial policies and production security review remain release work.
