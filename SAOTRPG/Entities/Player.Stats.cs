using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.RunState;

namespace SAOTRPG.Entities
{
    public partial class Player
    {
        // Applies damage to the player, logs the result, and marks defeated if HP falls to 0.
        public void TakeDamage(int damage)
        {
            if (IsDefeated) { _log?.LogCombat($"{FirstName} {LastName} is already defeated."); return; }

            CurrentHealth -= damage;
            _log?.LogCombat($"{FirstName} {LastName} takes {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
            DebugLogger.LogState($"Player \"{FirstName}\" took {damage} dmg", $"HP:{CurrentHealth}/{MaxHealth}");

            if (CurrentHealth <= 0)
            {
                var crystal = Inventory.Items.FirstOrDefault(
                    i => i is Potion { PotionType: "Revive" });
                if (crystal != null)
                {
                    Inventory.RemoveItem(crystal);
                    CurrentHealth = MaxHealth / 2;
                    _log?.LogSystem($"A Revive Crystal shatters — {FirstName} is pulled back from the brink! HP restored to {CurrentHealth}.");
                    return;
                }
                IsDefeated = true;
                _log?.LogCombat($"{FirstName} {LastName} has been defeated!");
            }
        }

        private static readonly string[] LevelUpFlavors =
        {
            "  You feel power surge through your veins!",
            "  The world sharpens. You're getting stronger.",
            "  Aincrad's challenges forge you into something greater.",
            "  New strength flows into your limbs. Press onward!",
            "  A warm glow envelops you. You've grown.",
        };

        private static readonly string[] AttackFlavors =
        {
            "{0} attacks {1} for {2} damage.",
            "{0} slashes at {1} — {2} damage!",
            "{0} swings at {1}, dealing {2} damage.",
            "{0} strikes {1} squarely for {2} damage.",
            "{0} lands a hit on {1} — {2} damage.",
        };

        private static readonly string[] CritFlavors =
        {
            "*** CRITICAL HIT! *** {0} strikes {1} for {2} damage!",
            "*** DEVASTATING BLOW! *** {0} cleaves {1} for {2} damage!",
            "*** PERFECT STRIKE! *** {0} finds {1}'s weak point — {2} damage!",
            "*** BRUTAL HIT! *** {0} smashes into {1} for {2} damage!",
            "*** PRECISION STRIKE! *** {0} pierces {1}'s guard — {2} damage!",
        };

        // Rolls attack against a monster — checks for crit, logs flavor text, returns (damage, wasCrit).
        // critChance comes from TurnManager.PlayerCritChance: the roll needs the equipped weapon's
        // CritRate tag and the weapon-proficiency perk, neither of which the Player can reach. No
        // default value — the caller must state it, so a new call site cannot silently roll on the
        // bare stat again.
        public (int Damage, bool IsCrit) AttackMonster(Monster monster, int critChance)
        {
            int damage = Attack;
            bool isCrit = RunRng.Next(0, 100) < critChance;
            if (isCrit)
            {
                damage += (int)CriticalHitDamage;
                _log?.LogCombat(string.Format(
                    CritFlavors[RunRng.Next(CritFlavors.Length)],
                    FirstName, monster.Name, damage));
            }
            else
            {
                _log?.LogCombat(string.Format(
                    AttackFlavors[RunRng.Next(AttackFlavors.Length)],
                    FirstName, monster.Name, damage));
            }
            return (damage, isCrit);
        }

        // Increments level, grants 5 skill points, fully restores HP, and logs fanfare.
        public void LevelUp()
        {
            Level++;
            SkillPoints += 5;
            CurrentHealth = MaxHealth;

            _log?.LogSystem("====================================");
            _log?.LogSystem($"  ** LEVEL UP! **  {FirstName} is now Level {Level}!");
            _log?.LogSystem($"  +5 Skill Points | HP fully restored");
            _log?.LogSystem(LevelUpFlavors[RunRng.Next(LevelUpFlavors.Length)]);
            _log?.LogSystem("====================================");
            DebugLogger.LogState($"Player \"{FirstName}\" leveled up", $"LVL:{Level} HP:{CurrentHealth}/{MaxHealth} ATK:{Attack} DEF:{Defense} SP:{SkillPoints}");
        }

        // Awards EXP, triggering one or more level-ups if the threshold is met.
        public void GainExperience(int experience)
        {
            CurrentExperience += experience;
            _log?.LogSystem($"{FirstName} gains {experience} EXP. Current: {CurrentExperience}/{ExperienceRequired}");
            while (CurrentExperience >= ExperienceRequired)
            {
                CurrentExperience -= ExperienceRequired;
                LevelUp();
            }
        }

        // Allocates skill points to a stat. Returns false if invalid stat name or insufficient points.
        public bool SpendSkillPoints(string statName, int points)
        {
            if (points <= 0 || points > SkillPoints) return false;

            switch (statName.ToLower())
            {
                case "vitality": Vitality += points; break;
                case "strength": Strength += points; break;
                case "endurance": Endurance += points; break;
                case "dexterity": Dexterity += points; break;
                case "agility": Agility += points; break;
                case "intelligence": Intelligence += points; break;
                default: return false;
            }

            SkillPoints -= points;
            return true;
        }

        // Permanent base-stat grant (guild perks, milestone rewards); negative v revokes it.
        // Health is paid as Vitality, 1 per 10 HP, and a grant also heals by v.
        public void AddBaseStat(StatType stat, int v)
        {
            switch (stat)
            {
                case StatType.Attack:       BaseAttack      += v; break;
                case StatType.Defense:      BaseDefense     += v; break;
                case StatType.Speed:        BaseSpeed       += v; break;
                case StatType.SkillDamage:  BaseSkillDamage += v; break;
                case StatType.Strength:     Strength        += v; break;
                case StatType.Vitality:     Vitality        += v; break;
                case StatType.Endurance:    Endurance       += v; break;
                case StatType.Dexterity:    Dexterity       += v; break;
                case StatType.Agility:      Agility         += v; break;
                case StatType.Intelligence: Intelligence    += v; break;
                case StatType.Health:
                    Vitality += Math.Sign(v) * Math.Max(1, Math.Abs(v) / 10);
                    if (v > 0)
                        CurrentHealth = Math.Min(CurrentHealth + v, MaxHealth);
                    break;
            }
        }
    }
}
