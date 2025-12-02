using UnityEngine;
using UnityEditor;

namespace SceneBookmarks
{
    public class InputDialog : EditorWindow
    {
        #region Constants / Delegates

        public enum FlagMode { Enabled, Disabled, Hidden }

        private const int BUTTON_WIDTH = 100;
        private const int WINDOW_WIDTH = 300;
        private const int WINDOW_HEIGHT = 70;
        private const int WINDOW_HEIGHT_NO_FLAG = 50;
        private const string TEXT_CONTROL = "InputDialog_TextField";
        private const string CHECK_CONTROL = "InputDialog_Checkbox";

        public delegate bool InputCallback(string value, bool flag, bool wasCancelled);

        #endregion


        #region Variables

        private string input;
        private bool flag;
        private bool needFocus;
        private InputCallback callback;
        private string flagLabel;
        private FlagMode flagMode;

        #endregion


        #region Display / Drawing Methods

        /// <summary>
        /// Display a simple input dialog with a specified title.
        /// </summary>
        /// <param name="title">Title for the window.</param>
        /// <param name="callback">Callback for the dialog to use.</param>
        /// <returns>Reference to the input dialog window created.</returns>
        public static InputDialog Display(string title, InputCallback callback)
        {
            return Display(title, "", "", false, FlagMode.Hidden, callback);
        }

        /// <summary>
        /// Display a simple input dialog with a specified title and a default value in the input field.
        /// </summary>
        /// <param name="title">Title for the window.</param>
        /// <param name="defaultValue">Default value for the input field.</param>
        /// <param name="callback">Callback for the dialog to use.</param>
        /// <returns>Reference to the input dialog window created.</returns>
        public static InputDialog Display(string title, string defaultValue, string flagLabel, bool defaultFlag, FlagMode flagMode, InputCallback callback)
        {
            if (callback != null)
            {
                InputDialog window = ScriptableObject.CreateInstance<InputDialog>();

                window.titleContent = new GUIContent(title);
                window.input = defaultValue;
                window.callback = callback;
                window.flag = defaultFlag;
                window.minSize = window.maxSize = new Vector2(WINDOW_WIDTH, flagMode == FlagMode.Hidden ? WINDOW_HEIGHT_NO_FLAG : WINDOW_HEIGHT);
                window.ShowUtility();
                window.needFocus = true;
                window.flagMode = flagMode;
                window.flagLabel = flagLabel;
                return window;
            }
            else
            {
                Debug.LogError("InputCallback value cannot be null, must specify a valid callback.");
                return null;
            }
        }

        private void OnGUI()
        {
            // Input field.
            GUILayout.FlexibleSpace();

            GUI.SetNextControlName(TEXT_CONTROL);
            input = GUILayout.TextField(input);

            if (flagMode != FlagMode.Hidden)
            {
                GUI.enabled = (flagMode == FlagMode.Enabled);
                GUI.SetNextControlName(CHECK_CONTROL);
                flag = GUILayout.Toggle(flag, flagLabel);
                GUI.enabled = true;
            }

            // Buttons
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(BUTTON_WIDTH)))
                {
                    Button_Cancel();
                }
                if (GUILayout.Button("Confirm", GUILayout.Width(BUTTON_WIDTH)))
                {
                    Button_Confirm();
                }
            }
            if (Event.current.isKey &&
                 Event.current.keyCode == KeyCode.Return &&
                 (GUI.GetNameOfFocusedControl() == TEXT_CONTROL || GUI.GetNameOfFocusedControl() == CHECK_CONTROL))
            {
                if (callback(input, flag, false))
                {
                    Close();
                }
            }


            EditorGUILayout.EndHorizontal();


            // Automatically set focus onto the input field when the window first displays for conveneince.
            if (needFocus)
            {
                needFocus = false;
                GUI.FocusControl(TEXT_CONTROL);
            }
        }

        #endregion


        #region Dialog Callbacks

        private void Button_Confirm()
        {
            if (callback(input, flag, false))
            {
                Close();
            }
        }

        private void Button_Cancel()
        {
            callback(input, flag, true);
            Close();
        }

        #endregion
    }

}