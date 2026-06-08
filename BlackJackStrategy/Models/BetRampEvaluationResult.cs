namespace BlackJackStrategy.Models;

public sealed record BetRampEvaluationResult(
    BetRampCandidate Candidate,
    SimulationResult SimulationResult)
{
    public decimal Score => SimulationResult.Statistics.TotalNetPayout;
}
