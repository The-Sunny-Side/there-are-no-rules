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
        private class Proxy : ScriptableObject
        {
            public int version;
        }

        private class StampRecord
        {
            public TerrainData data;
            public int x, y;
            public float[,] before, after;
        }

        private const int MaxStrokes = 64;
        private static Proxy proxy;
        private static readonly List<List<StampRecord>> Strokes = new List<List<StampRecord>>();
        private static int baseVersion;    // state before Strokes[0]
        private static int appliedVersion; // state the terrains currently reflect
        private static List<StampRecord> currentStroke;

        static SculptUndo() => Undo.undoRedoPerformed += OnUndoRedo;

        public static void BeginStroke()
        {
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
        }

        public static void EndStroke()
        {
            TerrainPaintableSurface.captureHook = null;
            currentStroke = null;
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
                    currentStroke.Add(new StampRecord
                    {
                        data = data,
                        x = r.x, y = r.y,
                        before = data.GetHeights(r.x, r.y, r.width, r.height),
                    });
                }
            }
            else
            {
                // fill "after" for this stamp's records (the trailing ones without it)
                for (int i = currentStroke.Count - 1; i >= 0 && currentStroke[i].after == null; i--)
                {
                    StampRecord rec = currentStroke[i];
                    rec.after = rec.data.GetHeights(rec.x, rec.y, rec.before.GetLength(1), rec.before.GetLength(0));
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
                    if (stroke[i].data != null && stroke[i].before != null)
                        stroke[i].data.SetHeights(stroke[i].x, stroke[i].y, stroke[i].before);
                appliedVersion--;
            }
            while (appliedVersion < target)
            {
                List<StampRecord> stroke = Strokes[appliedVersion - baseVersion];
                for (int i = 0; i < stroke.Count; i++)
                    if (stroke[i].data != null && stroke[i].after != null)
                        stroke[i].data.SetHeights(stroke[i].x, stroke[i].y, stroke[i].after);
                appliedVersion++;
            }
        }
    }
}
