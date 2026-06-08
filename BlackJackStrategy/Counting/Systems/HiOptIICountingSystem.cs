using BlackJackData.Enums;
using BlackJackData.Structs;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class HiOptIICountingSystem : CardCountingSystemBase
{
    public HiOptIICountingSystem()
        : base("Hi-Opt II", true, new CardCountTagSet(1, 1, 2, 2, 1, 1, 0, 0, -2, 0))
    {
        SetSideCount("Aces", 0d);
    }

    public override bool UsesSideCounts => true;

    protected override void OnCardObserved(CardDef card)
    {
        if (card.Rank == CardRank.Ace)
        {
            SetSideCount("Aces", GetSideCount("Aces") + 1d);
        }
    }
}
