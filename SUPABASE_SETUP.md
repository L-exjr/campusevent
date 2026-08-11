# Supabase Storage setup

Supabase is used only for object storage. Campus Events keeps its ASP.NET Core
JWT authentication and EF Core/PostgreSQL database. Every upload is authorized
and validated by the ASP.NET API before the backend writes to Supabase with a
server-only service-role key.

## 1. Configure backend credentials

In **Supabase → Project Settings → API Keys**, obtain the Project URL and the
service-role key. Store them only in backend User Secrets or the deployment
secret manager:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Supabase:Url" "https://your-project-ref.supabase.co"
dotnet user-secrets set "Supabase:ServiceRoleKey" "your-service-role-key"
```

Production uses `SUPABASE_URL` and `SUPABASE_SERVICE_ROLE_KEY`. Never create a
`VITE_SUPABASE_SERVICE_ROLE_KEY`; all `VITE_*` values are compiled into the
browser bundle.

## Private certificate storage

Create a bucket named `certificates` and leave **Public bucket** disabled.
Certificate PDFs contain attendee data and must never use a public object URL.
The API uploads with the server-only service-role key and calls Supabase's
`/storage/v1/object/sign/` endpoint to issue a time-limited download URL. Set
`CERTIFICATES_BUCKET=certificates` and `CERTIFICATE_SIGNED_URL_MINUTES=60` in
each backend deployment environment.

## 2. Create the public image buckets

Create `event-images` and `profile-images` as public-read buckets. Restrict both
to 5 MB and `image/jpeg`, `image/png`, and `image/webp`. Public reads are required
because event and profile image URLs appear in normal page markup.

```sql
insert into storage.buckets
  (id, name, public, file_size_limit, allowed_mime_types)
values
  ('event-images', 'event-images', true, 5242880,
   array['image/jpeg', 'image/png', 'image/webp']),
  ('profile-images', 'profile-images', true, 5242880,
   array['image/jpeg', 'image/png', 'image/webp'])
on conflict (id) do update set
  public = excluded.public,
  file_size_limit = excluded.file_size_limit,
  allowed_mime_types = excluded.allowed_mime_types;
```

## 3. Remove anonymous write policies

The service-role key bypasses Storage RLS, so the browser needs no Supabase
INSERT/UPDATE/DELETE policy. Remove the former anonymous upload policies after
the backend proxy is deployed and verified. Keep only policies required for the
chosen public/private read model.

The application exposes:

- `POST /api/uploads/profile-image` for any authenticated active user.
- `POST /api/uploads/event-image` for Organizer or Admin users.

Both endpoints accept multipart field `file`, enforce a 5 MB maximum, allow only
JPG/PNG/WebP, validate magic bytes, and return `{ "url": "..." }`. Provider
failures return a controlled 502 response; raw Supabase errors and credentials
are never returned.

Official references:

- [Storage bucket access models](https://supabase.com/docs/guides/storage/buckets/fundamentals)
- [Storage access control](https://supabase.com/docs/guides/storage/security/access-control)
- [Service-role security](https://supabase.com/docs/guides/api/api-keys)

## 4. Application database

The application database remains the source of truth for image URLs. Existing
nullable `ImageUrl` columns on `Events` and `Users` require no new migration.
