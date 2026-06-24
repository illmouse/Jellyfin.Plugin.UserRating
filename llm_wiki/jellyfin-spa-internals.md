# Jellyfin SPA Navigation Internals

Reference document for understanding Jellyfin's client-side navigation, view caching, and tab system. Read this before changing any code that interacts with Jellyfin's page DOM, tab system, or navigation flow.

## View Ring Cache (3 slots)

Jellyfin's SPA router uses an in-memory ring cache of **exactly 3 view slots**. This is the `viewContainer` module (module ID `64963` in `main.jellyfin.bundle.js`).

### Data structures

```js
var f = [],     // view elements (DOM nodes), max 3
    p = [],     // parallel URL strings, max 3
    m = -1;     // current slot index (-1 = none)
```

### Key operations

- **`loadView(e)`** — stores a new view in the next slot (`m + 1`, wraps at 3). Evicts the old occupant via `viewdestroy` event + `replaceChild`/`removeChild`. Adds `.mainAnimatedPage` class.
- **`tryRestoreView(e)`** — looks up URL in `p[]` via `p.indexOf(url)`. If found, removes `.hide` from the cached element and re-shows it. If not found, returns `Promise.reject()` and the caller loads a fresh view.
- **`reset()`** — clears all slots, empties `.mainAnimatedPages` container, resets `m = -1`. Called on `skinunload`.

### `.hide` class

The `.hide` class is applied/removed by the view manager JS (`classList.add("hide")` / `classList.remove("hide")`). The actual `display:none` rule for `.hide` is **not in any captured CSS file** — it is injected at runtime or comes from an uncaptured asset. The view manager helper functions:

```js
function u(views, keep1, keep2) {
    // hide all slots except keep1 and keep2
    for (var r = 0; r < views.length; r++)
        if (r !== keep1 && r !== keep2) views[r].classList.add("hide");
}
function c(views, keep) {
    // hide all slots except keep
    for (var n = 0; n < views.length; n++)
        if (n !== keep) views[n].classList.add("hide");
}
```

## `data-dom-cache` is inert

The `data-dom-cache="true"` attribute appears on `#indexPage` in the DOM but is **never read by current Jellyfin JS** (0 matches for `dom-cache`/`domCache` in any bundle). The ring cache is driven entirely by URL string matching in `p[]`. The attribute is a legacy Emby marker — do not rely on it for anything.

## DOM Structure

### Container hierarchy

```
.mainAnimatedPages (plural, container — queried by viewContainer)
  └── .mainAnimatedPage (singular, each cached view)
      ├── #indexPage (home page — pageWithAbsoluteTabs)
      │   ├── .tabContent.pageTabContent#homeTab (data-index="0")
      │   ├── .tabContent.pageTabContent#favoritesTab (data-index="1")
      │   └── .tabContent.pageTabContent#ratingsTab (data-index="2", plugin-injected)
      └── #itemDetailPage (detail page — itemDetailPage class)
          └── .detailPagePrimaryContent
              └── .detailSection (multiple — main, cast, more like this)
```

### Tab buttons (in persistent header, not inside pages)

Tab buttons live in `skinHeader` which is **persistent across navigation** — it is not cached/evicted:

```html
<div class="skinHeader ...">
  <div class="emby-tabs-slider">
    <button class="emby-tab-button emby-button" data-index="0">Home</button>
    <button class="emby-tab-button emby-button" data-index="1">Favorites</button>
    <button class="emby-tab-button emby-button emby-tab-button-active" data-index="2" data-ratings-tab="true">User Ratings</button>
  </div>
</div>
```

Tab **content** lives inside `#indexPage` (recreated/cached with the page). Tab **buttons** live in `skinHeader` (persistent). This separation is critical — buttons survive cache eviction, content does not.

## CSS Rules for Tab Visibility

### The critical rule (main.css)

```css
.pageTabContent:not(.is-active) {
    display: none !important;
}
```

This is the rule that actually governs Jellyfin tab visibility. The `!important` overrides the weaker base rule below.

### Base rule (home.css / page-specific CSS)

```css
.tabContent:not(.is-active) {
    display: none;
}
```

No `!important` — overridden by the `.pageTabContent` rule above.

### Absolute tab positioning (for `pageWithAbsoluteTabs` pages like `#indexPage`)

```css
.absolutePageTabContent {
    bottom: 0; left: 0; margin: 0 !important;
    position: absolute; right: 0; top: 6.9em !important;
    transition: transform .2s ease-out;
    z-index: 1;
}
/* Wide screens */
.absolutePageTabContent { top: 5.7em !important; }

.pageWithAbsoluteTabs:not(.noSecondaryNavPage) {
    padding-top: 6.7em !important;
}
```

### Page positioning (scroll container)

```css
.mainAnimatedPage {
    bottom: 0; contain: layout style size;
    left: 0; position: absolute; right: 0; top: 0;
}
```

`#indexPage` is `position:absolute` filling the viewport. **No `overflow-y:auto`** is set on `.mainAnimatedPage`, `.pageTabContent`, or any tab container. Content scrolls on the **window/document**, not on any inner container.

### Padding for scroll room

```css
.content-primary, .padded-bottom-page, .page,
.pageWithAbsoluteTabs .pageTabContent {
    padding-bottom: 5em !important;
    padding-bottom: calc(env(safe-area-inset-bottom) + 5em) !important;
}
```

## Scroll Behavior

**Jellyfin has no scroll save/restore code.** No `saveScroll`, `restoreScroll`, `scrollRestoration`, or `history.state` scroll coordinates exist in any JS bundle. The only `scroll*` references are:

- A generic focus/smooth-scroll helper (for scrolling list items into view on focus)
- `scrollY: false` passed to `createDialog()` for popup sizing

When a cached view is restored via `tryRestoreView`, the DOM node is reused as-is. The browser may or may not preserve scroll position on a hidden-then-shown element. Since `.hide` likely sets `display:none` (collapsing the element), scroll position typically resets to 0.

**Bottom line:** Jellyfin native tabs also don't restore scroll position. Back-navigation lands at whatever the browser decides — usually top.

## Duplicate Cached Pages (the ghost page bug source)

During home → details → back navigation, Jellyfin's ring cache can create **duplicate `#indexPage` elements** in `.mainAnimatedPages`:

```html
<div class="mainAnimatedPages skinBody">
  <div id="indexPage" class="... mainAnimatedPage">          <!-- visible, no .hide -->
    <div id="ratingsTab" class="tabContent pageTabContent is-active">...</div>
  </div>
  <div id="indexPage" class="... mainAnimatedPage hide">       <!-- hidden cached copy -->
    <div id="ratingsTab" class="tabContent pageTabContent is-active">...</div>
  </div>
</div>
```

Both have `data-dom-cache="true"`, both have `#ratingsTab.is-active`. The `.hide` copy is invisible but still in the DOM. This is why:
- `document.querySelector('#ratingsTab')` may return the wrong (hidden) copy
- `document.querySelector('.detailPagePrimaryContent .detailSection')` may return a section inside a hidden `#itemDetailPage`
- Any `querySelector` without visibility filtering is dangerous during navigation

## Key Takeaways for Plugin Code

1. **Never use `querySelector` for container lookup without visibility filtering** — walk the parent chain to check for `.hide`
2. **Tab buttons persist in `skinHeader`** — they survive cache eviction; tab content does not
3. **`#ratingsTab` content lives inside `#indexPage`** — it gets cached/evicted/recreated with the page
4. **Scroll container is the window** — not `#ratingsTab` or any `.pageTabContent`
5. **`data-dom-cache` is inert** — the real cache is URL-based ring buffer, not this attribute
6. **3-slot limit** — deep navigation (4+ hops) evicts the oldest cached view, forcing recreation on return
7. **`.hide` class hides cached views** — always check for `.hide` ancestors when querying the DOM