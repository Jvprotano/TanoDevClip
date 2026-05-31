using TanoDevClip.Core.Classification;
using TanoDevClip.Core.Clipboard;

namespace TanoDevClip.Tests;

public sealed class DefaultClipboardClassifierTests
{
    [Fact]
    public void Should_classify_json()
    {
        var classifier = new DefaultClipboardClassifier();

        var result = classifier.Classify("""{"name":"TanoDev"}""");

        Assert.Equal(ClipType.Json, result);
    }

    [Fact]
    public void Should_classify_sql()
    {
        var classifier = new DefaultClipboardClassifier();

        var result = classifier.Classify("select * from users");

        Assert.Equal(ClipType.Sql, result);
    }

    [Fact]
    public void Should_classify_url()
    {
        var classifier = new DefaultClipboardClassifier();

        var result = classifier.Classify("https://example.com");

        Assert.Equal(ClipType.Url, result);
    }
}