using BlackJackData.Rules;

namespace BlackJackStrategy.Models;

public sealed record BetRampOptimizationConfig(
    BlackjackRules Rules,
    int RoundsPerCandidate,
    decimal StartingBankroll,
    decimal MinimumWager,
    decimal UnitSize,
    IReadOnlyList<double> Thresholds,
    IReadOnlyList<decimal> AllowedUnits,
    decimal FallbackUnits = 1m,
    int TopResultsToKeep = 10,
    int? RandomSeed = null,
    bool StopOnBankruptcy = true);
