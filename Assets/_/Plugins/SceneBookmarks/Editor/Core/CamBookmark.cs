using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace SceneBookmarks
{

    /// <summary>
    /// Class to store the actuall bookmark data themselves.
    /// Also fascilitates immediate setting of a given scene view to the bookmark.
    /// </summary>
    [System.Serializable]
    public class CamBookmark
    {
        [SerializeField, FormerlySerializedAs("m_Name")]
        private string name;
        [SerializeField, FormerlySerializedAs("m_Position")]
        private Vector3 position;
        [SerializeField, FormerlySerializedAs("m_Rotation")]
        private Vector3 rotation;
        [SerializeField, FormerlySerializedAs("m_Orthographic")]
        private bool isOrthographic;
        [SerializeField, FormerlySerializedAs("m_2dMode")]
        private bool is2D;
        [SerializeField, FormerlySerializedAs("m_Size")]
        private float size;
        [SerializeField]
        private Texture2D thumbnail;

        public string Name { get { return name; } }
        public Vector3 Position { get { return position; } }
        public Vector3 Rotation { get { return rotation; } }
        public bool Orthographic { get { return isOrthographic; } }
        public bool Is2D { get { return is2D; } }
        public float Size { get { return size; } }
        public bool IsSet { get { return !string.IsNullOrEmpty(Name); } }
        public Texture2D Thumbnail
        {
            get { return thumbnail; }
            internal set { thumbnail = value; }
        }

        /// <summary>
        /// Creates a new instance of a CamBookmark object.
        /// </summary>
        /// <param name="name">Name of bookmark.</param>
        /// <param name="position">World space position of camera.</param>
        /// <param name="rotation">Rotation of camera in Euler angles.</param>
        /// <param name="size">Size of camera.</param>
        /// <param name="isOrthographic">Bookmark is orthogrpahic.</param>
        /// <param name="is2D">Bookmark uses 2D mode.</param>
        internal CamBookmark(string name, Vector3 position, Vector3 rotation, float size, bool isOrthographic, bool is2D)
        {
            this.name = name;
            this.position = position;
            this.rotation = rotation;
            this.is2D = is2D;
            this.isOrthographic = isOrthographic;
            this.size = size;
        }

        /// <summary>
        /// Sets the camera for a given scene view to the value of the bookmark.
        /// </summary>
        /// <param name="sceneView">Scene view to assume bookmark in.</param>
        public void AssumeBookmark(SceneView sceneView)
        {
            sceneView.pivot = position;
            sceneView.in2DMode = is2D;
            if (!sceneView.in2DMode)
            {
                sceneView.rotation = Quaternion.Euler(rotation);
            }
            sceneView.orthographic = isOrthographic;
            sceneView.size = size;
        }

        /// <summary>
        /// Create a new bookmark object with a given name from the current camera view of a given scene view.
        /// </summary>
        /// <param name="name">Name for the bookmark.</param>
        /// <param name="sceneView">Sceneview from which to generate the bookmark.</param>
        /// <returns>The new bookmark object.</returns>
        internal static CamBookmark CreateFromSceneView(string name, SceneView sceneView)
        {
            return new CamBookmark(name, sceneView.pivot, sceneView.rotation.eulerAngles, sceneView.size, sceneView.orthographic, sceneView.in2DMode);
        }
    }
}