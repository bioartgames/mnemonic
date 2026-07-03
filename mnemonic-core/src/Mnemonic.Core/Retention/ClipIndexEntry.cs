using Mnemonic;

namespace Mnemonic.Retention;

public sealed class ClipIndexEntry
{
    public required string Id { get; init; }

    public required int CreatedAt { get; init; }

    public required int DurationSeconds { get; init; }

    public required int Score { get; init; }

    public required string GitCommit { get; init; }

    public required string GitBranch { get; init; }

    public required string CommitSubject { get; init; }

    public IReadOnlyList<string>? FilesModified { get; init; }

    public required IReadOnlyList<string> ScenesActive { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public string? AiSummary { get; init; }

    public IReadOnlyList<string>? AiTopics { get; init; }

    public required bool HasVideo { get; init; }

    public required bool HasThumb { get; init; }

    public static ClipIndexEntry FromMetadata(string clipDir, ClipMetadata meta)
    {
        var videoPath = Path.Combine(clipDir, MnemonicConstants.ClipVideoFileName);
        var thumbPath = Path.Combine(clipDir, MnemonicConstants.ClipThumbFileName);

        var mergedTags = MergeTags(meta.Tags, ClipSceneTagDeriver.DeriveTags(meta.ScenesActive));

        return new ClipIndexEntry
        {
            Id = meta.Id,
            CreatedAt = meta.CreatedAt,
            DurationSeconds = meta.DurationSeconds,
            Score = meta.Score,
            GitCommit = meta.GitCommit,
            GitBranch = meta.GitBranch,
            CommitSubject = meta.CommitSubject,
            FilesModified = meta.FilesModified,
            ScenesActive = meta.ScenesActive,
            Tags = mergedTags,
            AiSummary = meta.AiSummary,
            AiTopics = meta.AiTopics ?? [],
            HasVideo = File.Exists(videoPath),
            HasThumb = File.Exists(thumbPath),
        };
    }

    private static IReadOnlyList<string> MergeTags(
        IReadOnlyList<string> existing,
        IReadOnlyList<string> sceneDerived)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outList = new List<string>();

        foreach (var tag in existing)
        {
            if (seen.Add(tag))
            {
                outList.Add(tag);
            }
        }

        foreach (var tag in sceneDerived)
        {
            if (seen.Add(tag))
            {
                outList.Add(tag);
            }
        }

        outList.Sort(StringComparer.Ordinal);
        return outList;
    }
}
