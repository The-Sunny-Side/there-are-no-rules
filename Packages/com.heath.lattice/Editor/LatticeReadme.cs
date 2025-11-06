using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lattice.Editor
{
	public class LatticeReadme : ScriptableObject { }

	[CustomEditor(typeof(LatticeReadme))]
	public class LatticeReadmeEditor : UnityEditor.Editor
	{
		private const string DocumentationUrl = "https://harryheath.com/lattice";
		private const string DiscordUrl = "https://discord.gg/q4F9YbtB6V";
		private const string ReviewUrl = "https://u3d.as/3mDH#reviews";
		private const string Email = "support@harryheath.com";
		private const string Version = "v1.3.1";

		private static readonly string DocumentationPath = Path.Combine("Documentation", "lattice.html");

		public override void OnInspectorGUI()
		{
			Header1($"Lattice Modifier for Unity - {Version}");
			Paragraph("Adds Lattice Modifiers to Unity, allowing you to deform both static and " + 
				"skinned objects to create otherwise advanced animations with ease.");

			Paragraph("");
			Header2("Documentation");
			Paragraph("Open the documentation using one of the following two sources:");

			using (new EditorGUILayout.HorizontalScope())
			{
				string readmePath = AssetDatabase.GetAssetPath(target);
				string packagePath = Path.GetDirectoryName(readmePath);
				string relativePath = Path.Combine(packagePath, DocumentationPath);
				string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), relativePath);

				Link(relativePath, fullPath);

				if (!File.Exists(fullPath))
				{
					Paragraph("(Missing, have you moved the README?)");
				}

				GUILayout.FlexibleSpace();
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				Link(DocumentationUrl, DocumentationUrl);
				GUILayout.FlexibleSpace();
			}

			Paragraph("");
			Header2("Support");
			Paragraph("If you have any questions or need help troubleshooting, " +
				"please feel free to email me at:");
			using (new EditorGUILayout.HorizontalScope())
			{
				Link(Email, "mailto:" + Email);
				GUILayout.FlexibleSpace();
			}

			Paragraph("");
			Header2("Community");
			Paragraph("For a place to discuss, ask questions and share your work with Lattices, " + 
				"check out the discord server:");
			using (new EditorGUILayout.HorizontalScope())
			{
				Link(DiscordUrl, DiscordUrl);
				GUILayout.FlexibleSpace();
			}

			Paragraph("");
			Header2("Thank you for your support!");
			Paragraph("If you have enjoyed using the asset, "+ 
				"please consider giving it a positive review. It would mean a lot to me, thank you:");
			using (new EditorGUILayout.HorizontalScope())
			{
				Link(ReviewUrl, ReviewUrl);
				GUILayout.FlexibleSpace();
			}
		}

		#region Utility

		private static void Header1(string text)
		{
			EditorGUILayout.Space();
			GUILayout.Label(text, Styles.Header1);
			EditorGUILayout.Space();
		}

		private static void Header2(string text)
		{
			EditorGUILayout.Space();
			GUILayout.Label(text, Styles.Header2);
			EditorGUILayout.Space();
		}

		private static void Paragraph(string text)
		{
			GUILayout.Label(text, Styles.Paragraph);
		}

		private static void Link(string text, string url)
		{
			GUIContent content = new(text);
			Rect position = GUILayoutUtility.GetRect(content, Styles.Link);

			using (new Handles.DrawingScope(Styles.Link.normal.textColor))
				Handles.DrawLine(
					new Vector3(position.xMin + Styles.Link.padding.left, position.yMax), 
					new Vector3(position.xMax - Styles.Link.padding.right, position.yMax)
				);

			EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

			if (GUI.Button(position, content, Styles.Link))
			{
				Application.OpenURL(url);
			}
		}

		private static class Styles
		{
			private static GUIStyle _header1;
			private static GUIStyle _header2;
			private static GUIStyle _paragraph;
			private static GUIStyle _link;

			public static GUIStyle Header1 => _header1 ??= new(EditorStyles.boldLabel)
			{
				fontSize = 24
			};

			public static GUIStyle Header2 => _header2 ??= new(EditorStyles.boldLabel)
			{
				fontSize = 18
			};

			public static GUIStyle Paragraph => _paragraph ??= new(EditorStyles.label)
			{
				fontSize = 14,
				wordWrap = true
			};

			public static GUIStyle Link => _link ??= new(EditorStyles.label)
			{
				fontSize = 14,
				normal = new()
				{
					textColor = EditorStyles.linkLabel.normal.textColor
				}
			};
		}

		#endregion
	}
}
