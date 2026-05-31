using TanoDevClip.Core.Clipboard;

namespace TanoDevClip.Core.Classification;

public interface IClipboardClassifier
{
    ClipType Classify(string content);
}