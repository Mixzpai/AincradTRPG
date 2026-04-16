# AincradTRPG ASCII VERSION

A Sword Art Online–themed ASCII roguelike built in C# / .NET 8 with [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui).

Climb all 100 floors of Aincrad. Die, and it's game over — no second try. Beat the death game.

## Features

- **100 floors** of procedurally generated dungeons, towns, and labyrinths
- **RGB-lit ASCII** rendering with a shadowcaster FOV and dynamic lighting
- **Named canon weapons and bosses** pulled from the Aincrad arc, Progressive novels, and SAO games
- **Sword Skills system** with weapon proficiency, Outside System Skills, and Unique Skills
- **Biomes, weather, and day/night cycle** that change how the floor plays
- **Crafting, cooking, field bosses, bounties**, faction reputation, and seasonal events
- **Run Modifiers** — stack challenge modifiers for a score multiplier (Naked Ingress, Laughing Coffin, Heathcliff's Gauntlet, and more)

## Run it

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
cd SAOTRPG
dotnet run
```

That's it. The game launches in your terminal.

## Controls

- Arrow keys / WASD — move
- Space — wait / attack adjacent
- `i` — inventory
- `p` — character sheet
- `m` — minimap
- `?` — full keybind list in-game

A full keyboard-only game. No mouse required.

## Requirements

- .NET 8 SDK
- A terminal with 24-bit color and Unicode support (Windows Terminal, iTerm2, Alacritty, WezTerm, kitty, etc.)
- ~60 MB of RAM
- Recommended terminal size: 120×40 or larger

Windows, Linux, and macOS all supported.

## Project layout

```
SAOTRPG/
├── Program.cs              — entry point
├── Entities/               — player, monsters, NPCs, allies
├── Inventory/              — inventory + equipment + stats
├── Items/                  — weapons, armor, food, potions, crystals (data-driven)
├── Map/                    — map generation, biomes, weather, lighting
├── Systems/                — turn manager, combat, AI, quests, skills, save/load
└── UI/                     — screens, dialogs, minimap, overlays
```

## License

See `LICENSE` — MIT.
