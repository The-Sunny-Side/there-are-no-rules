using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Editor
{
	/// <summary>
	/// Utility class for a three dimensial array.
	/// </summary>
	public class Array3D<T>
	{
		private Vector3Int _size;
		private List<T> _list = new();

		public Vector3Int Size => _size;
		private int Count => _size.x * _size.y * _size.z;

		public T this[Vector3Int coords]
		{
			get => _list[GetIndex(coords.x, coords.y, coords.z)];
			set => _list[GetIndex(coords.x, coords.y, coords.z)] = value;
		}

		public T this[int x, int y, int z]
		{
			get => _list[GetIndex(x, y, z)];
			set => _list[GetIndex(x, y, z)] = value;
		}

		public Array3D() { }

		public Array3D(Vector3Int size) => Resize(size);

		public Array3D(int x, int y, int z) : this(new Vector3Int(x, y, z)) { }

		public void Resize(Vector3Int size)
		{
			if (_size == size)
			{
				Clear();
				return;
			}

			_size = size;
			_list.Clear();
			for (int i = 0; i < Count; i++)
			{
				_list.Add(default);
			}
		}

		public void Resize(int x, int y, int z)
		{
			Resize(new Vector3Int(x, y, z));
		}

		public void Clear()
		{
			for (int i = 0; i < Count; i++)
			{
				_list[i] = default;
			}
		}

		private int GetIndex(int x, int y, int z)
		{
			return x + (_size.x * y) + (_size.x * _size.y * z);
		}
	}
}
