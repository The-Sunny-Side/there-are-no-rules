using System;
using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Data structure for per Lattice mask information.
	/// </summary>
	[Serializable]
	public struct LatticeMask : ISerializationCallbackReceiver
	{
		private const string MaterialTooltip =
			"Settings to control which vertices will be deformed.";

		private const string VertexTooltip =
			"Settings to control how much each individual vertex will be deformed.";

		[Tooltip(MaterialTooltip)]
		public SelectionSettings Selection;

		[Tooltip(VertexTooltip)]
		public VertexSettings Vertex;

		/// <summary>
		/// Settings to control which vertices will be deformed.
		/// </summary>
		[Serializable]
		public struct SelectionSettings
		{
			/// <summary>
			/// Currently supported mask types.
			/// </summary>
			public enum MaskType : int
			{
				ApplyToAll = 0,
				Material = 1,
			}

			private const string TypeTooltip = 
				"Type of selection masking to use:\n" +
				" - Apply To All:  Apply deformation to all vertices.\n" +
				" - Material:  Only apply deformation to vertices used by a material.";

			private const string IndexTooltip = 
				"Material index to use as a mask. Lattice deformation will only be " +
				"applied to portions of the mesh using this material.";

			[Tooltip(TypeTooltip)]
			public MaskType Type;

			[Tooltip(IndexTooltip)]
			[ShowIf(nameof(Type), MaskType.Material)]
			public int Index;
		}

		/// <summary>
		/// Settings to control how much each individual vertex will be deformed.
		/// </summary>
		[Serializable]
		public struct VertexSettings
		{
			/// <summary>
			/// Currently supported mask types.
			/// </summary>
			public enum MaskType : int
			{
				None = 0,
				Constant = 1,
				Color = 2,
				UV = 3,
				Texture = 4,
			}

			/// <summary>
			/// Available color channels.
			/// </summary>
			public enum ColorChannel : int
			{
				Red = 0,
				Green = 1,
				Blue = 2,
				Alpha = 3,
			}

			private const string TypeTooltip = 
				"Type of vertex masking to use:\n" +
				" - None:  No masking will be used.\n" +
				" - Constant:  Uses a constant multiplier.\n" +
				" - Color:  Uses the vertex color.\n" +
				" - UV:  Uses the vertex's texture coordinates.\n" +
				" - Texture:  Uses a texture sample.";

			private const string UVTooltip =
				"Varies by mask type:\n" +
				" - UV:  The texture coordinates to use as a mask.\n" +
				" - Texture:  The UVs that will be used to sample the texture.";

			private const string TextureTooltip = "Texture to use as mask.";
			private const string ChannelTooltip = "RGBA channel to use.";
			private const string MultiplierTooltip = "Amount to multiple mask by.";

			[Tooltip(TypeTooltip)]
			public MaskType Type;

			[Tooltip(UVTooltip)]
			[ShowIf(nameof(Type), MaskType.UV, MaskType.Texture)]
			public TextureCoordinate UV;

			[Tooltip(TextureTooltip)]
			[ShowIf(nameof(Type), MaskType.Texture)]
			public Texture Texture;

			[Tooltip(ChannelTooltip)]
			[ShowIf(nameof(Type), MaskType.Color, MaskType.UV, MaskType.Texture)]
			public ColorChannel Channel;

			[Tooltip(MultiplierTooltip)]
			[ShowIf(nameof(Type))]
			public float Multiplier;
		}

		#region Serialization

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			// Clear texture reference if unused,
			// prevents unnecessary reference to texture in the scene/prefab asset
			if (Vertex.Type != VertexSettings.MaskType.Texture)
			{
				Vertex.Texture = null;
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() { }

		#endregion
	}
}