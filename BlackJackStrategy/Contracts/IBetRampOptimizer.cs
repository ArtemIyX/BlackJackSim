using BlackJackStrategy.Models;

namespace BlackJackStrategy.Contracts;

public interface IBetRampOptimizer
{
    BetRampOptimizationResult Optimize(BetRampOptimizationConfig config);
}
