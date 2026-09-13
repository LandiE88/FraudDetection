# Fraud Detection Service

A .NET 8 service that ingests categorized financial transaction events, evaluates
each one against a configurable set of fraud rules, persists the results in
PostgreSQL, and exposes everything through a RESTful API.

## Contents

- [Architecture](#architecture)
- [Fraud rules](#fraud-rules)
- [Project layout](#project-layout)
- [Running with Docker (recommended)](#running-with-docker-recommended)
- [Running locally without Docker](#running-locally-without-docker)
- [API reference](#api-reference)
- [Testing](#testing)
- [Design notes](#design-notes)

## Architecture

The solution follows **Domain-Driven Design** with a classic four-layer structure.
Dependencies only ever point inward, toward the domain:

```
FraudDetection.Api  --->  FraudDetection.Application  --->  FraudDetection.Domain
        |                            |                             ^
        v                            v                             |
FraudDetection.Infrastructure -------+-----------------------------+
```

| Layer | Responsibility |
|---|---|
| **Domain** | The `TransactionEvent` aggregate, the `Money` value object, the `FraudFlag` entity, and the fraud rules themselves. Zero dependencies on anything outside the BCL — no EF Core, no ASP.NET Core, no third-party messaging library. Every fraud rule is a pure function of a transaction and its recent history: no I/O, no clock access, fully unit-testable in isolation. |
| **Application** | Orchestrates use cases as explicit command/query handlers (`IngestTransactionCommand` → `IngestTransactionCommandHandler`, `GetTransactionByIdQuery`, `GetTransactionsQuery`), each registered behind an `ICommandHandler`/`IQueryHandler` interface the API resolves directly via DI — no bus in between. FluentValidation runs inside each handler, DTO mapping, and a small custom `IDomainEventDispatcher` that routes domain events to their handlers. Depends only on Domain. |
| **Infrastructure** | EF Core `DbContext`, entity configurations, the Npgsql provider, repository/unit-of-work implementations, and the system clock. Depends on Domain + Application (to dispatch domain events through `IDomainEventDispatcher` after a successful save). |
| **Api** | ASP.NET Core Web API: controllers, request/response contracts, Swagger, and a single global exception handler that maps domain/application exceptions to RFC 7807 `ProblemDetails`. |

### Error handling

All exceptions are handled in exactly one place, [`GlobalExceptionHandler`](src/FraudDetection.Api/ErrorHandling/GlobalExceptionHandler.cs),
registered via ASP.NET Core 8's `IExceptionHandler`:

| Exception | HTTP status | Meaning |
|---|---|---|
| `FluentValidation.ValidationException` | 400 Bad Request | Request failed input validation; response body is a `ValidationProblemDetails` with one entry per invalid field. |
| `NotFoundException` | 404 Not Found | The requested transaction doesn't exist. |
| `DomainException` | 422 Unprocessable Entity | The request is well-formed but violates a domain invariant (e.g. a non-positive amount). |
| Anything else | 500 Internal Server Error | Logged with full detail server-side; the client only ever sees a generic message. |

## Fraud rules

Every incoming transaction is run through all of the rules below (see
[`src/FraudDetection.Domain/Fraud/Rules`](src/FraudDetection.Domain/Fraud/Rules)).
Adding a new rule is a matter of implementing `IFraudRule` and registering it in
[`Application/DependencyInjection.cs`](src/FraudDetection.Application/DependencyInjection.cs) — nothing else changes.

| Rule | Criterion |
|---|---|
| `LargeAmount` | Amount exceeds a threshold defined per `TransactionCategory` (e.g. R5,000 for a purchase, R2,000 for a withdrawal). Severity escalates to Critical at R50,000 or more, regardless of category. **Thresholds are stored in the database, not hardcoded** — see below. |
| `HighVelocity` | More than 5 transactions on the same account within a rolling 10-minute window. |
| `UnusualHours` | A transaction of R500+ occurring between 00:00–05:00 UTC. |
| `Structuring` | An amount sitting just under a common reporting threshold (R3,000 or R10,000) — a pattern known as structuring/smurfing. |
| `DuplicateTransaction` | An identical amount at the same merchant on the same account within the last 2 minutes — a possible double-charge or replay. |

### Customizing the LargeAmount thresholds

The per-category thresholds live in the `large_amount_thresholds` table (one row per
`TransactionCategory`), seeded by the `AddLargeAmountThresholds` migration with the
same values shown in the table above. An operator can retune them with a plain
`UPDATE` — no code change, rebuild, or restart required, since the API reads the
current values from the database on every request:

```sql
UPDATE large_amount_thresholds SET "ThresholdAmount" = 7500 WHERE "Category" = 'Purchase';
```

(Column names are quoted because EF Core generated them in `PascalCase` — the table
name itself is lowercase, set explicitly via `ToTable`, so it needs no quoting.)

If a category's row is ever missing (or the table is empty), the rule falls back to
its built-in defaults in [`LargeAmountRule.DefaultThresholds`](src/FraudDetection.Domain/Fraud/Rules/LargeAmountRule.cs)
rather than skipping the check for that category.

A transaction can trigger any number of these; each triggered rule becomes a
`FraudFlag` on the transaction, and `IsFlagged` / `HighestSeverity` summarize them.

## Project layout

```
FraudDetection.sln
Dockerfile                     # multi-stage: restore → build → test → publish → runtime
docker-compose.yml             # postgres + api
src/
  FraudDetection.Domain/
  FraudDetection.Application/
  FraudDetection.Infrastructure/
  FraudDetection.Api/
tests/
  FraudDetection.Domain.Tests/         # pure unit tests for the aggregate + fraud rules
  FraudDetection.Application.Tests/    # handler/validator tests with mocked repositories (Moq)
  FraudDetection.IntegrationTests/     # full API + real PostgreSQL via Testcontainers
```

## Running with Docker (recommended)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or another
Docker Engine) with Docker Compose.

```bash
docker compose up --build
```

This one command:

1. Builds the solution and runs the Domain + Application unit test suites — the image
   build **fails if any test fails** (see the `test` stage in the [Dockerfile](Dockerfile)).
2. Starts a PostgreSQL container.
3. Starts the API container, which waits for PostgreSQL to report healthy, then
   **applies EF Core migrations automatically on startup** — the `transaction_events`
   and `fraud_flags` tables are created for you; no manual `dotnet ef database update`
   step is needed.

Once it's up:

- API: http://localhost:8080/api/transactions
- Swagger UI: http://localhost:8080/swagger
- Health check: http://localhost:8080/health

Stop everything with `docker compose down` (add `-v` to also drop the database volume).

## Running locally without Docker

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and a
reachable PostgreSQL instance (a quick one: `docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16-alpine`).

```bash
# 1. Restore & build
dotnet build FraudDetection.sln

# 2. Point the API at your Postgres (defaults to localhost:5432 / postgres / postgres —
#    edit src/FraudDetection.Api/appsettings.Development.json or set an env var instead)
export ConnectionStrings__FraudDetectionDb="Host=localhost;Port=5432;Database=frauddetection;Username=postgres;Password=postgres"

# 3. Run the API — it applies pending migrations automatically on startup
dotnet run --project src/FraudDetection.Api
```

The API listens on the URL printed in the console (typically `http://localhost:5xxx`);
append `/swagger` to explore it interactively.

To manage migrations manually instead of relying on the startup auto-migrate step
(e.g. to generate a new one after a model change):

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> \
  --project src/FraudDetection.Infrastructure \
  --startup-project src/FraudDetection.Api \
  --output-dir Persistence/Migrations
```

## API reference

### `POST /api/transactions` — ingest a transaction event

```bash
curl -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
        "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "category": "Purchase",
        "amount": 25000,
        "currency": "ZAR",
        "merchantName": "Electronics Megastore",
        "occurredAtUtc": "2026-01-01T12:00:00Z"
      }'
```

`category` is one of `Purchase`, `Withdrawal`, `Deposit`, `Transfer`, `Refund`.
`accountHolderId` is optional — if supplied, it must be the `id` of an existing
account holder (see [`POST /api/account-holders`](#post-apiaccount-holders--register-an-account-holder)
to create one, or [`GET /api/account-holders`](#get-apiaccount-holders--search-account-holders)
to find one) or the request gets a `404 Not Found`. Unlike `accountId`, this one is a real
database foreign key: `transaction_events.AccountHolderId → account_holders.Id`.
Returns `201 Created` with the persisted transaction, including any fraud flags:

```json
{
  "id": "b1f0c6b2-...",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accountHolderId": null,
  "category": "Purchase",
  "amount": 25000,
  "currency": "ZAR",
  "merchantName": "Electronics Megastore",
  "occurredAtUtc": "2026-01-01T12:00:00Z",
  "ingestedAtUtc": "2026-01-01T12:00:01.234Z",
  "isFlagged": true,
  "highestSeverity": "Critical",
  "fraudFlags": [
    {
      "id": "...",
      "ruleName": "LargeAmount",
      "severity": "Critical",
      "reason": "Purchase amount of 25000.00 ZAR exceeds the 5000.00 ZAR threshold for this category.",
      "flaggedAtUtc": "2026-01-01T12:00:01.234Z"
    }
  ]
}
```

### `GET /api/transactions/{id}` — fetch a single transaction

Returns `200 OK` with the same shape as above, or `404 Not Found`.

### `GET /api/transactions` — search / list transactions

Query parameters (all optional): `accountId`, `accountHolderId` (the real foreign key
to `account_holders.Id` — use this to find all of a given holder's transactions),
`category`, `onlyFlagged` (`true`/`false`), `fromUtc`, `toUtc`, `page` (default 1),
`pageSize` (default 20, max 100).

```bash
curl "http://localhost:8080/api/transactions?onlyFlagged=true&page=1&pageSize=20"
```

```json
{
  "items": [ /* TransactionResponse[] */ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 3,
  "totalPages": 1
}
```

### `POST /api/account-holders` — register an account holder

```bash
curl -X POST http://localhost:8080/api/account-holders \
  -H "Content-Type: application/json" \
  -d '{
        "firstName": "Jane",
        "lastName": "Doe",
        "idPassport": "A1234567",
        "email": "jane.doe@example.com",
        "dateOfBirth": "1990-05-20"
      }'
```

Returns `201 Created` with the new account holder (same shape as the search results
below) and a `Location` header pointing at `GET /api/account-holders/{id}`.
`email` is checked against the same regex `EmailAddress` enforces domain-side (see
[Design notes](#design-notes)) — `400` if it doesn't look like an email, `422` if a
well-formed request still violates a domain rule (e.g. a future date of birth).

### `GET /api/account-holders/{id}` — fetch a single account holder

Returns `200 OK` with the account holder, or `404 Not Found`.

### `PUT /api/account-holders/{id}` — update an account holder

```bash
curl -X PUT http://localhost:8080/api/account-holders/b7e2... \
  -H "Content-Type: application/json" \
  -d '{
        "firstName": "Janet",
        "lastName": "Doe",
        "idPassport": "A1234567",
        "email": "janet.doe@example.com",
        "dateOfBirth": "1990-05-20"
      }'
```

Replaces every mutable field (name, id/passport, email, date of birth). Returns
`200 OK` with the updated holder, or `404 Not Found` if the id doesn't exist.

### `GET /api/account-holders` — search account holders

Finds people by name, id/passport, email or date of birth — pair it with
`GET /api/transactions?accountHolderId=...` to pull all transactions linked to a given
holder. **At least one criterion is required** (an unfiltered dump of PII isn't
something this endpoint allows).

Query parameters (all optional, but at least one required): `firstName`, `lastName`,
`idPassport`, `email` (all case-insensitive substring matches), `birthYear`,
`birthMonth` (exact, combinable), `page`, `pageSize`.

```bash
curl "http://localhost:8080/api/account-holders?lastName=doe&birthYear=1990"
```

```json
{
  "items": [
    {
      "id": "b7e2...",
      "firstName": "Jane",
      "lastName": "Doe",
      "idPassport": "A1234567",
      "email": "jane.doe@example.com",
      "dateOfBirth": "1990-05-20"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

An account holder is just a person — nothing on this table ties it to an account. The
only relationship between account holders and transactions is
`transaction_events.AccountHolderId`, a real foreign key to `account_holders.Id` (see
[Design notes](#design-notes)); link a transaction to a holder at ingestion time via
`accountHolderId`, then look it up again the same way.

### `GET /health`

Returns `200 OK` once the API can reach PostgreSQL — useful as a container/orchestrator
readiness probe.

## Testing

```bash
# Fast, no external dependencies — domain rules + application handlers
dotnet test tests/FraudDetection.Domain.Tests
dotnet test tests/FraudDetection.Application.Tests

# Full stack against a real PostgreSQL (spins up its own container via Testcontainers —
# requires Docker Desktop running on the host, but not docker-compose)
dotnet test tests/FraudDetection.IntegrationTests

# Everything at once
dotnet test FraudDetection.sln
```

| Project | What it covers | External dependencies |
|---|---|---|
| `FraudDetection.Domain.Tests` | Every fraud rule in isolation, `TransactionEvent`/`AccountHolder` invariants and domain-event raising, `Money`/`EmailAddress` value semantics, the rule engine. | None |
| `FraudDetection.Application.Tests` | Command/query handlers against mocked repositories (Moq), FluentValidation validators. | None |
| `FraudDetection.IntegrationTests` | The real API pipeline (`WebApplicationFactory`) against a real PostgreSQL (Testcontainers): ingestion, validation error shapes, 404s, filtering/paging, account holder CRUD + search, linking a transaction to an account holder, and the history-dependent rules (velocity, duplicate detection) end to end. | Docker (for Testcontainers) |

`docker compose up --build` also runs the Domain and Application suites automatically
as part of building the image (see [Running with Docker](#running-with-docker-recommended)).

## Design notes

- **Money is a value object**, not a decimal column: it bundles amount + currency and
  validates itself on construction, so an invalid transaction can never exist.
- **Fraud rules take pre-loaded data, never a repository.** `IFraudRule.Evaluate`
  receives a `FraudRuleEvaluationContext` containing the transaction, an
  already-fetched window of the account's recent history, and the current
  `FraudRuleSettings` (e.g. `LargeAmount` thresholds) — all fetched once by
  `IngestTransactionCommandHandler` before evaluation starts. This keeps the domain
  layer free of I/O and trivially testable, and it's why database-backed
  configuration (the LargeAmount thresholds) didn't require making `Evaluate` async:
  the handler does the one DB round trip, the rule just reads a value out of the
  context it's handed.
- **No messaging library — handlers and domain events are called directly.** Each
  command/query is a plain record with one handler behind an `ICommandHandler<,>` /
  `IQueryHandler<,>` interface that `TransactionsController` resolves via DI and calls
  directly; there's no bus, so validation happens explicitly at the top of each
  handler rather than through a pipeline. Domain events follow the same philosophy:
  `TransactionEvent` raises `TransactionFlaggedForFraudEvent` through a
  dependency-free `IDomainEvent` marker, `FraudDetectionDbContext` picks up pending
  events after a successful `SaveChanges`, and a small `IDomainEventDispatcher`
  (`FraudDetection.Application/Common/Events`) resolves and invokes whichever
  `IDomainEventHandler<T>` implementations are registered for that event type —
  currently just `TransactionFlaggedForFraudEventHandler`, which logs. A real
  deployment would swap that handler for one that pages an analyst or calls a
  case-management API — nothing else would need to change.
- **The database schema is never something you set up by hand.** EF Core migrations
  are applied automatically on API startup (with a short retry loop to ride out
  container start-up ordering), both in Docker and when running locally.
- **Email format is validated twice, on purpose.** `EmailAddress.Create` (Domain)
  checks it against a regex before an `AccountHolder` can exist with one; a
  `CHECK` constraint on the `account_holders.email` column (`AccountHolderConfiguration`)
  enforces the same pattern at the database level, so a write that somehow bypasses
  the API — a manual `INSERT`, a future service sharing this database — still can't
  leave a malformed email behind. Both use a deliberately simple, linear regex rather
  than a "complete" RFC 5322 pattern, which are a well-known source of catastrophic
  backtracking (ReDoS); `EmailAddress` also gives its `Regex` an explicit timeout as a
  second layer of defense.
- **`AccountHolder.Email` is an EF Core owned type, not a `HasConversion` scalar.**
  A value converter can translate the whole property to/from its column, but LINQ
  can't see through it to translate member access on the wrapped object (e.g.
  `holder.Email.Value` in an `ILIKE` search) — that's a well-known EF Core limitation.
  An owned type's own properties translate normally, which is also why `Money` is
  mapped the same way on `TransactionEvent`.
- **`TransactionEvent` links to `AccountHolder` exactly one way: a real foreign key.**
  `AccountHolderId` is a genuine, nullable foreign key to `AccountHolder.Id` (a real
  primary key), enforced by Postgres and checked by `IngestTransactionCommandHandler`
  before a transaction is created — an unknown `accountHolderId` fails fast with
  `404`, not a raw FK violation. It's nullable, not required — the holder behind a
  transaction isn't always known (or worth registering) at the moment it's ingested,
  so a transaction is still valid without one. There used to be a second, weaker
  link — both tables carried an `AccountId` field that a client could pair up by
  value, but `AccountId` was never unique on either side, so it was only ever a
  documented convention, not something the database enforced. It's gone now:
  `AccountHolder` has no `AccountId` at all, and the FK is the only relationship
  between the two tables. `TransactionEvent.AccountId` still exists — independent of
  `AccountHolder`, it's how the history-dependent rules (`HighVelocity`,
  `DuplicateTransaction`) group a transaction's own recent history by account.
- **`AccountHolder.Update` doesn't touch `Id`.** It only ever replaces the mutable
  personal fields (name, id/passport, email, date of birth). `Create` and `Update`
  share the same private validation helpers (`RequireName`, `RequireIdPassport`,
  `RequireNotFutureDateOfBirth`) so the two can never drift apart on what counts as
  valid.
- **`EmailAddress.IsValid` exists so the API can give a friendly `400` up front**,
  using the exact same regex `Create` enforces — the application layer's
  `CreateAccountHolderCommandValidator`/`UpdateAccountHolderCommandValidator` call
  `EmailAddress.IsValid` rather than duplicating the pattern, so there's one source of
  truth for "what counts as a valid email" shared by validation and construction.
