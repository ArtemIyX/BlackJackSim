using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class AceFiveCountingSystem : CardCountingSystemBase
{
    public AceFiveCountingSystem()
        : base("Ace/Five", false, new CardCountTagSet(0, 0, 0, 1, 0, 0, 0, 0, 0, -1))
    {
    }
}
