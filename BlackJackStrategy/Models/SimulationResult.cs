namespace BlackJackStrategy.Models;

public sealed record SimulationResult(
    SimulationConfig Config,
    SimulationStatistics Statistics,
    IReadOnlyList<SimulationRoundRecord> RoundRecords);
