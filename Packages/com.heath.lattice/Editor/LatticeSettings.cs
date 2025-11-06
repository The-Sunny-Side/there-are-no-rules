using System;
using UnityEditor;
using UnityEngine;

namespace Lattice.Editor
{
	/// <summary>
	/// Stores user preferences for displaying lattices
	/// </summary>
	[Serializable]
	internal class LatticeSettings : ScriptableObject
	{
		internal const string SettingsKey = "Lattice/Settings";

		private static LatticeSettings _instance;

		[SerializeField] internal Color _lineColor;
		[SerializeField] internal float _lineThickness;
		[SerializeField] internal float _lineInFrontOpacity;
		[SerializeField] internal float _lineBehindOpacity;

		[SerializeField] internal Color _handleColor;
		[SerializeField] internal Color _handleColorSelected;
		[SerializeField] internal float _handleSize;

		[SerializeField] internal Gradient _glowColorGradient;
		[SerializeField] internal float _glowOpacity;
		[SerializeField] internal float _glowThicknessSquish;
		[SerializeField] internal float _glowThicknessStretch;

		[SerializeField] internal bool _selectionFalloffEnabled;
		[SerializeField] internal float _selectionFalloffRadius;
		[SerializeField] internal AnimationCurve _selectionFalloffCurve;
		[SerializeField] internal Color _selectionFalloffColor;

		public static LatticeSettings Instance
		{
			get
			{
				if (_instance == null) Load();
				return _instance;
			}
		}

		public static Color LineColor => Instance._lineColor;
		public static float LineThickness => Instance._lineThickness;
		public static float LineInFrontOpacity => Instance._lineInFrontOpacity;
		public static float LineBehindOpacity => Instance._lineBehindOpacity;

		public static Gradient GlowColorGradient => Instance._glowColorGradient;
		public static float GlowOpacity => Instance._glowOpacity;
		public static float GlowThicknessSquish => Instance._glowThicknessSquish;
		public static float GlowThicknessStretch => Instance._glowThicknessStretch;

		public static Color HandleColor => Instance._handleColor;
		public static Color HandleColorSelected => Instance._handleColorSelected;
		public static float HandleSize => Instance._handleSize;

		public static bool SelectionFalloffEnabled { get => Instance._selectionFalloffEnabled; set => Instance._selectionFalloffEnabled = value; }
		public static float SelectionFalloffRadius { get => Instance._selectionFalloffRadius; set => Instance._selectionFalloffRadius = Mathf.Clamp(value, 0.33f, 10f); }
		public static AnimationCurve SelectionFalloffCurve => Instance._selectionFalloffCurve;
		public static Color SelectionFalloffColor => Instance._selectionFalloffColor;

		public static void Save()
		{
			string serializedSettings = EditorJsonUtility.ToJson(Instance, false);
			EditorPrefs.SetString(SettingsKey, serializedSettings);
		}

		private static void Load()
		{
			if (_instance == null)
			{
				_instance = CreateInstance<LatticeSettings>();
			}

			_instance.name = "Lattice Settings";
			_instance.Reset();

			string serializedSettings = EditorPrefs.GetString(SettingsKey, string.Empty);
			if (!string.IsNullOrEmpty(serializedSettings))
			{
				EditorJsonUtility.FromJsonOverwrite(serializedSettings, _instance);
			}

			_instance.hideFlags = HideFlags.DontSave;
		}

		public static void OpenPreferences()
		{
			SettingsService.OpenUserPreferences(LatticeSettingsProvider.PreferencesPath);
		}

		public void Reset()
		{
			_lineColor = Color.black;
			_lineThickness = 3f;
			_lineInFrontOpacity = 2f;
			_lineBehindOpacity = 0.5f;

			_glowColorGradient = new Gradient()
			{
				colorKeys = new GradientColorKey[]
				{
					new(new Color32(  0,  69, 255, 255), 0.00f),
					new(new Color32( 69, 174, 212, 255), 0.25f),
					new(new Color32(  0,   0,   0, 255), 0.50f),
					new(new Color32(233, 157,  76, 255), 0.75f),
					new(new Color32(255,   0,   0, 255), 1.00f),
				},
				alphaKeys = new GradientAlphaKey[]
				{
					new(1.00f, 0.00f),
					new(1.00f, 0.25f),
					new(0.25f, 0.50f),
					new(1.00f, 0.75f),
					new(1.00f, 1.00f)
				}
			};
			_glowOpacity = 0.5f;
			_glowThicknessSquish = 6f;
			_glowThicknessStretch = 3f;

			_handleColor = new Color(0f, 0f, 0f, 1f);
			_handleColorSelected = new Color(1f, 0.6f, 0f, 1f);
			_handleSize = 3.5f;

			_selectionFalloffEnabled = false;
			_selectionFalloffRadius = 1.0f;
			_selectionFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
			_selectionFalloffColor = new Color(1f, 1f, 1f, 0.5f);
		}
	}
}
