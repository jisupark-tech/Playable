using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SceneBookmarks
{
    public static class ThumbnailDrawer
    {
        private const int THUMBNAIL_DISPLAY_WIDTH = 160;
        private const int THUMBNAIL_DISPLAY_HEIGHT = 90;

        public static void DrawThumbnail(Rect buttonRect, Texture2D thumbnail)
        {
            Rect thumbRect = GetThumbnailRect(buttonRect);
            Rect insetRect = thumbRect;
            insetRect.size -= Vector2.one * 10;
            insetRect.position += Vector2.one * 5;

            GUI.Button(thumbRect, "");
            GUI.color = Color.white;
            GUI.DrawTexture(insetRect, thumbnail);
        }

        private static Rect GetThumbnailRect(Rect buttonRect)
        {
            // Define our rect, and then perform our initial offset based on position settings.
            // Initial position will be our "perfect" position, and we'll adjust to avoid clipping after.
            Rect rect = new Rect(0, 0, THUMBNAIL_DISPLAY_WIDTH, THUMBNAIL_DISPLAY_HEIGHT);
            if (SceneBookmarkSettings.ButtonPos == SceneBookmarkSettings.Position.TopLeft)
            {
                rect.y = buttonRect.yMax;
            }
            else
            {
                rect.y = buttonRect.yMin - rect.height;
            }

            float xLimit = SceneView.lastActiveSceneView.position.width - rect.width;
            rect.x = Mathf.Min(xLimit, Mathf.Max(0, buttonRect.center.x - (rect.width * 0.5f)));

            return rect;
        }
    }
}