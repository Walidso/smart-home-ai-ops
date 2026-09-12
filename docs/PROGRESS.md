# Progress Log

Keep this short — a few lines per session. It becomes both your portfolio story and your
interview talking points ("walk me through how this project evolved").

## Template (copy for each entry)

### YYYY-MM-DD — Phase X: <what you worked on>
- **Built:** what you added/changed
- **Learned:** the concept that clicked
- **Stuck on:** anything confusing (comes back around later, totally normal)
- **Next:** what you're doing next session

---

<!-- Add entries below, newest at the top -->

### 2026-09-12 — Phase 7 (part 3): SmartHome.Notify, a real Telegram approval loop
- **Built:** `SmartHome.Notify`, a Worker Service (no HTTP listener — just background
  services) that closes the human-in-the-loop story for real. `Actions.Api`'s
  `ProposeActionCommandHandler` now publishes an in-process MediatR `ActionProposedNotification`
  after saving; a handler for that notification forwards it out over RabbitMQ via a new
  `IActionEventPublisher` interface (defined in Application, implemented with RabbitMQ in Api —
  dependency inversion, so Application never references RabbitMQ.Client directly). `Notify`
  subscribes to that queue and sends a Telegram message with inline Approve/Reject buttons
  (`Telegram.Bot`, long-polling — no public webhook needed for local dev). Tapping a button
  calls straight back into `Actions.Api`'s `/actions/{id}/approve|reject` endpoints and edits
  the Telegram message in place with the result. Bot token and chat id live in .NET's Secret
  Manager (`dotnet user-secrets`), never in a committed file.
- **Learned:** the difference between a MediatR notification (in-process pub/sub — multiple
  handlers in the *same* app can react to one event) and a RabbitMQ message (pub/sub *between*
  processes) — this feature uses both, one after the other. Also practiced dependency
  inversion for real: `IActionEventPublisher` is declared where it's *needed* (Application),
  not where it's *implemented* (Api).
- **Verified live:** proposed an action via HTTP, watched it arrive on Telegram with buttons
  within a couple seconds, tapped Approve, watched the message edit in place to "✅ Approved.",
  and confirmed via `GET /actions/pending` and the Actions.Api SQL logs that the underlying row
  really updated — not just a UI illusion on the Telegram side.
- **Stuck on:** nothing blocking. Telegram.Bot 22.x's API (mostly de-`Async`-suffixed method
  names, e.g. `SendMessage` not `SendTextMessageAsync`) was a first-try guess based on the
  installed version and compiled correctly without a fix-up pass.
- **Next:** the MCP gateway — `SmartHome.McpGateway` exposing `get_house_status`,
  `propose_action`, and `list_pending_approvals` as MCP tools, tested with the MCP Inspector
  before connecting a real client like Claude Desktop.

---

### 2026-09-08 — Phase 7 (part 2): RabbitMQ ties Sensors and Actions together
- **Built:** Turned on the `rabbitmq` service in `docker-compose.yml` and wired real pub/sub
  between the two services. `ConsoleSim` now publishes a `TemperatureAnomalyEvent` (JSON, no
  shared C# type between services — the message schema is the contract) whenever a temperature
  reading exceeds the Phase 2 threshold, deduped with a `HashSet<Guid>` so a sensor stuck hot
  for several 3-second cycles in a row triggers one event, not one every cycle. `Actions.Api`
  runs a `BackgroundService` (`TemperatureAnomalyConsumer`) that listens on that queue and
  calls the existing `ProposeActionCommand` for each event — so a hot reading in the simulator
  turns into a real pending approval in Actions, with no HTTP call between the two services.
- **Learned:** a message broker decouples publisher from subscriber in a way direct HTTP calls
  can't — `ConsoleSim` doesn't know or care whether `Actions.Api` is even running; messages just
  queue up in RabbitMQ until a listener picks them up. Also: a `BackgroundService` is itself a
  singleton, but MediatR/DbContext are scoped, so the consumer opens a fresh DI scope per
  message (`IServiceScopeFactory`) — same short-lived unit-of-work idea as everywhere else in
  this project. And: services come up independently in a microservices system, so
  `TemperatureAnomalyConsumer` retries its RabbitMQ connection on a fixed delay instead of
  crashing the Api if the broker isn't ready yet at startup.
- **Stuck on:** nothing blocking — RabbitMQ.Client 7.x's fully-async API (`IChannel`,
  `BasicPublishAsync`, `AsyncEventingBasicConsumer`) was a first-try guess based on the
  library's version, and it compiled and worked correctly without needing a fix-up pass.
- **Verified live:** ran the simulator until it hit several hot readings, watched `[EVENT]`
  lines print as they published, then confirmed matching pending approvals appeared in
  `GET /actions/pending` with the right room/value text — and that approving one made it drop
  out of the pending list, same as a manually-proposed action.
- **Next:** `SmartHome.Notify` (Telegram bot) needs a bot token from @BotFather — that's a
  manual step outside this session. After that: the MCP gateway.

---

### 2026-09-08 — Phase 7 (part 1): SmartHome.Actions.Api, the second microservice
- **Built:** A genuinely separate second service — `SmartHome.Actions.Domain` /
  `.Infrastructure` / `.Application` / `.Api`, own SQLite db (`actions.db`), no shared
  projects with the Sensors service. `ProposedAction` is a richer domain entity than
  `Sensor`/`SensorReading`: `Approve()`/`Reject()` live on the entity itself and throw if the
  action isn't `Pending`, so "can't approve twice" is enforced by the domain, not scattered
  across handler code. Commands: `ProposeActionCommand`, `ApproveActionCommand`,
  `RejectActionCommand` (the latter two return a `DecisionOutcome` enum — `Success` /
  `NotFound` / `AlreadyDecided` — mapped to 204/404/409, rather than using exceptions for
  expected business outcomes). Query: `ListPendingApprovalsQuery`. Duplicated the
  `ValidationBehavior` pipeline behavior from the Sensors service rather than sharing a
  library — independent services shouldn't share internal code across the service boundary,
  even for small cross-cutting pieces.
- **Learned:** applied the DateTimeOffset-ordering lesson from Phase 5/6 proactively this
  time — `Enum.ToString()` inside an EF Core `.Select()` projection hits the same kind of SQL
  translation wall, so `ListPendingApprovalsQueryHandler` fetches entities first and maps to
  DTOs (including the enum-to-string conversion) in memory, before even trying it the naive
  way. Also: modeling "already decided" as a return value (`DecisionOutcome`) instead of a
  thrown exception keeps exceptions reserved for actually-exceptional situations.
- **Stuck on:** nothing blocking. RabbitMQ, the Telegram-based Notify service, and the MCP
  gateway are still ahead — deliberately sequenced after this so the approval workflow existed
  and was fully tested standalone first.
- **Next:** RabbitMQ next (needs Docker running) — Sensors publishes an event, Actions
  subscribes and can auto-propose an action from it.

---

### 2026-09-06 — Phase 6: Clean Architecture + CQRS (Api side)
- **Built:** New `SmartHome.Application` project holding the CQRS slice for the Api:
  `GetSensorHistoryQuery` (replaces the old inline `GET /sensors/{id}/readings` logic, now
  returns `null` for "sensor doesn't exist" vs. an empty list for "exists, no readings yet")
  and a new `GetCurrentStatusQuery` (latest reading per sensor + an `AnythingHot` flag,
  backing a new `GET /status` endpoint — full-circle back to the Phase 2 LINQ report, now
  served over HTTP). Wired MediatR + a `ValidationBehavior` pipeline behavior into the Api so
  any FluentValidation validator registered for a request runs automatically before its
  handler. Scoped this to the Api only — `ConsoleSim` still writes readings directly via
  `DbContext`; `RecordReadingCommand` is deferred until Phase 7 gives the simulator a real DI
  container.
- **Learned:** a MediatR pipeline behavior wraps every request before its handler runs — the
  place for cross-cutting stuff like validation, so individual handlers don't repeat it. Also
  hit a fun real bug: FluentValidation's default error messages auto-localize based on the
  server's OS culture — this machine produced Swedish ("måste anges") until pinned to English
  with `ValidatorOptions.Global.LanguageManager.Enabled = false`. An API's error text
  shouldn't depend on which machine happens to be hosting it.
- **Stuck on:** nothing blocking. Deliberately left `GET /sensors` as a plain `DbContext` call
  in the endpoint rather than wrapping it in a query — it's a one-liner with no real logic, so
  a query/handler pair would just be ceremony.
- **Next:** Phase 7 — split out `SmartHome.Actions` as a second service, add RabbitMQ so
  Sensors can publish events Actions subscribes to, and start on the MCP gateway.

---

### 2026-09-03 — Phase 5: ASP.NET Core Web API over the shared database
- **Built:** Extracted `SmartHomeDbContext` and its migrations out of `SmartHome.ConsoleSim`
  into a new shared `SmartHome.Infrastructure` class library (referenced by both apps) so the
  simulator and API don't duplicate/diverge on schema. Fixed a real bug along the way: a
  relative SQLite connection string resolves against whatever the *current process's* working
  directory happens to be, so the simulator and a second app could easily end up writing to two
  different database files depending on how each was launched. Fixed by resolving the db path
  from the running assembly's location up to the solution file, so it's always the same file
  regardless of launch method. Added `SmartHome.Sensors.Api` (ASP.NET Core minimal API, net8.0)
  with `SmartHomeDbContext` wired through DI (`AddDbContext`, scoped per request) and two
  endpoints: `GET /sensors` and `GET /sensors/{id}/readings` (404 for an unknown sensor id).
- **Learned:** minimal API route handlers get services auto-injected by declaring them as
  parameters — no manual resolution needed. Also hit and fixed a real EF Core + SQLite
  limitation: the SQLite provider can't translate `ORDER BY` on a `DateTimeOffset` column into
  SQL, so readings are pulled with `ToListAsync()` first and sorted client-side in C# instead.
- **Stuck on:** nothing blocking — just the two gotchas above (db path resolution, DateTimeOffset
  ordering), both fixed and left as code comments explaining why.
- **Next:** Phase 6 — split into proper Clean Architecture layers (`Domain`/`Application`/
  `Infrastructure`/`Api`) with MediatR + FluentValidation, same shape as the other project.

---

### 2026-08-30 — Phase 4: EF Core persistence with SQLite
- **Built:** Added `Microsoft.EntityFrameworkCore.Sqlite`/`.Design` (pinned to 8.0.11 to match
  the net8.0 target) to `SmartHome.ConsoleSim`, added an `Id` primary key to `SensorReading`,
  and created `SmartHomeDbContext` (`Data/SmartHomeDbContext.cs`) with `DbSet<Sensor>` and
  `DbSet<SensorReading>`. Generated the `InitialCreate` migration and wired `Program.cs` to
  call `Database.Migrate()` on startup, seed the 5 sensors only on the very first run (loads
  them from the DB on later runs instead), and persist each batch of readings — plus print an
  all-time reading count pulled back from the DB to prove it survives restarts.
- **Learned:** EF Core migrations are versioned C# "recipes" (`Up`/`Down`) for schema changes,
  applied via `Database.Migrate()`. Also learned why a long-running loop should open a *new*,
  short-lived `DbContext` per unit of work instead of reusing one for the app's whole lifetime
  — the change tracker only grows the longer a context stays alive, so one shared context in
  a forever-loop is a slow memory leak.
- **Stuck on:** the globally installed `dotnet-ef` CLI tool is v10.x while the project's EF Core
  packages are 8.0.11 (net8.0 doesn't support EF Core 10). It worked fine for `migrations add`,
  but this version mismatch is worth remembering if the EF CLI ever misbehaves later — the fix
  would be a locally pinned `dotnet-ef` via a tool manifest.
- **Next:** Phase 5 — turn this into `SmartHome.Sensors.Api`, an ASP.NET Core Web API with
  `GET /sensors` and `GET /sensors/{id}/readings`.

---

### 2026-08-30 — Phase 3: async simulation loop
- **Built:** Turned `Program.cs` into an infinite loop — sensor reading generation and the
  LINQ status report now run inside `while (true)`, with `await Task.Delay(3000)` between
  batches so it prints a fresh report every 3 seconds instead of exiting after one. Wrapped
  the per-sensor reading generation in `try`/`catch` so one bad sensor logs a `[WARN]` and
  gets skipped instead of crashing the whole loop.
- **Learned:** top-level statements don't need a hand-written `async Task Main` — the
  compiler generates one automatically as soon as the file contains an `await`. Also, why
  `await Task.Delay(3000)` doesn't freeze the console: it hands the thread back to the
  runtime instead of blocking it (unlike `Thread.Sleep`), and resumes where it left off
  once the timer fires.
- **Stuck on:** nothing — the try/catch is a safety net for now since nothing in the
  current reading logic actually throws yet; it'll matter more once real sensor/DB reads
  are involved.
- **Next:** Phase 4 — add EF Core persistence (SQLite to start) so readings survive a
  restart.

---

### 2026-08-28 — Phase 1 & 2: caught up the roadmap to match the code
- **Built:** Nothing new today — reviewed the repo and found Phase 1 (Sensor/SensorReading/SensorType +
  ConsoleSim) and Phase 2 (Where/Select/GroupBy/Any in Program.cs) were already fully working code,
  just with unchecked boxes in ROADMAP.md and no log entry. Checked them off so the roadmap reflects reality.
- **Learned:** the project was further along than the checklist showed — good reminder to update
  PROGRESS.md right after writing code, not days later.
- **Stuck on:** nothing yet.
- **Next:** Phase 3 — wrap the simulator in an async loop (`async`/`await`) so it produces a new
  batch of readings every few seconds instead of running once and exiting.

