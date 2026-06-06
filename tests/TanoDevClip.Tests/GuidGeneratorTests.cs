using TanoDevClip.DevTools;

namespace TanoDevClip.Tests;

public sealed class GuidGeneratorTests
{
    [Fact]
    public void Should_generate_default_guid()
    {
        var generator = new GuidGenerator();

        var value = generator.Generate();

        Assert.True(Guid.TryParse(value, out _));
        Assert.Contains("-", value);
    }

    [Fact]
    public void Should_generate_guid_without_hyphens()
    {
        var generator = new GuidGenerator();

        var value = generator.Generate(GuidFormat.NoHyphens);

        Assert.Equal(32, value.Length);
        Assert.DoesNotContain("-", value);
        Assert.True(Guid.TryParse(value, out _));
    }

    [Fact]
    public void Should_generate_uppercase_guid()
    {
        var generator = new GuidGenerator();

        var value = generator.Generate(GuidFormat.Uppercase);

        Assert.True(Guid.TryParse(value, out _));
        Assert.Equal(value.ToUpperInvariant(), value);
    }
}
