# Campus Events

[![CI](https://github.com/L-exjr/campusevent/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/L-exjr/campusevent/actions/workflows/ci.yml)

A role-based Event Management System frontend built with React, TypeScript, Vite, React-Bootstrap, and React Router.

The repository now also includes the [.NET 10/PostgreSQL backend API](backend/README.md) under `backend/EventManagement.Api`.

The frontend uses the ASP.NET Core API by default. Its typed API facade handles JWT headers, backend response mapping, pagination, and consistent error messages without leaking transport details into pages.

## API configuration

Copy `.env.example` to `.env.local` when local values differ:

```env
VITE_API_BASE_URL=http://localhost:5080/api
VITE_USE_MOCK_API=false
```

The ASP.NET Core API is the default, including when `VITE_USE_MOCK_API` is unset. Mock mode requires both the Vite development server (`import.meta.env.DEV`) and an explicit `VITE_USE_MOCK_API=true`; production builds always use the live API and remove the mock implementation from the generated bundle. Use the flag only for local frontend development when the backend or PostgreSQL is not running. Real API sessions use `sessionStorage`, and expired or rejected JWTs automatically clear the frontend session.

## Development-only mock accounts

These accounts exist only when running the Vite development server with `VITE_USE_MOCK_API=true`. All mock accounts use the password `demo123`.

| Role | Email |
| --- | --- |
| Student | `student@cevents.com` |
| Organizer | `organizer@cevents.com` |
| Admin | `admin@cevents.com` |

New accounts created from the registration page start with the Student role. Admins can promote a Student to Organizer from the user-management page.

## Available workflows

- Public: browse and filter upcoming events by search text, category, or date, and view event details without an account.
- Student: register for free events or complete server-verified Paystack checkout, use signed QR tickets, vote, review registrations, and download attendance-backed certificates.
- Organizer: create, edit, and delete owned events; review registrants; scan tickets; record attendance; and configure free or paid voting campaigns.
- Admin: approve or reject pending Organizer applications, manage user roles and account status, create, edit, or delete any event, and review system reports.
- Shared: JWT session handling, role-aware navigation, route guards, responsive views, 403/404 pages, and reusable loading/error states.

## Project structure

```text
src/
├── api/             # Typed facade, HTTP client, backend adapter, and optional mock
├── components/      # Reusable UI, tables, forms, layout, and guards
├── context/         # Authentication provider and context
├── hooks/           # Auth and async-data hooks
├── pages/           # Student, Organizer, Admin, auth, and error pages
├── types/           # Shared domain types
└── utils/           # Central permissions and formatting helpers
```

## API boundary

Pages import only `src/api/index.ts`. `realApi.ts` maps the ASP.NET contracts into frontend domain types, while `mockApi.ts` remains an optional development adapter implementing the same interface.

## Scripts

```bash
npm run dev
npm run build
npm run lint
npm run preview
```

## Production configuration

Backend payment, email, QR, certificate-storage, and Supabase secrets are listed in `.env.example` and must remain server-only. Public SEO configuration, optional LocalBusiness values, and the optional GA4 measurement ID use `VITE_` variables because Vite embeds them at build time. GA4 and LocalBusiness markup stay disabled when their required real values are blank.

The application remains a responsive web application; native mobile apps and refresh-token issuance are out of scope.
