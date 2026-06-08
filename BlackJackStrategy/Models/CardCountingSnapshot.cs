namespace BlackJackStrategy.Models;

public sealed record CardCountingSnapshot(
    string SystemName,
    bool IsBalanced,
    int RunningCount,
    double? TrueCount,
    double DecksRemaining);
