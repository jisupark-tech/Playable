using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace SceneBookmarks
{

    /// <summary>
    /// SceneGroup class collects a set of CamBookmarks together to associate them
    /// with a specific scene in the project based on the path to that scene file.
    /// </summary>
    [System.Serializable]
    public class SceneGroup
    {
        [SerializeField, FormerlySerializedAs("m_ScenePath"), ScenePath]
        private string scenePath;
        [SerializeField, FormerlySerializedAs("m_Bookmarks")]
        private List<CamBookmark> bookmarks;

        /// <summary>
        /// Path for the scene associated with this bookmark group.
        /// </summary>
        public string ScenePath
        {
            get { return scenePath; }
        }

        /// <summary>
        /// Bookmarks contained within this group.
        /// </summary>
        public List<CamBookmark> Bookmarks
        {
            get { return bookmarks; }
        }

        /// <summary>
        /// Construct a new SceneGroup for a specified scene.
        /// </summary>
        /// <param name="scene">Scene to associate with the group.</param>
        public SceneGroup(Scene scene)
        {
            scenePath = scene.path;
            bookmarks = new List<CamBookmark>();
        }

        /// <summary>
        /// Construct a new SceneGroup for a specified scene.
        /// </summary>
        /// <param name="scenePath">Scene to associate with the group.</param>
        public SceneGroup(string scenePath)
        {
            this.scenePath = scenePath;
            bookmarks = new List<CamBookmark>();
        }

        /// <summary>
        /// Adds a bookmark to the group.
        /// </summary>
        /// <param name="bookmark">Bookmark to add.</param>
        public void AddBookmark(CamBookmark bookmark)
        {
            if (bookmarks == null)
            {
                bookmarks = new List<CamBookmark>();
            }
            bookmarks.Add(bookmark);
        }
    }
}