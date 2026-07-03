namespace Mnemonic.Ipc;

public sealed class DataRootPaths
{
    public DataRootPaths()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Mnemonic Core v1 supports Windows only.");
        }

        Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            MnemonicConstants.DataRootFolderName);
    }

    public DataRootPaths(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public string ScratchDir => Path.Combine(Root, "scratch");

    public string ClipsDir => Path.Combine(Root, "clips");

    public string EventsDir => Path.Combine(Root, "events");

    public string SessionEventsFile => Path.Combine(EventsDir, "session_events.jsonl");

    public string ControlDir => Path.Combine(Root, "control");

    public string StatusFile => Path.Combine(ControlDir, "status.json");

    public string SegmentHistoryFile =>
        Path.Combine(ControlDir, MnemonicConstants.SegmentHistoryFileName);

    public string EditorSceneFile =>
        Path.Combine(ControlDir, MnemonicConstants.EditorSceneFileName);

    public string ClipsIndexFile => Path.Combine(ControlDir, MnemonicConstants.ClipsIndexFileName);

    public string SuggestedGroupsFile =>
        Path.Combine(ControlDir, MnemonicConstants.SuggestedGroupsFileName);

    public string CommandsDir => Path.Combine(ControlDir, "commands");

    public string FlagCurrentFile => Path.Combine(CommandsDir, "flag_current.json");

    public string PauseCaptureFile => Path.Combine(CommandsDir, "pause_capture.json");

    public string ResumeCaptureFile => Path.Combine(CommandsDir, "resume_capture.json");

    public string ExitCoreFile => Path.Combine(CommandsDir, "exit_core.json");

    public string RebuildClipsIndexFile =>
        Path.Combine(CommandsDir, MnemonicConstants.RebuildClipsIndexFileName);

    public string SettingsFile => Path.Combine(Root, "settings.json");

    public string LogsDir => Path.Combine(Root, "logs");
}
