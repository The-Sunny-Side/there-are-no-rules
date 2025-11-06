using UnityEditor;
using UnityEngine;

namespace Lattice.Editor
{
	/// <summary>
	/// Creates a tab within the user preferences window to edit Lattice settings
	/// </summary>
	internal class LatticeSettingsProvider : SettingsProvider
	{
		internal const string PreferencesPath = "Preferences/Lattice";
		private static readonly string[] Keywords = new string[] { "Lattice" };

		private static SerializedObject _serializedSettings;

		private static SerializedObject SerializedSettings => _serializedSettings ??= new(LatticeSettings.Instance);

		public LatticeSettingsProvider() : base(PreferencesPath, SettingsScope.User, Keywords) { }

		[SettingsProvider]
		internal static SettingsProvider CreateProvider()
		{
			return new LatticeSettingsProvider();
		}

		public override void OnGUI(string searchContext)
		{
			SerializedSettings.Update();

			// Add spacing on the left
			using EditorGUILayout.HorizontalScope horizontal = new();
			GUILayout.Space(10f);
			using EditorGUILayout.VerticalScope vertical = new();

			// Line settings
			EditorGUILayout.Space(10f);
			EditorGUILayout.LabelField("Lines", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._lineColor)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._lineThickness)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._lineInFrontOpacity)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._lineBehindOpacity)));

			// Glow settings
			EditorGUILayout.Space(10f);
			EditorGUILayout.LabelField("Glow", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._glowColorGradient)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._glowOpacity)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._glowThicknessSquish)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._glowThicknessStretch)));

			// Handle settings
			EditorGUILayout.Space(10f);
			EditorGUILayout.LabelField("Handles", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._handleColor)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._handleColorSelected)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._handleSize)));

			// Falloff settings
			EditorGUILayout.Space(10f);
			EditorGUILayout.LabelField("Falloff", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._selectionFalloffEnabled)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._selectionFalloffRadius)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._selectionFalloffCurve)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings._selectionFalloffColor)));

			SerializedSettings.ApplyModifiedProperties();

			// Reset button
			EditorGUILayout.Space(10f);
			if (GUILayout.Button("Reset to Defaults"))
			{
				Undo.RecordObject(LatticeSettings.Instance, "Reset Lattice Settings");
				LatticeSettings.Instance.Reset();
			}
		}

		public override void OnDeactivate()
		{
			LatticeSettings.Save();
		}
	}
}
