namespace CDriveCleaner.Models;

internal sealed class CleanupExecutionOptions
{
    public bool UseAggressiveMode { get; init; }

    public IReadOnlyList<string> SelectedPaths { get; init; } = Array.Empty<string>();
}
