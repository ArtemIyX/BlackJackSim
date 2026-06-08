namespace BlackJackStrategy.Models;

public sealed record BetRampCandidate(
    decimal FallbackUnits,
    IReadOnlyList<BetRampStep> Steps)
{
    public string ToDisplayString()
    {
        var parts = new List<string> { $"default:{FallbackUnits:0.##}u" };
        parts.AddRange(Steps.Select(step => $"tc>={step.MinimumTrueCountInclusive:0.##}:{step.Units:0.##}u"));
        return string.Join(", ", parts);
    }
}
