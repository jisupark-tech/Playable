using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneBookmarks
{
    internal static class ContextMenuUtils
    {
        /// <summary>
        /// Creates the context menu for the scene bookmarks tool.
        /// </summary>
        /// <param name="sceneView">SceneView for which to create a menu.</param>
        /// <returns>Context menu to show.</returns>
        internal static GenericMenu CreateMenu(SceneView sceneView)
        {
            GenericMenu contextMenu = new GenericMenu();

            Settings(contextMenu, sceneView);

            contextMenu.AddSeparator("");

            Create(contextMenu, sceneView);
            GotoBookmarks(contextMenu, sceneView);
            RemoveBookmarks(contextMenu, sceneView);
            Edit(contextMenu, sceneView);

            return contextMenu;
        }

        /// <summary>
        /// Adds the create options to the generic menu for a given scene view.
        /// </summary>
        private static void Create(GenericMenu menu, SceneView sceneView)
        {
            // Add from current view.
            menu.AddItem(new GUIContent("Create Bookmark/From Current View"), false, () =>
            {
                InputDialog.FlagMode mode = string.IsNullOrEmpty(SceneManager.GetActiveScene().name) ? InputDialog.FlagMode.Disabled : InputDialog.FlagMode.Enabled;
                InputDialog popup = InputDialog.Display("Enter Bookmark Name", "", "Scene Specific", false, mode, (string name, bool sceneSpecific, bool cancelled) =>
                {
                    if (!cancelled)
                    {
                        if (string.IsNullOrEmpty(name))
                        {
                            Debug.LogError("No name provided, bookmark will not be created.");
                        }
                        else
                        {
                            CamBookmark existing = SceneViewBookmarks.Instance.FindBookmark(SceneManager.GetActiveScene(), name);
                            if (existing == null)
                            {
                                Scene scene = sceneSpecific ? SceneManager.GetActiveScene() : new Scene();
                                CamBookmark newBookmark = CamBookmark.CreateFromSceneView(name, SceneView.lastActiveSceneView);
                                SceneViewBookmarks.Instance.AddBookmark(newBookmark, scene);
                                SceneBookmarkEvents.TriggerBookmarkEvent(scene.path, newBookmark, true);
                            }
                            else
                            {
                                if (EditorUtility.DisplayDialog("Bookmark already exists!", "A bookmark already exists for this scene by the name '" + name + "', Replace existing bookmark?", "Replace", "Cancel"))
                                {
                                    Scene scene = sceneSpecific ? SceneManager.GetActiveScene() : new Scene();
                                    SceneViewBookmarks.Instance.RemoveBookmark(existing);
                                    CamBookmark newBookmark = CamBookmark.CreateFromSceneView(name, SceneView.lastActiveSceneView);
                                    SceneViewBookmarks.Instance.AddBookmark(newBookmark, scene);
                                    SceneBookmarkEvents.TriggerBookmarkEvent(scene.path, newBookmark, true);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    return true; // Close regardsless in this case.
                });
                // Center the popup on the scene view in question (for convenience).
                Rect pos = popup.position;
                pos.x = sceneView.position.center.x - (pos.width * 0.5f);
                pos.y = sceneView.position.center.y - (pos.height * 0.5f);
                popup.position = pos;
            });
        }

        /// <summary>
        /// Adds the goto navigation options to the generic menu for a given scene view.
        /// </summary>
        private static void GotoBookmarks(GenericMenu menu, SceneView sceneView)
        {
            // Goto existing bookmarks.
            if (SceneViewBookmarks.Instance != null)
            {
                int count = 0;
                if (SceneViewBookmarks.Instance.Groups != null)
                {
                    List<SceneGroup> scenelessGroups = SceneViewBookmarks.Instance.Groups
                                                                        .Where(x => string.IsNullOrEmpty(x.ScenePath))
                                                                        .ToList();
                    if (scenelessGroups.Count > 0)
                    {
                        count += PopulateGotoList(menu, sceneView, scenelessGroups.SelectMany(x => x.Bookmarks).ToList(), null);
                        if (SceneViewBookmarks.Instance.Groups.Count > scenelessGroups.Count)
                        {
                            menu.AddSeparator("Goto Bookmark/");
                        }
                    }
                    for (int i = 0; i < SceneViewBookmarks.Instance.Groups.Count; i++)
                    {
                        SceneGroup group = SceneViewBookmarks.Instance.Groups[i];
                        if (!scenelessGroups.Contains(group))
                        {
                            count += PopulateGotoList(menu, sceneView, group.Bookmarks, group.ScenePath);
                        }
                    }
                }
                if (count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("Goto Bookmark/No Valid Bookmarks"));
                }
            }
        }

        private static int PopulateGotoList(GenericMenu menu, SceneView sceneView, List<CamBookmark> bookmarks, string scenePath)
        {
            int count = 0;
            string groupName = string.IsNullOrEmpty(scenePath) ? "No-Scene" : scenePath.Replace('/', '\u2215').Replace(".unity", "");
            for (int j = 0; j < bookmarks.Count; j++)
            {
                if (bookmarks[j] == null || string.IsNullOrEmpty(bookmarks[j].Name))
                {
                    continue;
                }

                count++;
                menu.AddItem(new GUIContent(string.Format("Goto Bookmark/{0}/{1}", groupName,
                                bookmarks[j].Name)),
                                false,
                                (object bookmark) =>
                                {
                                    SceneViewGUI.GotoBookmark(scenePath, bookmark as CamBookmark, sceneView, SceneViewBookmarks.Instance);
                                },
                                bookmarks[j]);
            }
            return count;
        }

        /// <summary>
        /// Adds the removal options to the generic menu for a given scene view.
        /// </summary>
        private static void RemoveBookmarks(GenericMenu menu, SceneView sceneView)
        {
            if (SceneViewBookmarks.Instance != null)
            {
                int count = 0;
                if (SceneViewBookmarks.Instance.Groups != null)
                {
                    List<SceneGroup> scenelessGroups = SceneViewBookmarks.Instance.Groups
                                                                            .Where(x => string.IsNullOrEmpty(x.ScenePath))
                                                                            .ToList();
                    if (scenelessGroups.Count > 0)
                    {
                        count += PopulateRemoveList(menu, sceneView, scenelessGroups.SelectMany(x => x.Bookmarks).ToList(), null);
                        if (SceneViewBookmarks.Instance.Groups.Count > scenelessGroups.Count)
                        {
                            menu.AddSeparator("Remove Bookmark/");
                        }
                    }
                    for (int i = 0; i < SceneViewBookmarks.Instance.Groups.Count; i++)
                    {
                        SceneGroup group = SceneViewBookmarks.Instance.Groups[i];
                        if (!scenelessGroups.Contains(group))
                        {
                            count += PopulateRemoveList(menu, sceneView, group.Bookmarks, group.ScenePath);
                        }
                    }
                }
                if (count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("Remove Bookmark/No Valid Bookmarks"));
                }
            }
        }

        private static int PopulateRemoveList(GenericMenu menu, SceneView sceneView, List<CamBookmark> bookmarks, string scenePath)
        {
            int count = 0;
            string groupName = string.IsNullOrEmpty(scenePath) ? "No-Scene" : scenePath.Replace('/', '\u2215');
            for (int j = 0; j < bookmarks.Count; j++)
            {
                if (bookmarks[j] == null || string.IsNullOrEmpty(bookmarks[j].Name))
                {
                    continue;
                }

                count++;
                menu.AddItem(new GUIContent(string.Format("Remove Bookmark/{0}/{1}", groupName,
                                bookmarks[j].Name)),
                                false,
                                (object bookmark) =>
                                {
                                    CamBookmark cast = bookmark as CamBookmark;
                                    if (EditorUtility.DisplayDialog(string.Format("Delete {0}", cast.Name),
                                                                        string.Format("Are you sure you want to delete {0} from Scene {1}?", cast.Name, scenePath),
                                                                        "Delete", "Cancel"))
                                    {
                                        Undo.RegisterCompleteObjectUndo(SceneViewBookmarks.Instance, "Delete Bookmark");
                                        SceneViewBookmarks.Instance.RemoveBookmark(cast);
                                        SceneBookmarkEvents.TriggerBookmarkEvent(scenePath, cast, false);
                                    }
                                },
                                bookmarks[j]);
            }
            return count;
        }


        /// <summary>
        /// Adds the settings to the generic menu for a given scene view.
        /// </summary>
        private static void Settings(GenericMenu menu, SceneView sceneView)
        {
            // Tween toggle control.
            menu.AddItem(new GUIContent("Settings/Use Tweens"),
                            SceneBookmarkSettings.UseTweens, () =>
                            {
                                SceneBookmarkSettings.UseTweens = !SceneBookmarkSettings.UseTweens;
                            });

            // Submenu for the button position selection.
            for (int i = 0; i < SceneBookmarkSettings.Positions.Length; i++)
            {
                menu.AddItem(new GUIContent(string.Format("Settings/Position/{0}", SceneBookmarkSettings.Positions[i])),
                                SceneBookmarkSettings.Positions[i].Equals(SceneBookmarkSettings.ButtonPos.ToString()),
                                (object position) =>
                                {
                                    SceneBookmarkSettings.ButtonPos = (SceneBookmarkSettings.Position)System.Enum.Parse(typeof(SceneBookmarkSettings.Position), position as string);
                                },
                                SceneBookmarkSettings.Positions[i]);
            }

            // Submenu for the button size selection.
            for (int i = 0; i < SceneBookmarkSettings.Sizes.Length; i++)
            {
                menu.AddItem(new GUIContent(string.Format("Settings/Size/{0}", SceneBookmarkSettings.Sizes[i])),
                                                SceneBookmarkSettings.Sizes[i].Equals(SceneBookmarkSettings.ButtonSize.ToString()),
                                                (object size) =>
                                                {
                                                    SceneBookmarkSettings.ButtonSize = (SceneBookmarkSettings.Size)System.Enum.Parse(typeof(SceneBookmarkSettings.Size), size as string);
                                                },
                                                SceneBookmarkSettings.Sizes[i]);
            }

            // Submenu for the open mode selection.
            for (int i = 0; i < SceneBookmarkSettings.OpenModes.Length; i++)
            {
                menu.AddItem(new GUIContent(string.Format("Settings/Open Scene Mode/{0}", SceneBookmarkSettings.OpenModes[i])),
                                                SceneBookmarkSettings.OpenModes[i].Equals(SceneBookmarkSettings.OpenMode.ToString()),
                                                (object mode) =>
                                                {
                                                    SceneBookmarkSettings.OpenMode = (OpenSceneMode)Enum.Parse(typeof(OpenSceneMode), mode as string);
                                                },
                                                SceneBookmarkSettings.OpenModes[i]);
            }

            // Submenu for the shortcuts

            menu.AddItem(new GUIContent("Settings/Shortcuts/Scene Specific"),
                         SceneBookmarkSettings.SceneSpecificShortcuts,
                         () =>
                         {
                             SceneBookmarkSettings.SceneSpecificShortcuts = !SceneBookmarkSettings.SceneSpecificShortcuts;
                         });
            menu.AddItem(new GUIContent("Settings/Shortcuts/Colour Coded"),
                         SceneBookmarkSettings.ColourCodeButtons,
                         () =>
                         {
                             SceneBookmarkSettings.ColourCodeButtons = !SceneBookmarkSettings.ColourCodeButtons;
                         });
            menu.AddItem(new GUIContent("Settings/Shortcuts/Use Thumbnails"),
                         SceneBookmarkSettings.UseThumbnails,
                         () =>
                         {
                             SceneBookmarkSettings.UseThumbnails = !SceneBookmarkSettings.UseThumbnails;
                         });

            menu.AddSeparator("Settings/Shortcuts/");
            if (SceneBookmarkSettings.EnableShortcuts)
            {
                menu.AddItem(new GUIContent("Settings/Shortcuts/Disable"), false, () => { SceneBookmarkSettings.EnableShortcuts = false; });
            }
            else
            {
                menu.AddItem(new GUIContent("Settings/Shortcuts/Enable"), false, () => { SceneBookmarkSettings.EnableShortcuts = true; });
            }

            for (int i = 0; i < SceneBookmarkSettings.MAX_SHORTCUTS; i++)
            {
                int count = i + 1;
                menu.AddItem(new GUIContent("Settings/Shortcuts/" + count),
                                count == BookmarkShortcuts.Instance.ShortcutMax,
                                () =>
                                {
                                    BookmarkShortcuts.Instance.ShortcutMax = count;
                                    if (!SceneBookmarkSettings.EnableShortcuts)
                                    {
                                        SceneBookmarkSettings.EnableShortcuts = true;
                                    }
                                });
            }
        }

        /// <summary>
        /// Adds the edit asset link to the generic menu for a given scene view.
        /// </summary>
        private static void Edit(GenericMenu menu, SceneView sceneView)
        {
            menu.AddItem(new GUIContent("Edit Bookmarks"), false, () =>
            {
                Selection.activeObject = SceneViewBookmarks.Instance;
                EditorGUIUtility.PingObject(Selection.activeObject);
            });
            menu.AddItem(new GUIContent("Edit Shortcuts"), false, () =>
            {
                Selection.activeObject = BookmarkShortcuts.Instance;
                EditorGUIUtility.PingObject(Selection.activeObject);
            });
        }
    }
}
