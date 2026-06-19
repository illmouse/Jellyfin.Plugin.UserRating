# Changelog

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