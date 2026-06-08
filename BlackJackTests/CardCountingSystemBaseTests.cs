using BlackJackData.Enums;
using BlackJackData.Structs;
using BlackJackStrategy.Counting;
using BlackJackStrategy.Models;
using FluentAssertions;

namespace BlackJackTests;

public class CardCountingSystemBaseTests
{
    [Fact]
    public void ObserveCards_ShouldUpdateRunningAndTrueCount_ForBalancedSystem()
    {
        var system = new TestBalancedCount();

        system.ObserveCards(
        [
            new CardDef(CardSuit.Spades, CardRank.Two),
            new CardDef(CardSuit.Spades, CardRank.Five),
            new CardDef(CardSuit.Spades, CardRank.King),
            new CardDef(CardSuit.Spades, CardRank.Ace)
        ]);

        var snapshot = system.GetSnapshot(decksRemaining: 2d);

        snapshot.RunningCount.Should().Be(0);
        snapshot.TrueCount.Should().Be(0d);
        snapshot.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public void GetSnapshot_ShouldLeaveTrueCountNull_ForUnbalancedSystem()
    {
        var system = new TestUnbalancedCount();
        system.ObserveCard(new CardDef(CardSuit.Hearts, CardRank.Seven));

        var snapshot = system.GetSnapshot(decksRemaining: 1.5d);

        snapshot.RunningCount.Should().Be(1);
        snapshot.TrueCount.Should().BeNull();
    }

    private sealed class TestBalancedCount : CardCountingSystemBase
    {
        public TestBalancedCount()
            : base("Test Balanced", true, new CardCountTagSet(1, 1, 1, 1, 1, 0, 0, 0, -1, -1))
        {
        }
    }

    private sealed class TestUnbalancedCount : CardCountingSystemBase
    {
        public TestUnbalancedCount()
            : base("Test Unbalanced", false, new CardCountTagSet(0, 0, 0, 0, 0, 1, 0, 0, 0, 0))
        {
        }
    }
}
