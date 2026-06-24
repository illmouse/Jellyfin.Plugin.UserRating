# Plex Import Architecture

Reference document for the Plex import/sync subsystem. Read this before modifying `Services/PlexImportService.cs`, `Services/TokenEncryption.cs`, `Services/PlexModels.cs`, `Services/ProgressTracker.cs`, `Api/PlexImportController.cs`, or `ScheduledTasks/PlexSyncScheduledTask.cs`.

## Token Encryption

`Services/TokenEncryption.cs` — static class, AES-256 symmetric encryption.

### Key derivation
- **Salt:** hardcoded UTF-8 bytes of `"Jellyfin.Plugin.UserRatings.TokenEncryption.v1"` (static, app-wide)
- **Key:** `Rfc2898DeriveBytes.Pbkdf2(machineId, Salt, 100_000, SHA256, 32)` → 256-bit AES key
- **Machine identifier:** reads `/etc/machine-id` → falls back to `/var/lib/dbus/machine-id` → falls back to `Environment.MachineName`
- **Cached:** `Lazy<byte[]>` — computed once per process lifetime

### Encrypt/Decrypt
- **Encrypt:** random IV per call (`aes.GenerateIV()`), output = `IV (16 bytes) || ciphertext`, base64-encoded
- **Decrypt:** base64-decode, split first 16 bytes as IV, rest as ciphertext, decrypt. Any exception → returns `string.Empty` (silent failure)
- **No per-token salt:** randomness comes from per-call IV only

### Important implications
- **Tokens are host-bound:** encrypted on machine A, cannot be decrypted on machine B (different machine-id → different key → silent decrypt to empty)
- **Silent failure on corruption:** a corrupted token yields empty string, treated as "not configured" — no exception propagated to callers
- **Encryption boundary:** `PluginConfiguration.PlexToken` getter (line 39) is the ONLY place decryption happens. All services read `config.PlexToken` and get plaintext. The service never sees the encrypted form.

## Import Flow

`PlexImportService.ImportFromPlexAsync(userId, operationId, CancellationToken, conflictMode)` (line 34):

1. **Setup:** read config, trim Plex URL, resolve conflict mode (`conflictMode ?? config.PlexImportConflictMode ?? "skip"`), check `EnablePlexWatchHistorySync`
2. **Fetch:** `GetPlexLibrariesAsync` → split movie/show libraries → fetch items from each into `allPlexItems`
3. **Ratings pass:** filter to rated non-episode items → per item: resolve to Jellyfin via provider IDs → build `UserRating` list → `repository.BulkSaveRatings(ratings, conflictMode)`
4. **Watch history pass** (if enabled): filter to `ViewCount > 0` → per item: resolve to Jellyfin → `MarkItemAsPlayed` (sets `Played=true`, `LastPlayedDate`, `PlayCount`)
5. **Result:** build `ImportResult`, call `progressTracker.CompleteOperation`

### Provider ID Extraction

From Plex XML responses (`ParsePlexXml`, line 297):
- Each `Video`/`Show`/`Episode` element has `<Guid id="...">` children
- Also parses legacy `guid` attribute (e.g., `imdb://tt1234567`, `tmdb://1234`, `tvdb://7890`) via `ParseLegacyGuid` (line 353)
- Dedup: only adds a `PlexGuid` if that provider isn't already present

### Provider ID Mapping

`PlexGuid.JellyfinProviderKey` maps Plex schemes to Jellyfin provider keys:

| Plex scheme | Jellyfin provider key |
|---|---|
| `imdb://` | `Imdb` |
| `tmdb://` | `Tmdb` |
| `tvdb://` | `Tvdb` |
| anything else | `""` (skipped) |

### Jellyfin Resolution Order

`ResolvePlexItemToJellyfin` (line 402) tries in fixed order:
1. **Imdb** → `FindByProviderId("Imdb", id)` via `InternalItemsQuery.HasAnyProviderId`
2. **Tmdb** → `FindByProviderId("Tmdb", id)`
3. **Tvdb** → `FindByProviderId("Tvdb", id)`

First match wins. Returns null if all miss.

### Rating Conversion

`ConvertRating(double plexRating)` (line 507):
- `Math.Round(plexRating, MidpointRounding.AwayFromZero)` clamped to `[1, 10]`
- **Floors at 1, not 0** — a Plex rating of 0 becomes 1 after conversion
- Note: `UserRating > 0` filter (line 85) means unrated Plex items are skipped before this function is called

### MarkItemAsPlayed

`MarkItemAsPlayed` (line 466):
- Fetches user + item via `userManager`/`libraryManager`
- Loads `userDataManager.GetUserData(userId, item)`
- Sets `Played = true`, `LastPlayedDate` from Plex `lastViewedAt` (Unix seconds → UTC) or `DateTime.UtcNow`
- `PlayCount = Math.Max(existing.PlayCount, plexItem.ViewCount)`
- `SaveUserData(..., UserDataSaveReason.Import, ...)`

## Progress Tracking

`Services/ProgressTracker.cs` — `ConcurrentDictionary<string, ImportProgress>`.

| Method | Purpose |
|---|---|
| `StartOperation()` | Returns new GUID-N op ID, creates `ImportProgress` with `status="running"` |
| `GetProgress(opId)` | Returns current progress snapshot |
| `UpdateProgress(opId, Action<ImportProgress>)` | Mutate progress in place |
| `CompleteOperation(opId, ImportResult)` | Sets terminal status, copies result fields |
| `FailOperation(opId, errorMessage)` | Sets `status="failed"` + error message |
| `RemoveOperation(opId)` | Called by SSE consumer when terminal status reached |

### SSE Streaming

`PlexImportController.StreamProgress` (line 59):
- Content-Type: `text/event-stream`
- `Cache-Control: no-cache`, `Connection: keep-alive`
- Polls `progressTracker.GetProgress` every 500ms
- Writes `data: {json}\n\n` each poll
- On terminal status (`completed`/`failed`/`cancelled`): writes final event, calls `RemoveOperation`, breaks

### Fire-and-Forget Pattern

`StartImport` (line 26):
- Creates operation via `progressTracker.StartOperation()`
- Launches `_ = Task.Run(async () => await importService.ImportFromPlexAsync(...))` — fire-and-forget
- Returns immediately with `StartImportResponse(true, operationId)`
- Client polls progress via SSE

**Note:** Passes `CancellationToken.None` to the background task. Cancellation is only checked inside the service via the same token (lines 98, 183). The import is not cancellable from the HTTP request.

## Conflict Modes

Delegated entirely to `repository.BulkSaveRatings(ratings, conflictMode)`. The service does not implement its own conflict logic.

| Mode | Config field | Default |
|---|---|---|
| Manual import | `conflictMode` query param | `config.PlexImportConflictMode` |
| Scheduled sync | `config.SyncConflictMode` | `"skip"` |

See `rating-database.md` for conflict mode behavior details.

## Scheduled Sync

`ScheduledTasks/PlexSyncScheduledTask.cs`:
- Runs on interval defined by `config.SyncIntervalHours` (default 24)
- Only runs if `config.EnableAutoSync` is true
- Uses `config.SyncUserId` to determine which Jellyfin user's ratings to sync to
- Uses `config.SyncConflictMode` (separate from manual import's conflict mode)

## Config Fields (PluginConfiguration.cs)

| Field | Default | Purpose |
|---|---|---|
| `PlexServerUrl` | `""` | Plex server URL |
| `EncryptedPlexToken` | `""` | Encrypted token (persisted) |
| `PlexToken` (getter) | — | Decrypts `EncryptedPlexToken` on access |
| `NewPlexToken` | `""` | Transient field for config page to receive new plaintext token |
| `PlexImportConflictMode` | `"skip"` | Conflict mode for manual import |
| `SyncConflictMode` | `"skip"` | Conflict mode for scheduled sync |
| `EnablePlexWatchHistorySync` | `false` | Also sync watch history (mark items as played) |
| `EnableAutoSync` | `false` | Enable scheduled sync |
| `SyncIntervalHours` | `24` | Sync interval |
| `SyncUserId` | `""` | Jellyfin user ID for scheduled sync |

## API Endpoints (PlexImportController)

All under `api/UserRatings`:

| Method | Route | Purpose |
|---|---|---|
| POST | `ImportFromPlex` | Start import (fire-and-forget, returns operation ID) |
| GET | `ImportProgress/{operationId}` | SSE stream of progress updates |
| GET | `PlexStatus` | Check Plex connection status |

## Key Takeaways

1. **Token encryption is transparent to services** — always read `config.PlexToken`, never `config.EncryptedPlexToken`
2. **Tokens are host-bound** — not portable across machines; silent decrypt-to-empty on mismatch
3. **Provider ID order is fixed** — Imdb → Tmdb → Tvdb, first match wins
4. **Import is fire-and-forget** — HTTP request returns immediately, progress via SSE polling
5. **Cancellation is check-only** — `CancellationToken.None` passed to background task; service checks cancellation at loop boundaries
6. **Conflict modes are separate** — manual import uses `PlexImportConflictMode`, scheduled sync uses `SyncConflictMode`
7. **Rating conversion floors at 1** — `ConvertRating` clamps to `[1, 10]`, not `[0, 10]`
8. **Unrated items are skipped** — `UserRating > 0` filter before conversion; no zero ratings imported