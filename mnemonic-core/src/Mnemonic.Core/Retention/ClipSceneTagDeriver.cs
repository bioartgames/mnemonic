namespace Mnemonic.Retention;

public static class ClipSceneTagDeriver
{
    private const int MaxTagsPerClip = 12;
    private const int MinSegmentLength = 2;

    public static IReadOnlyList<string> DeriveTags(IReadOnlyList<string> scenePaths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outList = new List<string>();

        foreach (var path in scenePaths)
        {
            if (outList.Count >= MaxTagsPerClip)
            {
                break;
            }

            AddSegmentsFromPath(path, seen, outList);
        }

        outList.Sort(StringComparer.Ordinal);
        return outList;
    }

    internal static string SlugSegment(string segment)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in segment.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        while (sb.Length > 0 && sb[^1] == '-')
        {
            sb.Length--;
        }

        return sb.ToString();
    }

    private static void AddSegmentsFromPath(string path, HashSet<string> seen, List<string> outList)
    {
        var trimmed = path.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        var withoutScheme = trimmed;
        if (withoutScheme.StartsWith("res://", StringComparison.Ordinal))
        {
            withoutScheme = withoutScheme["res://".Length..];
        }

        var parts = withoutScheme.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var fileName = parts[^1];
        var baseName = fileName;
        var dot = baseName.LastIndexOf('.');
        if (dot > 0)
        {
            baseName = baseName[..dot];
        }

        TryAddSlug(baseName, seen, outList);

        for (var i = 0; i < parts.Length - 1; i++)
        {
            TryAddSlug(parts[i], seen, outList);
        }
    }

    private static void TryAddSlug(string segment, HashSet<string> seen, List<string> outList)
    {
        if (outList.Count >= MaxTagsPerClip)
        {
            return;
        }

        var slug = SlugSegment(segment);
        if (slug.Length < MinSegmentLength)
        {
            return;
        }

        if (MnemonicConstants.SceneTagStopSegments.Contains(slug, StringComparer.Ordinal))
        {
            return;
        }

        if (seen.Add(slug))
        {
            outList.Add(slug);
        }
    }
}
