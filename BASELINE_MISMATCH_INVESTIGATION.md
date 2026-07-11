# Baseline Mismatch Investigation

## Scope and safety

This was a strictly offline inspection of files already on disk. The baseline tool, WPF application, SSH client, PostgreSQL utilities, and database services were not run. No local or remote database connection was attempted.

## 1. Configuration used by `AccountingBaselineReport`

`tools/AccountingBaselineReport/Program.cs` calculates the repository root from `AppContext.BaseDirectory`, sets that root as its configuration base path, and loads configuration in this precedence order (later sources override earlier ones):

1. Repository-root `appsettings.json` (required).
2. Repository-root `appsettings.Local.json` (optional).
3. Environment variables via `AddEnvironmentVariables()`.

`AddInfrastructure` then reads `ConnectionStrings:DefaultConnection` and passes it to Npgsql. Before resolving the DbContext, the tool calls `SshTunnelService.StartIfConfigured(configuration)`.

### Targets found on disk (credentials omitted)

| Source | Host | Port | Database | Meaning |
|---|---|---:|---|---|
| Repository-root `appsettings.json` | `localhost` | `5432` | `erp_pro` | Local/default PostgreSQL target. |
| Repository-root `appsettings.Local.json` as it exists now | `localhost` | `5433` | `erp_pro` | Local end of the configured SSH tunnel. |
| Current `SshTunnel` section | SSH host `65.21.136.217:2727`; forwarded DB endpoint `localhost:5432` on that VPS | local forward `5433` | `erp_pro` through the connection string | Production VPS route. |
| Tool build output `bin/Debug/net9.0/appsettings.json` | `localhost` | `5432` | `erp_pro` | Not selected by the current `Program.cs`, because its configuration base path is explicitly the repository root. |

There is no `appsettings.Production.json`, `appsettings.Development.json`, or `launchSettings.json` in the tool project. The tool does not load environment-specific JSON files. No launch profile override exists for this tool.

The code does not name a special environment variable directly; `AddEnvironmentVariables()` permits standard configuration-key overrides, including `ConnectionStrings__DefaultConnection` and keys under `SshTunnel__...`. No artifact records the environment-variable values of the process that generated the baseline, so any such override at that historical moment is **not determinable from available static data**.

### Historical limitation

The baseline artifact was written at `2026-07-11T12:39:47Z`. The current `appsettings.Local.json` has a later last-write timestamp, `2026-07-11T13:24:35Z`, and is git-ignored, so Git contains no historical version from the moment of capture. It existed before the run, but its exact contents at `12:39Z` cannot be reconstructed from the available files. Therefore the exact effective connection string used at capture time cannot be proven statically.

## 2. Comparison with the known production target

The known production target recorded by the Phase A artifacts is database `erp_pro` on VPS `65.21.136.217`, with PostgreSQL reached on the VPS at `localhost:5432` through the local SSH forward.

- **Current repository configuration:** **SAME target** — `localhost:5433/erp_pro` is forwarded to `65.21.136.217` → `localhost:5432`.
- **Configuration effective at the baseline's `12:39Z` capture time:** **UNDETERMINED from static inspection**, because the overriding local file was modified after capture and historical process environment values were not recorded.
- If `appsettings.Local.json` was absent/disabled or did not override the connection at that moment, the fallback would have been the **DIFFERENT local target** `localhost:5432/erp_pro`. Static files do not prove that this fallback was actually selected.

## 3. Why a wrong target could have been selected

From code alone, the wrong/local target would be selected if the optional root `appsettings.Local.json` was absent, unreadable, disabled as an override, or itself pointed locally, because the required root `appsettings.json` defaults to `localhost:5432/erp_pro`. An environment variable could also override either target because environment variables are loaded last.

Which of those conditions, if any, occurred during the historical run is **not determinable from available data**. Assigning one as the cause would be a guess.

## 4. Production-target branch

The configuration currently on disk points to production through the SSH tunnel. Per the task rule, no query or re-verification was attempted. Whether the tool also pointed there at the earlier artifact-generation time requires historical runtime evidence or a later live review; that live-dependent step is **requires live connection, deferred**.

## Conclusion

**Undetermined from static inspection.** The current configuration points to production, but it was modified after the baseline artifact was generated, and the run's environment-variable values were not captured. Static evidence therefore cannot conclusively label the captured baseline as either production or the local fallback. No code, configuration, or existing artifact was changed.
