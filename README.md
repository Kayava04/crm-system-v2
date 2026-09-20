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
