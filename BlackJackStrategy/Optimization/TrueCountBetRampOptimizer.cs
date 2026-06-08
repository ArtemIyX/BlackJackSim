using BlackJackStrategy.Betting;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Counting.Systems;
using BlackJackStrategy.Models;
using BlackJackStrategy.Simulation;
using BlackJackStrategy.Strategies;

namespace BlackJackStrategy.Optimization;

public sealed class TrueCountBetRampOptimizer : IBetRampOptimizer
{
    private readonly BlackjackSimulationRunner _runner;

    public TrueCountBetRampOptimizer(BlackjackSimulationRunner? runner = null)
    {
        _runner = runner ?? new BlackjackSimulationRunner();
    }

    public BetRampOptimizationResult Optimize(BetRampOptimizationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ValidateConfig(config);

        var candidates = GenerateCandidates(config).ToArray();
        var results = new List<BetRampEvaluationResult>(candidates.Length);

        foreach (var candidate in candidates)
        {
            var strategy = new CountingStrategyBot(
                new HiLoCountingSystem(),
                new TrueCountStepBetRamp(candidate.Steps, candidate.FallbackUnits),
                new BasicStrategyBot(config.MinimumWager, config.Rules),
                config.UnitSize);

            var simulationResult = _runner.Run(
                strategy,
                new SimulationConfig(
                    RoundsToPlay: config.RoundsPerCandidate,
                    StartingBankroll: config.StartingBankroll,
                    Rules: config.Rules,
                    MinimumWager: config.MinimumWager,
                    CaptureRoundRecords: false,
                    RandomSeed: config.RandomSeed,
                    StopOnBankruptcy: config.StopOnBankruptcy));

            results.Add(new BetRampEvaluationResult(candidate, simulationResult));
        }

        var topResults = results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.SimulationResult.Statistics.MaxDrawdown)
            .ThenByDescending(result => result.SimulationResult.Statistics.EndingBankroll)
            .Take(config.TopResultsToKeep)
            .ToArray();

        return new BetRampOptimizationResult(config, candidates.Length, topResults);
    }

    private static IEnumerable<BetRampCandidate> GenerateCandidates(BetRampOptimizationConfig config)
    {
        var thresholds = config.Thresholds
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        var units = config.AllowedUnits
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        var current = new decimal[thresholds.Length];
        foreach (var candidate in GenerateRecursive(0, config.FallbackUnits))
        {
            yield return candidate;
        }

        IEnumerable<BetRampCandidate> GenerateRecursive(int index, decimal previous)
        {
            if (index == thresholds.Length)
            {
                var steps = thresholds
                    .Select((threshold, i) => new BetRampStep(threshold, current[i]))
                    .ToArray();

                yield return new BetRampCandidate(config.FallbackUnits, steps);
                yield break;
            }

            foreach (var unit in units.Where(unit => unit >= previous))
            {
                current[index] = unit;
                foreach (var candidate in GenerateRecursive(index + 1, unit))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static void ValidateConfig(BetRampOptimizationConfig config)
    {
        if (config.RoundsPerCandidate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.RoundsPerCandidate, "Rounds per candidate must be positive.");
        }

        if (config.StartingBankroll <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.StartingBankroll, "Starting bankroll must be positive.");
        }

        if (config.MinimumWager <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.MinimumWager, "Minimum wager must be positive.");
        }

        if (config.UnitSize <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.UnitSize, "Unit size must be positive.");
        }

        if (config.FallbackUnits <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.FallbackUnits, "Fallback units must be positive.");
        }

        if (config.Thresholds.Count == 0)
        {
            throw new ArgumentException("At least one threshold is required.", nameof(config));
        }

        if (config.AllowedUnits.Count == 0)
        {
            throw new ArgumentException("At least one allowed unit value is required.", nameof(config));
        }

        if (config.TopResultsToKeep <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.TopResultsToKeep, "Top results count must be positive.");
        }
    }
}
