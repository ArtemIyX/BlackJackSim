# BlackJackSolver

A C#/.NET blackjack sandbox for:

- playing blackjack manually in the console
- simulating large numbers of rounds
- generating basic-strategy tables from an EV solver
- testing counting systems and bet ramps
- optimizing count-based betting ramps
- experimenting with back counting / wonging-in

The solution is split into small projects so the domain, engine, strategies, and console apps can evolve independently.

## Disclaimer

> This repository is for educational, research, and simulation purposes only.

- It is not financial advice.
- It is not legal advice.
- It is not encouragement to gamble.
- Real casinos have rules, countermeasures, and legal/policy restrictions that vary by jurisdiction and venue.
- Even strong blackjack play and card counting involve real risk, variance, and the possibility of losing money.

> If you use ideas from this repository in the real world, you are responsible for understanding and following all local laws, casino rules, and personal risk limits.

## Practical Observations

Some broad takeaways from simulation and strategy analysis in blackjack:

- Without disciplined strategy and betting rules, a player will usually lose their bankroll over time.
- With basic-strategy style EV evaluation, the house edge can be pushed down significantly, sometimes into roughly the `-0.2%` range under favorable rules.
- Adding counting systems and count-based bet sizing can improve that further, sometimes closer to around `-0.1% EV`.
- With favorable penetration, selective entry, back counting, and short-run variance, some simulation runs can get close to `-0.05% EV` or even approach break-even for a period.

There are also important real-world limits:

- Many online casinos do not offer conditions that make counting attractive.
- Penetration is often shallow, sometimes around `50%`, which cuts away much of the value of card counting.
- Some casinos and game formats also make back counting impractical or directly limit it.

Blackjack is still structurally asymmetric:

- the player acts before the dealer
- the player can bust before the dealer completes the hand

That built-in disadvantage is a big reason blackjack tends to stay negative EV unless the rules and conditions are especially favorable.

Even when the expected value is improved, variance remains high. Large drawdowns are normal, and a strategy that looks close to break-even on paper can still experience long losing stretches in practice.

## What Is Included

- `BlackJackData`
  Shared domain layer: cards, rules, round state, actions, results, and value objects.

- `BlackJackEngine`
  Deterministic round engine and shoe/session logic.

- `BlackJackStrategy`
  Strategies, counting systems, EV-based basic-strategy table generation, simulation runner, and bet-ramp optimizer.

- `BlackJackConsole`
  Manual console game against the dealer.

- `BlackJackConsoleRunner`
  Strategy simulation console with summary statistics and optional back counting.

- `BlackJackConsoleOptimizer`
  Console app for optimizing true-count bet ramps for supported counting systems.

- `BlackJackTests`
  xUnit + FluentAssertions test suite.

## Current Features

- Configurable blackjack rules via `BlackjackRules`
- Deterministic round engine with:
  - hit
  - stand
  - double
  - split
  - surrender
  - insurance
- Persistent shoes with configurable penetration and reshuffle behavior
- Manual play console that hides the dealer hole card until reveal time
- Simulation runner with bankroll and round statistics
- Back counting mode with:
  - 5 background players
  - basic-strategy background seats
  - counting while sitting out
  - entering only when the ramp calls for a stronger bet
- Basic strategy bot backed by generated strategy tables
- Infinite-deck EV solver used to generate:
  - hard-total table
  - soft-total table
  - pair-splitting table
- Card counting systems:
  - Hi-Lo
  - Knock-Out
  - Red Seven
  - Zen Count
  - Omega II
  - Hi-Opt I
  - Hi-Opt II
  - Wong Halves
  - Ace/Five
- Bet ramps:
  - flat betting
  - true-count step ramps
- Multithreaded bet-ramp optimizer with deterministic seeded runs

## Requirements

- .NET 10 SDK

## Build And Test

```bash
dotnet build BlackJackSolver.sln
dotnet test BlackJackSolver.sln
```

## Quick Start

### 1. Play Manually

```bash
dotnet run --project BlackJackConsole
```

This lets you play against the dealer in the terminal, shows your cards and totals, and keeps the dealer hole card hidden until it should be revealed.

### 2. Run Strategy Simulations

```bash
dotnet run --project BlackJackConsoleRunner
```

The runner currently uses:

- `BasicStrategyBot` for play decisions
- `WongHalvesCountingSystem`
- a `TrueCountStepBetRamp`

It reports metrics such as:

- rounds played
- hands played
- total wagered
- net payout
- ROI
- average return per round / hand
- drawdown
- win / loss / push counts
- blackjacks, doubles, splits, insurance
- rounds sat out
- participation rate

If you enable `Back counting`, the runner adds 5 background players using `BasicStrategyBot`. Your counting bot watches the table, stays out during weak counts, and enters only when the configured ramp produces at least a stronger bet.

### 3. Optimize Bet Ramps

```bash
dotnet run --project BlackJackConsoleOptimizer
```

The optimizer lets you choose:

- counting system
- deck count
- penetration
- true-count thresholds
- allowed unit sizes
- bankroll and table minimum
- random seed

It evaluates many monotonic `TrueCountStepBetRamp` candidates and ranks the best results by simulated performance.

This works especially well for systems like `Wong Halves`, where fractional thresholds such as `0.5,1.0,1.5,2.0,2.5` are meaningful.

## Example Bet Ramp

```csharp
new TrueCountStepBetRamp(
[
    new BetRampStep(1.5d, 2m),
    new BetRampStep(2.5d, 6m)
],
fallbackUnits: 1m)
```

Meaning:

- below `TC 1.5` -> `1u`
- `TC >= 1.5` -> `2u`
- `TC >= 2.5` -> `6u`

## Architecture Notes

The dependency direction is intentionally simple:

- `BlackJackData` defines the nouns
- `BlackJackEngine` defines the round flow
- `BlackJackStrategy` defines the decision-making and simulation layer
- console projects provide UX only

This makes it easier to:

- reuse the engine across manual play and simulation
- test round behavior deterministically
- compare strategies under the same rules and shoe conditions

## Strategy Notes

`BasicStrategyBot` is table-driven, and those tables are generated in memory from the infinite-deck EV solver rather than being handwritten static matrices.

That means the repo already supports:

- rules-aware table generation
- simulation of solver-generated basic strategy
- count-driven betting layered on top of basic strategy

## Current Limitations

- The current basic-strategy table generation is based on an infinite-deck EV model, not full finite-shoe composition-dependent analysis.
- Background seats in back-counting mode currently use fixed basic-strategy bots.
- The console runner is currently opinionated toward one counting setup rather than being a full strategy-selection UI.

## Roadmap Ideas

- finite-deck EV solver
- composition-dependent strategy
- counting deviations for play decisions
- multiple playable/counting bots selectable from console apps
- richer bankroll and risk-of-ruin analysis
- multi-hand optimization at high counts

## Test Status

The repository includes an automated test suite covering engine flow, simulation behavior, counting systems, optimizer behavior, and strategy logic.

## License
