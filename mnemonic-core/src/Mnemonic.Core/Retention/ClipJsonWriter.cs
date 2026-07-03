using Mnemonic.Capture;
using Mnemonic.Ipc;

namespace Mnemonic.Retention;

public static class ClipJsonWriter
{
    public static void Write(string clipDir, ClipWriteRequest request)
    {
        var id = ClipIdentity.FormatClipId(request.CapturePrefix, request.SegmentIndex);
        var scenes = ClipSceneExtractor.BuildScenesActive(request.Events, request.EditorScenePaths);
        var metadata = new ClipMetadata
        {
            Id = id,
            CreatedAt = (int)Math.Floor(request.TCloseUnix),
            DurationSeconds = request.SegmentDurationSeconds,
            Score = request.Score,
            GitCommit = request.Git.Commit.Trim(),
            GitBranch = request.Git.Branch.Trim(),
            CommitSubject = request.Git.Subject.Trim(),
            FilesModified = request.FilesModified is { Count: > 0 } ? request.FilesModified : null,
            ScenesActive = scenes,
            Tags = ClipTagBuilder.BuildTags(request.Events, request.Git.Branch, scenes),
            Notes = "",
            Assets = BuildAssets(request.AudioConfig),
        };

        var path = Path.Combine(clipDir, "clip.json");
        AtomicJsonFile.Write(path, metadata, JsonOptions.Shared);
    }

    private static ClipAssetsMetadata? BuildAssets(CaptureAudioConfig audioConfig)
    {
        var descriptors = CaptureAudioLayout.GetTrackDescriptors(audioConfig);
        if (descriptors.Count == 0)
        {
            return null;
        }

        var tracks = descriptors
            .Select(d => new ClipAudioTrackMetadata
            {
                Role = d.Role,
                StreamIndex = d.StreamIndex,
                Codec = d.Codec,
            })
            .ToList();

        return new ClipAssetsMetadata
        {
            Container = MnemonicConstants.ClipVideoFileName,
            VideoStreamIndex = 0,
            AudioTracks = tracks,
        };
    }
}
