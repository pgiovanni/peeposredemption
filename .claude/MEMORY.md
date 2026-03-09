# PeePo's Redemption - Project Memory

## DB Connection
Host=localhost;Port=5432;Database=peeposredemption;Username=postgres;Password=<ask user>

**Only ask for the DB password if connecting via psql directly. `dotnet ef database update` reads from appsettings.json and doesn't need it.**

## What It Is
A Discord-like communication platform (ASP.NET Core, .NET 10). Features: servers, channels, real-time chat, invites, DMs.

## Architecture
Clean Architecture with 4 layers:
- **API**: Razor Pages + SignalR hub (ChatHub)
- **Application**: CQRS via MediatR (commands/queries in Features/)
- **Infrastructure**: EF Core + PostgreSQL (Npgsql), Repositories, UnitOfWork
- **Domain**: Entities + Interfaces

Patterns: CQRS, Repository, Unit of Work, DI

## Key Tech
- ASP.NET Core Razor Pages + SignalR
- JWT auth (cookies + query string for SignalR)
- PostgreSQL via EF Core (snake_case naming convention)
- MediatR 14, FluentValidation, BCrypt
- Email: Resend (prod) / MailHog localhost:1025 (dev) — `IEmailService` interface swapped via `builder.Environment.IsDevelopment()`
- MailHog binary at `%USERPROFILE%/mailhog.exe`, web UI at http://localhost:8025

## Domain Entities
- User, Server, Channel, Message, ServerMember (composite PK), ServerInvite, DirectMessage, FriendRequest

## Moderation System
- `ServerRole` enum (Member=0, Moderator=1, Owner=2) on `ServerMember.Role`
- Owner auto-assigned on server create
- Kick/Ban (Owner only), Delete message/channel (Moderator+)
- `BannedMember` table enforced on invite join
- `ModerationLog` table — all actions logged with moderator + target
- Link scanner (`LinkScannerService`) blocks IP grabbers — merges both sources: hardcoded `FallbackDomains` array + remote GitHub repo list (`piperun/iploggerfilter/filterlist`). On fetch success, remote domains are loaded and all `FallbackDomains` are unioned in. On fetch failure, falls back to `FallbackDomains` only. Refreshes every 24h.
- `Message.IsDeleted` soft delete, shown as "[message deleted]"
- Server Settings page at `/App/ServerSettings?serverId=X` — member list + audit log
- Channel sidebar: ⚙ gear link (Owner only), channel delete (Mod+), message delete button (Mod+)

## Features Implemented
- Auth: register, login, logout (JWT cookie deleted), email confirmation, resend confirmation
- Servers: create (auto-creates "general" channel), invite codes, join via invite
- Channels: list, text/voice/announcement types
- Messages: send, persist, paginated history
- DMs: real-time broadcast + DB persistence (SendDirectMessageCommand saves to DB via UoW)
- SignalR: group-based channel messaging, user-based DMs, typing indicators
- Friends: send/accept/reject requests, view friends list

## VPS / Production State
- **VPS**: 187.77.215.240, user: root, Debian 13 (trixie)
- **SSH**: Key-based auth works from dev machine (BatchMode=yes, no password needed)
- **Claude SSH**: Non-interactive only — use `ssh -o BatchMode=yes root@187.77.215.240 "command"` or heredoc
- **App deployed to**: `/var/www/peeposredemption/` (published Release build)
- **Systemd service**: `peeposredemption.service` — enabled, running, auto-restarts
  - Runs as `www-data`, `ASPNETCORE_ENVIRONMENT=Production`
  - `ExecStart=/usr/bin/dotnet /var/www/peeposredemption/peeposredemption.API.dll`
- **nginx**: installed, running, site config at `/etc/nginx/sites-available/peeposredemption`
  - Reverse proxy: `torvex.app` → `localhost:5000`
  - WebSocket map in `/etc/nginx/conf.d/websocket.conf`
- **PostgreSQL 17**: installed, running, DB `peeposredemption` created, all migrations applied
  - Peer auth for postgres OS user: `sudo -u postgres psql` (no password needed via shell)
  - Password auth from app uses password in appsettings.Production.json
- **Domain**: `torvex.app` (NOT torvex.app — earlier memory was wrong)
- **App status**: Running and returning 302 → /Auth/Login through nginx ✓

## Production Config
- `appsettings.Production.json` exists in repo and deployed to VPS
- Kestrel: HTTP only on port 5000 (nginx handles TLS)
- `AppBaseUrl`: `https://torvex.app`
- `Email:From`: `noreply@torvex.app`
- `Email:AdminEmail`: `pgiovanni1234@gmail.com`
- JWT Issuer/Audience: `https://torvex.app`
- **DO NOT store secrets in memory** — ask user if needed

## Production Status — FULLY LIVE AND TESTED at https://torvex.app
- DNS: `torvex.app` + `www.torvex.app` A records → `187.77.215.240` ✓
- SSL: Let's Encrypt cert covers both `torvex.app` and `www.torvex.app`, auto-renewal ✓ (expires 2026-06-06)
- Hostinger firewall: Accept TCP 80, 443, 22 + Drop Any ✓
- Resend: `torvex.app` domain verified, all DNS records propagated (DKIM, SPF, DMARC, MX) ✓
- MediatR: downgraded to 12.4.1 (free/MIT) — was 14.0.0 ($489/yr license) ✓
- Emails send from `noreply@torvex.app` via Resend ✓
- DataProtection keys persisted to `/var/www/peeposredemption-keys` — antiforgery survives restarts ✓
- Registration + email confirmation flow fully tested and working ✓
- Bug fixed: ResendConfirmation.cshtml was missing `asp-antiforgery="true"` on form
- Admin signup notification email: fires on every new registration to pgiovanni1234@gmail.com
- NOTE: Claude triggered a test email (testuser/test@test.com) to verify the feature — that "[Torvex] New user registered" in Resend/Gmail is the test, not a real user
- verygoodname registered at 4:08 PM EST but no notification was received — under investigation

## VPS Monitoring
- Script: `/usr/local/bin/vps-monitor.sh` — runs every 5 min via cron
- Log: `/var/log/vps-monitor.log` — CPU, MEM, DISK, app status, nginx status
- To check: `ssh -o BatchMode=yes root@187.77.215.240 "tail -20 /var/log/vps-monitor.log"`

## Deploy Workflow
After code changes:
```bash
# 1. Publish locally (from repo root)
dotnet publish peeposredemption.API/peeposredemption.API.csproj -c Release -o /tmp/peepos-publish --nologo

# 2. Upload to VPS
scp -r /c/Users/pgiovanni/AppData/Local/Temp/peepos-publish/* root@187.77.215.240:/var/www/peeposredemption/

# 3. Fix permissions + restart
ssh -o BatchMode=yes root@187.77.215.240 "chown -R www-data:www-data /var/www/peeposredemption && systemctl restart peeposredemption"

# If new migrations exist — generate SQL and run on VPS:
dotnet ef migrations script --idempotent --project peeposredemption.Infrastructure --startup-project peeposredemption.API -o /tmp/migrations.sql
scp /tmp/migrations.sql root@187.77.215.240:/tmp/migrations.sql
ssh -o BatchMode=yes root@187.77.215.240 "sudo -u postgres psql -d peeposredemption -f /tmp/migrations.sql"
```

## Payment Gateway
- **Stripe** — chosen for M1 (storage upgrades) and M2 (Orb packs)
- NuGet: `Stripe.net`
- 2.9% + $0.30 per transaction, no monthly fee

## Business Model — Incentives

### Marketers
- Referral tracking: each marketer gets a unique referral link/code tied to their account
- Earn a commission % every time a user they referred makes a purchase (Orbs, storage upgrade, etc.)
- Payout via Stripe Connect or manual

### Artists
- Submit digital art (profile frames, effects, badges, emojis) for use in the platform
- Get paid per contribution accepted — flat fee or revenue share on items using their art
- Art review/approval workflow needed before publishing

## Infrastructure Cost Estimate (all planned features)
- **Total additional cost: ~$0/mo at small scale**
- Voice/Video: coturn on existing VPS (TURN relay) — $0 extra
- File uploads/emojis: Cloudflare R2 — free tier (10GB, 1M ops/mo), then $0.015/GB
- GIF embedding: Tenor API — free
- Twemoji: open source CDN — free
- All other features: pure code, no external deps
- Caveat: video TURN relay is VPS bandwidth — fine for small user counts; may need VPS upsize at heavy concurrency

## Backlog

### Bug Fixes
| # | Bug | Status |
|---|-----|--------|
| B1 | Messages not displaying in order (oldest→newest) | ✅ Fixed — awaiting deploy |
| B2 | Message timestamps showing UTC instead of user local time (channel + DM history) | ✅ Fixed — awaiting deploy |
| B3 | Admin notification email not sending (fire-and-forget racing scope disposal of transient IResend) | ✅ Fixed — deployed |

### Features

### F — Free Features (all users)
| # | Feature | Status |
|---|---------|--------|
| F1 | Voice channels (WebRTC — SignalR signaling, coturn TURN) | Not started |
| F2 | Video channels (WebRTC, same infrastructure as voice) | Not started |
| F3 | Custom server emojis + file uploads (Cloudflare R2) | Not started |
| F4 | GIF embedding (Tenor API) | Not started |
| F5 | Twemoji for standard unicode emojis | Not started |
| F6 | Message edit | Not started |
| F7 | User profiles (avatar, bio, display name) | Not started |
| F8 | Unfriend / real-time friend request notifications | Not started |
| F9 | Private channels (role-based visibility) | Not started |
| F10 | Mobile responsive design | Not started |
| F11 | Confirm password field on registration | Not started |

### I — Internal Monetization (pays developers/servers)
| # | Feature | Status |
|---|---------|--------|
| I1 | Stripe integration | Not started |
| I2 | Orbs — currency earnable via mini-game or purchased via Stripe | Not started |
| I3 | Extra storage for custom server emojis (paid upgrade via Stripe) | Not started |

### E — External Earnings (pays users for contributions)
| # | Feature | Status |
|---|---------|--------|
| E1 | Artist submission system — submit frames, effects, badges, emojis; paid per accepted contribution; needs review/approval workflow | Not started |
| E2 | Marketer referral system — unique referral link per user; earn commission % on every purchase made by referred users | Not started |

## Code Fixes Made This Deploy Session
- `RegisterCommand.cs` / `ResendConfirmationCommand.cs`: inject `IConfiguration`, use `AppBaseUrl` config key instead of hardcoded `https://localhost:443`
- `EmailService.cs`: From address and admin email now read from `Email:From` / `Email:AdminEmail` config keys
- `appsettings.json`: Removed broken Kestrel HTTPS section (had invalid JSON comments, caused crash on VPS)

## Pending Small Features (not yet built)
- Add confirm password field to registration form (validation only, no DB change needed)

## Preferences
- No "Co-Authored-By: Claude" tags in git commits

## Recent Fixes (not yet noted elsewhere)
- Message ordering fixed: query handler now `.OrderBy(m => m.SentAt)` after fetch (repo fetches DESC for pagination, handler reverses for display)
- Message timestamps: channel + DM history now use `data-time` ISO UTC string converted to local time via JS on load (consistent with real-time messages)
- Admin notification fire-and-forget replaced with `await` — was racing against scope disposal of transient `IResend`

## Known Issues / Gaps
- DM message history: loads on page open (first 50 msgs via GetConversationAsync). Page model calls repo directly, GetConversationQuery exists but unused — minor inconsistency, not a bug.
- Friend requests: no real-time notification (must refresh to see), no unfriend
- No message edit, no user profiles
- Private channels (planned)
- Invite system needs review (had debugger breakpoints)


## Key File Paths
- Hub: `peeposredemption.API/Hubs/ChatHub.cs`
- Pages: `peeposredemption.API/Pages/App/`
- Pages/Auth: `peeposredemption.API/Pages/Auth/`
- Program.cs: `peeposredemption.API/Program.cs`
- Features: `peeposredemption.Application/Features/`
- DB Context: `peeposredemption.Infrastructure/Persistence/AppDbContext.cs`
- UoW: `peeposredemption.Infrastructure/UnitOfWork.cs`
- Email services: `peeposredemption.Application/Services/`

## Deploy Command Reference
```bash
# Publish locally
dotnet publish peeposredemption.API/peeposredemption.API.csproj -c Release -o /tmp/peepos-publish --nologo -q

# Upload to VPS
scp -r /c/Users/pgiovanni/AppData/Local/Temp/peepos-publish/* root@187.77.215.240:/var/www/peeposredemption/

# Fix permissions + restart
ssh -o BatchMode=yes root@187.77.215.240 "chown -R www-data:www-data /var/www/peeposredemption && systemctl restart peeposredemption"

# Run migrations on VPS (generate SQL script, upload, run as postgres)
dotnet ef migrations script --idempotent --project peeposredemption.Infrastructure --startup-project peeposredemption.API -o /tmp/migrations.sql
scp /tmp/migrations.sql root@187.77.215.240:/tmp/migrations.sql
ssh -o BatchMode=yes root@187.77.215.240 "sudo -u postgres psql -d peeposredemption -f /tmp/migrations.sql"
```
