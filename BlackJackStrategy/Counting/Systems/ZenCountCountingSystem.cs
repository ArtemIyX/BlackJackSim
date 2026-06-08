using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class ZenCountCountingSystem : CardCountingSystemBase
{
    public ZenCountCountingSystem()
        : base("Zen Count", true, new CardCountTagSet(1, 1, 2, 2, 2, 1, 0, 0, -2, -1))
    {
    }
}
