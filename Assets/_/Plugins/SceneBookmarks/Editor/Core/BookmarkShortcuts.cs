using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneBookmarks
{
    public class BookmarkShortcuts : BookmarkCollection
    {
        #region Singleton

        /// <summary>
        /// Self Constructing Instance access for the BookmarkCollection asset.
        /// If asset already exists it is loaded, if not a new asset is generated.
        /// </summary>
        internal static BookmarkShortcuts Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.LoadAssetAtPath<BookmarkShortcuts>(SceneBookmarkSettings.ShortcutsAssetPath);
                    if (instance == null)
                    {
                        if (!System.IO.Directory.Exists(SceneBookmarkSettings.AssetDir))
                        {
                            System.IO.Directory.CreateDirectory(SceneBookmarkSettings.AssetDir);
                        }
                        instance = CreateInstance(typeof(BookmarkShortcuts)) as BookmarkShortcuts;
                        AssetDatabase.CreateAsset(instance, SceneBookmarkSettings.ShortcutsAssetPath);
                        AssetDatabase.SaveAssets();

                        // Ensure shortcuts setup correctly. This will trigger appropriate length checks and null filling.
                        instance.ShortcutMax = SceneBookmarkSettings.ShortcutMax;
                    }
                }
                return instance;
            }
        }
        private static BookmarkShortcuts instance;

        #endregion


        private Scene SceneToAssign
        {
            get
            {
                return SceneBookmarkSettings.SceneSpecificShortcuts ? SceneManager.GetActiveScene() : new Scene();
            }
        }

        public int ShortcutMax
        {
            get
            {
                return SceneBookmarkSettings.ShortcutMax;
            }
            set
            {
                if (SceneBookmarkSettings.ShortcutMax != value)
                {
                    SceneBookmarkSettings.ShortcutMax = value;
                    SanitizeBookmarks();

                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
            }
        }


        internal override void DrawSceneGUI(SceneView sceneView)
        {
            base.DrawSceneGUI(sceneView);

            SanitizeBookmarks();

            // Get rectof the main button, and then based on position settings, offset for our first shortcut rect.
            Rect rect = SceneBookmarkSettings.GetButtonRect(sceneView);
            if (SceneBookmarkSettings.ButtonPos == SceneBookmarkSettings.Position.BottomRight)
            {
                rect.x -= (rect.width + SceneBookmarkSettings.BUTTON_SPACING) * ShortcutMax;
            }
            else
            {
                rect.x += (rect.width + SceneBookmarkSettings.BUTTON_SPACING);
            }


            Color prevCol = GUI.color;

            // Get the appropriate scene group for schortcuts.
            SceneGroup group = FindOrCreateSceneGroup(SceneToAssign);
            List<CamBookmark> shortcuts = group.Bookmarks;

            // Start handling the shortcut buttons for this group.
            for (int i = 0; i < shortcuts.Count; i++)
            {
                if (ShortcutButton(rect, (i + 1).ToString(), shortcuts[i]))
                {
                    if (Event.current.control)
                    {
                        if (Event.current.button == 1)
                        {
                            SetShortcut(SceneToAssign, null, i);
                        }
                        else if (Event.current.button == 0)
                        {
                            CamBookmark newBookmark = CamBookmark.CreateFromSceneView("shortcut_" + (i + 1), SceneView.lastActiveSceneView);
                            SetShortcut(SceneToAssign, newBookmark, i);
                        }
                    }
                    else if (shortcuts[i] != null && shortcuts[i].IsSet)
                    {
                        SceneViewGUI.GotoBookmark(group.ScenePath, shortcuts[i], sceneView, this);
                    }
                }
                if (SceneBookmarkSettings.UseThumbnails && SceneBookmarkSettings.SceneSpecificShortcuts)
                {
                    TryDrawThumbnail(rect, shortcuts[i]);
                }

                rect.x += rect.width + SceneBookmarkSettings.BUTTON_SPACING;
            }
            GUI.color = prevCol;
        }


        /// <summary>
        /// Draws the thumbnail for a given shortcut if appropriate.
        /// </summary>
        /// <param name="rect">Screen rect of the shortcuts button.</param>
        /// <param name="bookmark">Bookmark associated with this button position.</param>
        private void TryDrawThumbnail(Rect rect, CamBookmark bookmark)
        {
            if (bookmark != null && bookmark.Thumbnail != null)
            {
                Event evt = Event.current;
                if (rect.Contains(evt.mousePosition))
                {
                    ThumbnailDrawer.DrawThumbnail(rect, bookmark.Thumbnail);
                }
            }
        }

        /// <summary>
        /// Renders a button for a shortcut, and reports if it was click or not.
        /// </summary>
        /// <param name="rect">Rect for the button</param>
        /// <param name="label">Label for the button.</param>
        /// <param name="shortcut">Shortcut associated with the button.</param>
        /// <returns>true, if button was clicked, false otherwise.</returns>
        private static bool ShortcutButton(Rect rect, string label, CamBookmark shortcut)
        {
            Color baseColour = SceneBookmarkSettings.ShortcutColour;
            baseColour.a = (shortcut == null || !shortcut.IsSet ? 0.35f : 1.0f);
            GUI.color = baseColour;
            return (GUI.Button(rect, label, SceneBookmarkSettings.ShortcutStyle));
        }

        /// <summary>
        /// Sets the value of a shortcut.
        /// </summary>
        /// <param name="scene">Scene to associate with the shortcut.</param>
        /// <param name="bookmark">Bookmark the shortcut represents.</param>
        /// <param name="index">Index in the shortcuts list to store this shortcut.</param>
        private void SetShortcut(Scene scene, CamBookmark bookmark, int index)
        {
            if (index >= 0 && index < ShortcutMax)
            {
                SceneGroup group = FindOrCreateSceneGroup(scene);
                if (group.Bookmarks.Count == 0)
                {
                    for (int i = 0; i < ShortcutMax; i++)
                        group.Bookmarks.Add(null);
                }
                if (bookmark != null)
                {
                    SceneBookmarkEvents.TriggerShortcutEvent(scene.path, bookmark, index, true);
                    ThumbnailManager.CreateThumbnail(SceneView.lastActiveSceneView.camera, scene.path, $"Shortcut_{index}");
                    bookmark.Thumbnail = ThumbnailManager.TryGetThumbnail(scene.path, $"Shortcut_{index}");
                    SceneView.lastActiveSceneView.Repaint();
                }
                else if (group.Bookmarks[index] != null)
                {
                    SceneBookmarkEvents.TriggerShortcutEvent(scene.path, group.Bookmarks[index], index, false);
                    ThumbnailManager.DeleteThumbnail(scene.path, $"Shortcut_{index}");
                }
                group.Bookmarks[index] = bookmark;


                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Gets a shortcut.
        /// </summary>
        /// <param name="scenePath">Scene path associated with shortcut.</param>
        /// <param name="index">Index of shortcut.</param>
        /// <returns>The shortcut if found, null otherwise.</returns>
        internal CamBookmark GetShortcut(string scenePath, int index)
        {
            if (index >= 0 && index < ShortcutMax)
            {
                SceneGroup group = FindOrCreateSceneGroup(scenePath);
                if (group.Bookmarks.Count == 0)
                {
                    for (int i = 0; i < ShortcutMax; i++)
                        group.Bookmarks.Add(null);
                }
                return group.Bookmarks[index];
            }
            return null;
        }

        /// <summary>
        /// Ensures that all bookmark collections are approrpiately sized to avoid out of range errors.
        /// </summary>
        internal void SanitizeBookmarks()
        {
            foreach (SceneGroup group in Groups)
            {
                // If necessary remove.
                while (group.Bookmarks.Count > ShortcutMax)
                {
                    group.Bookmarks.RemoveAt(group.Bookmarks.Count - 1);
                }
                // Or pad with nulls
                while (group.Bookmarks.Count < ShortcutMax)
                {
                    group.Bookmarks.Add(null);
                }
            }
        }
    }
}