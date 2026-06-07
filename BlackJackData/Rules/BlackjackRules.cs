using BlackJackData.Enums;

namespace BlackJackData.Rules;

public sealed record BlackjackRules(
    int DeckCount,
    decimal BlackjackPayout,
    decimal InsurancePayout,
    DealerHitRule DealerHitRule,
    DoubleDownRule DoubleDownRule,
    SurrenderRule SurrenderRule,
    bool DealerPeeksForBlackjack,
    bool AllowInsurance,
    bool AllowDoubleAfterSplit,
    bool AllowResplitHands,
    bool AllowResplitAces,
    bool AllowHitSplitAces,
    int MaxHandsPerSeat,
    double ShoePenetration)
{
    public static BlackjackRules Default { get; } = new(
        DeckCount: 6,
        BlackjackPayout: 1.5m,
        InsurancePayout: 2.0m,
        DealerHitRule: DealerHitRule.StandOnSoft17,
        DoubleDownRule: DoubleDownRule.AnyTwoCards,
        SurrenderRule: SurrenderRule.Late,
        DealerPeeksForBlackjack: true,
        AllowInsurance: true,
        AllowDoubleAfterSplit: true,
        AllowResplitHands: true,
        AllowResplitAces: false,
        AllowHitSplitAces: false,
        MaxHandsPerSeat: 4,
        ShoePenetration: 0.75d);
}
