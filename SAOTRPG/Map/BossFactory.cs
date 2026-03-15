using SAOTRPG.Entities;

namespace SAOTRPG.Map;

/// <summary>
/// Creates the floor boss for each floor. Add new floor bosses by adding a tuple to FloorBosses.
/// Falls back to a generic scaled boss for floors beyond the defined list.
/// </summary>
public static class BossFactory
{
    // (Name, Title, Level, ATK, DEF, HP, XP, Col)
    // Add new floor bosses by adding a row. Index = floor - 1.
    private static readonly (string Name, string Title, int Level, int Atk, int Def, int Hp, int Xp, int Col)[] FloorBosses =
    {
        ("Illfang the Kobold Lord", "Floor 1 Boss", 15, 10, 8, 180, 1000, 5000),
        ("Asterius the Taurus King", "Floor 2 Boss", 18, 14, 10, 250, 1500, 7000),
        ("Nerius the Evil Treant", "Floor 3 Boss", 22, 16, 14, 320, 2000, 9000),
        ("Wythege the Black Knight", "Floor 4 Boss", 26, 20, 18, 400, 2500, 12000),
        ("Kagachi the Samurai Lord", "Floor 5 Boss", 30, 24, 22, 500, 3500, 15000),
    };

    public static Boss CreateFloorBoss(int floor)
    {
        int idx = Math.Clamp(floor - 1, 0, FloorBosses.Length - 1);
        var (name, title, level, atk, def, hp, xp, col) = FloorBosses[idx];

        // Scale beyond defined floors
        int scale = Math.Max(0, floor - FloorBosses.Length);

        var boss = new GenericBoss
        {
            Id = 7 + floor,
            Name = scale > 0 ? $"Guardian of Floor {floor}" : name,
            Level = level + scale * 4,
            BaseAttack = atk + scale * 4,
            BaseDefense = def + scale * 3,
            BaseCriticalRate = 8,
            BaseCriticalHitDamage = 15,
            BaseSpeed = 6 + floor,
            BaseSkillDamage = 4 + floor,
            Vitality = 8 + floor * 2,
            Strength = 6 + floor * 2,
            Endurance = 5 + floor,
            Dexterity = 4 + floor,
            Agility = 3 + floor,
            Intelligence = 2 + floor,
            ExperienceYield = xp + scale * 500,
            ColYield = col + scale * 3000,
        };
        boss.SetBossTitle(scale > 0 ? $"Floor {floor} Boss" : title);
        boss.MaxHealth = hp + scale * 100;
        boss.CurrentHealth = boss.MaxHealth;
        return boss;
    }
}

/// <summary>
/// Generic boss subclass used by BossFactory. Exposes a setter for BossTitle.
/// </summary>
public class GenericBoss : Boss
{
    public void SetBossTitle(string title) => BossTitle = title;
}
