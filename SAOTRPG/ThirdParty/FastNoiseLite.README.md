# FastNoiseLite

Upstream source:
- Repo: https://github.com/Auburn/FastNoiseLite
- File: https://github.com/Auburn/FastNoiseLite/blob/master/CSharp/FastNoiseLite.cs
- License: MIT (see `FastNoiseLite.LICENSE`)

## Commit pinned

- Commit SHA: `4a17083770411d2af8fd99b1f249ea10143dc959`
- Commit date: 2026-02-13
- Vendored: 2026-04-22
- Status: full upstream copy, wrapped in `namespace SAOTRPG.ThirdParty { ... }`. No internal modifications.

## API surface consumed by SAOTRPG

Only `Map/Generation/HeightmapField.cs` uses this library. Relevant calls:

- `FastNoiseLite(int seed)`, `SetSeed`, `SetFrequency`
- `SetNoiseType(NoiseType.OpenSimplex2)`
- `SetFractalType(FractalType.FBm)`
- `SetFractalOctaves`, `SetFractalLacunarity`, `SetFractalGain`
- `SetDomainWarpType(DomainWarpType.OpenSimplex2)`, `SetDomainWarpAmp`
- `GetNoise(float, float)` → range ~[-1..1]
- `DomainWarp(ref float, ref float)`

The full upstream library exposes 3D noise, cellular/Worley, ping-pong fractals, and other variants. They are available via the same instance but unused today.

## Upgrade path

Do NOT hand-edit `FastNoiseLite.cs`. To refresh the vendored copy:

1. `curl -sSL -o FastNoiseLite.upstream.cs https://raw.githubusercontent.com/Auburn/FastNoiseLite/master/CSharp/FastNoiseLite.cs`
2. Record the latest commit SHA: `curl -sSL https://api.github.com/repos/Auburn/FastNoiseLite/commits/master | jq -r .sha` (or use `gh api repos/Auburn/FastNoiseLite/commits/master --jq .sha`)
3. Regenerate `FastNoiseLite.cs` with namespace wrap:
   ```
   { echo "// Upstream: https://github.com/Auburn/FastNoiseLite"; \
     echo "// Commit SHA: <SHA> (<DATE>)"; \
     echo "// Wrapped with SAOTRPG.ThirdParty namespace; do not hand-edit."; \
     echo ""; \
     echo "namespace SAOTRPG.ThirdParty"; \
     echo "{"; \
     cat FastNoiseLite.upstream.cs; \
     echo "}"; } > FastNoiseLite.cs
   ```
4. Update the "Commit pinned" section above with the new SHA and date.
5. Delete `FastNoiseLite.upstream.cs`.
6. Rebuild; API surface is stable so `HeightmapField` should not need changes.
