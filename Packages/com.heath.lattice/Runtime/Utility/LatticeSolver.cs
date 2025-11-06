using System;
using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Functions to calculate lattice deformation on the CPU.<br/>
	/// These should be equivalent to the GPU implementation found in LatticeCompute.compute.
	/// </summary>
	public static class LatticeSolver
	{
		#region Public Methods

		/// <summary>
		/// Deforms a point.
		/// </summary>
		public static Vector3 DeformPoint(LatticeItem item, Vector3 point)
		{
			Transform transform = item.Lattice.transform;
			Span<Vector3> input = stackalloc Vector3[1];
			Span<Vector3> output = stackalloc Vector3[1];

			input[0] = transform.InverseTransformPoint(point);
			TransformPositions(item, input, output);
			return transform.TransformPoint(output[0]);
		}

		/// <summary>
		/// Deforms a transform matrix, including position, orientation and scale.
		/// </summary>
		public static Matrix4x4 DeformTransform(LatticeItem item, Matrix4x4 localToWorld)
		{
			Transform transform = item.Lattice.transform;
			Span<Vector3> input = stackalloc Vector3[4];
			Span<Vector3> output = stackalloc Vector3[4];

			input[0] = transform.InverseTransformPoint(localToWorld.GetPosition());
			input[1] = transform.InverseTransformPoint(localToWorld.GetPosition() + 0.001f * (Vector3)localToWorld.GetColumn(0));
			input[2] = transform.InverseTransformPoint(localToWorld.GetPosition() + 0.001f * (Vector3)localToWorld.GetColumn(1));
			input[3] = transform.InverseTransformPoint(localToWorld.GetPosition() + 0.001f * (Vector3)localToWorld.GetColumn(2));

			TransformPositions(item, input, output);

			Vector3 position = transform.TransformPoint(output[0]);

			localToWorld = new Matrix4x4(
				1000f * (transform.TransformPoint(output[1]) - position),
				1000f * (transform.TransformPoint(output[2]) - position),
				1000f * (transform.TransformPoint(output[3]) - position),
				position
			);

			localToWorld[3, 3] = 1;

			return localToWorld;
		}

		#endregion

		#region Coefficients

		private interface ICoefficients
		{
			int Length { get; }
			void GetCoefficients(Vector3 cellPosition);
			float GetWeight(Vector3Int cell);
		}

		private struct CubicCoefficients : ICoefficients
		{
			private Vector4 _x;
			private Vector4 _y;
			private Vector4 _z;

			public readonly int Length => 4;

			public void GetCoefficients(Vector3 cellPosition)
			{
				Vector3 h_00 = Aa1(cellPosition + Vector3.one);
				Vector3 h_10 = Aa0(cellPosition);
				Vector3 h_01 = Aa0(Vector3.one - cellPosition);
				Vector3 h_11 = Aa1(2 * Vector3.one - cellPosition);

				_x = new Vector4(h_00.x, h_10.x, h_01.x, h_11.x);
				_y = new Vector4(h_00.y, h_10.y, h_01.y, h_11.y);
				_z = new Vector4(h_00.z, h_10.z, h_01.z, h_11.z);
			}

			private static Vector3 Aa0(Vector3 t)
			{
				Vector3 t2 = Vector3.Scale(t, t);
				return 1.5f * Vector3.Scale(t2, t) - 2.5f * t2 + Vector3.one;
			}

			private static Vector3 Aa1(Vector3 t)
			{
				Vector3 t2 = Vector3.Scale(t, t);
				return -0.5f * Vector3.Scale(t2, t) + 2.5f * t2 - 4.0f * t + 2.0f * Vector3.one;
			}

			public float GetWeight(Vector3Int cell)
			{
				return _x[cell.x] * _y[cell.y] * _z[cell.z];
			}
		}

		private struct LinearCoefficients : ICoefficients
		{
			private Vector2 _x;
			private Vector2 _y;
			private Vector2 _z;

			public readonly int Length => 2;

			public void GetCoefficients(Vector3 cellPosition)
			{
				Vector3 h_10 = Vector3.one - cellPosition;
				Vector3 h_01 = cellPosition;

				_x = new Vector2(h_10.x, h_01.x);
				_y = new Vector2(h_10.y, h_01.y);
				_z = new Vector2(h_10.z, h_01.z);
			}

			public float GetWeight(Vector3Int cell)
			{
				return _x[cell.x] * _y[cell.y] * _z[cell.z];
			}
		}

		private struct SmoothLinearCoefficients : ICoefficients
		{
			private LinearCoefficients _coeff;

			public readonly int Length => _coeff.Length;

			public void GetCoefficients(Vector3 cellPosition)
			{
				cellPosition = new(
					Mathf.SmoothStep(0, 1, cellPosition.x),
					Mathf.SmoothStep(0, 1, cellPosition.y),
					Mathf.SmoothStep(0, 1, cellPosition.z)
				);
				_coeff.GetCoefficients(cellPosition);
			}

			public float GetWeight(Vector3Int cell)
			{
				return _coeff.GetWeight(cell);
			}
		}

		#endregion

		#region Private Methods

		private static Vector3 GetOffset(Lattice lattice, Vector3Int cell, bool global)
		{
			if (!global)
			{
				if ((cell.x < 0) || (cell.x >= lattice.Resolution.x) ||
					(cell.y < 0) || (cell.y >= lattice.Resolution.y) ||
					(cell.z < 0) || (cell.z >= lattice.Resolution.z))
					return Vector3.zero;
			}

			cell = Vector3Int.Max(cell, Vector3Int.zero);
			cell = Vector3Int.Min(cell, lattice.Resolution - Vector3Int.one);
			return lattice.GetHandleOffset(cell);
		}

		private static Vector3 GetCellPosition(Lattice lattice, Vector3 latticePosition, Vector3Int cell) 
		{
			latticePosition += 0.5f * Vector3.one;
			latticePosition.Scale(lattice.Resolution - Vector3Int.one);
			return latticePosition - cell;
		}

		private static Vector3Int GetCell(Lattice lattice, Vector3 latticePosition)
		{
			latticePosition += 0.5f * Vector3.one;
			latticePosition.Scale(lattice.Resolution - Vector3Int.one);
			return Vector3Int.FloorToInt(latticePosition);
		}

		private static void TransformPositions(LatticeItem item, ReadOnlySpan<Vector3> inputs, Span<Vector3> outputs)
		{
			switch (item.Interpolation)
			{
				case InterpolationMethod.LinearSharp:
					TransformPositions<LinearCoefficients>(item, inputs, outputs);
					break;
				case InterpolationMethod.LinearSmooth:
					TransformPositions<SmoothLinearCoefficients>(item, inputs, outputs);
					break;
				case InterpolationMethod.Cubic:
					TransformPositions<CubicCoefficients>(item, inputs, outputs);
					break;
			}
		}

		private static void TransformPositions<Coefficients>(LatticeItem item, ReadOnlySpan<Vector3> input, Span<Vector3> output) where Coefficients : unmanaged, ICoefficients
		{
			int numPositions = input.Length;
			int numSamples = default(Coefficients).Length;

			Lattice lattice = item.Lattice;
			bool global = item.Global;

			float multiplier = (item.Mask.Vertex.Type != LatticeMask.VertexSettings.MaskType.None) 
				? item.Mask.Vertex.Multiplier : 1.0f;

			Vector3Int cell = GetCell(lattice, input[0]);

			Span<Coefficients> h = stackalloc Coefficients[numPositions];
			for (int r = 0; r < numPositions; r++)
			{
				h[r].GetCoefficients(GetCellPosition(lattice, input[r], cell));
				output[r] = input[r];
			}

			for (int i = 0; i < numSamples; i++)
			{
				for (int j = 0; j < numSamples; j++)
				{
					for (int k = 0; k < numSamples; k++)
					{
						Vector3Int index = new(i, j, k);
						Vector3Int handle = cell + index - (numSamples / 2 - 1) * Vector3Int.one;
						Vector3 handleOffset = multiplier * GetOffset(lattice, handle, global);

						for (int m = 0; m < numPositions; m++)
						{
							output[m] += handleOffset * h[m].GetWeight(index);
						}
					}
				}
			}
		}

		#endregion
	}
}
