using BlackJackData.Enums;
using BlackJackData.Structs;
using BlackJackEngine.Contracts;

namespace BlackJackEngine.Shoe;

public sealed class RandomShoeSession : IBlackjackShoe
{
    private readonly Random _random;
    private Queue<CardDef> _cards = new();

    public RandomShoeSession(int deckCount, double penetration, Random? random = null)
    {
        if (deckCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deckCount), deckCount, "Deck count must be positive.");
        }

        if (penetration <= 0d || penetration >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(penetration), penetration, "Penetration must be between 0 and 1.");
        }

        DeckCount = deckCount;
        Penetration = penetration;
        _random = random ?? Random.Shared;
        ShuffleNewShoe();
    }

    public int DeckCount { get; }

    public double Penetration { get; }

    public int TotalCards => DeckCount * 52;

    public int CutCardCardsRemaining => (int)Math.Floor(TotalCards * (1d - Penetration));

    public int CardsRemaining => _cards.Count;

    public bool LastRoundUsedFreshShoe { get; private set; }

    public void PrepareNextRound(int minimumCardsRequired = 15)
    {
        if (minimumCardsRequired <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCardsRequired), minimumCardsRequired, "Minimum cards required must be positive.");
        }

        var mustReshuffle = CardsRemaining <= CutCardCardsRemaining || CardsRemaining < minimumCardsRequired;
        if (mustReshuffle)
        {
            ShuffleNewShoe();
            LastRoundUsedFreshShoe = true;
            return;
        }

        LastRoundUsedFreshShoe = false;
    }

    public CardDef Draw()
    {
        if (_cards.Count == 0)
        {
            throw new InvalidOperationException("The shoe is empty.");
        }

        return _cards.Dequeue();
    }

    private void ShuffleNewShoe()
    {
        var cards = CreateCards(DeckCount);
        Shuffle(cards, _random);
        _cards = new Queue<CardDef>(cards);
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
