using BlackJackData.Enums;
using BlackJackData.Structs;

namespace BlackJackData.ValueObjects;

public readonly record struct HandValue(int HardTotal, int BestTotal, bool IsSoft, int CardCount)
{
    public bool IsBlackjack => CardCount == 2 && BestTotal == 21;

    public bool IsBust => HardTotal > 21 && BestTotal > 21;

    public static HandValue FromCards(IEnumerable<CardDef> cards)
    {
        var hardTotal = 0;
        var aceCount = 0;
        var cardCount = 0;

        foreach (var card in cards)
        {
            cardCount++;

            switch (card.Rank)
            {
                case CardRank.Ace:
                    aceCount++;
                    hardTotal += 1;
                    break;
                case >= CardRank.Jack and <= CardRank.King:
                    hardTotal += 10;
                    break;
                default:
                    hardTotal += (int)card.Rank;
                    break;
            }
        }

        var bestTotal = hardTotal;
        var softAcesUsed = 0;

        while (softAcesUsed < aceCount && bestTotal + 10 <= 21)
        {
            bestTotal += 10;
            softAcesUsed++;
        }

        return new HandValue(hardTotal, bestTotal, softAcesUsed > 0, cardCount);
    }
}
