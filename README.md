# SmartHome AI-Ops

A .NET microservices backend for a simulated smart home, with its own **MCP server** —
built to show off C#, ASP.NET Core, Clean Architecture, CQRS, messaging between services,
and the brand-new [Model Context Protocol (MCP)](https://github.com/modelcontextprotocol/csharp-sdk) C# SDK.

## The idea, in plain English

**MCP** is a standard that lets AI assistants plug into your own code and use it like a
tool — think of it like a USB port: any AI client that "speaks" MCP (Claude Desktop, the
official MCP Inspector dev tool, or others) can plug into *your* MCP server and ask it
things, without you writing custom integration code for each one.

This project builds a small "nervous system" of .NET microservices for a fake house, then
puts an MCP server in front of them. Once it's running, you can open an MCP client and:

- ask "what's the temperature in the living room right now?" (a **query**)
- ask it to check for energy usage spikes
- ask it to propose an action (e.g. "turn off the office heater, it's been on for 6 hours")
  — which your backend stores as a **pending approval** rather than doing immediately,
  because a good backend never lets a caller (AI or otherwise) silently take real-world
  actions without a check

That last point — human-in-the-loop approval before anything "real" happens — is a real
production pattern (used for anything risky: payments, infra changes, etc.) and makes a
great interview story, independent of the AI angle entirely.

No AI agent framework to install or host — you build the MCP server, and any existing
MCP client can plug into it. That's the whole point of the protocol.

## Why this stands out on a GitHub portfolio

Most junior .NET portfolios have a CRUD app or a to-do list. This project instead shows:

- **Clean Architecture + CQRS** applied across *multiple* independent services, not just one app
- **Async messaging** between services (a message bus), not just direct HTTP calls
- **A working integration with MCP**, a protocol that's actively being adopted industry-wide in 2026 — very few junior candidates will have built a C# MCP server themselves
- **A safety-conscious design** (approval workflow before actions), which shows product thinking, not just code-writing
- **Real observability** — Prometheus + Grafana dashboards showing live metrics, the same pattern used to monitor production backend systems

## Architecture (grows over the project's phases — see ROADMAP.md)

```
                     ┌─────────────────────┐
                     │   Any MCP client      │  (Claude Desktop, MCP Inspector, etc.)
                     │  — you choose one     │
                     └──────────┬───────────┘
                                │  MCP (tool calls)
                     ┌──────────▼───────────┐
                     │   McpGateway (ASP.NET  │  translates tool calls into
                     │   Core + MCP C# SDK)   │  Commands/Queries
                     └──────────┬───────────┘
                                │  message bus (RabbitMQ)
              ┌─────────────────┼─────────────────┐
              │                 │                 │
     ┌────────▼───────┐ ┌───────▼────────┐ ┌──────▼───────┐
     │ Sensors Service │ │ Actions Service │ │ Notify Service│
     │ (Clean Arch +   │ │ (approval       │ │ (Telegram bot │
     │  CQRS, own DB)  │ │  workflow)      │ │  for approvals)│
     └─────────────────┘ └────────────────┘ └───────────────┘
```

Each service on the bottom row is a self-contained microservice with its own `Domain` / `Application` / `Infrastructure` / `Api` layers — the same shape as your existing Clean Architecture + CQRS project, just repeated per service.

## Tech stack

- **C# / .NET 8** — services and simulator
- **ASP.NET Core** — Web APIs
- **EF Core** — persistence per service
- **MediatR + FluentValidation** — CQRS inside each service
- **RabbitMQ** (or Azure Service Bus) — async messaging between services
- **ModelContextProtocol.AspNetCore** (official MCP C# SDK) — your own MCP server, which any MCP client can plug into
- **Prometheus + Grafana** — metrics collection and dashboards, so the system's health is actually visible, not just logged
- **Docker Compose** — runs the whole system together

## Status

🚧 Early scaffold — see `ROADMAP.md` for the phase-by-phase build plan and `docs/PROGRESS.md` for the running build log.

## Getting started (current phase)

```bash
cd src/SmartHome.ConsoleSim
dotnet run
```

This runs the sensor simulator console app — the very first building block. Later phases turn this into real services (see ROADMAP.md).
