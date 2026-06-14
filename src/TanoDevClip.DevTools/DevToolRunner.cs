using TanoDevClip.DevTools.Tools;

namespace TanoDevClip.DevTools
{
    public sealed class DevToolRunner(GuidGenerator guidGenerator)
    {
        public DevToolResult Run(DevToolRequest request)
        {
            try
            {
                return request.Tool switch
                {
                    "guid" => guidGenerator.GenerateResult(ParseGuidFormat(request.Format)),
                    "cpf" => request.Action == "validate"
                        ? CpfTool.Validate(request.Input)
                        : CpfTool.Generate(request.Formatted ?? true),
                    "cnpj" => request.Action == "validate"
                        ? CnpjTool.Validate(request.Input)
                        : CnpjTool.Generate(request.Formatted ?? true),
                    "lorem" => LoremGenerator.GenerateByCharactersResult(Clamp(request.Amount, 1, 5000, 180)),
                    "string" => StringGenerator.GenerateResult(
                        Clamp(request.Length, 1, 512, 32),
                        request.IncludeUppercase ?? true,
                        request.IncludeLowercase ?? true,
                        request.IncludeNumbers ?? true,
                        request.IncludeSymbols ?? false),
                    "jwt" => JwtDecoder.Decode(request.Input),
                    "json" => JsonFormatter.Format(request.Input, request.Action == "minify"),
                    "base64" => request.Action == "decode"
                        ? Base64Tool.Decode(request.Input)
                        : Base64Tool.Encode(request.Input),
                    "url" => request.Action == "decode"
                        ? UrlTool.Decode(request.Input)
                        : UrlTool.Encode(request.Input),
                    "regex" => RegexTool.Evaluate(
                        request.Pattern,
                        request.Flags,
                        request.Sample,
                        request.Replacement),
                    _ => DevToolResult.Error("Unknown dev tool.")
                };
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return DevToolResult.Error(exception.Message);
            }
        }

        private static GuidFormat ParseGuidFormat(string? value)
        {
            return value switch
            {
                "no-hyphens" => GuidFormat.NoHyphens,
                "uppercase" => GuidFormat.Uppercase,
                _ => GuidFormat.Default
            };
        }

        private static int Clamp(int? value, int min, int max, int fallback)
        {
            return Math.Clamp(value ?? fallback, min, max);
        }
    }

    public sealed record DevToolRequest
    {
        public string Tool { get; init; } = string.Empty;
        public string Action { get; init; } = "generate";
        public string? Format { get; init; }
        public string? Input { get; init; }
        public int? Amount { get; init; }
        public int? Length { get; init; }
        public bool? Formatted { get; init; }
        public bool? IncludeUppercase { get; init; }
        public bool? IncludeLowercase { get; init; }
        public bool? IncludeNumbers { get; init; }
        public bool? IncludeSymbols { get; init; }
        public string? Pattern { get; init; }
        public string? Flags { get; init; }
        public string? Sample { get; init; }
        public string? Replacement { get; init; }
    }
}

