using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Jellyfin.Plugin.UserRatings.Services;

public class ProgressTracker
{
    private readonly ConcurrentDictionary<string, ImportProgress> _operations = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationSources = new();

    public string StartOperation()
    {
        var operationId = Guid.NewGuid().ToString("N");
        var progress = new ImportProgress
        {
            OperationId = operationId,
            Status = "running",
            PercentComplete = 0,
            CreatedAt = DateTime.UtcNow
        };
        _operations[operationId] = progress;

        EvictStaleOperations(TimeSpan.FromHours(1));

        return operationId;
    }

    public string StartOperation(CancellationTokenSource cts)
    {
        var operationId = StartOperation();
        _cancellationSources[operationId] = cts;
        return operationId;
    }

    public ImportProgress? GetProgress(string operationId)
    {
        return _operations.TryGetValue(operationId, out var progress) ? progress : null;
    }

    public CancellationToken GetCancellationToken(string operationId)
    {
        return _cancellationSources.TryGetValue(operationId, out var cts) ? cts.Token : CancellationToken.None;
    }

    public bool CancelOperation(string operationId)
    {
        if (_cancellationSources.TryGetValue(operationId, out var cts))
        {
            cts.Cancel();
        }

        if (_operations.TryGetValue(operationId, out var progress))
        {
            _operations[operationId] = progress with { Status = "cancelled", ErrorMessage = "Import cancelled by user" };
            return true;
        }

        return false;
    }

    public void UpdateProgress(string operationId, Func<ImportProgress, ImportProgress> update)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            var updated = update(progress);
            _operations[operationId] = updated;
        }
    }

    public void CompleteOperation(string operationId, ImportResult result)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            _operations[operationId] = progress with
            {
                Status = result.Success ? "completed" : "failed",
                PercentComplete = 100,
                ImportedItems = result.Imported,
                SkippedItems = result.Skipped,
                WatchedItems = result.Watched,
                UnmatchedItems = result.Unmatched,
                ErrorMessage = result.Message,
                Unmatched = result.UnmatchedItems
            };
        }

        if (_cancellationSources.TryRemove(operationId, out var cts))
        {
            cts.Dispose();
        }
    }

    public void FailOperation(string operationId, string errorMessage)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            _operations[operationId] = progress with { Status = "failed", ErrorMessage = errorMessage };
        }

        if (_cancellationSources.TryRemove(operationId, out var cts))
        {
            cts.Dispose();
        }
    }

    public void RemoveOperation(string operationId)
    {
        _operations.TryRemove(operationId, out _);

        if (_cancellationSources.TryRemove(operationId, out var cts))
        {
            cts.Dispose();
        }
    }

    public int EvictStaleOperations(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var staleKeys = _operations
            .Where(kvp =>
            {
                var status = kvp.Value.Status;
                return (status == "completed" || status == "failed" || status == "cancelled")
                    && kvp.Value.CreatedAt < cutoff;
            })
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            RemoveOperation(key);
        }

        return staleKeys.Count;
    }
}