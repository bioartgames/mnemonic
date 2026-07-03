namespace Mnemonic.Heuristic;

public sealed class HeuristicScoreLine
{
    public string Type { get; set; } = "";

    public int Count { get; set; }

    public int Points { get; set; }

    public string? Detail { get; set; }
}
