# Project: Jellyfin.Plugin.UserRating

## Rules

- Always pull with rebase (`git pull --rebase`) before starting work to integrate remote changes
- Always ask before committing and pushing changes
- Always bump the version number when making changes:
  - Bump the third octet (patch) for bug fixes and minor changes (e.g., 1.10.0 → 1.10.1)
  - Bump the second octet (minor) for new features (e.g., 1.10.0 → 1.11.0)
  - Never overwrite or move existing git tags — always create a new tag for the new version
- Changelog: only include bug fixes that existed in a previous stable release. Do not include bugs found and fixed during the same development session — keep CHANGELOG.md clean from noise

## Build & Test

- No `dotnet` CLI available in this environment — cannot build locally
- Verify C# syntax by reading files; cannot compile
- Use `dev/` directory inside the project for temporary files (test pages, scratch work, etc.)

## Project Structure

- `Api/RatingsController.cs` — REST API endpoints for ratings
- `Data/RatingRepository.cs` — JSON-file-based rating storage
- `Models/UserRating.cs` — Data models
- `Configuration/ratings.js` — Client-side JS injected into Jellyfin UI (detail page rating widget + home tab dashboard)
- `Configuration/configPage.html` — Admin config page
- `Plugin.cs` — Plugin entry point, injects ratings.js into index.html
- `PluginServiceRegistrator.cs` — DI registration
- `manifest.json` — Plugin repository manifest (version, changelog, checksum)

## Key Patterns

- `ratings.js` is served as an embedded resource and injected into Jellyfin's `index.html`
- Card layout uses Jellyfin's native CSS classes: `backdropCard`, `cardPadder-backdrop`, `vertical-wrap`
- API calls use `ApiClient.getUrl()` and `ApiClient.accessToken()` for auth
- Version must be updated in both `Jellyfin.Plugin.UserRatings.csproj` (AssemblyVersion/FileVersion) and `manifest.json`