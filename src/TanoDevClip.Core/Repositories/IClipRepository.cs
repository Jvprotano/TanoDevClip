using TanoDevClip.Core.Clipboard;

namespace TanoDevClip.Core.Repositories;

public interface IClipRepository
{
    Task SaveAsync(ClipItem clip, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClipItem>> SearchAsync(
        ClipSearchFilter filter,
        CancellationToken cancellationToken = default);

    Task<ClipItem?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task TogglePinAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task IncrementUseAsync(
        string id,
        CancellationToken cancellationToken = default);
}
