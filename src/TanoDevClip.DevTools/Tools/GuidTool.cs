namespace TanoDevClip.DevTools
{
    public enum GuidFormat
    {
        Default,
        NoHyphens,
        Uppercase
    }

    public sealed class GuidGenerator
    {
        public DevToolResult GenerateResult(GuidFormat format = GuidFormat.Default)
        {
            return DevToolResult.Success(Generate(format));
        }

        public string Generate(GuidFormat format = GuidFormat.Default)
        {
            var value = Guid.NewGuid();

            return format switch
            {
                GuidFormat.NoHyphens => value.ToString("N"),
                GuidFormat.Uppercase => value.ToString("D").ToUpperInvariant(),
                _ => value.ToString("D")
            };
        }
    }
}

