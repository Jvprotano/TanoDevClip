namespace TanoDevClip.DevTools
{
    public static class CnpjTool
    {
        private static readonly int[] FirstDigitWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        private static readonly int[] SecondDigitWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        public static DevToolResult Generate(bool formatted)
        {
            List<int> digits;
            string value;

            do
            {
                digits = Enumerable.Range(0, 12)
                    .Select(_ => Random.Shared.Next(0, 10))
                    .ToList();

                digits.Add(CalculateDigit(digits, FirstDigitWeights));
                digits.Add(CalculateDigit(digits, SecondDigitWeights));
                value = string.Concat(digits);
            }
            while (HasSameDigit(value));

            return DevToolResult.Success(formatted ? Format(value) : value);
        }

        public static DevToolResult Validate(string? value)
        {
            var digits = OnlyDigits(value);
            var isValid =
                digits.Length == 14 &&
                !HasSameDigit(digits) &&
                CalculateDigit(ToDigits(digits[..12]), FirstDigitWeights) == digits[12] - '0' &&
                CalculateDigit(ToDigits(digits[..13]), SecondDigitWeights) == digits[13] - '0';

            var normalized = digits.Length == 14 ? Format(digits) : digits;
            return isValid
                ? DevToolResult.Success($"valid{Environment.NewLine}{normalized}")
                : DevToolResult.Error($"invalid{Environment.NewLine}{(string.IsNullOrWhiteSpace(normalized) ? "CNPJ is empty" : normalized)}");
        }

        private static int CalculateDigit(IReadOnlyList<int> digits, IReadOnlyList<int> weights)
        {
            var total = digits.Select((digit, index) => digit * weights[index]).Sum();
            var remainder = total % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        private static string OnlyDigits(string? value)
        {
            return string.Concat((value ?? string.Empty).Where(char.IsDigit));
        }

        private static List<int> ToDigits(string value)
        {
            return value.Select(digit => digit - '0').ToList();
        }

        private static bool HasSameDigit(string value)
        {
            return value.All(digit => digit == value[0]);
        }

        private static string Format(string digits)
        {
            return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
        }
    }
}

