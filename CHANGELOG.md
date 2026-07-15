# Changelog

## v1.12.5.5 (beta)

### Fixes

- **Badge Not Loading on Movie Detail Pages** — Intermittent timing race: `injectRatingsUI` found `.itemMiscInfo-primary` but `.starRatingContainer` wasn't rendered yet (Jellyfin adds it progressively after metadata fetch). The retry gate now waits for `.starRatingContainer` before proceeding, not just `.itemMiscInfo-primary`.
- **Fallback Append When Star Rating Missing** — If `.starRatingContainer` is absent, the badge now appends to the end of `.itemMiscInfo-primary` instead of silently bailing with `return null`. Ensures the badge always renders even on items without community ratings.
- **Success Validation** — After `Promise.all` resolves, the injector now checks whether the badge actually exists in the DOM. If missing, it retries instead of logging a misleading "injected" success. Turns silent failures into retries.
- **Missing `emby-scroller-container` Class** — `#urRatingsCollapsible` now includes the `emby-scroller-container` class, matching native Cast/Similar sections for proper positioning and styling hooks.

---

## v1.12.5.4 (beta)

### Fixes

- **Person Icon Color** — The community-average person icon is now blue/teal (`var(--highlightOutlineColor, #00a4dc)`) instead of gold, providing better visual distinction from the gold ★ used for the public IMDb rating.
- **Rating Number Color** — The numeric score after the heart (♥) now uses the default page text color (`inherit`) instead of red. The heart itself stays red; only the number changes.
- **Ratings Title-to-Cards Gap** — Reduced the margin between the "Ratings" section title and the card scroller from `0.8em` to `0.4em` to match native Cast/Similar section spacing.

### Improvements

- **Native Scroll Button Styling** — Scroll buttons in the Ratings section now use native Jellyfin styling (`paper-icon-button-light` class: transparent background, circular, Material Icons `chevron_left`/`chevron_right`). Buttons sit inline with the `h2` title in the same row (flat siblings, not a nested wrapper), matching the Cast & Crew and More Like This sections above.

---

## v1.12.5.3 (beta)

### Fixes

- **Global /10 Rating Scale** — All rating displays now show scores on a 10-point scale (e.g. `♥ 9/10`, `★ 8.6/10`) instead of the previous `/5` suffix. The 5-star visual with half-stars is preserved; only the numeric score changes. Backend already stored 1-10; this is a display-only change.
- **Heart Icon Color** — The personal rating heart (♥) is now red (`#e53935`) instead of teal, matching conventional "favorite/love" semantics.
- **Person Icon Color** — The community-average person icon is now gold (`#ffd700`), matching the star color used for public IMDb ratings.
- **Popup Shows "Post Rating" on Already-Rated Items** — The badge click handler used sync `getUserRating(itemId)` which read from `userRatingsMap` before the fire-and-forget `fetchUserRatings()` populated it. Now uses `await loadMyRating(itemId)` so the popup correctly shows "Update" + delete button for already-rated items.
- **Ratings Section Crash on Navigation** — `injectRatingsSection` used `is="emby-scroller"`/`is="emby-itemscontainer"` attributes which triggered Jellyfin's web component upgrade (`attachedCallback` → `addScrollEventListener is not a function` crash) because our itemsContainer was a plain div. Replaced with plain divs and manual scroll button logic. Fixes the "rating icons not loading" issue on details→details navigation.
- **Scroll Button Disable State** — Left scroll button now grays out when at the start of the scroller; right button grays out at the end. Matches native Jellyfin scroll button behavior.

### Improvements

- **Plain-Divs Scroller** — Ratings section no longer relies on Jellyfin web component prototypes, making it robust against Jellyfin version changes that alter the `emby-scroller`/`emby-itemscontainer` element interfaces.

---

## v1.12.5.2 (beta)

### Fixes

- **Heart Icon Not Showing on Badge** — `injectRatingBadge` now uses `await loadMyRating(itemId)` (async API call) instead of the sync `getUserRating` lookup, which read from `userRatingsMap` before it was populated by the fire-and-forget `fetchUserRatings()`. The badge now correctly renders in the rated state on page load.
- **Badge Format Overhaul** — Replaced the gold ★ for community average with a teal person Material Icon (`person`) for visual distinction from the public IMDb rating. Personal rating keeps the teal ♥ heart. Removed the `/5` suffix from all badge states. Unrated states now show `♥ RATE` as the call-to-action.
- **Hover Tooltip** — Badge now has a `title` attribute showing both the community average (with rating count) and the user's personal rating (or "Click to rate" when unrated).

### Improvements

- **Native Ratings Section DOM** — Rewrote `injectRatingsSection` to produce a DOM structure faithful to Jellyfin's native Cast/Similar sections: flat `h2` + `emby-scrollbuttons` (with `paper-icon-button-light` buttons and `chevron_left`/`chevron_right` Material Icons) + `emby-scroller`/`emby-itemscontainer` wrappers. Rating cards use `inline-flex` with `vertical-align:top` to match native `white-space:nowrap` scroll behavior.

---

## v1.12.5.1 (beta)

### Redesign

- **Detail-Page Rating UI Redesign** — Replaced the tall inline `#user-ratings-ui` block with a compact clickable badge inserted next to the public IMDb rating in `.itemMiscInfo-primary`. The badge shows the community average (gold ★) alongside the user's own rating (teal ♥) when rated, or a "Rate" call-to-action when unrated. Clicking the badge opens the existing rate popup (no card context needed).
- **Bottom "Ratings" Section** — Added a horizontal scroller `#urRatingsCollapsible` after the "More Like This" section, mimicking Jellyfin's Cast/Similar card layout. Each card shows a reviewer's avatar, name, star rating, date, and a 3-line-clamped comment. Clicking a card opens a new read-only details popup with the full review.
- **No-Reload Popup Handlers** — Save/delete from the detail-page popup now refresh the badge and ratings section in-place (updating `userRatingsMap` and re-rendering the user's card) instead of calling `location.reload()`. Card-context path remains intact.
- **Removed** `createRatingsUI`, `displayAllRatings`, and the old `#user-ratings-ui` injection flow. The MutationObserver and hashchange handler now clean up the badge/section/details popup on navigation.

---

## v1.12.5.0

### Bug Fixes

- **Rating UI Not Loaded on Details→Details Navigation (Rated Items)** — Navigating from one item's details page directly to another item's details page (without returning to home) caused the user rating UI to fail to appear for items that had ratings. Container queries in `injectRatingsUI` now filter out hidden cached `#itemDetailPage` copies (Jellyfin's ring cache keeps hidden copies during details→details navigation). The MutationObserver no longer resets `isInjecting` mid-flight, which prevented a duplicate injection race that affected rated items more (slower `createRatingsUI` due to 3 sequential API calls).

---

## v1.12.4.1

### Bug Fixes

- **Rating UI Not Loaded on Details→Details Navigation (Rated Items)** — When navigating from one item's details page directly to another item's details page (without returning to home), the user rating UI failed to appear for items that had ratings. Two root causes fixed: (1) Container queries in `injectRatingsUI` used `document.querySelector()` which returns the first match in DOM order — Jellyfin keeps hidden cached `#itemDetailPage` copies with `.hide` class during details→details navigation, and the hidden copy often precedes the visible one in DOM order, causing the UI to be injected into an invisible container. Now uses `querySelectorAll` + visibility filtering (walks parent chain to check for `.hide`). (2) The MutationObserver reset `isInjecting = false` when progressive detail-page sections appeared (Cast, More Like This), which raced with the slower `createRatingsUI` for rated items (extra API calls + render time), causing duplicate or missed injections. Now guards with `!isInjecting` check before triggering re-injection.

---

## v1.12.4.0

### Bug Fixes

- **Collapsed/Ghost Pages After Navigation** — Navigating Home → item details → back → different tab left stale pages visible in the background (home bleeding through Favorites, item cards lingering, mixed overlays). The plugin now makes `#ratingsTab` a real `.tabContent.pageTabContent` inside `#indexPage`, recreates it fresh on every tab click, and lets Jellyfin's native tab system manage visibility via `.is-active`.

### New Features

- **Average Rating Overlay on All Cards** — `★ X.X (count)` badge on cards across all Jellyfin surfaces (library, home, collections, search). Items with no ratings show no badge.
- **Maintained Averages Index** — Server-side `Dictionary<Guid, (Sum, Count)>` index updated on every rating mutation for O(K) batch lookups.
- **Client-Side Average Cache** — Session-scoped LRU cache (max 500 entries) eliminates redundant batch fetches.
- **Single-Flight Batch Coalescing** — Concurrent decoration requests coalesced into one in-flight request.
- **New Batch Endpoint** — `POST /api/UserRatings/BatchAverage` accepts up to 100 ItemIds.

---

## v1.12.3.8

### Reverts

- **Reverted v1.12.3.7 cached-DOM-reuse experiment.** The attempt to skip re-render when `#ratingsTab` was still active in cached `#indexPage` caused multiple regressions: rating content not loaded on details page when opening from ratingsTab, Home tab marked active on return despite being on ratingsTab, Home tab click not loading content, and Favorites tab breaking after return. Reverted to v1.12.3.6 behavior: always re-render on back-from-details via programmatic tab click. Back-from-details lands at top of User Ratings tab (no scroll restore), which is stable.

---

## v1.12.3.7

### Bug Fixes

- **No Reload on Back-From-Details (Common Case)** — When returning from item details to the User Ratings tab, the plugin now checks if the cached `#ratingsTab` is still present and active in the visible `#indexPage` (Jellyfin's ring cache reused the DOM). If so, it skips re-rendering — matching how Jellyfin's native tabs behave (cached content shown as-is, no flash, scroll potentially preserved). Only when the cached DOM was evicted (deep navigation, 4+ hops) does it fall back to the full re-render via programmatic tab click. Previously, every back-navigation forced a full re-render with a loading flash.

### Refactors

- **Tightened Skip Guard** — The tab click skip guard now checks `.is-active` on `#ratingsTab` in addition to existence and visibility. This prevents a false "already active" skip when Jellyfin resets to the Home tab on cache restore but the button still has `emby-tab-button-active`.

---

## v1.12.3.6

### Bug Fixes

- **Removed Scroll Position Restore** — The scroll save/restore mechanism was unreliable (saved `window.scrollY` but restore landed at top anyway due to async rendering timing). Removed all scroll-restore code for stability: back-from-details now lands at the top of the User Ratings tab, which is predictable and consistent. Stability prioritized over convenience.

### Refactors

- **Removed Vestigial `userRatingsDirty` Flag** — The `userRatingsDirty` sessionStorage flag had no remaining effect (the click-driven `displayRatingsList()` already fetches fresh data on every tab activation). Removed all 6 references: the 4 setter calls in rating add/delete handlers, and the reader block in the hashchange handler.

---

## v1.12.3.5

### Bug Fixes

- **Scroll Position Restore Fixed** — The scroll save/restore code was using `ratingsTab.scrollTop` which was always 0 because `#ratingsTab` is not the scroll container (the window/document is, due to `#indexPage` being `position:absolute` via `.mainAnimatedPage`). Now uses `window.scrollY` for saving and `window.scrollTo()` wrapped in `requestAnimationFrame` for restoring, ensuring the browser has finished layout before scrolling.

### Refactors

- **Removed Dead Back-Navigation Code** — Removed `restoreLastSection()` function and `lastVisibleSection` history-state tracking (replaced by pixel-exact scroll restore). Removed redundant `popstate` handler (hashchange covers all SPA navigation). Removed unused `lastCheckedItemId` variable and `checkAndInjectTab()` wrapper. Merged duplicate hashchange listeners into one. Simplified MutationObserver options (dropped `attributes` and `characterData` that caused excessive callbacks). Simplified dirty-changes handling (the click-driven `displayRatingsList()` already fetches fresh data — no separate re-fetch needed).

---

## v1.12.3.4

### Bug Fixes

- **User Ratings Tab Breaks After Navigation** — The v1.12.3.3 fix made `#ratingsTab` a real `.tabContent.pageTabContent` inside `#indexPage`, but Jellyfin's `data-dom-cache` creates duplicate `#indexPage` copies during home↔details navigation. `#ratingsTab` could get trapped inside a hidden cached copy, making the tab content invisible even though the tab button showed as active. Now `displayRatingsList()` always removes any existing `#ratingsTab` from all `#indexPage` copies and creates a fresh one inside the currently visible `#indexPage`. The tab click skip-guard also checks if `#ratingsTab` is actually visible before skipping. Added scroll position save/restore so back-from-details returns the user to the exact pixel they were at.

---

## v1.12.3.3

### Bug Fixes

- **Collapsed/Ghost Pages After Navigation (Root Cause Fix)** — The v1.12.3.2 fix removed page-level interference but left tab-level state broken. The real root cause was that `#ratingsTab` was a fake sibling `[data-role="page"]` overlay with `position:absolute`, and the tab button click handler called `e.preventDefault()`, blocking Jellyfin's own tab handler from deactivating `.is-active` on native `.pageTabContent` elements. This caused stale `.is-active` classes to accumulate across navigation cycles (e.g. both `homeTab` and `favoritesTab` marked active simultaneously), producing the "ghost page in background" effect. Now `#ratingsTab` is a real `.tabContent.pageTabContent` div inside `#indexPage` (just like `favoritesTab`), `e.preventDefault()` is removed, and Jellyfin's native tab system fully manages visibility via `.is-active`. Back-navigation restore uses a programmatic `.click()` on the tab button instead of manual class toggling.

---

## v1.12.3.2

### Bug Fixes

- **Collapsed/Ghost Pages After Navigation** — Fixed a bug where navigating Home → item card → Home → Favorites (or User Ratings) left stale pages visible in the background: home bleeding through behind Favorites, item cards lingering behind other tabs, mixed User Ratings + item card overlays. The plugin was fighting Jellyfin's SPA router by toggling `.hide`/`display` on `[data-role="page"]` elements it didn't own, using fragile `querySelector` selectors that matched the wrong page copy after Jellyfin re-created `#indexPage` during home↔details transitions. The plugin now only manages its own `#ratingsTab` element and lets Jellyfin reconcile its own pages.

---

## v1.12.3.1

### New Features

- **Average Rating Overlay on All Cards** — A non-interactive `★ X.X (count)` average rating badge now appears on cards across all Jellyfin surfaces: library browse grids, home page suggestion rows, collections, and search results. Items with no ratings show no badge. Clicks pass through to the underlying card (badge has `pointer-events: none`).

### Performance

- **Maintained Averages Index** — The server now keeps a `Dictionary<Guid, (Sum, Count)>` index updated on every rating mutation (save, delete, bulk import, re-key, migration), so batch average lookups are O(K) for K requested items instead of a full scan of all ratings. The dashboard's `AllRatedItems` endpoint benefits too.
- **Client-Side Average Cache** — Session-scoped LRU cache (max 500 entries) keyed by ItemId eliminates redundant batch fetches when navigating between pages that share items.
- **Single-Flight Batch Coalescing** — Concurrent decoration requests (e.g. during fast scroll) are coalesced into a single in-flight `POST /api/UserRatings/BatchAverage` request, with queued IDs absorbed into the next request. Prevents request storms.
- **New Batch Endpoint** — `POST /api/UserRatings/BatchAverage` accepts up to 100 ItemIds and returns a map of `{itemId → {averageRating, totalRatings}}`. Unrated items are omitted from the response.

### Bug Fixes

- **Provider Index Stale After 10-Star Migration** — `MigrateTo10StarScale` reassigned `_ratings` but never rebuilt `_providerIndex`, leaving provider-ID lookups stale post-migration. Now calls `RebuildProviderIndex()` alongside the new `RebuildAveragesIndex()`.

### Refactors

- **Unified Card Decoration** — Dashboard card badge injection (average badge + personal compact badge) and the new global card decoration now share a single `decorateCard`/`decorateCardsIn` code path. `buildCategoryGrid`/`buildUnratedGrid` emit plain Jellyfin `backdropCard` markup; badges are injected post-render. Removed `fillCompactBadges` and `attachRatedCardListeners` (folded into `decorateCard`).

---

## v1.12.3.0

### New Features

- **Clickable Rate Badge on Unrated Cards** — Unrated dashboard cards show a solid green "Rate" badge button. Clicking opens the rating popup directly. After rating, the badge transitions to "★ N/5". Replaces the earlier hover-to-rate overlay — improves mobile usability where hover was not discoverable.
- **Half-Star Rating Precision** — The rating popup supports half-star precision. Click the left or right half of any star to set a `.5` value (e.g., 3.5/5).
- **Version History Table** — Plugin settings now show a scrollable version history table with install dates. Each version installed on the server is recorded with a timestamp; the current version is highlighted.
- **Configurable Page Size** — The "Recently Rated Items Count" setting now controls how many items are displayed per page across all dashboard sections.

### Performance

- **Server-Side Pagination** — The dashboard fetches only a page of items at a time from the server instead of loading all rated items at once. Eliminates multi-second load times on large libraries.

### UI/UX Improvements

- **Dashboard Simplification** — Replaced duplicate "Recently Rated" sections with two full-featured sections: Rated Movies and Rated Shows, each with sorting, direction toggle, and pagination. Added Watched Movies/Shows sections for not-yet-rated content.
- **Scheduled Sync Section Reorganized** — "Enable Automatic Sync" toggle now appears first, followed by sync options, with "Sync Now" as the last element.
- **Sync Watch History Description Clarified** — Updated to: "Sync all watch history from Plex and mark items as played in Jellyfin. This includes all movies and episodes you've watched on Plex, not just rated items."

---

## v1.12.2.20

### Changed

- **Surrender to Jellyfin default on browser refresh** — On F5 refresh, the Home tab is now shown (content and highlight), matching Jellyfin's native behaviour for the Favourites tab. Previously the plugin tried to restore the User Ratings tab content on refresh but the tab highlight stayed on Home, causing an inconsistent UI. The `userRatingsActive` history state is now cleared on fresh page load. Returning from an item details page (in-app back navigation) still restores the User Ratings tab and scroll position as before.

---

## v1.12.2.19

### Fixed

- **User Ratings tab highlight after browser refresh** — On refresh, the tab content was restored but the Home tab remained marked active. The active-class highlight and content restore are now applied atomically at tab-button injection time, eliminating the timing race with Jellyfin's home rendering.

---

## v1.12.2.18

### Changed

- **Removed star icon from User Ratings tab** — The `star_border` Material Icon on the tab button was not native to Jellyfin's theme; built-in tabs (Home, Favourites) don't use icons. Removed the icon span and its dedicated CSS rule (`.emby-tab-button .material-icons`) to match the native tab styling.

---

## v1.12.2.17

### Changed

- **Sync Watch History description clarified** — The previous annotation ("Also mark items as played based on Plex watch history") was misleading — it implied only rated items would be synced. Updated to: "Sync all watch history from Plex and mark items as played in Jellyfin. This includes all movies and episodes you've watched on Plex, not just rated items."

---

## v1.12.2.16

### Changed

- **Enable Automatic Sync moved to top of Scheduled Sync** — The feature toggle now appears first in the section, followed by the detailed sync options. Enabling a feature naturally precedes configuring its details.

---

## v1.12.2.15

### Changed

- **Sync Now button moved to end of Scheduled Sync** — The manual sync button now appears after the "Enable Automatic Sync" checkbox, as the last element in the Scheduled Sync section, rather than between the sync interval input and the progress bar.

---

## v1.12.2.14

### Changed

- **Removed redundant version history from Migration section** — The "Previous versions" line inside the Rating System Migration section duplicated the new Version History table. Removed the line and its JS population code; the dedicated Version History table above remains the single source.

---

## v1.12.2.13

### Added

- **Version History table** — Plugin settings now show a scrollable version history table with install dates. Each version installed on the server is recorded with a timestamp. Newest version appears at the top; the current version is highlighted in green with a "(current)" badge. Old versions from before this update show "Unknown" in the date column (the data model previously only stored version strings without timestamps). The table lives in its own section above the Rating System Migration section.

### Changed

- **PluginMetadata data model** — `VersionHistory` changed from `List<string>` to `List<VersionEntry>` (version + optional install date). A custom `JsonConverter` reads both the legacy string-array format and the new object format transparently, so existing `ratings.json` files upgrade automatically on first load. Added `CurrentVersionInstalledAt` field to track when the currently running version was installed.
- **Migration section** — The cramped version-history text blob inside the Rating System Migration section was replaced with a concise "Previous versions" line; the full version history now lives in its own dedicated section.

---

## v1.12.2.12

### Changed

- **Star icon on User Ratings tab restyled** — Removed inline styles from the `star_border` material icon; replaced with a CSS rule (`.emby-tab-button .material-icons`) that gives the icon proper sizing (1.25em), vertical alignment, and spacing. Added `aria-hidden="true"` for accessibility, matching Jellyfin's icon convention. The hollow star now renders at a natural icon size like other Material Icons throughout the UI.

---

## v1.12.2.11

### Fixed

- **Extra spacing between tab bar and Rated Movies section** — The User Ratings tab had a wider gap between the Home/Favourites/User Ratings tab bar and the first section than built-in tabs. Root cause: the `#ratingsTab` div already has class `libraryPage` which applies native Jellyfin `padding-top` (4.6em–7.5em depending on viewport) — the same mechanism used by Home/Favourites. An additional wrapper `<div style="padding-top: 4em;">` was doubling the gap. Removed the redundant inline padding; the `.libraryPage` class's native padding now handles it alone.

### Added

- **Star icon on User Ratings tab** — Added an outline star (`star_border` material icon) before the "User Ratings" text on the tab button, giving the tab a visual identity consistent with the plugin's rating theme.

---

## v1.12.2.10

### Fixed

- **Persistent scroll shift after filter/sort changes** — v1.12.2.9 attempted to fix the scroll shift by adding `block: 'start'` to `scrollIntoView` calls, but this made the problem worse: forcing the section top to the viewport top caused a visible downward shift on every sort/filter/page interaction. Root cause: the section containers are fixed DOM elements — only their `innerHTML` changes on re-render, so the sort/filter controls stay in place naturally without any scrolling. Removed all 13 `scrollIntoView` calls from Movies/Shows/Unrated event handlers. The only remaining `scrollIntoView` is in `restoreLastSection` (navigating back to a previously-viewed section).

---

## v1.12.2.9

### Fixed

- **Sort direction toggle in unrated sections** — The sort direction arrow in "Watched Movies — Not Yet Rated" and "Watched Shows — Not Yet Rated" sections was stuck: clicking toggled the sort but the arrow never flipped, and the direction only changed one way. Root cause: the arrow icon was hardcoded in the re-rendered HTML template, overwriting the manual DOM update. Fixed by adding `unratedSortDir` state variable and making the template read the arrow direction from state (same pattern used by Rated Movies/Shows sections).
- **Scroll shift after filter/sort changes** — After changing sort, filter, or page in any dashboard section, the page scrolled so the section bottom was visible, pushing the sort/filter controls off-screen. Fixed by adding `block: 'start'` to all `scrollIntoView` calls so the section top (where controls are) aligns to the viewport top.

---

## v1.12.2.8

### Changed

- **Rate badge redesign** — The "Rate" badge on unrated cards now has a solid green gradient background with border, border-radius, and box-shadow — looks like a real button instead of a flat transparent label. Hover effect lifts and scales the badge slightly (web feedback), active state presses it down (tactile for touch). Removed all inline styles; badge is fully CSS-styled via `.rate-badge` class.

---

## v1.12.2.7

### Changed

- **Configurable page size** — The "Recently Rated Items Count" setting now controls how many items are displayed per page across all dashboard sections (Rated Movies, Rated Shows, Watched Movies, Watched Shows). Previously hardcoded to 24. Updated the config page description to reflect the new behavior.
- **Clickable Rate badge on unrated cards** — Removed the separate "RATE" overlay gradient at the bottom of unrated cards. The existing red "Unrated" badge in the corner is now a clickable "Rate" badge (with star icon) that opens the rating popup directly. After rating, the badge transitions to "★ N/5" as before. Cleaner UI with no duplicate overlay.

---

## v1.12.2.6

### New Features

- **Dashboard Simplification** — Removed duplicate "Recently Rated Movies/Shows/Episodes" and "All Rated Items" sections. Replaced with two full-featured sections: **Rated Movies** (paginated, sortable by Rating/Title/Recently Rated/Most Ratings, direction toggle, default Recently Rated) and **Rated Shows** (same controls plus tab sub-filter for All/Shows/Episodes). Both sections support server-side pagination to browse all rated items.
- **RATE Button on Unrated Cards** — Replaced the hover-to-rate star overlay on unrated dashboard cards with an always-visible "RATE" clickable button. Clicking opens the rating popup directly (no preselection). Improves mobile usability where hover was not discoverable.

### Changed

- **AllRatedItems API** — `typeFilter` parameter now accepts comma-separated values (e.g., `Series,Episode`) to support the Shows section's "All" tab combining shows and episodes.

---

## v1.12.2.5

### Fixed

- **Build warning** — Removed unused `logger` parameter from `RatingResolver` after simplification in v1.12.2.4 eliminated all logging calls in the class.

---

## v1.12.2.4

### Performance

- **Server-side pagination for All Rated Items** — The dashboard now fetches only 24 items per page from the server instead of loading all rated items at once. Page navigation, sort, and type filter changes trigger targeted API calls. Eliminates the 17-second load time on large libraries.
- **Batch library lookup** — Replaced 386+ individual `GetItemById` calls with a single batch `GetItemList(ItemIds=[...])` query. Library metadata resolution is now one indexed query regardless of rating count.
- **Removed provider-ID fallback from request path** — Broken items (deleted from library) no longer trigger expensive per-item provider-ID resolution lookups during page load. Stale items are skipped and healed by the scheduled health check task instead.
- **Simplified RatingResolver** — `ResolveRating`, `ResolveRatingsForItem`, and `HasRating` now do direct dictionary lookups only (O(1)). All self-healing logic moved to the scheduled health check task.
- **Provider-ID index** — Added an in-memory `Dictionary<(provider, id, userId), key>` to `RatingRepository`, rebuilt on load and maintained on mutations. `FindByProviderIds` is now O(1) instead of O(N) full scan.
- **Health check batch resolution** — `HealthCheckService.RunHealthCheck` now uses a single batch `GetItemList` call instead of per-item `GetItemById`.
- **Health check interval 24h → 30min** — Broken items from library updates are healed within 30 minutes instead of up to 24 hours. Users can adjust the interval via Jellyfin Dashboard > Scheduled Tasks.

---

## v1.12.2.3

### New Features

- **Half-Star Popup Rating** — The rating popup now supports half-star precision. Click the left or right half of any star to set a `.5` value (e.g., 3.5/5). Uses CSS gradient `background-clip: text` for smooth fill rendering. `openRatePopup` preselection, `fillCompactBadges` display, and edit flow all handle float values correctly.

---

## v1.12.2.2

### Fixed

- **Popup Stars Fixated by Hover** — Star fill in the rating popup only reset when leaving the entire popup overlay. `mouseleave` handler moved from the full-page backdrop to the star row container, so stars now correctly reset when the cursor leaves the star area.
- **Card Not Updating After Rating** — `_popupCardElement` was nulled by `closeRatePopup()` before `animateRatingSuccess` could read it. Captured to a local variable before closing the popup.

---

## v1.12.2.1

### New Features

- **Dashboard Hover-to-Rate (Phase 1)** — Hover over unrated cards on the User Ratings dashboard to reveal a star overlay. Click any star to open a popup modal with note input. The card stays in place with a green pulse animation and shows a compact "★ N/5 ✎" badge after rating. Rated cards show the compact badge top-left; clicking it opens the edit popup. All client-side, no API changes.

---

## v1.12.2.0

### New Features

- **Plex Watch History Sync** — Optional feature flag (`EnablePlexWatchHistorySync`) to mark movies and episodes as played in Jellyfin based on Plex view history, alongside the existing rating import. Works for both one-time and scheduled sync. Progress bar resets to 0% for watch history phase.

### Changed

- **Config Page Consolidation** — Removed the separate "Import Ratings (One-Time)" section. Manual import now uses the same settings as scheduled sync. Section renamed from "Automatic Sync (Scheduled)" to "Scheduled Sync" with a "Sync Now" button.

---

## v1.12.1.0

### BREAKING CHANGES

- **10-Star Rating Scale** — The rating system has been upgraded from 5-star (0–5) to 10-star with half-star precision (0–10). Existing ratings must be migrated via the plugin configuration page. The migration system creates an automatic backup before converting and provides a one-click migration button with version tracking and pre-migration warnings. After migration, the new scale applies everywhere — detail page, User Ratings dashboard, and all API endpoints.

### New Features

- **DOM State Sync on Back-Navigation** — When rating or unrating an item on the detail page and returning to the User Ratings dashboard, the dashboard now re-fetches data and scrolls to the section you were viewing. Items move correctly between "Not Yet Rated" and "Recently Rated" sections without a manual refresh.

### Fixed

- **Edit Button Not Showing After Posting New Rating** — The "Edit" button now appears immediately after posting a rating for the first time.
- **Missing Rating Display on Detail Pages** — Fixed cases where user ratings did not render on the detail page after the 10-star migration.
- **Migration Version History Display** — Fixed layout of the plugin version history section on the config page.

---

## v1.11.8.0

### Fixed

- **Plex Import: Conflict Mode Not Passed to API** — One-time import's "On Conflict" dropdown was cosmetic only; the API always used the saved config value (default "skip"). Now the conflict mode is passed as a query parameter, making the one-time import truly ephemeral. Auto-sync has its own persisted `SyncConflictMode` setting with a dedicated dropdown in the config UI.

---

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