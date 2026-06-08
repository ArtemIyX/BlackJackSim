using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Betting;

public sealed class TrueCountStepBetRamp : IBetRamp
{
    private readonly BetRampStep[] _steps;
    private readonly decimal _fallbackUnits;

    public TrueCountStepBetRamp(IEnumerable<BetRampStep> steps, decimal fallbackUnits = 1m)
    {
        if (fallbackUnits <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(fallbackUnits), fallbackUnits, "Fallback units must be positive.");
        }

        _steps = steps
            .OrderBy(step => step.MinimumTrueCountInclusive)
            .ToArray();

        if (_steps.Any(step => step.Units <= 0m))
        {
            throw new ArgumentOutOfRangeException(nameof(steps), "Ramp step units must be positive.");
        }

        _fallbackUnits = fallbackUnits;
    }

    public string Name => "True Count Step Ramp";

    public decimal GetWager(BetRampContext context)
    {
        var trueCount = context.CountSnapshot.TrueCount;
        var units = _fallbackUnits;

        if (trueCount.HasValue)
        {
            foreach (var step in _steps)
            {
                if (trueCount.Value >= step.MinimumTrueCountInclusive)
                {
                    units = step.Units;
                }
            }
        }

        var rawWager = units * context.UnitSize;
        return context.ClampToBankroll(rawWager);
    }
}
