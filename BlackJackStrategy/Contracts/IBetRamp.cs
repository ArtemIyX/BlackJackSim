using BlackJackStrategy.Models;

namespace BlackJackStrategy.Contracts;

public interface IBetRamp
{
    string Name { get; }

    decimal GetWager(BetRampContext context);
}
