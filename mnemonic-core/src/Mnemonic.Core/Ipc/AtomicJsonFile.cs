using System.Text.Json;
using System.Threading;

namespace Mnemonic.Ipc;

internal static class AtomicJsonFile
{
    private const int WriteRetryMaxAttempts = 6;
    private const int WriteRetryDelayMs = 40;

    public static void Write<T>(string targetPath, T value, JsonSerializerOptions options)
    {
        for (var attempt = 1; attempt <= WriteRetryMaxAttempts; attempt++)
        {
            try
            {
                WriteOnce(targetPath, value, options);
                return;
            }
            catch (UnauthorizedAccessException) when (attempt < WriteRetryMaxAttempts)
            {
                Thread.Sleep(WriteRetryDelayMs);
            }
            catch (IOException) when (attempt < WriteRetryMaxAttempts)
            {
                Thread.Sleep(WriteRetryDelayMs);
            }
        }
    }

    private static void WriteOnce<T>(string targetPath, T value, JsonSerializerOptions options)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = targetPath + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(value, options);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    public static T? Read<T>(string path, JsonSerializerOptions options)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, options);
    }
}
