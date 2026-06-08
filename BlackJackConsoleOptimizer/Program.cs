using BlackJackData.Rules;
using BlackJackStrategy.Counting;
using BlackJackStrategy.Models;
using BlackJackStrategy.Optimization;

Console.WriteLine("Blackjack Bet Ramp Optimizer");
Console.WriteLine();

var roundsPerCandidate = ReadInt("Rounds per candidate", 5000, min: 1);
var startingBankroll = ReadDecimal("Starting bankroll", 1000m, min: 1m);
var minimumWager = ReadDecimal("Minimum wager", 10m, min: 0.01m, max: startingBankroll);
var unitSize = ReadDecimal("Unit size", minimumWager, min: minimumWager, max: startingBankroll);
var topResults = ReadInt("Top results to show", 10, min: 1);
var countingSystemName = ReadChoice("Counting system", CardCountingSystems.Names, "Hi-Lo");
var deckCount = ReadInt("Deck count", BlackjackRules.Default.DeckCount, min: 1);
var penetrationPercent = ReadDouble("Penetration percent", BlackjackRules.Default.ShoePenetration * 100d, minExclusive: 0d, maxExclusive: 100d);
var thresholds = ReadDoubleList("True-count thresholds", "1,2,3");
var allowedUnits = ReadDecimalList("Allowed units", "1,2,4,8");
var seed = ReadOptionalInt("Random seed (blank for random)");

var rules = BlackjackRules.Default with
{
    DeckCount = deckCount,
    ShoePenetration = penetrationPercent / 100d
};

var config = new BetRampOptimizationConfig(
    Rules: rules,
    RoundsPerCandidate: roundsPerCandidate,
    StartingBankroll: startingBankroll,
    MinimumWager: minimumWager,
    UnitSize: unitSize,
    CountingSystemName: countingSystemName,
    Thresholds: thresholds,
    AllowedUnits: allowedUnits,
    TopResultsToKeep: topResults,
    RandomSeed: seed);

Console.WriteLine();
Console.WriteLine("Optimizing...");
Console.WriteLine();

var optimizer = new TrueCountBetRampOptimizer();
var result = optimizer.Optimize(config);

Console.WriteLine($"Candidates evaluated: {result.CandidatesEvaluated}");
Console.WriteLine($"Counting system:      {result.Config.CountingSystemName}");
Console.WriteLine(new string('-', 80));

for (var index = 0; index < result.TopResults.Count; index++)
{
    var item = result.TopResults[index];
    var stats = item.SimulationResult.Statistics;

    Console.WriteLine($"#{index + 1}");
    Console.WriteLine($"Ramp:        {item.Candidate.ToDisplayString()}");
    Console.WriteLine($"Net payout:  {stats.TotalNetPayout:+0.##;-0.##;0}");
    Console.WriteLine($"ROI:         {stats.ReturnOnInvestment:P2}");
    Console.WriteLine($"End bankroll:{stats.EndingBankroll:0.##}");
    Console.WriteLine($"Drawdown:    {stats.MaxDrawdown:0.##}");
    Console.WriteLine($"Win/Loss:    {stats.WinHands}/{stats.LoseHands}");
    Console.WriteLine(new string('-', 80));
}

return;

static int ReadInt(string label, int defaultValue, int min)
{
    while (true)
    {
        Console.Write($"{label} [{defaultValue}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        if (int.TryParse(input, out var value) && value >= min)
        {
            return value;
        }

        Console.WriteLine($"Please enter an integer >= {min}.");
    }
}

static string ReadChoice(string label, IReadOnlyList<string> choices, string defaultValue)
{
    while (true)
    {
        Console.WriteLine($"{label}:");
        for (var i = 0; i < choices.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {choices[i]}{(choices[i].Equals(defaultValue, StringComparison.OrdinalIgnoreCase) ? " (default)" : string.Empty)}");
        }

        Console.Write($"Choose [default: {defaultValue}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        if (int.TryParse(input, out var index) && index >= 1 && index <= choices.Count)
        {
            return choices[index - 1];
        }

        var match = choices.FirstOrDefault(choice => choice.Equals(input, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return match;
        }

        Console.WriteLine("Please enter a valid choice number or exact name.");
    }
}

static int? ReadOptionalInt(string label)
{
    while (true)
    {
        Console.Write($"{label}: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (int.TryParse(input, out var value))
        {
            return value;
        }

        Console.WriteLine("Please enter a valid integer or leave it blank.");
    }
}

static decimal ReadDecimal(string label, decimal defaultValue, decimal min, decimal? max = null)
{
    while (true)
    {
        Console.Write($"{label} [{defaultValue:0.##}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        if (decimal.TryParse(input, out var value) && value >= min && (max is null || value <= max.Value))
        {
            return value;
        }

        Console.WriteLine(max is null
            ? $"Please enter a value >= {min:0.##}."
            : $"Please enter a value between {min:0.##} and {max.Value:0.##}.");
    }
}

static double ReadDouble(string label, double defaultValue, double minExclusive, double maxExclusive)
{
    while (true)
    {
        Console.Write($"{label} [{defaultValue:0.##}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        if (double.TryParse(input, out var value) && value > minExclusive && value < maxExclusive)
        {
            return value;
        }

        Console.WriteLine($"Please enter a value greater than {minExclusive:0.##} and less than {maxExclusive:0.##}.");
    }
}

static double[] ReadDoubleList(string label, string defaultText)
{
    while (true)
    {
        Console.Write($"{label} [{defaultText}]: ");
        var input = Console.ReadLine()?.Trim();
        input = string.IsNullOrWhiteSpace(input) ? defaultText : input;

        try
        {
            return input
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(double.Parse)
                .ToArray();
        }
        catch
        {
            Console.WriteLine("Please enter comma-separated numbers, for example: 1,2,3");
        }
    }
}

static decimal[] ReadDecimalList(string label, string defaultText)
{
    while (true)
    {
        Console.Write($"{label} [{defaultText}]: ");
        var input = Console.ReadLine()?.Trim();
        input = string.IsNullOrWhiteSpace(input) ? defaultText : input;

        try
        {
            return input
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(decimal.Parse)
                .ToArray();
        }
        catch
        {
            Console.WriteLine("Please enter comma-separated decimal numbers, for example: 1,2,4,8");
        }
    }
}
