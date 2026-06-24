# ratings.js Architecture Reference

Reference document for the client-side JavaScript (`Configuration/ratings.js`). Read this before modifying any injection logic, event handlers, or state management in the plugin's frontend code.

## Overview

`ratings.js` is a single IIFE (`(function() { 'use strict'; ... })();`) injected into Jellyfin's `index.html` via `ScriptInjectionMiddleware`. It injects one `<style>` block (lines 8–606) and registers all behavior on `document.body`.

## Three Injection Mechanisms

The plugin has three independent mechanisms that add UI to different Jellyfin surfaces:

### 1. Tab Button + Content (`injectRatingsTab`, line 2406)

Adds a "User Ratings" tab button to Jellyfin's home page tab slider and creates the `#ratingsTab` content div.

- **When:** only when `window.location.hash` includes `'home'` (line 2409)
- **Tab button:** created as `<button is="emby-button" class="emby-tab-button" data-ratings-tab="true">` and appended to `.emby-tabs-slider` in `skinHeader` (persistent across navigation)
- **Tab content:** `#ratingsTab` created as `<div class="tabContent pageTabContent">` inside the visible `#indexPage` via `ensureRatingsTabContent()` (line 2513)
- **Click handler:** calls `displayRatingsList()` to populate content; sets `history.state.userRatingsActive = true` for back-navigation restore
- **Skip guard:** if button is active AND `#ratingsTab` exists inside visible `#indexPage:not(.hide)`, skip re-render (line 2459)
- **Critical:** does NOT call `e.preventDefault()` — lets Jellyfin's native tab handler manage `.is-active` on `.pageTabContent` and `.emby-tab-button-active` on buttons

### 2. Detail Page UI (`injectRatingsUI`, line 1586)

Injects `#user-ratings-ui` (rating form + all ratings list) into item detail pages.

- **Item ID extraction:** three strategies — URLSearchParams, hash query string, GUID regex (lines 1594–1608)
- **Container lookup:** four selector strategies, all filtered with `isVisibleContainer()` to skip hidden cached `#itemDetailPage` copies (lines 1630–1670)
- **Concurrency:** guarded by `isInjecting` flag; cleared 200ms after `createRatingsUI` resolves (line 1697)
- **Retry:** up to 30 attempts with exponential backoff if container not found (lines 1672–1684)
- **Visibility filter:** `isVisibleContainer(el)` walks parent chain checking for `.hide` class (lines 1636–1644); `findVisible(selector)` returns first non-hidden match (lines 1646–1649)

### 3. Global Card Decoration (`decorateCard`, line 869)

Adds average rating badges (`★ X.X (count)`) and personal rating badges to `.card` elements across all Jellyfin surfaces (library, home, collections, search).

- **Idempotent:** `data-ur-decorated="1"` attribute prevents double-decoration (line 870)
- **Average badge:** `.ur-avg-badge` inside `.cardIndicators.cardIndicators-bottomright` (lines 875–888)
- **Personal badge:** `.compact-rating` inserted before `.cardImageContainer` (lines 890–922)
- **Batch fetch:** `decorateCardsIn()` uses `avgCache` first, then `averagesMap` from server response, then falls back to `fetchBatchAverageCoalesced()` (lines 931–957)
- **Debounced:** `scheduleGlobalDecorate()` 150ms debounce (line 2562)

## Event Flow

### MutationObserver #1 — Detail page detection (line 1710)

Observed: `document.body`, `{ subtree: true, childList: true }`

- **Trigger A (URL change):** if `location.href !== lastUrl`, removes old `#user-ratings-ui`, resets injection state, schedules `injectRatingsUI` at 150ms (lines 1714–1736)
- **Trigger B (detail containers added):** if `.detailPagePrimaryContent`/`.detailSection`/`.itemDetailPage` appears in added nodes AND `!isInjecting`, schedules `injectRatingsUI` at 100ms (lines 1743–1774). The `!isInjecting` guard prevents duplicate injection race — rated items have slower `createRatingsUI` (3 sequential API awaits), so progressive render could trigger a second injection without this guard.

### MutationObserver #2 — Tab injection + card decoration (line 2577)

Observed: `document.body`, `{ subtree: true, childList: true }`

- Calls `injectRatingsTab()` unconditionally on every mutation batch
- If any `.card[data-id]` was added, calls `scheduleGlobalDecorate()` (150ms debounce)

### hashchange handler (line 1783)

- Removes `#user-ratings-ui`
- If hash includes `'home'` AND `history.state.userRatingsActive`: strips `emby-tab-button-active` from all tab buttons, clicks the User Ratings tab button to restore it
- Resets `isInjecting`, `injectionAttempts`, `currentItemId`
- Schedules: `injectRatingsTab` (100ms, 500ms), `injectRatingsUI` (100ms, 300ms), `scheduleGlobalDecorate()`

### User Ratings tab click handler (line 2455)

- Skip guard: if button active AND `#ratingsTab` exists in visible `#indexPage`, return without refetch
- Otherwise: `await displayRatingsList()` → sets `history.state.userRatingsActive = true`

### Native tab click listeners (line 2493)

Capture-phase listeners on all native `.emby-tab-button` (not the ratings tab). On click: deletes `userRatingsActive` from history state so back-navigation restores the native tab, not User Ratings.

## Shared State Variables

| Variable | Line | Purpose |
|---|---|---|
| `currentItemId` | 608 | Item ID the detail-page UI is for; detects same-item vs new-item navigation |
| `currentRating` | 609 | Currently selected rating (0–10) in the detail-page form |
| `isInjecting` | 610 | Boolean lock preventing concurrent `injectRatingsUI` runs |
| `userRatingsMap` | 611 | `Object<itemId, {rating, note}>` — current user's ratings |
| `popupModal` | 612 | `#ratePopupOverlay` element, lazily created |
| `popupActiveItemId` | 613 | Item ID the popup is targeting |
| `AVG_CACHE_MAX` | 615 | 500 — max entries in `avgCache` |
| `avgCache` | 616 | `Map<itemId, {averageRating, totalRatings}>` — LRU cache |
| `avgCacheOrder` | 617 | `Array<itemId>` — MRU order for LRU eviction |
| `_batchInFlight` | 618 | Current in-flight batch-average promise for request coalescing |
| `_batchQueuedIds` | 619 | `Set<itemId>` — IDs queued while batch in flight |
| `_decorateTimer` | 620 | Debounce timer for `scheduleGlobalDecorate` |
| `_userRatingsPrimed` | 621 | Whether `fetchUserRatings` has run at least once |
| `injectionAttempts` | 1583 | Retry counter for `injectRatingsUI` |
| `maxInjectionAttempts` | 1584 | 30 — max retries |
| `lastUrl` | 1709 | Last `location.href` for URL change detection |
| `_popupCardElement` | 755 | Card element that opened the popup (for success animation) |

## displayRatingsList() Lifecycle (line 1821)

1. Find visible `#indexPage:not(.hide)` (fallback: any `#indexPage`)
2. Remove ALL `#ratingsTab` copies (orphan cleanup)
3. Create fresh `#ratingsTab` inside visible `#indexPage`
4. Show "Loading ratings..." placeholder
5. `await fetchUserRatings()` — populates `userRatingsMap`
6. Get config for page size (`RecentlyRatedItemsCount`, default 24)
7. Build placeholder sections HTML (`#moviesSection`, `#showsSection`, `#unratedMoviesSection`, `#unratedSeriesSection`)
8. `renderMoviesSection(1)` — server-paginated via `fetchRatedItemsPage()` (sort: rating/title/recent/count)
9. `renderShowsSection(1)` — same + type sub-filter (All/Series/Episode)
10. Fetch unrated watched items — client-side paginated
11. `decorateCardsIn(#ratingsTab, null)` + `attachRateButtonListeners(#ratingsTab)`

**Always recreates fresh.** This is the stability guarantee — no dependency on cached DOM state.

## createRatingsUI() Lifecycle (line 1240)

1. Create `#user-ratings-ui` container
2. `await ApiClient.getItem(userId, itemId)` — item name for header
3. Build header (title, average display, summary)
4. Build My Rating section (interactive stars, note textarea, save/delete buttons)
5. Build All Ratings placeholder
6. `await loadMyRating(itemId)` — pre-fill if rated
7. `await displayAllRatings(itemId, container)` — render all ratings list

**Why rated items are slower:** 3 sequential API awaits (`getItem` + `loadMyRating` + `loadRatings` via `displayAllRatings`). Unrated items short-circuit `loadMyRating` (returns null fast) but still wait for `loadRatings`. The wider async window for rated items is why the `isInjecting` race existed before the `!isInjecting` guard was added to MutationObserver #1.

## Key Functions Index

| Line | Function | Description |
|---|---|---|
| 623 | `createStarRating(rating, interactive, onHover, onClick)` | Builds 5-star element with half-star support |
| 664 | `updateStarDisplay(container, rating)` | Sets filled/half classes from 0–10 rating |
| 678 | `loadRatings(itemId)` | GET api/UserRatings/Item/{itemId} |
| 694 | `loadMyRating(itemId)` | GET api/UserRatings/MyRating/{itemId} |
| 710 | `saveRating(itemId, rating, note)` | POST api/UserRatings/Rate |
| 736 | `deleteRating(itemId)` | DELETE api/UserRatings/Rating |
| 757 | `fetchUserRatings()` | GET api/UserRatings/User/{userId} — populates userRatingsMap |
| 777 | `getUserRating(itemId)` | Sync lookup in userRatingsMap |
| 785 | `avgCacheGet(itemId)` | LRU get from avgCache |
| 795 | `avgCacheSet(itemId, entry)` | LRU insert with eviction at 500 |
| 807 | `fetchBatchAverage(itemIds)` | POST api/UserRatings/BatchAverage |
| 842 | `fetchBatchAverageCoalesced(itemIds)` | Coalesces concurrent batch requests |
| 869 | `decorateCard(card, info)` | Adds avg + personal badge to one card |
| 931 | `decorateCardsIn(container, averagesMap)` | Decorates all undecorated cards in container |
| 969 | `decorateAllCards()` | Primes cache then decorates globally |
| 1240 | `createRatingsUI(itemId)` | Builds detail-page rating UI (3 awaits) |
| 1493 | `displayAllRatings(itemId, container)` | Renders all ratings list |
| 1586 | `injectRatingsUI()` | Finds item ID + visible container, injects UI |
| 1821 | `displayRatingsList()` | Builds home User Ratings tab content |
| 1948 | `fetchRatedItemsPage(...)` | GET api/UserRatings/AllRatedItems (paginated) |
| 2406 | `injectRatingsTab()` | Adds tab button + creates #ratingsTab |
| 2513 | `ensureRatingsTabContent()` | Creates/moves #ratingsTab into visible #indexPage |
| 2562 | `scheduleGlobalDecorate()` | 150ms debounce for card decoration |

## Boot Sequence (lines 2539–2574)

1. Clear stale `userRatingsActive` from history (fresh page load surrenders to Home tab)
2. `injectRatingsTab()` immediately + 5 timers (100/500/1000/2000/3000ms) + `setInterval(2000)`
3. `fetchUserRatings()` to prime cache, set `_userRatingsPrimed = true`
4. `decorateAllCards()` immediately + 3 timers (300/1000/2500ms) + `setInterval(3000)`
5. Register MutationObserver #2

## CSS (lines 8–606)

- `#user-ratings-ui` grid layout (line 10)
- Rating form, stars, inputs, buttons, all-ratings items (lines 14–355)
- `.rate-badge` green badge for unrated cards (lines 358–384)
- `.compact-rating` overlay badge for rated cards (lines 387–426)
- `@keyframes rate-success-pulse` (lines 429–436)
- `.ur-avg-badge` average badge (lines 439–459)
- `.rate-popup-overlay` / `.rate-popup` modal (lines 462–604)

All CSS uses `var(--custom, fallback)` patterns to integrate with ElegantFin and default Jellyfin themes.