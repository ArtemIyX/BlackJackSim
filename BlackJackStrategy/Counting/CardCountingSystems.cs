using BlackJackStrategy.Contracts;
using BlackJackStrategy.Counting.Systems;

namespace BlackJackStrategy.Counting;

public static class CardCountingSystems
{
    public static IReadOnlyList<string> Names => CreateFactories().Keys.OrderBy(name => name).ToArray();

    public static IReadOnlyList<ICardCountingSystem> CreateAll()
    {
        return CreateFactories().Values.Select(factory => factory()).ToArray();
    }

    public static ICardCountingSystem Create(string name)
    {
        var factories = CreateFactories();
        if (!factories.TryGetValue(name, out var factory))
        {
            throw new ArgumentException($"Unknown counting system '{name}'.", nameof(name));
        }

        return factory();
    }

    private static Dictionary<string, Func<ICardCountingSystem>> CreateFactories()
    {
        return new Dictionary<string, Func<ICardCountingSystem>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Hi-Lo"] = () => new HiLoCountingSystem(),
            ["Knock-Out"] = () => new KnockOutCountingSystem(),
            ["Red Seven"] = () => new RedSevenCountingSystem(),
            ["Zen Count"] = () => new ZenCountCountingSystem(),
            ["Omega II"] = () => new OmegaIICountingSystem(),
            ["Hi-Opt I"] = () => new HiOptICountingSystem(),
            ["Hi-Opt II"] = () => new HiOptIICountingSystem(),
            ["Wong Halves"] = () => new WongHalvesCountingSystem(),
            ["Ace/Five"] = () => new AceFiveCountingSystem()
        };
    }
}
