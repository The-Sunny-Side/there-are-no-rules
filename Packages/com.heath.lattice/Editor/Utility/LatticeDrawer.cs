using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.Editor
{
	/// <summary>
	/// Utility class for drawing lattices in the scene
	/// </summary>
	public class LatticeDrawer
	{
		private Lattice _lattice;

		private readonly List<Line> _lines = new();

		public LatticeDrawer(Lattice lattice)
		{
			_lattice = lattice;
		}

		public void Draw()
		{
			if (Event.current.type == EventType.Layout)
			{
				SortLines();
			}
			else if (Event.current.type == EventType.Repaint)
			{
				DrawLines(LatticeSettings.LineBehindOpacity, CompareFunction.Greater);
				DrawLines(LatticeSettings.LineInFrontOpacity, CompareFunction.LessEqual);
			}
		}

		private static readonly LatticeDrawer _instance = new(null);

		public static void Draw(Lattice lattice)
		{
			if (Event.current.type == EventType.Repaint)
			{
				_instance._lattice = lattice;
				_instance.SortLines();
				_instance.DrawLines(LatticeSettings.LineBehindOpacity, CompareFunction.LessEqual);
			}
		}

		private void SortLines()
		{
			_lines.Clear();

			// Get all lines
			for (int i = 0; i < _lattice.Resolution.x; i++)
			{
				for (int j = 0; j < _lattice.Resolution.y; j++)
				{
					for (int k = 0; k < _lattice.Resolution.z; k++)
					{
						Vector3 pos = _lattice.GetHandleWorldPosition(i, j, k);

						if (i != _lattice.Resolution.x - 1) _lines.Add(new Line(pos, _lattice.GetHandleWorldPosition(i + 1, j, k)));
						if (j != _lattice.Resolution.y - 1) _lines.Add(new Line(pos, _lattice.GetHandleWorldPosition(i, j + 1, k)));
						if (k != _lattice.Resolution.z - 1) _lines.Add(new Line(pos, _lattice.GetHandleWorldPosition(i, j, k + 1)));
					}
				}
			}

			static int CompareDepth(Line a, Line b)
			{
				Vector3 forward = Camera.current.transform.forward;
				float depthA = Vector3.Dot(forward, a.Centre());
				float depthB = Vector3.Dot(forward, b.Centre());
				float difference = 100 * (depthB - depthA);
				return (int)difference;
			}

			// Sort lines
			_lines.Sort(CompareDepth);
		}

		private void DrawLines(float alpha, CompareFunction compareFunction)
		{
			CompareFunction previousZTest = Handles.zTest;
			Handles.zTest = compareFunction;

			for (int i = 0; i < _lines.Count; i++)
			{
				DrawLine(_lines[i], alpha);
			}

			Handles.zTest = previousZTest;
		}

		private static readonly Vector3[] _line = new Vector3[2];

		private void DrawLine(Line line, float alpha)
		{
			Vector3 relativeOffset = _lattice.transform.InverseTransformVector(line.B - line.A);
			relativeOffset = Vector3.Scale(relativeOffset, _lattice.Resolution - Vector3Int.one);

			float squishStretchFactor = Mathf.Clamp(relativeOffset.magnitude - 1f, -1f, 1f);

			// Determine line thickness
			float minThickness = LatticeSettings.LineThickness;
			float maxThickness = (squishStretchFactor < 0)
				? LatticeSettings.GlowThicknessSquish
				: LatticeSettings.GlowThicknessStretch;
			float glowThickness = 2 * Mathf.Lerp(minThickness, maxThickness, Mathf.Abs(squishStretchFactor));

			// Store line in array to prevent allocation
			_line[0] = line.A;
			_line[1] = line.B;

			// Draw stretch/squish glow
			Color glowColour = LatticeSettings.GlowColorGradient.Evaluate(0.5f * squishStretchFactor + 0.5f);
			glowColour.a *= alpha * LatticeSettings.GlowOpacity;

			using (new Handles.DrawingScope(glowColour, Matrix4x4.identity))
			{
				Handles.DrawAAPolyLine(glowThickness, _line);
			}

			// Draw constant width line
			Color lineColour = LatticeSettings.LineColor;
			lineColour.a *= alpha;

			using (new Handles.DrawingScope(lineColour, Matrix4x4.identity))
			{
				Handles.DrawAAPolyLine(LatticeSettings.LineThickness, _line);
			}
		}

		struct Line
		{
			public Vector3 A;
			public Vector3 B;

			public Line(Vector3 a, Vector3 b)
			{
				A = a;
				B = b;
			}

			public readonly Vector3 Centre()
			{
				return (A + B) / 2;
			}
		}
	}
}
