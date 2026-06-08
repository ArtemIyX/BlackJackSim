using BlackJackData.Structs;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting;

public abstract class CardCountingSystemBase : ICardCountingSystem
{
    protected CardCountingSystemBase(string name, bool isBalanced, CardCountTagSet tags)
    {
        Name = name;
        IsBalanced = isBalanced;
        Tags = tags;
    }

    protected CardCountTagSet Tags { get; }

    protected int RunningCount { get; private set; }

    public string Name { get; }

    public bool IsBalanced { get; }

    public virtual void Reset()
    {
        RunningCount = 0;
    }

    public virtual void ObserveCard(CardDef card)
    {
        RunningCount += Tags.GetTag(card.Rank);
    }

    public virtual void ObserveCards(IEnumerable<CardDef> cards)
    {
        foreach (var card in cards)
        {
            ObserveCard(card);
        }
    }

    public virtual CardCountingSnapshot GetSnapshot(double decksRemaining)
    {
        if (decksRemaining < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(decksRemaining), decksRemaining, "Decks remaining cannot be negative.");
        }

        double? trueCount = IsBalanced && decksRemaining > 0d
            ? RunningCount / decksRemaining
            : null;

        return new CardCountingSnapshot(Name, IsBalanced, RunningCount, trueCount, decksRemaining);
    }
}
