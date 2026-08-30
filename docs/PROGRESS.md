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

