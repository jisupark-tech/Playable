using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace SceneBookmarks
{

    public static class SceneviewBookmarksToolbar
    {
        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 1")]
        public static void ShortcutBookmark1() { GotoShortcut(0); }

        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 2")]
        public static void ShortcutBookmark2() { GotoShortcut(1); }

        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 3")]
        public static void ShortcutBookmark3() { GotoShortcut(2); }

        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 4")]
        public static void ShortcutBookmark4() { GotoShortcut(3); }

        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 5")]
        public static void ShortcutBookmark5() { GotoShortcut(4); }

        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 6")]
        public static void ShortcutBookmark6() { GotoShortcut(5); }

        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 7")]
        public static void ShortcutBookmark7() { GotoShortcut(6); }

        [MenuItem("Tools/Scene Bookmarks/Shortcuts/Goto 8")]
        public static void ShortcutBookmark8() { GotoShortcut(7); }



        private static void GotoShortcut(int index)
        {
            if (SceneBookmarkSettings.SceneSpecificShortcuts)
            {
                Scene scene = SceneManager.GetActiveScene();
                CamBookmark bookmark = BookmarkShortcuts.Instance.GetShortcut(scene.path, index);
                if (bookmark != null && bookmark.IsSet)
                {
                    SceneViewGUI.GotoBookmark(scene.path, bookmark, SceneView.lastActiveSceneView, BookmarkShortcuts.Instance);
                }
            }
            else
            {
                CamBookmark bookmark = BookmarkShortcuts.Instance.GetShortcut("", index);
                if (bookmark != null && bookmark.IsSet)
                {
                    SceneViewGUI.GotoBookmark("", bookmark, SceneView.lastActiveSceneView, BookmarkShortcuts.Instance);
                }
            }
        }
    }

}
