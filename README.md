# Torvex

One ASP.NET Core app serving the Torvex platform:

- **[torvex.app](https://torvex.app)** — Torvex IT Solutions: service packages, contact/leads, customer dashboard
- **[community.torvex.app](https://community.torvex.app)** — the Torvex community: Discord-style servers, chat, DMs, orbs economy, and a text-based RPG
- **[dashboard.torvex.app](https://dashboard.torvex.app)** — dashboard for the Torvex Forerunner Discord bot (separate repo)

**Stack:** ASP.NET Core Razor Pages + SignalR on .NET 10, Clean Architecture (API / Application with CQRS-MediatR / Infrastructure with EF Core / Domain), PostgreSQL, Stripe.

> The repo, solution, and namespaces keep the project's original working name (`peeposredemption`); everything user-facing is branded Torvex.
