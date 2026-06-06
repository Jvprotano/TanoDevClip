namespace TanoDevClip.Core.Clipboard;

public sealed class ClipSearchFilter
{
    public string? Query { get; init; }
    public ClipType? ClipType { get; init; }
    public int Limit { get; init; } = 100;
}