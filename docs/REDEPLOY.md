# Redeploying torvex.app

**DONE 2026-08-08 21:52.** Prod now runs `9146d28`-era code built from the
current tree; it had been serving a 2026-05-13 publish of `ca79332`.

What actually happened, in order:

1. **ProxyCheck key rotated.** New key lives ONLY in
   `appsettings.Production.json` (root:www-data 640). The deployed
   `appsettings.json` now ships `ProxyCheck.ApiKey = ""` — the burned key is
   gone from the box. Verified absent from the artifact before the swap.
2. **Backup**: nightly set `20260808_214754` plus a dedicated
   `/root/pre-migration-peeposredemption.dump` (3.1 MB).
3. **Migrations reconciled** in one transaction. 47 -> 49 applied:
   * `AddDiscordLink` recorded, NOT run — `discord_links` already existed with
     824 rows and running it would have aborted the whole update.
   * `AddGuildConfig`: three of its four operations targeted columns that no
     longer exist, so only `guild_configs` + its unique index were created,
     then the migration was recorded. **This was the actual "guild-config is
     broken" bug — a missing table, never a code fault.**
4. **Published** `dotnet publish -c Release` (net10.0, framework-dependent),
   staged to `/var/www/peeposredemption.new`, `Production.json` carried across
   because `396869c` correctly keeps environment configs out of the artifact.
5. **Swapped and restarted.** Old build kept at `/var/www/peeposredemption.old`
   (39 MB) — that is the rollback.

Verified after: `/` `/Wiki` `/Marketplace` all 200 and rendering titles,
`/wiki/monster/hobgoblin` and `/wiki/item/thunder-staff` 200, zero error lines
in the journal, `guild_configs` present and queryable.

**Remaining, NOT done:** the bot API key is still inline in a mode-644
`peeposredemption.service` (`Environment=Bot__ApiKey=...`), readable by both
unprivileged accounts and by `systemctl show` without root. Move it to an
`EnvironmentFile=` at 600 and rotate it.

Once `/var/www/peeposredemption.old` is no longer wanted, delete it.

---

## Original investigation (kept for the reasoning)


**Investigated 2026-08-08. Nothing here has been executed.**

Prod is a compiled publish from **2026-05-13 21:17**, built from `ca79332`
(2026-05-10). Ten commits have landed since and none are deployed. The site is
up and serving 200, so this is not an outage — it is seven weeks of drift.

## Why it matters now

| Commit | Date | Why it matters undeployed |
|---|---|---|
| `380c3fe` | 06-22 | **security: remove leaked ProxyCheck API key.** The key is STILL LIVE in `/var/www/peeposredemption/appsettings.json` |
| `396869c` | 06-22 | keep environment configs out of the publish artifact — has never run, which is *why* a config with a key sits in the deploy dir |
| `fa6a833` | 06-22 | strip debug symbols from Release publishes |
| `adad9ed` | 06-19 | public marketplace listings page |
| `e88b2df` | 06-19 | public wiki pages for monsters + item index |
| `f8c6e37` | 06-19 | API endpoints for wiki, leaderboard, pvp, audit |
| `6871610` | 06-19 | game command handler updates |
| `342e834` | 06-19 | crafting recipes seeder |
| `b270d61` | 06-19 | blight status effect + element multiplier tuning |
| `60c7131` | 06-19 | coin & item transaction audit ledger |

## The blocker: two migrations that CANNOT be run as written

Repo has 49 migrations. The database has 47. The two missing ones are **both
already applied to the schema by hand** and simply never recorded, so
`dotnet ef database update` fails on the first one and never reaches the second.

### `20260504214919_AddDiscordLink` — 100% already applied

Creates table `discord_links`. That table **exists and holds 824 rows**.
Running it errors with "relation already exists" and aborts the whole update.

**Fix: record it, don't run it.**

```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260504214919_AddDiscordLink', '<match the ProductVersion of the other rows>');
```

### `20260510224859_AddGuildConfig` — 3 of 4 operations already applied

| Operation | Live schema | Result if run |
|---|---|---|
| `DropColumn enchant_bonus` on `player_inventory_items` | column absent | **fails** |
| `DropColumn enchant_element` on `player_inventory_items` | column absent | **fails** |
| `RenameColumn enchant_name -> enchants_json` | `enchant_name` absent, `enchants_json` already present | **fails** |
| `CreateTable guild_configs` + index | **absent** | this is the only real work |

So the enchant refactor was applied by hand at some point and only
`guild_configs` is genuinely missing. **This is the root cause of the
long-standing "guild-config system is broken" note** — it was never a code bug,
the table simply does not exist.

**Fix: create the one missing object exactly as the migration defines it, then
record the migration.** Do not edit the migration file — rewriting applied
history to match drifted prod is how the next person gets misled.

## Prerequisites (all present)

- dotnet SDK **10.0.103** on the VPS, **10.0.104** locally — can build in either place
- Service: `peeposredemption.service`, `User=www-data`,
  `ExecStart=/usr/bin/dotnet /var/www/peeposredemption/peeposredemption.API.dll`,
  `ASPNETCORE_ENVIRONMENT=Production`
- Postgres 17, database `peeposredemption`, nightly dump in `/var/backups/torvex`

## Order of operations

1. **Rotate the ProxyCheck key** at proxycheck.io. It is burned regardless of
   this deploy. Put the new one in `appsettings.Production.json` only.
2. **Back up**: `systemctl start torvex-backup` (covers the Postgres dump and,
   since 2026-08-08, `appsettings.Production.json` and the data-protection keys).
3. **Reconcile migration history** — the two INSERTs / one CREATE TABLE above,
   inside a transaction, verified with `dotnet ef migrations list` showing zero
   pending afterwards.
4. **Publish** `dotnet publish -c Release`, to a *staging directory*, not over
   the live one.
5. **Preserve** `appsettings.Production.json` and `peeposredemption-keys/` —
   `396869c` changes what lands in the artifact, so confirm they survive.
6. **Swap + restart**, then verify `torvex.app` returns 200 and the new
   marketplace/wiki routes actually resolve.
7. **Roll back** = restore the previous publish directory. Keep it until verified.

## Two other things found while investigating

**The bot API key is in a world-readable file.**
`/etc/systemd/system/peeposredemption.service` is mode 644 and contains
`Environment=Bot__ApiKey=0f4725ee...` inline. Both non-root accounts on the box
(`debian`, `dbreview`) can read it, and `systemctl show peeposredemption -p
Environment` exposes it without root at all. Move it to an
`EnvironmentFile=` at mode 600, or into `appsettings.Production.json`. Worth
rotating too, since `dbreview` is the shared SQL-review account.

**The data-protection keys in `/var/www/peeposredemption-keys/` are now backed
up** (added 2026-08-08). Losing them invalidates every issued cookie and token,
so any redeploy must keep that directory intact.

## Risk

Low for the app (a publish swap with a kept rollback directory), **medium for
the database** — step 3 writes to `__EFMigrationsHistory` and creates a table on
the live game database. Do it with a fresh dump in hand and inside a
transaction. It is two INSERTs and one CREATE TABLE; it is not a data migration.
