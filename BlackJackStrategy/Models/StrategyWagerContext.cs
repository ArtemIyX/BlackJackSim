using BlackJackData.Rules;

namespace BlackJackStrategy.Models;

public sealed record StrategyWagerContext(
    long RoundNumber,
    decimal Bankroll,
    decimal MinimumWager,
    BlackjackRules Rules,
    int CardsRemaining,
    int TotalCards,
    int CutCardCardsRemaining,
    bool LastRoundUsedFreshShoe);
