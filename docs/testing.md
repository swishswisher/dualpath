# Validation

## Recorded results — 2 November 2026

| Check | Result |
|---|---|
| .NET 10 Release solution build | Passed, zero warnings and errors |
| Full Release test suite | 81 passed, zero failures or skipped tests |
| Database | Real PostgreSQL 18 on an isolated Neon development project; uniquely named, verified schema per run |
| Concurrent last-seat acceptance | One accepted member, one capacity rejection; no overselling |
| Concurrent completion | Two saved attempts, one completion |
| Paid entitlement boundaries | Expired/missing periods and personal-plan access to private institution courses rejected |
| Provider contracts | Synthetic HTTP responses and signed billing fixtures; no real customer payment/email/AI call |
| Dependency vulnerability query | No known vulnerable production packages reported by NuGet at validation time |
| Release publish | Generated framework-dependent .NET output |
| Visual review | Actual Razor-rendered HTML snapshots: public, student and institution layouts; desktop and 390px mobile; mobile navigation opened successfully |
| GitHub Actions | No workflow files; no hosted Actions runs used |

The managed preview host could not initialise CoreCLR (`0x8007000E`), so browser QA used HTML exported from the working ASP.NET HTTP test server. Snapshot forms are inert; they are not a live application. Functional form submissions, authentication, database writes and sessions were exercised through real HTTP integration tests. A compatible deployed .NET host still needs live browser, email delivery, provider sandbox and recovery smoke tests before commercial launch. Docker image execution could not be checked here because Docker was unavailable.

Direct database TCP was unavailable in this executor. The real PostgreSQL run used Npgsql through a local development transport carrying PostgreSQL wire messages over verified Neon WSS, with GSS negotiation disabled on that local leg. Production remains ordinary .NET/Npgsql/PostgreSQL; it does not depend on the test transport. An earlier local PGlite run also passed the pre-concurrency suite; the authoritative final suite used real PostgreSQL.

## Run the same checks

Set `DUALPATH_TEST_DATABASE` to a dedicated PostgreSQL database, then run `./scripts/check.sh`. Never select production. Tests create a unique schema and verify `current_schema()` before proceeding. They do not drop pre-existing schemas. Remove old `dp_test_*` schemas through your normal development-database maintenance process after reviewing them.

Tests cover public routes, role workspaces, authentication and antiforgery, verification/recovery tokens, stored text escaping, persistence across logout, institution boundaries and mixed roles, private files, publishing snapshots, submissions/revisions/reviews, invitations/capacity/revocation, billing signatures/replay/stale events, provider failure and concurrent writes.

The visual export test writes synthetic HTML snapshots into ignored `artifacts/ui/`. It does not publish them. Production demo seeding is rejected by startup validation.

## Browser release checklist

On the actual release candidate, verify keyboard navigation, focus visibility, mobile menus, zoom and form errors. Register and confirm an account, save a goal, complete a lesson, save and submit an assignment with an attachment, review it from the assigned instructor account, revise and resubmit, and confirm progress survives a new session. Try a second institution to verify its records are inaccessible.

Verify email confirmation and password recovery arrive through the configured sender. In Stripe sandbox, complete checkout, replay a webhook, change a plan, schedule cancellation, fail a renewal, and verify access follows server-side subscription state. Test AI with the configured model and consent, then withdraw consent and verify no new answer is sent. Confirm live billing stays disabled until the operator deliberately enables it.

Perform and record a database/private-file restore drill and ensure the restored application can authenticate and download a known private attachment. These host/provider checks require operational access and must not be replaced with simulated success banners.
