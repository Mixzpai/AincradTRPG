# AincradTRPG

A *Sword Art Online*–themed ASCII roguelike for the terminal, built in C# on
[Terminal.Gui](https://github.com/gui-cs/Terminal.Gui).

Climb all 100 floors of Aincrad. Death deletes the save — there is no second try.

```
################################
#..........♣....................#
#....@.....♣......k.............#
#..........♣..........Π.........#
#...◇......................◈....#
################################
```

## What it is

A turn-based roguelike where the whole world is a fixed character grid. Every floor is generated
from a seed, so the same seed always produces the same world — and the run ends for good when you
die. The presentation leans on 24-bit colour, a shadowcasting field of view and per-tile lighting
rather than on sprites; the player is `@`, as tradition requires.

The SAO setting is played straight: canonical floor bosses, named weapons, the sword-skill system,
and the towns you would expect where you would expect them. There is no blood — combat resolves in
light and polygon shatter, the way the source material does.

## Running it

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd SAOTRPG
dotnet run
```

The game launches in your terminal. A **120×30** terminal is the supported minimum; it is happier
with more. Keyboard only — there are no mouse bindings anywhere.

Useful flags:

```bash
dotnet run -- --perf          # frame/loop timing to debug.log
dotnet run -- --freeze-anim   # pin all wall-clock animation
dotnet run -- --verify-tiles  # diff the incremental map layer against a full recompute
```

## What's in it

| | |
|---|---|
| Floors | 100, each generated from the run seed |
| Biomes | 11, JSON-tunable and hot-reloadable at runtime |
| Room prefabs | 102 hand-authored ASCII grids |
| Monsters | 66 species, 100 floor bosses, 34 field bosses |
| Items | 549 registered definitions |
| Sword skills | 123 across 17 weapon types, plus Unique Skills |
| Milestones | 337 — achievements, titles, life-skill ranks, a collection log |
| In-game guide | 251 topics, searchable, unlocked as you climb |

Beyond the climb: crafting and weapon evolution, enhancement and refinement, cooking, mining and
other life skills, guilds and faction reputation, karma, quests, party members, field bosses,
weather and a day/night cycle, and stackable run modifiers that raise both the difficulty and the
score multiplier.

## Layout

```
SAOTRPG/          the game
  Entities/       player, monsters, NPCs
  Items/          registry and item definitions
  Inventory/      equipment slots and stat aggregation
  Map/            generation pipeline, tiles, FOV, biomes
  Systems/        turn manager, combat, skills, progression
  UI/             Terminal.Gui screens, dialogs and the map renderer
  Content/        biome JSON and room prefabs
Tools/            offline verification harnesses (see below)
```

`TurnManager`, `MapView` and `MapGenerator` are partial classes split across many files, one
concern per file.

## Verification

The game is checked by a suite of offline probes rather than a unit-test project. They drive the
real systems — a real `TurnManager`, a real `MapView`, real dialogs — and each exists because of a
specific class of failure that is silent at runtime: content that parses but is never consumed,
a milestone that can never be earned, an item that can never be obtained, a map layer that goes
stale, a key path that renders but cannot be reached.

```bash
cd Tools/ContentProbe   && dotnet run   # content integrity
cd Tools/SeedProbe      && dotnet run   # run reproducibility
cd Tools/ProgressProbe  && dotnet run   # run progression and permadeath
cd Tools/TileVerifyProbe && dotnet run  # map render correctness
cd Tools/DialogKeyProbe && dotnet run   # dialog key paths
cd Tools/GuideAudit     && dotnet run   # in-game guide invariants
cd Tools/CodeAudit      && python invariants.py
```

All of them exit non-zero on failure.

## Status

Playable end to end and under active development. Saves are not migrated between versions — a
change to generation or to save structure means starting a fresh run, which is by design rather
than an oversight.

## Credits

Sword Art Online is the work of Reki Kawahara. This is a non-commercial fan project and is not
affiliated with or endorsed by the rights holders.
