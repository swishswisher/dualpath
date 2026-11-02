# Configuration

ASP.NET Core configuration uses double underscores for environment nesting. Keep credentials in a host secret manager or local environment, outside source control.

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Database` | Npgsql PostgreSQL connection string; production TLS verification must remain enabled |
| `ASPNETCORE_ENVIRONMENT` | `Development`, `Testing`, or `Production` |
| `DualPath__BaseUrl` | Canonical origin used in verification, invitation and provider return links; HTTPS required in Production |
| `AllowedHosts` | Semicolon-separated hostnames for the actual deployment |
| `DualPath__ApplyMigrations` | Apply EF migrations on startup; development convenience; use an isolated release migration step for production |
| `DualPath__SeedDemo` | Development accounts and institution; prohibited in Production |
| `DualPath__DemoPassword` | Development-only password meeting Identity policy |
| `DualPath__BootstrapAdminEmail` / `DualPath__BootstrapAdminPassword` | Create the initial platform operator if absent; remove after bootstrap |
| `DualPath__InstitutionSeatLimit` | Initial capacity for a new institution; default 50 |
| `Storage__Root` | Persistent private directory outside `wwwroot`; default `App_Data/private` |
| `Email__ApiKey` / `Email__From` | Resend API key and verified sender |
| `Billing__SecretKey` / `Billing__WebhookSecret` | Stripe API credential and snapshot-event signing secret |
| `Billing__IndependentPriceId` / `Billing__GuidedPriceId` / `Billing__InstitutionPriceId` | Recurring Stripe Price identifiers |
| `Billing__AllowLive` | Must explicitly be true to use an `sk_live_` API credential |
| `Practice__ApiKey` / `Practice__Model` | OpenAI Responses credential and supported model identifier chosen by the operator |
| `Practice__DailyLimit` | Per-user daily reservations; default 5; failed calls do not consume the quota |
| `Practice__MaximumOutputTokens` | Response cap; default 700 |
| `RateLimits__FormRequestsPerMinute` | Per-identity/IP limit for authentication and public enquiry POSTs; default 12 |
| `RateLimits__RequestsPerMinute` | Other application requests per identity/IP; default 120 |

Default API bases are official HTTPS endpoints. Do not expose keys to the browser. Do not disable TLS certificate checks. For Neon migrations, testing with session state and dumps, use its direct endpoint; the application may use its pooled endpoint.

Configure Stripe snapshot events at `/webhooks/stripe` for `customer.subscription.created`, `customer.subscription.updated`, and `customer.subscription.deleted`, with API version `2025-06-30.basil`. The adapter handles both older top-level and Basil item-level billing periods. Configure the Stripe billing portal for invoices, payment methods and cancellation. The webhook verifies the unmodified request payload, a five-minute signature window and duplicate event IDs.

OpenAI practice sends only the selected question and the answer the learner explicitly submits, with `store: false`. It requires consent and does not send the profile, private reflections or portfolio attachments. This option is not a claim about every provider retention policy; the operator must document its provider agreement and retention rules.

Email delivery needs a verified sender. While it is absent, development messages remain queued and Production registration reports unavailable. The development mailbox is never available in Production.
