# Enugu deployment seeding (Phase 6B) runbook

This runbook takes a fresh Pickly environment to the point where a sender in Enugu can quote and book a delivery
and an eligible rider is matched to it. All seed data is applied by the DatabaseMigrator's post-deploy scripts.
This folder only holds read-only tools and documentation; the migrator never executes it.

## What runs on every deployment

The migrator runs the following in order:

1. `Scripts/PreDeploy`
2. The EF Core migrations
3. Every top-level `Scripts/PostDeploy/*.sql`, in file-name order

| Script | Seeds | Status |
|---|---|---|
| `0001.template-seeder` | countries, NGN/USD, payment providers, `customer` type (existing) | approved |
| `0002.rider-stakeholder-type` | `rider` stakeholder type per tenant | approved |
| `0003.vehicle-categories` | MOTORCYCLE 10 kg, CAR 50 kg, VAN 300 kg (Mapbox `driving`) | approved |
| `0004.enugu-delivery-zones` | 10 Enugu zones (`ENU-*`) | **draft polygons, review pending** |
| `0005.enugu-locations` | 130 streets and 43 landmarks from OpenStreetMap | **draft, ground check pending** |
| `0006.enu-v1-pricing-policy` | ENU-V1 tariff: 3 vehicle rules, 100 directional zone rules | approved rates; **₦400/₦600 pairs pending** |
| `0007.test-rider-accounts` | 18 ready-to-use test rider accounts `piklytech+rider1..18@gmail.com` | **MVP dummy data, created in every environment; remove before launch** (section 11) |
| `0008.enugu-locations-expansion` | 51 reviewed OSM-node landmarks and a second provenance way for Annunciation Hospital Road | **draft, ground check pending** |

Because 0004–0006 run everywhere, review the open items in `GEOGRAPHIC-SOURCES.md` before the first production
deployment. ENU-V1 becomes immutable as soon as it exists in an environment.

## Idempotency: why every deployment is safe

The migrator keeps **no execution journal**. Every script runs on every deployment, so every script converges
instead of assuming a first run.

Each file is a single batch (no `GO`), which is one transaction. Each script validates its input first and then
writes; a failure rolls the whole file back, fails `/health/readiness` and stops later scripts. Each script also
takes a transaction-scoped advisory lock. Foreign keys are resolved through natural keys; generated GUIDs are never
hard-coded.

| Script | Natural key | On every rerun |
|---|---|---|
| 0002 rider type | `(TenantId, Key)` | `ON CONFLICT DO NOTHING` |
| 0003 vehicles | `Code` | Insert missing; update only attributes that differ. |
| 0004 zones | `(CountryCode, Code)` | Insert missing (active); update Name, Priority and Boundary only when they differ. IsActive and effective dates are never touched after creation. |
| 0005 locations | OSM `(Provider, FeatureId)` and normalized identity | Insert-only. Existing or equivalent locations are never modified or duplicated. |
| 0006 ENU-V1 | `Version` | Policy and all rules inserted together the first time; afterwards nothing changes. Tariff changes are new versions (ENU-V2). |
| 0007 test riders | account email | Create a missing account once (never reset an existing password, lockout or 2FA); converge profile, approval and zones to the roster; a no-op once converged. Skips rather than fails. |
| 0008 location expansion | OSM `(Provider, FeatureId)` and normalized identity | Insert reviewed node landmarks or reuse equivalent canonical locations; attach all reviewed OSM references without changing existing locations. |

**Verified with the real migrator:**
- Two deployments to a fresh database produce identical row counts and rewrite no rows.
- Test rider accounts are created on the first deployment; later deployments do nothing to them.
- Fixture drift (vehicle, service zone) is corrected on the next deployment.
- Invalid input aborts the file without partial data.

## 1. Configure the services

Use the existing appsettings sections, overridden by `__` environment variables. Never put secrets in SQL or in
committed files.

| Service | Required settings |
|---|---|
| DatabaseMigrator | `ConnectionStrings__PostgresWrite` (compose: `DATABASE_URL`). The role must be able to create the database (first run only) and the `postgis` extension; pre-create the extension on managed PostgreSQL. |
| WebAPI | `ConnectionStrings__PostgresWrite` / `PostgresRead` / `Redis`; `Jwt__SigningKey` (replace the template placeholder); `Locations__Geoapify__ApiKey`, `Deliveries__Routing__Geoapify__ApiKey`, `Locations__LocationIQ__ApiKey`, and `Deliveries__Routing__LocationIQ__ApiKey` (compose maps these from `GEOAPIFY_API_KEY` and `LOCATIONIQ_API_KEY`); `Riders__Matching__*` (appsettings defaults). |
| Jobs | Postgres, Redis, `Messaging__RabbitMq__*` (`RABBITMQ_*`), `Riders__Matching__*`, `Jobs__Riders__Matching__PollInterval` (10 s). `RiderMatchingProcessor` is always registered; matching only happens while Jobs runs. |
| Consumer | Postgres, Redis, RabbitMQ, `Notifications__Email__Mailtrap__ApiToken` (`MAILTRAP_TOKEN`). It sends the registration OTP to senders (test riders are created already confirmed). |
| Payments (WebAPI, Test B) | Not in compose. Add sandbox values as WebAPI environment variables: `Payments__Credo__PublicKey`, `__SecretKey`, `__CallbackUrl`, or `Payments__SafeHaven__ClientId`, `__ClientAssertion`, `__CallbackUrl`. The provider webhooks must reach `/api/v1/payments/webhooks/...`. |

Empty routing keys do not fail startup; quotes then fail with `routing_dependency_unavailable` after bounded provider failover.

## 2. Deploy

These commands run on the deployment server from the repository root.

```bash
# docker-compose.local.sh (gitignored) holds the real values. Create it from docker-compose.env.example.sh
# if it does not exist yet. "Local" also starts Postgres/PostGIS, Redis and RabbitMQ; "Cloud" uses managed ones.
./docker-compose.local.sh Local      # or: ./docker-compose.local.sh Cloud

# The migrator applies migrations and PostDeploy 0000-0008, then reports ready:
curl --fail http://localhost:8081/health/readiness
```

Redeploying, or restarting the migrator, is always safe.

## 3. Verify the seeded data (read-only)

Set up a psql entry point, then run the validation script:

```bash
# Local profile:
export PSQL="docker compose -f docker-compose.yml -f docker-compose.local.yml --profile local exec -T postgres \
  psql -U postgres -d BLVK.Pickly.LogisticsDb -v ON_ERROR_STOP=1"
# External database, from any host with Docker:
#   export PSQL="docker run --rm -i postgis/postgis:16-3.5-alpine psql $DATABASE_URL -v ON_ERROR_STOP=1"

cd src/BLVK.Pickly.Logistics.DatabaseMigrator/Diagnostics
$PSQL -f - < enugu-validation.sql      # must end with "Enugu validation passed."
```

The script prints PASS, WARN or FAIL for each check and exits non-zero on any FAIL. It covers:

- **Zones:** count, validity, SRID 4326, MultiPolygon, lon/lat order, same-priority overlap.
- **Locations:** counts, duplicate identities, coordinates outside Enugu, locations outside every zone or with an
  ambiguous top zone, the Annunciation hospital point and its connected road-segment references.
- **Vehicle categories:** codes, weight limits and Mapbox profiles.
- **Pricing:**
  - NGN currency; ENU-V1 base, distance and weight rates; vehicle rules;
  - N² matrix completeness and duplicate directional rules;
  - the tariff band checked against live zone adjacency;
  - ambiguous effective policies.

To review the polygons and the tariff, see `GEOGRAPHIC-SOURCES.md`. It covers sources and licence, the zone
method, a GeoJSON export query, the street exclusion list, the 10×10 matrix and open review items.

## 4. How ENU-V1 prices a delivery

```
Total = 1200                      base fee
      + 250 × Mapbox route km     (rounded to 2 dp, midpoint away from zero)
      + 50 × total parcel kg      (rounded to 2 dp)
      + vehicle charge            MOTORCYCLE 0 · CAR 1500 · VAN 3500
      + zone charge               same zone 0 · adjacent 200 · others 400/600 (directional rule)
```

The zone of each location is the highest-priority active zone that covers its representative point.

**Worked example.** Independence Avenue (ENU-IND) to UNEC (ENU-UWA), 3.5 kg, MOTORCYCLE, with a 5.35 km Mapbox
route: 1200 + 1337.50 + 175.00 + 0 + 200 = **₦2,912.50**.

## 5. Provision test senders (3–5)

Use the normal API only:

1. `POST /api/v1/authentication/registrations` with header `X-Tenant-Id: 1203d9d1-2a6b-48ef-9cc1-e561a23aff72`
   and a body of `email`, `password` and `confirmPassword`.
2. Read the 5-digit OTP from the Mailtrap inbox.
3. Confirm with `POST /api/v1/authentication/email-confirmations`.

Use addresses outside the `piklytech+rider…` range (for example `piklytech+sender1@gmail.com`). Keep passwords
in a password manager or the untracked `.env` next to `enugu-smoke.http`.

## 6. Test riders (18 accounts, created by the deployment)

Nothing to provision. `0007` creates `piklytech+rider1@gmail.com` … `piklytech+rider18@gmail.com` on deployment,
already email-confirmed, so each can sign in straight away with
`POST /api/v1/authentication/sessions`. The shared test password is held by the product owner and is not
recorded in this repository.

For each account, `0007`:
- creates the login and a verified stakeholder of type `rider` in the default tenant;
- creates the RiderProfile (Offline), with the fixture's approval and vehicle;
- sets the fixture's service zones.

There is no rider onboarding API in Phase 6. `0007` mirrors `AppUser.Create`/`MarkEmailVerified`,
`Stakeholder.MarkVerified`, and `RiderProfile.Create`/`Approve`/`ChangeVehicleCategory`.

An account that already exists is never recreated, and its password, lockout and 2FA are never reset.

The script also skips:
- any account that has sender quotes or deliveries;
- a vehicle change while the rider has an active delivery;
- an `unapproved` fixture that is already Approved.

**Roster:**

| # | Role | Vehicle | Approval | Service zones | Start position (seeded location) |
|---|---|---|---|---|---|
| 01–10 | eligible | MOTORCYCLE | Approved | one each: IND, OGU, NHV, GRA, TEK, ABK, THC, ACH, UWA, EME | Okpara Square, Nnamdi Azikiwe Stadium, Neo Hospital, Polo Park Mall, Christ The King Hospital, Abakpa Market, National Orthopedic Hospital, Ogbete Main Market, UNEC, Akanu Ibiam Airport |
| 11 | eligible-car (wrong vehicle for motorcycle jobs) | CAR | Approved | IND, OGU, NHV | Hotel Presidential (6.4413529, 7.5125018) |
| 12 | eligible-car | CAR | Approved | GRA, OGU, TEK | National Museum of Unity (6.4530724, 7.4866157) |
| 13 | eligible-van | VAN | Approved | IND, OGU, NHV, UWA | Unity Park (6.4412500, 7.5215947) |
| 14 | missing-service-zone | MOTORCYCLE | Approved | EME only | Okpara Square (6.4410453, 7.5191522) |
| 15 | unapproved | MOTORCYCLE | Pending | IND | Okpara Square |
| 16 | offline | MOTORCYCLE | Approved | IND | Okpara Square (never enable availability) |
| 17 | stale-location | MOTORCYCLE | Approved | IND | Okpara Square (enable, then stop reporting for > 2 min) |
| 18 | active-assignment | MOTORCYCLE | Approved | IND | Unity Park (complete Test A with this rider; it then stays busy) |

To get exact start coordinates for any fixture, query its seeded location:
`SELECT "AddressLine", ST_Y("Coordinates"::geometry), ST_X("Coordinates"::geometry) FROM locations."Locations" WHERE "AddressLine" = '<name>';`

## 7. Refresh rider GPS presence

Positions expire after `Riders:Matching:LocationFreshness` (2 min), so presence is never seeded. As the rider:

1. Sign in.
2. `PUT /api/v1/riders/me/location` with `{ latitude, longitude, capturedAtUtc: <now UTC> }`.
3. `PUT /api/v1/riders/me/availability` with `{ "isAvailable": true }`.
4. Repeat the location call at least every 2 minutes. Capture times must increase, and may be at most 30 s in the
   future.

Bring online only the riders a scenario names. The nearest eligible rider gets the offer.

## 8. Booking-to-assignment tests (`enugu-smoke.http`)

- **Test A, PayOnDelivery (A1–A16):** quote → `PendingAssignment` → `piklytech+rider1` online → offer within one Jobs poll →
  accept within 30 s → `RiderAssigned`, with the rider's name on the sender's delivery.
- **Test B, PayBeforeDelivery (B1–B5):** book → `AwaitingPayment` → pay through the Credo or SafeHaven sandbox →
  verified webhook → `PendingAssignment` → assignment. Never mark payments paid in SQL.
- **Test C, exclusions:** use a fresh motorcycle delivery from ENU-IND, with riders 11, 14, 15, 16 and 17 set up as
  in the roster (rider15 gets a 409 when enabling availability). None of them should receive an offer; rider 1
  then should. Run `rider-matching-diagnostics.sql` to see why each rider was skipped.
- **Test D, scheduled dispatch (D1–D3):** a Scheduled delivery 35 minutes ahead stays `AwaitingDispatch`, with no
  offers, until `scheduledPickupAtUtc − 30 min`. Jobs then opens dispatch and matches it.

## 9. Diagnose failed matching

```bash
$PSQL -f - < rider-matching-diagnostics.sql                                  # readiness of every rider
$PSQL -v delivery_id=<deliveryId> -f - < rider-matching-diagnostics.sql     # one delivery
```

The script prints the delivery's status, origin zone, session attempts, radius and offer history. For every rider
it gives the first failing rule, in the order matching applies them:

1. tenant
2. verified rider
3. approved
4. exact vehicle
5. service zone for the pickup zone
6. availability
7. fresh position
8. busy
9. pending offer
10. search radius

**If no rider appears eligible:**

- Confirm Jobs logs `Rider matching iteration …` every poll.
- Check that the delivery is `PendingAssignment`.
- Check that the rider's position is fresh.

## 10. Changing seeded data later

- **Zones:** edit the WKT in 0004 and deploy; boundaries realign in place. Then rerun the validation.
- **Locations:** 0005 is insert-only. Add rows to add locations. Correcting an existing location is a deliberate,
  reviewed data change, not a script edit.
- **Tariff:** never edit ENU-V1 once it exists anywhere. Add a new script (for example `0008.enu-v2-pricing-policy`)
  that creates ENU-V2 with a later `EffectiveFromUtc` and closes ENU-V1's period.
- **Test riders:** edit the roster in 0007 and deploy; each test rider converges to the new roster.

## 11. Removing the test riders before launch

The test riders are MVP dummy data: they exist in every environment and share one known password. To remove them:

1. Delete `Scripts/PostDeploy/0007.test-rider-accounts.postdeploy.sql` and deploy. Otherwise the next deployment
   recreates the accounts.
2. In each environment, run:

   ```bash
   $PSQL --single-transaction -f - < remove-test-rider-accounts.sql
   ```

The removal script only affects users created by 0007 (`CreatedBy = 'system:test-rider-fixture'`). It revokes their
sessions and refresh tokens, then soft-deletes the user, stakeholder, rider profile, service zones and position.
Historical deliveries keep their rider reference.

It changes nothing while a test rider holds a pending offer or an active delivery, and rerunning it is safe.
