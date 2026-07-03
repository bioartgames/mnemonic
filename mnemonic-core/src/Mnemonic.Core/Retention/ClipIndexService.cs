using System.Text.Json;
using Mnemonic;
using Mnemonic.Ipc;

namespace Mnemonic.Retention;

public sealed class ClipIndexService
{
    private readonly DataRootPaths _paths;

    public ClipIndexService(DataRootPaths paths)
    {
        _paths = paths;
    }

    public void Rebuild()
    {
        var entries = new List<ClipIndexEntry>();

        if (Directory.Exists(_paths.ClipsDir))
        {
            foreach (var subdirectory in Directory.GetDirectories(_paths.ClipsDir))
            {
                var jsonPath = Path.Combine(subdirectory, "clip.json");
                if (!File.Exists(jsonPath))
                {
                    continue;
                }

                try
                {
                    var meta = AtomicJsonFile.Read<ClipMetadata>(jsonPath, JsonOptions.Shared);
                    if (meta is null)
                    {
                        continue;
                    }

                    entries.Add(ClipIndexEntry.FromMetadata(subdirectory, meta));
                }
                catch (JsonException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        entries.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        var builtAtUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var file = new ClipIndexFile
        {
            IndexVersion = MnemonicConstants.ClipsIndexVersion,
            BuiltAtUnix = builtAtUnix,
            Clips = entries,
        };

        AtomicJsonFile.Write(_paths.ClipsIndexFile, file, JsonOptions.Shared);

        var groups = ClipSessionGrouper.Build(entries, builtAtUnix);
        AtomicJsonFile.Write(_paths.SuggestedGroupsFile, groups, JsonOptions.Shared);
    }
}
