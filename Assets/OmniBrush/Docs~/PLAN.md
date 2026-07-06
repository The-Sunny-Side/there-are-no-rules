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
- [x] S3 Terrain sculpt — raise/lower/smooth/flatten via PaintContext (GPU,
      multi-tile), dirty-rect undo (SculptUndo proxy versioning), window got
      Scatter/Sculpt tabs, IPaintableSurface introduced. E2E-verified via MCP
      (raise 10m exact, flatten converges, smooth lowers peaks).
- [x] S4 Mesh sculpt — all sculpt ops (raise/lower/smooth/flatten/stamp) on
      any collider'd mesh. First stamp clones the shared mesh (MeshDeformation
      marker + revert button); cylindrical brush (lateral-distance weights —
      3D-distance version made flatten stall on displaced verts, E2E caught
      it); vertex-diff undo records; MeshCollider re-cooked at stroke end.
      CPU vertex ops for now — GPU delta-map path later for high-poly.
- [x] S5 Heightmap stamps — Stamp op in sculpt tab: custom heightmap texture
      (or procedural round brush), world-height target, max blend (exact +
      idempotent, E2E-verified) or additive, fixed/random rotation. Click-only.
      Shader params: x=strength, z=kScale*h01 packed, w=0 max/1 add.
- [x] S6 Texture paint (terrain half) — Texture tab: paint TerrainLayer
      weights via PaintTexture pass, auto-adds missing layers, alphamap
      dirty-rect undo (SculptUndo StrokeKind). E2E-verified (weight 1 center /
      0 edge, auto-add 2→3 layers). Mesh vertex color still open (→ S4/S6b).
- [x] S7 Grass/detail paint (terrain half) — Grass tab paints terrain detail
      density (texture billboard or mesh prefab, prototype auto-added +
      deduped), Ctrl fades to zero, dirty-rect undo records. E2E-verified
      (density 8 center / 0 corner / 0 after erase). Grass-on-mesh = Scatter
      tab for now; dedicated instanced mesh-grass is S7b.
- [ ] S8 Procedural brush filters — noise, curvature, texture-under-brush.
- [ ] S9 Splines (roads/rivers: carve + texture + scatter along).
- [ ] S10 Physics drop, erosion brush, node-driven brushes, biome presets.

## Status (2026-07-06)
S1 + S3 + S4 + S5 + S6(terrain) + S7(terrain) done, all E2E-checked via Unity
MCP. Tabs: Scatter | Sculpt | Texture | Grass; sculpt/stamp on terrain AND
meshes. Remaining, suggested order: S2 (scatter perf + per-entry footprint
radius — user deferred), S8 (procedural filters), S9 (splines), S6b/S7b
(mesh vertex color / instanced mesh grass), S10 (physics drop, erosion,
nodes, biome presets, non-destructive layers).

## S4 known limits (accepted)
- Primitive colliders (Sphere/Box) don't follow deformation — raycasts drift
  after big edits; use a MeshCollider (re-cooked at stroke end) for accuracy.
- CPU per-stamp full vertex scan: fine to ~100k verts, GPU delta map later.
- Smooth = relax-toward-local-plane (adjacency-free), not Laplacian.
- Custom stamp textures need Read/Write enabled on meshes (analytic fallback).
- The clone mesh is serialized into the scene; undo restores vertices but
  doesn't swap back to the original asset (use the Revert inspector button).

## S6 known limits (accepted)
- Undo restores alphamap weights but keeps auto-added TerrainLayers.
- Painting a layer not yet on the terrain while capture is off (no stroke)
  never happens from the window; API callers must BeginStroke first.

## S3 known limits (accepted)
- Sculpt undo history is in-memory: lost on domain reload / play mode (proxy
  survives Unity's stack but records don't; undo then no-ops safely).
- Overlapping stamps in one stroke store redundant region copies.
- Flatten uses Unity's 0.01-strength shader semantics (NaN at 1.0 — the
  `w=(1-p)/p` smoothing math); converges over repeated stamps by design.
- Scatter instances do not follow terrain height edits.

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
- `Runtime/Sculpt/IPaintableSurface.cs` — SculptOp enum, SculptStampArgs,
  surface interface (runtime-capable by design).
- `Runtime/Sculpt/TerrainPaintableSurface.cs` — PaintContext pipeline (brush
  transform → BeginPaintHeightmap → builtin material pass blit → End +
  ApplyDelayedActions), `captureHook` for editor undo, `TryFrom(Collider)`.
- `Runtime/Sculpt/SculptBrushTexture.cs` — procedural radial falloff brushes,
  cached per hardness step.
- `Runtime/Sculpt/MeshPaintableSurface.cs` — mesh sculpting: clone-on-first-
  stamp, cylindrical brush gather, per-op vertex displacement, recordHook for
  undo, RefreshCollider.
- `Runtime/Sculpt/MeshDeformation.cs` — marker: original + deformed mesh.
- `Editor/MeshDeformationEditor.cs` — revert-to-original button.
- `Editor/SculptUndo.cs` — dirty-rect undo: per-stamp before/after height
  regions via PaintContext tile rects, proxy-version replay on undo/redo.
- `Editor/OmniBrushWindow.cs` — Tools > OmniBrush > Brush; Scatter/Sculpt
  tabs, all brush logic (raycast, stamp sampling, filters, undo routing).
- `Editor/ScatterLayerEditor.cs` — count, open-brush, refresh cache,
  enable-GPU-instancing fix, bake to GameObjects.
