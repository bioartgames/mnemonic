namespace Mnemonic.Heuristic;

public sealed record HeuristicCatalogEntry(
    string Type,
    string Label,
    string Description,
    string Category,
    int DefaultWeight,
    int DefaultCap);
