# Delivery status — 9 October 2026

| Requirement | Implemented in this edition | Remaining work |
|---|---|---|
| Website | Marketing pages, proposed prices, working browser organiser | Public audience/domain and commercial policies |
| PC / Android / Mac / iOS | .NET MAUI targets, native offline event/timeline/tasks/suppliers, export/import, transfer source | Compile, sign, install, device-test all four platforms |
| Reuse KeepMyTravels | Core, SQLite repository, booking parser, transfer engine | Reuse production auth/licensing/media services only after product isolation review |
| Branding | BigDaySorted; Big day? Sorted. | Trademark/domain clearance before launch |
| Free contributors | Invited helper/guest/honoree/gallery roles, server authorization | Public guest route on a suitably accessible deployment |
| Gift lists | HTTPS registry/product links, guest reservation/purchase status | Approved retailer API integrations and reconciliation |
| Big-day examples | Wedding, anniversary, birthday, multi-band concert, daily two-stage festival templates, charity, awards, corporate, graduation, community | Practitioner review of live-event templates |
| Competitor-informed features | Timelines, tasks, budgets, suppliers, RSVP, seating list, gifts, galleries | Visual floor plans, contracts/e-signatures, CRM, ticketing |
| Competitive tiers | Trial, Pro, Event Pass, Premium, Premium Plus, Business proposals | Payment provider, signed entitlements, tax/currency pricing |
| Gallery | Separate per event, guest submissions, moderation, viewing roles | Video, native uploader, direct downloads for all photo types, retention automation |
| Invites | Personal revocable links, RSVP/meal replies, draft and CSV export | Automated sender, bounced-email handling and reminders |
| Thank yous | Gift-linked editable post-event drafts | Automated email delivery, sent-status workflow, print card templates |
| Gift surprise | Server-side secrecy, no early totals/identities, protected date changes | Independent security review and honoree-owned registry provider behaviour |
| Granular day(s) | Multiple dates, band/stage line-ups, soundchecks and changeovers, minute timings, durations, tracks, locations, owners, dependencies, status and crew/stage overlap warnings | Dependency timing validation, drag-and-drop, native live sync |
| Extra features | Seating/meal list, audit trail, portable .bigday archives, offline native plans, QR transfer engine | Calendar reminders, QR check-in, accessible event microsites, production backup/restore |

## Release boundary

The completed browser workflows are available for private testing. All proposed tiers are marketing/configuration data; payment and licence limits are deliberately not asserted as live. The preview has no shared editing licence purchase requirement. Native source exists but has not been compiled or executed here. This report is the authoritative distinction between implemented, proposed and blocked items.

## First acceptance test

1. Create a Wedding named “Alex & Sam — test” for a future date.
2. Inspect and edit the running order; assign a moment and task to “Jamie”.
3. Add a supplier, cost, paid amount and due date. Check totals.
4. Create a helper invitation named “Jamie” and a guest invitation named “Taylor”.
5. Use a separate browser session for Taylor: RSVP, enter meal requirements, reserve a gift and submit a photo.
6. Confirm organiser/honoree gift screens do not show reservations, purchaser identities or early totals.
7. Approve the photo and create a gallery-only invitation. Confirm no documents, costs, gifts or guest records are accessible through it.
8. Revoke Taylor's invitation; existing token requests must fail immediately.
9. Save and reopen the event, print a running order, assign tables and export a .bigday archive.
10. Create a past event only for testing the post-event gift reveal and thank-you draft workflow. Never alter a real future event to test secrecy.

Private hosted access applies before these application roles. Guest invitation tests against external recipients require a public or appropriately shared deployment, which is not enabled by this delivery.

## Concert and festival update

New Concert templates include three example bands and production timings per selected day. Festival templates include two stages, six example acts per day, daily crew tasks, gates, welfare checks and overnight/final handovers. Templates cover 1–30 days; every name and timing is editable. The browser Bands & stages screen filters and prints daily programmes. Adding a band block creates linked soundcheck, changeover and performance moments. Running orders filter by stage/area and warn about overlapping sets on the same stage as well as crew assignments. Explicit public programme slots expose only programme details to guests; private notes, leads and soundchecks remain internal. Native models, templates and portable exports carry the same band fields, but native clients still require compilation/device testing and use their existing running-order screen.

## UI and daily planning update — 0.1.1 build 2

- Native source: consistent light-theme text, labelled inputs, larger readable buttons, scrollbars and Back controls. A sidebar/menu is available from overview, editors and Wi-Fi transfer screens; narrow screens toggle the menu.
- Setup: concerts/festivals ask 1–14 days, 1–5 stages and 1–12 bands in total for each day. Other occasions ask expected attendance. Answers survive exports/imports. Existing customised plans are not regenerated on editing setup.
- Online menus omit gifts/thank-yous and seating/meals for concerts/festivals. Existing gift data is preserved rather than deleted.
- Browser and native source provide a daily critical path based on duration and one predecessor per moment, with float and dependency timing warnings. Waiting gaps are excluded. Drag/drop and Move up/down resequence the selected day/area into consecutive times; dependencies stay intact, and organiser review is required for warnings. Native drag/drop remains unexecuted until platform builds are tested.
- Vector B/check logo replaces the previous plain checkmark in the website, native icon, splash and header.
- Automated browser/API and planning tests pass (24). Native build/signing/device verification remains outstanding. Only the first screenshot was attached.
