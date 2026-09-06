# Build Roadmap

No deadlines here — this is a map, not a race. Each phase lines up with a phase in your
.NET learning path, so you build the project *while* you learn the thing it needs, not
before. Skip ahead, repeat, or slow down freely. Tick boxes off as you go and jot notes
in `docs/PROGRESS.md`.

## Phase 1 — Domain modeling (C# basics)
- [x] `Sensor`, `SensorReading`, `SensorType` classes in `SmartHome.Domain`
- [x] A console app (`SmartHome.ConsoleSim`) that creates a few fake sensors and prints readings
- **Ready when:** you can run the console app and see believable sensor data printed.

## Phase 2 — LINQ over the simulated data
- [x] Use `.Where()` to find readings above/below a threshold (e.g. temp > 26°C)
- [x] Use `.Select()` to project readings into a summary shape
- [x] Use `.GroupBy()` to group readings by sensor or by hour
- [x] Use `.Any()` to detect "is anything currently abnormal?"
- **Ready when:** your console app can print a one-line "status report" of the house using LINQ.

## Phase 3 — Async simulation loop
- [x] Turn the simulator into a loop using `async`/`await` (e.g. a new reading every few seconds)
- [x] Wrap sensor generation in `try`/`catch` so one bad sensor doesn't crash the app
- **Ready when:** you understand why the loop doesn't freeze the console while "waiting."

## Phase 4 — Persistence with EF Core
- [x] Add a real database (SQLite to start, Postgres later) for sensor readings
- [x] EF Core migrations for the `Sensors` and `SensorReadings` tables
- **Ready when:** readings survive a restart of the app.

## Phase 5 — Turn it into a Web API
- [x] `SmartHome.Sensors.Api` — ASP.NET Core Web API with endpoints like `GET /sensors`,
      `GET /sensors/{id}/readings`
- [x] Dependency injection wiring for the DbContext and services
- **Ready when:** you can hit the API from Postman/curl and get real JSON back.

## Phase 6 — Clean Architecture + CQRS
- [x] Split into `Domain` / `Application` / `Infrastructure` / `Api` layers (you've done this before!)
- [ ] Command: `RecordReadingCommand` — deferred until the simulator becomes a real service in
      Phase 7 (for now `ConsoleSim` still writes readings directly via `DbContext`, no DI
      container there yet)
- [x] Queries: `GetCurrentStatusQuery`, `GetSensorHistoryQuery`
- [x] MediatR + FluentValidation, same shape as your existing project
- **Ready when:** you can explain where new sensor-related code should live.

## Phase 7 — Microservices + MCP (the fun part!) ← you are here
- [ ] Split out a second service: `SmartHome.Actions` (proposed actions + approval state)
- [ ] Add RabbitMQ; Sensors publishes events, Actions subscribes
- [ ] Add `SmartHome.Notify` — a small service with a Telegram bot that pings you for approval
- [ ] Build `SmartHome.McpGateway` using `ModelContextProtocol.AspNetCore`, exposing tools:
      `get_house_status`, `propose_action`, `list_pending_approvals`
- [ ] Test it with the official **MCP Inspector** (a small dev tool from the MCP project
      that lets you call your tools by hand, no AI needed, before connecting a real client)
- [ ] Connect a real MCP client — e.g. Claude Desktop — and have an actual conversation
      about your (simulated) house
- **Ready when:** you can ask "is anything weird going on at home?" through an MCP client
  and it calls your API to answer for real.

## Phase 8 — Observability with Prometheus + Grafana
- [ ] Add the `prometheus-net.AspNetCore` package to each service and expose a `/metrics` endpoint
      (a plain-text page listing numbers like request counts — Prometheus reads it periodically)
- [ ] Add a couple of your own custom metrics, not just the free built-in ones — e.g. a gauge for
      "current temperature per sensor" or a counter for "pending approvals created"
- [ ] Run Prometheus in Docker (`observability/prometheus.yml` in this repo already has a starter
      scrape config) — it polls each service's `/metrics` endpoint and stores the history
- [ ] Run Grafana in Docker, add Prometheus as a data source, and build one dashboard with a
      couple of panels (e.g. a line graph of temperature over time, a counter of pending approvals)
- **Ready when:** you can leave the simulator running, open Grafana, and watch real graphs update
  as fake sensor readings come in.
- *Interview angle:* "How would you know if this service was unhealthy in production?" —
  now you have a real answer: metrics + a dashboard, not just "check the logs."

## Phase 9 — Polish + practice
- [ ] Unit tests for the Application layer (command/query handlers)
- [ ] Global error handling middleware in each API
- [ ] Light auth on the APIs (API key or JWT) — the gateway shouldn't be wide open
- [ ] Record a short demo (GIF or video) for your README — this time you can show off the
      Grafana dashboard updating live, which looks great in a portfolio
- [ ] Practice explaining the architecture out loud in 2 minutes

## A note on scope

You do not need Phase 7 to be a "real" smart home with real hardware — it's a simulation,
and that's fine. Interviewers care about the architecture and the reasoning, not whether a
real lightbulb turned off. Keep the domain playful; keep the engineering real.
