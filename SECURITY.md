# Security and secrets

## Configuration strategy

- Tracked `appsettings*.json` files contain only non-secret defaults, empty
  placeholders, and documentation. Never commit signing keys, passwords,
  connection-string passwords, API keys, or tokens.
- For local development, copy
  `backend/EventManagement.Api/appsettings.Development.example.json` to
  `appsettings.Development.json` if local non-secret overrides are needed. The
  destination is ignored by git. Store the JWT signing key with .NET User
  Secrets:

  ```bash
  cd backend/EventManagement.Api
  dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -hex 64)" >/dev/null
  ```

- For deployed environments, inject the key from the platform's secret manager
  as `Jwt__SigningKey`. Environment variables use double underscores for nested
  .NET configuration keys. Do not place the value in deployment manifests or
  checked-in environment files.
- Rotate a signing key immediately if it is printed, logged, committed, or
  disclosed. Rotation invalidates every JWT signed with the previous key. That
  is low-risk before public deployment, but requires coordinated session expiry
  and user reauthentication once real users exist.

## SMTP credentials

SMTP configuration follows the same secret-storage rule as the JWT signing key.
For local development, put the complete Mailtrap sandbox configuration in .NET
User Secrets from `backend/EventManagement.Api`:

```bash
dotnet user-secrets set "Email:Smtp:Host" "sandbox.smtp.mailtrap.io"
dotnet user-secrets set "Email:Smtp:Port" "2525"
dotnet user-secrets set "Email:Smtp:Username" "your-mailtrap-sandbox-username"
dotnet user-secrets set "Email:Smtp:Password" "your-mailtrap-sandbox-password"
dotnet user-secrets set "Email:Smtp:FromAddress" "notifications@campus-events.test"
dotnet user-secrets set "Email:Smtp:FromName" "Campus Events"
dotnet user-secrets set "Email:Smtp:EnableSsl" "true"
```

Do not add these values to `appsettings.json`, an environment file, source code,
or logs. Development deliberately refuses any SMTP host other than
`sandbox.smtp.mailtrap.io`, so local tests cannot deliver to real inboxes.

In Production, configure the chosen provider's SMTP endpoint through the hosting
platform's secret manager. ASP.NET Core maps these environment variables without
any code changes:

```text
Email__Smtp__Host
Email__Smtp__Port
Email__Smtp__Username
Email__Smtp__Password
Email__Smtp__FromAddress
Email__Smtp__FromName
Email__Smtp__EnableSsl
```

Treat SMTP credentials as compromised if exposed and rotate them in the provider
immediately. Production credentials must never be copied into a local development
profile; local development is Mailtrap sandbox-only.

## Exposed-key history

The former development key must be treated as compromised because removing it
from the latest commit does not remove it from git history. Before making this
repository public, coordinate with every collaborator and purge the old value
using `git filter-repo` or BFG Repo-Cleaner, then force-push the rewritten
branches/tags and have collaborators re-clone. Do not rewrite shared history
without coordination.
