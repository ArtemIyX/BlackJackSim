using BlackJackData.Structs;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting;

public abstract class CardCountingSystemBase : ICardCountingSystem
{
    private readonly Dictionary<string, double> _sideCounts = new();

    protected CardCountingSystemBase(string name, bool isBalanced, CardCountTagSet tags)
    {
        Name = name;
        IsBalanced = isBalanced;
        Tags = tags;
    }

    protected CardCountTagSet Tags { get; }

    protected double RunningCount { get; private set; }

    public string Name { get; }

    public bool IsBalanced { get; }

    public virtual bool UsesSideCounts => _sideCounts.Count > 0;

    public virtual void Reset()
    {
        RunningCount = 0;
        _sideCounts.Clear();
    }

    public virtual void ObserveCard(CardDef card)
    {
        RunningCount += GetCardTag(card);
        OnCardObserved(card);
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

        return new CardCountingSnapshot(
            Name,
            IsBalanced,
            UsesSideCounts,
            RunningCount,
            trueCount,
            decksRemaining,
            new Dictionary<string, double>(_sideCounts));
    }

    protected virtual double GetCardTag(CardDef card)
    {
        return Tags.GetTag(card.Rank);
    }

    protected virtual void OnCardObserved(CardDef card)
    {
    }

    protected void SetSideCount(string name, double value)
    {
        _sideCounts[name] = value;
    }

    protected double GetSideCount(string name)
    {
        return _sideCounts.GetValueOrDefault(name);
    }
}
