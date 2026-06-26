namespace TanoDevClip.Core.Clipboard
{
    public sealed class ClipItem
    {
        public required string Id { get; init; }
        public required string Content { get; init; }
        public required string ContentHash { get; init; }
        public required ClipType ClipType { get; init; }
        public byte[]? BinaryContent { get; init; }
        public byte[]? PreviewContent { get; init; }
        public string? ContentMimeType { get; init; }
        public int? ImageWidth { get; init; }
        public int? ImageHeight { get; init; }

        public string? Title { get; init; }
        public string? SourceApp { get; init; }
        public string? SourceWindowTitle { get; init; }
        public string? SourceUrl { get; init; }

        public bool IsPinned { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? LastUsedAt { get; init; }

        public int UseCount { get; init; }
    }
}
