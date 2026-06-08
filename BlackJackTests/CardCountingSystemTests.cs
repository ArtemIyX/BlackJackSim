using BlackJackData.Enums;
using BlackJackData.Structs;
using BlackJackStrategy.Counting;
using BlackJackStrategy.Counting.Systems;
using FluentAssertions;

namespace BlackJackTests;

public class CardCountingSystemTests
{
    [Fact]
    public void HiLo_ShouldProduceExpectedRunningAndTrueCount()
    {
        var system = new HiLoCountingSystem();

        system.ObserveCards(
        [
            C(CardRank.Two),
            C(CardRank.Five),
            C(CardRank.King),
            C(CardRank.Ace)
        ]);

        var snapshot = system.GetSnapshot(2d);

        snapshot.RunningCount.Should().Be(0d);
        snapshot.TrueCount.Should().Be(0d);
        snapshot.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public void RedSeven_ShouldTreatOnlyRedSevensAsPositive()
    {
        var system = new RedSevenCountingSystem();

        system.ObserveCards(
        [
            new CardDef(CardSuit.Hearts, CardRank.Seven),
            new CardDef(CardSuit.Spades, CardRank.Seven)
        ]);

        var snapshot = system.GetSnapshot(1d);

        snapshot.RunningCount.Should().Be(1d);
        snapshot.TrueCount.Should().BeNull();
    }

    [Fact]
    public void WongHalves_ShouldSupportFractionalRunningCount()
    {
        var system = new WongHalvesCountingSystem();

        system.ObserveCards(
        [
            C(CardRank.Two),
            C(CardRank.Five),
            C(CardRank.Nine),
            C(CardRank.Ace)
        ]);

        var snapshot = system.GetSnapshot(2d);

        snapshot.RunningCount.Should().BeApproximately(0.5d, 1e-9);
        snapshot.TrueCount.Should().BeApproximately(0.25d, 1e-9);
    }

    [Fact]
    public void HiOptII_ShouldTrackAceSideCount()
    {
        var system = new HiOptIICountingSystem();

        system.ObserveCards(
        [
            C(CardRank.Ace),
            C(CardRank.Five),
            C(CardRank.Ace)
        ]);

        var snapshot = system.GetSnapshot(1d);

        snapshot.UsesSideCounts.Should().BeTrue();
        snapshot.SideCounts["Aces"].Should().Be(2d);
        snapshot.RunningCount.Should().Be(2d);
    }

    [Fact]
    public void CreateAll_ShouldReturnMajorCountingSystems()
    {
        var systems = CardCountingSystems.CreateAll();

        systems.Select(system => system.Name).Should().BeEquivalentTo(
        [
            "Hi-Lo",
            "Knock-Out",
            "Red Seven",
            "Zen Count",
            "Omega II",
            "Hi-Opt I",
            "Hi-Opt II",
            "Wong Halves",
            "Ace/Five"
        ]);
    }

    private static CardDef C(CardRank rank)
    {
        return new CardDef(CardSuit.Diamonds, rank);
    }
}
