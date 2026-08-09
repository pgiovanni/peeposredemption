# Redeploying torvex.app — what it actually involves

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
