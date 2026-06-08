using BlackJackData.Structs;

namespace BlackJackEngine.Contracts;

public interface IBlackjackShoe
{
    int CardsRemaining { get; }

    CardDef Draw();
}
