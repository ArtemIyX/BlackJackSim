namespace BlackJackStrategy.Models;

public sealed record BetRampContext(
    decimal Bankroll,
    decimal MinimumWager,
    decimal MaximumWager,
    decimal UnitSize,
    CardCountingSnapshot CountSnapshot)
{
    public decimal ClampToBankroll(decimal wager)
    {
        return Math.Min(Math.Max(wager, MinimumWager), MaximumWager);
    }
}
