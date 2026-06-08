using BlackJackEngine.Shoe;
using FluentAssertions;

namespace BlackJackTests;

public class RandomShoeSessionTests
{
    [Fact]
    public void PrepareNextRound_ShouldNotReshuffle_WhenCutCardHasNotBeenReached()
    {
        var shoe = new RandomShoeSession(deckCount: 1, penetration: 0.75d, random: new Random(123));

        for (var index = 0; index < 12; index++)
        {
            shoe.Draw();
        }

        var cardsRemainingBefore = shoe.CardsRemaining;
        shoe.PrepareNextRound();

        shoe.LastRoundUsedFreshShoe.Should().BeFalse();
        shoe.CardsRemaining.Should().Be(cardsRemainingBefore);
    }

    [Fact]
    public void PrepareNextRound_ShouldReshuffle_WhenCutCardHasBeenReached()
    {
        var shoe = new RandomShoeSession(deckCount: 1, penetration: 0.75d, random: new Random(123));

        for (var index = 0; index < 39; index++)
        {
            shoe.Draw();
        }

        shoe.CardsRemaining.Should().Be(shoe.CutCardCardsRemaining);

        shoe.PrepareNextRound();

        shoe.LastRoundUsedFreshShoe.Should().BeTrue();
        shoe.CardsRemaining.Should().Be(shoe.TotalCards);
    }

    [Fact]
    public void PrepareNextRound_ShouldReshuffle_WhenTooFewCardsRemainForNextRound()
    {
        var shoe = new RandomShoeSession(deckCount: 1, penetration: 0.95d, random: new Random(123));

        while (shoe.CardsRemaining > 10)
        {
            shoe.Draw();
        }

        shoe.PrepareNextRound(minimumCardsRequired: 15);

        shoe.LastRoundUsedFreshShoe.Should().BeTrue();
        shoe.CardsRemaining.Should().Be(shoe.TotalCards);
    }
}
