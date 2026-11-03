# DualPath status

## Recovery

- Recovered on 3 November 2026 in Windows PowerShell from `origin/rebuild/dotnet-startup` at `254dd454c23d647f02c520f3d30da61ecd0d8522`. The remote branch matched the recovery reference and [draft PR #1](https://github.com/swishswisher/dualpath/pull/1).
- This is the .NET 10 Razor Pages, Identity, EF Core and PostgreSQL application. The earlier prototype lives in `legacy/nextjs-prototype/`.
- The local branch is `rebuild/dotnet-startup`. All changes are local commits. No GitHub Actions workflows or runs were used.

## Implemented and reviewed

- Verified the recovered public, student, instructor, institution and platform-operator entry points. Existing domain services implement lessons and assessments, assignments and revision history, reflections, invitations and seats, announcements and reporting, public content, support, billing and opt-in practice.
- Assignment drafts and their attachments are visible to their owners. Assigned instructors gain access on submission. Added a database-backed regression test.
- Authentication form limits use the actual Razor route, including case and optional item variants. Form data cannot override the route section. Added an HTTP regression test.
- Added opt-in forwarding from explicitly trusted reverse proxies. Restricted live Stripe keys require the live-billing flag. Signature verification rejects out-of-range timestamps without overflow.
- Mobile public and workspace menus support keyboard opening and Escape with focus restored to the trigger. Submission busy state clears when the browser restores a page. Kept the existing navy and sky-blue design.

## Local setup and evidence

- Workspace-local SDK **10.0.403**, verified against Microsoft's published SHA-512. Isolated PostgreSQL **18.4**, listening on loopback port **55438**. Development and tests use separate databases, and tests create a fresh schema.
- From this checkout in PowerShell, run `& ../work/start-dualpath.ps1` if the development server has stopped. The helper uses local files outside Git. Use `student@demo.dualpath.test`, `instructor@demo.dualpath.test`, `institution@demo.dualpath.test` or `admin@demo.dualpath.test` with the development password stored at `../work/runtime/demo-password`.
- `dotnet restore DualPath.sln --locked-mode` passed. `dotnet build DualPath.sln -c Release --no-restore /p:UseAppHost=false` passed with zero warnings and errors. The final Release test run passed **83 of 83** with no skips. See `artifacts/test-results/platform.trx`. Tests use synthetic provider responses and signed Stripe fixtures.
- `dotnet list src/DualPath.Web/DualPath.Web.csproj package --vulnerable --include-transitive --no-restore` found no vulnerable packages from the configured NuGet source at check time. `dotnet tool restore` restored EF 10.0.14.
- `dotnet publish src/DualPath.Web/DualPath.Web.csproj -c Release --no-restore -o artifacts/publish /p:UseAppHost=false` passed. Docker Desktop's Linux engine was unavailable, so local verification used the published .NET application and native PostgreSQL.
- Live Edge exercised public pages and role workspaces at 320px, keyboard focus and Escape, lesson completion, saved reflection, registration, confirmation and password reset through the development outbox. Public and authenticated layouts were also checked at 1280px.
- Edge verified that a public enquiry reached the operator workspace, a draft resource stayed private until publication, and an institution announcement reached an enrolled learner.
- Edge verified draft, multipart PDF upload, learner download, denial to the instructor before submission and to the platform admin, instructor download after submission, rubric feedback, revision request, resubmission and approval. Automated coverage includes institution and reflection isolation, seat races, and Stripe replay and entitlement rules.
- Verified restart persistence at [local development](http://127.0.0.1:5086/health/ready). Restored a PostgreSQL dump and a copy of private files and keys to a separate local database and instance at [local restore rehearsal](http://127.0.0.1:5088/health/ready). An existing browser session authenticated with the restored keys and opened the saved reflection, approved work and original PDF.
- Applied both EF migrations explicitly with `dotnet ef database update --project src/DualPath.Web --configuration Release --no-build` to a clean release rehearsal database. Ran the publish output in Production at [local HTTPS rehearsal](https://localhost:54486/health/ready) using a development certificate. Readiness returned 200 over TLS and through a simulated trusted HTTPS proxy hop. Direct HTTP redirected with 307. The thirteenth alternating sign-in POST returned 429.

## External staging and launch

- The authenticated Vercel project reports framework `nextjs` and its latest preview reports `ERROR`. Vercel's beta Container runtime can build Dockerfiles but scales down when idle. The beta Daemons service uses ephemeral storage and excludes preview deployments. The current application needs a continuously running outbox and durable private files and keys. No suitable authorized external staging target was available. The existing Vercel project and production deployment were left alone. [Container runtime](https://vercel.com/docs/functions/runtimes/container) and [Daemon limitations](https://vercel.com/docs/daemons#what-does-the-daemons-beta-support).
- Provide access to a dedicated staging VM or always-on .NET 10 container host with a staging HTTPS hostname and persistent writable `App_Data`. Use a separate PostgreSQL database with TLS verification, apply migrations through the release role, and configure a separate runtime role. Set `AllowedHosts`, `DualPath__BaseUrl`, `DualPath__TrustedProxies__0` if proxied, and temporary `DualPath__BootstrapAdminEmail` and `DualPath__BootstrapAdminPassword`. Run with `DualPath__SeedDemo=false` and `DualPath__ApplyMigrations=false` after migration. See [deployment](docs/deployment.md) and [configuration](docs/configuration.md).
- For email, supply a Resend API key and verified sender through `Email__ApiKey` and `Email__From`. Confirm real registration and password recovery delivery. For billing, supply Stripe sandbox API and signing secrets and three recurring price IDs, enable the billing portal, and subscribe `/webhooks/stripe` to the documented subscription events. Exercise checkout, replay, cancellation and access. Keep `Billing__AllowLive=false` for staging.
- If AI practice is enabled, supply `Practice__ApiKey` and `Practice__Model`. Verify consent, withdrawal, quota and failure handling against the chosen provider. Live email, Stripe and AI traffic were outside the local validation. Complete operator contact and retention, privacy, refund and institution policies before public launch.
- Hosted staging acceptance should repeat the browser, provider and backup restore checks using staging storage and the staging hostname.

## Publish local commits

From this checkout, fetch and review the branch before manually publishing to the existing draft PR.

```powershell
git fetch origin
git log --oneline origin/rebuild/dotnet-startup..HEAD
git push origin HEAD:refs/heads/rebuild/dotnet-startup
```
