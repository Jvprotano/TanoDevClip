namespace TanoDevClip.DevTools
{
    public static class CpfTool
    {
        public static DevToolResult Generate(bool formatted)
        {
            List<int> digits;
            string value;

            do
            {
                digits = Enumerable.Range(0, 9)
                    .Select(_ => Random.Shared.Next(0, 10))
                    .ToList();

                digits.Add(CalculateDigit(digits, 10));
                digits.Add(CalculateDigit(digits, 11));
                value = string.Concat(digits);
            }
            while (HasSameDigit(value));

            return DevToolResult.Success(formatted ? Format(value) : value);
        }

        public static DevToolResult Validate(string? value)
        {
            var digits = OnlyDigits(value);
            var isValid =
                digits.Length == 11 &&
                !HasSameDigit(digits) &&
                CalculateDigit(ToDigits(digits[..9]), 10) == digits[9] - '0' &&
                CalculateDigit(ToDigits(digits[..10]), 11) == digits[10] - '0';

            var normalized = digits.Length == 11 ? Format(digits) : digits;
            return isValid
                ? DevToolResult.Success($"valid{Environment.NewLine}{normalized}")
                : DevToolResult.Error($"invalid{Environment.NewLine}{(string.IsNullOrWhiteSpace(normalized) ? "CPF is empty" : normalized)}");
        }

        private static int CalculateDigit(IReadOnlyList<int> digits, int factor)
        {
            var total = digits.Select((digit, index) => digit * (factor - index)).Sum();
            var remainder = total * 10 % 11;
            return remainder == 10 ? 0 : remainder;
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
            return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
        }
    }
}

