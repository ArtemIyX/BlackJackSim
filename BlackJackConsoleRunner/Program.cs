using BlackJackData.Rules;
using BlackJackStrategy.Models;
using BlackJackStrategy.Simulation;
using BlackJackStrategy.Strategies;



Console.WriteLine("Blackjack Strategy Runner");
Console.WriteLine();

var rounds = ReadInt("Rounds to simulate", 10000, min: 1);
var startingBankroll = ReadDecimal("Starting bankroll", 1000m, min: 1m);
var minimumWager = ReadDecimal("Minimum wager", 10m, min: 0.01m, max: startingBankroll);
var strategyWager = ReadDecimal("Strategy wager", minimumWager, min: minimumWager, max: startingBankroll);
var deckCount = ReadInt("Deck count", BlackjackRules.Default.DeckCount, min: 1);
var penetrationPercent = ReadDouble("Penetration percent", BlackjackRules.Default.ShoePenetration * 100d, minExclusive: 0d, maxExclusive: 100d);
var captureRoundLog = ReadYesNo("Capture per-round log", defaultValue: false);
var seed = ReadOptionalInt("Random seed (blank for random)");

var rules = BlackjackRules.Default with
{
    DeckCount = deckCount,
    ShoePenetration = penetrationPercent / 100d
};

var config = new SimulationConfig(
    RoundsToPlay: rounds,
    StartingBankroll: startingBankroll,
    Rules: rules,
    MinimumWager: minimumWager,
    CaptureRoundRecords: captureRoundLog,
    RandomSeed: seed);


var runner = new BlackjackSimulationRunner();

Console.WriteLine();
Console.WriteLine("Running simulation...");
Console.WriteLine();

var strategy = new BasicStrategyBot(strategyWager);
var result = runner.Run(strategy, config);

WriteSummary(result);

if (captureRoundLog && result.RoundRecords.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Round log sample");
    Console.WriteLine(new string('-', 60));

    foreach (var round in result.RoundRecords.Take(10))
    {
        Console.WriteLine(
            $"Round {round.RoundNumber}: wager {round.Wager:0.##}, net {round.NetPayout:+0.##;-0.##;0}, bankroll {round.EndingBankroll:0.##}, fresh shoe: {YesNo(round.UsedFreshShoe)}");
    }

    if (result.RoundRecords.Count > 10)
    {
        Console.WriteLine($"... {result.RoundRecords.Count - 10} more rounds not shown");
    }
}

return;

static void WriteSummary(SimulationResult result)
{
    var stats = result.Statistics;

    Console.WriteLine("Simulation summary");
    Console.WriteLine(new string('-', 60));
    Console.WriteLine($"Rounds played:       {stats.RoundsPlayed}");
    Console.WriteLine($"Hands played:        {stats.HandsPlayed}");
    Console.WriteLine($"Starting bankroll:   {stats.StartingBankroll:0.##}");
    Console.WriteLine($"Ending bankroll:     {stats.EndingBankroll:0.##}");
    Console.WriteLine($"Total wagered:       {stats.TotalWagered:0.##}");
    Console.WriteLine($"Net payout:          {stats.TotalNetPayout:+0.##;-0.##;0}");
    Console.WriteLine($"ROI:                 {stats.ReturnOnInvestment:P2}");
    Console.WriteLine($"Avg / round:         {stats.AverageNetPerRound:+0.####;-0.####;0}");
    Console.WriteLine($"Avg / hand:          {stats.AverageNetPerHand:+0.####;-0.####;0}");
    Console.WriteLine($"Max bankroll:        {stats.MaxBankroll:0.##}");
    Console.WriteLine($"Min bankroll:        {stats.MinBankroll:0.##}");
    Console.WriteLine($"Max drawdown:        {stats.MaxDrawdown:0.##}");
    Console.WriteLine($"Reshuffles:          {stats.ReshuffleCount}");
    Console.WriteLine($"Bankrupt:            {YesNo(stats.WentBankrupt)}");
    Console.WriteLine();
    Console.WriteLine($"Win / loss / push:   {stats.WinHands} / {stats.LoseHands} / {stats.PushHands}");
    Console.WriteLine($"Win / loss / push %: {stats.WinRate:P2} / {stats.LossRate:P2} / {stats.PushRate:P2}");
    Console.WriteLine($"Blackjacks:          {stats.BlackjackHands}");
    Console.WriteLine($"Surrenders:          {stats.SurrenderHands}");
    Console.WriteLine($"Busts:               {stats.BustHands}");
    Console.WriteLine($"Doubles:             {stats.DoubledHands}");
    Console.WriteLine($"Split hands:         {stats.SplitHands}");
    Console.WriteLine($"Insurance hands:     {stats.InsuranceHands}");
}

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

        if (max is null)
        {
            Console.WriteLine($"Please enter a value >= {min:0.##}.");
        }
        else
        {
            Console.WriteLine($"Please enter a value between {min:0.##} and {max.Value:0.##}.");
        }
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

static bool ReadYesNo(string label, bool defaultValue)
{
    while (true)
    {
        Console.Write($"{label} [{(defaultValue ? "y" : "n")}]: ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        if (input is "y" or "yes")
        {
            return true;
        }

        if (input is "n" or "no")
        {
            return false;
        }

        Console.WriteLine("Please answer y or n.");
    }
}

static string YesNo(bool value) => value ? "yes" : "no";
