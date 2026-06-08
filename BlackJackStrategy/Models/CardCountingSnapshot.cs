namespace BlackJackStrategy.Models;

public sealed record CardCountingSnapshot(
    string SystemName,
    bool IsBalanced,
    bool UsesSideCounts,
    double RunningCount,
    double? TrueCount,
    double DecksRemaining,
    IReadOnlyDictionary<string, double> SideCounts);
