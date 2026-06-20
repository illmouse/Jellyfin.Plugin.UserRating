using System;
using System.Collections.Concurrent;

namespace Jellyfin.Plugin.UserRatings.Services;

public class ProgressTracker
{
    private readonly ConcurrentDictionary<string, ImportProgress> _operations = new();

    public string StartOperation()
    {
        var operationId = Guid.NewGuid().ToString("N");
        var progress = new ImportProgress
        {
            OperationId = operationId,
            Status = "running",
            PercentComplete = 0
        };
        _operations[operationId] = progress;
        return operationId;
    }

    public ImportProgress? GetProgress(string operationId)
    {
        return _operations.TryGetValue(operationId, out var progress) ? progress : null;
    }

    public void UpdateProgress(string operationId, Action<ImportProgress> update)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            update(progress);
        }
    }

    public void CompleteOperation(string operationId, ImportResult result)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            progress.Status = result.Success ? "completed" : "failed";
            progress.PercentComplete = 100;
            progress.ImportedItems = result.Imported;
            progress.SkippedItems = result.Skipped;
            progress.UnmatchedItems = result.Unmatched;
            progress.ErrorMessage = result.Message;
            progress.Unmatched = result.UnmatchedItems;
        }
    }

    public void FailOperation(string operationId, string errorMessage)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            progress.Status = "failed";
            progress.ErrorMessage = errorMessage;
        }
    }

    public void RemoveOperation(string operationId)
    {
        _operations.TryRemove(operationId, out _);
    }
}
