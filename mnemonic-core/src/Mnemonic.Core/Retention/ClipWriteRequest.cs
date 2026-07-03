using Mnemonic.Capture;
using Mnemonic.Events;

namespace Mnemonic.Retention;

public sealed record ClipWriteRequest(
    string CapturePrefix,
    int SegmentIndex,
    double TOpenUnix,
    double TCloseUnix,
    int SegmentDurationSeconds,
    int Score,
    IReadOnlyList<SessionEvent> Events,
    CaptureAudioConfig AudioConfig,
    GitSnapshot Git,
    EditorScenePaths EditorScenePaths = default,
    IReadOnlyList<string> FilesModified = default!);
