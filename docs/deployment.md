# Deployment

Use a .NET 10 compatible container host, VM or ASP.NET service. Native Sites preview/hosting is a separate runtime; the original design preview must not be presented as the deployed .NET product.

## Manual release: no GitHub Actions

1. Set a dedicated test database and run `scripts/check.sh` on a workstation or a separately managed runner. This repository contains no GitHub Actions workflows.
2. Review the migration and take a database and private-storage backup. Use a direct PostgreSQL endpoint with a separate migration role.
3. Build the image locally with `docker build -t dualpath:<commit> .`, or deploy `artifacts/publish` with the .NET 10 runtime installed.
4. Apply the reviewed migration once from the release environment:

   ```sh
   dotnet tool restore
   dotnet ef database update --project src/DualPath.Web
   ```

   `ConnectionStrings__Database` must select the release target for this command. For the running app, turn `DualPath__ApplyMigrations` off and use a less privileged application role with required DML permissions.
5. Set production configuration, persistent storage and HTTPS. Restrict the application port to the trusted reverse proxy. Persist `App_Data/keys` and private files; provide the app user with write access to those directories.
6. Configure email, Stripe and optional AI secrets in the host secret manager. Set actual hostnames in `AllowedHosts`, and the HTTPS origin in `DualPath__BaseUrl`. Set `ASPNETCORE_ENVIRONMENT=Production`, `DualPath__SeedDemo=false`. Production also rejects a database containing demonstration accounts or plans; always use a separate clean production database.
7. Bootstrap the initial operator using temporary bootstrap secrets, then remove those secrets. Complete operator identity, contact, retention, billing, refund and institution terms before accepting real customers.
8. Check `/health/live` and `/health/ready`, then exercise registration and verification, one student lesson, an assignment/review cycle, a private download, an invitation, billing test events and a real password recovery delivery on the release candidate.
9. Run the browser checklist in `docs/testing.md` on that compatible host before opening registration or paid service to customers.

## HTTPS and reverse proxies

The application uses HSTS, HTTPS redirection and Secure session cookies in Production. Configure an explicit trusted proxy with the ASP.NET Core forwarding settings described by your host. Do not trust arbitrary forwarded headers from Internet clients. The default forwarded-header behaviour is deliberately not widened by this repository. If the host terminates TLS, verify the app recognises HTTPS and correct client addresses before launch; otherwise redirects and rate limits can behave incorrectly.

## Rollback

Keep the previous image and database backup. Deploy the old image only if its schema remains compatible. Avoid automatic down-migrations on a live database. If a destructive migration requires restoration, restore into a new database, verify it, switch configuration during a maintenance window and retain the previous target until recovery is verified.

## Release boundaries

Email, payment and AI adapters are implemented and tested without live credentials. No real payment, email or AI request is fabricated. A live application host, domain, provider accounts, legal operator details and successful restore/browser/provider smoke tests are operational launch requirements.
