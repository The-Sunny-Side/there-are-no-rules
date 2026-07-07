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
- [x] S2 Scatter v2 (core, 2026-07-07) — incremental batch append
      (MatrixBlock shared by submesh batches, appendable open block per
      entry/part, full-capacity last chunk): 2500 appends in 2ms, structure
      identical to full rebuild (E2E). Per-entry footprintRadius (scaled,
      pairwise sum; legacy entries deserialize to 0 = old behavior). Single
      Place click mode (filters bypassed, footprints apply, Ctrl-drag erase
      kept; E2E blocked@3.5m free@4.5m with 2m+2m footprints).
      Deferred to S2b: lighter stroke undo, EditorPrefs, binary storage.
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
- [x] S6 Texture paint — Texture tab is dual: terrains get TerrainLayer splat
      (PaintTexture pass, auto-add, alphamap undo; E2E: weight 1 center/0
      edge); meshes get VERTEX COLOR painting (clone-based, undoable, E2E:
      red top/white bottom). Materials must read COLOR0 to show it — helper
      button swaps a scene-local copy using URP Particles/Lit (keeps
      _BaseMap/_BaseColor). Proper splat-mask shader painting = later.
- [x] S7 Grass/detail paint (terrain half) — Grass tab paints terrain detail
      density (texture billboard or mesh prefab, prototype auto-added +
      deduped), Ctrl fades to zero, dirty-rect undo records, density
      auto-scaled for CoverageMode (Unity 6 default) vs InstanceCountMode.
      E2E-verified on a real terrain (coverage 255 center / 0 after erase).
      Grass-on-mesh = Scatter tab for now; instanced mesh-grass is S7b.
- [x] S8 Procedural brush filters (scatter) — Noise Mask (Perlin patches,
      Patch Size + Coverage sliders), Only On Terrain Layer (splat weight
      under candidate, non-terrain hits pass), Curvature (hollows/bumps via
      4 neighbor probes along the normal). All feed the live rejection
      feedback. E2E: noise 99/44/0 pass at threshold 0/.5/.99 deterministic;
      layer weight 1.0 painted / 0.0 other; curvature −0.78 bump / +1.69 pit
      / 0.0 flat. TODO later: same filters on Grass/Sculpt tabs.
      Test gotcha: fresh colliders need Physics.SyncTransforms() in E2E.
- [x] S9 Splines (2026-07-07) — OmniSpline component (Catmull-Rom, world
      handles + add/remove points via custom editor) + Spline tab: Flatten
      Terrain Along (CPU exact-target carve, flat width + smoothstep feather,
      dirty-rect undo via SculptUndo.RecordHeights), Paint Texture Along
      (reuses splat pipeline), Scatter Along (spacing, Center/Left/Right/Both
      side offsets, jitter, footprints honored; uses Scatter tab layer +
      palette). Each op = one undo step. E2E: 41 samples/80m at exact 2m
      spacing; road carved to 5.00m on-path, 0.00 off-path.
      Limits: per-sample terrain lookup (seams at tile borders untested),
      spline stores local points (move the object moves the path — ops don't
      auto-reapply).
- [x] S10a Node-brush core (2026-07-07) — ProceduralBrush asset: layer stack
      (Constant / fbm Noise with octaves+ridged, blend Add/Sub/Mul/Min/Max),
      deterministic per world position. "Proc" op in the Sculpt tab paints
      the stack as height through the falloff, Ctrl digs, dirty-rect undo.
      E2E: constant +2m exact/additive/invertible, noise bounded+varied.
      Visual node-graph UI = later skin over this same data model.
      Terrain only for now (mesh via normal displacement later).
- [x] S10b Physics drop (2026-07-07) — "Drop & Settle" in the Scatter tab:
      temp rigidbodies (convex mesh collider from prefab, box fallback) fall,
      roll and pile against scene colliders and each other via editor
      Physics.Simulate (Script mode, sleep early-exit); settled transforms
      written back as instance data, one undo step. >2000 bodies asks first.
      E2E: two tilted cubes from 10m settle at y=0.50 exact.
- [ ] S10c Erosion brush, biome presets.

## Status (2026-07-06, end of day — field-tested by Simonpaolo on his map)
Done & E2E-verified: S1, S3, S4, S5, S6 (terrain splat + mesh vertex color),
S7 (terrain details, scatter-mode aware). All tabs work: Scatter | Sculpt |
Texture | Grass. Every real-usage failure became a fix (see Hardening).

Next (S8 + S2 + S9 + S10a + S10b done 2026-07-07):
1. Non-destructive layers (the MicroVerse-style differentiator, multi-session
   arc — start with the terrain op-stack data model).
2. S7b instanced mesh grass / S2b leftovers / S10c erosion + biome presets.
Also pending: Asset Store packaging pass (samples, docs, demo scene, name).

## Hardening from field testing (all fixed & committed 2026-07-06)
- Instances invisible until bake → every palette material had GPU Instancing
  OFF; renderer skips those batches. Red error + one-click fix in Scatter
  tab, consent prompt on Quick Add. (2ed02f0)
- Silent no-op stamps → live rejection feedback "placed 0/8: N height-
  filtered, N falloff, ..." in Scatter and Grass tabs. (7e6b843)
- Inspector "+" creates all-zero palette entries (weight 0 = never picked) →
  OnValidate heals fresh zeroed entries; window warns per-entry and blocks
  painting at zero total weight. (b508798)
- Mesh sculpt "bomb" → UV-seam duplicate vertices displaced along diverging
  normals tear the mesh; weld clusters cached on MeshDeformation, raise step
  halved. E2E: maxTear 0 on sphere (129 duplicates). (ed9552f)
- Grass invisible on Unity 6 terrains → CoverageMode expects 0–255, not
  instance counts; Density auto-scales per detailScatterMode. (ce24baa)
- Quick Add rejected hierarchy objects → resolves the prefab from a scene
  instance, explicit Add button, dedupe dialog. (d8c17d9)
- UX: Raise/Lower unified (Ctrl inverts), gray disc + label on invalid
  surfaces, slope filter numeric fields, guided scatter setup (auto-find
  layer, create palette, quick add). (ce84d26, 82d7756)

## Field notes (Simonpaolo's project)
- His map is MESH-based: grass there = Scatter tab until S7b; the Grass tab
  needs a real Unity Terrain. His test terrain accumulated tree/SpawnPoint
  detail prototypes — cleanup via Terrain inspector > Paint Details > Edit.
- His GeneralToonShader (ShaderGraph) reads no vertex colors: to integrate
  mesh texture painting properly, add a Vertex Color node × Base Color.
  Meanwhile the "Make Selected ... Show Vertex Colors" button works.

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
