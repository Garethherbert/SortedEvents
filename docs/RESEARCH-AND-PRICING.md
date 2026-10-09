# Research and proposed positioning — 9 October 2026

## Competitor benchmarks (supplier pages, not independently trialled)

- Aisle Planner: full planning starts at $69.99/month for up to 15 active projects. Sales-only $49.99/month. https://aisleplanner.com/pricing/
- eventplanner: free for one active event; Pro from £8.50/seat/month on annual billing. https://www.eventplanner.co.uk/eventsoftware/pricing.php
- Timeline Genius: $54.95/month, $549.95/year, $194.95/five-event pack. https://www.timelinegenius.com/pricing
- PerfectTablePlan: $29.95 Home, $74.95 Advanced, $299.95 Professional, one-time plus tax. Offline desktop seating. https://www.perfecttableplan.com/html/purchase.html
- Joy: free registry; gift tracking and editable digital thank-you notes already exist. https://withjoy.com/registry/ and https://withjoy.com/help/en/articles/8328353-sending-thank-you-notes

The code and visual design are original except for the user's KeepMyTravels reuse. Public competitor functionality informed requirements; competitor source code, branding and proprietary templates were not copied.

## Proposed price architecture

| Plan | Price | Scope |
|---|---|---|
| Trial | Free | One local trial event |
| Pro | £39.99 once | Purchased local version; 12 months of updates; unlimited local events |
| Event Pass | £24.99/event | One event; 12 months hosted service; 2 organisers; 5 GB; no renewal |
| Premium | £6.99/month or £69.99/year | 3 active events; 2 organisers; 10 GB total |
| Premium Plus | £11.99/month or £119.99/year | 10 active events; 5 organisers; 50 GB total |
| Business | £19.99/month or £199.99/year | 25 active events; 5 staff; 100 GB total |

Guests, viewers and assigned helpers are free contributors, not staff seats. They cannot independently create or administer client workspaces. Premium need not require a Pro purchase. Event Pass must include its hosted editor for one event, so someone without Pro can still use it.

Pro plus Event Pass fills the space between subscription-only products and perpetual software with no hosting. Buying Pro cannot fund indefinite galleries, mail delivery or storage. Subscriptions fund ongoing costs. Event Pass offers fixed-term sharing for infrequent organisers. These are proposed prices; actual willingness to pay, customer support, acquisition cost and storage economics remain unvalidated.

Tax presentation, international currency price bands, upgrade credits, grace periods, retention, refund terms and the subscription-only user's local editing rights must be decided before taking payment. No purchase flow or entitlement engine is implemented in this testing edition.

## Amazon findings

Amazon's current Creators API documents GetItems, SearchItems, GetVariations and GetBrowseNodes, with product attributes, offers and images—not a general registry purchaser-history API:
https://affiliate-program.amazon.com/creatorsapi/docs/en-us/api-reference

Login with Amazon grants customer profile scopes, not registry purchases:
https://developer.amazon.com/docs/login-with-amazon/authorization-code-grant.html

Amazon itself offers registry purchase marking and a gift tracker:
https://www.aboutamazon.com/news/retail/how-to-create-wedding-registry-amazon

Joy's supported retailer integrations demonstrate partner-specific arrangements and different purchase-tracking behaviours; those capabilities do not establish access for BigDaySorted:
https://withjoy.com/help/

Therefore this edition implements retailer links, manual gift entries and guest-recorded reservations/purchases. It does not scrape private pages or claim to confirm retailer orders. No affiliate revenue is assumed. Adding approved product lookup could help populate catalogue metadata later, but purchase verification needs a separately authorised partner integration.

## Product opportunities

- Personal plans that stay usable offline, combined with time-limited online sharing.
- Guest helpers can participate without paying or receiving organiser authority.
- Gift surprise protected in server responses and exports, not merely hidden visually.
- Multi-day setup/rehearsal/event/pack-down timeline with named responsibilities.
- Gallery moderation and invitation-specific access for every event.

None of these individual ideas is asserted to be unique. The opportunity lies in usability and their combination. Independent wedding coordinators and small celebration businesses are the starting audience. Concert/festival support is coordination only; ticket sales, certified safety workflows and access control require substantial further work.
