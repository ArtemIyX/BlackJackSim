using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class OmegaIICountingSystem : CardCountingSystemBase
{
    public OmegaIICountingSystem()
        : base("Omega II", true, new CardCountTagSet(1, 1, 2, 2, 2, 1, 0, -1, -2, 0))
    {
    }
}
