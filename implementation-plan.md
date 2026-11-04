# ATM Application — Implementation Plan

**Stack:** .NET 9 (C#) Web API · Vue 3 + TypeScript (Vite) · EF Core + SQLite
**Time box:** 2–4 hours. The reviewers score architecture, separation of concerns, error handling, readability, and tradeoffs. Effort goes to a clean backend core first and a functional, tidy frontend second.

---

## 1. Scope

### In scope (must have)
| # | Feature | Notes |
|---|---------|-------|
| 1 | View the two accounts and their balances | Seeded at startup: *Checking* and *Savings* |
| 2 | Deposit funds into an account | |
| 3 | Withdraw funds from an account | No overdraft |
| 4 | Transfer between the two accounts | Atomic: both legs succeed or neither does |
| 5 | Transaction history per account | Newest first, paged |
| 6 | Consistent error handling | Validation and business-rule errors → RFC 7807 ProblemDetails, shown clearly in the UI |

### Out of scope (call out in README as "with more time")
Authentication/multi-user, real card/PIN flow, currency handling beyond a single currency, idempotency keys, audit/event sourcing, Docker/CI pipeline, E2E tests.

### Business rules
- Amount must be `> 0` and have at most 2 decimal places.
- Per-operation maximum (e.g. `10,000.00`), configurable through `appsettings.json` (`AtmOptions`).
- Withdrawals and outgoing transfers cannot make the balance negative.
- Transfer source and destination must differ and both must exist.
- Every balance change writes a transaction record with the resulting balance (`BalanceAfter`), so history can be reconciled with the balance.
- Withdrawals must be a multiple of `WithdrawalMultiple` (default **20**, as an ATM dispenses notes). This applies to quick amounts and "Other amount" alike, and is enforced by the server (FE mirrors it). Set it to `0` to disable. Deposits and transfers are not affected. This is an ATM-machine policy, not an account invariant, so it's checked in the application-layer `WithdrawRequestValidator`, not in `Account`.
- **N accounts by design:** two accounts are seeded, but nothing in the BE or FE assumes exactly two (no "the other account" logic in the API; the FE uses lists and selectors).

---

## 2. Repository Layout

```
/Challenge
├── README.md                    # setup, run, test, design decisions, tradeoffs
├── implementation-plan.md
├── .gitignore
├── Backend/
│   ├── Atm.sln
│   ├── src/
│   │   ├── Atm.Domain/          # entities, value rules, domain exceptions (no dependencies)
│   │   ├── Atm.Application/     # use cases, DTOs, validation, interfaces
│   │   ├── Atm.Infrastructure/  # EF Core DbContext, repositories, seeding
│   │   └── Atm.Api/             # HTTP endpoints, DI composition, error mapping
│   └── tests/
│       ├── Atm.Domain.Tests/
│       ├── Atm.Application.Tests/
│       └── Atm.Api.IntegrationTests/
└── Frontend/
    └── atm-web/                 # Vue 3 + Vite + TS
```

**Why four projects instead of one:** the main thing being graded is architecture. A layered/clean split makes the dependency direction visible (`Api → Application → Domain`, `Infrastructure → Application/Domain`), and four projects is still small enough for the time box. Going further (CQRS/MediatR, a project per feature) would be over-engineering at this scope. That is a tradeoff worth saying out loud in the interview.

---

## 3. Backend (.NET 9)

### 3.1 Domain layer — `Atm.Domain`
Pure C#, no framework references. The business invariants live here.

**Entities**
- `Account` (aggregate root, **rich domain model**: data and behavior in the same class, see §8.1)
  - `Id` (Guid), `AccountNumber` (string), `Name` (string), `Balance` (decimal), `IReadOnlyCollection<Transaction> Transactions`
  - No concurrency property on the entity. The token is an EF **shadow property** (§3.3), so persistence concerns stay out of the domain.
  - Private parameterless constructor for EF. The public factory `Account.Open(...)` is used by the seeder and tests.
  - Behavior (the only way to mutate the balance):
    - `Deposit(decimal amount, DateTime utcNow) → Transaction`
    - `Withdraw(decimal amount, DateTime utcNow) → Transaction`
    - `TransferOut(decimal amount, Guid transferId, Guid counterpartyId, DateTime utcNow) → Transaction`
    - `TransferIn(decimal amount, Guid transferId, Guid counterpartyId, DateTime utcNow) → Transaction`
  - Private setters. Guard clauses throw domain exceptions.
- `Transaction`
  - `Id`, `AccountId`, `Type` (`TransactionType`), `Amount`, `BalanceAfter`, `CreatedAtUtc`, `TransferId?` (links the two transfer legs), `CounterpartyAccountId?`, `Description`
  - Immutable once created.
- `TransactionType` enum: `Deposit`, `Withdrawal`, `TransferIn`, `TransferOut`.

**Domain services**
- `TransferService.Transfer(Account from, Account to, decimal amount, DateTime utcNow)`: checks `from != to` and creates both legs with a shared `TransferId`.

**Domain exceptions** (base `DomainException`)
- `InvalidAmountException`, `InsufficientFundsException`, `SameAccountTransferException`, `AmountLimitExceededException`

**Decision: `decimal` + guard method vs. a `Money` value object.** A `Money` record (amount + currency) is better modelling, but with one currency it mostly adds ceremony. Plan: a small static `AmountRules.Validate(decimal)` helper. A `Money` type goes on the "with more time" list.

### 3.2 Application layer — `Atm.Application`
Use cases and contracts. No EF or HTTP types.

**Interfaces (ports)**
- `IAccountRepository`: `GetAllAsync`, `GetByIdAsync`, `GetTransactionsAsync(accountId, page, pageSize)`
- `IUnitOfWork`: `SaveChangesAsync` (atomic commit, used by transfers)
- `TimeProvider` (built into .NET 8+), injected for testable timestamps

**Services (one per functional area, thin)**
- `AccountQueryService`
  - `GetAccountsAsync() → IReadOnlyList<AccountDto>`
  - `GetAccountAsync(id) → AccountDto`
  - `GetTransactionsAsync(id, page, pageSize) → PagedResult<TransactionDto>`
- `AccountCommandService`
  - `DepositAsync(id, DepositRequest) → OperationResultDto`
  - `WithdrawAsync(id, WithdrawRequest) → OperationResultDto`
  - `TransferAsync(TransferRequest) → TransferResultDto`

**DTOs** (records): `AccountDto`, `TransactionDto`, `PagedResult<T>`, `DepositRequest`, `WithdrawRequest`, `TransferRequest`, `OperationResultDto` (updated account + created transaction), `TransferResultDto` (both accounts + transfer id).

**Validation**: FluentValidation validators per request: amount > 0, ≤ 2 decimals, ≤ configured max (read from `IOptions<AtmOptions>`), required ids, `from != to`. Input validation runs here; the domain enforces its invariants again as the last line of defence. Rationale in §8.3.

**Application exceptions**: `NotFoundException` (account not found), `ConcurrencyConflictException`.

**Mapping**: hand-written extension methods in their own static classes under `Atm.Application/Mapping/` (`AccountMappings.ToDto()`, `TransactionMappings.ToDto()`). DTOs stay plain records with no logic and no knowledge of domain types. Simpler and more explicit than AutoMapper/Mapster at this size. See §8.4.

```
Atm.Application/
├── Abstractions/   IAccountRepository, IUnitOfWork
├── Accounts/       AccountQueryService, AccountCommandService
├── Contracts/      request/response DTO records
├── Validation/     *RequestValidator
├── Mapping/        AccountMappings, TransactionMappings
└── Exceptions/     NotFoundException, ConcurrencyConflictException
```

### 3.3 Infrastructure layer — `Atm.Infrastructure`
- `AtmDbContext` (EF Core 9, SQLite provider)
  - Fluent configuration in `IEntityTypeConfiguration<T>` classes
  - **Optimistic concurrency:** shadow property `Version` (`long`) on `Account`, configured with `.IsConcurrencyToken()`. `AtmDbContext.SaveChangesAsync` increments it for every `Modified` account. EF then issues `UPDATE ... WHERE Id = @id AND Version = @originalVersion`. If no row is affected, EF throws `DbUpdateConcurrencyException`. SQLite has no native `rowversion`, so we manage the counter ourselves. Why not a `DateTime` token: §8.2.
  - Index on `Transaction(AccountId, CreatedAtUtc)`
  - Store `CreatedAtUtc` as `DateTime` (UTC), not `DateTimeOffset`; the EF SQLite provider can't `ORDER BY` a `DateTimeOffset`. Secondary sort on an auto-increment `Sequence` column makes ordering deterministic for same-tick entries (e.g. the two transfer legs).
  - **UTC value converter** on every `DateTime` property: values read back from the DB come back as `DateTimeKind.Unspecified`. The converter forces `DateTimeKind.Utc`, so the JSON includes the `Z` suffix and the browser doesn't read the time as local. See §8.5.
- `AccountRepository` implements `IAccountRepository`. Read queries use `AsNoTracking()`.
- `UnitOfWork` wraps `DbContext.SaveChangesAsync`, which already runs in one DB transaction, so the transfer is atomic. Translates `DbUpdateConcurrencyException` → `ConcurrencyConflictException`.
- `DatabaseSeeder`: on startup, `EnsureCreated()` and insert two accounts if the database is empty (e.g. Checking $1,000.00 and Savings $5,000.00).
- `DependencyInjection.AddInfrastructure(IServiceCollection, IConfiguration)` extension.

**Decision: SQLite vs. EF InMemory.** The EF InMemory provider doesn't support transactions or concurrency tokens realistically, so it would hide exactly the behavior worth demonstrating (atomic transfer). SQLite is a single file, needs zero setup, and survives restarts. `EnsureCreated` instead of migrations to save time; migrations are on the "with more time" list.

### 3.4 API layer — `Atm.Api`
Minimal APIs organised into endpoint groups (one static class per area). Controllers would work equally well. Minimal APIs keep the code compact and are idiomatic for .NET 9.

**Endpoints**
| Method | Route | Body | Success | Errors |
|--------|-------|------|---------|--------|
| GET | `/api/accounts` | – | 200 `AccountDto[]` | – |
| GET | `/api/accounts/{id}` | – | 200 `AccountDto` | 404 |
| GET | `/api/accounts/{id}/transactions?page=1&pageSize=20` | – | 200 `PagedResult<TransactionDto>` | 404, 400 |
| POST | `/api/accounts/{id}/deposits` | `{ amount }` | 200 `OperationResultDto` | 400, 404 |
| POST | `/api/accounts/{id}/withdrawals` | `{ amount }` | 200 `OperationResultDto` | 400, 404, 422 |
| POST | `/api/transfers` | `{ fromAccountId, toAccountId, amount }` | 200 `TransferResultDto` | 400, 404, 422 |
| GET | `/api/atm/settings` | – | 200 `{ maxTransactionAmount, withdrawalMultiple }` | – (optional; lets the FE mirror server limits) |

**Cross-cutting**
- **Global error handling**: `IExceptionHandler` implementation + `AddProblemDetails()`:
  - `ValidationException` → 400 with `errors` dictionary
  - `NotFoundException` → 404
  - `DomainException` (e.g. insufficient funds) → 422 with a stable `code` extension (e.g. `"insufficient_funds"`) the FE can use
  - `ConcurrencyConflictException` → 409
  - anything else → 500, generic message, full exception logged (no stack trace leaked)
- **OpenAPI**: the built-in .NET 9 `AddOpenApi()` / `MapOpenApi()`, plus Scalar UI in Development.
- **Logging**: built-in `ILogger` with structured messages for each operation (account id, amount, transaction id).
- **CORS**: not needed in dev because the Vite dev server proxies `/api`. A named CORS policy for `http://localhost:5173` as a fallback.
- **Configuration**: `AtmOptions` (MaxTransactionAmount, WithdrawalMultiple) bound with `IOptions<T>` + `ValidateOnStart`.
- **Health**: `/health` via `AddHealthChecks()` (cheap to add).

**Decision: exceptions vs. Result pattern for business errors.** A `Result<T>` type avoids exceptions for expected failures and makes them explicit in signatures. Domain exceptions + one central handler is less code and very readable at this size. Plan: exceptions, and mention the Result pattern as the alternative.

### 3.5 Backend tests
| Project | Focus | Examples |
|---------|-------|----------|
| `Atm.Domain.Tests` (xUnit) | Invariants | deposit increases balance; withdraw over balance throws `InsufficientFundsException`; negative/zero/3-decimal amounts rejected; transfer creates two linked legs and preserves total |
| `Atm.Application.Tests` | Use-case orchestration | not-found account → `NotFoundException`; validator rules (withdrawal of 50 rejected, 60 accepted, rule off when `WithdrawalMultiple = 0`); uses `FakeTimeProvider` |
| `Atm.Api.IntegrationTests` | HTTP contract end to end | `WebApplicationFactory` + a **temp-file SQLite DB per test class** (a shared in-memory `SqliteConnection` is not thread-safe, which would break the concurrency tests); POST withdrawal over balance → 422 ProblemDetails; transfer updates both balances and history |

Libraries: xUnit, **AwesomeAssertions** (the Apache-2.0 fork of FluentAssertions; v8+ of FluentAssertions requires a commercial license), `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`).

**Concurrency tests (required)**
1. *Infrastructure level, deterministic:* open two `AtmDbContext` instances on the same DB and load the same account in both. Withdraw in context A and save, then withdraw in context B and save. Assert that B throws `ConcurrencyConflictException` (via `UnitOfWork`) and that the persisted balance reflects only A's withdrawal.
2. *Version increment:* after a successful save, the `Version` shadow property increased by exactly 1, for both accounts of a transfer.
3. *API level, stress:* account balance 100. Fire 10 parallel `POST /withdrawals` of 30 with `Task.WhenAll`. Assert:
   - every response is 200, 409 or 422 (never 500)
   - final balance ≥ 0 and equals `100 − 30 × (number of 200s)`, so the number of 200s is ≤ 3
   - transaction-history count equals the number of 200s, and the last `BalanceAfter` equals the balance
4. *Transfer under contention:* parallel transfers A→B and B→A. Afterwards the sum of both balances is unchanged (money is never created or destroyed).

Priority if time runs short: domain tests > concurrency tests 1 & 3 > other integration tests > application tests.

---

## 4. Frontend (Vue 3 + TypeScript)

### 4.1 Tooling
- Vite + Vue 3 (Composition API, `<script setup lang="ts">`)
- **Pinia** for state (accounts + balances shared across views)
- **Vue Router** (2 routes)
- Native `fetch` wrapped in a small typed client (no axios needed)
- Plain CSS with CSS variables (scoped styles). No UI library: it keeps things light, and design isn't graded heavily.
- **Vitest + Vue Test Utils** for a few unit tests
- ESLint + Prettier (from `npm create vue@latest` scaffolding)
- Vite `server.proxy`: `/api` → `https://localhost:<api-port>`

### 4.2 Structure
```
src/
├── api/
│   ├── httpClient.ts        # fetch wrapper: JSON, base URL, parses ProblemDetails → ApiError
│   ├── accountsApi.ts       # getAccounts, getAccount, getTransactions, deposit, withdraw, transfer
│   └── types.ts             # Account, Transaction, PagedResult, request/response types, ProblemDetails
├── stores/
│   └── accounts.ts          # Pinia store
├── composables/
│   ├── useAsyncAction.ts    # loading/error state wrapper for operations
│   └── useCurrency.ts       # Intl.NumberFormat formatting
├── components/
│   ├── AccountCard.vue
│   ├── AmountInput.vue
│   ├── OperationPanel.vue   # tabs: Deposit | Withdraw | Transfer
│   ├── DepositForm.vue
│   ├── WithdrawForm.vue
│   ├── TransferForm.vue
│   ├── TransactionTable.vue
│   ├── ReceiptDialog.vue    # "receipt" after a successful operation
│   └── AlertBanner.vue      # error/success messages
├── views/
│   ├── DashboardView.vue    # route "/"
│   └── AccountHistoryView.vue  # route "/accounts/:id"
├── router/index.ts
├── App.vue                  # layout shell: header ("ATM"), <RouterView/>
└── main.ts
```

### 4.3 Modules and functionality

**API module (`api/`)**
- `httpClient.request<T>()`: sets JSON headers. On a non-2xx response it parses the ProblemDetails body and throws a typed `ApiError { status, title, detail, code?, fieldErrors? }`. Network failures become an `ApiError` with a friendly message.
- `accountsApi`: one function per endpoint, fully typed. This is the only place that knows URLs.

**State module (`stores/accounts.ts`)**
- State: `accounts: Account[]`, `loading`, `error`
- Getters: `accountById(id)`, `transferTargets(fromId)` (all accounts except `fromId`; works for any number of accounts)
- `ApiError` with status 409 → message "The account was updated by another operation, balances refreshed — please try again" and an automatic `fetchAccounts()`
- Actions: `fetchAccounts()`, `deposit(id, amount)`, `withdraw(id, amount)`, `transfer(from, to, amount)`. After a successful operation, update balances from the response (no extra round trip) and return the result for the receipt.

**Dashboard view (`/`)**
- Two `AccountCard`s: name, masked account number, current balance, and a "View history" link
- **Account selection drives the operation panel** (low effort, ~15 min):
  - Clicking a card (or pressing Enter/Space on it, since it's a focusable `button`/`role="radio"`) selects that account. The selected card is highlighted and the panel scrolls into view.
  - Each card also has shortcut buttons (Deposit / Withdraw / Transfer) that select the account **and** open that tab.
  - Source of truth: route query `/?account=<id>&op=withdraw`. The selection survives a refresh, and the history view can link straight to it ("New operation" → `/?account=<id>`). `DashboardView` reads the query and passes `selectedAccountId` + `activeTab` as props to `OperationPanel`. Forms emit changes back, and the view updates the query with `router.replace`.
  - Defaults from the selected account: **Deposit** target = selected; **Withdraw** source = selected; **Transfer** from = selected, to = first entry of `transferTargets(from)`. The user can still change them in the form selectors. If nothing is selected, default to the first account.
  - Cards render with `v-for` in a wrapping grid, so a 3rd+ account needs no UI change.
- `OperationPanel` with three tabs:
  - **Deposit**: account selector (prefilled) + `AmountInput`
  - **Withdraw**: account selector (prefilled) + shows available balance. Amount choice as an ATM-style grid:
    - quick amounts **20 / 40 / 60 / 100 / 200 / 500**; a quick amount above the available balance is disabled
    - **"Other amount"** tile reveals an `AmountInput` for a custom value. It is validated the same way as quick amounts: positive, ≤ max, ≤ balance, and **a multiple of `withdrawalMultiple` (20)**. The hint "Amounts in multiples of $20" is shown under the field, and decimals are effectively not allowed. The FE reads the value from `GET /api/atm/settings` (hard-coded constant if time is short). If the client check is bypassed, the server returns 400 with a field error.
    - Quick amounts are generated as multiples of `withdrawalMultiple`, so they always satisfy the rule.
    - selecting a quick amount hides the custom input and vice versa. One `selectedAmount` ref feeds the submit.
  - **Transfer**: **from** selector (all accounts, prefilled with the selected account) + **to** selector (options = `transferTargets(from)`, prefilled with the first one) + amount. Changing *from* recomputes the *to* options. If the current *to* equals the new *from*, it resets to the first valid target. With two accounts the *to* selector has one option but is still shown, so the user can see the destination and the UI is ready for more accounts.
- Client-side validation mirrors the server rules (positive, 2 decimals, ≤ max, ≤ balance for withdraw/transfer) for instant feedback. The server stays the source of truth.
- Submit button disabled while the request is pending (prevents double submission)
- On success: `ReceiptDialog` shows the type, amount, new balance, transaction id and time. The form resets.
- On error: `AlertBanner` shows the ProblemDetails `detail`, and field errors appear inline.

**Account history view (`/accounts/:id`)**
- Header with account name and balance
- `TransactionTable`: date/time, type (badge), description (e.g. "Transfer to Savings"), signed amount (+green / −red), balance after
- Pagination (prev/next) using `PagedResult`
- Optional client-side filter by type
- Empty state ("No transactions yet") and loading skeleton

**Shared UI**
- `AmountInput`: numeric input that limits to 2 decimals, `$` prefix, emits a `number`
- `useCurrency().format(amount)`: `Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })`
- Accessibility basics: labels on inputs, `aria-live` on alerts, keyboard-usable tabs
- Responsive: cards stack on narrow screens

### 4.4 Frontend tests (light)
- `AmountInput` rejects >2 decimals and negatives
- `WithdrawForm`: quick amounts above the balance are disabled; "Other amount" shows the custom input and rejects non-multiples of 20; the selected account from props is the default source
- Accounts store: `withdraw` updates the balance from the API response; an `ApiError` is surfaced (with `accountsApi` mocked)
- `httpClient` maps a ProblemDetails response to `ApiError`

---

## 5. Execution Order and Time Budget (~4h)

| Phase | Task | Est. |
|-------|------|------|
| 1 | Scaffold solution/projects, references, `.gitignore`, git init | 15 min |
| 2 | Domain entities + exceptions + domain unit tests | 35 min |
| 3 | Application services, DTOs, validators | 30 min |
| 4 | Infrastructure: DbContext, config, repository, seeder | 25 min |
| 5 | API: endpoints, exception handler / ProblemDetails, OpenAPI, options | 25 min |
| 6 | Integration tests (happy path + insufficient funds + transfer) + concurrency tests | 35 min |
| 7 | Frontend scaffold, proxy, API client, store | 25 min |
| 8 | Dashboard + account selection + operation forms (incl. "Other amount") + receipt/errors | 45 min |
| 9 | History view + pagination | 20 min |
| 10 | README (setup, architecture, decisions, tradeoffs, "with more time") | 20 min |

**Cut list if behind schedule** (in order): FE tests → type filter on history → receipt dialog (replace with a success banner) → application-layer tests. Domain tests, error handling and the README are never cut.

---

## 6. README Outline (deliverable)
1. Overview and screenshot
2. Prerequisites: .NET 9 SDK, Node 20+
3. Run: `dotnet run --project Backend/src/Atm.Api` · `cd Frontend/atm-web && npm install && npm run dev`
4. Test: `dotnet test Backend/Atm.sln` · `npm run test:unit`
5. API docs URL (Scalar)
6. Architecture diagram (layers + dependency direction)
7. Key design decisions and tradeoffs (SQLite vs InMemory, exceptions vs Result, Minimal APIs, `decimal` vs `Money`, optimistic concurrency, `EnsureCreated` vs migrations)
8. What I'd do with more time: auth + multi-user, idempotency keys on POSTs, `Money` value object + multi-currency, EF migrations, Docker Compose, CI (GitHub Actions), E2E tests (Playwright), daily withdrawal limits, audit log

---

## 7. Interview Talking Points (prep)
- **Why balance is stored *and* history is recorded:** fast reads vs. pure event-sourced derivation. `BalanceAfter` makes reconciliation possible.
- **How transfer atomicity is guaranteed:** one aggregate operation per account, a single `SaveChanges`, one DB transaction.
- **Concurrency:** what happens with two simultaneous withdrawals (version token → 409 → client refreshes and retries), and how the tests prove it. A server-side automatic retry (reload + re-apply, up to N times) is a possible improvement.
- **Where validation lives and why it appears twice:** FE for UX, application layer for input, domain for invariants.
- **Why `decimal` and never `double` for money.**
- **HTTP status choices:** 400 malformed vs 422 business-rule violation vs 409 conflict.

---

## 8. Design Q&A (decisions log)

### 8.1 Behavior in the model classes: rich vs. anemic domain model
**Decision: rich model.** `Account` owns `Deposit/Withdraw/TransferIn/TransferOut`, and its setters are private.
- **Pros:** invariants (no overdraft, valid amount, history written with each balance change) live in one place and can't be bypassed. No other code can do `account.Balance -= x`. They're unit-testable without mocks, and the app services stay thin orchestrators.
- **Anemic alternative** (data-only entities + `AccountService` holding the rules): familiar and CRUD-friendly, but the rules can be skipped by any code that touches the entity, and they tend to spread across services.
- **Guardrails so it stays clean:** no EF attributes in the domain (Fluent config in Infrastructure), the concurrency token is a shadow property, time is passed in rather than read from `DateTime.UtcNow`, and logic that spans two accounts (transfer) lives in a small domain service rather than on one entity.

### 8.2 `[ConcurrencyCheck]` on a `DateTime` (e.g. `LastModifiedUtc`) vs. a version counter
**Decision: version counter (`long`, shadow property).** A `DateTime` token is worse here:
- **Resolution/collisions:** two updates in the same tick, or a clock that doesn't move forward (VM clock adjustments, NTP corrections), give the same value, and the conflict goes undetected. A counter always changes.
- **Round-trip precision:** SQLite stores `DateTime` as TEXT. Any precision or format difference between the written value and the value read back makes the `WHERE Version = @original` comparison fail, giving false conflicts.
- **Mixes concerns:** a timestamp used for auditing *and* concurrency couples two things. If we want "last modified", add it as a separate, non-token column.
- *On SQL Server* the idiomatic choice would be `rowversion` (`[Timestamp]` / `.IsRowVersion()`), which the DB maintains itself. That's also a counter, not a time.

### 8.3 Validation library ("awesome validations" vs. FluentValidation)
I don't know of a widely used .NET validation library called "Awesome Validations". You may be thinking of **AwesomeAssertions**, the community fork of **FluentAssertions** made after FluentAssertions v8 moved to a commercial license. That library covers *test assertions*, not request validation.
- **Request validation: keep FluentValidation.** It is still Apache-2.0/free. It handles rules that depend on config (max amount from `IOptions`), cross-field rules (`from != to`), and is easy to unit-test. One small package.
- **Alternatives considered:** DataAnnotations (no dependency, but awkward for config-driven and cross-field rules), and built-in Minimal API validation (`AddValidation()`), which exists only in .NET 10, so it's not available on .NET 9.
- **Tests: use AwesomeAssertions** instead of FluentAssertions to avoid the license issue. *(Confirmed: the name was the mix-up.)*

### 8.4 Mappers separate from DTOs?
**Yes.** DTOs are plain `record`s in `Contracts/`, and mapping lives in static extension classes in `Mapping/` (`AccountMappings`, `TransactionMappings`).
- DTOs stay pure data contracts with no dependency on domain types, so they could be moved to a shared contracts package unchanged.
- One place to look for "how does X become Y" (single responsibility); the mappings are trivially unit-testable.
- The alternative, `static AccountDto FromDomain(Account a)` on the DTO, is fine for tiny apps but couples the contract to the domain.

### 8.5 If we used SQL Server / MySQL, would `DateTimeOffset` be better? *(informational, the DB stays SQLite)*
- **SQL Server:** native `datetimeoffset`. EF maps, filters and orders it correctly (ordering is by the UTC instant). Here `DateTimeOffset` **would be the better choice**: the value is unambiguous by type, and it avoids the `DateTimeKind.Unspecified` problem we have to fix with a converter for `DateTime`.
- **MySQL:** has no offset-aware type. The Pomelo provider stores `DateTimeOffset` as `datetime(6)` converted to UTC and drops the offset, so you gain little over `DateTime` UTC.
- **PostgreSQL** (for completeness): `timestamptz` stores a UTC instant, and Npgsql only accepts `DateTimeOffset` with offset 0. It works fine but holds no extra information.
- **Takeaway:** for server-generated timestamps (always UTC) the offset carries no business information. What matters is that the value is *unambiguously UTC* end to end. `DateTimeOffset` gives that for free on SQL Server; on SQLite we get it with `DateTime` + a UTC converter. (Another option on SQLite: keep `DateTimeOffset` in the model and store it as `long` ticks with `DateTimeOffsetToBinaryConverter`, which is sortable.)
