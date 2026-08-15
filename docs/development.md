# Development and deployment

## Prerequisites

- Node.js and npm
- .NET SDK 10
- PostgreSQL
- Optional provider accounts for Supabase, email, Paystack, and Flutterwave sandbox

Install and run:

```bash
npm install
dotnet restore backend/EventManagement.Api/EventManagement.Api.csproj
dotnet ef database update --project backend/EventManagement.Api
dotnet run --project backend/EventManagement.Api
npm run dev
```

Use user secrets or deployed environment variables for secrets. Important values include `ConnectionStrings__DefaultConnection`, JWT issuer/audience/signing key, `Frontend__BaseUrl`, `SUPABASE_URL`, `SUPABASE_SERVICE_ROLE_KEY`, `PAYSTACK_SECRET_KEY`, `FLUTTERWAVE_SECRET_KEY`, `FLUTTERWAVE_WEBHOOK_SECRET`, `PAYMENTS_PROVIDER`, `QR_SIGNING_KEY`, and the selected email-provider credentials. See `.env.example`, `DEV_SETUP.md`, and `DEPLOYMENT.md` for the complete operational list.

The default provider is Paystack. Use only Flutterwave test keys while validating sandbox behavior. Never expose backend secrets through `VITE_` variables.

## Verification

```bash
npm test -- --run
npm run build
dotnet test backend/EventManagement.Api.UnitTests
dotnet test backend/EventManagement.Api.IntegrationTests
```

Integration tests start an isolated PostgreSQL database and apply every migration, including the default-tier historical backfill.
In restricted or offline environments, the .NET test host must be allowed to open its loopback coordination socket and start PostgreSQL child processes. Restore and build first, then use `--no-build --no-restore` with TRX logging; allow roughly five minutes for the serialized suite. See the canonical [restricted/offline runner guidance](../backend/README.md#restricted-or-offline-test-environments) instead of duplicating the full command here.

The current verified totals are 86/86 frontend tests, 62/62 backend unit tests, and 107/107 backend integration tests: 255/255 overall.
