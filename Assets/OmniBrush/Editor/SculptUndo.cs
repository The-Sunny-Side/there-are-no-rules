using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TerrainTools;

namespace OmniBrush.Editor
{
    /// <summary>
    /// Dirty-rect undo for terrain sculpting: per stamp, captures before/after
    /// heights of only the touched heightmap region — never a whole-TerrainData
    /// snapshot. A proxy object's version participates in Unity's undo stack;
    /// undo/redo replays region diffs to match the restored version.
    /// Records are in-memory: sculpt undo history is lost on domain reload.
    /// </summary>
    public static class SculptUndo
    {
        public enum StrokeKind { Heights, Alphamaps }

        private class Proxy : ScriptableObject
        {
            public int version;
        }

        private class StampRecord
        {
            public TerrainData data;
            public StrokeKind kind;
            public int x, y;
            public float[,] before, after;       // Heights
            public float[,,] beforeA, afterA;    // Alphamaps
            public Mesh mesh;                    // mesh vertex records
            public int[] indices;
            public Vector3[] beforeV, afterV;
            public bool HasAfter => mesh != null ? afterV != null
                : kind == StrokeKind.Heights ? after != null : afterA != null;
        }

        private const int MaxStrokes = 64;
        private static Proxy proxy;
        private static readonly List<List<StampRecord>> Strokes = new List<List<StampRecord>>();
        private static int baseVersion;    // state before Strokes[0]
        private static int appliedVersion; // state the terrains currently reflect
        private static List<StampRecord> currentStroke;
        private static StrokeKind currentKind;

        static SculptUndo() => Undo.undoRedoPerformed += OnUndoRedo;

        public static void BeginStroke(StrokeKind kind)
        {
            currentKind = kind;
            if (proxy == null)
            {
                proxy = ScriptableObject.CreateInstance<Proxy>();
                proxy.hideFlags = HideFlags.HideAndDontSave;
                baseVersion = appliedVersion = proxy.version = 0;
                Strokes.Clear();
            }

            // discard redo tail
            int keep = appliedVersion - baseVersion;
            if (Strokes.Count > keep) Strokes.RemoveRange(keep, Strokes.Count - keep);

            Undo.RegisterCompleteObjectUndo(proxy, "OmniBrush Sculpt");
            proxy.version = ++appliedVersion;
            currentStroke = new List<StampRecord>();
            Strokes.Add(currentStroke);
            if (Strokes.Count > MaxStrokes)
            {
                Strokes.RemoveAt(0);
                baseVersion++;
            }
            TerrainPaintableSurface.captureHook = Capture;
            MeshPaintableSurface.recordHook = RecordMesh;
        }

        public static void EndStroke()
        {
            TerrainPaintableSurface.captureHook = null;
            MeshPaintableSurface.recordHook = null;
            currentStroke = null;
        }

        private static void RecordMesh(Mesh mesh, int[] indices, Vector3[] before, Vector3[] after)
        {
            if (currentStroke == null) return;
            currentStroke.Add(new StampRecord
            {
                mesh = mesh, indices = indices, beforeV = before, afterV = after,
            });
        }

        private static void Capture(PaintContext ctx, bool before)
        {
            if (currentStroke == null) return;
            if (before)
            {
                for (int i = 0; i < ctx.terrainCount; i++)
                {
                    RectInt r = ctx.GetClippedPixelRectInTerrainPixels(i);
                    if (r.width <= 0 || r.height <= 0) continue;
                    TerrainData data = ctx.GetTerrain(i).terrainData;
                    var rec = new StampRecord { data = data, kind = currentKind, x = r.x, y = r.y };
                    if (currentKind == StrokeKind.Heights)
                        rec.before = data.GetHeights(r.x, r.y, r.width, r.height);
                    else
                        rec.beforeA = data.GetAlphamaps(r.x, r.y, r.width, r.height);
                    currentStroke.Add(rec);
                }
            }
            else
            {
                // fill "after" for this stamp's records (the trailing ones without it)
                for (int i = currentStroke.Count - 1; i >= 0 && !currentStroke[i].HasAfter; i--)
                {
                    StampRecord rec = currentStroke[i];
                    if (rec.kind == StrokeKind.Heights)
                        rec.after = rec.data.GetHeights(rec.x, rec.y, rec.before.GetLength(1), rec.before.GetLength(0));
                    else
                        rec.afterA = rec.data.GetAlphamaps(rec.x, rec.y, rec.beforeA.GetLength(1), rec.beforeA.GetLength(0));
                }
            }
        }

        private static void OnUndoRedo()
        {
            if (proxy == null || appliedVersion == proxy.version) return;
            int target = Mathf.Clamp(proxy.version, baseVersion, baseVersion + Strokes.Count);
            while (appliedVersion > target)
            {
                List<StampRecord> stroke = Strokes[appliedVersion - 1 - baseVersion];
                for (int i = stroke.Count - 1; i >= 0; i--)
                    Apply(stroke[i], true);
                appliedVersion--;
            }
            while (appliedVersion < target)
            {
                List<StampRecord> stroke = Strokes[appliedVersion - baseVersion];
                for (int i = 0; i < stroke.Count; i++)
                    Apply(stroke[i], false);
                appliedVersion++;
            }
        }

        private static void Apply(StampRecord rec, bool useBefore)
        {
            if (rec.mesh != null)
            {
                Vector3[] target = useBefore ? rec.beforeV : rec.afterV;
                if (target == null) return;
                Vector3[] verts = rec.mesh.vertices;
                for (int i = 0; i < rec.indices.Length; i++)
                    verts[rec.indices[i]] = target[i];
                rec.mesh.vertices = verts;
                rec.mesh.RecalculateNormals();
                rec.mesh.RecalculateBounds();
                return;
            }
            if (rec.data == null) return;
            if (rec.kind == StrokeKind.Heights)
            {
                float[,] h = useBefore ? rec.before : rec.after;
                if (h != null) rec.data.SetHeights(rec.x, rec.y, h);
            }
            else
            {
                float[,,] a = useBefore ? rec.beforeA : rec.afterA;
                if (a != null) rec.data.SetAlphamaps(rec.x, rec.y, a);
            }
        }
    }
}
