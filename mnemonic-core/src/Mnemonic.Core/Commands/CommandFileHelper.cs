using System.Diagnostics;
using System.Text.Json;
using Mnemonic.Ipc;

namespace Mnemonic.Commands;

internal enum CommandConsumeResult
{
    NotPresent,
    IoError,
    InvalidJson,
    HandlerFailed,
    Success,
}

internal static class CommandFileHelper
{
    public static CommandConsumeResult TryConsume<T>(string path, string commandName, Action handler)
        where T : class
    {
        if (!File.Exists(path))
        {
            return CommandConsumeResult.NotPresent;
        }

        try
        {
            var json = File.ReadAllText(path);
            JsonSerializer.Deserialize<T>(json, JsonOptions.Shared);
        }
        catch (JsonException ex)
        {
            Trace.WriteLine($"Mnemonic: {commandName} invalid json: {ex.Message}");
            TryDelete(path);
            return CommandConsumeResult.InvalidJson;
        }
        catch (IOException ex)
        {
            Trace.WriteLine($"Mnemonic: {commandName} read failed: {ex.Message}");
            return CommandConsumeResult.IoError;
        }

        try
        {
            handler();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Mnemonic: {commandName} handler failed: {ex}");
            TryDelete(path);
            return CommandConsumeResult.HandlerFailed;
        }

        TryDelete(path);
        return CommandConsumeResult.Success;
    }

    public static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best effort.
        }
    }
}
