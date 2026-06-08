using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Betting;

public sealed class FlatBetRamp : IBetRamp
{
    public FlatBetRamp(decimal units = 1m)
    {
        if (units <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(units), units, "Units must be positive.");
        }

        Units = units;
    }

    public string Name => "Flat Bet";

    public decimal Units { get; }

    public decimal GetWager(BetRampContext context)
    {
        var rawWager = context.UnitSize * Units;
        return context.ClampToBankroll(rawWager);
    }
}
