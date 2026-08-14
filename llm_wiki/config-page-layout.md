# Config Page Layout

## Section Order

The plugin config page (`Configuration/configPage.html`) has these sections in order:

1. **Rating Display** — star size, show half stars, badge positions, favorite threshold, Mark Existing Favorites button
2. **Home Dashboard** — recently rated count, dashboard visibility
3. **Card Overlays** — badge visibility, badge position
4. **Plex Import** — server URL, token, test connection
5. **Scheduled Sync** — enable auto sync, sync ratings, sync watch history, sync user, conflict mode, interval, Sync Now button
6. **Database Health** — enable health check, interval, healing conflict mode, Check/Heal buttons, recoverable/healed/stale lists
7. **Save Settings button** — always between the last setting section and Danger Zone
8. **Danger Zone** — delete all ratings, clear stale ratings

## Rules

- **Save Settings button** must always appear between the last setting section and Danger Zone. Do not place it between mid-page sections — users expect it at the bottom of all settings.
- **Action buttons** (Sync Now, Check Health, Heal, Delete) live inside their respective sections, not in a shared action bar.
- **Danger Zone** is always the last section, visually separated with a red header and `border-top`.