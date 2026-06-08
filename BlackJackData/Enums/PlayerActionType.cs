namespace BlackJackData.Enums;

public enum PlayerActionType : byte
{
    None = 0,
    Bet = 1,
    Hit = 2,
    Stand = 3,
    Double = 4,
    Split = 5,
    Surrender = 6,
    Insurance = 7,
    DeclineInsurance = 8
}
