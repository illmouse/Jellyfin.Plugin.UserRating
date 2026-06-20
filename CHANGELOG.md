# Changelog

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