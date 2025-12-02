using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;

namespace SceneBookmarks
{
    internal static class IconUtils
    {
        private static string IconsRoot
        {
            get { return SceneBookmarkSettings.PLUGIN_DIR + "/Editor/Icons/"; }
        }

        private static Texture2D camBookmarkIconWhite;
        internal static Texture2D CamBookmarkIconWhite
        {
            get
            {
                if (camBookmarkIconWhite == null)
                {
                    camBookmarkIconWhite = AssetDatabase.LoadAssetAtPath<Texture2D>(IconsRoot + "Cam_Bookmark_White_Icon.png");
                }
                return camBookmarkIconWhite ?? EditorGUIUtility.whiteTexture; // Hail mary use of whiteTexture to avoid null refs spamming the console if the icons are accidentally deleted / moved.
            }
        }

        private static Texture2D camBookmarkIconBlack;
        internal static Texture2D CamBookmarkIconBlack
        {
            get
            {
                if (camBookmarkIconBlack == null)
                {
                    camBookmarkIconBlack = AssetDatabase.LoadAssetAtPath<Texture2D>(IconsRoot + "Cam_Bookmark_Black_Icon.png");
                }
                return camBookmarkIconBlack ?? EditorGUIUtility.whiteTexture; // Hail mary use of whiteTexture to avoid null refs spamming the console if the icons are accidentally deleted / moved.
            }
        }
    }
}