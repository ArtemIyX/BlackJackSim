using BlackJackData.Enums;
using BlackJackData.Rules;
using BlackJackData.Structs;
using BlackJackData.ValueObjects;

namespace BlackJackData.States;

public sealed record RoundState(
    RoundId Id,
    BlackjackRules Rules,
    GamePhase Phase,
    IReadOnlyList<SeatState> Seats,
    DealerState Dealer,
    int ShoeCardsRemaining,
    SeatId? ActiveSeatId = null,
    HandId? ActiveHandId = null,
    bool InsuranceOffered = false)
{
    public CardDef? DealerUpCard => Dealer.UpCard;

    public bool IsComplete => Phase == GamePhase.Completed;
}
