namespace Mnemonic.Ipc;

public sealed class DataRootLayout
{
    public void EnsureExists(DataRootPaths paths)
    {
        Directory.CreateDirectory(paths.ScratchDir);
        Directory.CreateDirectory(paths.ClipsDir);
        Directory.CreateDirectory(paths.EventsDir);
        Directory.CreateDirectory(paths.ControlDir);
        Directory.CreateDirectory(paths.CommandsDir);
        Directory.CreateDirectory(paths.LogsDir);

        if (!File.Exists(paths.SessionEventsFile))
        {
            File.WriteAllText(paths.SessionEventsFile, "");
        }
    }
}
