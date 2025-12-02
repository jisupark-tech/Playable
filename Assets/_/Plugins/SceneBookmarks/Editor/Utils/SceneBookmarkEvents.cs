using System;
using UnityEngine.SceneManagement;

namespace SceneBookmarks
{
    public static class SceneBookmarkEvents
    {
        public delegate void BookmarkDelegate(string scenePath, CamBookmark bookmark);
        public delegate void ShortcutDelegate(string scenePath, CamBookmark bookmark, int shortcutIndex);

        public static event BookmarkDelegate onBookmarkCreated;
        public static event BookmarkDelegate onBookmarkRemoved;
        public static event ShortcutDelegate onShortcutCreated;
        public static event ShortcutDelegate onShortcutRemoved;

        internal static void TriggerBookmarkEvent(string scenePath, CamBookmark bookmark, bool wasAdded)
        {
            if (wasAdded)
            {
                onBookmarkCreated?.Invoke(scenePath, bookmark);
            }
            else
            {
                onBookmarkRemoved?.Invoke(scenePath, bookmark);
            }
        }

        internal static void TriggerShortcutEvent(string scenePath, CamBookmark bookmark, int index, bool wasAdded)
        {
            if (wasAdded)
            {
                onShortcutCreated?.Invoke(scenePath, bookmark, index);
            }
            else
            {
                onShortcutRemoved?.Invoke(scenePath, bookmark, index);
            }
        }
    }
}
