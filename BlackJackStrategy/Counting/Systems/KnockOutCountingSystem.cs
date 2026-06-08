using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class KnockOutCountingSystem : CardCountingSystemBase
{
    public KnockOutCountingSystem()
        : base("Knock-Out", false, new CardCountTagSet(1, 1, 1, 1, 1, 1, 0, 0, -1, -1))
    {
    }
}
