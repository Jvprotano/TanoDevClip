using TanoDevClip.DevTools;

namespace TanoDevClip.Tests;

public sealed class DevToolGeneratorsTests
{
    [Fact]
    public void Should_generate_random_string_with_requested_length()
    {
        var value = StringGenerator.GenerateRandomString(
            length: 24,
            includeUppercase: false,
            includeLowercase: true,
            includeNumbers: false,
            includeSymbols: false);

        Assert.Equal(24, value.Length);
        Assert.All(value, character => Assert.InRange(character, 'a', 'z'));
    }

    [Fact]
    public void Should_reject_random_string_without_character_groups()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StringGenerator.GenerateRandomString(
                length: 12,
                includeUppercase: false,
                includeLowercase: false,
                includeNumbers: false,
                includeSymbols: false));
    }

    [Fact]
    public void Should_generate_lorem_by_characters()
    {
        var value = LoremGenerator.GenerateByCharacters(32);

        Assert.True(value.Length <= 32);
        Assert.StartsWith("lorem", value);
    }

    [Fact]
    public void Should_generate_lorem_by_words()
    {
        var value = LoremGenerator.GenerateByWords(4);

        Assert.Equal("Lorem ipsum dolor sit.", value);
    }
}
