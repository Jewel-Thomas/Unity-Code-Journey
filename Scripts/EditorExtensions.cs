// Unity Design Pattern Example: EditorExtensions
// This script demonstrates the EditorExtensions pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'EditorExtensions' design pattern in Unity refers to the practice of extending the Unity Editor's functionality to create custom tools, inspectors, windows, and more. This pattern allows developers to tailor the editor to specific project needs, making workflows more efficient, less error-prone, and more user-friendly.

This example will demonstrate:
1.  **A `ScriptableObject`:** Our core data asset that will be managed.
2.  **A `[System.Serializable]` struct/class:** A nested data type within our `ScriptableObject`.
3.  **A `PropertyDrawer`:** To customize how the nested data type is displayed in the Inspector.
4.  **A `CustomEditor`:** To create an entirely custom Inspector for our `ScriptableObject`, using the `PropertyDrawer` where appropriate and adding custom buttons and layout.

This setup is very common for managing game data like items, spells, quests, or configuration settings.

---

**File Structure Requirement:**
The `EditorExtensions` related scripts (`GameItemPropertyDrawer.cs`, `GameDataManagerEditor.cs`) *must* be placed in a folder named `Editor` (or a subfolder within an `Editor` folder) anywhere in your Unity project. The `GameDataManagerSO.cs` script (containing the `ScriptableObject` and `GameItem` class) can be anywhere else in your project.

**Example Setup in Unity:**
1.  Create a folder named `Scripts` (or similar).
2.  Inside `Scripts`, create `GameDataManagerSO.cs` and paste the first code block into it.
3.  Create a folder named `Editor` (e.g., `Assets/Editor`).
4.  Inside `Editor`, create `GameDataManagerEditor.cs` and paste the second code block into it.

---

### **1. Core Game Data (Non-Editor Script)**
This script defines the data structures for our game items and a `ScriptableObject` to hold a collection of these items. This script can be placed anywhere in your project (e.g., `Assets/Scripts/GameDataManagerSO.cs`).

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List

namespace EditorExtensionsExample.GameData // Using a namespace to organize our code
{
    /// <summary>
    /// Represents a single game item with various properties.
    /// This class is marked [System.Serializable] so it can be serialized by Unity
    /// and displayed in the Inspector, even without inheriting from MonoBehaviour or ScriptableObject.
    /// It does NOT need to be in an 'Editor' folder.
    /// </summary>
    [System.Serializable]
    public class GameItem
    {
        public string itemName = "New Item";
        public int itemID = 0;
        [TextArea(3, 5)] // Makes the string field a multi-line text area in the inspector
        public string itemDescription = "A new game item.";
        public Sprite itemIcon; // Reference to an item icon sprite
        public float baseValue = 10.0f; // Base value of the item
        public bool isStackable = true; // Can this item stack?
    }

    /// <summary>
    /// A ScriptableObject that holds a collection of GameItems.
    /// ScriptableObjects are great for storing data assets that don't need to be attached to a GameObject.
    /// The [CreateAssetMenu] attribute allows us to create instances of this ScriptableObject
    /// directly from the Unity Editor's 'Assets/Create' menu.
    /// It does NOT need to be in an 'Editor' folder.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameDataManager", menuName = "EditorExtensions/Game Data Manager")]
    public class GameDataManagerSO : ScriptableObject
    {
        public List<GameItem> items = new List<GameItem>();

        /// <summary>
        /// Adds a new default GameItem to the list.
        /// This method will be called from our custom Editor Extension.
        /// </summary>
        public void AddNewItem()
        {
            items.Add(new GameItem());
        }

        /// <summary>
        /// Clears all items from the list.
        /// This method will be called from our custom Editor Extension.
        /// </summary>
        public void ClearAllItems()
        {
            items.Clear();
        }

        /// <summary>
        /// Sorts the items by their name.
        /// This method will be called from our custom Editor Extension.
        /// </summary>
        public void SortItemsByName()
        {
            items.Sort((a, b) => string.Compare(a.itemName, b.itemName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// An example method to get an item by ID.
        /// </summary>
        /// <param name="id">The ID of the item to find.</param>
        /// <returns>The GameItem if found, otherwise null.</returns>
        public GameItem GetItemByID(int id)
        {
            return items.Find(item => item.itemID == id);
        }
    }
}
```

---

### **2. Editor Extensions (Editor Folder Required)**
This script contains the `PropertyDrawer` and `CustomEditor`. It **must** be placed inside an `Editor` folder in your Unity project (e.g., `Assets/Editor/GameDataManagerEditor.cs`).

```csharp
using UnityEngine;
using UnityEditor; // This namespace is crucial for all Editor Extensions
using System.Collections.Generic;
using EditorExtensionsExample.GameData; // Reference our game data namespace

namespace EditorExtensionsExample.EditorExtensions // Using a specific namespace for editor tools
{
    /// <summary>
    /// PropertyDrawer for the GameItem class.
    /// This allows us to customize how each individual GameItem is drawn in the Inspector,
    /// especially when it's part of a list or array.
    /// The [CustomPropertyDrawer(typeof(GameItem))] attribute links this drawer to our GameItem class.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameItem))]
    public class GameItemPropertyDrawer : PropertyDrawer
    {
        // Define heights and spacing for consistent layout
        private const float LineHeight = EditorGUIUtility.singleLineHeight;
        private const float Spacing = EditorGUIUtility.standardVerticalSpacing;

        /// <summary>
        /// Calculates the total height needed to draw the GameItem property.
        /// This is essential for lists and arrays to allocate enough vertical space for each item.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Calculate height for each field:
            // 1 line for name/ID
            // 3 lines for description/icon (due to TextArea attribute on description)
            // 1 line for baseValue
            // 1 line for isStackable
            // Total: 1 + 3 + 1 + 1 = 6 lines + 3 * spacing (between rows of fields)
            return (LineHeight * 6) + (Spacing * 3);
        }

        /// <summary>
        /// Draws the custom GUI for the GameItem property.
        /// </summary>
        /// <param name="position">The rectangle on the screen to draw the property within.</param>
        /// <param name="property">The SerializedProperty representing the GameItem instance.</param>
        /// <param name="label">The label to display for the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property ensures that
            // prefab override logic works correctly on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw the foldout label for the GameItem itself
            // If the property is not expanded, we don't draw its children.
            // label.text will be "Element 0", "Element 1", etc. when in a list.
            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, LineHeight), property.isExpanded, label, true);

            if (property.isExpanded)
            {
                // Indent the child fields relative to the parent label
                EditorGUI.indentLevel++;

                // Calculate rects for each field, starting from the second line (after the foldout label)
                Rect currentRect = new Rect(position.x, position.y + LineHeight + Spacing, position.width, LineHeight);

                // Find properties by their field names
                SerializedProperty itemNameProp = property.FindPropertyRelative("itemName");
                SerializedProperty itemIDProp = property.FindPropertyRelative("itemID");
                SerializedProperty itemDescriptionProp = property.FindPropertyRelative("itemDescription");
                SerializedProperty itemIconProp = property.FindPropertyRelative("itemIcon");
                SerializedProperty baseValueProp = property.FindPropertyRelative("baseValue");
                SerializedProperty isStackableProp = property.FindPropertyRelative("isStackable");

                // Draw Name and ID on the same line
                float halfWidth = position.width * 0.5f - Spacing * 0.5f;
                Rect nameRect = new Rect(currentRect.x, currentRect.y, halfWidth, LineHeight);
                Rect idRect = new Rect(currentRect.x + halfWidth + Spacing, currentRect.y, halfWidth, LineHeight);

                EditorGUI.PropertyField(nameRect, itemNameProp, new GUIContent("Name"));
                EditorGUI.PropertyField(idRect, itemIDProp, new GUIContent("ID"));

                // Move to the next line for Description and Icon
                currentRect.y += LineHeight + Spacing;
                Rect descriptionRect = new Rect(currentRect.x, currentRect.y, position.width * 0.7f - Spacing, LineHeight * 3); // Allocate 3 lines for TextArea
                Rect iconRect = new Rect(currentRect.x + position.width * 0.7f, currentRect.y, position.width * 0.3f, LineHeight * 3); // Allocate 3 lines for Sprite field

                EditorGUI.PropertyField(descriptionRect, itemDescriptionProp, new GUIContent("Description"));
                EditorGUI.PropertyField(iconRect, itemIconProp, new GUIContent("Icon"));

                // Move to the next line for Base Value
                currentRect.y += LineHeight * 3 + Spacing; // Jump 3 lines + spacing
                EditorGUI.PropertyField(currentRect, baseValueProp, new GUIContent("Base Value"));

                // Move to the next line for Is Stackable
                currentRect.y += LineHeight + Spacing;
                EditorGUI.PropertyField(currentRect, isStackableProp, new GUIContent("Is Stackable"));

                // Reset indent level
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty(); // End the property drawing context
        }
    }


    /// <summary>
    /// CustomEditor for the GameDataManagerSO ScriptableObject.
    /// This replaces the default Inspector Unity provides for GameDataManagerSO,
    /// allowing us to draw custom UI, add buttons, and organize information better.
    /// The [CustomEditor(typeof(GameDataManagerSO))] attribute links this editor to our ScriptableObject.
    /// </summary>
    [CustomEditor(typeof(GameDataManagerSO))]
    public class GameDataManagerEditor : Editor
    {
        // SerializedProperty is used to work with properties through Unity's serialization system.
        // This is crucial for Undo/Redo functionality and prefab overrides.
        private SerializedProperty itemsProperty;

        /// <summary>
        /// Called when the editor is enabled.
        /// This is where we typically find the properties we want to draw.
        /// </summary>
        void OnEnable()
        {
            // Find the 'items' property within the target ScriptableObject (GameDataManagerSO)
            // The 'serializedObject' is a reference to the SerializedObject of the object being inspected.
            itemsProperty = serializedObject.FindProperty("items");
        }

        /// <summary>
        /// This is the main method where we draw our custom Inspector GUI.
        /// It's called every time the Inspector needs to be redrawn.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Always call Update() at the beginning of OnInspectorGUI.
            // This pulls the latest values from the actual object into the SerializedObject representation.
            serializedObject.Update();

            // Cast the target object to our specific type.
            // This allows us to call methods directly on the GameDataManagerSO instance.
            GameDataManagerSO manager = (GameDataManagerSO)target;

            // --- Custom Header and Styling ---
            EditorGUILayout.LabelField("Game Data Manager", EditorStyles.boldHeader);
            EditorGUILayout.HelpBox("Use this manager to organize your game's items. You can add, clear, and sort items directly from here.", MessageType.Info);
            EditorGUILayout.Space();

            // --- Draw the List of Items ---
            // EditorGUILayout.PropertyField draws the property using its default inspector logic,
            // or a CustomPropertyDrawer if one is defined (which we have for GameItem!).
            // The 'true' argument ensures that child properties (like the fields within GameItem) are drawn.
            EditorGUILayout.PropertyField(itemsProperty, true);

            EditorGUILayout.Space();

            // --- Custom Buttons for Actions ---
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldHeader);

            // Add New Item Button
            if (GUILayout.Button("Add New Item"))
            {
                // Call the method directly on the target object.
                manager.AddNewItem();
                // Mark the object as dirty to ensure Unity saves the changes.
                // This is crucial for ScriptableObjects when modifying them directly outside of SerializedProperty.
                EditorUtility.SetDirty(manager);
            }

            // Sort Items Button
            if (GUILayout.Button("Sort Items by Name"))
            {
                manager.SortItemsByName();
                EditorUtility.SetDirty(manager);
            }

            // Clear All Items Button with Confirmation Dialog
            if (GUILayout.Button("Clear All Items"))
            {
                // Display a confirmation dialog before performing a destructive action.
                if (EditorUtility.DisplayDialog(
                    "Clear All Items", // Dialog title
                    "Are you absolutely sure you want to clear ALL game items? This action cannot be undone.", // Message
                    "Yes, Clear Them", // OK button text
                    "No, Cancel" // Cancel button text
                ))
                {
                    manager.ClearAllItems();
                    EditorUtility.SetDirty(manager);
                }
            }

            EditorGUILayout.Space();

            // --- Apply Changes ---
            // Always call ApplyModifiedProperties() at the end of OnInspectorGUI.
            // This writes the changes from the SerializedObject back to the actual target object.
            // It also handles Undo/Redo automatically for properties modified via SerializedProperty.
            serializedObject.ApplyModifiedProperties();
        }
    }
}
```