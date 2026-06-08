using BlackJackData.Enums;

namespace BlackJackStrategy.Models;

public sealed record CardCountTagSet(
    int Two,
    int Three,
    int Four,
    int Five,
    int Six,
    int Seven,
    int Eight,
    int Nine,
    int Ten,
    int Ace)
{
    public int GetTag(CardRank rank)
    {
        return rank switch
        {
            CardRank.Two => Two,
            CardRank.Three => Three,
            CardRank.Four => Four,
            CardRank.Five => Five,
            CardRank.Six => Six,
            CardRank.Seven => Seven,
            CardRank.Eight => Eight,
            CardRank.Nine => Nine,
            CardRank.Ten or CardRank.Jack or CardRank.Queen or CardRank.King => Ten,
            CardRank.Ace => Ace,
            _ => 0
        };
    }
}
