# Client-Side Pitfalls — Lessons From Real Bugs

Anti-patterns and gotchas discovered while fixing bugs in `Configuration/ratings.js`. Read this before changing any navigation, tab, injection, or DOM-querying code.

## 1. Never use `querySelector` without visibility filtering

**Bug:** Rating UI not loaded on details→details navigation for rated items (v1.12.4.1 fix).

**Cause:** `document.querySelector('.detailPagePrimaryContent .detailSection')` returns the first match in DOM order. During details→details navigation, Jellyfin's ring cache keeps the old `#itemDetailPage` (hidden with `.hide`). If the hidden copy precedes the visible one in DOM order, `querySelector` returns the hidden `.detailSection`, and the UI gets injected into an invisible container.

**Fix:** Always filter by visibility. Walk the parent chain to check for `.hide`:

```js
function isVisibleContainer(el) {
    if (!el) return false;
    let node = el;
    while (node) {
        if (node.classList && node.classList.contains('hide')) return false;
        node = node.parentElement;
    }
    return true;
}

function findVisible(selector) {
    const candidates = document.querySelectorAll(selector);
    return Array.from(candidates).find(isVisibleContainer) || null;
}
```

**Rule:** Any `document.querySelector` for a DOM container that could exist in a cached hidden page MUST use `findVisible` or equivalent visibility filtering. This applies to all detail-page container lookups and all `#ratingsTab` lookups.

## 2. Never call `e.preventDefault()` on native Jellyfin elements

**Bug:** Collapsed/ghost pages after navigation (v1.12.3.3 fix).

**Cause:** The tab button click handler called `e.preventDefault()`, blocking Jellyfin's own tab handler from managing `.is-active` on `.pageTabContent` elements. Stale `.is-active` classes accumulated across navigation cycles, producing ghost pages.

**Fix:** Remove `e.preventDefault()`. Let Jellyfin's native tab handler fully manage `.is-active` on `.pageTabContent` and `.emby-tab-button-active` on buttons. The plugin should only populate content, never manage visibility classes that Jellyfin owns.

**Rule:** Never call `e.preventDefault()` on clicks of `.emby-tab-button` or any other Jellyfin-native UI element unless you fully understand and intend to override Jellyfin's handler.

## 3. Never toggle `.hide` on pages you don't own

**Bug:** Collapsed/ghost pages after navigation (v1.12.3.2 fix).

**Cause:** The plugin toggled `.hide`/`display` on `[data-role="page"]` elements using fragile `querySelector` selectors that matched the wrong page copy after Jellyfin re-created `#indexPage` during home↔details transitions.

**Fix:** Only manage elements the plugin created (`#ratingsTab`, `#user-ratings-ui`, `.ur-avg-badge`, `.compact-rating`, `.rate-badge`). Let Jellyfin reconcile its own pages.

**Rule:** The plugin must never add/remove `.hide` on `[data-role="page"]`, `.mainAnimatedPage`, `#indexPage`, `#itemDetailPage`, or any Jellyfin-owned page container.

## 4. Don't reset `isInjecting` while `createRatingsUI` is in flight

**Bug:** Rating UI not loaded on details→details navigation for rated items (v1.12.4.1 fix).

**Cause:** MutationObserver #1 detected progressive detail-page sections (Cast, More Like This) appearing and reset `isInjecting = false` while `createRatingsUI` was still awaiting API calls. This allowed a concurrent `injectRatingsUI` to pass the concurrency guard and start a second `createRatingsUI`, causing duplicate or missed injections. Rated items were affected more because `createRatingsUI` for rated items has 3 sequential API awaits (getItem + loadMyRating + loadRatings), giving the observer a wider window to fire.

**Fix:** Guard the observer's reset with `!isInjecting`:

```js
if (!isInjecting) {
    injectionAttempts = 0;
    setTimeout(injectRatingsUI, 100);
}
```

**Rule:** Any MutationObserver or event handler that triggers `injectRatingsUI` must check `!isInjecting` before resetting state or scheduling a new injection.

## 5. Scroll container is the window, not `#ratingsTab`

**Bug:** Scroll position restore failed silently (v1.12.3.5 fix attempt, removed in v1.12.3.6).

**Cause:** Code saved `ratingsTab.scrollTop` but `#ratingsTab` is not the scroll container. `#indexPage` is `position:absolute` via `.mainAnimatedPage` CSS with no `overflow-y:auto` — content overflows to the document/window. `ratingsTab.scrollTop` was always 0.

**Fix:** Use `window.scrollY` for saving and `window.scrollTo()` for restoring. But note: scroll restore is unreliable due to async rendering timing — we removed it entirely for stability (v1.12.3.6). Back-from-details lands at top, matching Jellyfin's native tab behavior.

**Rule:** The scroll container for plugin content inside `#indexPage` is the **window/document**. Never use `element.scrollTop` on `#ratingsTab` or `.pageTabContent`.

## 6. Don't depend on cached DOM for `#ratingsTab`

**Bug:** User Ratings tab breaks after deep navigation (v1.12.3.4 fix).

**Cause:** `#ratingsTab` could get trapped inside a hidden cached `#indexPage` copy. Jellyfin's `data-dom-cache` creates duplicate `#indexPage` elements during home↔details navigation.

**Fix:** `displayRatingsList()` always removes ALL `#ratingsTab` copies and creates a fresh one inside the currently visible `#indexPage:not(.hide)`. This is the "always recreate" approach (Option C) — it's stable because it makes no assumptions about DOM persistence.

**Rule:** `displayRatingsList()` must always: (1) remove all `#ratingsTab` from all `#indexPage` copies, (2) create fresh inside the visible `#indexPage:not(.hide)`. Never assume `#ratingsTab` from a previous render is still valid.

## 7. Tab buttons persist, tab content does not

**Bug:** Tab button shows active but `#ratingsTab` is invisible (v1.12.3.4 fix).

**Cause:** Tab buttons live in `skinHeader` (persistent across navigation). Tab content lives inside `#indexPage` (cached/evicted/recreated by Jellyfin's ring cache). The button survives cache eviction; the content does not.

**Fix:** The tab click skip guard checks both: (a) button has `emby-tab-button-active` AND (b) `#ratingsTab` exists inside the visible `#indexPage:not(.hide)`. If the button is active but `#ratingsTab` is missing or in a hidden page, force re-creation.

**Rule:** Always check both button state AND content visibility. Button active ≠ content visible.

## 8. Attempted cached-DOM-reuse causes regressions

**Bug:** v1.12.3.7 experiment — tried to skip re-render when cached `#ratingsTab` was still active. Caused: rating content not loaded on details page, Home tab marked active on return, Home tab click not loading, Favorites tab breaking after return.

**Cause:** Jellyfin's native tab system and the plugin's tab management have interdependent state. Skipping the re-render (which includes stripping `emby-tab-button-active` and clicking the tab) left Jellyfin's tab state inconsistent.

**Fix:** Reverted to always-click approach (v1.12.3.8). The stable approach is: always strip `emby-tab-button-active` from all buttons, then click the User Ratings tab button. This lets Jellyfin's native handler reset state cleanly.

**Rule:** Always re-render via the tab click mechanism. Do not try to reuse cached DOM for `#ratingsTab` — the complexity causes regressions that are harder to diagnose than a loading flash.

## 9. Duplicate hashchange listeners cause confusion

**Bug:** Two hashchange listeners existed (v1.12.3.5 cleanup).

**Cause:** Code was added incrementally. Two separate listeners handled different aspects of hashchange, making behavior hard to predict.

**Fix:** Merged into one listener with clear sections: UI removal → tab restore → state reset → re-injection scheduling.

**Rule:** One hashchange listener, one MutationObserver per concern. If you need multiple reactions to the same event, combine them in one handler with clear section comments.

## 10. `data-dom-cache` is inert — don't rely on it

**Bug:** Misunderstanding of caching mechanism led to wrong fixes early in the bug cycle.

**Cause:** `data-dom-cache="true"` on `#indexPage` was assumed to control caching behavior. It does not — the real cache is a URL-based ring buffer in `viewContainer` module. The attribute is a legacy Emby marker with 0 references in current Jellyfin JS.

**Rule:** Never write code that reads `data-dom-cache` or assumes it controls caching. The ring cache is driven by URL string matching in `p[]` array, not by DOM attributes.

## Summary Checklist

Before changing navigation/tab/injection code, verify:
- [ ] All `querySelector` calls for containers use `findVisible()` or equivalent
- [ ] No `e.preventDefault()` on native Jellyfin tab buttons
- [ ] No `.hide` toggling on Jellyfin-owned page elements
- [ ] `isInjecting` is not reset while `createRatingsUI` is in flight
- [ ] Scroll code uses `window.scrollY` / `window.scrollTo()`, not `element.scrollTop`
- [ ] `displayRatingsList()` removes all `#ratingsTab` copies before creating fresh
- [ ] Tab click skip guard checks both button active AND content visible in `#indexPage:not(.hide)`
- [ ] No attempt to reuse cached `#ratingsTab` DOM (always re-render via click)
- [ ] One hashchange listener, one MutationObserver per concern
- [ ] No code reads `data-dom-cache` attribute