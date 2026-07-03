using System.Text.Json;

namespace Mnemonic.Events;

public static class SessionEventJson
{
    public static SessionEvent CreateGitCommit(double t, string commit, string subject) =>
        new()
        {
            T = t,
            Type = "git_commit",
            Extra = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["commit"] = JsonSerializer.SerializeToElement(commit),
                ["subject"] = JsonSerializer.SerializeToElement(subject),
            },
        };

    public static SessionEvent CreateGitBranchChange(double t, string branch) =>
        new()
        {
            T = t,
            Type = "git_branch_change",
            Extra = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["branch"] = JsonSerializer.SerializeToElement(branch),
            },
        };

    public static SessionEvent CreateGitPush(double t, string branch, string remote) =>
        new()
        {
            T = t,
            Type = "git_push",
            Extra = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["branch"] = JsonSerializer.SerializeToElement(branch),
                ["remote"] = JsonSerializer.SerializeToElement(remote),
            },
        };

    public static SessionEvent CreateDebugSessionStart(double t) =>
        new() { T = t, Type = "debug_session_start" };

    public static SessionEvent CreateDebugSessionStop(double t, double durationSec) =>
        new()
        {
            T = t,
            Type = "debug_session_stop",
            Extra = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["duration_sec"] = JsonSerializer.SerializeToElement(durationSec),
            },
        };

    public static SessionEvent CreateScriptSave(double t, string path) =>
        new()
        {
            T = t,
            Type = "script_save",
            Extra = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["path"] = JsonSerializer.SerializeToElement(path),
            },
        };

    public static SessionEvent CreateEditorFocusedSession(double t, string focus, double durationSec) =>
        new()
        {
            T = t,
            Type = "editor_focused_session",
            Extra = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["focus"] = JsonSerializer.SerializeToElement(focus),
                ["duration_sec"] = JsonSerializer.SerializeToElement(durationSec),
            },
        };

    public static string ToJsonLine(SessionEvent evt)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("t", evt.T);
            writer.WriteString("type", evt.Type);

            if (evt.Extra is not null)
            {
                foreach (var (key, value) in evt.Extra)
                {
                    writer.WritePropertyName(key);
                    value.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
