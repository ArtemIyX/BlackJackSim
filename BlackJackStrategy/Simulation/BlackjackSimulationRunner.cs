using BlackJackData.Actions;
using BlackJackData.Enums;
using BlackJackData.Results;
using BlackJackEngine.Contracts;
using BlackJackEngine.Engine;
using BlackJackEngine.Models;
using BlackJackEngine.Shoe;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;
using BlackJackStrategy.Strategies;
using BlackJackData.ValueObjects;

namespace BlackJackStrategy.Simulation;

public sealed class BlackjackSimulationRunner
{
    private readonly IBlackjackRoundEngine _roundEngine;
    private readonly Func<SimulationConfig, RandomShoeSession> _shoeFactory;

    public BlackjackSimulationRunner(
        IBlackjackRoundEngine? roundEngine = null,
        Func<SimulationConfig, RandomShoeSession>? shoeFactory = null)
    {
        _roundEngine = roundEngine ?? new BlackjackRoundEngine();
        _shoeFactory = shoeFactory ?? CreateShoeSession;
    }

    public SimulationResult Run(IBlackjackStrategy strategy, SimulationConfig config)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(config);

        ValidateConfig(config);

        strategy.Reset();
        var shoe = _shoeFactory(config);
        var bankroll = config.StartingBankroll;
        var maxBankroll = bankroll;
        var minBankroll = bankroll;
        var peakBankroll = bankroll;
        var maxDrawdown = 0m;
        var reshuffleCount = 0;
        var records = config.CaptureRoundRecords ? new List<SimulationRoundRecord>() : null;
        var handRecords = new List<SimulationHandRecord>();
        var roundsPlayed = 0;
        var roundsSatOut = 0;
        var totalWagered = 0m;
        var totalNetPayout = 0m;
        var mainSeatId = new SeatId(1);
        var backgroundStrategies = CreateBackgroundStrategies(config);

        foreach (var backgroundStrategy in backgroundStrategies)
        {
            backgroundStrategy.Reset();
        }

        var winHands = 0;
        var loseHands = 0;
        var pushHands = 0;
        var blackjackHands = 0;
        var surrenderHands = 0;
        var bustHands = 0;
        var doubledHands = 0;
        var splitHands = 0;
        var insuranceHands = 0;

        for (var roundNumber = 1; roundNumber <= config.RoundsToPlay; roundNumber++)
        {
            if (bankroll < config.MinimumWager && config.StopOnBankruptcy)
            {
                break;
            }

            shoe.PrepareNextRound();
            if (shoe.LastRoundUsedFreshShoe)
            {
                reshuffleCount++;
            }

            var wagerContext = new StrategyWagerContext(
                roundNumber,
                bankroll,
                config.MinimumWager,
                config.Rules,
                shoe.CardsRemaining,
                shoe.TotalCards,
                shoe.CutCardCardsRemaining,
                shoe.LastRoundUsedFreshShoe,
                strategy is CountingStrategyBot countingBot
                    ? countingBot.GetCurrentCountSnapshot((double)shoe.CardsRemaining / 52d)
                    : null);

            var wager = strategy.GetWager(wagerContext);
            if (wager <= 0m)
            {
                if (config.BackgroundPlayerCount == 0)
                {
                    break;
                }

                roundsSatOut++;
            }

            if (wager > 0m && (wager < config.MinimumWager || wager > bankroll))
            {
                throw new InvalidOperationException($"Strategy returned invalid wager '{wager}'.");
            }

            var bankrollBeforeRound = bankroll;
            var seatBets = CreateSeatBets(config, mainSeatId, bankroll, wager);
            var state = _roundEngine.StartRound(
                new RoundStartOptions(
                    new RoundId(roundNumber),
                    config.Rules,
                    seatBets),
                shoe);

            while (state.Phase is GamePhase.InsuranceDecision or GamePhase.PlayerTurn)
            {
                var legalActions = _roundEngine.GetLegalActions(state);
                var activeStrategy = ResolveSeatStrategy(strategy, backgroundStrategies, state.ActiveSeatId, mainSeatId);
                var actionType = activeStrategy.GetAction(new StrategyActionContext(roundNumber, bankroll, state, legalActions));
                if (!legalActions.Contains(actionType))
                {
                    throw new InvalidOperationException($"Strategy returned illegal action '{actionType}'.");
                }

                state = _roundEngine.ApplyPlayerAction(
                    state,
                    new PlayerAction(state.ActiveSeatId!.Value, state.ActiveHandId!.Value, actionType),
                    shoe);
            }

            if (state.Phase == GamePhase.DealerTurn)
            {
                state = _roundEngine.PlayDealerTurn(state, shoe);
            }

            var roundResult = _roundEngine.ResolveRound(state);
            var seatResult = roundResult.Seats.FirstOrDefault(seat => seat.SeatId == mainSeatId);
            var mainHands = state.Seats.FirstOrDefault(seat => seat.Id == mainSeatId)?.Hands ?? Array.Empty<BlackJackData.States.HandState>();
            var netPayout = seatResult?.NetPayout ?? 0m;

            bankroll += netPayout;
            totalWagered += wager;
            totalNetPayout += netPayout;
            roundsPlayed++;

            if (seatResult is not null)
            {
                foreach (var hand in mainHands.Zip(seatResult.Hands))
                {
                    var roundHandRecord = new SimulationHandRecord(
                        hand.Second.Outcome,
                        hand.Second.Wager,
                        hand.Second.NetPayout,
                        hand.First.IsSplitHand,
                        hand.Second.UsedInsurance,
                        hand.First.IsDoubledDown,
                        hand.First.IsBust);

                    handRecords.Add(roundHandRecord);

                    switch (hand.Second.Outcome)
                    {
                        case HandOutcomeType.Win:
                            winHands++;
                            break;
                        case HandOutcomeType.Lose:
                            loseHands++;
                            break;
                        case HandOutcomeType.Push:
                            pushHands++;
                            break;
                        case HandOutcomeType.Blackjack:
                            blackjackHands++;
                            break;
                        case HandOutcomeType.Surrender:
                            surrenderHands++;
                            break;
                    }

                    if (hand.First.IsBust)
                    {
                        bustHands++;
                    }

                    if (hand.First.IsDoubledDown)
                    {
                        doubledHands++;
                    }

                    if (hand.First.IsSplitHand)
                    {
                        splitHands++;
                    }

                    if (hand.Second.UsedInsurance)
                    {
                        insuranceHands++;
                    }
                }
            }

            var roundRecord = new SimulationRoundRecord(
                roundNumber,
                bankrollBeforeRound,
                wager,
                netPayout,
                bankroll,
                seatResult is not null,
                shoe.LastRoundUsedFreshShoe,
                shoe.CardsRemaining,
                config.CaptureRoundRecords && seatResult is not null
                    ? handRecords.TakeLast(seatResult.Hands.Count).ToArray()
                    : Array.Empty<SimulationHandRecord>());

            if (records is not null)
            {
                records.Add(roundRecord);
            }

            strategy.OnRoundCompleted(new StrategyRoundResultContext(
                roundNumber,
                bankrollBeforeRound,
                bankroll,
                wager,
                roundResult,
                roundRecord));

            foreach (var backgroundStrategy in backgroundStrategies)
            {
                backgroundStrategy.OnRoundCompleted(new StrategyRoundResultContext(
                    roundNumber,
                    bankrollBeforeRound,
                    bankroll,
                    wager,
                    roundResult,
                    roundRecord));
            }

            if (bankroll > maxBankroll)
            {
                maxBankroll = bankroll;
            }

            if (bankroll < minBankroll)
            {
                minBankroll = bankroll;
            }

            if (bankroll > peakBankroll)
            {
                peakBankroll = bankroll;
            }

            var drawdown = peakBankroll - bankroll;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }

            if (bankroll <= 0m && config.StopOnBankruptcy)
            {
                break;
            }
        }

        var statistics = new SimulationStatistics(
            roundsPlayed,
            roundsSatOut,
            handRecords.Count,
            config.StartingBankroll,
            bankroll,
            totalWagered,
            totalNetPayout,
            maxBankroll,
            minBankroll,
            maxDrawdown,
            reshuffleCount,
            winHands,
            loseHands,
            pushHands,
            blackjackHands,
            surrenderHands,
            bustHands,
            doubledHands,
            splitHands,
            insuranceHands);

        return new SimulationResult(config, statistics, records?.ToArray() ?? Array.Empty<SimulationRoundRecord>());
    }

    private static RandomShoeSession CreateShoeSession(SimulationConfig config)
    {
        var random = config.RandomSeed.HasValue ? new Random(config.RandomSeed.Value) : null;
        return new RandomShoeSession(config.Rules.DeckCount, config.Rules.ShoePenetration, random);
    }

    private static void ValidateConfig(SimulationConfig config)
    {
        if (config.RoundsToPlay <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.RoundsToPlay, "Rounds to play must be positive.");
        }

        if (config.StartingBankroll <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.StartingBankroll, "Starting bankroll must be positive.");
        }

        if (config.MinimumWager <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.MinimumWager, "Minimum wager must be positive.");
        }

        if (config.BackgroundPlayerCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.BackgroundPlayerCount,
                "Background player count cannot be negative.");
        }

        if (config.BackgroundPlayerCount > 0 && config.BackgroundPlayerWager <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.BackgroundPlayerWager,
                "Background player wager must be positive when background players are enabled.");
        }

        if (config.BackgroundPlayerCount > 0 && config.BackgroundPlayerWager < config.MinimumWager)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.BackgroundPlayerWager,
                "Background player wager must be at least the table minimum.");
        }
    }

    private static IBlackjackStrategy[] CreateBackgroundStrategies(SimulationConfig config)
    {
        return Enumerable.Range(0, config.BackgroundPlayerCount)
            .Select(_ => (IBlackjackStrategy)new BasicStrategyBot(config.BackgroundPlayerWager, config.Rules))
            .ToArray();
    }

    private static IReadOnlyList<SeatBet> CreateSeatBets(SimulationConfig config, SeatId mainSeatId, decimal bankroll, decimal wager)
    {
        var seatBets = new List<SeatBet>(config.BackgroundPlayerCount + (wager > 0m ? 1 : 0));

        if (wager > 0m)
        {
            seatBets.Add(new SeatBet(mainSeatId, wager, bankroll));
        }

        for (var playerIndex = 0; playerIndex < config.BackgroundPlayerCount; playerIndex++)
        {
            var seatId = new SeatId(playerIndex + 2);
            var backgroundBankroll = Math.Max(config.BackgroundPlayerWager * config.Rules.MaxHandsPerSeat * 2m, config.BackgroundPlayerWager * 10m);
            seatBets.Add(new SeatBet(seatId, config.BackgroundPlayerWager, backgroundBankroll));
        }

        return seatBets;
    }

    private static IBlackjackStrategy ResolveSeatStrategy(
        IBlackjackStrategy mainStrategy,
        IReadOnlyList<IBlackjackStrategy> backgroundStrategies,
        SeatId? activeSeatId,
        SeatId mainSeatId)
    {
        if (activeSeatId is null || activeSeatId == mainSeatId)
        {
            return mainStrategy;
        }

        var backgroundIndex = activeSeatId.Value.Value - 2;
        if (backgroundIndex < 0 || backgroundIndex >= backgroundStrategies.Count)
        {
            throw new InvalidOperationException($"No strategy is configured for background seat {activeSeatId.Value.Value}.");
        }

        return backgroundStrategies[backgroundIndex];
    }
}
