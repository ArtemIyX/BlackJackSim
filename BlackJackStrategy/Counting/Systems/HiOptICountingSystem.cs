using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class HiOptICountingSystem : CardCountingSystemBase
{
    public HiOptICountingSystem()
        : base("Hi-Opt I", true, new CardCountTagSet(0, 1, 1, 1, 1, 0, 0, 0, -1, 0))
    {
    }
}
