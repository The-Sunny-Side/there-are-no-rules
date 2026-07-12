# OmniBrush — Contributor Guide

One brush for everything: scatter, sculpt, texture, grass, splines on
**Terrain, meshes and prefabs**. This doc is the stable reference for anyone
(human or agent) touching the code. Current status and what to build next
live in [PLAN.md](PLAN.md) — read both before writing code.

## Ground rules
1. **One feature = one commit**, message `feat|fix(omnibrush): ...`, after the
   feature is compile-checked AND behavior-verified in the editor (see loop).
2. **Never fail silently.** Every op that can produce "nothing happened" must
   say why, in the window (`lastStampWarning`, HelpBoxes, gray brush disc with
   a label). Every field-tested bug so far was a silent no-op.
3. **Every user-facing control gets a tooltip** (`GUIContent` in the window,
   `[Tooltip]` on serialized fields). Short, says what changes when you move it.
4. **Undo everything** through the existing systems (below). Never
   `Undo.RecordObject` a whole TerrainData.
5. Update PLAN.md (status + roadmap checkbox + known limits) in the same commit.

## Verify loop (Unity MCP)
The Unity bridge (`.mcp.json` → HTTP `127.0.0.1:8080/mcp`) lets you compile
and run code inside the editor:
1. `initialize` handshake (capture `mcp-session-id` header) + `notifications/initialized`.
2. `refresh_unity` → triggers compile + domain reload. If it times out, check
   `Library/ScriptAssemblies/OmniBrush.*.dll` timestamps before trusting any
   result — `execute_code` may run STALE code.
3. `read_console` (types: error) → must be 0 entries.
4. `execute_code` for E2E: create throwaway rigs far from origin (temp
   TerrainData/primitives at ~(-9000,-9000)), run ops, **measure numbers**,
   destroy everything in `finally`. Gotchas: the CodeDom compiler needs
   explicit `ref` for `in`/ByRef params; freshly created colliders need
   `Physics.SyncTransforms()` before raycasts; internal types aren't visible.
5. Assert expected values, not just "no exception". Real bugs found this way:
   flatten NaN at strength 1, stamp pass param layout, coverage-mode grass,
   seam-tearing mesh sculpt, 3D-vs-lateral brush distance.

## Architecture map

### Scatter (paint prefabs on anything)
- `Runtime/ScatterPalette.cs` — SO: entries (prefab, weight, scale, yaw,
  align, verticalOffset, footprintRadius). `OnValidate` heals inspector-zeroed
  entries. `PickWeightedIndex(random01)`.
- `Runtime/ScatterLayer.cs` — instances are pure data (world-space TRS +
  entry index), rendered with `Graphics.DrawMeshInstanced`:
  `MatrixBlock` (≤1023 matrices, shared by submesh `Batch`es) + appendable
  open block per (entry, part) → `AddInstance` is O(1), no rebuild per stamp.
  XZ spatial hash for `OverlapsExisting` (footprintA+footprintB or global min
  distance). Render hooks: SRP `beginCameraRendering` + built-in `onPreCull`.
  Materials without GPU Instancing are SKIPPED (UI surfaces this loudly).
  `UpdateInstance` for physics drop. Layer transform is ignored by design.
- `Editor/ScatterMaterialUtility.cs` — count/enable instancing on palette materials.
- `Editor/PhysicsDrop.cs` — temp rigidbodies (convex MeshCollider, box
  fallback) + `Physics.simulationMode = Script` + `Physics.Simulate` loop with
  sleep early-exit; writes settled TRS back; one undo.

### Sculpt / Texture / Grass
- `Runtime/Sculpt/IPaintableSurface.cs` — `SculptOp`, `SculptStampArgs`,
  the surface seam. Implementations: terrain + mesh.
- `Runtime/Sculpt/TerrainPaintableSurface.cs` — GPU PaintContext pipeline
  (brush transform → BeginPaintHeightmap → builtin material pass → End +
  `PaintContext.ApplyDelayedActions()`). Pass params (probed empirically):
  RaiseLower `x=±strength*0.01`; Flatten/SetHeights `x=strength*0.01`
  (shader NaNs at 1.0!), `y=kNormalizedHeightScale*target01`; Stamp
  `x=strength, z=kScale*h01, w=0 max/1 add`; PaintTexture `x=strength,
  y=1`. `captureHook` (before/after per stamp) feeds undo.
  `ApplyTexturePaint` auto-adds missing TerrainLayers.
- `Runtime/Sculpt/MeshPaintableSurface.cs` — clone-on-first-stamp (never the
  shared asset; `MeshDeformation` marker + revert), **cylindrical** brush
  (lateral-distance weights — 3D distance stalls on displaced verts),
  **weld clusters** for co-located seam duplicates (they must move together
  or the mesh tears), ops: raise/lower (weld-averaged normals), smooth
  (plane-relax), flatten (plane project), stamp (shape displacement),
  `ApplyProcedural`, `ApplyVertexColor`. `recordHook`/`colorRecordHook` feed
  undo. `RefreshCollider` re-cooks MeshColliders at stroke end.
- `Runtime/Sculpt/SculptBrushTexture.cs` — procedural falloff brush textures.
- `Runtime/Grass/TerrainDetailPainter.cs` — terrain detail density, CPU
  dirty-rect. **Scales the target to `detailScatterMode`** (CoverageMode =
  0–255, InstanceCount = per-cell). `EnsurePrototype` dedupes/auto-adds.
- `Runtime/Filters/BrushFilters.cs` — noise gate, splat weight under hit,
  curvature via 4 neighbor probes. `Runtime/Filters/ProceduralBrush.cs` —
  node-brush core: layer stack (Constant/fbm Noise, blends), deterministic
  `Evaluate(worldX, worldZ)` in meters.

### Undo (the heart — extend, don't reinvent)
- `Editor/SculptUndo.cs` — dirty-rect undo for everything non-GameObject.
  A hidden proxy SO's `version` int sits in Unity's undo stack; strokes store
  typed `StampRecord`s (terrain heights, alphamaps, detail density, mesh
  verts, mesh colors); undo/redo replays region diffs to match the restored
  version. `BeginStroke(kind)` installs the capture hooks, `EndStroke()`
  clears them. Public writers for CPU ops: `RecordHeights`, plus the hooks.
  Records are in-memory: history is lost on domain reload (no-ops safely).
  **New data type? Add a record field + branch in `Apply`/`HasAfter`.**
- Scatter instance edits: plain `Undo.RegisterCompleteObjectUndo(layer)` per
  stroke (list snapshot — heavy at 100k+, acceptable).

### Splines
- `Runtime/Spline/OmniSpline.cs` — Catmull-Rom over local points,
  `SampleByDistance` (arc-length resample), always-visible gizmos.
- `Editor/OmniSplineEditor.cs` — position handles, Shift+Click appends points.
- `Editor/SplineOps.cs` — `FindTerrainAt`, CPU `FlattenStamp` (exact target,
  flat core + smoothstep feather — the GPU flatten converges too slowly for
  roads). `Editor/ProceduralOps.cs` — CPU proc stamp for terrains.
- Window spline tab orchestrates: per sample → terrain op, else raycast down
  → mesh op (flatten via `ApplyStamp`, texture via `ApplyVertexColor`).

### The window
- `Editor/OmniBrushWindow.cs` — Tools > OmniBrush > Brush. Tabs: Scatter |
  Sculpt | Texture | Grass | Spline. One `OnSceneGUI` handles raycast, brush
  disc (gray + label when the surface can't take the mode), stroke lifecycle
  (`hotControl`, Esc, Ctrl modifiers) and routes stamps per mode. Scatter
  stamps count every rejection reason into `lastStampWarning` when a stamp
  places 0. Spline tab is button-driven (no brush).

## How to add a feature (checklist)
1. Runtime logic in `Runtime/` (asmdef `OmniBrush.Runtime`) — keep it
   editor-free so it can go runtime later; editor-only glue in `Editor/`.
2. Undo: pick the right integration (SculptUndo record type, capture hook, or
   RegisterCompleteObjectUndo for scatter data).
3. UI: tab section or op entry; tooltips on every control; a HelpBox stating
   scope and modifier keys; feedback when the op does nothing.
4. E2E via MCP with measured numbers; fix what the numbers reveal.
5. PLAN.md: tick the roadmap entry, note accepted limits.
6. Commit. Stage `Assets/OmniBrush` paths explicitly — the user's project has
   unrelated changes in the same repo.

## Next work
See PLAN.md "Next" — currently: non-destructive layers (op-stack data model
first), instanced mesh grass (S7b), erosion, biome presets, Asset Store
packaging (demo scene, docs, final name — "OmniBrush" is a working title).
