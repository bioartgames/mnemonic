using Mnemonic;
using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class ActivityPacketAggregatorTests
{
    [Fact]
    public void Emits_packet_when_window_closes()
    {
        var agg = new ActivityPacketAggregator();
        agg.SetWindowSeconds(60);
        var packets = new List<SessionEvent>();
        packets.AddRange(agg.OnRaw(Parse("{\"t\":100,\"type\":\"resource_saved\",\"path\":\"res://a.gd\"}")));
        Assert.Empty(packets);
        packets.AddRange(agg.OnRaw(Parse("{\"t\":161,\"type\":\"resource_saved\",\"path\":\"res://b.gd\"}")));
        Assert.Single(packets);
        Assert.Equal("activity_packet", packets[0].Type);
        Assert.Equal(160, packets[0].T);
        Assert.Equal(2, SessionEventExtras.GetInt(packets[0], "resource_saves"));
    }

    [Fact]
    public void Distinct_paths_capped()
    {
        var agg = new ActivityPacketAggregator();
        agg.SetWindowSeconds(60);
        var packets = new List<SessionEvent>();
        for (var i = 0; i < 25; i++)
        {
            packets.AddRange(
                agg.OnRaw(Parse($"{{\"t\":{100 + i},\"type\":\"resource_saved\",\"path\":\"res://f{i}.gd\"}}")));
        }
        packets.AddRange(agg.OnRaw(Parse("{\"t\":161,\"type\":\"resource_saved\",\"path\":\"res://flush.gd\"}")));

        var packet = packets[^1];
        var paths = SessionEventExtras.GetStringArray(packet, "distinct_paths");
        Assert.Equal(MnemonicConstants.ActivityPacketMaxDistinctPaths, paths.Count);
    }

    [Fact]
    public void Focus_seconds_integrated()
    {
        var agg = new ActivityPacketAggregator();
        agg.SetWindowSeconds(60);
        var packets = new List<SessionEvent>();
        packets.AddRange(agg.OnRaw(Parse("{\"t\":100,\"type\":\"editor_focus_changed\",\"focus\":\"script\"}")));
        packets.AddRange(agg.OnRaw(Parse("{\"t\":161,\"type\":\"resource_saved\",\"path\":\"res://a.gd\"}")));
        var packet = Assert.Single(packets);
        Assert.True(SessionEventExtras.GetInt(packet, "focus_script_sec") >= 50);
    }

    [Fact]
    public void Flush_emits_partial_when_activity_present()
    {
        var agg = new ActivityPacketAggregator();
        agg.SetWindowSeconds(60);
        agg.OnRaw(Parse("{\"t\":100,\"type\":\"resource_saved\",\"path\":\"res://a.gd\"}"));
        agg.OnRaw(Parse("{\"t\":110,\"type\":\"resource_saved\",\"path\":\"res://b.gd\"}"));

        var packets = agg.Flush(155).ToList();
        Assert.Single(packets);
        Assert.Equal(154.999, packets[0].T, precision: 3);
        Assert.Equal(2, SessionEventExtras.GetInt(packets[0], "resource_saves"));
    }

    [Fact]
    public void Flush_skips_empty_window()
    {
        var agg = new ActivityPacketAggregator();
        agg.SetWindowSeconds(60);
        Assert.Empty(agg.Flush(155));
    }

    [Fact]
    public void Dynamic_window_30s()
    {
        var agg = new ActivityPacketAggregator();
        agg.SetWindowSeconds(30);
        var packets = new List<SessionEvent>();
        packets.AddRange(agg.OnRaw(Parse("{\"t\":100,\"type\":\"resource_saved\",\"path\":\"res://a.gd\"}")));
        Assert.Empty(packets);
        packets.AddRange(agg.OnRaw(Parse("{\"t\":131,\"type\":\"resource_saved\",\"path\":\"res://b.gd\"}")));
        Assert.Single(packets);
        Assert.Equal(130, packets[0].T);
        Assert.Equal(30, SessionEventExtras.GetInt(packets[0], "window_sec"));
    }

    private static SessionEvent Parse(string line)
    {
        Assert.True(SessionEvent.TryParseFromJsonLine(line, out var evt));
        return evt!;
    }
}
