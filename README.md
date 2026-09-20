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

### Configuration

| Key | Purpose |
|---|---|
| `Cors:AllowedOrigins` | Origins of the frontend (default: the React dev servers on 5173 and 3000). With none configured every cross-origin call is refused. |
| `Scheduling:TimeZone` | School time zone; lessons are agreed in it and stored in UTC (default `Europe/Kyiv`). |
| `Database:MigrateOnStartup` | Apply all module migrations when the application starts (default `false`). |
| `Notifications:Automation:*` | The periodic job (see below); off by default. |

### Notifications and the periodic job

Notifications are in-app only: they are stored in the database and shown in the user's inbox
(`GET /api/notifications`, `GET /api/notifications/unread-count`). Nothing is sent by email or SMS.

With `Notifications:Automation:Enabled=true` a background service inside the API process runs every
`IntervalSeconds` (default 900): it marks overdue invoices and sends the invoice and lesson reminders
(`LessonReminderHoursAhead`, `InvoiceReminderDaysBefore`, `IncludeOverdue`, `MarkOverdueInvoices`).
Every reminder is sent once. A Postgres advisory lock makes sure only one instance runs the job at a time.
The admin endpoints (`/api/notifications/invoice-reminders`, `/lesson-reminders`, `/api/billing/invoices/mark-overdue`)
do the same work on demand and are unaffected by the setting.

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
