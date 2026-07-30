# Supabase Storage setup

Supabase is used only for object storage. Campus Events continues to use its own
ASP.NET Core JWT authentication and EF Core/PostgreSQL database. Do not enable or
integrate Supabase Auth, and do not point the application at Supabase Postgres.

## 1. Open the existing project and get browser configuration

This setup assumes the Supabase project already exists. Do not create another
project for this workspace.

1. Sign in to the [Supabase dashboard](https://supabase.com/dashboard), select the
   organization, and open the existing Event Management System project.
2. Click **Connect** in the project toolbar and open **App Frameworks**.
3. Copy the **Project URL** and the **Publishable key**. If the project still uses
   legacy JWT keys, open **Project Settings → API Keys → Legacy API Keys** and
   copy the `anon` key instead. Never use a secret or `service_role` key here.
4. The project URL can also be copied from **Project Settings → Data API**.
5. Copy `.env.example` to `.env` and set:
   works. Never copy a `service_role` or secret key into the frontend.
   ```dotenv
   VITE_SUPABASE_URL=https://your-project-ref.supabase.co
   VITE_SUPABASE_ANON_KEY=your-anon-or-publishable-key
   ```

The publishable/anon key is intentionally exposed to the browser and its power is
limited by Storage policies. Any variable prefixed with `VITE_` is compiled into
the client bundle, so a service-role key must never be used here.

The repository ignores `.env`; only the empty `.env.example` template is checked
in. Once the values and buckets are configured, verify the live connection with:

```bash
npm run supabase:smoke
```

The command lists at most one object from each image bucket and prints a success
line for `event-images` and `profile-images`. It does not use Supabase Auth or its
Postgres database.

## 2. Create the public image buckets

Use **Storage → New bucket** to create `event-images` and `profile-images`. Mark
both as public, restrict MIME types to `image/jpeg`, `image/png`, and `image/webp`,
and set the file-size limit to 5 MB.

Alternatively, run this in the Supabase SQL editor to create both buckets with
the same restrictions:

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

Public buckets allow anyone who has an object URL to view it. That is appropriate
for public event covers and the current product decision for profile pictures.
Profile pictures should be moved to a private bucket if they become sensitive;
that design would require short-lived signed URLs from trusted server-side code or
an Edge Function instead of permanently storing a public URL.

## 3. Allow browser uploads

A public bucket makes downloads public, but uploads still require an RLS policy.
This application intentionally does not use Supabase Auth, so Supabase cannot
interpret the application's JWT. Direct uploads must therefore permit the `anon`
role:

```sql
create policy "Allow anonymous event image uploads"
on storage.objects for insert to anon
with check (bucket_id = 'event-images');

create policy "Allow anonymous profile image uploads"
on storage.objects for insert to anon
with check (bucket_id = 'profile-images');
```

Do not add anonymous `UPDATE` or `DELETE` policies. Uploads use random object names
and `upsert: false`, so clients cannot overwrite existing images. If uploads fail
with a `RETURNING`/RLS error in your Supabase project, add metadata-read policies:

```sql
create policy "Allow event image metadata reads"
on storage.objects for select to anon
using (bucket_id = 'event-images');

create policy "Allow profile image metadata reads"
on storage.objects for select to anon
using (bucket_id = 'profile-images');
```

### Security limitation

The UI validates JPG/PNG/WebP and a 5 MB maximum, while bucket restrictions provide
a second content-type/size boundary. A malicious client can still bypass the UI and
use the public key to upload directly, and MIME declarations alone do not prove file
contents. For production, put upload authorization and content inspection in a
Supabase Edge Function or another trusted service that validates the Campus Events
JWT and returns a controlled upload URL. This MVP does not proxy file bytes through
the ASP.NET Core API.

Official references:

- [Storage bucket access models](https://supabase.com/docs/guides/storage/buckets/fundamentals)
- [Storage access-control policies](https://supabase.com/docs/guides/storage/security/access-control)
- [JavaScript file uploads](https://supabase.com/docs/reference/javascript/file-buckets-upload)
- [Public asset URLs](https://supabase.com/docs/reference/javascript/file-buckets-getpublicurl)

## 4. Apply the application migration

The application database remains the source of truth for URLs. Apply the EF Core
migration to the existing application PostgreSQL database:

```bash
cd backend
dotnet ef database update --project EventManagement.Api/EventManagement.Api.csproj
```

The migration adds nullable `ImageUrl` columns to `Events` and `Users`. Existing
rows remain valid and render with local placeholder images.
