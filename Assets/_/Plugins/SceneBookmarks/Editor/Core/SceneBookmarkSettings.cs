using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SceneBookmarks
{
    internal static class SceneBookmarkSettings
    {
        #region Const / ReadOnly

        internal const string PLUGIN_DIR = "Assets/_/Plugins/SceneBookmarks";

        internal const string ASSET_DIR = "/Generated";
        internal const string BOOKMARK_ASSET_PATH_EXT = "/Bookmarks.asset";
        internal const string SHORTCUTS_ASSET_PATH_EXT = "/Shortcuts.asset";
        internal const float BUTTON_SPACING = 2.0f;
        internal const int MAX_SHORTCUTS = 8;
        internal const float PADDING = 5;
        internal const float TOOLBAR_OFFSET = 20;
        internal static readonly float[] BUTTON_SIZES = new float[] {20, 30, 40};

        private static GUIStyle shortcutStyle;

        internal static GUIStyle ShortcutStyle
        {
            get
            {
                if (shortcutStyle == null)
                {
                    shortcutStyle = new GUIStyle("button")
                    {
                        alignment = TextAnchor.MiddleCenter,
                        margin = new RectOffset(),
                        padding = new RectOffset(),
                    };
                    switch (ButtonSize)
                    {
                        case Size.Small:
                            shortcutStyle.fontSize = 10;
                            break;

                        case Size.Medium:
                            shortcutStyle.fontSize = 15;
                            break;

                        case Size.Large:
                            shortcutStyle.fontSize = 20;
                            break;
                    }
                }

                return shortcutStyle;
            }
        }

        #endregion


        #region Enums / Nested

        // NOTE: TopRight is not a position as unity puts that ortho/perspective selection gizmo up here.
        public enum Position
        {
            TopLeft,
            BottomLeft,
            BottomRight
        }

        public enum Size
        {
            Small,
            Medium,
            Large
        }

        #endregion


        #region Members

        private static string[] openModes;
        private static string[] positionsAsStrings;
        private static string[] sizesAsStrings;
        private static Color? sceneSpecificColour;
        private static Color? sceneAgnosticColour;

        #endregion


        #region Properties

        internal static string AssetDir
        {
            get { return PLUGIN_DIR + ASSET_DIR; }
        }

        internal static string BookmarksAssetPath
        {
            get { return AssetDir + BOOKMARK_ASSET_PATH_EXT; }
        }

        internal static string ShortcutsAssetPath
        {
            get { return AssetDir + SHORTCUTS_ASSET_PATH_EXT; }
        }

        internal static string[] OpenModes
        {
            get
            {
                if (openModes == null)
                {
                    openModes = System.Enum.GetNames(typeof(OpenSceneMode));
                }

                return openModes;
            }
        }

        internal static string[] Positions
        {
            get
            {
                if (positionsAsStrings == null)
                {
                    positionsAsStrings = System.Enum.GetNames(typeof(Position));
                }

                return positionsAsStrings;
            }
        }

        internal static string[] Sizes
        {
            get
            {
                if (sizesAsStrings == null)
                {
                    sizesAsStrings = System.Enum.GetNames(typeof(Size));
                }

                return sizesAsStrings;
            }
        }

        internal static bool UseTweens
        {
            get { return EditorPrefs.GetBool("SceneBookmarks_UseTweens", true); }
            set { EditorPrefs.SetBool("SceneBookmarks_UseTweens", value); }
        }

        internal static bool SceneSpecificShortcuts
        {
            get { return EditorPrefs.GetBool("SceneBookmarks_SceneSpecificShortcuts", true); }
            set { EditorPrefs.SetBool("SceneBookmarks_SceneSpecificShortcuts", value); }
        }

        internal static bool EnableShortcuts
        {
            get { return EditorPrefs.GetBool("SceneBookmarks_EnableShortcuts", true); }
            set { EditorPrefs.SetBool("SceneBookmarks_EnableShortcuts", value); }
        }
        
        internal static int ShortcutMax
        {
            get { return EditorPrefs.GetInt("SceneBookmarks_ShortcutsMax", MAX_SHORTCUTS); }
            set { EditorPrefs.SetInt("SceneBookmarks_ShortcutsMax", value); }
        }

        internal static bool ColourCodeButtons
        {
            get { return EditorPrefs.GetBool("SceneBookmarks_ColourCodeButtons", true); }
            set { EditorPrefs.SetBool("SceneBookmarks_ColourCodeButtons", value); }
        }

        /// <summary>
        /// Gets or sets the mode used to open scenes when opening scene specific bookmarks.
        /// </summary>
        public static OpenSceneMode OpenMode
        {
            get
            {
                if (EditorPrefs.HasKey("SceneBookmarks_OpenMode"))
                {
                    try
                    {
                        return (OpenSceneMode) System.Enum.Parse(typeof(OpenSceneMode), EditorPrefs.GetString("SceneBookmarks_OpenMode", Position.TopLeft.ToString()));
                    }
                    catch
                    {
                        Debug.LogError("[SceneViewBookmarks] Failed to find open scene mode, resetting to additive.");
                    }
                }

                OpenMode = OpenSceneMode.Additive;
                return OpenSceneMode.Additive;
            }
            set { EditorPrefs.SetString("SceneBookmarks_OpenMode", value.ToString()); }
        }

        /// <summary>
        /// Gets or sets the button position within the scene view.
        /// </summary>
        public static Position ButtonPos
        {
            get
            {
                if (EditorPrefs.HasKey("SceneBookmarks_Position"))
                {
                    try
                    {
                        return (Position) System.Enum.Parse(typeof(Position), EditorPrefs.GetString("SceneBookmarks_Position", Position.TopLeft.ToString()));
                    }
                    catch
                    {
                        Debug.LogError("[SceneViewBookmarks] Error reading button position, setting to top left by default.");
                    }
                }

                ButtonPos = Position.TopLeft;
                return Position.TopLeft;
            }
            set { EditorPrefs.SetString("SceneBookmarks_Position", value.ToString()); }
        }

        /// <summary>
        /// Gets or sets the size of the buttons drawn in the scene view.
        /// </summary>
        public static Size ButtonSize
        {
            get
            {
                if (EditorPrefs.HasKey("SceneBookmarks_Size"))
                {
                    try
                    {
                        return (Size) System.Enum.Parse(typeof(Size), EditorPrefs.GetString("SceneBookmarks_Size", Size.Medium.ToString()));
                    }
                    catch
                    {
                        Debug.LogError("[SceneViewBookmarks] Error reading button size, setting to medium by default.");
                    }
                }

                ButtonSize = Size.Medium;
                return Size.Medium;
            }
            set
            {
                EditorPrefs.SetString("SceneBookmarks_Size", value.ToString());
                shortcutStyle = null; // force rebuild on next request, to update font size.
            }
        }

        public static Color ShortcutColour
        {
            get
            {
                if (!ColourCodeButtons)
                    return Color.white;

                return (SceneSpecificShortcuts ? SceneSpecificColour : SceneAgnosticColour);
            }
        }

        private static Color SceneSpecificColour
        {
            get
            {
                if (!sceneSpecificColour.HasValue)
                {
                    sceneSpecificColour = LoadColour("SceneBookmarks_SceneSpecificColour", new Color(0.588f, 0.686f, 1.0f));
                }

                return sceneSpecificColour.Value;
            }
            set
            {
                if (sceneSpecificColour != value)
                {
                    sceneSpecificColour = value;
                    EditorPrefs.SetString("SceneBookmarks_SceneSpecificColour", JsonUtility.ToJson(value));
                }
            }
        }

        private static Color SceneAgnosticColour
        {
            get
            {
                if (!sceneAgnosticColour.HasValue)
                {
                    sceneAgnosticColour = LoadColour("SceneBookmarks_SceneAgnosticColour", new Color(0.431f, 1.0f, 0.431f));
                }

                return sceneAgnosticColour.Value;
            }
            set
            {
                if (sceneAgnosticColour != value)
                {
                    sceneAgnosticColour = value;
                    EditorPrefs.SetString("SceneBookmarks_SceneAgnosticColour", JsonUtility.ToJson(value));
                }
            }
        }

        internal static bool UseThumbnails
        {
            get { return EditorPrefs.GetBool("SceneBookmarks_UseThumbnails", true); }
            set { EditorPrefs.SetBool("SceneBookmarks_UseThumbnails", value); }
        }

        #endregion


        #region Utility Methods

        private static Color LoadColour(string prefKey, Color defaultColour)
        {
            if (EditorPrefs.HasKey(prefKey))
            {
                string json = EditorPrefs.GetString(prefKey);
                return JsonUtility.FromJson<Color>(json);
            }
            else
            {
                return defaultColour;
            }
        }

        /// <summary>
        /// Utility method to get the target rectangle to draw the button for a given
        /// SceneView based on current button position and size settings.
        /// </summary>
        /// <param name="sceneView">SceneView to calculcate button position for.</param>
        /// <returns>Rect value for the button location.</returns>
        internal static Rect GetButtonRect(SceneView sceneView)
        {
            float size = BUTTON_SIZES[(int) ButtonSize];
            Rect position = new Rect(0, 0, size, size);
            switch (ButtonPos)
            {
                case Position.TopLeft:
                    position.x = PADDING;
                    position.y = PADDING;
                    break;

                case Position.BottomLeft:
                    position.x = PADDING;
                    position.y = sceneView.position.height - (PADDING + size + TOOLBAR_OFFSET);
                    break;

                case Position.BottomRight:
                    position.x = sceneView.position.width - (PADDING + size);
                    position.y = sceneView.position.height - (PADDING + size + TOOLBAR_OFFSET);
                    break;
            }

            return position;
        }

        #endregion


        #region GUI Methods

        [SettingsProvider]
        private static SettingsProvider PreferencesGUI()
        {
            return new SettingsProvider("Preferences/Scene Bookmarks", SettingsScope.User)
            {
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        EditorGUILayout.BeginVertical("box");
                        {
                            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

                            UseTweens = EditorGUILayout.Toggle("Tween Transitions", UseTweens);
                            ButtonPos = (Position) EditorGUILayout.EnumPopup("Location", ButtonPos);
                            ButtonSize = (Size) EditorGUILayout.EnumPopup("Size", ButtonSize);
                            OpenMode = (OpenSceneMode) EditorGUILayout.EnumPopup("Open Scene Mode", OpenMode);
                            EditorGUILayout.HelpBox("When attempting to go to a bookmark for a scene that is not currently loaded, SceneBookmarks will open the scene for you. This setting controls how the scene is opened.", MessageType.Info);
                        }
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical("box");
                        {
                            EditorGUILayout.LabelField("Shortcut Settings", EditorStyles.boldLabel);

                            EnableShortcuts = EditorGUILayout.Toggle("Enable Shortcuts", EnableShortcuts);
                            if (EnableShortcuts)
                            {
                                EditorGUI.indentLevel++;
                                SceneSpecificShortcuts = EditorGUILayout.Toggle("Scene Specific", SceneSpecificShortcuts);
                                UseThumbnails = EditorGUILayout.Toggle("Use Thumbnails", UseThumbnails);
                                BookmarkShortcuts.Instance.ShortcutMax = Mathf.Clamp(EditorGUILayout.IntField("Shortcut Count", BookmarkShortcuts.Instance.ShortcutMax), 1, MAX_SHORTCUTS);

                                ColourCodeButtons = EditorGUILayout.Toggle("Colour Code Shortcuts", ColourCodeButtons);

                                if (ColourCodeButtons)
                                {
                                    EditorGUI.indentLevel++;
                                    SceneSpecificColour = EditorGUILayout.ColorField("Scene Specific", SceneSpecificColour);
                                    SceneAgnosticColour = EditorGUILayout.ColorField("Non Scene Specific", SceneAgnosticColour);
                                    EditorGUI.indentLevel--;
                                }

                                EditorGUI.indentLevel--;
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Reset Defaults"))
                        {
                            EditorPrefs.DeleteKey("SceneBookmarks_UseTweens");
                            EditorPrefs.DeleteKey("SceneBookmarks_SceneSpecificShortcuts");
                            EditorPrefs.DeleteKey("SceneBookmarks_EnableShortcuts");
                            EditorPrefs.DeleteKey("SceneBookmarks_ColourCodeButtons");
                            EditorPrefs.DeleteKey("SceneBookmarks_OpenMode");
                            EditorPrefs.DeleteKey("SceneBookmarks_Position");
                            EditorPrefs.DeleteKey("SceneBookmarks_Size");
                            EditorPrefs.DeleteKey("SceneBookmarks_SceneSpecificColour");
                            EditorPrefs.DeleteKey("SceneBookmarks_SceneAgnosticColour");
                            SceneSpecificColour = SceneSpecificColour;
                            SceneAgnosticColour = SceneAgnosticColour;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                    {
                        // Refresh scene view if something has changed.
                        SceneView.RepaintAll();
                    }
                }
            };
        }

        #endregion
    }
}