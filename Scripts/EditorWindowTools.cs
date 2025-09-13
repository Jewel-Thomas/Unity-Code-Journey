// Unity Design Pattern Example: EditorWindowTools
// This script demonstrates the EditorWindowTools pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# script provides a complete and practical implementation of the 'EditorWindowTools' design pattern for Unity. This pattern focuses on centralizing reusable GUI elements, serialization logic, and common asset management tasks into a dedicated static utility class. This approach helps keep your custom `EditorWindow` classes cleaner, more consistent, and easier to maintain.

## EditorWindowTools Design Pattern Explained:

**Problem:** When developing multiple custom `EditorWindow`s or complex ones, you often find yourself repeating GUI code (headers, lines, buttons), serialization logic for window preferences, or standard asset operations (creating, finding, deleting ScriptableObjects). This leads to code duplication, inconsistency in UI, and a more difficult maintenance process.

**Solution (EditorWindowTools Pattern):**
Encapsulate these common functionalities within one or more static utility classes (e.g., `EditorWindowTools`). This utility class provides:

1.  **GUI Helpers:** Static methods to draw common UI patterns (e.g., styled headers, collapsible sections, horizontal lines, custom object fields, save/load buttons).
2.  **Serialization Helpers:** Generic methods to save and load editor-specific preferences using `EditorPrefs` (often serialized as JSON).
3.  **Asset Management Helpers:** Methods to simplify common `AssetDatabase` operations (e.g., creating ScriptableObjects, finding assets).

**Benefits:**
*   **Reduced Duplication:** Write GUI and logic code once and reuse it across many `EditorWindow`s.
*   **Consistency:** All windows using the tools will have a consistent look and feel (e.g., same header style, button layout).
*   **Improved Readability:** `EditorWindow` code becomes cleaner, as repetitive GUI drawing logic is abstracted away.
*   **Easier Maintenance:** Changes to a UI element or serialization logic only need to be made in one place.
*   **Testability (Partial):** While GUI is hard to test, the serialization and asset management helpers can be more easily tested in isolation.

**How to Use:**
1.  **Create the `EditorWindowTools` static class:** Define all your helper methods here.
2.  **Create a `ScriptableObject` (optional but common):** This will be the data your `EditorWindow` manages.
3.  **Implement your `EditorWindow`:** In `OnGUI`, call the static methods from `EditorWindowTools` instead of writing the raw `EditorGUILayout` and `GUILayout` calls directly.

---

### Instructions for Use:

1.  Create a new C# script in your Unity project (e.g., named `MyCustomEditorWindow.cs`).
2.  Copy and paste the entire code below into this script.
3.  Ensure the script is placed in an `Editor` folder (or a subfolder of an `Editor` folder, like `Assets/Editor/MyTools`). The `#if UNITY_EDITOR` directive ensures it only compiles in the editor.
4.  Open the editor window in Unity: `Window > EditorWindowTools Demo > Game Setting Editor`.

---

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like OrderBy, FirstOrDefault

// This ensures this code only compiles in the Unity Editor.
// Editor-specific classes like EditorWindow and most UnityEditor namespace types
// are not available in player builds.
#if UNITY_EDITOR

// Define a namespace for better organization of editor-related tools and windows.
namespace EditorWindowToolsPattern
{
    // =================================================================================
    // PART 1: Data Model (ScriptableObject)
    // This is a simple ScriptableObject that our EditorWindow will manage.
    // It serves as a practical example of data that an editor tool might interact with.
    // =================================================================================
    [CreateAssetMenu(fileName = "NewGameSetting", menuName = "EditorWindowTools Demo/Game Setting")]
    public class GameSettingSO : ScriptableObject
    {
        public string settingName = "New Setting";
        public int valueInt = 0;
        public float valueFloat = 0.0f;
        public bool valueBool = false;
        public Color settingColor = Color.white;
        public Texture2D icon;

        [TextArea]
        public string description = "A general game setting.";

        // A unique ID is crucial for robust editor window preferences, especially
        // when referencing assets that might be renamed or moved. We can save this ID
        // in EditorPrefs instead of an asset path or name.
        [HideInInspector] public string uniqueID; 

        /// <summary>
        /// Ensures this ScriptableObject has a unique ID.
        /// Useful when creating new assets or loading old ones that might lack an ID.
        /// </summary>
        public void EnsureUniqueID()
        {
            if (string.IsNullOrEmpty(uniqueID))
            {
                uniqueID = Guid.NewGuid().ToString();
                // Mark the object dirty so Unity saves the new unique ID to the asset file.
                EditorUtility.SetDirty(this); 
            }
        }
    }


    // =================================================================================
    // PART 2: EditorWindowTools Design Pattern Implementation
    // This static class encapsulates reusable GUI rendering logic and common editor operations.
    // It acts as a utility belt for EditorWindows, promoting clean, modular, and consistent
    // UI across multiple custom editor windows.
    // =================================================================================
    public static class EditorWindowTools
    {
        #region GUI Styles
        // Pre-defined GUI styles for consistent look and feel across all windows
        // that use these tools. Styles are lazily initialized for efficiency.

        public static GUIStyle HeaderStyle => _headerStyle ?? (_headerStyle = new GUIStyle(EditorStyles.boldLabel) {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(0, 0, 10, 5)
        });
        private static GUIStyle _headerStyle;

        public static GUIStyle SubHeaderStyle => _subHeaderStyle ?? (_subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(5, 5, 2, 2),
            margin = new RectOffset(0, 0, 5, 2)
        });
        private static GUIStyle _subHeaderStyle;

        public static GUIStyle SectionHeaderStyle => _sectionHeaderStyle ?? (_sectionHeaderStyle = new GUIStyle(EditorStyles.foldout) {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            padding = new RectOffset(17, 0, 0, 0) // Adjust padding to align foldout arrow
        });
        private static GUIStyle _sectionHeaderStyle;

        public static GUIStyle ButtonStyle => _buttonStyle ?? (_buttonStyle = new GUIStyle(GUI.skin.button) {
            fontStyle = FontStyle.Bold
        });
        private static GUIStyle _buttonStyle;

        public static GUIStyle BoxStyle => _boxStyle ?? (_boxStyle = new GUIStyle(GUI.skin.box) {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        });
        private static GUIStyle _boxStyle;

        #endregion

        #region Core GUI Elements

        /// <summary>
        /// Draws a prominent header for an EditorWindow section.
        /// Ensures consistent heading style throughout editor tools.
        /// </summary>
        /// <param name="title">The text to display as the header.</param>
        /// <param name="tooltip">Optional tooltip for the header.</param>
        public static void DrawHeader(string title, string tooltip = "")
        {
            EditorGUILayout.Space(10);
            GUIContent headerContent = new GUIContent(title, tooltip);
            EditorGUILayout.LabelField(headerContent, HeaderStyle);
            DrawHorizontalLine(); // Add a line for visual separation
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draws a collapsible section header with a foldout toggle.
        /// The `ref` keyword allows the method to directly modify the `toggleState` variable
        /// from the calling `EditorWindow`, simplifying state management.
        /// </summary>
        /// <param name="title">The title of the section.</param>
        /// <param name="toggleState">A ref to the boolean that controls the foldout state.</param>
        /// <param name="tooltip">Optional tooltip for the section header.</param>
        /// <returns>The new toggle state (also modified via ref).</returns>
        public static bool DrawSectionToggle(string title, ref bool toggleState, string tooltip = "")
        {
            EditorGUILayout.Space(5);
            GUIContent content = new GUIContent(title, tooltip);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10); // Indent the foldout slightly for better hierarchy
            toggleState = EditorGUILayout.Foldout(toggleState, content, true, SectionHeaderStyle);
            EditorGUILayout.EndHorizontal();
            return toggleState;
        }

        /// <summary>
        /// Draws a simple horizontal line, useful for visual separation of sections.
        /// </summary>
        /// <param name="height">The height of the line.</param>
        /// <param name="color">The color of the line. Defaults to a subtle gray based on Unity's skin.</param>
        public static void DrawHorizontalLine(float height = 1, Color? color = null)
        {
            GUILayout.Space(5);
            // Default color adapts to Unity's light or dark theme
            Color lineColor = color ?? (EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f, 1) : new Color(0.6f, 0.6f, 0.6f, 1));
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, lineColor);
            GUILayout.Space(5);
        }

        /// <summary>
        /// Draws a styled help box with a message and a message type (Info, Warning, Error).
        /// Standardizes how messages are displayed.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="type">The type of the message, influencing the icon and color.</param>
        public static void DrawHelpBox(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, type);
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draws an object field for a specific type of Unity.Object.
        /// Provides a generic way to select assets or scene objects.
        /// </summary>
        /// <typeparam name="T">The type of Unity.Object to accept (e.g., GameObject, ScriptableObject, Texture2D).</typeparam>
        /// <param name="label">The label for the field.</param>
        /// <param name="currentAsset">The currently assigned asset.</param>
        /// <param name="tooltip">Optional tooltip for the field.</param>
        /// <param name="allowSceneObjects">True if scene objects are allowed, false for assets only.</param>
        /// <returns>The selected asset.</returns>
        public static T DrawAssetField<T>(string label, T currentAsset, string tooltip = "", bool allowSceneObjects = false) where T : UnityEngine.Object
        {
            GUIContent content = new GUIContent(label, tooltip);
            T newAsset = EditorGUILayout.ObjectField(content, currentAsset, typeof(T), allowSceneObjects) as T;
            return newAsset;
        }

        /// <summary>
        /// Draws a pair of buttons for saving and loading.
        /// Simplifies UI for persisting data within the editor.
        /// </summary>
        /// <param name="saveLabel">Label for the save button.</param>
        /// <param name="loadLabel">Label for the load button.</param>
        /// <param name="onSave">Action to invoke when save is pressed.</param>
        /// <param name="onLoad">Action to invoke when load is pressed.</param>
        public static void DrawSaveLoadButtons(string saveLabel, string loadLabel, Action onSave, Action onLoad)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(saveLabel, ButtonStyle, GUILayout.Height(30)))
            {
                onSave?.Invoke();
            }
            if (GUILayout.Button(loadLabel, ButtonStyle, GUILayout.Height(30)))
            {
                onLoad?.Invoke();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws a button that opens a folder selection dialog and updates a path string.
        /// Ensures folder paths are project-relative for portability.
        /// </summary>
        /// <param name="label">The label for the button.</param>
        /// <param name="currentPath">A ref to the string holding the current folder path.</param>
        /// <param name="defaultPath">The initial path for the dialog.</param>
        /// <param name="title">The title of the folder selection dialog.</param>
        /// <returns>True if a new folder was selected, false otherwise.</returns>
        public static bool DrawFolderPicker(string label, ref string currentPath, string defaultPath, string title = "Select Folder")
        {
            bool changed = false;
            EditorGUILayout.BeginHorizontal();
            // Display current path in a read-only text field
            EditorGUILayout.TextField(label, currentPath); 
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel(title, defaultPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert absolute path to a project-relative path (e.g., "Assets/MyFolder")
                    selectedPath = FileUtil.GetProjectRelativePath(selectedPath);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        currentPath = selectedPath;
                        changed = true;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            return changed;
        }

        #endregion

        #region EditorPrefs / Serialization Helpers

        /// <summary>
        /// Serializes an object to JSON and saves it to EditorPrefs.
        /// Useful for persisting editor window settings or preferences between Unity sessions.
        /// Objects must be marked [Serializable] and have public fields for JsonUtility.
        /// </summary>
        /// <typeparam name="T">The type of the object to save.</typeparam>
        /// <param name="key">The key to store the data under.</param>
        /// <param name="data">The object to save.</param>
        public static void SaveEditorPrefs<T>(string key, T data)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                EditorPrefs.SetString(key, json);
                Debug.Log($"[EditorWindowTools] Saved EditorPrefs '{key}'.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EditorWindowTools] Failed to save EditorPrefs '{key}': {e.Message}");
            }
        }

        /// <summary>
        /// Loads and deserializes an object from EditorPrefs.
        /// </summary>
        /// <typeparam name="T">The type of the object to load.</typeparam>
        /// <param name="key">The key the data is stored under.</param>
        /// <param name="defaultValue">The default value to return if the key doesn't exist or deserialization fails.</param>
        /// <returns>The deserialized object or the default value.</returns>
        public static T LoadEditorPrefs<T>(string key, T defaultValue)
        {
            if (EditorPrefs.HasKey(key))
            {
                try
                {
                    string json = EditorPrefs.GetString(key);
                    T loadedData = JsonUtility.FromJson<T>(json);
                    // JsonUtility can return null for certain types if the JSON is empty or invalid
                    if (loadedData == null)
                    {
                        Debug.LogWarning($"[EditorWindowTools] EditorPrefs '{key}' contained null after deserialization. Returning default value.");
                        return defaultValue;
                    }
                    Debug.Log($"[EditorWindowTools] Loaded EditorPrefs '{key}'.");
                    return loadedData;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EditorWindowTools] Failed to load EditorPrefs '{key}': {e.Message}. Returning default value.");
                    return defaultValue;
                }
            }
            Debug.Log($"[EditorWindowTools] EditorPrefs '{key}' not found. Returning default value.");
            return defaultValue;
        }

        /// <summary>
        /// Removes a specific key from EditorPrefs.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        public static void ClearEditorPrefs(string key)
        {
            if (EditorPrefs.HasKey(key))
            {
                EditorPrefs.DeleteKey(key);
                Debug.Log($"[EditorWindowTools] Cleared EditorPrefs '{key}'.");
            }
            else
            {
                Debug.Log($"[EditorWindowTools] EditorPrefs '{key}' not found, nothing to clear.");
            }
        }

        #endregion

        #region Asset Management Helpers

        /// <summary>
        /// Creates a new ScriptableObject asset of type T at the specified path.
        /// Handles unique naming and asset refreshing.
        /// </summary>
        /// <typeparam name="T">The type of ScriptableObject to create.</typeparam>
        /// <param name="folderPath">The project-relative folder path (e.g., "Assets/MyFolder").</param>
        /// <param name="assetName">The desired name for the new asset file (without extension).</param>
        /// <returns>The newly created asset, or null if creation failed.</returns>
        public static T CreateAsset<T>(string folderPath, string assetName) where T : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError($"[EditorWindowTools] Invalid folder path: {folderPath}. Cannot create asset. Please ensure the folder exists in your project.");
                return null;
            }

            string fullPath = Path.Combine(folderPath, $"{assetName}.asset");
            // Ensure the asset name is unique in the specified path
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            T asset = ScriptableObject.CreateInstance<T>();
            
            // If the asset is a GameSettingSO, ensure it has a unique ID immediately.
            if (asset is GameSettingSO gameSetting)
            {
                gameSetting.EnsureUniqueID(); 
            }

            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();      // Save all pending asset changes
            AssetDatabase.Refresh();         // Refresh the asset database to show the new asset in Project window
            EditorUtility.FocusProjectWindow(); // Focus the project window
            Selection.activeObject = asset;  // Select the newly created asset
            Debug.Log($"[EditorWindowTools] Created new asset: {fullPath}");
            return asset;
        }

        #endregion
    }

    // =================================================================================
    // PART 3: EditorWindow Implementation
    // This is the actual EditorWindow that consumes the EditorWindowTools.
    // It demonstrates how to use the helper methods for a cleaner and more structured UI,
    // thereby showcasing the benefits of the EditorWindowTools pattern.
    // =================================================================================

    /// <summary>
    /// A serializable class to hold the EditorWindow's preferences.
    /// This allows the window to remember its state between Unity sessions.
    /// Needs [Serializable] for JsonUtility.
    /// </summary>
    [Serializable] 
    public class MyWindowPrefs
    {
        public string lastSelectedAssetID = ""; // Stores the unique ID of the last selected asset
        public string assetCreationPath = "Assets/GameSettings"; // Default path for new assets
        public bool showAssetManagement = true; // Foldout state for asset management section
        public bool showSelectedAsset = true;   // Foldout state for selected asset details section
        public bool showWindowPrefs = true;     // Foldout state for window preferences section
        public Vector2 scrollPosition = Vector2.zero; // Stores the scroll position of the window
    }

    /// <summary>
    /// A custom EditorWindow to manage GameSettingSO assets, demonstrating the EditorWindowTools pattern.
    /// </summary>
    public class MyCustomEditorWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Game Setting Editor";
        private const string EDITOR_PREFS_KEY = "MyCustomEditorWindowPrefs"; // Unique key for EditorPrefs

        private GameSettingSO _selectedSetting;
        private List<GameSettingSO> _allSettings = new List<GameSettingSO>();
        private SerializedObject _serializedObject; // Used for efficient property editing with Undo/Redo support
        private MyWindowPrefs _prefs; // Instance of our window preferences

        /// <summary>
        /// Menu item to open the custom editor window.
        /// </summary>
        [MenuItem("Window/EditorWindowTools Demo/Game Setting Editor")]
        public static void ShowWindow()
        {
            MyCustomEditorWindow window = GetWindow<MyCustomEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(400, 300);
            window.LoadWindowPrefs(); // Load preferences immediately when the window opens
        }

        /// <summary>
        /// Called when the window is enabled or gains focus.
        /// </summary>
        private void OnEnable()
        {
            // Subscribe to asset database changes to automatically refresh our list
            // if assets are added, deleted, or renamed outside of this window.
            AssetDatabase.globalObjectContextMenu -= OnGlobalObjectContextMenu; // Prevent double subscription
            AssetDatabase.globalObjectContextMenu += OnGlobalObjectContextMenu;

            LoadWindowPrefs(); // Ensure preferences are loaded
            RefreshSettingList(); // Populate the list of GameSettingSO assets

            // After loading prefs and refreshing the list, try to re-select the last chosen asset.
            if (_selectedSetting == null && !string.IsNullOrEmpty(_prefs.lastSelectedAssetID))
            {
                _selectedSetting = _allSettings.FirstOrDefault(s => s.uniqueID == _prefs.lastSelectedAssetID);
                if (_selectedSetting != null)
                {
                    _serializedObject = new SerializedObject(_selectedSetting);
                } else {
                    // If the asset no longer exists, clear the preference.
                    _prefs.lastSelectedAssetID = "";
                    SaveWindowPrefs();
                }
            }
        }

        /// <summary>
        /// Called when the window is disabled or closed.
        /// </summary>
        private void OnDisable()
        {
            AssetDatabase.globalObjectContextMenu -= OnGlobalObjectContextMenu; // Unsubscribe
            SaveWindowPrefs(); // Save current preferences before closing
        }

        /// <summary>
        /// Called when the selection in the Unity editor changes.
        /// Allows the window to react to selecting GameSettingSO assets in the Project window.
        /// </summary>
        private void OnSelectionChange()
        {
            if (Selection.activeObject is GameSettingSO selected)
            {
                _selectedSetting = selected;
                _serializedObject = new SerializedObject(_selectedSetting);
                _prefs.lastSelectedAssetID = _selectedSetting.uniqueID; // Remember this selection
                Repaint(); // Redraw the window to show the new selection
            }
            else if (Selection.activeObject == null && _selectedSetting != null)
            {
                // If nothing is selected and we had a selection, clear it.
                _selectedSetting = null;
                _serializedObject = null;
                _prefs.lastSelectedAssetID = "";
                Repaint();
            }
        }

        /// <summary>
        /// Handles context menu events from the Project window.
        /// Used here to refresh the asset list if the user interacts with assets.
        /// </summary>
        private void OnGlobalObjectContextMenu(string clickedPath, string[] selectedPaths)
        {
            RefreshSettingList(); // Refresh list to catch any external changes
            Repaint();
        }

        /// <summary>
        /// The main GUI drawing method for the EditorWindow.
        /// This is where the EditorWindowTools methods are extensively used.
        /// </summary>
        private void OnGUI()
        {
            // Always ensure preferences are initialized before drawing anything
            if (_prefs == null)
            {
                LoadWindowPrefs();
            }

            // Use a scroll view for the entire window content to handle varying content length
            _prefs.scrollPosition = EditorGUILayout.BeginScrollView(_prefs.scrollPosition);

            // ---------------------------------------------------------------------
            // DEMONSTRATION OF EDITORWINDOWTOOLS USAGE
            // ---------------------------------------------------------------------

            // 1. Draw a main header for the window using EditorWindowTools.DrawHeader
            EditorWindowTools.DrawHeader(WINDOW_TITLE, "Manage and edit GameSetting ScriptableObjects easily.");

            // 2. Asset Management Section (collapsible)
            // Uses EditorWindowTools.DrawSectionToggle for a consistent collapsible UI.
            if (EditorWindowTools.DrawSectionToggle("Asset Management", ref _prefs.showAssetManagement, "Create, find, and manage GameSetting assets."))
            {
                EditorGUI.indentLevel++; // Indent content within the section
                EditorGUILayout.Space();

                // Folder picker for asset creation path using EditorWindowTools.DrawFolderPicker
                bool pathChanged = EditorWindowTools.DrawFolderPicker("Creation Path", ref _prefs.assetCreationPath, "Assets/", "Select Asset Creation Folder");
                if (pathChanged)
                {
                    SaveWindowPrefs(); // Save path change immediately
                }
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                // Create New Button: Uses EditorWindowTools.CreateAsset for safe asset creation
                if (GUILayout.Button("Create New Game Setting", EditorWindowTools.ButtonStyle, GUILayout.Height(30)))
                {
                    GameSettingSO newSetting = EditorWindowTools.CreateAsset<GameSettingSO>(_prefs.assetCreationPath, "NewGameSetting");
                    if (newSetting != null)
                    {
                        _selectedSetting = newSetting; // Automatically select the newly created asset
                        _serializedObject = new SerializedObject(_selectedSetting);
                        _prefs.lastSelectedAssetID = _selectedSetting.uniqueID;
                        RefreshSettingList(); // Update list to include the new asset
                    }
                }
                // Refresh List Button
                if (GUILayout.Button("Refresh List", EditorWindowTools.ButtonStyle, GUILayout.Width(100), GUILayout.Height(30)))
                {
                    RefreshSettingList();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                // Display information using EditorWindowTools.DrawHelpBox
                EditorWindowTools.DrawHelpBox($"Found {_allSettings.Count} GameSetting assets.", MessageType.Info);

                // Display list of existing settings
                DisplaySettingList();

                EditorGUI.indentLevel--;
            }
            EditorWindowTools.DrawHorizontalLine(); // Visual separator

            // 3. Selected Asset Details Section (collapsible)
            // Uses EditorWindowTools.DrawSectionToggle for consistency.
            if (EditorWindowTools.DrawSectionToggle("Selected Asset Details", ref _prefs.showSelectedAsset, "View and edit properties of the selected GameSetting."))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();

                if (_selectedSetting != null && _serializedObject != null)
                {
                    EditorGUILayout.BeginVertical(EditorWindowTools.BoxStyle); // Group details in a styled box

                    // Ensure the serialized object is updated before drawing fields.
                    // This is crucial for Undo/Redo and keeping UI in sync with data.
                    _serializedObject.Update();

                    // Display the selected asset using EditorWindowTools.DrawAssetField
                    GameSettingSO newSelected = EditorWindowTools.DrawAssetField("Selected Setting", _selectedSetting, "The currently selected GameSetting to edit.");
                    if (newSelected != _selectedSetting)
                    {
                        _selectedSetting = newSelected;
                        _serializedObject = (newSelected != null) ? new SerializedObject(newSelected) : null;
                        _prefs.lastSelectedAssetID = (newSelected != null) ? newSelected.uniqueID : "";
                        Repaint();
                    }

                    // Use SerializedProperty to draw fields for robustness with Undo/Redo.
                    // This is standard Unity practice for editing ScriptableObjects/MonoBehaviours.
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("settingName"));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("valueInt"));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("valueFloat"));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("valueBool"));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("settingColor"));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("icon"));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("description"));
                    
                    // Display the unique ID (read-only)
                    EditorGUI.BeginDisabledGroup(true); // Make the field non-editable
                    EditorGUILayout.TextField("Unique ID", _selectedSetting.uniqueID);
                    EditorGUI.EndDisabledGroup();

                    // Apply changes to the serialized object and mark the asset dirty.
                    // This tells Unity that the asset needs to be saved.
                    if (_serializedObject.ApplyModifiedProperties())
                    {
                        EditorUtility.SetDirty(_selectedSetting);
                    }

                    EditorGUILayout.Space();

                    // Delete Selected Asset Button
                    if (GUILayout.Button("Delete Selected Asset", EditorWindowTools.ButtonStyle, GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Delete", $"Are you sure you want to delete '{_selectedSetting.name}'?", "Delete", "Cancel"))
                        {
                            string assetPath = AssetDatabase.GetAssetPath(_selectedSetting);
                            AssetDatabase.DeleteAsset(assetPath);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            _selectedSetting = null; // Clear selection
                            _serializedObject = null;
                            _prefs.lastSelectedAssetID = "";
                            RefreshSettingList(); // Update list after deletion
                        }
                    }
                    EditorGUILayout.EndVertical(); // End Box
                }
                else
                {
                    EditorWindowTools.DrawHelpBox("No Game Setting selected. Create a new one or select an existing one from the list.", MessageType.Warning);
                }

                EditorGUI.indentLevel--;
            }
            EditorWindowTools.DrawHorizontalLine(); // Visual separator


            // 4. Window Preferences Section (collapsible)
            // Uses EditorWindowTools.DrawSectionToggle for consistency.
            if (EditorWindowTools.DrawSectionToggle("Window Preferences", ref _prefs.showWindowPrefs, "Save and load custom preferences for this editor window."))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();

                EditorWindowTools.DrawHelpBox("Editor preferences are saved per user and per project, allowing the window to remember its state, " +
                                              "like selected asset and foldout states.", MessageType.Info);

                // Use EditorWindowTools.DrawSaveLoadButtons for consistent Save/Load UI.
                EditorWindowTools.DrawSaveLoadButtons(
                    "Save Window Preferences", "Load Window Preferences",
                    SaveWindowPrefs, LoadWindowPrefs // Pass actions for saving/loading
                );
                
                // Button to clear all preferences for this window.
                if (GUILayout.Button("Clear All Window Preferences", EditorWindowTools.ButtonStyle, GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Confirm Clear Prefs", "Are you sure you want to clear all preferences for this window? This cannot be undone.", "Clear", "Cancel"))
                    {
                        EditorWindowTools.ClearEditorPrefs(EDITOR_PREFS_KEY); // Clear preferences using the tool
                        _prefs = new MyWindowPrefs(); // Reset window preferences to default
                        RefreshSettingList(); // Potentially re-evaluate selections if unique IDs changed or assets were modified
                        Debug.Log("Window preferences reset to default.");
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView(); // End the main scroll view

            // If any GUI element was changed, repaint the window to reflect updates immediately.
            if (GUI.changed)
            {
                Repaint();
            }
        }

        /// <summary>
        /// Saves the current window preferences using EditorWindowTools.SaveEditorPrefs.
        /// </summary>
        private void SaveWindowPrefs()
        {
            if (_prefs == null) _prefs = new MyWindowPrefs(); // Initialize if null
            // Update last selected asset ID before saving
            if (_selectedSetting != null)
            {
                _prefs.lastSelectedAssetID = _selectedSetting.uniqueID;
            }
            else
            {
                _prefs.lastSelectedAssetID = "";
            }
            EditorWindowTools.SaveEditorPrefs(EDITOR_PREFS_KEY, _prefs);
        }

        /// <summary>
        /// Loads window preferences using EditorWindowTools.LoadEditorPrefs.
        /// Includes logic to re-establish the selected asset based on its unique ID.
        /// </summary>
        private void LoadWindowPrefs()
        {
            _prefs = EditorWindowTools.LoadEditorPrefs(EDITOR_PREFS_KEY, new MyWindowPrefs());
            
            // After loading preferences, attempt to re-select the asset that was previously chosen.
            // This ensures the window state is restored correctly.
            RefreshSettingList(); // Ensure _allSettings is up-to-date before trying to find the asset
            if (!string.IsNullOrEmpty(_prefs.lastSelectedAssetID))
            {
                _selectedSetting = _allSettings.FirstOrDefault(s => s.uniqueID == _prefs.lastSelectedAssetID);
                if (_selectedSetting != null)
                {
                    _serializedObject = new SerializedObject(_selectedSetting);
                }
                else
                {
                    // If the asset could not be found (e.g., deleted, ID changed), clear the preference.
                    _prefs.lastSelectedAssetID = ""; 
                    _selectedSetting = null;
                    _serializedObject = null;
                }
            } else {
                _selectedSetting = null;
                _serializedObject = null;
            }
        }

        /// <summary>
        /// Refreshes the list of all GameSettingSO assets found in the project.
        /// Uses AssetDatabase to find assets of a specific type.
        /// </summary>
        private void RefreshSettingList()
        {
            _allSettings.Clear();
            // Find all assets of type GameSettingSO using their type name.
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(GameSettingSO).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameSettingSO setting = AssetDatabase.LoadAssetAtPath<GameSettingSO>(path);
                if (setting != null)
                {
                    setting.EnsureUniqueID(); // Make sure all existing assets have an ID
                    _allSettings.Add(setting);
                }
            }
            _allSettings = _allSettings.OrderBy(s => s.settingName).ToList(); // Order alphabetically
            Repaint(); // Redraw the window as the list has changed
        }

        /// <summary>
        /// Displays the list of all found GameSettingSOs with buttons to select and ping them.
        /// </summary>
        private void DisplaySettingList()
        {
            if (_allSettings.Count == 0)
            {
                EditorWindowTools.DrawHelpBox("No Game Settings found in the project. Use the 'Create New Game Setting' button above!", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorWindowTools.BoxStyle); // Group the list visually in a box
            GUILayout.Label("Existing Game Settings:", EditorWindowTools.SubHeaderStyle);

            foreach (GameSettingSO setting in _allSettings)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = (setting != _selectedSetting); // Disable button if asset is already selected
                if (GUILayout.Button(setting.settingName, EditorWindowTools.ButtonStyle))
                {
                    _selectedSetting = setting;
                    _serializedObject = new SerializedObject(_selectedSetting);
                    _prefs.lastSelectedAssetID = _selectedSetting.uniqueID; // Remember the selection
                    GUI.FocusControl(null); // Deselect the button to prevent it staying "pressed" visually
                }
                GUI.enabled = true; // Re-enable GUI for subsequent elements

                // Button to ping (highlight) the asset in the Project window.
                if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    EditorGUIUtility.PingObject(setting);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical(); // End Box
        }
    }
} // End namespace EditorWindowToolsPattern
#endif // UNITY_EDITOR
```