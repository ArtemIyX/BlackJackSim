using BlackJackData.Enums;
using BlackJackData.Rules;

namespace BlackJackStrategy.Tables;

public static class BasicStrategyTableGenerator
{
    public static BasicStrategyTables Generate(BlackjackRules rules)
    {
        var hardTotals = new PlayerActionType[18, 10];
        var softTotals = new PlayerActionType[9, 10];
        var pairs = new PlayerActionType[10, 10];

        for (var dealerValue = 2; dealerValue <= 11; dealerValue++)
        {
            var column = dealerValue - 2;

            for (var total = 4; total <= 21; total++)
            {
                hardTotals[total - 4, column] = ChooseHardAction(total, dealerValue, rules);
            }

            for (var total = 13; total <= 21; total++)
            {
                softTotals[total - 13, column] = ChooseSoftAction(total, dealerValue, rules);
            }

            for (var pairValue = 2; pairValue <= 11; pairValue++)
            {
                pairs[pairValue - 2, column] = ChoosePairAction(pairValue, dealerValue, rules);
            }
        }

        return new BasicStrategyTables(hardTotals, softTotals, pairs);
    }

    private static PlayerActionType ChoosePairAction(int pairValue, int dealerValue, BlackjackRules rules)
    {
        return pairValue switch
        {
            11 => PlayerActionType.Split,
            10 => PlayerActionType.Stand,
            9 => dealerValue is >= 2 and <= 6 or 8 or 9 ? PlayerActionType.Split : PlayerActionType.Stand,
            8 => PlayerActionType.Split,
            7 => dealerValue is >= 2 and <= 7 ? PlayerActionType.Split : PlayerActionType.Hit,
            6 => dealerValue is >= 2 and <= 6 ? PlayerActionType.Split : PlayerActionType.Hit,
            5 => ChooseHardAction(10, dealerValue, rules),
            4 => rules.AllowDoubleAfterSplit && dealerValue is 5 or 6 ? PlayerActionType.Split : PlayerActionType.Hit,
            3 => dealerValue is >= 2 and <= 7 ? PlayerActionType.Split : PlayerActionType.Hit,
            2 => dealerValue is >= 2 and <= 7 ? PlayerActionType.Split : PlayerActionType.Hit,
            _ => PlayerActionType.Hit
        };
    }

    private static PlayerActionType ChooseSoftAction(int total, int dealerValue, BlackjackRules rules)
    {
        if (rules.DoubleDownRule != DoubleDownRule.HardTenToElevenOnly &&
            rules.DoubleDownRule != DoubleDownRule.HardNineToElevenOnly)
        {
            if (total is 13 or 14 && dealerValue is 5 or 6)
            {
                return PlayerActionType.Double;
            }

            if (total is 15 or 16 && dealerValue is >= 4 and <= 6)
            {
                return PlayerActionType.Double;
            }

            if (total == 17 && dealerValue is >= 3 and <= 6)
            {
                return PlayerActionType.Double;
            }

            if (total == 18 && dealerValue is >= 3 and <= 6)
            {
                return PlayerActionType.Double;
            }
        }

        return total switch
        {
            <= 17 => PlayerActionType.Hit,
            18 when dealerValue is 2 or 7 or 8 => PlayerActionType.Stand,
            18 when dealerValue is 9 or 10 or 11 => PlayerActionType.Hit,
            >= 19 => PlayerActionType.Stand,
            _ => PlayerActionType.Hit
        };
    }

    private static PlayerActionType ChooseHardAction(int total, int dealerValue, BlackjackRules rules)
    {
        if (rules.DoubleDownRule != DoubleDownRule.HardTenToElevenOnly &&
            rules.DoubleDownRule != DoubleDownRule.HardNineToElevenOnly &&
            total == 11 &&
            (dealerValue != 11 || rules.DealerHitRule == DealerHitRule.HitSoft17))
        {
            return PlayerActionType.Double;
        }

        if (rules.DoubleDownRule != DoubleDownRule.HardTenToElevenOnly &&
            total == 9 &&
            dealerValue is >= 3 and <= 6)
        {
            return PlayerActionType.Double;
        }

        if (total == 10 && dealerValue is >= 2 and <= 9)
        {
            return PlayerActionType.Double;
        }

        if (total >= 17)
        {
            return PlayerActionType.Stand;
        }

        if (total is >= 13 and <= 16 && dealerValue is >= 2 and <= 6)
        {
            return PlayerActionType.Stand;
        }

        if (total == 12 && dealerValue is >= 4 and <= 6)
        {
            return PlayerActionType.Stand;
        }

        return PlayerActionType.Hit;
    }
}
