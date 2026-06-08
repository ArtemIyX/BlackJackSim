namespace BlackJackStrategy.Models;

public sealed record SimulationStatistics(
    int RoundsPlayed,
    int HandsPlayed,
    decimal StartingBankroll,
    decimal EndingBankroll,
    decimal TotalWagered,
    decimal TotalNetPayout,
    decimal MaxBankroll,
    decimal MinBankroll,
    decimal MaxDrawdown,
    int ReshuffleCount,
    int WinHands,
    int LoseHands,
    int PushHands,
    int BlackjackHands,
    int SurrenderHands,
    int BustHands,
    int DoubledHands,
    int SplitHands,
    int InsuranceHands)
{
    public decimal ReturnOnInvestment => TotalWagered == 0m ? 0m : TotalNetPayout / TotalWagered;

    public decimal AverageNetPerRound => RoundsPlayed == 0 ? 0m : TotalNetPayout / RoundsPlayed;

    public decimal AverageNetPerHand => HandsPlayed == 0 ? 0m : TotalNetPayout / HandsPlayed;

    public decimal WinRate => HandsPlayed == 0 ? 0m : (decimal)WinHands / HandsPlayed;

    public decimal LossRate => HandsPlayed == 0 ? 0m : (decimal)LoseHands / HandsPlayed;

    public decimal PushRate => HandsPlayed == 0 ? 0m : (decimal)PushHands / HandsPlayed;

    public bool WentBankrupt => EndingBankroll <= 0m;
}
