namespace Mnemonic.Heuristic;

public sealed class HeuristicTypeSettings
{
    public bool Enabled { get; set; } = true;

    public int Weight { get; set; }

    /// <summary>0 = use catalog default cap.</summary>
    public int Cap { get; set; }
}
