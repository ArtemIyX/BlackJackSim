using BlackJackData.Enums;
using BlackJackData.Structs;
using BlackJackEngine.Contracts;

namespace BlackJackEngine.Shoe;

public sealed class RandomShoe : IBlackjackShoe
{
    private readonly Queue<CardDef> _cards;

    public RandomShoe(int deckCount, Random? random = null)
    {
        if (deckCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deckCount), deckCount, "Deck count must be positive.");
        }

        random ??= Random.Shared;
        var cards = CreateCards(deckCount);
        Shuffle(cards, random);
        _cards = new Queue<CardDef>(cards);
    }

    public int CardsRemaining => _cards.Count;

    public CardDef Draw()
    {
        if (_cards.Count == 0)
        {
            throw new InvalidOperationException("The shoe is empty.");
        }

        return _cards.Dequeue();
    }

    private static List<CardDef> CreateCards(int deckCount)
    {
        var cards = new List<CardDef>(deckCount * 52);

        for (var deck = 0; deck < deckCount; deck++)
        {
            foreach (CardSuit suit in Enum.GetValues<CardSuit>())
            {
                foreach (CardRank rank in Enum.GetValues<CardRank>())
                {
                    cards.Add(new CardDef(suit, rank));
                }
            }
        }

        return cards;
    }

    private static void Shuffle(List<CardDef> cards, Random random)
    {
        for (var index = cards.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
        }
    }
}
