using BlackJackData.Enums;
using BlackJackData.Rules;
using BlackJackStrategy.Solvers;

namespace BlackJackStrategy.Tables;

public static class BasicStrategyTableGenerator
{
    public static BasicStrategyTables Generate(BlackjackRules rules)
    {
        var hardTotals = new PlayerActionType[18, 10];
        var softTotals = new PlayerActionType[9, 10];
        var pairs = new PlayerActionType[10, 10];
        var solver = new InfiniteDeckBasicStrategySolver(rules);

        for (var dealerValue = 2; dealerValue <= 11; dealerValue++)
        {
            var column = dealerValue - 2;

            for (var total = 4; total <= 21; total++)
            {
                hardTotals[total - 4, column] = solver.SolveHardAction(total, dealerValue);
            }

            for (var total = 13; total <= 21; total++)
            {
                softTotals[total - 13, column] = solver.SolveSoftAction(total, dealerValue);
            }

            for (var pairValue = 2; pairValue <= 11; pairValue++)
            {
                pairs[pairValue - 2, column] = solver.SolvePairAction(pairValue, dealerValue);
            }
        }

        return new BasicStrategyTables(hardTotals, softTotals, pairs);
    }
}
