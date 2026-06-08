using BlackJackData.Enums;

namespace BlackJackStrategy.Models;

public sealed record CardCountTagSet(
    double Two,
    double Three,
    double Four,
    double Five,
    double Six,
    double Seven,
    double Eight,
    double Nine,
    double Ten,
    double Ace)
{
    public double GetTag(CardRank rank)
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
