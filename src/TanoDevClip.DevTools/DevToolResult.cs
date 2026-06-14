namespace TanoDevClip.DevTools
{
    public sealed record DevToolResult(bool IsSuccess, string Value)
    {
        public static DevToolResult Success(string value)
        {
            return new DevToolResult(true, value);
        }

        public static DevToolResult Error(string value)
        {
            return new DevToolResult(false, value);
        }
    }
}

