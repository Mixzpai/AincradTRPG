namespace SAOTRPG.Systems;

// Braille subcell composition. Bits 0-7 → Unicode dot positions:
// b0=⠁ b1=⠂ b2=⠄ b3=⡀ b4=⠈ b5=⠐ b6=⠠ b7=⢀ . OR-combine; 0xFF = ⣿.
public static class BrailleCanvas
{
    public static char Braille(byte dotMask) => (char)(0x2800 | dotMask);

    // Common pattern library — bit positions verified against U+2800 base.
    // Cell layout (Unicode 8-dot order): col-left=1,2,3,7 col-right=4,5,6,8.
    public const byte FullCell    = 0xFF; // ⣿
    public const byte LeftHalf    = 0x47; // dots 1,2,3,7 → ⡇
    public const byte RightHalf   = 0xB8; // dots 4,5,6,8 → ⢸
    public const byte TopHalf     = 0x1B; // dots 1,2,4,5 → ⠛
    public const byte BottomHalf  = 0xE4; // dots 3,6,7,8 → ⣤
    public const byte CenterDot   = 0x12; // dots 2,5     → ⠒
    public const byte CornerNW    = 0x01; // dot 1
    public const byte CornerNE    = 0x08; // dot 4
    public const byte CornerSW    = 0x40; // dot 7
    public const byte CornerSE    = 0x80; // dot 8

    // Drop alternate bits — preserves rough density at half. Fast popcount-aware reduction.
    public static byte HalfDots(byte mask) => (byte)(mask & 0x55);

    // Pick `dotCount` dots deterministically by seed. Fisher-Yates over the 8 positions.
    public static byte SpreadDots(int seed, int dotCount)
    {
        if (dotCount >= 8) return 0xFF;
        if (dotCount <= 0) return 0x00;
        byte mask = 0;
        var rng = new Random(seed);
        Span<int> positions = stackalloc int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 7; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }
        for (int i = 0; i < dotCount; i++) mask |= (byte)(1 << positions[i]);
        return mask;
    }

    // Density 0..1 → dot count 0..8. Used by spread/fade frames.
    public static byte DensityDots(int seed, float density)
    {
        density = Math.Clamp(density, 0f, 1f);
        int n = (int)Math.Round(density * 8f);
        return SpreadDots(seed, n);
    }
}
