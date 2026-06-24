# Project: Jellyfin.Plugin.UserRating

## Rules

- Always pull with rebase (`git pull --rebase`) before starting work to integrate remote changes. This must be done before EVERY commit, not just at session start — the CI/CD pipeline builds and pushes changes (checksums, version bumps) to the repo between releases, so local state will diverge. Pulling rebase before each commit avoids having to resolve conflicts every time you push

## Referenced Instructions

Documents under `llm_wiki/` contain detailed procedures and architecture reference for the project. **Only read these when they are needed** — do not load them proactively.

### Procedures

| When | Read |
|---|---|
| User asks to release or push a new version | [`llm_wiki/release-process.md`](llm_wiki/release-process.md) |

### Architecture Reference

| When working on | Read |
|---|---|
| Tab/navigation/page-level DOM, view caching, `#ratingsTab` or `#indexPage` | [`llm_wiki/jellyfin-spa-internals.md`](llm_wiki/jellyfin-spa-internals.md) |
| `Configuration/ratings.js` — injection logic, event handlers, state variables | [`llm_wiki/ratings-js-architecture.md`](llm_wiki/ratings-js-architecture.md) |
| Client-side navigation/tab/injection changes — anti-patterns and gotchas | [`llm_wiki/client-side-pitfalls.md`](llm_wiki/client-side-pitfalls.md) |
| `Data/` or `Services/` — rating storage, provider resolution, health checks, backups | [`llm_wiki/rating-database.md`](llm_wiki/rating-database.md) |
| `Services/PlexImportService.cs`, token encryption, progress tracking, scheduled sync | [`llm_wiki/plex-import.md`](llm_wiki/plex-import.md) |

## Build & Test

- No `dotnet` CLI available in this environment — cannot build locally
- Verify C# syntax by reading files; cannot compile
- Use `dev/` directory inside the project for temporary files (test pages, scratch work, etc.)

## Project Structure

- `Api/RatingsController.cs` — REST API endpoints for ratings
- `Api/HealthController.cs` — REST endpoints for health report, heal, and clear stale ratings
- `Api/PlexImportController.cs` — REST endpoint for Plex rating import
- `Data/RatingRepository.cs` — JSON-file-based rating storage (fail-safe per-entry deserialization)
- `Data/BackupService.cs` — Scheduled/manual backup with rotation
- `Models/UserRating.cs` — Data models (UserRating, RatingStats, RatedItemSummary, HealthReport, StaleItem)
- `Configuration/ratings.js` — Client-side JS injected into Jellyfin UI (detail page rating widget + home tab dashboard)
- `Configuration/configPage.html` — Admin config page (includes Database Health section)
- `Configuration/PluginConfiguration.cs` — Plugin configuration model
- `Middleware/ScriptInjectionMiddleware.cs` — ASP.NET middleware that injects `<script>` tag into index.html in-memory
- `Middleware/ScriptInjectionStartupFilter.cs` — IStartupFilter that registers ScriptInjectionMiddleware
- `Plugin.cs` — Plugin entry point
- `PluginServiceRegistrator.cs` — DI registration
- `Services/RatingResolver.cs` — Self-healing lookup: resolves ratings by ItemId, falls back to provider IDs
- `Services/HealthCheckService.cs` — DB consistency scanner (ok/healed/stale categorization, healing, stale cleanup)
- `Services/PlexImportService.cs` — Plex rating import with provider ID extraction
- `Services/PlexModels.cs` — Data models for Plex import (PlexUser, PlexRating, etc.)
- `Services/ProgressTracker.cs` — Tracks import progress for progress bar reporting
- `Services/TokenEncryption.cs` — Encrypts/decrypts stored Plex tokens
- `ScheduledTasks/RatingsHealthTask.cs` — Scheduled task for automatic DB health checks
- `ScheduledTasks/RatingsBackupTask.cs` — Scheduled task for automatic DB backups
- `ScheduledTasks/PlexSyncScheduledTask.cs` — Scheduled task for automatic Plex sync
- `manifest.json` — Plugin repository manifest (version, changelog, checksum)

## Key Patterns

- `ratings.js` is served as an embedded resource and injected into Jellyfin's `index.html` via `ScriptInjectionMiddleware`
- Card layout uses Jellyfin's native CSS classes: `backdropCard`, `cardPadder-backdrop`, `vertical-wrap`
- API calls use `ApiClient.getUrl()` and `ApiClient.accessToken()` for auth
- Version must be updated in both `Jellyfin.Plugin.UserRatings.csproj` (AssemblyVersion/FileVersion) and `manifest.json`
- Provider ID resolution: match by ItemId first, then by ProviderIds (Imdb/Tmdb/Tvdb/etc.). On provider ID match, the ItemId is healed (re-keyed) in the DB automatically
- Health check task runs as a Jellyfin scheduled task (category: "User Ratings"). Can also be triggered from the admin config page
- All repository mutations are lock-protected (`lock _lock`)
- `LoadRatings` uses per-entry deserialization — one malformed entry is logged and skipped, not fatal. Backs up corrupted files before starting empty
- `SaveRatings` is guarded by `_loadFailed` flag — won't overwrite data file if load failed and DB is empty