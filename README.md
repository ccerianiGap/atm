# ATM

A web-based ATM for a single user with two bank accounts: deposit, withdraw (in $20 notes), transfer between accounts, and view balances and transaction history.

- **Backend:** .NET 9 Minimal API, EF Core 9 + SQLite, FluentValidation
- **Frontend:** Vue 3 + TypeScript, Vite, Pinia, Vue Router
- **Tests:** xUnit + AwesomeAssertions (55 backend tests, including concurrency); Vitest + Vue Test Utils (24 frontend tests)

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) 22.18+ or 24.12+

## Run

Two terminals, from the repository root:

```bash
# 1. API: http://localhost:5080 (API reference at http://localhost:5080/scalar/v1)
dotnet run --project Backend/src/Atm.Api

# 2. Web app: http://localhost:5173
cd Frontend/atm-web
npm install
npm run dev
```

The Vite dev server proxies `/api` to the API, so no CORS setup is needed. On first start, the API creates `atm.db` (SQLite) next to the project and seeds two accounts: **Checking** ($1,000) and **Savings** ($5,000). Delete `atm.db*` to reset.

To point the frontend at a different API address: `ATM_API_URL=http://host:port npm run dev`.

## Test

```bash
dotnet test Backend/Atm.sln

cd Frontend/atm-web
npm run test:unit -- --run
npm run type-check
npm run lint
```

## Features

| | |
|---|---|
| **Dashboard** | One card per account with its balance. Clicking a card selects it as the default account for every operation. Shortcut buttons open a specific operation. The selection lives in the URL (`/?account=…&op=withdraw`), so it survives a refresh. |
| **Deposit** | Any positive amount with up to 2 decimals, up to the per-operation limit. |
| **Withdraw** | ATM-style quick amounts ($20 to $500), disabled when above the balance, plus **Other amount**. Withdrawals must be multiples of $20. |
| **Transfer** | From/To selectors. *To* lists every other account, so it works for any number of accounts. |
| **Receipt** | Shown after every successful operation: reference number, new balance(s), time. |
| **History** | Per-account history, newest first, paged, with signed amounts and the balance after each entry. |
| **Errors** | Inline field errors (client and server), banners for business errors, automatic refresh of balances on a concurrency conflict. |

## Architecture

```
Backend/src
├── Atm.Domain          Entities + business invariants. No dependencies.
├── Atm.Application     Use cases, DTOs, validators, mapping, ports (IAccountRepository, IUnitOfWork)
├── Atm.Infrastructure  EF Core DbContext, repository, unit of work, seeding
└── Atm.Api             HTTP endpoints, DI composition, exception → ProblemDetails mapping

Dependencies point inwards:  Api → Application → Domain ← Infrastructure
```

```
Frontend/atm-web/src
├── api/          Typed fetch client (ProblemDetails → ApiError) and one function per endpoint
├── stores/       Pinia store: accounts, balances, ATM settings, operations
├── composables/  useAsyncAction (pending/error state), useOperationForm (shared form flow)
├── components/   AccountCard, OperationPanel, Deposit/Withdraw/TransferForm, ReceiptDialog, ...
├── views/        DashboardView, AccountHistoryView
└── utils/        Amount validation (mirrors server rules), formatting, receipt builders
```

### API

| Method | Route | Notes |
|---|---|---|
| GET | `/api/accounts` | All accounts |
| GET | `/api/accounts/{id}` | One account |
| GET | `/api/accounts/{id}/transactions?page=1&pageSize=20` | Paged, newest first |
| POST | `/api/accounts/{id}/deposits` | `{ "amount": 12.50 }` |
| POST | `/api/accounts/{id}/withdrawals` | `{ "amount": 60 }` |
| POST | `/api/transfers` | `{ "fromAccountId", "toAccountId", "amount" }` |
| GET | `/api/atm/settings` | Limits, so the client can mirror them |
| GET | `/health` | Health check |

Every error is an RFC 7807 `application/problem+json` response with a stable `code`:

| Status | When | `code` |
|---|---|---|
| 400 | Invalid input (amount, paging, same account, malformed body) | `validation_failed`, `bad_request` |
| 404 | Unknown account | `not_found` |
| 409 | Account changed by a concurrent operation | `concurrency_conflict` |
| 422 | Business rule violated (e.g. insufficient funds) | `insufficient_funds`, ... |
| 500 | Unexpected; details are logged, not returned | `internal_error` |

## Design decisions and tradeoffs

- **Rich domain model.** `Account` is the only code that can change a balance (`Deposit`, `Withdraw`, transfer legs). Every change records a `Transaction` holding the resulting balance, so history always reconciles with the balance. Transfer legs are `internal` and created in pairs by `TransferService`, so half a transfer can't be produced. EF details stay out of the domain (fluent configuration, a shadow concurrency property).
- **Validation in layers.** The Vue app validates for instant feedback. FluentValidation in the Application layer handles input and ATM policy: max per operation, and $20 multiples for withdrawals. That multiple is a rule of the machine, not of the account, so it is configurable in `appsettings.json` (`Atm:WithdrawalMultiple`, `0` disables it). The domain re-checks its own invariants: positive amount, 2 decimals, no overdraft.
- **Minimal APIs instead of controllers.** Endpoints are grouped in one static class per area, and each handler is a one-line call into the Application layer. This keeps the HTTP layer compact and is idiomatic in .NET 9. Controllers would work equally well; they pay off when you need filters, model-binding conventions or a large surface organised by class, none of which this API has.
- **`decimal` instead of a `Money` value object.** Amounts are `decimal` (exact arithmetic, never `double`), and the shared rules (positive, at most 2 decimals) live in `AmountRules`. A `Money` type (amount + currency) is better modelling, but with a single currency it mostly adds ceremony: wrapping, EF conversions and JSON converters. The cost is that an amount is just a number, so the type system can't stop an invalid amount being passed around; it is caught by validation instead.
- **Exceptions + one handler instead of a Result type.** Business failures throw typed exceptions, and `GlobalExceptionHandler` is the only place that turns them into HTTP responses. A `Result<T>` makes failures explicit in method signatures, but at this size it adds more ceremony than clarity.
- **SQLite instead of the EF InMemory provider.** InMemory supports neither transactions nor concurrency tokens realistically, which are exactly the behaviours worth demonstrating. SQLite needs zero setup and survives restarts. `EnsureCreated` replaces migrations to keep setup to one step.
- **Atomic transfers.** Both legs and both balances are written by a single `SaveChanges`, which runs in one database transaction.
- **Optimistic concurrency with a version counter**, not a timestamp: two writes in the same clock tick can't be told apart, and SQLite's text storage of dates makes them a fragile token. The counter is a shadow property bumped in `SaveChanges`. A stale write gets a 409, and the client refreshes its balances. Tests cover the deterministic conflict, the counter increments, 10 parallel withdrawals (never overdrawn; history matches the balance) and parallel transfers in both directions (total preserved).
- **SQLite connection pooling disabled.** The parallel tests exposed an intermittent `Microsoft.Data.Sqlite` failure when connections are returned to the pool under concurrent writes ("unable to delete/modify user-function due to active statements"), surfacing as a 500. Opening a SQLite connection is just a file open, so pooling is turned off in `AddInfrastructure` (see the comment there).
- **UTC timestamps.** Stored as `DateTime`, because the SQLite provider can't `ORDER BY` a `DateTimeOffset`. A value converter re-tags values read back as UTC, so the JSON carries a `Z` and the browser shows the correct local time. On SQL Server, `DateTimeOffset` would be the natural choice.
- **DTOs separate from mapping.** Contracts are plain records; mapping lives in extension classes in `Application/Mapping`. AutoMapper would be overkill at this size.
- **Built for N accounts.** Two accounts are seeded, but nothing assumes two: no "other account" logic in the API, and the UI renders lists and selectors.
- **Frontend state in the URL.** The selected account and tab are query parameters rather than store state, so links and refreshes keep them.

## What I'd do with more time

The 2–4 hour time box went to correctness (atomic operations, concurrency, consistent error handling) and tests. The items below were either outside the brief or are production hardening that wouldn't change the architecture.

- **Authentication and multiple users** (each owning accounts). The brief specifies a single user with no authentication.
- **Idempotency keys on POST endpoints**, so a retried request can't apply twice. This needs a key table written in the same transaction as the operation, plus client support. It is the most important gap for a real ATM.
- **Automatic server-side retry on concurrency conflicts** (reload, re-validate, re-apply). Conflicts are already detected and return a 409; retrying is a convenience on top of that.
- **A `Money` value object and multi-currency support.** With one currency it adds ceremony without benefit (see the design decisions above).
- **EF Core migrations instead of `EnsureCreated`; SQL Server/PostgreSQL for production.** `EnsureCreated` and SQLite keep setup to one step for simplicity. Only the Infrastructure layer would change.
- **Daily withdrawal limits; a filter by transaction type in history.** Not in the brief. Daily limits also need a decision on which time zone defines "a day".
- **Multi-language support:** vue-i18n for UI text and locale-aware currency/date formatting, with the client translating the API's stable error `code`s instead of showing server messages. Not in the brief; the stable `code`s already make it possible.
- **CI and end-to-end tests.** The time box; the unit and integration suites cover the behaviour today.


