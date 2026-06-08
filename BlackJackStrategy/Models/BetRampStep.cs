namespace BlackJackStrategy.Models;

public sealed record BetRampStep(
    double MinimumTrueCountInclusive,
    decimal Units);
