using BlackJackStrategy.Contracts;
using BlackJackStrategy.Counting.Systems;

namespace BlackJackStrategy.Counting;

public static class CardCountingSystems
{
    public static IReadOnlyList<ICardCountingSystem> CreateAll()
    {
        return
        [
            new HiLoCountingSystem(),
            new KnockOutCountingSystem(),
            new RedSevenCountingSystem(),
            new ZenCountCountingSystem(),
            new OmegaIICountingSystem(),
            new HiOptICountingSystem(),
            new HiOptIICountingSystem(),
            new WongHalvesCountingSystem(),
            new AceFiveCountingSystem()
        ];
    }
}
