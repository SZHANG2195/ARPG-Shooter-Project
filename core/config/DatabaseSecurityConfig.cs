namespace lethal.core.config;

public static class DatabaseSecurityConfig
{
    private static readonly string Part1 = "83cdd0ea5c";
    private static readonly string Part2 = "9e589dd7233";
    private static readonly string Part3 = "e032cfeba34";

    public static string GetEncryptionKey()
    {
        return Part1 + Part2 + Part3;
    }
}
