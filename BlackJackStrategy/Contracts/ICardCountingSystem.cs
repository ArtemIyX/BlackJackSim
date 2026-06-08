using BlackJackData.Structs;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Contracts;

public interface ICardCountingSystem
{
    string Name { get; }

    bool IsBalanced { get; }

    void Reset();

    void ObserveCard(CardDef card);

    void ObserveCards(IEnumerable<CardDef> cards);

    CardCountingSnapshot GetSnapshot(double decksRemaining);
}
