namespace CDriveCleaner.Models;

internal sealed class CleanupTargetState
{
    public CleanupTargetState(CleanupTargetDefinition definition)
    {
        Definition = definition;
    }

    public CleanupTargetDefinition Definition { get; }

    public long EstimatedBytes { get; private set; }

    public string StatusText { get; private set; } = "Not scanned";

    public bool IsRelevant { get; private set; } = true;

    public bool IsSelected { get; set; }

    public bool HasKnownEstimate { get; private set; } = true;

    public IReadOnlyList<CleanupCandidate> Candidates { get; private set; } = Array.Empty<CleanupCandidate>();

    public DateTimeOffset? LastUpdated { get; private set; }

    public void SetRelevance(bool isRelevant)
    {
        IsRelevant = isRelevant;
    }

    public void ApplyAnalysis(CleanupAnalysisResult result)
    {
        EstimatedBytes = result.EstimatedBytes;
        StatusText = result.Message;
        HasKnownEstimate = result.HasKnownEstimate;
        Candidates = result.Candidates;
        LastUpdated = DateTimeOffset.Now;
    }

    public void ApplyCleanup(CleanupActionResult result)
    {
        StatusText = result.Message;
        LastUpdated = DateTimeOffset.Now;
    }
}
