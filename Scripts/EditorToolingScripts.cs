// Unity Design Pattern Example: EditorToolingScripts
// This script demonstrates the EditorToolingScripts pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'EditorToolingScripts' design pattern in Unity involves creating custom scripts that extend the Unity editor's functionality. These scripts are typically placed in a special `Editor` folder (or a subfolder within it) and do not get included in the final game build. Their purpose is to enhance developer workflow, automate tasks, or provide custom UI elements within the Unity editor.

This example demonstrates the pattern using a simple `MonoBehaviour` (`MyLevelObject`) and two editor scripts:
1.  **A Custom Inspector (`MyLevelObjectEditor`)**: To customize how `MyLevelObject` instances appear in the Inspector window.
2.  **A Custom Editor Window (`LevelObjectAssignerWindow`)**: To create a standalone tool for batch operations on `MyLevelObject` instances across the scene.

---

**Project Setup:**

1.  Create a new Unity project.
2.  In the `Assets` folder, create a new folder named `Scripts`.
3.  In the `Assets` folder, create a new folder named `Editor`.

---

**1. The `MonoBehaviour` (Target for Editor Tools)**

This script will be attached to GameObjects in your scene. It has a unique ID, an importance flag, and a custom tag.

**File:** `Assets/Scripts/MyLevelObject.cs`

```csharp
using UnityEngine;
using System; // Required for Guid

// This is a regular MonoBehaviour script.
// It contains data and logic relevant to a game object in your scene.
// Editor tooling will be built around inspecting and manipulating instances of this class.
public class MyLevelObject : MonoBehaviour
{
    // A unique identifier for this object.
    // We want our editor tool to help manage these, e.g., auto-generate if missing.
    [Tooltip("A unique identifier for this level object.")]
    public string objectID;

    // A flag to mark important objects.
    // Editor tools can provide quick ways to toggle or filter by this.
    [Tooltip("Is this object considered important?")]
    public bool isImportant;

    // A custom tag string, separate from Unity's built-in tags.
    // Editor tools can offer dropdowns or batch assignment for this.
    [Tooltip("A custom tag for categorization.")]
    public string customTag;

    [Tooltip("A general purpose value, could be anything.")]
    public float someValue;

    // This method is called in the editor when a new component is added,
    // or when resetting an existing one. We can use it to initialize.
    void Reset()
    {
        // Ensure a unique ID is present when the component is first added or reset.
        if (string.IsNullOrEmpty(objectID))
        {
            GenerateNewID();
        }
        customTag = "Untagged";
        isImportant = false;
        someValue = 0f;
    }

    /// <summary>
    /// Generates a new unique ID for this object using a GUID.
    /// This method can be called by both runtime code and editor tools.
    /// </summary>
    public void GenerateNewID()
    {
        objectID = Guid.NewGuid().ToString();
        Debug.Log($"Generated new ID for {gameObject.name}: {objectID}");
    }

    /// <summary>
    /// Example method that could be called by an editor tool for a specific action.
    /// </summary>
    public void SetImportantAndTag(string tag)
    {
        isImportant = true;
        customTag = tag;
        Debug.Log($"Set {gameObject.name} as Important with tag: {tag}");
    }
}
```

---

**2. The Custom Inspector (Editor Script)**

This script customizes the Inspector UI for `MyLevelObject`. It *must* be in an `Editor` folder.

**File:** `Assets/Editor/MyLevelObjectEditor.cs`

```csharp
using UnityEditor; // Required for Editor, CustomEditor, EditorGUILayout, etc.
using UnityEngine;
using System.Linq; // Required for LINQ operations like Select, Distinct

// This script demonstrates the 'EditorToolingScripts' pattern by creating
// a custom inspector for the 'MyLevelObject' MonoBehaviour.
//
// Key aspects of EditorToolingScripts:
// 1. Located in an 'Editor' folder: This ensures it's only compiled and run in the Unity Editor,
//    and not included in the final game build.
// 2. Extends 'Editor': This is the base class for all custom inspectors.
// 3. Uses '[CustomEditor(typeof(MyLevelObject))]': This attribute tells Unity which class
//    this custom inspector is for.
// 4. Overrides 'OnInspectorGUI()': This is where you draw your custom UI for the inspector.

[CustomEditor(typeof(MyLevelObject))]
public class MyLevelObjectEditor : Editor
{
    // SerializedProperty allows you to work with properties without direct field access,
    // which is crucial for proper Undo/Redo functionality and multi-object editing.
    private SerializedProperty objectIDProp;
    private SerializedProperty isImportantProp;
    private SerializedProperty customTagProp;
    private SerializedProperty someValueProp;

    // An array of common tags that can be used in a dropdown.
    // This could also be loaded from a ScriptableObject for more flexible management.
    private string[] commonTags = { "LevelGeometry", "Prop", "Pickup", "Enemy", "PlayerStart", "Decoration" };

    // Called when the inspector is enabled (e.g., when an object with MyLevelObject is selected).
    private void OnEnable()
    {
        // Find the SerializedProperty for each field.
        // The string argument must match the exact field name in MyLevelObject.
        objectIDProp = serializedObject.FindProperty("objectID");
        isImportantProp = serializedObject.FindProperty("isImportant");
        customTagProp = serializedObject.FindProperty("customTag");
        someValueProp = serializedObject.FindProperty("someValue");
    }

    // This is the main method where you draw your custom UI in the inspector.
    public override void OnInspectorGUI()
    {
        // 1. Update the serialized object:
        //    Always call this at the beginning of OnInspectorGUI to ensure the SerializedObject
        //    is up-to-date with the latest values from the target object(s).
        serializedObject.Update();

        // --- Drawing Custom UI Elements ---

        // Display the objectID. Make it read-only but selectable.
        EditorGUILayout.LabelField("Unique Object ID", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(objectIDProp.stringValue, EditorStyles.wordWrappedLabel);

        // Button to generate a new ID.
        if (GUILayout.Button("Generate New Unique ID"))
        {
            // Iterate over all selected objects this inspector applies to.
            // 'targets' is an array of all selected objects of the type 'MyLevelObject'.
            foreach (MyLevelObject myObject in targets)
            {
                // Record the current state of the object for Undo functionality.
                // This is crucial for any editor modification.
                Undo.RecordObject(myObject, "Generate New ID for " + myObject.name);
                myObject.GenerateNewID(); // Call the method on the actual object.
                // Mark the object as dirty to ensure Unity saves the changes.
                EditorUtility.SetDirty(myObject);
            }
        }

        EditorGUILayout.Space();

        // Display 'isImportant' with a toggle.
        EditorGUILayout.PropertyField(isImportantProp);

        // Custom dropdown for 'customTag'.
        EditorGUILayout.LabelField("Custom Tag", EditorStyles.boldLabel);
        int currentTagIndex = ArrayUtility.IndexOf(commonTags, customTagProp.stringValue);
        int newTagIndex = EditorGUILayout.Popup("Select Tag", currentTagIndex, commonTags);

        if (newTagIndex != currentTagIndex && newTagIndex >= 0)
        {
            // Apply changes via SerializedProperty for proper Undo/Redo and multi-object editing.
            customTagProp.stringValue = commonTags[newTagIndex];
        }
        else if (newTagIndex == -1 && !string.IsNullOrEmpty(customTagProp.stringValue) && !commonTags.Contains(customTagProp.stringValue))
        {
            // If the current tag is not in our commonTags list, show it in a text field
            // and allow the user to type a new one or choose from the dropdown.
            EditorGUILayout.HelpBox($"Current tag '{customTagProp.stringValue}' is not in the common tags list.", MessageType.Warning);
        }

        // Always provide a fallback or direct input for custom tags.
        EditorGUILayout.PropertyField(customTagProp, new GUIContent("Manual Tag Entry"));


        EditorGUILayout.Space();

        // Display 'someValue' with a slider for better usability.
        EditorGUILayout.LabelField("Custom Value", EditorStyles.boldLabel);
        someValueProp.floatValue = EditorGUILayout.Slider("Value Range", someValueProp.floatValue, 0f, 100f);

        EditorGUILayout.Space();

        // Button to set important and a specific tag.
        if (GUILayout.Button("Mark as Important (Prop)"))
        {
            foreach (MyLevelObject myObject in targets)
            {
                Undo.RecordObject(myObject, "Set Important and Tag for " + myObject.name);
                myObject.SetImportantAndTag("Prop");
                EditorUtility.SetDirty(myObject);
            }
        }

        // 2. Apply modified properties:
        //    Always call this at the end of OnInspectorGUI to write the changes
        //    back to the actual target object(s).
        serializedObject.ApplyModifiedProperties();
    }
}
```

---

**3. The Custom Editor Window (Editor Script)**

This script creates a standalone window accessed via the Unity menu, providing a more complex tool for batch operations. It *must* be in an `Editor` folder.

**File:** `Assets/Editor/LevelObjectAssignerWindow.cs`

```csharp
using UnityEditor; // Required for EditorWindow, MenuItem, EditorGUILayout, EditorUtility, Undo, etc.
using UnityEngine;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ operations

// This script demonstrates creating a custom Editor Window, a more advanced form
// of 'EditorToolingScripts'. It provides a dedicated UI for specific tasks.
//
// Key aspects:
// 1. Located in an 'Editor' folder.
// 2. Extends 'EditorWindow': The base class for custom editor windows.
// 3. Uses '[MenuItem("Path/To/Menu Item")]': This attribute adds an entry to Unity's top menu bar,
//    allowing users to open your window.
// 4. Overrides 'OnGUI()': This is where you draw your custom window UI.

public class LevelObjectAssignerWindow : EditorWindow
{
    private List<MyLevelObject> foundObjects = new List<MyLevelObject>();
    private bool showObjectList = false;
    private string commonTagToAssign = "DefaultBatchTag";
    private bool batchIsImportant = false;
    private float batchSomeValue = 50f;

    // Store a reference to the SerializedObject and its properties for batch operations
    // on currently selected objects if we were to act directly on 'Selection.gameObjects'.
    // For this example, we'll iterate the 'foundObjects' list directly.

    // ------------------------------------------------------------------------------------------
    // 1. Menu Item to Open the Window
    // ------------------------------------------------------------------------------------------
    [MenuItem("Tools/Level Object Tools/Open Assigner Window")]
    public static void ShowWindow()
    {
        // Get existing open window or create a new one.
        LevelObjectAssignerWindow window = GetWindow<LevelObjectAssignerWindow>("Level Object Assigner");
        window.minSize = new Vector2(350, 400); // Set minimum size for the window
        window.Show(); // Display the window
    }

    // ------------------------------------------------------------------------------------------
    // 2. UI Drawing in the Window
    // ------------------------------------------------------------------------------------------
    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("MyLevelObject Batch Operations", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --------------------------------------------------------------------------------------
        // Find All MyLevelObjects in Scene
        // --------------------------------------------------------------------------------------
        if (GUILayout.Button("Find All MyLevelObjects in Scene"))
        {
            FindAllLevelObjects();
        }

        EditorGUILayout.LabelField($"Found {foundObjects.Count} objects.", EditorStyles.miniLabel);

        // Toggle to show/hide the list of found objects.
        showObjectList = EditorGUILayout.Foldout(showObjectList, "Show Found Objects", true);
        if (showObjectList)
        {
            EditorGUI.indentLevel++;
            if (foundObjects.Count > 0)
            {
                foreach (MyLevelObject obj in foundObjects)
                {
                    if (obj != null)
                    {
                        EditorGUILayout.ObjectField(obj.name, obj, typeof(MyLevelObject), true);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No MyLevelObjects found in the current scene.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Enable/Disable buttons based on whether objects are found.
        GUI.enabled = foundObjects.Count > 0;

        // --------------------------------------------------------------------------------------
        // Assign Missing IDs
        // --------------------------------------------------------------------------------------
        if (GUILayout.Button("Assign Missing Unique IDs to Found Objects"))
        {
            AssignMissingIDs();
        }

        EditorGUILayout.Space();

        // --------------------------------------------------------------------------------------
        // Batch Tag Assignment
        // --------------------------------------------------------------------------------------
        EditorGUILayout.LabelField("Batch Tag & Importance", EditorStyles.boldLabel);
        commonTagToAssign = EditorGUILayout.TextField("Tag to Assign", commonTagToAssign);
        batchIsImportant = EditorGUILayout.Toggle("Mark as Important", batchIsImportant);
        batchSomeValue = EditorGUILayout.Slider("Set Value to", batchSomeValue, 0f, 100f);

        if (GUILayout.Button("Apply Batch Properties to Found Objects"))
        {
            ApplyBatchProperties();
        }

        GUI.enabled = true; // Re-enable GUI after conditional disable.
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // --------------------------------------------------------------------------------------
        // Refresh button (for when scene changes outside the window)
        // --------------------------------------------------------------------------------------
        if (GUILayout.Button("Refresh Object List"))
        {
            FindAllLevelObjects();
        }
    }

    // ------------------------------------------------------------------------------------------
    // 3. Editor Logic and Helper Methods
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Finds all MyLevelObject instances in the current scene.
    /// </summary>
    private void FindAllLevelObjects()
    {
        // FindObjectsOfType is an editor-only method that finds all active instances
        // of a specific type in the scene.
        foundObjects = FindObjectsOfType<MyLevelObject>().ToList();
        Debug.Log($"Found {foundObjects.Count} MyLevelObjects in the scene.");
        // Repaint the window to reflect the updated list.
        Repaint();
    }

    /// <summary>
    /// Iterates through found objects and assigns new IDs if they are missing.
    /// </summary>
    private void AssignMissingIDs()
    {
        if (foundObjects == null || foundObjects.Count == 0) return;

        int assignedCount = 0;
        foreach (MyLevelObject obj in foundObjects)
        {
            if (obj != null && string.IsNullOrEmpty(obj.objectID))
            {
                // Record object state for Undo.
                Undo.RecordObject(obj, "Assign Missing ID to " + obj.name);
                obj.GenerateNewID();
                // Mark object as dirty to save changes to the scene.
                EditorUtility.SetDirty(obj);
                assignedCount++;
            }
        }
        Debug.Log($"Assigned IDs to {assignedCount} objects.");
    }

    /// <summary>
    /// Applies the configured batch tag and importance to all found objects.
    /// </summary>
    private void ApplyBatchProperties()
    {
        if (foundObjects == null || foundObjects.Count == 0) return;

        int modifiedCount = 0;
        foreach (MyLevelObject obj in foundObjects)
        {
            if (obj != null)
            {
                // Record object state for Undo before making multiple changes.
                // One Undo operation covers all changes for this object.
                Undo.RecordObject(obj, "Apply Batch Properties to " + obj.name);

                obj.customTag = commonTagToAssign;
                obj.isImportant = batchIsImportant;
                obj.someValue = batchSomeValue;

                // Mark object as dirty to save changes to the scene.
                EditorUtility.SetDirty(obj);
                modifiedCount++;
            }
        }
        Debug.Log($"Applied batch properties to {modifiedCount} objects.");
    }

    // Optional: Lifecycle methods for EditorWindow
    private void OnFocus()
    {
        // When the window gains focus, refresh the object list.
        // This ensures the window is up-to-date if scene objects changed.
        FindAllLevelObjects();
    }
}
```

---

**Example Usage in a Unity Project:**

1.  **Place the scripts:**
    *   `MyLevelObject.cs` goes into `Assets/Scripts/`.
    *   `MyLevelObjectEditor.cs` goes into `Assets/Editor/`.
    *   `LevelObjectAssignerWindow.cs` goes into `Assets/Editor/`.

2.  **Create GameObjects:**
    *   In your scene, create a few empty GameObjects (e.g., `GameObject -> Create Empty`).
    *   Rename them to `Cube_A`, `Sphere_B`, `Cylinder_C`.
    *   Select each GameObject and add the `MyLevelObject` component to it (`Add Component -> MyLevelObject`).

3.  **Test the Custom Inspector:**
    *   Select `Cube_A` in the Hierarchy.
    *   Observe the Inspector window for `MyLevelObject`. You'll see:
        *   A "Unique Object ID" field with a selectable label.
        *   A "Generate New Unique ID" button. Click it to generate a new GUID.
        *   The "Is Important" toggle.
        *   A "Select Tag" dropdown with predefined tags, and a "Manual Tag Entry" field.
        *   A "Value Range" slider.
        *   A "Mark as Important (Prop)" button.
    *   Try selecting multiple `MyLevelObject` instances (e.g., `Cube_A` and `Sphere_B`). The custom inspector will still work, applying changes to all selected objects simultaneously.
    *   Experiment with the Undo (Ctrl/Cmd+Z) functionality after making changes.

4.  **Test the Custom Editor Window:**
    *   In the Unity editor menu bar, go to `Tools -> Level Object Tools -> Open Assigner Window`.
    *   The "Level Object Assigner" window will open.
    *   Click "Find All MyLevelObjects in Scene". You should see your created GameObjects listed (you can expand "Show Found Objects").
    *   Leave some `MyLevelObject`s with empty `objectID`s (e.g., remove the component and add it back, or manually clear the field). Then, click "Assign Missing Unique IDs to Found Objects".
    *   Enter a new tag (e.g., "BatchProcessed") and toggle "Mark as Important" in the window. Set a `Set Value to` value.
    *   Click "Apply Batch Properties to Found Objects". Observe the changes on your GameObjects in the Inspector.
    *   Close and reopen the window, or make changes to scene objects and note how `OnFocus()` refreshes the list.

This complete example provides a solid foundation for understanding and implementing the EditorToolingScripts pattern in your Unity projects, enabling you to build powerful custom tools for your specific development needs.