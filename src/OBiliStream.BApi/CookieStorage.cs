namespace OBiliStream.BApi;

internal static class CookieStorage
{
    public static string GetStoragePath(int mid)
    {
        string? path = Environment.GetEnvironmentVariable("OBILISTREAM_COOKIE");
        if (!string.IsNullOrWhiteSpace(path))
            return path;

        // Windows 标准路径 / XDG 规范路径
        path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        path = Path.Combine(path, "OBiliStream", $"{mid}.cookie");
        return path;
    }
}
