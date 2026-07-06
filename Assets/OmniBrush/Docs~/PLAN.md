# OmniBrush — one brush for everything (working title)

Vision: the most complete painting tool on the Unity Asset Store — scatter,
sculpt, texture, grass, stamps on **Terrain, meshes and prefabs**, with
procedural filters. One tool to build a whole map.

Session rules: keep this file updated (status + next step), smallest useful
increments, everything under `Assets/OmniBrush`, namespace `OmniBrush`.
Env: Unity 6000.2.10f1, URP 17.2, branch `PaintableSurface`.

## Locked architecture decisions
1. Scatter = pure data (`ScatterLayer` component, world-space instances)
   rendered via GPU instancing; "Bake to GameObjects" only when
   colliders/gameplay are needed.
2. Terrain undo (S3+): own per-region snapshots (dirty rects), never
   `Undo.RecordObject` on the whole TerrainData.
3. Brush ops on GPU (S3+): RenderTexture/PaintContext pipeline, reused for meshes.
4. Mesh deform (S4): never touch the shared mesh asset — per-instance clone + delta.
5. `IPaintableSurface` abstraction lands in S3, when the 2nd implementation exists.

## Roadmap
- [x] S1 Scatter MVP — palette SO, layer (data + instanced render + frustum
      cull), brush window (paint / Ctrl-erase, slope/height/layer filters,
      stroke spacing, min distance, edge falloff), per-stroke undo, bake, LOD0.
- [ ] S2 Scatter v2 — incremental batch update (no full rebuild per stamp),
      lighter undo (delta, not full snapshot), EditorPrefs persistence,
      precision single-place mode, binary instance storage (scene bloat).
- [ ] S3 Terrain sculpt — raise/lower/smooth/flatten via PaintContext,
      dirty-rect undo, introduce IPaintableSurface.
- [ ] S4 Mesh sculpt — per-instance clone + delta map, same brush set as S3.
- [ ] S5 Heightmap stamps (rotation, blend modes add/max/min).
- [ ] S6 Texture paint — terrain splat; vertex color on meshes.
- [ ] S7 Grass/detail paint (terrain details + instanced grass on meshes).
- [ ] S8 Procedural brush filters — noise, curvature, texture-under-brush.
- [ ] S9 Splines (roads/rivers: carve + texture + scatter along).
- [ ] S10 Physics drop, erosion brush, node-driven brushes, biome presets.

## Status (2026-07-06)
S1 implemented and compile-checked via Unity MCP. Next: S2 or S3.

## S1 known limits (accepted, revisit in S2)
- Paint target needs a collider (mesh BVH raycast comes with S3/S4).
- Full batch rebuild per stamp → hitches above ~50k instances.
- Stroke undo = full instance-list snapshot (heavy at 100k+).
- LOD0 only; materials must have GPU Instancing on (fix button in inspector).
- `ScatterLayer` transform is ignored — instances are world-space; keep the
  layer object at identity.
- 2D (XZ) spatial hash — stacked vertical painting queries degrade.

## Map
- `Runtime/ScatterPalette.cs` — SO: entries (prefab, weight, scale range,
  random yaw, align-to-normal, vertical offset) + weighted pick.
- `Runtime/ScatterLayer.cs` — instance list, XZ spatial hash, batch build
  (≤1023/batch), render hooks (SRP `beginCameraRendering` + built-in
  `onPreCull`), frustum cull, public data API (`AddInstance`,
  `RemoveInRadius`, `HasInstanceWithin`, `MarkDirty`, `MarkPaletteDirty`).
- `Editor/OmniBrushWindow.cs` — Tools > OmniBrush > Brush; all brush logic
  (raycast, stamp sampling, filters, undo registration).
- `Editor/ScatterLayerEditor.cs` — count, open-brush, refresh cache,
  enable-GPU-instancing fix, bake to GameObjects.
