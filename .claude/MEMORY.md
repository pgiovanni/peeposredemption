# PeePo's Redemption - Project Memory

## DB Connection
Host=localhost;Port=5432;Database=peeposredemption;Username=postgres;Password=<ask user>

**Only ask for the DB password if connecting via psql directly. `dotnet ef database update` reads from appsettings.json and doesn't need it.**

## What It Is
A Discord-like communication platform + text-based RPG game system (ASP.NET Core, .NET 10). Features: servers, channels, real-time chat, invites, DMs, orb economy, badges, and a planned RuneScape/FF-inspired game layer.

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
- MediatR 12.4.1 (free/MIT), FluentValidation, BCrypt
- Stripe.net (v50.4.1) — payments
- AWSSDK.S3 (v4.0.18.8) — file storage (Cloudflare R2 planned)
- Email: Resend (prod) / MailHog localhost:1025 (dev) — `IEmailService` interface swapped via `builder.Environment.IsDevelopment()`
- MailHog binary at `%USERPROFILE%/mailhog.exe`, web UI at http://localhost:8025

## Domain Entities
**Chat/Social:** User, Server, Channel, Message, ServerMember (composite PK), ServerInvite, DirectMessage, FriendRequest, BannedMember, ModerationLog, Notification, RefreshToken
**Economy:** OrbTransaction, OrbPurchase, OrbGift, StorageUpgradePurchase
**Gamification:** BadgeDefinition, UserBadge, UserActivityStats, UserLoginStreak
**Other:** ServerEmoji, ReferralCode, ParentalLink

## Features Implemented

### Chat & Social
- Auth: register, login, logout (JWT cookie deleted), email confirmation, resend confirmation
- Servers: create (auto-creates "general" channel), invite codes, join via invite
- Channels: list, text/voice/announcement types
- Messages: send, persist, paginated history, soft delete
- DMs: real-time broadcast + DB persistence
- SignalR: group-based channel messaging, user-based DMs, typing indicators
- Friends: send/accept/reject requests, view friends list
- Moderation: kick/ban, role-based permissions, audit log, link scanner

### Orbs Economy (fully built)
- **Daily login rewards**: 10 orbs/day base, +50 bonus at 7-day streak, +200 at 30-day streak
- **Message rewards**: 1 orb per 10 messages, 50 orbs/day cap (fire-and-forget in ChatHub)
- **P2P gifting**: send orbs to users with optional message, SignalR broadcast
- **Stripe purchases**: checkout session creation for orb packs
- **Transaction history**: full audit trail with 14 transaction types
- **OrbTransactionType enum**: DailyLogin(0), MessageReward(1), StripePurchase(2), GiftSent(3), GiftReceived(4), CrateOpen(5), TradeSpent(6), TradeReceived(7), StockBuy(8), StockSell(9), AdminGrant(10), CraftingSpent(11), MarketplaceSale(12), MarketplacePurchase(13)
- Types 5-9, 11-13 are defined but **not yet implemented** — reserved for game system

### Badges/Achievements (fully built)
- 13 badges across Activity, Social, Economy categories
- Auto-award when stat thresholds met (rule-based: stat key + threshold)
- UserActivityStats tracks: TotalMessages, LongestStreak, TotalOrbsGifted, ServersJoined, PeakOrbBalance
- API endpoints: `/api/badges/progress`, `/api/users/{userId}/badges`

### Orbs API Endpoints
- `POST /api/orbs/daily-claim` — claim daily login rewards
- `GET /api/orbs/balance` — balance + streak info
- `GET /api/orbs/transactions` — transaction history
- `POST /api/orbs/purchase` — Stripe checkout session
- `POST /api/orbs/gift` — send orbs (REST + SignalR)

## Moderation System
- `ServerRole` enum (Member=0, Moderator=1, Owner=2) on `ServerMember.Role`
- Owner auto-assigned on server create
- Kick/Ban (Owner only), Delete message/channel (Moderator+)
- `BannedMember` table enforced on invite join
- `ModerationLog` table — all actions logged with moderator + target
- Link scanner (`LinkScannerService`) blocks IP grabbers — merges hardcoded `FallbackDomains` + remote GitHub list. Refreshes every 24h.
- `Message.IsDeleted` soft delete, shown as "[message deleted]"
- Server Settings page at `/App/ServerSettings?serverId=X` — member list + audit log

## Game System Vision (not yet built)
Text-based RPG inspired by RuneScape + Final Fantasy + Discord integration:
- **Characters**: stats (STR, DEF, MAG, etc.), levels, XP, classes/jobs
- **Combat**: turn-based or text-based fighting, monsters/NPCs, loot drops
- **Items/Inventory**: weapons, armor, consumables, equipped slots
- **Skills/Abilities**: skill trees, unlockable abilities
- **Trading**: P2P item + orb trades with confirmation (enum types 6/7 ready)
- **Crafting**: combine materials into gear/items (enum type 11 ready)
- **Quests**: NPC quest givers, objectives, rewards
- **Marketplace**: auction house / Grand Exchange style (enum types 12/13 ready)
- **Crates/Loot boxes**: gacha mechanic (enum type 5 ready)
- **Stock market**: economic mini-game (enum types 8/9 ready)
- **Areas/Zones**: explorable text-based locations, encounters

Orbs economy + badges + transaction infrastructure provide the foundation.

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
- **Domain**: `torvex.app`
- **App status**: Running and returning 302 → /Auth/Login through nginx

## Production Config
- `appsettings.Production.json` exists in repo and deployed to VPS
- Kestrel: HTTP only on port 5000 (nginx handles TLS)
- `AppBaseUrl`: `https://torvex.app`
- `Email:From`: `noreply@torvex.app`
- `Email:AdminEmail`: `pgiovanni1234@gmail.com`
- JWT Issuer/Audience: `https://torvex.app`
- **DO NOT store secrets in memory** — ask user if needed

## Production Status — FULLY LIVE at https://torvex.app
- DNS: `torvex.app` + `www.torvex.app` A records → `187.77.215.240`
- SSL: Let's Encrypt cert covers both, auto-renewal (expires 2026-06-06)
- Hostinger firewall: Accept TCP 80, 443, 22 + Drop Any
- Resend: `torvex.app` domain verified, all DNS records propagated (DKIM, SPF, DMARC, MX)
- Emails send from `noreply@torvex.app` via Resend
- DataProtection keys persisted to `/var/www/peeposredemption-keys`
- Registration + email confirmation flow fully tested and working
- Admin signup notification email fires on every new registration

## VPS Monitoring
- Script: `/usr/local/bin/vps-monitor.sh` — runs every 5 min via cron
- Log: `/var/log/vps-monitor.log` — CPU, MEM, DISK, app status, nginx status
- To check: `ssh -o BatchMode=yes root@187.77.215.240 "tail -20 /var/log/vps-monitor.log"`

## Payment Gateway
- **Stripe** — orb purchases + storage upgrades
- NuGet: `Stripe.net` v50.4.1
- 2.9% + $0.30 per transaction, no monthly fee

## Business Model — Incentives

### Marketers
- Referral tracking: unique referral link/code per marketer
- Earn commission % on every purchase by referred users
- Payout via Stripe Connect or manual

### Artists
- Submit digital art (profile frames, effects, badges, emojis)
- Paid per accepted contribution — flat fee or revenue share
- Review/approval workflow needed

## Infrastructure Cost Estimate
- Voice/Video: coturn on existing VPS — $0 extra
- File uploads/emojis: Cloudflare R2 — free tier (10GB, 1M ops/mo), then $0.015/GB
- GIF embedding: Tenor API — free
- Twemoji: open source CDN — free
- Caveat: video TURN relay is VPS bandwidth — may need VPS upsize at heavy concurrency

## Backlog

### Bug Fixes
| # | Bug | Status |
|---|-----|--------|
| B1 | Messages not displaying in order (oldest→newest) | Fixed + deployed |
| B2 | Message timestamps showing UTC instead of user local time | Fixed + deployed |
| B3 | Admin notification email racing scope disposal | Fixed + deployed |

### F — Free Features
| # | Feature | Status |
|---|---------|--------|
| F1 | Voice channels (WebRTC — SignalR signaling, coturn TURN) | Not started |
| F2 | Video channels (WebRTC, same infra as voice) | Not started |
| F3 | Custom server emojis + file uploads (Cloudflare R2) | Not started |
| F4 | GIF embedding (Tenor API) | Not started |
| F5 | Twemoji for standard unicode emojis | Not started |
| F6 | Message edit | Not started |
| F7 | User profiles (avatar, bio, display name) | Not started |
| F8 | Unfriend / real-time friend request notifications | Not started |
| F9 | Private channels (role-based visibility) | Not started |
| F10 | Mobile responsive design | Not started |
| F11 | Confirm password field on registration | Not started |

### I — Internal Monetization
| # | Feature | Status |
|---|---------|--------|
| I1 | Stripe integration | Done (Orb purchases) |
| I2 | Orbs economy (daily login, message rewards, gifting, Stripe) | Done |
| I3 | Badges/Achievements (13 badges, auto-award, progress tracking) | Done |
| I4 | Extra storage for custom server emojis (paid upgrade) | Not started |

### G — Game System (text-based RPG)
| # | Feature | Status |
|---|---------|--------|
| G1 | Character system (stats, levels, XP, classes) | Not started |
| G2 | Combat system (turn-based, monsters, loot) | Not started |
| G3 | Items/Inventory (weapons, armor, consumables, equip slots) | Not started |
| G4 | Skills/Abilities (skill trees, unlockables) | Not started |
| G5 | P2P Trading (item + orb trades with confirmation) | Not started |
| G6 | Crafting (combine materials into gear) | Not started |
| G7 | Quests (NPC quest givers, objectives, rewards) | Not started |
| G8 | Marketplace / Grand Exchange | Not started |
| G9 | Crates / Loot boxes | Not started |
| G10 | Stock market mini-game | Not started |
| G11 | Areas/Zones (text-based exploration, encounters) | Not started |

### E — External Earnings
| # | Feature | Status |
|---|---------|--------|
| E1 | Artist submission system | Not started |
| E2 | Marketer referral system | Not started |

## Known Issues / Gaps
- DM history: page model calls repo directly, GetConversationQuery exists but unused (minor inconsistency)
- Friend requests: no real-time notification (must refresh), no unfriend
- No message edit, no user profiles
- Invite system needs review (had debugger breakpoints)

## Key File Paths
- Hub: `peeposredemption.API/Hubs/ChatHub.cs`
- Pages: `peeposredemption.API/Pages/App/`
- Pages/Auth: `peeposredemption.API/Pages/Auth/`
- Program.cs: `peeposredemption.API/Program.cs`
- Features: `peeposredemption.Application/Features/`
- Orbs Features: `peeposredemption.Application/Features/Orbs/`
- Badge Features: `peeposredemption.Application/Features/Badges/`
- DB Context: `peeposredemption.Infrastructure/Persistence/AppDbContext.cs`
- UoW: `peeposredemption.Infrastructure/UnitOfWork.cs`
- Email services: `peeposredemption.Application/Services/`
- Entities: `peeposredemption.Domain/Entities/`
- UI Pages: Wallet.cshtml, OrbShop.cshtml, Badges.cshtml (in Pages/App/)

## Deploy Command Reference
```bash
# Publish locally
dotnet publish peeposredemption.API/peeposredemption.API.csproj -c Release -o /tmp/peepos-publish --nologo -q

# Upload to VPS
scp -r /c/Users/pgiovanni/AppData/Local/Temp/peepos-publish/* root@187.77.215.240:/var/www/peeposredemption/

# Fix permissions + restart
ssh -o BatchMode=yes root@187.77.215.240 "chown -R www-data:www-data /var/www/peeposredemption && systemctl restart peeposredemption"

# Run migrations on VPS
dotnet ef migrations script --idempotent --project peeposredemption.Infrastructure --startup-project peeposredemption.API -o /tmp/migrations.sql
scp /tmp/migrations.sql root@187.77.215.240:/tmp/migrations.sql
ssh -o BatchMode=yes root@187.77.215.240 "sudo -u postgres psql -d peeposredemption -f /tmp/migrations.sql"
```

## Preferences
- No "Co-Authored-By: Claude" tags in git commits
