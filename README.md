# CRM System — School of Foreign Languages

![Version](https://img.shields.io/badge/version-0.6.0-blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![License](https://img.shields.io/badge/license-private-lightgrey)

> CRM system for a foreign language school. Modular Monolith with a planned
> phased migration to microservices architecture.

## Backend

### Run

```bash
docker compose up -d                       # Postgres (5432) and Seq (UI 8081)
dotnet user-secrets set 'ConnectionStrings:Default' '<connection string>' --project backend/src/Host
dotnet user-secrets set 'Jwt:SecretKey' '<at least 32 characters>' --project backend/src/Host
dotnet user-secrets set 'SuperAdmin:Email' '<email>' --project backend/src/Host
dotnet user-secrets set 'SuperAdmin:Password' '<password with upper/lower case, digit and symbol>' --project backend/src/Host
dotnet run --project backend/src/Host      # API docs: /scalar
```

Migrations are applied by hand per module (`dotnet ef database update --context <Module>DbContext ...`) or
automatically at start-up with `Database:MigrateOnStartup=true`.

### Docker (optional)

`docker compose up -d` starts only Postgres and Seq. To run the whole application in containers too
(API and frontend), add to `.env` `JWT_SECRET_KEY` (32+ characters), `SUPERADMIN_EMAIL`,
`SUPERADMIN_PASSWORD` (and `FRONTEND_ORIGIN`, default `http://localhost:5173`), then:

```bash
docker compose --profile app up -d --build     # API on :8080, frontend on :5173
```

The container applies the migrations at start-up. Nothing else is needed; there is no CI or deployment pipeline.
The `app` profile builds and starts both `api` and `web` (the frontend, see Frontend → Docker below); to
run only the API container add `--build api` or list the service explicitly.

### Configuration

| Key | Purpose |
|---|---|
| `Cors:AllowedOrigins` | Origins of the frontend (default: the React dev servers on 5173 and 3000). With none configured every cross-origin call is refused. |
| `Scheduling:TimeZone` | School time zone; lessons are agreed in it and stored in UTC (default `Europe/Kyiv`). |
| `Database:MigrateOnStartup` | Apply all module migrations when the application starts (default `false`). |
| `Notifications:Automation:*` | The periodic job (see below); off by default. |
| `Identity:Lockout:*` | `MaxAttempts` (5) wrong passwords lock an account for `DurationMinutes` (15). An administrator's password reset unlocks it. |
| `RateLimiting:Auth:*` | `PermitLimit` (30) login/refresh requests per `WindowSeconds` (60) from one address; answered with 429 and `Retry-After`. |

### What a student or a teacher can see about themselves

No administrator permission is needed, only the role:

| Endpoint | Role | Returns |
|---|---|---|
| `GET /api/auth/me` | any | account, roles, permissions, the linked student or teacher profile, and `contact` (name, middle name, phone, date of birth, city, country and salary kept on the account itself, for administrators and managers) |
| `PUT /api/auth/me/contact` | any without a student/teacher record | set my first/last/middle name, phone, date of birth, city and country (administrators and managers); salary is never self-service; the SuperAdmin system account has none (403); students and teachers change theirs in their own record (409) |
| `GET /api/auth/users[?isActive=]` | `CanManageAdmins` | list administrator and manager accounts with contact details, salary, status and permissions (the SuperAdmin, students and teachers are not listed) |
| `PUT /api/auth/users/{id}/status` | `CanManageAdmins` | deactivate or reactivate an administrator; nothing is deleted, refresh tokens are revoked on deactivation |
| `PUT /api/auth/users/{id}/permissions` | `CanManageAdmins` | replace the permissions of an administrator; you cannot change yourself, the SuperAdmin (403) or a student/teacher account (409) |
| `PUT /api/auth/users/{id}/salary` | `CanManageAdmins` | set or clear an administrator's salary; same "not yourself, not the SuperAdmin" rule as above |
| `GET /api/students/me` | Student | own profile |
| `GET /api/teachers/me` | Teacher | own profile with salary rates |
| `GET /api/enrollments/my` | Student | own enrollments with course names |
| `GET /api/billing/invoices/my` | Student | own invoices (`status`, paging) |
| `GET /api/billing/payrolls/my` | any | own payrolls: a teacher's from lessons taught, anyone else's (administrators and managers) from their Salary; simply empty for a role that has neither |
| `GET /api/calendar/my`, `GET /api/notifications` | Student, Teacher | own calendar, own notifications |
| `GET/POST/PUT/DELETE /api/calendar/events` | any | arbitrary calendar entries that are not lessons: a personal reminder only its owner ever sees, or (with `CanManageSchedule`) a notice visible to everyone; a personal event of someone else is a 404, not a 403 |

### Notifications and the periodic job

Notifications are in-app only: they are stored in the database and shown in the user's inbox
(`GET /api/notifications`, `GET /api/notifications/unread-count`). Nothing is sent by email or SMS.

With `Notifications:Automation:Enabled=true` a background service inside the API process runs every
`IntervalSeconds` (default 900): it marks overdue invoices and sends the invoice and lesson reminders
(`LessonReminderHoursAhead`, `InvoiceReminderDaysBefore`, `IncludeOverdue`, `MarkOverdueInvoices`).
Every reminder is sent once. A Postgres advisory lock makes sure only one instance runs the job at a time.
The admin endpoints (`/api/notifications/invoice-reminders`, `/lesson-reminders`, `/api/billing/invoices/mark-overdue`)
do the same work on demand and are unaffected by the setting.

### Profile photos

Any signed-in user (student, teacher, administrator) has one photo, tied to their account:

| Endpoint | What it does |
|---|---|
| `PUT /api/auth/me/photo` (multipart field `file`) | Upload or replace my photo: JPEG, PNG or WebP, up to 5 MB. The type is checked from the file's own bytes. |
| `GET /api/auth/me/photo` | My photo as an image (browser-cacheable via `ETag`). `GET /api/auth/me` tells whether there is one (`hasPhoto`, `photoUrl`). |
| `DELETE /api/auth/me/photo` | Remove it. |
| `GET /api/students/{id}/photo`, `GET /api/teachers/{id}/photo` | For staff with `CanViewStudents` / `CanViewTeachers`. |

The image files are stored in a folder on the server (`Storage:Path`, default `uploads` next to the application;
under Docker `/app/uploads` on a volume). The database keeps only what the file is (`identity.user_photos`):
its key in the folder, type, size and time. File names are generated by the server, never taken from the client.

### Payroll

`POST /api/billing/payrolls` accrues one entry for one period (`YYYY-MM`), for exactly one of:

- `teacherId` — a teacher's pay for the month: their current base salary and per-lesson rate
  (`TeacherSalaryRate`) times the lessons they actually completed that period.
- `userId` — any other staff account's pay: a flat amount, simply that account's `Salary` at the
  time (set separately, see `PUT /api/auth/users/{id}/salary` above). No lessons involved.

Both live in the same table and go through the same history/status/`mark paid` flow — an
administrator's payroll is not a separate feature, just a second way to fill in the same record.
One entry per teacher or staff member per period; `GET /api/billing/payrolls` and `.../my` accept
either `teacherId` or `userId` as a filter.

### Import and export (students and teachers)

Excel (`.xlsx`) and JSON, the same for `/api/students` and `/api/teachers`:

| Endpoint | What it does |
|---|---|
| `GET .../export?fileFormat=xlsx\|json&lang=en\|uk` | Download the data; accepts the same filters as the list (`search`, `city`, ...). |
| `GET .../import-template?fileFormat=xlsx\|json&lang=en\|uk` | An empty Excel template (headers, drop-down lists, a help sheet with allowed values and examples) or a JSON sample. |
| `POST .../import` (multipart field `file`) | Import a file. `?dryRun=true` only checks it, `?allOrNothing=true` saves nothing if any row is bad. |

- Excel column names are meant for people: `First name`, `Date of birth`, `Learning goal`... or, with `lang=uk`,
  `Ім'я`, `Дата народження`, `Мета навчання`. Values are readable too (`Yes/No` or `Так/Ні`, `Online` or `Онлайн`).
- An import understands both languages (even mixed), any letter case, and dates as Excel dates, `2001-03-25` or `25.03.2001`.
- A file exported from the system can be edited and imported again (status, id and creation date are ignored).
- The response lists every row with its Excel row number, whether it was saved, and errors that name the column
  (`Date of birth: '31.02.2001' is not a valid date...`).
- Limits: 5 MB and 2000 rows per file, 10000 rows per export. Parent information is not part of the files.

### Health

- `GET /health`: the process is alive.
- `GET /health/ready`: the database is reachable and all module schemas exist (fails with 503 otherwise).

### Tests

```bash
dotnet test backend/CrmSystem.slnx
```

- `Crm.UnitTests`: domain rules, validators and pure services; no infrastructure needed.
- `Crm.IntegrationTests`: the whole application in memory against a real Postgres. Every run creates its own
  database and drops it afterwards. The server comes from the `CRM_TEST_CONNECTION` environment variable, or from the
  Host user secret `ConnectionStrings:Default` (the database name is replaced).

## Frontend

React 18 + TypeScript (strict) + Vite, in `frontend/`. Tailwind CSS v4 + hand-rolled shadcn-style
components (Radix primitives), TanStack Query for server state, React Router, react-hook-form + zod,
i18next (Ukrainian default, English second). The typed API client is generated from
`frontend/openapi/v1.json` (`npm run gen:api` regenerates it from a running backend's
`/openapi/v1.json`).

### Run

```bash
cd frontend
npm install
npm run dev            # http://localhost:5173
```

The app calls relative `/api/...` paths; the Vite dev server proxies them to the backend at
`http://localhost:8080` (see `server.proxy` in `vite.config.ts`), so the backend just needs to be
running (`docker compose --profile app up -d --build` at the repo root) — no CORS setup and nothing
to point at each other manually.

### Scripts

| Script | What it does |
|---|---|
| `npm run dev` | Vite dev server |
| `npm run build` | Type-check (`tsc -b`) and production build to `dist/` |
| `npm run lint` / `npm run format` | ESLint / Prettier |
| `npm test` / `npm run test:watch` | Vitest |
| `npm run gen:api` | Regenerate `src/api/schema.d.ts` from `openapi/v1.json` |

### Docker

`frontend/Dockerfile` builds the SPA with Vite and serves the static output with nginx
(`frontend/nginx.conf` — SPA fallback to `index.html` so client-side routes survive a hard refresh).
No API URL is baked into the bundle: the app calls relative `/api/...` paths, and nginx reverse-proxies
them to the `api` service over the compose network. The browser only ever talks to one origin — whatever
host or IP it used to reach the frontend — so the same image works unchanged from `localhost`, from
another device on the LAN, or behind a real domain, with nothing to rebuild or reconfigure per
environment:

```bash
docker build -t crm-web frontend
docker run -p 5173:80 crm-web
```

Via compose it's part of the `app` profile (see Backend → Docker above). To reach it from another
device on the same network, open `http://<this machine's LAN IP>:5173` — no config changes needed.

### Notes

- Access token lives only in memory; the refresh token is in `localStorage` (per the brief — acceptable
  for this project, but means an XSS vulnerability could read it, hence the short-lived access token and
  rotation on every refresh).
- Every request goes through one `openapi-fetch` client (`src/api/client.ts`) whose middleware attaches
  the bearer token and, on a 401, serializes a single refresh call and retries the original request once.
- The sidebar/route table (`src/routes/navConfig.ts`, `src/App.tsx`) is permission/role-gated client-side
  for UX only; the server remains the authority and every endpoint is still checked there.
