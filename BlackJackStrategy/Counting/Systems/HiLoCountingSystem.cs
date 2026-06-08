using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class HiLoCountingSystem : CardCountingSystemBase
{
    public HiLoCountingSystem()
        : base("Hi-Lo", true, new CardCountTagSet(1, 1, 1, 1, 1, 0, 0, 0, -1, -1))
    {
    }
}
