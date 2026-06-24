# Rating Database Architecture

Reference document for the server-side data layer. Read this before modifying `Data/RatingRepository.cs`, `Services/RatingResolver.cs`, `Services/HealthCheckService.cs`, or `Data/BackupService.cs`.

## Storage

- **Format:** Single JSON file at `{PluginConfigurationsPath}/UserRatings/ratings.json`
- **Structure:** `_metadata` property (plugin version, install history, migration records) + one property per rating keyed as `{ItemId}_{UserId}`
- **Loading:** Entire file loaded into memory (`_ratings: Dictionary<string, UserRating>`) on startup. All reads are in-memory. All mutations rewrite the entire file.
- **No partial writes:** `SaveRatings()` serializes the entire `_ratings` dict + `_metadata` via `Utf8JsonWriter` into a `MemoryStream`, then `File.WriteAllBytes`. There is no append or patch.

## Lock Pattern

All reads and mutations of `_ratings`, `_providerIndex`, and `_averagesByItem` are guarded by `lock (_lock)`. Every public method acquires the lock. There is no lock-free path.

The lock is held during file I/O in `SaveRatings()` (line 267) and `LoadRatings()` (line 72). This is acceptable because:
- Reads are fast (in-memory dict lookups)
- Writes are infrequent (user rates an item)
- No high-concurrency scenario (single server, not clustered)

## Per-Entry Deserialization (fail-safe load)

`LoadRatings()` (line 70) reads the file as `Dictionary<string, JsonElement>` and deserializes each entry individually in a loop (lines 118–150). On `JsonException` for one entry:
- Logs a warning with the malformed key
- Increments `skipped` counter
- Continues to the next entry

One malformed entry does **not** abort the load. Only a whole-file parse failure triggers the corruption handler.

## `_loadFailed` Guard

`SaveRatings()` (line 259) checks `if (_loadFailed && _ratings.Count == 0)` before writing. If the load failed and the in-memory DB is empty, it refuses to write — preventing an empty DB from destroying a corrupted-but-recoverable file on disk.

The flag is cleared on successful `LoadRatings()` (line 153).

## Corruption Handler

On any exception during `LoadRatings()` (lines 167–188):
1. Logs the error
2. Copies the corrupt file to `ratings.json.corrupt.{yyyyMMddHHmmss}` (backup before starting empty)
3. Empties `_ratings` and `_averagesByItem`
4. Sets `_loadFailed = true`

This ensures the plugin starts in a safe empty state rather than crashing, while preserving the corrupt file for manual recovery.

## Secondary Indexes

### Provider Index (`_providerIndex`)

- **Type:** `Dictionary<(string provider, string id, Guid userId), string>` — maps `(normalized provider name, normalized provider ID, user ID)` to rating key `{ItemId}_{UserId}`
- **Purpose:** Self-healing lookup — find a rating by provider IDs (Imdb/Tmdb/Tvdb) when the ItemId has changed
- **Built:** `RebuildProviderIndex()` after load (line 162) and after migration (line 352)
- **Maintained:** `IndexProviderIds` / `UnindexProviderIds` called on every SaveRating, DeleteRating, BulkSaveRatings, RepairRatingKey
- **Normalized:** Provider name and ID are lowercased via `NormalizeProviderKey` (line 232)

### Averages Index (`_averagesByItem`)

- **Type:** `Dictionary<Guid, (double Sum, int Count)>` — running sum and count per ItemId
- **Purpose:** O(K) batch average lookups for card decoration (instead of scanning all ratings)
- **Built:** `RebuildAveragesIndex()` after load (line 163) and after migration (line 352)
- **Maintained:** `IndexAdd` / `IndexRemove` called on every SaveRating, DeleteRating, BulkSaveRatings, RepairRatingKey, DeleteAllRatings
- **Read by:** `GetAllRatedItems()` (line 467), `GetBatchAverages()` (line 491)
- **Note:** `GetStatsForItem()` (line 440) does NOT use the index — it recomputes via LINQ. This is acceptable because it's called per-item (detail page), not in batch.

## Self-Healing Lookup

Two-tier resolution in `RatingResolver` + `RatingRepository`:

1. **ItemId match:** `repository.GetRating(itemId, userId)` — direct dict lookup by `{ItemId}_{UserId}`
2. **Provider ID match:** `repository.FindByProviderIds(userId, providerIds)` — looks up each provider ID in `_providerIndex`, returns first match
3. **Heal:** `repository.RepairRatingKey(oldItemId, newItemId, userId)` — removes old key, inserts under new ItemId, re-indexes provider IDs and averages

The healing is performed by `HealthCheckService.RunHealthCheck(heal: true)`:
- Categorizes each rating as `Ok`, `Recoverable` (provider match found, not healed), `Healed` (provider match found + re-keyed), or `Stale` (no match)
- When `heal=true`: calls `RepairRatingKey` to re-key the rating under the correct ItemId
- Can be triggered from admin config page or scheduled task

## Backup Service

- **Location:** `{config.BackupPath}` or default `{ApplicationPaths.DataPath}/backups/UserRatings`
- **Filename:** `ratings_{yyyyMMdd_HHmmss}.json` (UTC, second precision)
- **Method:** `File.Copy` of `ratings.json` — no JSON transformation
- **Rotation:** `RotateBackups(backupDir, maxBackups)` — keeps newest `maxBackups` (default 7) `ratings_*.json` files, deletes older ones. Only `ratings_*` files are counted — `pre_restore_*` and `uploaded_*` are excluded from rotation.
- **Restore:** `RestoreBackup(fileName)` — copies current `ratings.json` to `pre_restore_{timestamp}.json` first (safety), then overwrites from backup, then `repository.Reload()`
- **Path traversal guard:** `GetBackupFilePath` rejects names containing `..`, `/`, `\` and verifies canonical path equals backup dir

## Conflict Modes (BulkSaveRatings)

Used by Plex import. Passed through from `PlexImportService`:

| Mode | Behavior |
|---|---|
| `skip` | If key exists, skip and count as `skipped`. Default. |
| `overwrite` | Always replace existing. Count as `overwritten`. |
| `keepHigher` | Replace only if new rating > existing. Count both `imported` and `overwritten`. |
| default | Falls through to `skip` behavior |

## API Endpoints (RatingsController)

All under `api/UserRatings`:

| Method | Route | Purpose |
|---|---|---|
| POST | `Rate` | Save/update a rating (validates 1–10) |
| GET | `Item/{itemId}` | All ratings for an item + stats |
| GET | `User/{userId}` | All ratings by a user |
| DELETE | `Rating` | Delete a user's rating for an item |
| GET | `MyRating/{itemId}` | Current user's rating for an item |
| DELETE | `DeleteAll` | Delete all ratings (nuclear option) |
| GET | `AllRatedItems` | Server-paginated rated items (offset/limit/sort/filter) |
| GET | `UnratedWatchedItems` | Watched-but-unrated items (movies + series) |
| GET | `MigrationStatus` | Version history + migration records + above-5 ratings |
| POST | `MigrateTo10Star` | Migrate old 5-star scale to 10-star (backs up first) |
| POST | `BatchAverage` | Batch average lookup (max 100 IDs) |

## API Endpoints (HealthController)

All under `api/UserRatings`:

| Method | Route | Purpose |
|---|---|---|
| GET | `HealthReport` | DB consistency report (heal=false by default) |
| POST | `HealRatings` | Heal recoverable ratings (re-key by provider ID) |
| DELETE | `ClearStale` | Delete stale ratings (no library match) |
| POST | `Backup` | Create a backup |
| GET | `Backups` | List all backup files |
| POST | `RestoreBackup` | Restore from a backup file |
| GET | `DownloadBackup` | Download a backup file |
| POST | `UploadBackup` | Upload a backup file |
| DELETE | `DeleteBackup` | Delete a backup file |

## Data Models

### UserRating (record)
- `Guid ItemId`, `Guid UserId`, `int Rating` (1–10), `string? Note`, `DateTime Timestamp`, `string? UserName`, `Dictionary<string,string> ProviderIds`

### HealthReport (record)
- `int Ok/Recoverable/Healed/Updated/Stale` + lists of `StaleItem`, `RecoverableItem`, `HealedItem`

### PluginMetadata (class, stored as `_metadata` in JSON)
- `string CurrentVersion`, `DateTime? CurrentVersionInstalledAt`, `List<VersionEntry> VersionHistory`, `List<MigrationRecord> Migrations`
- `VersionHistoryConverter` is legacy-format tolerant: reads entries as bare strings or objects, always writes object format