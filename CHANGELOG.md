# Changelog

## v1.11.7.0

### Fixed

- **Plex Import: UserName Not Set** — Imported ratings showed "Unknown User" instead of the actual sync user's display name. `PlexImportService` now resolves the username via `IUserManager.GetUserById()` before creating rating entries.

---

## v1.11.6.0

### New Features

- **Global Exception Filter** — Unhandled API exceptions now return JSON error responses instead of crashing the server or returning HTML error pages.

### Changed

- **Modern C# Cleanup** — Converted to file-scoped namespaces, primary constructors, and record types across the codebase.
- **Typed Response DTOs** — Replaced anonymous types with typed records for all API responses.

### Architecture Decisions

| Decision | Rationale |
|---|---|
| `IAsyncExceptionFilter` instead of per-controller try/catch | Single path for all error responses — no duplicated error handling across 3+ controllers |
| Typed records over anonymous types | Serialization is predictable and testable; anonymous types serialize properties in declaration order with lowercase names (implicit) |

---

## v1.11.5.0

### Fixed

- **Series Query Performance** — Changed from `IsPlayed`-on-Series (required scanning all episodes, 40s) to episode-based watched lookup with SeriesId deduplication (<50ms).
- **Series Sort by Last Watched** — `lastPlayedDate` now uses the episode's actual play date instead of the always-null series-level `UserData.LastPlayedDate`. Sorting unrated series by Last Watched now works correctly.
- **Rating Oscillation** — `RatingResolver.ResolveRating` now guards against re-keying when the old ItemId is still a valid library item. Prevents ping-pong between duplicate entries with same ProviderIds.
- **Rated Items Appearing as Unrated** — Server-side `GetUnratedWatchedItems` endpoint uses provider-ID-aware `HasRating` check instead of fragile client-side ID filtering.
- **GUID Format Mismatch** — All plugin API responses now use `ToString("N")` for ItemIds, matching Jellyfin's native convention.

---

## v1.11.4.0

### Fixed

- **Back Button Returns to User Ratings Tab** — When navigating from the User Ratings tab to a details page and pressing back, the browser now returns to the User Ratings tab instead of always landing on Home or Favourites. Uses `history.replaceState` to annotate the current history entry with `userRatingsActive`, so the `hashchange` handler restores the correct tab. Switching to a native tab (Home/Favourites) clears the flag — last-clicked native tab wins on back. No refetch on re-activation; existing DOM content is restored.

---

## v1.11.3.0

### Fixed

- **Recently Rated / Oldest Rated Sort Broken** — `lastRatedTimestamp` was stored as an ISO date string from the API, causing `NaN` in the arithmetic sort comparisons — the "Recently Rated" and "Oldest Rated" sort options were silent no-ops. Now converted to epoch milliseconds at mapping time via `new Date(item.lastRated).getTime()`.

### Changed

- **Sort Direction Toggle** — Replaced paired sort options (e.g. `Rating: High to Low` / `Rating: Low to High`, `Recently Rated` / `Oldest Rated`) with a single sort-field dropdown plus an arrow direction-toggle button (▼/▲). Applies to all three sortable blocks: All Rated Items, Watched Movies — Not Yet Rated, and Watched Shows — Not Yet Rated. Direction state is per-section; only the All Rated Items block syncs to global so the type-filter handler can read it.

---

## v1.11.2.0

### Fixed

- **Backup Files Table Width** — Changed table from full-width (`width: 100%`) to auto-fit (`width: auto`) so it renders compactly instead of stretching across the entire config page.

---

## v1.11.1.0

Plugin config page CSS delivery fix, backup file management, and UI polish.

### Fixed

- **Plugin Config Page CSS Not Loading** — Jellyfin's plugin page loader extracts only the `div[data-role="page"]` subtree via `querySelector`. The `<style>` block was placed outside that div and was silently stripped. Moved `<style>` inside the page div so all custom CSS (inputs, buttons, tables, sections) is now applied.
- **Button Text Alignment** — All `is="emby-button"` elements now center text via `justify-content: center !important` (Heal Database, Save Settings, Import, etc.).
- **Input Styling** — Darker background (`#1a1a1a`), stronger borders (`rgba(255,255,255,0.2)`), consistent padding for text/password/number inputs and selects.
- **Backup Directory Path Input** — Widened to 600px max-width (was 420px).
- **Backup Files Table** — Added borders and row hover effect.
- **Upload Button Height** — Matched Test Connection button height.

### New Features

- **Backup File Management** — Download, restore, delete, and upload backup files from the admin config page.

### Changed

- **Import Button** — Renamed from "Import Ratings from Plex" to "Import".

---

## v1.10.1.0

### Fixed

- **Script Injection Log Spam** — Changed middleware script injection log from `Information` to `Debug` level to avoid flooding Jellyfin logs on every page load.

---

## v1.10.0.1

Middleware-based script injection — works in all Jellyfin deployment modes (root, non-root, Docker).

### Fixed

- **Script Injection in Docker Containers** — Replaced the file-write approach (broken when `index.html` is owned by `root:root` in Jellyfin 10.11 containers) with ASP.NET `ScriptInjectionMiddleware` that injects the `<script>` tag in-memory before `</body>`. No file permissions needed, no disk writes.
- **Content Encoding Error** — Removes `Accept-Encoding` header on index.html requests so the middleware receives uncompressed HTML instead of gzip/brotli. Prevents "Content Encoding Error" in browsers.

### Architecture Decisions

| Decision | Rationale |
|---|---|
| `IStartupFilter`-based middleware registration | Wraps the ASP.NET pipeline — our middleware runs outermost, intercepting responses after all other processing |
| Strip `Accept-Encoding` before `_next` | Prevents compression middleware from gzip-wrapping the response, which would make the HTML injection unreadable |

---

## v1.10.0

Provider ID resolution, self-healing ratings, database health checks, and automatic backups.

### New Features

- **Provider ID Resolution** — Ratings are now matched by ItemId first, then by provider IDs (IMDB, TMDB, TVDB, etc.). If an item's ID changes in Jellyfin, ratings are automatically re-linked via provider IDs.
- **Self-Healing Ratings** — When a rating's ItemId no longer matches a library item, the system searches by provider IDs and re-keys the rating to the correct item. Healing runs automatically on all read endpoints and via a scheduled task.
- **Database Health Check** — New admin section to check database consistency. Reports OK, Recoverable (can be healed), Updated (provider IDs backfilled), and Stale (no match by any ID) counts.
- **Heal & Clear Stale** — One-click "Heal Database" button to fix all recoverable ratings. "Clear Stale" button in the Danger Zone to remove orphaned ratings that match no library item.
- **Automatic Backups** — Scheduled backups of the ratings JSON file with configurable interval (default 24h), configurable retention (default 7 backups), and configurable backup directory. Manual "Create Backup Now" button also available.
- **Fail-Safe Loading** — If a single rating entry is malformed (e.g., corrupted GUID), it is logged and skipped instead of breaking the entire database. Corrupted files are automatically backed up before starting fresh.
- **Save Guard** — If loading fails and the in-memory database is empty, saves are blocked to prevent overwriting the file with no data.

### Architecture Decisions

| Decision | Rationale |
|---|---|
| Per-entry JSON deserialization | One malformed entry should not destroy all ratings — skip and log instead |
| `ProviderIds` dictionary on `UserRating` | Mirrors Jellyfin's `BaseItem.ProviderIds` — flexible, no schema changes for new providers |
| Linear scan for provider ID fallback | Acceptable for typical DB sizes (hundreds/thousands); can add secondary index later if needed |
| Separate Recoverable/Healed counts | Check reports what *can* be fixed; Heal reports what *was* fixed — avoids confusion |
| Backup path configurable | Users may want backups on a different drive or network share |
| `_loadFailed` flag blocks saves | Prevents catastrophic data loss: empty DB + save = all data gone |

---

## v1.9.0

Unrated watched items browser, smart image fallbacks, type filters, independent section loading, and CI/release improvements.

### New Features

- **Unrated Watched Items** — "Movies You Watched But Didn't Rate" and "Shows You Watched But Didn't Rate" sections on the Viewer Ratings page
- **Smart Image Fallback** — Card thumbnails try Thumb → Backdrop → Primary image type so posters always appear (16:9 crop, optimized quality)
- **Type Filter** — Filter "All Rated Items" by All / Movies / Shows / Episodes
- **Independent Section Loading** — Movies render instantly (~60ms) without waiting for the slower Series query; Shows section displays a loading placeholder
- **Relevant Sort Options** — Unrated sections sort by Last Watched, Oldest Watched, Title A-Z, Title Z-A (no rating-based sorts since all items are unrated)
- **Fast Unrated API** — Unrated items fetched via Jellyfin's native `/Items` API instead of a custom server endpoint
- **Dynamic Release Notes** — CI now reads from CHANGELOG.md for release body instead of hardcoded "Release X.Y.Z"

### Architecture Decisions

| Decision | Rationale |
|---|---|
| Frontend `/Items` API calls instead of custom server endpoint | Jellyfin's native API is faster and doesn't require server-side item resolution for unrated items |
| `IsPlayed=true` for both Movies and Series | Series `UserData` lacks fields for partial watch detection (`PlayCount=0`, `LastPlayedDate=null`) |
| Independent promise rendering (not `Promise.all`) | Movies render in ~60ms; don't block on the ~5.8s Series query |
| Thumb → Backdrop → Primary image fallback | `Thumb` is often missing; `Primary` always exists |

---

## v1.8.4

Viewer page performance — eliminate per-item API calls, server provides item metadata.

- Eliminated 66+ per-item API calls on the ratings viewer page
- `GetAllRatedItems` API now includes `name`, `type`, `seriesId` resolved server-side via `ILibraryManager`
- Client falls back to per-item `ApiClient.getItem()` only for items missing metadata

---

## v1.8.3

Config page UX improvements.

- Moved global **Save** button to bottom of the page
- Auto-sync section with toggle, interval input, and user dropdown
- Plex import section with connection test, import button, and SSE progress display

---

## v1.8.2

Scheduled auto-sync for Plex ratings.

- `PlexSyncScheduledTask` implementing `IScheduledTask`
- Configurable interval (1–168 hours, default 24)
- Sync user selection dropdown

---

## v1.8.1

Jellyfin 10.11+ targeting fix (net9.0, namespace/signature changes).

---

## v1.8.0

Plex rating import with SSE progress, encrypted tokens, series-level transfer.

- **Plex Rating Import** — import ratings from Plex Media Server into Jellyfin
- **Plex Token Security** — AES-256-CBC encryption of Plex tokens using PBKDF2-SHA256 key from `/etc/machine-id`
- **SSE Progress Tracking** — real-time import progress with matched/skipped/unmatched counts
- Conflict resolution modes: Skip (default), Overwrite, Keep Higher
- Rating conversion: `round(plexRating / 2)` — Plex 10-point → Jellyfin 5-star
- ID resolution: IMDB → TMDB → TVDB via `ILibraryManager.GetItemList(HasAnyProviderId)`

---

## v1.7.37

Theme-aware UI, collapse/expand ratings, mobile responsive, injection reliability rewrite.

- **Collapse/Expand Rating Dialog** — click header to toggle between collapsed and expanded views
- **Mobile Responsive Layout** — buttons adapt to smaller screens
- **ElegantFin Theme Compatibility** — replaced hardcoded colors with CSS custom properties