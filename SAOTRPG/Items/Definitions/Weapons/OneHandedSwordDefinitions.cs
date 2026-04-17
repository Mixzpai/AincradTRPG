using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;


// Static registry of all one-handed sword weapons.
public static class OneHandedSwordDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "One-Handed Sword",
            BaseDamage = baseDmg, AttackSpeed = 1, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    // FD Paired Dual-Wield marker — sets IsDualWieldPaired=true so the
    // weapon may occupy the OffHand slot without the DualBlades unlock
    // and participates in the Pair Resonance synergy lookup
    // (Systems.DualWieldPairs). Applied to the 3 canonical pair members
    // (Elucidator/Dark Repulser, Elucidator Rouge/Flare Pulsar, Black Iron
    // Dual Sword A/B) and the 3 solo "Dual"-flavored canon weapons
    // (Chaos Raider, Lightning Divider Dual, Murasama G4 Dual — the last
    // lives in KatanaDefinitions).
    private static Weapon Paired(Weapon w) { w.IsDualWieldPaired = true; return w; }

    public static Weapon CreateIronSword() => Make("iron_sword", "Iron Sword", 100, "Common", 50, 1, 10,
        B().Add(StatType.Attack, 8));

    public static Weapon CreateSteelSword() => Make("steel_sword", "Steel Sword", 250, "Uncommon", 75, 10, 25,
        B().Add(StatType.Attack, 15).Add(StatType.Strength, 5));

    public static Weapon CreateMythrilSword() => Make("mythril_sword", "Mythril Sword", 800, "Rare", 100, 25, 45,
        B().Add(StatType.Attack, 25).Add(StatType.Strength, 8));

    public static Weapon CreateAdamantiteSword() => Make("adamantite_sword", "Adamantite Sword", 2500, "Epic", 150, 50, 80,
        B().Add(StatType.Attack, 45).Add(StatType.Strength, 15).Add(StatType.Dexterity, 8));

    public static Weapon CreateCelestialBlade() => Make("celestial_blade", "Celestial Blade", 6000, "Legendary", 200, 75, 130,
        B().Add(StatType.Attack, 70).Add(StatType.Strength, 20).Add(StatType.Agility, 10));

    // --- Named / SAO Legendary Weapons ---

    // IF canon F1-2 craftable baseline. Lisbeth's first-pass forge for starter players.
    // Rebalanced to IF's "crafted entry sword" spec (was a mid-tier Rare, now the
    // canonical starter craft per Phase B IF expansion).
    public static Weapon CreateAnnealBlade() => Make("anneal_blade", "Anneal Blade", 180, "Uncommon", 80, 1, 25,
        B().Add(StatType.Attack, 10).Add(StatType.Dexterity, 2));

    // IF canon F4-5 upgrade of Anneal Blade. Re-heated, folded, harder bite.
    public static Weapon CreateToughAnnealBlade() => Make("tough_anneal_blade", "Tough Anneal Blade", 420, "Rare", 120, 4, 38,
        B().Add(StatType.Attack, 18).Add(StatType.Strength, 3));

    // IF canon F8-10 rare drop. Black-tempered cousin — longer cooldown forge, razor-fine bleed edge.
    public static Weapon CreatePitchBlackAnnealBlade() => Make("pitch_black_anneal_blade", "Pitch-black Anneal Blade", 780, "Rare", 140, 8, 48,
        B().Add(StatType.Attack, 24).Add(StatType.Dexterity, 4), "Bleed+10");

    public static Weapon CreateSwordBreaker() => Make("sword_breaker", "Sword Breaker", 400, "Uncommon", 70, 15, 20,
        B().Add(StatType.Attack, 10).Add(StatType.Dexterity, 5), "ParryChance+10");

    public static Weapon CreateQueensKnightsword() => Make("queens_knightsword", "Queen's Knightsword", 1200, "Epic", 100, 20, 30,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8), "ComboBonus+50");

    public static Weapon CreateAzureSkyBlade() => Make("azure_sky_blade", "Azure Sky Blade", 1800, "Rare", 120, 30, 50,
        B().Add(StatType.Attack, 30).Add(StatType.Agility, 8));

    public static Weapon CreateElucidator() => Paired(Make("elucidator", "Elucidator", 10000, "Legendary", 200, 50, 90,
        B().Add(StatType.Attack, 50).Add(StatType.Strength, 18).Add(StatType.Agility, 10), "SkillCooldown-1"));

    public static Weapon CreateDarkRepulser() => Paired(Make("dark_repulser", "Dark Repulser", 10000, "Legendary", 200, 50, 85,
        B().Add(StatType.Attack, 48).Add(StatType.Dexterity, 12).Add(StatType.Strength, 12), "CritHeal+5"));

    public static Weapon CreateLiberator() => Make("liberator", "Liberator", 15000, "Legendary", 250, 75, 140,
        B().Add(StatType.Attack, 65).Add(StatType.Strength, 25), "BlockChance+15");

    // ── Hollow Realization Evolution Chain (1H Sword) ───────────────
    // Final Espada -> Asmodeus -> Final Avalanche -> Tyrfing.

    // Final Espada, gleaming duel-sword. T1 of the Tyrfing chain.
    public static Weapon CreateFinalEspada() => Make("final_espada", "Final Espada", 1900, "Rare", 120, 15, 62,
        B().Add(StatType.Attack, 35).Add(StatType.Strength, 10));

    // Asmodeus, demon-prince blade wreathed in sin. T2 of the Tyrfing chain.
    public static Weapon CreateAsmodeus() => Make("asmodeus", "Asmodeus", 5400, "Epic", 160, 35, 108,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 15).Add(StatType.Dexterity, 8), "CritRate+10");

    // Final Avalanche, cascading-strike longsword. T3 of the Tyrfing chain.
    public static Weapon CreateFinalAvalanche() => Make("final_avalanche", "Final Avalanche", 12500, "Legendary", 210, 60, 148,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 22).Add(StatType.Dexterity, 14), "SkillCooldown-1");

    // T4 Divine of the One-Handed Sword evolution chain.
    public static Weapon CreateTyrfing() => Make("tyrfing", "Tyrfing", 20000, "Divine", 999, 80, 160,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 20).Add(StatType.Dexterity, 15), "SkillDamage+25");

    // F3 Elf War reward. Dark-elven steel, faint twilight glow.
    public static Weapon CreateSwordOfEventide() => Make("sword_of_eventide", "Sword of Eventide", 2200, "Rare", 120, 6, 28,
        B().Add(StatType.Attack, 16).Add(StatType.Agility, 6).Add(StatType.Dexterity, 4), "CritRate+10");

    // Alternate F3-4 Elf War reward. Dark blue blade with a boar-tusk guard.
    public static Weapon CreateBlueBoar() => Make("blue_boar", "Blue Boar", 1800, "Rare", 110, 8, 32,
        B().Add(StatType.Attack, 18).Add(StatType.Strength, 8), "BackstabDmg+30");

    // Drop from Laughing Coffin-aligned PKers. Bloody red, jagged.
    public static Weapon CreateCrimsonLongsword() => Make("crimson_longsword", "Crimson Longsword", 4200, "Epic", 150, 25, 62,
        B().Add(StatType.Attack, 34).Add(StatType.Strength, 12).Add(StatType.Dexterity, 6), "Bleed+15");

    // Remains Heart — Lisbeth's masterwork (Infinity Moment / Hollow Fragment canon).
    // Her character-episode questline ends in this weapon — the sole top-tier
    // Legendary that is fully enhanceable in canon. Strongest enhanceable 1H sword.
    public static Weapon CreateRemainsHeart() => Make("remains_heart", "Remains Heart", 22000, "Legendary", 240, 85, 158,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 22).Add(StatType.Dexterity, 12).Add(StatType.Defense, 10), "SkillCooldown-1");

    // Alicization Lycoris Divine Beast drops (Priority 4). Dropped by non-canon
    // floor bosses on F1-F50 as their canonical hand-placed reward.

    // F17 Gelidus the Frozen Colossus drops. Storm-wrapped blade. "Squall" flavor.
    public static Weapon CreateSavageSquall() => Make("savage_squall", "Savage Squall", 9500, "Legendary", 180, 38, 110,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 18).Add(StatType.Agility, 12), "Slow+15");

    // F30 Primos the World Serpent drops. Void-blackened blade, devours skill time.
    public static Weapon CreateVoidEater() => Make("void_eater", "Void Eater", 15000, "Legendary", 210, 35, 135,
        B().Add(StatType.Attack, 68).Add(StatType.Strength, 20).Add(StatType.Dexterity, 12).Add(StatType.Agility, 8), "SkillCooldown-1");

    // ── Hollow Fragment / Infinity Moment Legendaries ──────────────
    // Endgame drops from F77-F99 in the Hollow Fragment canon.

    // Hollow Fragment F90 floor-boss weapon. Holy radiance, blessed steel.
    public static Weapon CreateEurynomesHolySword() => Make("eurynomes_holy_sword", "Eurynome's Holy Sword", 18000, "Legendary", 220, 85, 155,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 22).Add(StatType.Dexterity, 12), "HolyDamage+20");

    // Hollow Fragment F81 implement. Absorbs 20% HP on hit (flavor).
    public static Weapon CreateFiendbladeDeathbringer() => Make("fiendblade_deathbringer", "Fiendblade: Deathbringer", 16000, "Legendary", 200, 78, 148,
        B().Add(StatType.Attack, 72).Add(StatType.Strength, 20).Add(StatType.Dexterity, 10), "Bleed+25");

    // Hollow Fragment F83 implement. High-speed fay-forged blade.
    public static Weapon CreateFaybladeTizona() => Make("fayblade_tizona", "Fayblade: Tizona", 17000, "Legendary", 210, 80, 152,
        B().Add(StatType.Attack, 70).Add(StatType.Speed, 18).Add(StatType.Agility, 15).Add(StatType.Dexterity, 10), "AttackSpeed+2");

    // Hollow Fragment F85 implement. +50% damage versus dragons (flavor).
    public static Weapon CreateGodbladeDragonslayer() => Make("godblade_dragonslayer", "Godblade: Dragonslayer", 19000, "Legendary", 220, 82, 160,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 25).Add(StatType.Dexterity, 12), "DragonSlayer+50");

    // ── Divine Objects ──────────────────────────────────────────────
    // Above Legendary. Hand-placed only (no random drops). Unbreakable.
    // Bypass enemy block rolls (armor damage reduction still applies).
    // Canon Priority / Sacred Object tier from Alicization arc.

    // Kirito's Priority 46 Divine Object. Wooden blade carved from the Gigas
    // Cedar's top branch. Canonical Underworld weapon — "bypass armor" feel.
    public static Weapon CreateNightSkySword() => Make("night_sky_sword", "Night Sky Sword", 40000, "Divine", 999, 80, 180,
        B().Add(StatType.Attack, 95).Add(StatType.Strength, 28).Add(StatType.Agility, 15).Add(StatType.Dexterity, 12), "ArmorPierce+30");

    // Eugeo's ice-attribute Divine Object. Pure white blade with blue rose guard.
    // Full Control Art freezes a wide field; we model it as high freeze-on-hit.
    public static Weapon CreateBlueRoseSword() => Make("blue_rose_sword", "Blue Rose Sword", 40000, "Divine", 999, 80, 175,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 25).Add(StatType.Dexterity, 18), "Freeze+20");

    // Alice Synthesis Thirty's Divine Object. Forged from a fragrant olive tree
    // blessed by Goddess Stacia. Full Control Art scatters thousands of petals.
    public static Weapon CreateFragrantOliveSword() => Make("fragrant_olive_sword", "Fragrant Olive Sword", 45000, "Divine", 999, 85, 178,
        B().Add(StatType.Attack, 92).Add(StatType.Strength, 22).Add(StatType.Agility, 14).Add(StatType.Dexterity, 14).Add(StatType.SkillDamage, 15), "HolyAoE+15");

    // Bercouli Synthesis One's Divine Object. Created from the original System
    // Clock of the Underworld — can slice a moment of the past or future.
    public static Weapon CreateTimePiercingSword() => Make("time_piercing_sword", "Time Piercing Sword", 48000, "Divine", 999, 88, 185,
        B().Add(StatType.Attack, 95).Add(StatType.Strength, 30).Add(StatType.Dexterity, 15), "ExecuteThreshold+25");

    // Sheyta Synthesis Twelve's Divine Object. Granted by Administrator;
    // said to cut through any and all things. Severing flavor.
    public static Weapon CreateBlackLilySword() => Make("black_lily_sword", "Black Lily Sword", 50000, "Divine", 999, 90, 190,
        B().Add(StatType.Attack, 100).Add(StatType.Strength, 28).Add(StatType.Dexterity, 22), "SeveringStrike+50");

    // ── Integral Factor Series (1H Sword entries) ───────────────────
    // Canon named-series swords from IF. Epic F14 / Epic F25 / Legendary F61+.

    // F14 Integral Series 1H sword. Radgrid, a valkyrie-name longsword of the dawn series.
    public static Weapon CreateIntegralRadgrid() => Make("ohs_integral_radgrid", "Integral Radgrid", 4800, "Epic", 160, 14, 82,
        B().Add(StatType.Attack, 42).Add(StatType.Strength, 12).Add(StatType.Dexterity, 8), "CritRate+10");

    // F25 Nox Series 1H sword. Nox Radgrid, shadow-forged counterpart from the Underground Labyrinth.
    public static Weapon CreateNoxRadgrid() => Make("ohs_nox_radgrid", "Nox Radgrid", 7200, "Epic", 175, 25, 95,
        B().Add(StatType.Attack, 48).Add(StatType.Strength, 14).Add(StatType.Agility, 8), "LifeSteal+8");

    // F61 Rosso Series 1H sword. Forneus, red-demon named after the marquis of the sea.
    public static Weapon CreateRossoForneus() => Make("ohs_rosso_forneus", "Rosso Forneus", 14000, "Legendary", 220, 55, 148,
        B().Add(StatType.Attack, 72).Add(StatType.Strength, 22).Add(StatType.Dexterity, 14), "Bleed+20");

    // F85→F87 Yasha Series 1H sword. Astaroth, grand-duke demon; the Yasha prefix leans oni/yaksha.
    public static Weapon CreateYashaAstaroth() => Make("ohs_yasha_astaroth", "Yasha Astaroth", 18500, "Legendary", 235, 78, 155,
        B().Add(StatType.Attack, 76).Add(StatType.Strength, 24).Add(StatType.Dexterity, 16), "CritRate+15");

    // F90+ Gaou Series 1H sword. Reginleifr, Norse valkyrie of heir-inheritance.
    public static Weapon CreateGaouReginleifr() => Make("ohs_gaou_reginleifr", "Gaou Reginleifr", 26000, "Legendary", 255, 88, 170,
        B().Add(StatType.Attack, 84).Add(StatType.Strength, 26).Add(StatType.Agility, 14).Add(StatType.Dexterity, 14), "SkillCooldown-1");

    // ── Hollow Fragment Implement System (1H Sword) ───────────────

    // HF F92 implement. Aurumbrand — Hauteclaire; -20% incoming damage shroud (flavor). Quest NPC reward.
    public static Weapon CreateAurumbrandHauteclaire() => Make("ohs_aurumbrand_hauteclaire", "Aurumbrand: Hauteclaire", 23000, "Legendary", 240, 86, 162,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 22).Add(StatType.Vitality, 18).Add(StatType.Dexterity, 16), "DamageReduction+20");

    // ── Hollow Area Uniques (1H Sword) — Rare/Legendary drops ────
    // Tyler's placement direction: balanced variety, rare chest drops.

    // HF F35 Hollow Area. Traitorblade — cursed duel-edge, pitch-dark finish.
    public static Weapon CreateTraitorbladeArguteBrand() => Make("ohs_traitorblade_argute_brand", "Traitorblade: Argute Brand", 2400, "Rare", 130, 30, 65,
        B().Add(StatType.Attack, 34).Add(StatType.Dexterity, 10).Add(StatType.Agility, 6), "Bleed+15");

    // HF F82 Hollow Area. Fake Sword Velocious Brain — artificer's replica, mind-piercing pommel.
    public static Weapon CreateFakeSwordVelociousBrain() => Make("ohs_velocious_brain", "Fake Sword: Velocious Brain", 16500, "Legendary", 200, 75, 138,
        B().Add(StatType.Attack, 72).Add(StatType.Strength, 20).Add(StatType.Dexterity, 20), "SkillDamage+20");

    // ── Lisbeth Rarity 6 Crafted (1H Sword) ──────────────────────
    // Canon HF Lisbeth-craft-only weapons. Cost: 3M Col + rare materials.

    public static Weapon CreateVariableVVice() => Make("ohs_variable_v_vice", "Variable V Vice", 28000, "Legendary", 250, 88, 168,
        B().Add(StatType.Attack, 82).Add(StatType.Strength, 24).Add(StatType.Dexterity, 20), "CritRate+18");

    public static Weapon CreateLiberatorAstralLegion() => Make("ohs_liberator_astral_legion", "Liberator: Astral Legion", 30000, "Legendary", 260, 90, 175,
        B().Add(StatType.Attack, 86).Add(StatType.Strength, 26).Add(StatType.Dexterity, 20), "SkillDamage+15");

    public static Weapon CreateMarginlessBlade() => Make("ohs_marginless_blade", "Marginless Blade", 32000, "Legendary", 260, 92, 180,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 28).Add(StatType.Dexterity, 22).Add(StatType.Agility, 16), "ComboBonus+50");

    // ── Memory Defrag — Alicization gap weapons (Legendary) ────────────

    // MD-awakened Fragrant Olive variant. Stronger than the base Divine
    // Fragrant Olive in raw damage but loses the Divine crit-bypass;
    // placed as a quest-reward upgrade F65+. (Flavor: Alice canon wielder.)
    public static Weapon CreateUnfoldingTruthFragrantOlive() => Make("ohs_unfolding_truth_fragrant_olive", "Unfolding Truth Fragrant Olive Sword", 26000, "Legendary", 250, 65, 170,
        B().Add(StatType.Attack, 85).Add(StatType.Strength, 24).Add(StatType.Dexterity, 16).Add(StatType.Agility, 14), "HolyAoE+20");

    // Red Rose Sword — the canonical pair to Night Sky (Kirito). F95+ rare
    // field-boss drop; darker sibling to Blue Rose. Kirito flavor.
    public static Weapon CreateRedRoseSword() => Make("ohs_red_rose_sword", "Red Rose Sword", 28000, "Legendary", 250, 92, 175,
        B().Add(StatType.Attack, 88).Add(StatType.Strength, 26).Add(StatType.Dexterity, 16), "Burn+20");

    // Black Iron Dual Swords A — Underworld Kirito set (pair A). Late-game
    // chest/quest drops F80+. Flavor: Kirito canon wielder.
    public static Weapon CreateBlackIronDualSwordA() => Paired(Make("ohs_black_iron_dual_sword_a", "Black Iron Dual Sword (Paired)", 23000, "Legendary", 240, 80, 158,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 22).Add(StatType.Dexterity, 18), "ComboBonus+30"));

    // Black Iron Dual Swords B — Underworld Kirito set (pair B). Matching
    // off-hand counterpart. Lower base damage to reflect off-hand role.
    public static Weapon CreateBlackIronDualSwordB() => Paired(Make("ohs_black_iron_dual_sword_b", "Black Iron Dual Sword (Off-hand)", 22000, "Legendary", 240, 80, 155,
        B().Add(StatType.Attack, 76).Add(StatType.Strength, 22).Add(StatType.Agility, 18), "CritRate+15"));

    // ── Memory Defrag Originals (1H Sword entries) ─────────────────────

    // MD event weapon. Celestial choir blade wielded by the Diva. Legendary.
    public static Weapon CreateSwordOfDiva() => Make("ohs_sword_of_diva", "Sword of Diva", 24000, "Legendary", 240, 80, 162,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 24).Add(StatType.Dexterity, 16), "HolyDamage+20");

    // MD Rare. Knight-order saber, sapphire-blue finish.
    public static Weapon CreateCobaltTristan() => Make("ohs_cobalt_tristan", "Cobalt Tristan", 2800, "Rare", 130, 32, 70,
        B().Add(StatType.Attack, 36).Add(StatType.Strength, 10).Add(StatType.Dexterity, 8), "CritRate+10");

    // MD Rare. Ocean-forged longsword, brine-blue edge.
    public static Weapon CreateAtlantisSword() => Make("ohs_atlantis_sword", "Atlantis Sword", 2900, "Rare", 130, 30, 72,
        B().Add(StatType.Attack, 36).Add(StatType.Strength, 10).Add(StatType.Dexterity, 8), "FrostDamage+10");

    // MD Epic. Keepsake blade that heals its bearer between strikes.
    public static Weapon CreateEternalPromise() => Make("ohs_eternal_promise", "Eternal Promise", 6200, "Epic", 165, 50, 125,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 15).Add(StatType.Vitality, 12), "HPRegen+3");

    // ── Fractured Daydream — Character Core Canon (1H Sword) ───────────

    // Kirito canon — red-edged successor to Elucidator. F98+ rare drop.
    public static Weapon CreateElucidatorRouge() => Paired(Make("ohs_elucidator_rouge", "Elucidator Rouge", 27000, "Legendary", 250, 90, 172,
        B().Add(StatType.Attack, 86).Add(StatType.Strength, 26).Add(StatType.Agility, 14), "Burn+15"));

    // Kirito canon — dual-wield chaos blade. F85+ rare drop. (FD class-lock
    // is flavor only — any player can equip.) Solo "Dual"-flavored: bypasses
    // DualBlades unlock for offhand routing, but has no canonical partner.
    public static Weapon CreateChaosRaiderDual() => Paired(Make("ohs_chaos_raider_dual", "Chaos Raider", 19500, "Legendary", 230, 78, 158,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 22).Add(StatType.Dexterity, 18), "ComboBonus+30"));

    // Alice canon — golden-osmanthus variant. F75+ rare drop. Fragrant Olive
    // lineage (Alice flavor wielder).
    public static Weapon CreateGoldenOsmanthusSword() => Make("ohs_golden_osmanthus", "Golden Osmanthus Sword", 21000, "Legendary", 235, 75, 162,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 22).Add(StatType.Dexterity, 16).Add(StatType.Agility, 10), "HolyDamage+18");

    // Oberon canon — fairy-king regalia blade. F75+ rare drop.
    public static Weapon CreateTanquiem() => Make("ohs_tanquiem", "Tanquiem", 20500, "Legendary", 235, 75, 160,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 22).Add(StatType.Agility, 14), "ParryChance+15");

    // Administrator canon — Pontifex's blade. F95+ Legendary.
    public static Weapon CreateSilveryRuler() => Make("ohs_silvery_ruler", "Silvery Ruler", 29000, "Legendary", 260, 92, 178,
        B().Add(StatType.Attack, 88).Add(StatType.Strength, 26).Add(StatType.Dexterity, 18), "HolyDamage+20");

    // ── Fractured Daydream — Elemental Variants (1H Sword) ─────────────

    // Kirito fire variant. Flare Pulsar — solar-burst edge.
    public static Weapon CreateFlarePulsar() => Paired(Make("ohs_flare_pulsar", "Flare Pulsar", 5400, "Epic", 160, 50, 115,
        B().Add(StatType.Attack, 52).Add(StatType.Strength, 15).Add(StatType.Dexterity, 10), "Burn+20"));

    // Kirito thunder dual variant. Lightning Divider — dual-storm edge.
    // Solo "Dual"-flavored: bypasses DualBlades unlock, no canonical partner.
    public static Weapon CreateLightningDividerDual() => Paired(Make("ohs_lightning_divider_dual", "Lightning Divider", 5600, "Epic", 160, 55, 118,
        B().Add(StatType.Attack, 54).Add(StatType.Strength, 14).Add(StatType.Dexterity, 12), "Stun+10"));

    // Alice fire variant. Red Peony Sword — crimson-petal Fragrant Olive cousin.
    public static Weapon CreateRedPeonySword() => Make("ohs_red_peony_sword", "Red Peony Sword", 5200, "Epic", 155, 50, 112,
        B().Add(StatType.Attack, 50).Add(StatType.Strength, 14).Add(StatType.Dexterity, 10), "Burn+20");

    // Alice wind variant. Sword of the Gentle Breeze — feather-light Fragrant Olive variant.
    public static Weapon CreateSwordOfTheGentleBreeze() => Make("ohs_sword_of_the_gentle_breeze", "Sword of the Gentle Breeze", 3400, "Rare", 130, 45, 78,
        B().Add(StatType.Attack, 38).Add(StatType.Dexterity, 10).Add(StatType.Agility, 10), "Slow+15");

    // Alice thunder variant. Thunderclap Sword — storm-rung edge.
    public static Weapon CreateThunderclapSword() => Make("ohs_thunderclap_sword", "Thunderclap Sword", 5400, "Epic", 155, 55, 115,
        B().Add(StatType.Attack, 52).Add(StatType.Strength, 14).Add(StatType.Dexterity, 10), "Stun+10");

    // Alice dark variant. Purple Bellflower Sword — twilight Fragrant Olive kin.
    public static Weapon CreatePurpleBellflowerSword() => Make("ohs_purple_bellflower_sword", "Purple Bellflower Sword", 3300, "Rare", 130, 48, 76,
        B().Add(StatType.Attack, 38).Add(StatType.Dexterity, 10).Add(StatType.Agility, 8), "Bleed+15");

    // Heathcliff water variant. Arc Order — rippling curve-edge.
    public static Weapon CreateArcOrder() => Make("ohs_arc_order", "Arc Order", 3400, "Rare", 130, 48, 80,
        B().Add(StatType.Attack, 40).Add(StatType.Strength, 10).Add(StatType.Vitality, 8), "Freeze+15");

    // Heathcliff thunder variant. Topaz Edge — gold-plated storm blade.
    public static Weapon CreateTopazEdge() => Make("ohs_topaz_edge", "Topaz Edge", 5400, "Epic", 155, 55, 116,
        B().Add(StatType.Attack, 52).Add(StatType.Strength, 14).Add(StatType.Vitality, 10), "Stun+10");

    // Heathcliff light variant. Saint Guarder — radiant cathedral blade.
    public static Weapon CreateSaintGuarder() => Make("ohs_saint_guarder", "Saint Guarder", 5300, "Epic", 155, 55, 114,
        B().Add(StatType.Attack, 52).Add(StatType.Strength, 14).Add(StatType.Vitality, 12), "HolyDamage+15");

    // Heathcliff dark variant. Abyss Keeper — void-black knightblade.
    public static Weapon CreateAbyssKeeper() => Make("ohs_abyss_keeper", "Abyss Keeper", 3300, "Rare", 130, 48, 78,
        B().Add(StatType.Attack, 38).Add(StatType.Strength, 10).Add(StatType.Vitality, 10), "Bleed+15");

    // Oberon light variant. Excalibur — fae-king holy blade (FD event reward,
    // DefId disambiguated from the Arthurian display name via the `ohs_`
    // prefix + `oberon` subtag).
    public static Weapon CreateExcaliburOberon() => Make("ohs_excalibur_oberon", "Excalibur", 5800, "Epic", 165, 55, 122,
        B().Add(StatType.Attack, 54).Add(StatType.Strength, 14).Add(StatType.Dexterity, 12), "HolyDamage+15");

    // Oberon fire variant. Bloodthirst — crimson-kissed Tanquiem cousin.
    public static Weapon CreateBloodthirst() => Make("ohs_bloodthirst", "Bloodthirst", 5500, "Epic", 160, 55, 118,
        B().Add(StatType.Attack, 52).Add(StatType.Dexterity, 14).Add(StatType.Agility, 10), "Burn+20");
}
