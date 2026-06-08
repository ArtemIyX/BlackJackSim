using BlackJackData.Enums;
using BlackJackData.Rules;

namespace BlackJackStrategy.Solvers;

public sealed class InfiniteDeckBasicStrategySolver
{
    private static readonly (int Value, double Probability)[] CardDistribution =
    [
        (2, 1d / 13d),
        (3, 1d / 13d),
        (4, 1d / 13d),
        (5, 1d / 13d),
        (6, 1d / 13d),
        (7, 1d / 13d),
        (8, 1d / 13d),
        (9, 1d / 13d),
        (10, 4d / 13d),
        (11, 1d / 13d)
    ];

    private readonly BlackjackRules _rules;
    private readonly Dictionary<PlayerStateKey, ActionEv> _playerCache = new();
    private readonly Dictionary<DealerStartKey, DealerOutcomeDistribution> _dealerCache = new();

    public InfiniteDeckBasicStrategySolver(BlackjackRules rules)
    {
        _rules = rules;
    }

    public PlayerActionType SolveHardAction(int total, int dealerValue)
    {
        var state = CreateHardState(total, dealerValue);
        return GetBestAction(state).Action;
    }

    public PlayerActionType SolveSoftAction(int total, int dealerValue)
    {
        var state = CreateSoftState(total, dealerValue);
        return GetBestAction(state).Action;
    }

    public PlayerActionType SolvePairAction(int pairValue, int dealerValue)
    {
        var state = CreatePairState(pairValue, dealerValue);
        return GetBestPairAction(state).Action;
    }

    private ActionEv GetBestAction(PlayerStateKey state)
    {
        if (_playerCache.TryGetValue(state, out var cached))
        {
            return cached;
        }

        var candidates = new List<ActionEv>
        {
            new(PlayerActionType.Stand, EvaluateStand(state))
        };

        if (state.CanHit)
        {
            candidates.Add(new ActionEv(PlayerActionType.Hit, EvaluateHit(state)));
        }

        if (state.CanDouble && IsDoubleAllowed(state))
        {
            candidates.Add(new ActionEv(PlayerActionType.Double, EvaluateDouble(state)));
        }

        if (state.CanSurrender && _rules.SurrenderRule != SurrenderRule.None)
        {
            candidates.Add(new ActionEv(PlayerActionType.Surrender, -0.5d));
        }

        var best = SelectBest(candidates);
        _playerCache[state] = best;
        return best;
    }

    private ActionEv GetBestPairAction(PlayerStateKey pairState)
    {
        var baseState = pairState with
        {
            PairValue = 0,
            CanSurrender = true,
            CanDouble = true
        };

        var candidates = new List<ActionEv>
        {
            GetBestAction(baseState)
        };

        if (pairState.PairValue > 0)
        {
            candidates.Add(new ActionEv(PlayerActionType.Split, EvaluateSplit(pairState)));
        }

        return SelectBest(candidates);
    }

    private double EvaluateStand(PlayerStateKey state)
    {
        if (state.IsBusted)
        {
            return -1d;
        }

        var playerTotal = state.BestTotal;
        var distribution = GetDealerDistribution(state.DealerUpValue, state.DealerBlackjackKnownAbsent);

        if (state.BlackjackEligible && playerTotal == 21)
        {
            return distribution.BlackjackProbability * 0d +
                   (1d - distribution.BlackjackProbability) * (double)_rules.BlackjackPayout;
        }

        var ev = -distribution.BlackjackProbability;
        ev += distribution.BustProbability;

        for (var total = 17; total <= 21; total++)
        {
            var probability = distribution.GetProbability(total);
            if (playerTotal > total)
            {
                ev += probability;
            }
            else if (playerTotal < total)
            {
                ev -= probability;
            }
        }

        return ev;
    }

    private double EvaluateHit(PlayerStateKey state)
    {
        var ev = 0d;
        foreach (var (cardValue, probability) in CardDistribution)
        {
            var next = DrawCard(
                state,
                cardValue,
                canDouble: false,
                canSurrender: false,
                canHit: true,
                blackjackEligible: false,
                pairValue: 0);

            ev += probability * (next.IsBusted ? -1d : GetBestAction(next).Ev);
        }

        return ev;
    }

    private double EvaluateDouble(PlayerStateKey state)
    {
        var ev = 0d;
        foreach (var (cardValue, probability) in CardDistribution)
        {
            var next = DrawCard(
                state,
                cardValue,
                canDouble: false,
                canSurrender: false,
                canHit: false,
                blackjackEligible: false,
                pairValue: 0);

            ev += probability * (next.IsBusted ? -2d : 2d * EvaluateStand(next));
        }

        return ev;
    }

    private double EvaluateSplit(PlayerStateKey state)
    {
        var isAcePair = state.PairValue == 11;
        var canHitAfterSplit = !isAcePair || _rules.AllowHitSplitAces;
        var canDoubleAfterSplit = _rules.AllowDoubleAfterSplit && canHitAfterSplit && IsDoubleRuleCompatibleForSplit(state.PairValue);

        var handEv = 0d;
        foreach (var (cardValue, probability) in CardDistribution)
        {
            var next = CreateSplitHandState(
                originalPairValue: state.PairValue,
                drawnCardValue: cardValue,
                dealerValue: state.DealerUpValue,
                dealerBlackjackKnownAbsent: state.DealerBlackjackKnownAbsent,
                canDoubleAfterSplit: canDoubleAfterSplit,
                canHit: canHitAfterSplit);

            handEv += probability * (canHitAfterSplit ? GetBestAction(next).Ev : EvaluateStand(next));
        }

        return handEv * 2d;
    }

    private DealerOutcomeDistribution GetDealerDistribution(int dealerUpValue, bool dealerBlackjackKnownAbsent)
    {
        var key = new DealerStartKey(dealerUpValue, dealerBlackjackKnownAbsent);
        if (_dealerCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var hiddenCards = GetHiddenCardDistribution(dealerUpValue, dealerBlackjackKnownAbsent);
        var distribution = DealerOutcomeDistribution.Empty;

        foreach (var (hiddenValue, probability) in hiddenCards)
        {
            var state = CreateDealerState(dealerUpValue, hiddenValue, cardCount: 2);

            if (state.BestTotal == 21 && state.CardCount == 2)
            {
                distribution.BlackjackProbability += probability;
                continue;
            }

            distribution += probability * ResolveDealerState(state);
        }

        _dealerCache[key] = distribution;
        return distribution;
    }

    private DealerOutcomeDistribution ResolveDealerState(DealerStateKey state)
    {
        if (state.BestTotal > 21)
        {
            return DealerOutcomeDistribution.WithBust(1d);
        }

        if (state.BestTotal < 17 ||
            (_rules.DealerHitRule == DealerHitRule.HitSoft17 && state.BestTotal == 17 && state.IsSoft))
        {
            var distribution = DealerOutcomeDistribution.Empty;
            foreach (var (cardValue, probability) in CardDistribution)
            {
                distribution += probability * ResolveDealerState(DrawDealerCard(state, cardValue));
            }

            return distribution;
        }

        return DealerOutcomeDistribution.WithTotal(state.BestTotal, 1d);
    }

    private static ActionEv SelectBest(IReadOnlyList<ActionEv> candidates)
    {
        var best = candidates[0];
        for (var index = 1; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            if (candidate.Ev > best.Ev + 1e-12)
            {
                best = candidate;
                continue;
            }

            if (Math.Abs(candidate.Ev - best.Ev) <= 1e-12 && Preference(candidate.Action) < Preference(best.Action))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static int Preference(PlayerActionType action)
    {
        return action switch
        {
            PlayerActionType.Surrender => 0,
            PlayerActionType.Split => 1,
            PlayerActionType.Double => 2,
            PlayerActionType.Stand => 3,
            PlayerActionType.Hit => 4,
            _ => 9
        };
    }

    private bool IsDoubleAllowed(PlayerStateKey state)
    {
        return _rules.DoubleDownRule switch
        {
            DoubleDownRule.AnyTwoCards => true,
            DoubleDownRule.HardNineToElevenOnly => !state.IsSoft && state.BestTotal is >= 9 and <= 11,
            DoubleDownRule.HardTenToElevenOnly => !state.IsSoft && state.BestTotal is >= 10 and <= 11,
            _ => false
        };
    }

    private bool IsDoubleRuleCompatibleForSplit(int pairValue)
    {
        var state = CreatePairState(pairValue, dealerValue: 2);
        return _rules.DoubleDownRule switch
        {
            DoubleDownRule.AnyTwoCards => true,
            DoubleDownRule.HardNineToElevenOnly => pairValue != 11 && pairValue * 2 is >= 9 and <= 11,
            DoubleDownRule.HardTenToElevenOnly => pairValue != 11 && pairValue * 2 is >= 10 and <= 11,
            _ => false
        };
    }

    private static IEnumerable<(int Value, double Probability)> GetHiddenCardDistribution(int dealerUpValue, bool excludeBlackjack)
    {
        if (!excludeBlackjack)
        {
            return CardDistribution;
        }

        if (dealerUpValue == 11)
        {
            return CardDistribution
                .Where(card => card.Value != 10)
                .Select(card => (card.Value, card.Probability / (9d / 13d)));
        }

        if (dealerUpValue == 10)
        {
            return CardDistribution
                .Where(card => card.Value != 11)
                .Select(card => (card.Value, card.Probability / (12d / 13d)));
        }

        return CardDistribution;
    }

    private PlayerStateKey CreateHardState(int total, int dealerValue)
    {
        return new PlayerStateKey(
            HardTotal: total,
            IsSoft: false,
            DealerUpValue: dealerValue,
            CardCount: 2,
            CanDouble: true,
            CanSurrender: true,
            CanHit: true,
            BlackjackEligible: false,
            PairValue: 0,
            DealerBlackjackKnownAbsent: DealerBlackjackKnownAbsent(dealerValue));
    }

    private PlayerStateKey CreateSoftState(int total, int dealerValue)
    {
        return new PlayerStateKey(
            HardTotal: total - 10,
            IsSoft: true,
            DealerUpValue: dealerValue,
            CardCount: 2,
            CanDouble: true,
            CanSurrender: true,
            CanHit: true,
            BlackjackEligible: total == 21,
            PairValue: 0,
            DealerBlackjackKnownAbsent: DealerBlackjackKnownAbsent(dealerValue));
    }

    private PlayerStateKey CreatePairState(int pairValue, int dealerValue)
    {
        var isAcePair = pairValue == 11;
        return new PlayerStateKey(
            HardTotal: isAcePair ? 2 : pairValue * 2,
            IsSoft: isAcePair,
            DealerUpValue: dealerValue,
            CardCount: 2,
            CanDouble: true,
            CanSurrender: true,
            CanHit: true,
            BlackjackEligible: false,
            PairValue: pairValue,
            DealerBlackjackKnownAbsent: DealerBlackjackKnownAbsent(dealerValue));
    }

    private bool DealerBlackjackKnownAbsent(int dealerValue)
    {
        return _rules.DealerPeeksForBlackjack && dealerValue is 10 or 11;
    }

    private static PlayerStateKey DrawCard(
        PlayerStateKey state,
        int cardValue,
        bool canDouble,
        bool canSurrender,
        bool canHit,
        bool blackjackEligible,
        int pairValue)
    {
        var hardTotal = state.HardTotal + CardAsHardValue(cardValue);
        var isSoft = state.IsSoft;

        if (cardValue == 11 && hardTotal + 10 <= 21)
        {
            isSoft = true;
        }

        if (isSoft && hardTotal + 10 > 21)
        {
            isSoft = false;
        }

        return state with
        {
            HardTotal = hardTotal,
            IsSoft = isSoft,
            CardCount = state.CardCount + 1,
            CanDouble = canDouble,
            CanSurrender = canSurrender,
            CanHit = canHit,
            BlackjackEligible = blackjackEligible,
            PairValue = pairValue
        };
    }

    private static PlayerStateKey CreateSplitHandState(
        int originalPairValue,
        int drawnCardValue,
        int dealerValue,
        bool dealerBlackjackKnownAbsent,
        bool canDoubleAfterSplit,
        bool canHit)
    {
        var hardTotal = CardAsHardValue(originalPairValue) + CardAsHardValue(drawnCardValue);
        var isSoft = originalPairValue == 11 || drawnCardValue == 11;

        if (isSoft && hardTotal + 10 > 21)
        {
            isSoft = false;
        }

        return new PlayerStateKey(
            HardTotal: hardTotal,
            IsSoft: isSoft,
            DealerUpValue: dealerValue,
            CardCount: 2,
            CanDouble: canDoubleAfterSplit,
            CanSurrender: false,
            CanHit: canHit,
            BlackjackEligible: false,
            PairValue: 0,
            DealerBlackjackKnownAbsent: dealerBlackjackKnownAbsent);
    }

    private static DealerStateKey CreateDealerState(int upCardValue, int hiddenValue, int cardCount)
    {
        var hardTotal = CardAsHardValue(upCardValue) + CardAsHardValue(hiddenValue);
        var isSoft = upCardValue == 11 || hiddenValue == 11;
        if (isSoft && hardTotal + 10 > 21)
        {
            isSoft = false;
        }

        return new DealerStateKey(hardTotal, isSoft, cardCount);
    }

    private static DealerStateKey DrawDealerCard(DealerStateKey state, int cardValue)
    {
        var hardTotal = state.HardTotal + CardAsHardValue(cardValue);
        var isSoft = state.IsSoft;

        if (cardValue == 11 && hardTotal + 10 <= 21)
        {
            isSoft = true;
        }

        if (isSoft && hardTotal + 10 > 21)
        {
            isSoft = false;
        }

        return new DealerStateKey(hardTotal, isSoft, state.CardCount + 1);
    }

    private static int CardAsHardValue(int cardValue)
    {
        return cardValue == 11 ? 1 : cardValue;
    }

    private readonly record struct PlayerStateKey(
        int HardTotal,
        bool IsSoft,
        int DealerUpValue,
        int CardCount,
        bool CanDouble,
        bool CanSurrender,
        bool CanHit,
        bool BlackjackEligible,
        int PairValue,
        bool DealerBlackjackKnownAbsent)
    {
        public int BestTotal => HardTotal + (IsSoft ? 10 : 0);

        public bool IsBusted => BestTotal > 21;
    }

    private readonly record struct DealerStateKey(int HardTotal, bool IsSoft, int CardCount)
    {
        public int BestTotal => HardTotal + (IsSoft ? 10 : 0);
    }

    private readonly record struct DealerStartKey(int DealerUpValue, bool DealerBlackjackKnownAbsent);

    private readonly record struct ActionEv(PlayerActionType Action, double Ev);

    private struct DealerOutcomeDistribution
    {
        private readonly double[] _totals;

        public DealerOutcomeDistribution()
        {
            _totals = new double[22];
        }

        public double BustProbability { get; set; }

        public double BlackjackProbability { get; set; }

        public double GetProbability(int total) => _totals[total];

        public static DealerOutcomeDistribution Empty => new();

        public static DealerOutcomeDistribution WithBust(double probability)
        {
            return new DealerOutcomeDistribution { BustProbability = probability };
        }

        public static DealerOutcomeDistribution WithTotal(int total, double probability)
        {
            var distribution = new DealerOutcomeDistribution();
            distribution._totals[total] = probability;
            return distribution;
        }

        public static DealerOutcomeDistribution operator +(DealerOutcomeDistribution left, DealerOutcomeDistribution right)
        {
            var distribution = new DealerOutcomeDistribution
            {
                BustProbability = left.BustProbability + right.BustProbability,
                BlackjackProbability = left.BlackjackProbability + right.BlackjackProbability
            };

            for (var total = 0; total < distribution._totals.Length; total++)
            {
                distribution._totals[total] = left._totals[total] + right._totals[total];
            }

            return distribution;
        }

        public static DealerOutcomeDistribution operator *(double multiplier, DealerOutcomeDistribution distribution)
        {
            var scaled = new DealerOutcomeDistribution
            {
                BustProbability = distribution.BustProbability * multiplier,
                BlackjackProbability = distribution.BlackjackProbability * multiplier
            };

            for (var total = 0; total < scaled._totals.Length; total++)
            {
                scaled._totals[total] = distribution._totals[total] * multiplier;
            }

            return scaled;
        }
    }
}
