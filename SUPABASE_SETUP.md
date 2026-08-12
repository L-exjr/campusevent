# Supabase Storage setup

Supabase is used only for object storage. Application users, events, payments,
votes, and registrations remain in the ASP.NET Core/EF Core PostgreSQL database,
and the API continues to issue and validate its own JWTs.

## 1. Configure backend credentials

From **Supabase → Project Settings → API Keys**, copy the project URL and the
server-only service-role key. Store them in .NET User Secrets locally:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Supabase:Url" "https://your-project-ref.supabase.co"
dotnet user-secrets set "Supabase:ServiceRoleKey" "your-service-role-key"
```

Railway uses `SUPABASE_URL` and `SUPABASE_SERVICE_ROLE_KEY`. Never create a
`VITE_SUPABASE_SERVICE_ROLE_KEY`; every `VITE_*` value is public browser data.

## 2. Create the buckets

Create and configure these buckets in the Supabase Storage dashboard:

| Bucket | Access | Restrictions | Purpose |
| --- | --- | --- | --- |
| `event-images` | Public | 5 MB; JPEG, PNG, WebP | Images rendered on public event pages |
| `profile-images` | Public | 5 MB; JPEG, PNG, WebP | Public user profile images |
| `certificates` | Private | PDF | Attendance-backed certificates containing attendee data |

Bucket restrictions complement the API's validation. The upload endpoints also
enforce the 5 MB image limit, accepted MIME types, and file signatures.

Public buckets bypass read access control only for downloads; write operations
must still be protected. Private certificate downloads use short-lived signed
URLs issued by the API. Configure:

```text
CERTIFICATES_BUCKET=certificates
CERTIFICATE_SIGNED_URL_MINUTES=60
CERTIFICATE_TEMPLATE_VERSION=1
```

Use the dashboard or supported Supabase management APIs to change bucket
configuration. Do not directly mutate or delete rows in the `storage` schema;
those rows are metadata and direct changes can leave stored objects inconsistent.

## 3. Remove browser write policies

The browser never writes directly to Supabase. The authenticated API validates
the request, records image lifecycle state in the application database, and then
uses the service-role key to write the object. Remove policies that grant the
`anon` role `INSERT`, `UPDATE`, or `DELETE` access after the backend proxy is
deployed and verified.

The API routes are:

- `POST /api/uploads/profile-image` for an authenticated active user.
- `POST /api/uploads/event-image` for an Organizer or Admin.

Both accept multipart field `file` and return `{ "url": "..." }`. Provider
failures return a controlled API error; raw Supabase responses and credentials
are not returned.

## 4. Lifecycle and database behavior

Uploaded image objects start as pending records owned by the authenticated user.
A profile or event save claims its pending object. Background cleanup leases and
removes expired pending objects and superseded images. Supplying an arbitrary new
third-party image URL is rejected.

The application database remains the source of truth for image URLs and object
keys. The `certificates` bucket must remain private because generated PDFs contain
attendee information.

## Verification checklist

1. Upload an allowed image smaller than 5 MB through the app.
2. Confirm the request targets `/api/uploads/*`, not Supabase directly.
3. Confirm the returned public image URL renders.
4. Reject a disallowed type and an image larger than 5 MB.
5. Generate a certificate for an eligible attended event and confirm its signed
   URL expires.
6. Confirm anonymous clients cannot create, replace, or delete Storage objects.

Official references:

- [Storage buckets and access models](https://supabase.com/docs/guides/storage/buckets/fundamentals)
- [Storage access control](https://supabase.com/docs/guides/storage/security/access-control)
- [Storage file limits](https://supabase.com/docs/guides/storage/uploads/file-limits)
- [Storage schema guidance](https://supabase.com/docs/guides/storage/schema/design)
