using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class WongHalvesCountingSystem : CardCountingSystemBase
{
    public WongHalvesCountingSystem()
        : base("Wong Halves", true, new CardCountTagSet(0.5d, 1d, 1d, 1.5d, 1d, 0.5d, 0d, -0.5d, -1d, -1d))
    {
    }
}
