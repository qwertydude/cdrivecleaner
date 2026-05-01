namespace CDriveCleaner.Models;

internal sealed class CleanupAnalysisResult
{
    public required string TargetId { get; init; }

    public bool Success { get; init; }

    public long EstimatedBytes { get; init; }

    public int ItemCount { get; init; }

    public required string Message { get; init; }

    public bool HasKnownEstimate { get; init; } = true;

    public IReadOnlyList<CleanupCandidate> Candidates { get; init; } = Array.Empty<CleanupCandidate>();
}

internal sealed class CleanupActionResult
{
    public required string TargetId { get; init; }

    public bool Success { get; init; }

    public long BytesFreed { get; init; }

    public int ItemsRemoved { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

internal sealed record CleanupCandidate(string Label, string Path, long Bytes);

internal readonly record struct DriveSummary(string Name, long TotalBytes, long UsedBytes, long FreeBytes);
