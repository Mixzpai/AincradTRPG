using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Systems;

namespace SAOTRPG.Map;

// Entity population (mobs, bosses, vendors, NPCs, chests) and terrain helpers.
public static partial class MapGenerator
{
    public static void PopulateFloor(GameMap map, List<Room> rooms, Player player,
        int floor, int statScale = 100, HashSet<string>? defeatedFieldBosses = null,
        bool skipFieldBosses = false, Random? rng = null)
    {
        // Derive a deterministic per-floor Random from the persisted master seed
        // when callers don't supply one. Keeps F9 hot-reload reproducible.
        rng ??= new Random(CurrentGlobalSeed ^ floor);
        UI.DebugLogger.LogGame("POPULATE", $"PopulateFloor({floor}) — {rooms.Count} rooms, map {map.Width}x{map.Height}");
        if (rooms.Count == 0) return;

        // Player at spawn clearing
        map.PlaceEntity(player, rooms[0].CenterX, rooms[0].CenterY);

        // F1 TOB — dedicated NPC/mob layout; skips random den/chest scatter.
        if (floor == 1)
        {
            PopulateTownOfBeginnings(map, player, floor, statScale, rng);
            return;
        }

        // Mobs in rooms (skip spawn room; skip rooms inside the safe zone)
        for (int i = 1; i < rooms.Count; i++)
        {
            if (IsRoomInSafeZone(map, rooms[i])) continue;
            int mobCount = rng.Next(1, 4);
            for (int j = 0; j < mobCount; j++)
            {
                var (mx, my) = FindOpenSpot(map, rooms[i], rng);
                if (mx < 0 || IsInSafeZone(map, mx, my)) continue;
                var mob = MobFactory.CreateFloorMob(floor, statScale);
                map.PlaceEntity(mob, mx, my);
            }
        }

        // Wandering mobs between rooms — more on larger floors
        int wanderers = FloorScale.WanderingMobs(floor);
        for (int i = 0; i < wanderers; i++)
        {
            var (wx, wy) = FindOpenSpotOutsideSafeZone(map, rng);
            if (wx < 0) continue;
            var mob = MobFactory.CreateFloorMob(floor, statScale);
            map.PlaceEntity(mob, wx, wy);
        }

        // Every floor has a unique boss (lives in the labyrinth boss room).
        if (rooms.Count > 1)
        {
            var bossRoom = rooms[^1];
            var boss = floor >= 100
                ? BossFactory.CreatePlayerClone(player)
                : BossFactory.CreateFloorBoss(floor);
            map.PlaceEntity(boss, bossRoom.CenterX, bossRoom.CenterY);
        }

        // Field bosses — wilderness named elites. Skipped when defeated this run, seasonal inactive, or skipFieldBosses=true.
        if (!skipFieldBosses)
        {
            var defeatedSet = defeatedFieldBosses ?? new HashSet<string>();
            var seasonal = SeasonalEvents.GetActive();
            foreach (var fieldBoss in FieldBossFactory.GetActiveForFloor(floor, defeatedSet, seasonal))
            {
                var (fx, fy) = FindOpenSpotOutsideSafeZone(map, rng);
                if (fx < 0) continue;
                map.PlaceEntity(fieldBoss, fx, fy);
            }
        }

        // Vendor
        if (rooms.Count > 2)
        {
            var vendorRoom = rooms[rng.Next(1, rooms.Count)];
            var (vx, vy) = FindOpenSpot(map, vendorRoom, rng);
            if (vx >= 0)
            {
                var vendor = new Vendor();
                vendor.ShopName = floor <= 5 ? "General Store" : floor <= 10 ? "Adventurer's Supply" : "Elite Outfitters";
                vendor.GenerateStock(floor);
                map.PlaceEntity(vendor, vx, vy);
            }
        }

        // Wandering NPCs — 1-2 per floor with tips or small trades
        int wandererCount = 1 + rng.Next(0, 2);
        for (int i = 0; i < wandererCount; i++)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = rng.Next(10, map.Width - 10), y = rng.Next(10, map.Height - 10);
                if (IsWalkableType(map.Tiles[x, y].Type) && map.Tiles[x, y].Occupant == null
                    && Math.Abs(x - rooms[0].CenterX) > 8 && Math.Abs(y - rooms[0].CenterY) > 8)
                {
                    var npc = WandererFactory.CreateWanderer(floor);
                    map.PlaceEntity(npc, x, y);
                    break;
                }
            }
        }

        // Progressive-canon NPCs (Ran/Klein/Argo): gated by floor + room count. Order must match RNG consumption order.
        PlaceNpcsFromTable(map, rooms, floor, rng);

        // Monster dens — scaled to floor area
        int denCount = FloorScale.DenCount(floor);
        for (int di = 0; di < denCount; di++)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int dx = rng.Next(15, map.Width - 20), dy = rng.Next(15, map.Height - 20);
                double dist = Math.Sqrt((dx - rooms[0].CenterX) * (dx - rooms[0].CenterX)
                    + (dy - rooms[0].CenterY) * (dy - rooms[0].CenterY));
                if (dist < 15) continue;
                if (IsInSafeZone(map, dx + 2, dy + 2)) continue;
                BuildStructure(map, dx, dy, 5, 5);
                var denRoom = new Room(dx, dy, 5, 5);
                rooms.Add(denRoom);
                if (map.InBounds(dx + 2, dy + 2))
                    map.Tiles[dx + 2, dy + 2].Type = TileType.Chest;
                int doorX = dx + 2, doorY = dy + 4;
                for (int ddx = -1; ddx <= 1; ddx++)
                    if (map.InBounds(doorX + ddx, doorY + 1))
                        map.Tiles[doorX + ddx, doorY + 1].Type = TileType.DangerZone;
                int denMobCount = 3 + rng.Next(0, 2);
                var firstMob = MobFactory.CreateFloorMob(floor, statScale);
                map.PlaceEntity(firstMob, dx + 1, dy + 1);
                for (int m = 1; m < denMobCount; m++)
                {
                    var (mx, my) = FindOpenSpot(map, denRoom, rng);
                    if (mx < 0) continue;
                    var mob = MobFactory.CreateFloorMob(floor, statScale);
                    map.PlaceEntity(mob, mx, my);
                }
                break;
            }
        }

        // Treasure chests — scaled to floor area
        int chestCount = FloorScale.ChestCount(floor);
        for (int i = 0; i < chestCount; i++)
        {
            var (cx, cy) = FindOpenSpotOutsideSafeZone(map, rng);
            if (cx >= 0)
                map.Tiles[cx, cy].Type = TileType.Chest;
        }

        // Secret Shrines — one per listed floor in a non-spawn room. Hosts T1 chain weapon collected on step-on.
        if (rooms.Count > 3 && Systems.WeaponEvolutionChains.SecretShrineByFloor.ContainsKey(floor))
        {
            // Room index > 2 — shrine avoids spawn area and adjacent corridors.
            int shrineRoomIdx = rng.Next(3, rooms.Count);
            var shrineRoom = rooms[shrineRoomIdx];
            var (sx, sy) = FindOpenSpot(map, shrineRoom, rng);
            if (sx >= 0)
                map.Tiles[sx, sy].Type = TileType.SecretShrine;
        }
    }

    // F1 populator: town NPCs + shopkeeper relative to spawn; wilderness mobs outside the walls.
    private static void PopulateTownOfBeginnings(GameMap map, Player player, int floor, int statScale, Random rng)
    {
        int sx = player.X, sy = player.Y;

        var vendor = new Vendor
        {
            Name = "Agil",
            ShopName = "Agil's General Store",
        };
        vendor.GenerateStock(floor);
        // IF canon: Anneal Blade — iconic starter 1H sword; Agil always stocks one (with markup).
        var anneal = ItemRegistry.Create("anneal_blade");
        if (anneal != null)
        {
            anneal.Value = (int)(anneal.Value * 1.2);
            vendor.ShopStock.Add(anneal);
        }
        TryPlaceEntityNear(map, vendor, sx - 15, sy + 6);

        var klein = new WorldSpawn('K', Color.BrightRed)
        {
            Name = "Klein",
            DialogueLines = new DialogueLine[]
            {
                new("Hey, you! You look like you just woke up in this death game. " +
                    "Don't panic — I'm Klein. Stick with me and you'll survive Floor 1.",
                    new DialogueChoice[]
                    {
                        new("How do I fight?",
                            "Walk into enemies to attack. Your weapon type matters — " +
                            "kill enough with one weapon and you'll unlock Sword Skills. " +
                            "Press F to see your skill list, F1-F4 to use them in combat."),
                        new("What should I do first?",
                            "Explore the town first. Talk to Agil for supplies, visit the Anvil " +
                            "to repair gear. When you're ready, head through the gates into the wilderness. " +
                            "Find the Labyrinth entrance — that's where the floor boss lives."),
                        new("Any survival tips?",
                            "Always carry Health Potions — press 1 to use one fast. " +
                            "Rest with R when out of combat to heal. Watch your hunger bar. " +
                            "And if you see a glowing enemy? That's an Elite. Be careful."),
                    }),
                new("One more thing — the Labyrinth is where the floor boss waits. " +
                    "You HAVE to beat it to reach Floor 2. Good luck out there!"),
            },
        };
        TryPlaceEntityNear(map, klein, sx, sy - 12);

        var argo = new WorldSpawn('A', Color.BrightYellow)
        {
            Name = "Argo the Rat",
            DialogueLines = new DialogueLine[]
            {
                new("Psst! I'm Argo — information broker. First tip's free.",
                    new DialogueChoice[]
                    {
                        new("Where's the boss?",
                            "Look for the Labyrinth entrance — it's a big stone archway, usually in " +
                            "a corner of the map. You can see it on the minimap as a cyan Π symbol."),
                        new("How do I get stronger?",
                            "Fight with the same weapon type to build proficiency. " +
                            "Higher proficiency = more damage + new Sword Skills. " +
                            "Also check shops for better gear as you climb floors."),
                        new("What's the catch?",
                            "Die here and you lose everything — save gets wiped clean. " +
                            "Aincrad doesn't hand out second chances, yeah? So don't die."),
                    }),
                new("That's all the freebies you get. Next time, bring Col!"),
            },
        };
        TryPlaceEntityNear(map, argo, sx - 2, sy);

        var priest = new WorldSpawn('P', Color.BrightCyan)
        {
            Name = "Priest Tadashi",
            Dialogue = "May your blade find true purpose, traveler. The monument remembers every name.",
        };
        TryPlaceEntityNear(map, priest, sx, sy + 10);

        var smith = new WorldSpawn('B', Color.Yellow)
        {
            Name = "Nezha the Smith",
            Dialogue = "Bring me a broken blade and I'll see what I can do. Anvil's behind me.",
        };
        TryPlaceEntityNear(map, smith, sx - 15, sy - 2);

        // Townspeople — wandering flavor NPCs.
        (string Name, char Symbol, Color Color, string Line)[] townfolk =
        {
            ("Lisbeth",  'L', Color.BrightMagenta, "If you find any rare ores out there, bring them to me!"),
            ("Silica",   'S', Color.BrightCyan,    "Have you seen a small blue dragon? I think it flew north."),
            ("Diavel",   'D', Color.BrightBlue,    "A clearing guild is forming. We march on the labyrinth soon."),
        };
        foreach (var (name, sym, color, line) in townfolk)
        {
            var npc = new WorldSpawn(sym, color) { Name = name, Dialogue = line };
            TryPlaceEntityNear(map, npc,
                sx + rng.Next(-20, 21),
                sy + rng.Next(-10, 11));
        }

        // Guild recruitment: Kibaou (ALF) on F1 TOB plaza; others live on HQ floor via FloorNpcSpawns.
        var kibaou = new WorldSpawn('K', Color.BrightYellow)
        {
            Name = "Kibaou",
            Dialogue = "The ALF needs every able sword. Clear some beasts for us and we'll make it official.",
        };
        TryPlaceEntityNear(map, kibaou, sx + 6, sy + 6);

        // Campfire landmark just north of the gate — signals "wilds start here".
        if (map.InBounds(sx, sy - TownHalfH - 4))
            map.Tiles[sx, sy - TownHalfH - 4].Type = TileType.Campfire;

        // Wilderness mobs outside the town walls — scaled to map size.
        int wanderers = FloorScale.WildernessMobsFloor1(floor);
        for (int i = 0; i < wanderers; i++)
        {
            var (wx, wy) = FindOpenSpotOutsideSafeZone(map, rng);
            if (wx < 0) continue;
            var mob = MobFactory.CreateFloorMob(floor, statScale);
            map.PlaceEntity(mob, wx, wy);
        }

        // Town Guard outlaw patrol: activates at karma <= -50; 3-5 guards near spawn. Map regen per entry, so no carryover.
        if (player.Karma <= -50)
        {
            int guardCount = 3 + rng.Next(0, 3);
            for (int i = 0; i < guardCount; i++)
            {
                var guard = MobFactory.CreateTownGuard();
                int ox = rng.Next(-8, 9);
                int oy = rng.Next(-8, 9);
                TryPlaceEntityNear(map, guard, sx + ox, sy + oy);
            }
        }
    }


    // Named NPC spawns on non-town floors. Iteration = RNG call order. Entry: (gate, idxMin, idxMaxExcl, factory).
    private static readonly (Func<int, List<Room>, bool> Gate,
        Func<int, List<Room>, int> IdxMin,
        Func<int, List<Room>, int> IdxMax,
        Func<WorldSpawn> Create)[] FloorNpcSpawns =
    {
        // Ran the Brawler — Progressive-canon F2 Martial Arts trial giver.
        ((f, r) => f == 2 && r.Count > 2,
         (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('R', Color.BrightGreen)
         {
             Name = "Ran the Brawler",
             DialogueLines = new DialogueLine[]
             {
                 new("You want power? Fists only. Defeat five beasts on this floor using nothing but your bare hands. No blade. No bow. Just you.",
                     new DialogueChoice[]
                     {
                         new("Accept the trial.",
                             "Good. Five kills, bare-handed, on this floor. Come back when it's done and I'll show you what your body can truly do."),
                         new("Not interested.",
                             "Suit yourself. The offer stands if you change your mind."),
                         new("Leave.",
                             "Walk well, then."),
                     }),
             },
         }),
        // Klein — floors 2-3 (floor 1 places him at the town gate).
        ((f, r) => f is 2 or 3 && r.Count > 1,
         (f, r) => 1, (f, r) => Math.Min(r.Count, 3),
         () => new WorldSpawn('K', Color.BrightRed)
         {
             Name = "Klein",
             Dialogue = "Hey! Stick close and watch your HP. We'll clear this floor together!",
         }),
        // Argo — floors 3+ (floor 1 places her in the plaza).
        ((f, r) => f >= 3 && r.Count > 2,
         (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('A', Color.BrightYellow)
         {
             Name = "Argo the Rat",
             Dialogue = "Information is power, friend. Want to know what lurks ahead?",
         }),

        // Sister Azariya — F50 Divine Object questline; former Fanatio apprentice, guards Heaven-Piercing Blade.
        ((f, r) => f == 50 && r.Count > 2,
         (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('A', Color.BrightCyan)
         {
             Name = "Sister Azariya",
             Dialogue = "The light does not answer to unsteady hands. Prove yourself and I will entrust it to you.",
         }),

        // Selka the Novice — F65 Divine Object (Fragrant Olive Sword); also hosts chained "Unfolding Truth" awakening quest.
        ((f, r) => f == 65 && r.Count > 2,
         (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('S', Color.White)
         {
             Name = "Selka the Novice",
             Dialogue = "My sister's blade waits for a worthy wielder. Show me you can honor her memory.",
         }),

        // Agil's Apprentice — F55 gates axe_ground_gorge behind a 15-kill quest (replaces old floor-banded pool drop).
        ((f, r) => f == 55 && r.Count > 2,
         (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('G', Color.BrightYellow)
         {
             Name = "Agil's Apprentice",
             Dialogue = "Agil sent me up with a two-hander he swears cleaves the ground. Prove you can swing it.",
         }),

        // Dorothy — F78 Divine Object (Starlight Banner); "Purify the Darkness" 20-kill quest.
        ((f, r) => f == 78 && r.Count > 2,
         (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('D', Color.BrightCyan)
         {
             Name = "Dorothy",
             Dialogue = "Shadow gathers thick on this floor. I need a blade that cleaves darkness, not flesh — are you that blade?",
         }),

        // Scholar Vesper — F89 Divine Object (Satanachia, Scimitar); Goetia-grimoire seal quest.
        ((f, r) => f == 89 && r.Count > 2,
         (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('V', Color.BrightMagenta)
         {
             Name = "Scholar Vesper",
             Dialogue = "The grimoire hungers for a wielder. Break the wards below, and we will see if its pages open to you.",
         }),

        // Yulier (F40) — KoB-era Asuna friend, gives Lambent Light rapier. LN canon.
        ((f, r) => f == 40 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('Y', Color.BrightYellow) { Name = "Yulier",
             Dialogue = "I served beside Asuna once. She gave me her old rapier — but Lambent Light deserves a wielder, not a memorial." }),

        // Jun (F76) — Sleeping Knights survivor; Mother's Rosario handover. LN MR-arc canon.
        ((f, r) => f == 76 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('J', Color.BrightMagenta) { Name = "Jun",
             Dialogue = "Yuuki's sword waits with us. The Sleeping Knights agreed: it goes to the next worthy hand. Are you that hand?" }),

        // ── HF Hollow Mission questgivers (9): each gates a canon HNM weapon behind a kill-count quest.

        ((f, r) => f == 79 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('E', Color.BrightMagenta) { Name = "Scholar Ellroy",
             Dialogue = "The coil-beasts below multiply. I need proof of their fall." }),
        ((f, r) => f == 80 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('H', Color.BrightRed) { Name = "Hunter Kojiro",
             Dialogue = "The ant-knight walks the galleries. Walk them too." }),
        ((f, r) => f == 81 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('T', Color.BrightGreen) { Name = "Ranger Torva",
             Dialogue = "The grove is sick. Thin its ranks and a weapon waits for you." }),
        ((f, r) => f == 83 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('P', Color.BrightYellow) { Name = "Apiarist Nell",
             Dialogue = "The hornets have gone wrong. I hold a blade for whoever ends them." }),
        ((f, r) => f == 88 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('W', Color.BrightCyan) { Name = "Watcher Kael",
             Dialogue = "The shining swarms grow bolder each turn. Break them." }),
        ((f, r) => f == 90 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('O', Color.BrightYellow) { Name = "High Priestess Sola",
             Dialogue = "The holy sword tests resolve, not strength. Prove yours." }),
        ((f, r) => f == 91 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('M', Color.Yellow) { Name = "Torchbearer Meir",
             Dialogue = "The lanterns have gone out. Rekindle them the only way left — in blood." }),
        ((f, r) => f == 95 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('B', Color.BrightRed) { Name = "Elder Beastkeeper",
             Dialogue = "My charges turn on themselves. The sword of the storm cloud is yours if you still them." }),
        ((f, r) => f == 98 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('C', Color.Gray) { Name = "Sentinel Captain",
             Dialogue = "The guardians have fallen, one by one. Hold their line and Gungnir is yours." }),

        // Lisbeth (F48 Lindarth) — canon smithing hub; opens Rarity 6 craft dialog.
        ((f, r) => f == 48 && r.Count > 2, (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('L', Color.BrightMagenta) { Name = "Lisbeth",
             Dialogue = "Lindarth's the heart of my forge. Got Col and rare materials? I'll craft you something the labyrinth won't forget." }),

        // ── HF Implement System questgivers (4): F84 Spiralblade, F85 Crusher, F92 Aurumbrand, F99 Deathglutton.
        ((f, r) => f == 84 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('V', Color.BrightCyan) { Name = "Spiralist Vey",
             Dialogue = "Geometry broke here. Ten failed spirals and the rapier answers." }),
        ((f, r) => f == 85 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('D', Color.BrightYellow) { Name = "Crusher Drago",
             Dialogue = "The storm-axe hums for a worthy haft. Ten battles and it's yours." }),
        ((f, r) => f == 92 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('H', Color.Yellow) { Name = "Auric Knight Halric",
             Dialogue = "Hauteclaire will shroud only the unshaken. Show me your fifteen." }),
        ((f, r) => f == 99 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('X', Color.BrightRed) { Name = "Last Herald Xiv",
             Dialogue = "The pact demands twenty. Feed Epetamu, and it will feed your climb." }),

        // ── Guild recruiters — Kibaou (ALF) lives in TOB F1; the remaining 7 spawn on their guild HQ floor.

        // Keita — Moonlit Black Cats (F10). BrightCyan K (distinct from Kibaou).
        ((f, r) => f == 10 && r.Count > 2, (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('K', Color.BrightCyan) { Name = "Keita",
             Dialogue = "The Cats are a small family. If you've got a warm heart and a steady blade, we'll make room." }),

        // Klein's Second — Fuurinkazan (F20).
        ((f, r) => f == 20 && r.Count > 2, (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('F', Color.BrightRed) { Name = "Klein's Second",
             Dialogue = "Klein's off drinking. I handle the paperwork. Katana in hand, karma in pocket — we don't check the second." }),

        // Schmitt — Legend Braves (F25). Wary of Coffin sympathizers.
        ((f, r) => f == 25 && r.Count > 2, (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('S', Color.Green) { Name = "Schmitt",
             Dialogue = "We're forging a name out here. Prove you hunt PKers, not work with them, and we'll have you." }),

        // Lind — Divine Dragon Alliance (F40).
        ((f, r) => f == 40 && r.Count > 2, (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('L', Color.BrightBlue) { Name = "Lind",
             Dialogue = "The DDA clears floors by formation, not by heroics. If you can keep a line, we'll have you." }),

        // Godfree — Knights of the Blood Oath lieutenant (F55). Honor-gated.
        ((f, r) => f == 55 && r.Count > 2, (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('G', Color.BrightRed) { Name = "Godfree",
             Dialogue = "Commander Heathcliff does not suffer shady oaths. Bring honor, or bring yourself elsewhere." }),

        // Siune — Sleeping Knights (F60). Elite, karma-gated.
        ((f, r) => f == 60 && r.Count > 2, (f, r) => 1, (f, r) => r.Count,
         () => new WorldSpawn('Y', Color.BrightMagenta) { Name = "Siune",
             Dialogue = "Yuuki left one condition: only the worthy. Show us resolve and we'll show you a family." }),

        // PoH's Herald — Laughing Coffin (F75 hidden); only meaningful at karma <= -50 (otherwise generic fallback line).
        ((f, r) => f == 75 && r.Count > 2, (f, r) => 2, (f, r) => r.Count,
         () => new WorldSpawn('P', Color.Red) { Name = "PoH's Herald",
             Dialogue = "PoH is always listening. When you have earned enough crimson, come back — we'll be watching." }),
    };

    // Walks FloorNpcSpawns in order, placing each NPC whose gate passes.
    private static void PlaceNpcsFromTable(GameMap map, List<Room> rooms, int floor, Random rng)
    {
        foreach (var spec in FloorNpcSpawns)
        {
            if (!spec.Gate(floor, rooms)) continue;
            var room = rooms[rng.Next(spec.IdxMin(floor, rooms), spec.IdxMax(floor, rooms))];
            var (x, y) = FindOpenSpot(map, room, rng);
            if (x >= 0) map.PlaceEntity(spec.Create(), x, y);
        }
    }

    // Place at (x,y); if blocked, spiral outward for nearest valid cell.
    private static bool TryPlaceEntityNear(GameMap map, Entity entity, int x, int y)
    {
        for (int r = 0; r <= 4; r++)
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            var t = map.Tiles[nx, ny].Type;
            if (map.Tiles[nx, ny].Occupant != null) continue;
            if (t != TileType.Floor && t != TileType.Path && !IsGrassType(t)) continue;
            map.PlaceEntity(entity, nx, ny);
            return true;
        }
        return false;
    }

    private static bool IsInSafeZone(GameMap map, int x, int y) =>
        map.SafeZone?.Contains(x, y) == true;

    private static bool IsRoomInSafeZone(GameMap map, Room room) =>
        map.SafeZone?.Contains(room.CenterX, room.CenterY) == true;

    private static (int x, int y) FindOpenSpotOutsideSafeZone(GameMap map, Random rng)
    {
        for (int attempt = 0; attempt < 80; attempt++)
        {
            int x = rng.Next(5, map.Width - 5);
            int y = rng.Next(5, map.Height - 5);
            if (!IsWalkableType(map.Tiles[x, y].Type)) continue;
            if (map.Tiles[x, y].Occupant != null) continue;
            if (IsInSafeZone(map, x, y)) continue;
            return (x, y);
        }
        return (-1, -1);
    }

    // Helpers

    internal static bool IsGrassType(TileType t) =>
        t == TileType.Grass || t == TileType.GrassTall || t == TileType.GrassSparse || t == TileType.Flowers;

    private static bool IsWalkableType(TileType t) =>
        IsGrassType(t) || t == TileType.Floor || t == TileType.Path || t == TileType.Door;

    internal static void PlaceCluster(GameMap map, int cx, int cy, int radius, TileType type, double density, Random rng)
    {
        for (int x = cx - radius; x <= cx + radius; x++)
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            if (!map.InInterior(x, y)) continue;
            double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (dist <= radius && rng.NextDouble() < density)
                map.Tiles[x, y].Type = type;
        }
    }

    internal static void ClearArea(GameMap map, int cx, int cy, int radius, TileType fill)
    {
        for (int x = cx - radius; x <= cx + radius; x++)
        for (int y = cy - radius; y <= cy + radius; y++)
            if (map.InInterior(x, y))
                map.Tiles[x, y].Type = fill;
    }

    internal static void CarvePath(GameMap map, int x1, int y1, int x2, int y2, Random rng)
    {
        int x = x1, y = y1;
        while (x != x2 || y != y2)
        {
            SetPathTile(map, x, y);
            // Occasional side-widening for a natural, well-trodden look
            if (rng.Next(4) == 0) SetPathTile(map, x + 1, y);
            if (rng.Next(4) == 0) SetPathTile(map, x, y + 1);

            int dx = Math.Sign(x2 - x);
            int dy = Math.Sign(y2 - y);
            if (dx != 0 && dy != 0)
            {
                if (rng.Next(2) == 0) x += dx;
                else y += dy;
            }
            else { x += dx; y += dy; }
        }
        SetPathTile(map, x2, y2);
    }

    // Bresenham line carve (no jitter) for main roads; clears trees/bushes in the way.
    internal static void CarveStraightPath(GameMap map, int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy;
        int x = x1, y = y1;
        while (true)
        {
            SetRoadTile(map, x, y);
            if (x == x2 && y == y2) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    private static void SetPathTile(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        var t = map.Tiles[x, y].Type;
        if (IsGrassType(t) || t == TileType.Bush)
            map.Tiles[x, y].Type = TileType.Path;
    }

    // Road-tile setter; clears trees/rocks so a main road cuts cleanly through terrain.
    private static void SetRoadTile(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        var t = map.Tiles[x, y].Type;
        if (t == TileType.Wall || t == TileType.Floor || t == TileType.Door
            || t == TileType.StairsUp || t == TileType.StairsDown) return;
        map.Tiles[x, y].Type = TileType.Path;
    }

    internal static void BuildStructure(GameMap map, int sx, int sy, int w, int h)
    {
        for (int x = sx; x < sx + w; x++)
        for (int y = sy; y < sy + h; y++)
            if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;

        for (int x = sx; x < sx + w; x++)
        {
            if (map.InBounds(x, sy)) map.Tiles[x, sy].Type = TileType.Wall;
            if (map.InBounds(x, sy + h - 1)) map.Tiles[x, sy + h - 1].Type = TileType.Wall;
        }
        for (int y = sy; y < sy + h; y++)
        {
            if (map.InBounds(sx, y)) map.Tiles[sx, y].Type = TileType.Wall;
            if (map.InBounds(sx + w - 1, y)) map.Tiles[sx + w - 1, y].Type = TileType.Wall;
        }

        if (map.InBounds(sx + w / 2, sy + h - 1))
            map.Tiles[sx + w / 2, sy + h - 1].Type = TileType.Door;
    }

    private static (int x, int y) FindOpenSpot(GameMap map, Room room, Random rng)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = room.X + rng.Next(1, Math.Max(2, room.Width - 1));
            int y = room.Y + rng.Next(1, Math.Max(2, room.Height - 1));
            if (map.InBounds(x, y) && IsWalkableType(map.Tiles[x, y].Type) && map.Tiles[x, y].Occupant == null)
                return (x, y);
        }
        return (-1, -1);
    }

}
