namespace BlackJackStrategy.Models;

public sealed record BetRampOptimizationResult(
    BetRampOptimizationConfig Config,
    int CandidatesEvaluated,
    IReadOnlyList<BetRampEvaluationResult> TopResults);
