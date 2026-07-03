namespace Mnemonic.Retention;

public sealed class ClipMetadata
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

    public string Notes { get; init; } = "";

    public string? AiSummary { get; init; }

    public IReadOnlyList<string>? AiTopics { get; init; }

    public ClipAssetsMetadata? Assets { get; init; }
}

public sealed class ClipAssetsMetadata
{
    public required string Container { get; init; }

    public required int VideoStreamIndex { get; init; }

    public required IReadOnlyList<ClipAudioTrackMetadata> AudioTracks { get; init; }
}

public sealed class ClipAudioTrackMetadata
{
    public required string Role { get; init; }

    public required int StreamIndex { get; init; }

    public string Codec { get; init; } = "aac";
}
