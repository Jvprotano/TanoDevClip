namespace TanoDevClip.DevTools
{
    public static class StringGenerator
    {
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Numbers = "0123456789";
        private const string Symbols = "!@#$%&*_-+=?";

        public static DevToolResult GenerateResult(
            int length,
            bool includeUppercase = true,
            bool includeLowercase = true,
            bool includeNumbers = true,
            bool includeSymbols = false)
        {
            return DevToolResult.Success(GenerateRandomString(
                length,
                includeUppercase,
                includeLowercase,
                includeNumbers,
                includeSymbols));
        }

        public static string GenerateRandomString(
            int length,
            bool includeUppercase = true,
            bool includeLowercase = true,
            bool includeNumbers = true,
            bool includeSymbols = false)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            var alphabet = BuildAlphabet(
                includeUppercase,
                includeLowercase,
                includeNumbers,
                includeSymbols);

            if (alphabet.Length == 0)
                throw new InvalidOperationException("At least one character group must be selected.");

            return string.Create(length, alphabet, static (buffer, chars) =>
            {
                var random = Random.Shared;

                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = chars[random.Next(chars.Length)];
                }
            });
        }

        private static string BuildAlphabet(
            bool includeUppercase,
            bool includeLowercase,
            bool includeNumbers,
            bool includeSymbols)
        {
            var length = 0;

            if (includeUppercase) length += Uppercase.Length;
            if (includeLowercase) length += Lowercase.Length;
            if (includeNumbers) length += Numbers.Length;
            if (includeSymbols) length += Symbols.Length;

            return string.Create(length, (includeUppercase, includeLowercase, includeNumbers, includeSymbols), static (buffer, state) =>
            {
                var position = 0;

                if (state.includeUppercase)
                    Append(Uppercase, buffer, ref position);

                if (state.includeLowercase)
                    Append(Lowercase, buffer, ref position);

                if (state.includeNumbers)
                    Append(Numbers, buffer, ref position);

                if (state.includeSymbols)
                    Append(Symbols, buffer, ref position);
            });
        }

        private static void Append(string source, Span<char> destination, ref int position)
        {
            source.AsSpan().CopyTo(destination[position..]);
            position += source.Length;
        }
    }
}
