# DualPath

A .NET 10 application for internship preparation, independent learning and institution programmes. The redesigned interface uses the approved navy and sky-blue palette. The previous Next.js prototype is preserved in `legacy/nextjs-prototype/`.

## What works

- Public programmes, institution pages, resources, enquiries and published content.
- ASP.NET Core Identity registration, email verification, recovery, lockout and authenticated sessions.
- Twenty original lessons across five modules, knowledge checks, persistent progress and assignments.
- Private PDF/DOCX attachments, draft/submission/revision states, versioned lessons, instructor rubrics and feedback.
- Reflections with explicit private or assigned-instructor visibility; consent-based AI practice with quotas.
- Institutions, cohorts, expiring email-bound invitations, membership roles, capacity controls, announcements and scoped reports.
- Stripe Checkout and billing portal adapters, verified idempotent subscription webhooks, support workflow, audit records and transactional email outbox.

External email, payment and AI services require the operator’s own configuration. Unconfigured providers return an explicit unavailable state and never create simulated successes or charge a customer. Demonstration accounts are development-only; Production rejects demo seeding.

## Run locally

Install the .NET SDK specified by `global.json` and PostgreSQL 18. Alternatively, set `POSTGRES_PASSWORD` and `DUALPATH_DEMO_PASSWORD` in your shell, then run:

```sh
docker compose up --build
```

Open `http://localhost:8080`. The development accounts are `student@demo.dualpath.test`, `instructor@demo.dualpath.test`, `institution@demo.dualpath.test`, and `admin@demo.dualpath.test`; their password is the value you set in `DUALPATH_DEMO_PASSWORD`.

Without Docker, export `ConnectionStrings__Database`, `DualPath__BaseUrl`, `ASPNETCORE_ENVIRONMENT=Development`, and `DualPath__ApplyMigrations=true`, then:

```sh
dotnet restore --locked-mode
dotnet run --project src/DualPath.Web --urls http://localhost:5000
```

Demo seeding additionally requires `DualPath__SeedDemo=true` and `DualPath__DemoPassword`. Keep secrets outside Git. Built-in learning content is seeded without demo users in normal environments.

## Verify

Set `DUALPATH_TEST_DATABASE` to a **dedicated test PostgreSQL database**, then run `./scripts/check.sh`. Each test run creates and verifies a uniquely named schema; existing schemas are never dropped. See [testing](docs/testing.md) for coverage and environment limits.

No `.github/workflows` exist. Checks, test results and releases are local/manual: **zero GitHub Actions minutes**. Hosting and external provider usage have their own costs.

## Deploy and operate

Read [deployment](docs/deployment.md), [architecture](docs/architecture.md), [configuration](docs/configuration.md) and [operations](docs/operations.md). The Docker image runs as a non-root user. Production requires HTTPS, a PostgreSQL connection, persistent private file and Data Protection key storage, configured providers, and operator-specific policies.

Native Sites hosting runs a different server runtime and cannot host this .NET application. The earlier design preview is separate from this working server implementation.
