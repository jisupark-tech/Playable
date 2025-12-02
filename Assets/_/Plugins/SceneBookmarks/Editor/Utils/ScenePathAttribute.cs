using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SceneBookmarks
{
    /// <summary>
    /// Purely marking attribute so that I don't have to redo the an entire editor for the SceneBookmarks asset.
    /// </summary>
    public class ScenePathAttribute : PropertyAttribute
    {
        public ScenePathAttribute()
        {
        }
    }

    [CustomPropertyDrawer(typeof(ScenePathAttribute))]
    public class ScenePathAttributeDrawer : PropertyDrawer
    {
        private static readonly float BUTTON_WIDTH = EditorGUIUtility.singleLineHeight * 2;

        private ScenePathAttribute Attribute
        {
            get
            {
                return (ScenePathAttribute)attribute;
            }
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Draw the prefix label.
            position = EditorGUI.PrefixLabel(position, label);

            // Leave some space for a button and draw the input field.
            position.width -= BUTTON_WIDTH;

            /*property.stringValue =*/
            EditorGUI.LabelField(position, property.stringValue, EditorStyles.textField);

            // Shift the postion to that space we left and draw the button.
            position.x += position.width;
            position.width = BUTTON_WIDTH;

            // Draw the find button and if pressed, let used select scene.
            if (GUI.Button(position, "Find", EditorStyles.miniButtonRight))
            {
                string scenePath = EditorUtility.OpenFilePanel("Select Scene", Application.dataPath, "unity");
                if (!string.IsNullOrEmpty(scenePath))
                {
                    string trimmedPath = scenePath.Substring(Application.dataPath.Length - 6); // strip data path, except for Assets which we want to keep.
                    if (!scenePath.StartsWith(Application.dataPath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.DisplayDialog("Error", "Unable to pair bookmarks with scenes that re not part of this project.", "Okay");
                    }
                    else if (SceneViewBookmarks.Instance.FindSceneGroup(trimmedPath) != null)
                    {
                        EditorUtility.DisplayDialog("Error", "Scene Already has a bookmark group assigned to it.", "Okay");
                    }
                    else
                    {
                        property.stringValue = trimmedPath;
                    }
                }
            }
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}