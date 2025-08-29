// Unity Design Pattern Example: CustomEditorScripts
// This script demonstrates the CustomEditorScripts pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'CustomEditorScripts' design pattern in Unity. It enhances the Unity Inspector for a `SpellManager` MonoBehaviour, allowing designers to easily add, remove, and configure spells with a user-friendly interface.

**Design Pattern: CustomEditorScripts**

*   **Problem:** The default Unity Inspector can become cluttered or insufficient for complex data structures (like lists of custom classes) or when specific workflows are required.
*   **Solution:** Create a custom editor script (inheriting from `UnityEditor.Editor`) that overrides `OnInspectorGUI()`. This allows you to draw your own UI elements, add custom buttons, validate input, and organize information more effectively.
*   **Benefits:**
    *   **Improved Workflow:** Simplifies the design process for complex game systems.
    *   **Clarity:** Presents information in a more organized and intuitive way.
    *   **Validation:** Allows for custom input validation within the editor.
    *   **Automation:** Add buttons for common actions (e.g., "Add Item", "Generate ID").
    *   **Data Integrity:** Can enforce rules and prevent errors at design time.

---

### Step 1: Create the Runtime MonoBehaviour (`SpellManager.cs`)

This script will hold the data that our custom editor will modify. It needs to be a standard C# script in your `Assets` folder (e.g., `Assets/Scripts/SpellManager.cs`).

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List

// This script will be attached to a GameObject in your scene.
// Its data (the list of spells) will be displayed and managed by our Custom Editor.
public class SpellManager : MonoBehaviour
{
    // The [System.Serializable] attribute is crucial!
    // It tells Unity that this custom class can be serialized and
    // its properties should be visible in the Inspector (even without a custom editor).
    // This allows our Custom Editor to access and display its properties easily.
    [System.Serializable]
    public class SpellData
    {
        public string spellName = "New Spell";
        public Sprite spellIcon; // Visual representation of the spell
        [Range(0, 100)] public int damage = 10;
        [Range(0, 50)] public int manaCost = 5;
        [Range(0.1f, 30f)] public float cooldown = 2.0f;
        public bool isAOE = false; // Area of Effect spell?
        [TextArea(3, 5)] public string description = "A basic spell description.";

        // You could add methods here for spell logic if this was a full game system
        public void Cast()
        {
            Debug.Log($"Casting {spellName}! Deals {damage} damage.");
            // Add actual game logic here (e.g., spawn particles, apply effects)
        }
    }

    // This is the public list of SpellData that our Custom Editor will manage.
    // It's public so Unity can serialize it and the editor can access it.
    public List<SpellData> spells = new List<SpellData>();

    // --- Example Runtime Usage ---
    // These methods show how you might use the data configured in the editor.

    // Displays all spell names and basic info to the console.
    public void DisplayAllSpells()
    {
        Debug.Log("--- Available Spells ---");
        if (spells.Count == 0)
        {
            Debug.Log("No spells configured.");
            return;
        }
        foreach (var spell in spells)
        {
            Debug.Log($"Name: {spell.spellName}, Damage: {spell.damage}, Mana: {spell.manaCost}, Cooldown: {spell.cooldown}s");
        }
        Debug.Log("------------------------");
    }

    // Finds a spell by name and casts it.
    public void CastSpellByName(string name)
    {
        SpellData foundSpell = spells.Find(s => s.spellName == name);
        if (foundSpell != null)
        {
            foundSpell.Cast();
        }
        else
        {
            Debug.LogWarning($"Spell '{name}' not found!");
        }
    }

    // Example Unity Lifecycle method (for demonstration, not directly part of the pattern)
    void Start()
    {
        // You might call DisplayAllSpells() here for debugging or initialization.
        // For this example, we're primarily focusing on the editor part.
        // DisplayAllSpells();
    }
}

/*
How to set up the SpellManager in Unity:
1. Create an empty GameObject in your Unity scene (e.g., name it "GameManagers").
2. Attach this 'SpellManager.cs' script to it.
3. Select the "GameManagers" GameObject in the scene.
4. Observe its Inspector: It will initially show the 'spells' list with Unity's default list UI (add/remove buttons, collapsable elements).
5. After creating the 'SpellManagerEditor.cs' (next step) in an 'Editor' folder, re-select the "GameManagers" GameObject.
6. The Inspector will now display the custom editor we created, providing a much more organized and intuitive interface for managing spells!
*/
```

---

### Step 2: Create the Custom Editor Script (`SpellManagerEditor.cs`)

**Crucial:** This script **MUST** be placed inside a folder named `Editor` (or a subfolder within an `Editor` folder), e.g., `Assets/Editor/SpellManagerEditor.cs` or `Assets/Scripts/Game/Editor/SpellManagerEditor.cs`. Unity compiles scripts in `Editor` folders separately and only includes them in the Unity Editor, not in the final game build.

```csharp
// Required namespaces for Unity Editor scripting
using UnityEditor;
using UnityEngine;
using System.Collections.Generic; // Although not strictly needed for this specific drawing, good practice for editor logic that might use collections

// The [CustomEditor(typeof(SpellManager))] attribute is essential!
// It tells Unity that this custom editor class should be used whenever a 'SpellManager' component
// is selected in the Inspector, overriding the default Inspector drawing.
[CustomEditor(typeof(SpellManager))]
public class SpellManagerEditor : Editor
{
    // SerializedProperty allows us to work with properties in a way that Unity
    // understands, ensuring proper undo/redo functionality, multi-object editing,
    // and correct serialization to the asset or scene.
    private SerializedProperty spellsProperty;

    // This method is called when the editor is enabled (e.g., when the GameObject with SpellManager is selected).
    // It's the ideal place to initialize SerializedProperties by finding them.
    void OnEnable()
    {
        // Find the 'spells' property in the target object (which is an instance of SpellManager).
        // The string "spells" must match the name of the public List<SpellData> field in SpellManager.
        spellsProperty = serializedObject.FindProperty("spells");
    }

    // This is the core method where you draw your custom Inspector GUI.
    // Unity calls this method whenever the Inspector needs to refresh.
    public override void OnInspectorGUI()
    {
        // Always call serializedObject.Update() at the beginning of OnInspectorGUI.
        // It fetches the latest values from the target object into the SerializedObject
        // system, ensuring that our editor reflects the current state.
        serializedObject.Update();

        // ---------------------------------------------------------------------
        // PART 1: Drawing Header and General Information
        // ---------------------------------------------------------------------
        EditorGUILayout.LabelField("Spell Manager Configuration", EditorStyles.boldHeader);
        EditorGUILayout.HelpBox("Use this custom editor to manage the list of spells. Each spell can be expanded for details.", MessageType.Info);
        EditorGUILayout.Space(); // Adds a vertical space for better readability

        // Display current number of spells, centered.
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // Pushes content to the right
        EditorGUILayout.LabelField($"Total Spells: {spellsProperty.arraySize}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(150));
        GUILayout.FlexibleSpace(); // Pushes content to the left
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        // ---------------------------------------------------------------------
        // PART 2: Custom Drawing for the 'spells' list
        // ---------------------------------------------------------------------

        // Begin a vertical group for the entire spell list, giving it a box background for visual separation.
        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Loop through each spell in the 'spellsProperty' array.
        // We use 'arraySize' to get the current count of elements.
        for (int i = 0; i < spellsProperty.arraySize; i++)
        {
            // Get the SerializedProperty for the current spell (element at index i).
            SerializedProperty spell = spellsProperty.GetArrayElementAtIndex(i);
            // Get the 'spellName' property specifically to use it as a label for the foldout.
            SerializedProperty spellName = spell.FindPropertyRelative("spellName");

            // Begin another vertical group for each individual spell, using a helpBox style.
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Create a collapsible foldout for each spell.
            // spell.isExpanded controls whether the foldout is open or closed.
            // The label for the foldout is dynamically created using the spell's name.
            // The 'true' argument makes the foldout toggle when its label is clicked.
            spell.isExpanded = EditorGUILayout.Foldout(spell.isExpanded, $"Spell {i + 1}: {spellName.stringValue}", true, EditorStyles.foldoutHeader);

            // If the foldout is expanded, draw the spell's properties.
            if (spell.isExpanded)
            {
                // Increase indentation for children properties for better visual hierarchy.
                EditorGUI.indentLevel++;

                // Draw each property of the SpellData class.
                // EditorGUILayout.PropertyField automatically handles labels, input fields,
                // and even specific controls like Sprite selectors for ObjectReference fields.
                // It respects [Range], [TextArea], etc., attributes from the MonoBehaviour script.
                EditorGUILayout.PropertyField(spellName);
                EditorGUILayout.PropertyField(spell.FindPropertyRelative("spellIcon"));
                EditorGUILayout.PropertyField(spell.FindPropertyRelative("damage"));
                EditorGUILayout.PropertyField(spell.FindPropertyRelative("manaCost"));
                EditorGUILayout.PropertyField(spell.FindPropertyRelative("cooldown"));
                EditorGUILayout.PropertyField(spell.FindPropertyRelative("isAOE"));
                EditorGUILayout.PropertyField(spell.FindPropertyRelative("description"));

                EditorGUILayout.Space(); // Add some space before the remove button

                // Add a "Remove This Spell" button for each individual spell.
                // Only enable if there's more than one spell to avoid accidentally emptying.
                // Or, simply allow it always for convenience.
                if (GUILayout.Button($"Remove '{spellName.stringValue}'", GUILayout.Height(25), GUILayout.ExpandWidth(true)))
                {
                    // Record the current state of the target object for Undo functionality.
                    Undo.RecordObject(target, "Remove Spell");
                    // Delete the array element at the current index.
                    // This shifts subsequent elements up, so be careful if modifying the loop index.
                    spellsProperty.DeleteArrayElementAtIndex(i);
                    // After deleting, we break the loop and force a repaint of the Inspector.
                    // This is simpler than adjusting the loop index and potentially safer.
                    break;
                }

                // Decrease indentation to revert to the parent level.
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical(); // End of individual spell box
            EditorGUILayout.Space(5); // Small space between spell boxes
        }

        // ---------------------------------------------------------------------
        // PART 3: Custom Buttons for List Management (Add/Remove)
        // ---------------------------------------------------------------------
        EditorGUILayout.Space(10); // Add vertical space before buttons

        EditorGUILayout.BeginHorizontal(); // Group buttons horizontally

        // "Add New Spell" button
        if (GUILayout.Button("Add New Spell", GUILayout.Height(30)))
        {
            // Record the current state for Undo.
            Undo.RecordObject(target, "Add New Spell");
            // Increase the array size by one, which adds a new default element.
            spellsProperty.arraySize++;
            // Get the newly added element (which is always at the end of the list).
            SerializedProperty newSpell = spellsProperty.GetArrayElementAtIndex(spellsProperty.arraySize - 1);

            // It's good practice to explicitly reset or set default values for new elements,
            // as Unity's default initialization might not be what you expect for complex classes.
            newSpell.FindPropertyRelative("spellName").stringValue = "New Spell " + spellsProperty.arraySize;
            newSpell.FindPropertyRelative("damage").intValue = 10;
            newSpell.FindPropertyRelative("manaCost").intValue = 5;
            newSpell.FindPropertyRelative("cooldown").floatValue = 2.0f;
            newSpell.FindPropertyRelative("isAOE").boolValue = false;
            newSpell.FindPropertyRelative("description").stringValue = "A newly added spell.";
            newSpell.FindPropertyRelative("spellIcon").objectReferenceValue = null; // Ensure no default icon
        }

        // Only show "Remove Last Spell" button if there are spells to remove.
        if (spellsProperty.arraySize > 0)
        {
            if (GUILayout.Button("Remove Last Spell", GUILayout.Height(30)))
            {
                // Record the current state for Undo.
                Undo.RecordObject(target, "Remove Last Spell");
                // Decrease the array size, effectively removing the last element.
                spellsProperty.arraySize--;
            }
        }
        EditorGUILayout.EndHorizontal(); // End of horizontal button group

        EditorGUILayout.EndVertical(); // End of the main spells list box

        EditorGUILayout.Space();

        // ---------------------------------------------------------------------
        // PART 4: Applying Changes
        // ---------------------------------------------------------------------

        // Always call serializedObject.ApplyModifiedProperties() at the end of OnInspectorGUI.
        // This writes all modified properties back to the target object(s) and marks the scene
        // or asset as dirty, ensuring that changes are saved and Undo/Redo works correctly.
        serializedObject.ApplyModifiedProperties();

        // --- Optional: Add runtime method buttons ---
        // You can cast 'target' to your MonoBehaviour type to access its public methods
        // and create buttons to call them directly from the Inspector.
        SpellManager spellManager = (SpellManager)target;
        if (GUILayout.Button("Display All Spells (Runtime Debug)", GUILayout.Height(35)))
        {
            spellManager.DisplayAllSpells();
        }
        if (GUILayout.Button("Cast 'Fireball' (Example Runtime)", GUILayout.Height(35)))
        {
            spellManager.CastSpellByName("Fireball");
        }
    }
}

/*
Summary of Key Concepts for CustomEditorScripts:

1.  **Placement:** Custom Editor scripts MUST reside in a folder named `Editor` (e.g., `Assets/Editor/YourEditor.cs`). This ensures they are compiled as editor-only code and not included in your game build.

2.  **Inheritance:** Custom Editor scripts must inherit from `UnityEditor.Editor`.

3.  **Targeting:** The `[CustomEditor(typeof(YourMonoBehaviourClass))]` attribute is essential. It tells Unity which specific MonoBehaviour (or ScriptableObject) this editor should customize.

4.  **Namespaces:** You'll typically need `using UnityEditor;` in addition to `using UnityEngine;`.

5.  **`OnInspectorGUI()`:** This is the core method where you draw all your custom UI elements. Unity calls this method whenever the Inspector for the target object needs to be drawn or refreshed.

6.  **`serializedObject` and `SerializedProperty`:**
    *   `serializedObject`: Represents the object being inspected (`SpellManager` in this example). Use this to access the properties of your target object in a safe and Unity-compatible way.
    *   `SerializedProperty`: Represents a single property (like `spells`, `spellName`, `damage`) of the `serializedObject`. Using `SerializedProperty` is crucial because it correctly handles:
        *   **Undo/Redo:** Unity automatically tracks changes made through `SerializedProperty`.
        *   **Multi-object Editing:** If multiple `SpellManager` objects are selected, changes apply to all.
        *   **Prefab Overrides:** Properly handles changes to prefab instances and applies them as overrides.
    *   **Workflow:**
        *   Always call `serializedObject.Update()` at the beginning of `OnInspectorGUI()` to read the latest values from the actual object.
        *   Always call `serializedObject.ApplyModifiedProperties()` at the end of `OnInspectorGUI()` to write the modified values back to the actual object and mark the scene/asset as dirty (for saving).

7.  **`EditorGUILayout` vs. `EditorGUI`:**
    *   `EditorGUILayout`: Provides layout-aware controls. It automatically handles positioning and sizing, stacking elements vertically by default. This is generally preferred for most custom inspectors due to its simplicity.
    *   `EditorGUI`: Provides more fine-grained control over positioning. You explicitly define a `Rect` for each control. This is useful for complex, custom layouts where you need precise pixel control.
    *   This example primarily uses `EditorGUILayout` for its ease of use.

8.  **`[System.Serializable]`:** For custom classes (like `SpellData`) to appear and be editable in the Inspector (even in a default or custom one), they *must* be marked with the `[System.Serializable]` attribute.
*/
```