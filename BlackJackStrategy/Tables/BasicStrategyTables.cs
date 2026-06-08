using BlackJackData.Enums;

namespace BlackJackStrategy.Tables;

public sealed class BasicStrategyTables
{
    private readonly PlayerActionType[,] _hardTotals;
    private readonly PlayerActionType[,] _softTotals;
    private readonly PlayerActionType[,] _pairs;

    public BasicStrategyTables(
        PlayerActionType[,] hardTotals,
        PlayerActionType[,] softTotals,
        PlayerActionType[,] pairs)
    {
        _hardTotals = hardTotals;
        _softTotals = softTotals;
        _pairs = pairs;
    }

    public BasicStrategyTableKind GetTableKind(bool isPair, bool isSoft)
    {
        if (isPair)
        {
            return BasicStrategyTableKind.Pairs;
        }

        return isSoft ? BasicStrategyTableKind.SoftTotals : BasicStrategyTableKind.HardTotals;
    }

    public PlayerActionType GetHardAction(int total, int dealerValue)
    {
        return _hardTotals[ClampHardTotal(total), DealerColumn(dealerValue)];
    }

    public PlayerActionType GetSoftAction(int total, int dealerValue)
    {
        return _softTotals[ClampSoftTotal(total), DealerColumn(dealerValue)];
    }

    public PlayerActionType GetPairAction(int pairRankValue, int dealerValue)
    {
        return _pairs[ClampPairRank(pairRankValue), DealerColumn(dealerValue)];
    }

    public PlayerActionType[,] ExportHardTotals() => (PlayerActionType[,])_hardTotals.Clone();

    public PlayerActionType[,] ExportSoftTotals() => (PlayerActionType[,])_softTotals.Clone();

    public PlayerActionType[,] ExportPairs() => (PlayerActionType[,])_pairs.Clone();

    private static int DealerColumn(int dealerValue)
    {
        return dealerValue switch
        {
            >= 2 and <= 11 => dealerValue - 2,
            _ => throw new ArgumentOutOfRangeException(nameof(dealerValue), dealerValue, "Dealer value must be in the range 2..11.")
        };
    }

    private static int ClampHardTotal(int total)
    {
        return total switch
        {
            <= 4 => 0,
            >= 21 => 17,
            _ => total - 4
        };
    }

    private static int ClampSoftTotal(int total)
    {
        return total switch
        {
            <= 13 => 0,
            >= 21 => 8,
            _ => total - 13
        };
    }

    private static int ClampPairRank(int pairRankValue)
    {
        return pairRankValue switch
        {
            >= 2 and <= 11 => pairRankValue - 2,
            _ => throw new ArgumentOutOfRangeException(nameof(pairRankValue), pairRankValue, "Pair rank value must be in the range 2..11.")
        };
    }
}
