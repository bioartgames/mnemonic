using System.Text;

namespace Mnemonic.Events;

public sealed class JsonlEventTailer
{
    private long _offset;

    public IReadOnlyList<SessionEvent> Poll(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length < _offset)
        {
            _offset = 0;
        }

        if (fileInfo.Length <= _offset)
        {
            return [];
        }

        var bytes = File.ReadAllBytes(path);
        var start = (int)_offset;
        _offset = bytes.Length;
        var text = Encoding.UTF8.GetString(bytes, start, bytes.Length - start);

        var events = new List<SessionEvent>();
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (SessionEvent.TryParseFromJsonLine(line.TrimEnd('\r'), out var evt) && evt is not null)
            {
                events.Add(evt);
            }
        }

        return events;
    }
}
