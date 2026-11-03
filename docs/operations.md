# Operations

## Back up and recover

Back up PostgreSQL with your managed provider’s point-in-time recovery and periodic encrypted `pg_dump --format=custom` exports. Use the direct endpoint and credential files or a secret manager, not passwords in command arguments. Back up private uploads and Data Protection keys as separate encrypted storage. Set retention based on the operator’s institution contracts and privacy policy.

Run a restore drill into a new isolated database and private-storage location. Verify row counts, a known lesson completion, an assignment with its reviews, one authorised private download, sign-in and a newly issued recovery link. Record the elapsed recovery time and the last recoverable point. Never overwrite the active database as the first restore experiment.

## Monitor

- Alert on sustained readiness failures, HTTP 5xx/429 rates, DB pool exhaustion, disk usage and file backup failures.
- Review failed outbox messages and provider status before using the admin retry action. Messages retain their idempotency key.
- Monitor Stripe webhook failures in its dashboard; retry delivery after configuration or database recovery. Duplicate event delivery is harmless to entitlement state.
- Audit role, seat and invitation changes. Revoke invitations and deactivate membership when access is no longer needed.
- Watch AI reservation failures and configured daily usage. Practice failure never changes instructor grades.

Application logs exclude provider credentials and private learning bodies. Do not enable EF sensitive-data logging in production. Restrict operator console and log access. Review infrastructure access separately from application membership.

## Privacy and support

Users can export their own learning records and open access, correction or deletion requests. Support response is persisted and queued by email. Destructive privacy requests are handled by the operator with identity verification and contract/retention checks; the application does not pretend that a support ticket instantly erases backups or institution records.

Private attachments are limited to five per assignment, 10 MB each, and 100 MB per learner. They use generated storage keys outside the public directory, have file signature/type checks, and are downloaded as attachments after record-level authorisation. Add an approved content-scanning service if required by the institution’s file policy.

## Maintain

Use supported .NET security patches, review package lockfile updates, and run the complete local checks before release. Check production dependencies with `dotnet list package --vulnerable --include-transitive`. No automation is enabled in GitHub. Manual validation avoids Actions minutes; hosting, storage, database and provider usage are separate expenses.
