namespace BlackJackData.Enums;

public enum GamePhase : byte
{
    WaitingForBets = 0,
    InitialDeal = 1,
    InsuranceDecision = 2,
    PlayerTurn = 3,
    DealerTurn = 4,
    Payout = 5,
    Completed = 6
}
