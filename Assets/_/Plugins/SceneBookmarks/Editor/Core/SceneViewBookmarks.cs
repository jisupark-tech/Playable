using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Serialization;
using System.Linq;

namespace SceneBookmarks
{
    public class SceneViewBookmarks : BookmarkCollection
    {

        #region Singleton

        /// <summary>
        /// Self Constructing Instance access for the BookmarkCollection asset.
        /// If asset already exists it is loaded, if not a new asset is generated.
        /// </summary>
        internal static SceneViewBookmarks Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.LoadAssetAtPath<SceneViewBookmarks>(SceneBookmarkSettings.BookmarksAssetPath);
                    if (instance == null)
                    {
                        if (!System.IO.Directory.Exists(SceneBookmarkSettings.AssetDir))
                        {
                            System.IO.Directory.CreateDirectory(SceneBookmarkSettings.AssetDir);
                        }
                        instance = CreateInstance(typeof(SceneViewBookmarks)) as SceneViewBookmarks;
                        AssetDatabase.CreateAsset(instance, SceneBookmarkSettings.BookmarksAssetPath);
                        AssetDatabase.SaveAssets();
                    }
                }
                return instance;
            }
        }
        private static SceneViewBookmarks instance;

        #endregion


        internal override void DrawSceneGUI(SceneView sceneView)
        {
            base.DrawSceneGUI(sceneView);

            Texture2D icon = EditorGUIUtility.isProSkin ? IconUtils.CamBookmarkIconWhite : IconUtils.CamBookmarkIconBlack;
            Rect rect = SceneBookmarkSettings.GetButtonRect(sceneView);
            if (GUI.Button(rect, icon, EditorStyles.helpBox))
            {
                try
                {
                    // Now create the menu, add items and show it
                    GenericMenu menu = ContextMenuUtils.CreateMenu(sceneView);
                    menu.ShowAsContext();
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

        }
    }
}
