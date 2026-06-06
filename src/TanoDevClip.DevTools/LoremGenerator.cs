using System.Text;

public static class LoremGenerator
{
    private static readonly string[] Words =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur",
        "adipiscing", "elit", "sed", "do", "eiusmod", "tempor",
        "incididunt", "ut", "labore", "et", "dolore", "magna",
        "aliqua", "enim", "minim", "veniam", "quis", "nostrud",
        "exercitation", "ullamco", "laboris", "nisi", "aliquip",
        "commodo", "consequat"
    ];

    public static string GenerateByCharacters(int length = 460)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        return string.Create(length, length, static (buffer, _) =>
        {
            var wordIndex = 0;
            var position = 0;

            while (position < buffer.Length)
            {
                var word = Words[wordIndex];

                foreach (var character in word)
                {
                    if (position >= buffer.Length)
                        return;

                    buffer[position++] = character;
                }

                if (position >= buffer.Length)
                    return;

                buffer[position++] = ' ';

                wordIndex++;

                if (wordIndex >= Words.Length)
                    wordIndex = 0;
            }
        }).TrimEnd();
    }

    public static string GenerateByWords(int wordCount = 46)
    {
        if (wordCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(wordCount));

        var estimatedLength = wordCount * 8;
        var builder = new StringBuilder(estimatedLength);

        for (var i = 0; i < wordCount; i++)
        {
            if (i > 0)
                builder.Append(' ');

            builder.Append(Words[i % Words.Length]);
        }

        builder.Append('.');

        builder[0] = char.ToUpperInvariant(builder[0]);

        return builder.ToString();
    }
}