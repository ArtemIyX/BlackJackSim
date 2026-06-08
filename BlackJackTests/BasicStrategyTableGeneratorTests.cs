using BlackJackData.Enums;
using BlackJackData.Rules;
using BlackJackStrategy.Tables;
using FluentAssertions;

namespace BlackJackTests;

public class BasicStrategyTableGeneratorTests
{
    [Fact]
    public void Generate_ShouldBuildExpectedHardSoftAndPairEntries()
    {
        var tables = BasicStrategyTableGenerator.Generate(BlackjackRules.Default);

        tables.GetHardAction(16, 10).Should().Be(PlayerActionType.Hit);
        tables.GetHardAction(12, 4).Should().Be(PlayerActionType.Stand);
        tables.GetSoftAction(18, 6).Should().Be(PlayerActionType.Double);
        tables.GetSoftAction(18, 9).Should().Be(PlayerActionType.Hit);
        tables.GetPairAction(8, 10).Should().Be(PlayerActionType.Split);
        tables.GetPairAction(10, 6).Should().Be(PlayerActionType.Stand);
    }

    [Fact]
    public void Generate_ShouldRespectRuleDifferences()
    {
        var h17Tables = BasicStrategyTableGenerator.Generate(
            BlackjackRules.Default with { DealerHitRule = DealerHitRule.HitSoft17 });
        var noDasTables = BasicStrategyTableGenerator.Generate(
            BlackjackRules.Default with { AllowDoubleAfterSplit = false });

        h17Tables.GetHardAction(11, 11).Should().Be(PlayerActionType.Double);
        noDasTables.GetPairAction(4, 5).Should().Be(PlayerActionType.Hit);
    }
}
