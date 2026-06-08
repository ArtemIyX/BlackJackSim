using BlackJackData.Actions;
using BlackJackData.Enums;
using BlackJackData.Results;
using BlackJackData.Rules;
using BlackJackData.States;
using BlackJackData.Structs;
using BlackJackData.ValueObjects;
using BlackJackEngine.Engine;
using BlackJackEngine.Models;
using BlackJackEngine.Shoe;

var bankroll = 100m;
var roundNumber = 1L;
var penetrationPercent = ReadPenetrationPercent(BlackjackRules.Default.ShoePenetration * 100d);
var rules = BlackjackRules.Default with { ShoePenetration = penetrationPercent / 100d };
var engine = new BlackjackRoundEngine();
var shoe = new RandomShoeSession(rules.DeckCount, rules.ShoePenetration);

Console.WriteLine("Blackjack Console");
Console.WriteLine("Bankroll starts at 100.");
Console.WriteLine($"Shoe penetration: {penetrationPercent:0.##}%");
Console.WriteLine();

while (bankroll > 0m)
{
    shoe.PrepareNextRound();
    if (shoe.LastRoundUsedFreshShoe)
    {
        Console.WriteLine("Shuffling a new shoe...");
    }

    Console.WriteLine($"Current bankroll: {bankroll:0.##}");
    Console.WriteLine($"Cards remaining in shoe: {shoe.CardsRemaining}");
    var wager = ReadWager(bankroll);
    var state = engine.StartRound(
        new RoundStartOptions(
            new RoundId(roundNumber++),
            rules,
            [new SeatBet(new SeatId(1), wager, bankroll)]),
        shoe);

    while (state.Phase is GamePhase.InsuranceDecision or GamePhase.PlayerTurn)
    {
        RenderState(state);
        var action = PromptForAction(engine, state);
        state = engine.ApplyPlayerAction(state, action, shoe);
        Console.WriteLine();
    }

    if (state.Phase == GamePhase.DealerTurn)
    {
        RenderState(state);
        Console.WriteLine("Dealer plays...");
        state = engine.PlayDealerTurn(state, shoe);
        Console.WriteLine();
    }

    RenderState(state);
    var result = engine.ResolveRound(state);
    var seatResult = result.Seats.Single();
    bankroll += seatResult.NetPayout;

    WriteRoundResult(seatResult, bankroll);

    if (bankroll <= 0m)
    {
        Console.WriteLine("Bankroll is empty.");
        break;
    }

    Console.WriteLine();
    Console.Write("Play another round? (y/n): ");
    var playAgain = Console.ReadLine()?.Trim().ToLowerInvariant();
    Console.WriteLine();
    if (playAgain is not ("y" or "yes"))
    {
        break;
    }
}

return;

static double ReadPenetrationPercent(double defaultPercent)
{
    while (true)
    {
        Console.Write($"Enter shoe penetration percent [{defaultPercent:0.##}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultPercent;
        }

        if (double.TryParse(input, out var percent) && percent > 0d && percent < 100d)
        {
            return percent;
        }

        Console.WriteLine("Please enter a value greater than 0 and less than 100.");
    }
}

static decimal ReadWager(decimal bankroll)
{
    while (true)
    {
        Console.Write("Enter wager: ");
        var input = Console.ReadLine();
        if (decimal.TryParse(input, out var wager) && wager > 0m && wager <= bankroll)
        {
            return decimal.Round(wager, 2);
        }

        Console.WriteLine($"Please enter a valid amount between 0.01 and {bankroll:0.##}.");
    }
}

static PlayerAction PromptForAction(BlackjackRoundEngine engine, RoundState state)
{
    var actions = engine.GetLegalActions(state);
    var activeSeatId = state.ActiveSeatId ?? throw new InvalidOperationException("No active seat.");
    var activeHandId = state.ActiveHandId ?? throw new InvalidOperationException("No active hand.");

    while (true)
    {
        Console.WriteLine($"Available actions: {string.Join(", ", actions.Select(FormatAction))}");
        Console.Write("Choose action: ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();

        var actionType = input switch
        {
            "h" or "hit" => PlayerActionType.Hit,
            "s" or "stand" => PlayerActionType.Stand,
            "d" or "double" => PlayerActionType.Double,
            "p" or "split" => PlayerActionType.Split,
            "u" or "surrender" => PlayerActionType.Surrender,
            "i" or "insurance" => PlayerActionType.Insurance,
            "n" or "no" or "decline" => PlayerActionType.DeclineInsurance,
            _ => PlayerActionType.None
        };

        if (actions.Contains(actionType))
        {
            return new PlayerAction(activeSeatId, activeHandId, actionType);
        }

        Console.WriteLine("That action is not available right now.");
    }
}

static string FormatAction(PlayerActionType actionType)
{
    return actionType switch
    {
        PlayerActionType.Hit => "Hit (h)",
        PlayerActionType.Stand => "Stand (s)",
        PlayerActionType.Double => "Double (d)",
        PlayerActionType.Split => "Split (p)",
        PlayerActionType.Surrender => "Surrender (u)",
        PlayerActionType.Insurance => "Insurance (i)",
        PlayerActionType.DeclineInsurance => "Decline insurance (n)",
        _ => actionType.ToString()
    };
}

static void RenderState(RoundState state)
{
    Console.Clear();
    Console.WriteLine("Blackjack");
    Console.WriteLine(new string('-', 36));
    WriteDealer(state);
    Console.WriteLine();
    WritePlayerHands(state);
    Console.WriteLine();
}

static void WriteDealer(RoundState state)
{
    var showHoleCard = state.Dealer.HoleCardRevealed ||
                       state.Phase is GamePhase.DealerTurn or GamePhase.Payout or GamePhase.Completed;

    var dealerCards = state.Dealer.Cards
        .Select((card, index) => showHoleCard || index == 0 ? FormatCard(card) : "[Hidden]")
        .ToArray();

    var totalText = showHoleCard
        ? FormatHandValue(state.Dealer.Value)
        : VisibleDealerTotal(state.Dealer);

    Console.WriteLine($"Dealer: {string.Join(" ", dealerCards)}");
    Console.WriteLine($"Dealer total: {totalText}");
}

static void WritePlayerHands(RoundState state)
{
    var seat = state.Seats.Single();
    for (var index = 0; index < seat.Hands.Count; index++)
    {
        var hand = seat.Hands[index];
        var marker = state.ActiveHandId == hand.Id ? ">>" : "  ";
        var label = seat.Hands.Count == 1 ? "Player" : $"Hand {index + 1}";
        var flags = GetHandFlags(hand);

        Console.WriteLine($"{marker} {label}: {string.Join(" ", hand.Cards.Select(FormatCard))}");
        Console.WriteLine($"   Total: {FormatHandValue(hand.Value)} | Wager: {hand.Wager:0.##}{flags}");
    }
}

static string GetHandFlags(HandState hand)
{
    var flags = new List<string>();

    if (hand.IsBlackjack)
    {
        flags.Add("Blackjack");
    }

    if (hand.IsBust)
    {
        flags.Add("Bust");
    }

    if (hand.IsStanding && !hand.IsBust)
    {
        flags.Add("Stand");
    }

    if (hand.IsDoubledDown)
    {
        flags.Add("Doubled");
    }

    if (hand.IsSplitHand)
    {
        flags.Add("Split");
    }

    if (hand.IsSurrendered)
    {
        flags.Add("Surrendered");
    }

    if (hand.HasInsurance)
    {
        flags.Add("Insured");
    }

    return flags.Count == 0 ? string.Empty : $" | {string.Join(", ", flags)}";
}

static string VisibleDealerTotal(DealerState dealer)
{
    if (dealer.UpCard is null)
    {
        return "-";
    }

    return dealer.UpCard.Value.Rank switch
    {
        CardRank.Ace => "soft 11 / 1",
        >= CardRank.Jack and <= CardRank.King => "10",
        _ => ((int)dealer.UpCard.Value.Rank).ToString()
    };
}

static string FormatHandValue(HandValue value)
{
    if (value.IsBust)
    {
        return $"{value.BestTotal} (bust)";
    }

    if (value.IsSoft && value.HardTotal != value.BestTotal)
    {
        return $"{value.BestTotal} (soft {value.BestTotal})";
    }

    return value.BestTotal.ToString();
}

static string FormatCard(CardDef card)
{
    return $"[{FormatRank(card.Rank)}{FormatSuit(card.Suit)}]";
}

static string FormatRank(CardRank rank)
{
    return rank switch
    {
        CardRank.Ace => "A",
        CardRank.King => "K",
        CardRank.Queen => "Q",
        CardRank.Jack => "J",
        CardRank.Ten => "10",
        _ => ((int)rank).ToString()
    };
}

static string FormatSuit(CardSuit suit)
{
    return suit switch
    {
        CardSuit.Spades => "S",
        CardSuit.Hearts => "H",
        CardSuit.Diamonds => "D",
        CardSuit.Clubs => "C",
        _ => "?"
    };
}

static void WriteRoundResult(SeatResult seatResult, decimal bankroll)
{
    Console.WriteLine("Round result");
    Console.WriteLine(new string('-', 36));

    foreach (var hand in seatResult.Hands.Select((result, index) => (result, index)))
    {
        var label = seatResult.Hands.Count == 1 ? "Player" : $"Hand {hand.index + 1}";
        Console.WriteLine($"{label}: {hand.result.Outcome} | Net: {hand.result.NetPayout:+0.##;-0.##;0}");
    }

    Console.WriteLine($"Round net: {seatResult.NetPayout:+0.##;-0.##;0}");
    Console.WriteLine($"Bankroll: {bankroll:0.##}");
}
