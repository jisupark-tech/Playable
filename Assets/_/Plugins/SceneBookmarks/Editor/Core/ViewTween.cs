using UnityEngine;
using UnityEditor;

namespace SceneBookmarks
{
    /// <summary>
    /// Utility class for tweening from current camera within a scene view to a given bookmark.
    /// This allows the tool to provide a smooth tween to bookmarks instead of a snap (if tweens enabled).
    /// </summary>
    internal class ViewTween
    {
        private const double TWEEN_DURATION = 0.15;

        private Vector3 initPosition;
        private Quaternion initRotation;
        private float initSize;
        private CamBookmark target;
        private double startTime;
        private SceneView view;

        public bool Complete { get; private set; }

        /// <summary>
        /// Create a new tween object using a given sceneview and a target bookmark.
        /// </summary>
        /// <param name="targetBookmark">Bookmark you wish to assume.</param>
        /// <param name="sceneView">Sceneview that you wish to apply theis bookmark to.</param>
        public ViewTween(CamBookmark targetBookmark, SceneView sceneView)
        {
            if (sceneView == null || targetBookmark == null)
                return;

            Complete = false;

            view = sceneView;
            target = targetBookmark;

            initPosition = sceneView.pivot;
            initRotation = sceneView.rotation;
            initSize = sceneView.size;

            view.in2DMode = target.Is2D;
            view.orthographic = target.Orthographic;

            startTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Updates the tween, lerping between the previous camera state, and the desired bookmark.
        /// </summary>
        public void Update()
        {
            if (!Complete) // Just in case...for sanity.
            {
                if (view != null)
                {
                    float progress = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - startTime) / TWEEN_DURATION));
                    view.pivot = Vector3.Lerp(initPosition, target.Position, progress);
                    view.size = Mathf.Lerp(initSize, target.Size, progress);
                    if (!view.in2DMode)
                    {
                        view.rotation = Quaternion.Lerp(initRotation, Quaternion.Euler(target.Rotation), progress);
                    }
                    if (progress >= 1)
                    {
                        Complete = true;
                    }
                }
            }
        }
    }
}