# Project: Jellyfin.Plugin.UserRating

## Rules

- Always pull with rebase (`git pull --rebase`) before starting work to integrate remote changes
- Always ask before committing and pushing changes
- Always bump the version number when making changes:
  - During a development session, use the fourth octet for beta builds: 1.10.0.1, 1.10.0.2, etc. (increment for each change within the session)
  - Stable releases use fourth octet 0: 1.10.0.0, 1.10.1.0, etc.
  - After the session is complete and the version is stable, decide the final version:
    - Patch bump for bug fixes and minor changes (e.g., 1.10.0.0 → 1.10.1.0)
    - Minor bump for new features (e.g., 1.10.0.0 → 1.11.0.0)
  - Never overwrite or move existing git tags — always create a new tag for the new version
  - A git tag must be created for every version (including betas) — Jellyfin repository requires a tag to fetch the plugin version
  - In manifest.json, always add new versions as new entries at the top of the versions array — never replace or remove previous stable versions. Only the latest beta entry may be replaced/superseded
- Changelog: only include bug fixes that existed in a previous stable release. Do not include bugs found and fixed during the same development session — keep CHANGELOG.md clean from noise
- After a stable release is ready: remove transitional/beta tags from the repo (both local and remote) and remove beta entries from manifest.json — keep only stable versions

## Build & Test

- No `dotnet` CLI available in this environment — cannot build locally
- Verify C# syntax by reading files; cannot compile
- Use `dev/` directory inside the project for temporary files (test pages, scratch work, etc.)

## Project Structure

- `Api/RatingsController.cs` — REST API endpoints for ratings
- `Api/HealthController.cs` — REST endpoints for health report, heal, and clear stale ratings
- `Data/RatingRepository.cs` — JSON-file-based rating storage (fail-safe per-entry deserialization)
- `Data/BackupService.cs` — Scheduled/manual backup with rotation
- `Models/UserRating.cs` — Data models (UserRating, RatingStats, RatedItemSummary, HealthReport, StaleItem)
- `Configuration/ratings.js` — Client-side JS injected into Jellyfin UI (detail page rating widget + home tab dashboard)
- `Configuration/configPage.html` — Admin config page (includes Database Health section)
- `Plugin.cs` — Plugin entry point, injects ratings.js into index.html
- `PluginServiceRegistrator.cs` — DI registration
- `Services/RatingResolver.cs` — Self-healing lookup: resolves ratings by ItemId, falls back to provider IDs
- `Services/HealthCheckService.cs` — DB consistency scanner (ok/healed/stale categorization, healing, stale cleanup)
- `Services/PlexImportService.cs` — Plex rating import with provider ID extraction
- `ScheduledTasks/RatingsHealthTask.cs` — Scheduled task for automatic DB health checks
- `ScheduledTasks/RatingsBackupTask.cs` — Scheduled task for automatic DB backups
- `manifest.json` — Plugin repository manifest (version, changelog, checksum)

## Key Patterns

- `ratings.js` is served as an embedded resource and injected into Jellyfin's `index.html`
- Card layout uses Jellyfin's native CSS classes: `backdropCard`, `cardPadder-backdrop`, `vertical-wrap`
- API calls use `ApiClient.getUrl()` and `ApiClient.accessToken()` for auth
- Version must be updated in both `Jellyfin.Plugin.UserRatings.csproj` (AssemblyVersion/FileVersion) and `manifest.json`
- Provider ID resolution: match by ItemId first, then by ProviderIds (Imdb/Tmdb/Tvdb/etc.). On provider ID match, the ItemId is healed (re-keyed) in the DB automatically
- Health check task runs as a Jellyfin scheduled task (category: "User Ratings"). Can also be triggered from the admin config page
- All repository mutations are lock-protected (`lock _lock`)
- `LoadRatings` uses per-entry deserialization — one malformed entry is logged and skipped, not fatal. Backs up corrupted files before starting empty
- `SaveRatings` is guarded by `_loadFailed` flag — won't overwrite data file if load failed and DB is empty