using BlackJackData.Rules;

namespace BlackJackStrategy.Models;

public sealed record SimulationConfig(
    int RoundsToPlay,
    decimal StartingBankroll,
    BlackjackRules Rules,
    decimal MinimumWager = 1m,
    int BackgroundPlayerCount = 0,
    decimal BackgroundPlayerWager = 1m,
    bool CaptureRoundRecords = false,
    int? RandomSeed = null,
    bool StopOnBankruptcy = true);
