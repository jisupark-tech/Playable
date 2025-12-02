using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SceneBookmarks
{
    public static class SceneViewGUI
    {
        private static List<ViewTween> activeTweens = new List<ViewTween>();

        /// <summary>
        /// Initialization method used to register for the OnScene and Update events from the editor.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnScene;
#else
			SceneView.onSceneGUIDelegate += OnScene;
#endif
            EditorApplication.update += Update;
        }

        /// <summary>
        /// Updates the in-progress tween if there is one.
        /// </summary>
        private static void Update()
        {
            // Process any active tweens.
            if (activeTweens != null)
            {
                for (int i = 0; i < activeTweens.Count; i++)
                {
                    activeTweens[i].Update();
                    if (activeTweens[i].Complete)
                    {
                        activeTweens.RemoveAt(i);
                        i--;
                    }
                }
            }

            /*
            SceneView repaints don't occur by mouse movement alone, because of this the showing 
            of thumbnails can feel very laggy and unresponsive. So we'll actually use the update
            to check if the mouse is over a scene view and if it is, we repaint it to make sure
            that the thumbnail display feels snappy and responsive.
            */
            if ( EditorWindow.mouseOverWindow != null && EditorWindow.mouseOverWindow is SceneView)
            {
                EditorWindow.mouseOverWindow.Repaint();
            }
        }

        /// <summary>
        /// Draws the menu in the scene view.
        /// </summary>
        /// <param name="sceneView">SceneView being drawn.</param>
        private static void OnScene(SceneView sceneView)
        {
            Handles.BeginGUI();
            {
                // Main Menu.
                SceneViewBookmarks.Instance.DrawSceneGUI(sceneView);

                // Shortcuts
                if (SceneBookmarkSettings.EnableShortcuts)
                {
                    BookmarkShortcuts.Instance.DrawSceneGUI(sceneView);
                }
                Handles.EndGUI();
            }
        }

        /// <summary>
        /// Triggers the specified scene view to assume a given bookmark position.
        /// </summary>
        /// <param name="bookmark">Bookmark to assume.</param>
        /// <param name="sceneView">SceneView in which to assume the bookmark.</param>
        internal static void GotoBookmark(string scenePath, CamBookmark bookmark, SceneView sceneView, Object fromCollection)
        {
            if (string.IsNullOrEmpty(scenePath) || PrepareSceneForBookmark(scenePath, fromCollection))
            {
                // Proceed to the bookmark.
                if (SceneBookmarkSettings.UseTweens)
                {
                    activeTweens.Add(new ViewTween(bookmark as CamBookmark, sceneView));
                }
                else
                {
                    (bookmark as CamBookmark).AssumeBookmark(sceneView);
                }
            }
        }

        /// <summary>
        /// Attempts to load the specified scene.
        /// </summary>
        /// <param name="scenePath">Path of scene to load.</param>
        /// <returns>True if scene was was loaded, false if scene not found or loaded.</returns>
        internal static bool PrepareSceneForBookmark(string scenePath, Object fromCollection)
        {
            // First thing's first, validate that the scene exists.
            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Missing Scene",
                                             string.Format("The scene associated with this bookmark {0} cannot be found. Please update the scene path value of the bookmark group from the bookmarks asset.", scenePath),
                                             "Ok");

                Selection.activeObject = fromCollection;
                EditorGUIUtility.PingObject(Selection.activeObject);

                return false;
            }

            // Ensure that our target scene is loaded.
            bool isSceneLoaded = false;
            Scene targetScene = SceneManager.GetSceneByPath(scenePath);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene tempScene = SceneManager.GetSceneAt(i);
                if (tempScene == targetScene && tempScene.isLoaded)
                {
                    isSceneLoaded = true;
                    break;
                }
            }

            if (!isSceneLoaded)
            {
                // New scene is being loaded, If loading via Single mode we need to ensure the user is warned about any unsaved changes.
                if (SceneBookmarkSettings.OpenMode != OpenSceneMode.Single ||
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    targetScene = EditorSceneManager.OpenScene(scenePath, SceneBookmarkSettings.OpenMode);
                }
                else
                {
                    // user cancelled the action, BAIL OUT!
                    return false;
                }
            }

            // Set target scene as the active one.
            SceneManager.SetActiveScene(targetScene);

            return true;
        }

    }

}