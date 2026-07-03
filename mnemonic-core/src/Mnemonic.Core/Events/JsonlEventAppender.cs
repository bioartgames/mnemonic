namespace Mnemonic.Events;

public static class JsonlEventAppender
{
    private static readonly object AppendLock = new();

    public static void Append(string path, SessionEvent evt)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = SessionEventJson.ToJsonLine(evt) + "\n";
        lock (AppendLock)
        {
            File.AppendAllText(path, line);
        }
    }
}
