using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Serialization;
using System.Linq;

namespace SceneBookmarks
{
    public abstract class BookmarkCollection : ScriptableObject
    {
        #region Members

        [SerializeField, FormerlySerializedAs("m_SceneBookmarks")]
        private List<SceneGroup> groups = new List<SceneGroup>();
        public List<SceneGroup> Groups { get { return groups; } }

        #endregion


        #region Private / Internal

        internal virtual void DrawSceneGUI(SceneView sceneView) { }

        /// <summary>
        /// Attempt to locate a SceneGroup object for a specified scene.
        /// </summary>
        /// <param name="scene">Scene to find a group for.</param>
        /// <returns>Matching SceneGroup if found, null otherwise.</returns>
        internal SceneGroup FindSceneGroup(Scene scene)
        {
            return FindSceneGroup(scene.path);
        }

        /// <summary>
        /// Attempt to locate a SceneGroup object for a specified scene path.
        /// </summary>
        /// <param name="scenePath">Scene path to find a group for.</param>
        /// <returns>Matching SceneGroup if found, null otherwise.</returns>
        internal SceneGroup FindSceneGroup(string scenePath)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].ScenePath == scenePath || (string.IsNullOrEmpty(scenePath) && string.IsNullOrEmpty(groups[i].ScenePath)))
                {
                    return groups[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Create a new scene group for a given scene.
        /// </summary>
        /// <param name="scene">Scene to great a group for.</param>
        /// <returns>The newly craeted SceneGroup, or existing SceneGroup if already exists.</returns>
        protected SceneGroup FindOrCreateSceneGroup(Scene scene)
        {
            SceneGroup sceneGroup = FindSceneGroup(scene);
            if (sceneGroup == null)
            {
                sceneGroup = new SceneGroup(scene);
                groups.Add(sceneGroup);
            }
            return sceneGroup;
        }

        /// <summary>
        /// Create a new scene group for a given scene.
        /// </summary>
        /// <param name="scenePath">Scene to great a group for.</param>
        /// <returns>The newly craeted SceneGroup, or existing SceneGroup if already exists.</returns>
        protected SceneGroup FindOrCreateSceneGroup(string scenePath)
        {
            SceneGroup sceneGroup = FindSceneGroup(scenePath);
            if (sceneGroup == null)
            {
                sceneGroup = new SceneGroup(scenePath);
                groups.Add(sceneGroup);
            }
            return sceneGroup;
        }

        /// <summary>
        /// Locates a SceneGroup that owns a specified CamBookmark.
        /// </summary>
        /// <param name="bookmark">Bookmark to find the SceneGroup for.</param>
        /// <returns>Associated SceneGroup if match found, null otherwise.</returns>
        protected SceneGroup FindSceneGroup(CamBookmark bookmark)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Bookmarks.Contains(bookmark))
                {
                    return groups[i];
                }
            }
            return null;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Try to find a bookmark for a given scene by name.
        /// </summary>
        /// <param name="scene">Scene the bookmark relates to.</param>
        /// <param name="name">Name of the bookmark.</param>
        /// <returns>Matching bookmark if found, null otherwise.</returns>
        public CamBookmark FindBookmark(Scene? scene, string name)
        {
            if (!scene.HasValue)
                scene = new Scene();

            return FindBookmark(scene.Value.path, name);
        }

        /// <summary>
        /// Try to find a bookmark for a given scene by name.
        /// </summary>
        /// <param name="scenePath">Scene path of the scene the bookmark relates to.</param>
        /// <param name="name">Name of the bookmark.</param>
        /// <returns>Matching bookmark if found, null otherwise.</returns>
        public CamBookmark FindBookmark(string scenePath, string name)
        {
            SceneGroup sceneGroup = FindSceneGroup(scenePath);
            if (sceneGroup != null)
            {
                for (int i = 0; i < sceneGroup.Bookmarks.Count; i++)
                {
                    if (sceneGroup.Bookmarks[i].Name.Equals(name))
                    {
                        return sceneGroup.Bookmarks[i];
                    }
                }
            }
            return null;
        }



        /// <summary>
        /// Goto a specified bookmark.
        /// </summary>
        /// <param name="scenePath">ScenePath for the scene the bookmark is associated with.</param>
        /// <param name="bookmarkName">Name of the bookmark.</param>
        /// <param name="sceneView">Scene view on which to apply the bookmark.</param>
        public void GotoBookmark(string scenePath, string bookmarkName, SceneView sceneView = null)
        {
            SceneGroup sceneGroup = FindSceneGroup(scenePath);
            if (sceneGroup == null)
            {
                Debug.Log(string.Format("Unable to locate any bookmarks for scene path {0}", scenePath));
                return;
            }
            else
            {
                CamBookmark bookmark = FindBookmark(scenePath, bookmarkName);
                if (bookmark == null)
                {
                    Debug.Log(string.Format("Unable to locate bookmark {0} associated with scene path {1}", bookmarkName, scenePath));
                    return;
                }
                else
                {
                    GotoBookmark(sceneGroup.ScenePath, bookmark, sceneView);
                }
            }
        }

        /// <summary>
        /// Goto a specified bookmark.
        /// </summary>
        /// <param name="scenePath">ScenePath for the scene the bookmark is associated with.</param>
        /// <param name="bookmark">The bookmark to assume</param>
        /// <param name="sceneView">Scene view on which to apply the bookmark.</param>
        public void GotoBookmark(string scenePath, CamBookmark bookmark, SceneView sceneView = null)
        {
            SceneGroup sceneGroup = FindSceneGroup(scenePath);
            if (sceneGroup == null)
            {
                Debug.Log(string.Format("Unable to locate any bookmarks for scene path {0}", scenePath));
                return;
            }
            else
            {
                SceneViewGUI.GotoBookmark(sceneGroup.ScenePath, bookmark, sceneView, this);
            }
        }

        /// <summary>
        /// Goto the specifeid bookmark.
        /// </summary>
        /// <param name="bookmark">Bookmark to assume in the scene view.</param>
        /// <param name="sceneView">Scene View in which to assume the bookmark. If null SceneView.lastActiveSceneView will be used.</param>
        public void GotoBookmark(CamBookmark bookmark, SceneView sceneView = null)
        {
            SceneGroup sceneGroup = FindSceneGroup(bookmark);
            if (sceneGroup != null)
            {
                SceneViewGUI.GotoBookmark(sceneGroup.ScenePath, bookmark, sceneView, this);
            }
            else
            {
                Debug.LogError("Unable to locate scene group associated with bookmark.");
            }
        }




        /// <summary>
        /// Adds a new bookmark to the scene group for the provided scene, or creates a new scene group if one does not already exist.
        /// </summary>
        /// <param name="bookmark">Bookmark to add.</param>
        /// <param name="associatedScene">Scene the bookmark refers to.</param>
        public void AddBookmark(CamBookmark bookmark, Scene? associatedScene)
        {
            if (!associatedScene.HasValue)
                associatedScene = new Scene();

            SceneGroup sceneGroup = FindSceneGroup(associatedScene.Value);
            if (sceneGroup == null)
            {
                sceneGroup = FindOrCreateSceneGroup(associatedScene.Value);
            }
            sceneGroup.AddBookmark(bookmark);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Add a new bookmark and assigns it to the currently active scene.
        /// </summary>
        /// <param name="name">Name of the bookmark.</param>
        /// <param name="position">Positions of the camera.</param>
        /// <param name="eulerAngles">Euler rotation of the camera.</param>
        /// <param name="size">Size of the camera</param>
        /// <param name="orthographic">True if camera uses perspective view, false for orthographic.</param>
        /// <param name="is2D">True if bookmark should be in 2D mode, false otherwise.</param>
        /// <returns>The generated CamBookmark object.</returns>
        public CamBookmark CreateNewBookmark(string name, Vector3 position, Vector3 eulerAngles, float size, bool orthographic, bool is2D)
        {
            CamBookmark bookmark = new CamBookmark(name, position, eulerAngles, size, orthographic, is2D);
            AddBookmark(bookmark, SceneManager.GetActiveScene());
            return bookmark;
        }

        /// <summary>
        /// Add a new bookmark to the collection for the specified scene.
        /// </summary>
        /// <param name="name">Name of the bookmark.</param>
        /// <param name="position">Positions of the camera.</param>
        /// <param name="eulerAngles">Euler rotation of the camera.</param>
        /// <param name="size">Size of the camera</param>
        /// <param name="orthographic">True if camera uses perspective view, false for orthographic.</param>
        /// <param name="is2D">True if bookmark should be in 2D mode, false otherwise.</param>
        /// <param name="scene">Scene this bookmark is associated with.</param>
        /// <returns>The generated CamBookmark object.</returns>
        public CamBookmark CreateNewBookmark(string name, Vector3 position, Vector3 eulerAngles, float size, bool orthographic, bool is2D, Scene scene)
        {
            CamBookmark bookmark = new CamBookmark(name, position, eulerAngles, size, orthographic, is2D);
            AddBookmark(bookmark, scene);
            return bookmark;
        }



        /// <summary>
        /// Removes a bookmark from the system.
        /// </summary>
        /// <param name="bookmark">Bookmark to remove.</param>
        public void RemoveBookmark(CamBookmark bookmark)
        {
            SceneGroup sceneGroup = FindSceneGroup(bookmark);
            if (sceneGroup != null)
            {
                for (int i = 0; i < sceneGroup.Bookmarks.Count; i++)
                {
                    if (sceneGroup.Bookmarks[i] == bookmark)
                    {
                        sceneGroup.Bookmarks.RemoveAt(i);
                        if (sceneGroup.Bookmarks.Count == 0)
                        {
                            groups.Remove(sceneGroup);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a bookmark from the system.
        /// </summary>
        /// <param name="scenePath">Scene path of the scene associated with the bookmark.</param>
        /// <param name="bookmarkName">Name of the bookmark to delete.</param>
        public void RemoveBookmark(string scenePath, string bookmarkName)
        {
            CamBookmark bookmark = FindBookmark(scenePath, bookmarkName);
            if (bookmark != null)
            {
                RemoveBookmark(bookmark);
            }
        }

        #endregion

    }
}
