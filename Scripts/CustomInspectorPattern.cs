// Unity Design Pattern Example: CustomInspectorPattern
// This script demonstrates the CustomInspectorPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The **Custom Inspector Pattern** in Unity allows developers to completely customize how a `MonoBehaviour` (or `ScriptableObject`) appears and behaves in the Inspector window. Instead of Unity's default reflection-based inspector, you can define your own UI elements, logic, and layout, making your scripts more intuitive, robust, and user-friendly for designers and other team members.

This pattern is incredibly useful for:
*   **Conditional Field Visibility:** Showing/hiding fields based on the value of another field.
*   **Custom UI Elements:** Adding buttons, sliders, progress bars, or even complex nested UIs.
*   **Data Validation:** Displaying warnings or errors when data is invalid.
*   **Workflow Enhancements:** Providing quick actions or tools directly in the Inspector.
*   **Improved Readability:** Organizing complex data into collapsible groups or tabs.

---

### Example: Custom Inspector for a `GameItem`

We'll create a `GameItem` script that can represent different types of items (Weapon, Armor, Potion). Its inspector will dynamically change based on the selected `ItemType`, showing only relevant fields and including custom buttons and validation.

This example consists of two main files:

1.  **`GameItem.cs`**: The `MonoBehaviour` script that holds the actual game item data.
2.  **`GameItemEditor.cs`**: The `Editor` script that defines the custom Inspector UI for `GameItem`.

---

**1. `GameItem.cs` (The MonoBehaviour Script)**

This script defines the data for our game item. Notice how type-specific properties (like `damage`, `defenseRating`, `healingAmount`) are marked with `[HideInInspector]`. This tells Unity's *default* inspector not to draw them, as their visibility will be completely managed by our custom inspector.

```csharp
// GameItem.cs
// This script defines the data structure for a game item.

using UnityEngine;
using System.Collections; // Required for Coroutines if any are used, good practice to include if might be needed.

/// <summary>
/// Represents a generic game item with various properties that can change based on its type.
/// </summary>
[System.Serializable] // Important for nested classes/structs if you have them, though not strictly needed here.
public class GameItem : MonoBehaviour
{
    // --- Item Type Definition ---
    public enum ItemType
    {
        Weapon,
        Armor,
        Potion
    }

    [Header("Basic Item Properties")]
    [Tooltip("The unique identifier name for this item.")]
    [SerializeField]
    private string itemName = "New Game Item";

    [Tooltip("A brief description of the item's purpose or lore.")]
    [TextArea(3, 5)] // Makes the string field a multiline text area in the inspector.
    [SerializeField]
    private string itemDescription = "A generic item.";

    [Tooltip("The visual representation (icon) of this item.")]
    [SerializeField]
    private Sprite itemIcon;

    [Tooltip("The category this item belongs to. This determines which other properties are relevant.")]
    [SerializeField]
    private ItemType itemType = ItemType.Weapon;

    // --- Type-Specific Properties ---
    // These properties are hidden from Unity's default inspector
    // because our custom inspector will manage their visibility and display.

    // Weapon Properties
    [Header("Weapon Properties (if ItemType is Weapon)")]
    [HideInInspector] // Hide from default inspector, our custom inspector will handle it.
    [SerializeField]
    private int damage = 10;

    [HideInInspector]
    [SerializeField]
    private float attackSpeed = 1.0f;

    // Armor Properties
    [Header("Armor Properties (if ItemType is Armor)")]
    [HideInInspector] // Hide from default inspector.
    [SerializeField]
    private int defenseRating = 5;

    [HideInInspector]
    [SerializeField]
    private ArmorType armorType = ArmorType.Light;

    public enum ArmorType
    {
        Light,
        Medium,
        Heavy
    }

    // Potion Properties
    [Header("Potion Properties (if ItemType is Potion)")]
    [HideInInspector] // Hide from default inspector.
    [SerializeField]
    private int healingAmount = 25;

    [HideInInspector]
    [SerializeField]
    private float effectDuration = 0f; // Potions might have an effect duration (e.g., buff potions)

    // --- Public Getters ---
    // It's good practice to provide public getters for private serialized fields.
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public Sprite ItemIcon => itemIcon;
    public ItemType GetItemType => itemType;

    public int Damage => damage;
    public float AttackSpeed => attackSpeed;
    public int DefenseRating => defenseRating;
    public ArmorType GetArmorType => armorType;
    public int HealingAmount => healingAmount;
    public float EffectDuration => effectDuration;

    // --- Example Item Actions ---
    /// <summary>
    /// Simulates using the item. In a real game, this would trigger game logic.
    /// </summary>
    public void UseItem()
    {
        Debug.Log($"Using {itemName}! Type: {itemType}.");

        switch (itemType)
        {
            case ItemType.Weapon:
                Debug.Log($"Dealt {damage} damage with attack speed {attackSpeed}.");
                break;
            case ItemType.Armor:
                Debug.Log($"Equipped {GetArmorType} armor with {defenseRating} defense.");
                break;
            case ItemType.Potion:
                if (effectDuration > 0)
                {
                    Debug.Log($"Healed for {healingAmount} and gained an effect for {effectDuration} seconds.");
                }
                else
                {
                    Debug.Log($"Healed for {healingAmount}.");
                }
                break;
        }
    }

    // Example for validation: Check if item name is set
    public bool IsItemNameValid()
    {
        return !string.IsNullOrEmpty(itemName) && itemName.Trim().Length > 0;
    }
}
```

---

**2. `GameItemEditor.cs` (The Custom Inspector Script)**

This script *must* be placed inside a folder named `Editor` anywhere in your Unity project (e.g., `Assets/Editor/GameItemEditor.cs`). This tells Unity that it's an editor-only script and shouldn't be compiled into the final game build.

```csharp
// GameItemEditor.cs
// This script defines the custom editor for the GameItem MonoBehaviour.

using UnityEngine;
using UnityEditor; // Essential for editor scripting.

/// <summary>
/// Custom Inspector for the GameItem class.
/// This class demonstrates the 'CustomInspectorPattern' by providing a tailored UI
/// for GameItem objects in the Unity Editor.
/// </summary>
[CustomEditor(typeof(GameItem))] // This attribute links this editor script to the GameItem class.
public class GameItemEditor : Editor // All custom inspectors must inherit from UnityEditor.Editor.
{
    // --- Cached SerializedProperties ---
    // We cache SerializedProperty objects for better performance and to leverage Unity's
    // serialization system, which handles undo/redo, prefab overrides, and multi-object editing automatically.
    private SerializedProperty itemNameProp;
    private SerializedProperty itemDescriptionProp;
    private SerializedProperty itemIconProp;
    private SerializedProperty itemTypeProp;

    // Type-specific properties
    private SerializedProperty damageProp;
    private SerializedProperty attackSpeedProp;
    private SerializedProperty defenseRatingProp;
    private SerializedProperty armorTypeProp;
    private SerializedProperty healingAmountProp;
    private SerializedProperty effectDurationProp;

    /// <summary>
    /// Called when the inspector is initialized or when the selection changes.
    /// Used to find and cache the SerializedProperties.
    /// </summary>
    private void OnEnable()
    {
        // Find all serialized properties of the target GameItem object.
        itemNameProp = serializedObject.FindProperty("itemName");
        itemDescriptionProp = serializedObject.FindProperty("itemDescription");
        itemIconProp = serializedObject.FindProperty("itemIcon");
        itemTypeProp = serializedObject.FindProperty("itemType");

        // Find type-specific properties. Even if they are [HideInInspector], we can still find them.
        damageProp = serializedObject.FindProperty("damage");
        attackSpeedProp = serializedObject.FindProperty("attackSpeed");
        defenseRatingProp = serializedObject.FindProperty("defenseRating");
        armorTypeProp = serializedObject.FindProperty("armorType");
        healingAmountProp = serializedObject.FindProperty("healingAmount");
        effectDurationProp = serializedObject.FindProperty("effectDuration");
    }

    /// <summary>
    /// This is the core method where you draw your custom Inspector UI.
    /// It's called whenever the Inspector needs to be drawn or redrawn.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // --- 1. Update the SerializedObject ---
        // Always call Update() at the beginning of OnInspectorGUI.
        // This ensures the SerializedObject reflects the latest values from the actual MonoBehaviour.
        serializedObject.Update();

        // --- 2. Get a reference to the target object ---
        // 'target' is a property of the Editor base class, representing the object being inspected.
        // We cast it to our specific type (GameItem).
        GameItem gameItem = (GameItem)target;

        // --- 3. Draw Common Properties ---
        EditorGUILayout.LabelField("Base Item Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(itemNameProp); // Draws a field for the 'itemName' property.
        EditorGUILayout.PropertyField(itemDescriptionProp);
        EditorGUILayout.PropertyField(itemIconProp);
        EditorGUILayout.PropertyField(itemTypeProp);

        // --- 4. Conditional UI based on ItemType ---
        // This is a key advantage of custom inspectors: dynamically showing/hiding fields.
        EditorGUILayout.Space(); // Add some vertical spacing for readability.
        EditorGUILayout.LabelField("Item Type Specific Settings", EditorStyles.boldLabel);

        // Use the enum value to determine which fields to show.
        // itemTypeProp.enumValueIndex gives us the integer index of the selected enum value.
        // We cast it to ItemType to make comparisons readable.
        GameItem.ItemType currentItemType = (GameItem.ItemType)itemTypeProp.enumValueIndex;

        switch (currentItemType)
        {
            case GameItem.ItemType.Weapon:
                EditorGUILayout.PropertyField(damageProp);
                EditorGUILayout.PropertyField(attackSpeedProp);
                break;
            case GameItem.ItemType.Armor:
                EditorGUILayout.PropertyField(defenseRatingProp);
                EditorGUILayout.PropertyField(armorTypeProp);
                break;
            case GameItem.ItemType.Potion:
                EditorGUILayout.PropertyField(healingAmountProp);
                // Only show effect duration if healing amount is greater than 0, or just always show for potions
                // For demonstration, let's always show it for potions.
                EditorGUILayout.PropertyField(effectDurationProp);
                break;
        }

        EditorGUILayout.Space(10); // More spacing.

        // --- 5. Custom Button ---
        // Adds a button that triggers a method on the target MonoBehaviour.
        if (GUILayout.Button("Simulate Use Item"))
        {
            // Direct call to the method on the target object.
            gameItem.UseItem();
        }

        EditorGUILayout.Space(5);

        // --- 6. Custom Validation/Warnings ---
        // Display a warning message if the item name is empty.
        if (!gameItem.IsItemNameValid())
        {
            // Displays a message box in the inspector.
            EditorGUILayout.HelpBox("Item Name cannot be empty!", MessageType.Warning);
        }

        // --- 7. Apply Modified Properties ---
        // Always call ApplyModifiedProperties() at the end of OnInspectorGUI.
        // This writes the changes back to the actual MonoBehaviour, handles undo/redo,
        // and marks the scene as dirty (requiring saving).
        if (GUI.changed) // Check if any GUI element was changed before applying.
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
```

---

### How to Use This Example in Unity:

1.  **Create `GameItem.cs`:**
    *   In your Unity project, create a new C# script named `GameItem.cs` (e.g., in `Assets/Scripts/`).
    *   Copy and paste the code from the `GameItem.cs` section above into this file.

2.  **Create `GameItemEditor.cs`:**
    *   In your Unity project, create a new folder named `Editor` (e.g., `Assets/Editor/`).
    *   Inside the `Editor` folder, create a new C# script named `GameItemEditor.cs`.
    *   Copy and paste the code from the `GameItemEditor.cs` section above into this file.

3.  **Test in Unity:**
    *   Create an empty GameObject in your scene (`GameObject -> Create Empty`).
    *   Add the `GameItem` component to this new GameObject (`Add Component -> GameItem`).
    *   Observe the Inspector:
        *   You will see the custom layout with "Basic Item Properties" and "Item Type Specific Settings".
        *   Change the "Item Type" dropdown:
            *   If you select "Weapon", you'll see "Damage" and "Attack Speed" fields.
            *   If you select "Armor", you'll see "Defense Rating" and "Armor Type" fields.
            *   If you select "Potion", you'll see "Healing Amount" and "Effect Duration" fields.
        *   Click the "Simulate Use Item" button to see debug messages in the Console.
        *   Try deleting the "Item Name" field text; a warning message will appear.

---

### Key Concepts and Benefits of the Custom Inspector Pattern:

*   **`[CustomEditor(typeof(YourMonoBehaviour))]`**: This attribute tells Unity which `MonoBehaviour` (or `ScriptableObject`) this custom editor is for.
*   **`UnityEditor.Editor`**: Your custom inspector script *must* inherit from this base class.
*   **`OnEnable()`**: Ideal for caching `SerializedProperty` references. This is called once when the inspector is first opened or when the selection changes.
*   **`OnInspectorGUI()`**: The heart of the custom inspector. This method is called every time the inspector needs to be drawn or updated. You place all your custom UI drawing logic here.
*   **`serializedObject`**: This is a `SerializedObject` representing the `MonoBehaviour` you are inspecting. It's crucial for properly interacting with the target object's data.
    *   **`serializedObject.Update()`**: *Always* call this at the beginning of `OnInspectorGUI()`. It synchronizes the `SerializedObject` with the actual `MonoBehaviour`'s data, ensuring you're working with the most up-to-date values.
    *   **`serializedObject.FindProperty("propertyName")`**: Used to get a `SerializedProperty` for a specific field by its name. Using `SerializedProperty` is highly recommended over direct field access because it automatically handles:
        *   **Undo/Redo**: Changes made via `SerializedProperty` are automatically registered with Unity's undo system.
        *   **Multi-Object Editing**: If multiple `GameItem` objects are selected, changes apply correctly to all of them.
        *   **Prefab Overrides**: Properly handles prefab instances and their overrides.
    *   **`serializedObject.ApplyModifiedProperties()`**: *Always* call this at the end of `OnInspectorGUI()`. It writes any changes made to the `SerializedProperties` back to the actual `MonoBehaviour` and marks the scene as dirty, prompting the user to save changes.
*   **`EditorGUILayout` and `EditorGUI`**: These static classes provide various methods for drawing standard Unity UI controls (labels, fields, buttons, toggles, sliders, etc.) in your custom inspector.
    *   `EditorGUILayout` methods automatically handle layout (spacing, alignment).
    *   `EditorGUI` methods give you finer control over positioning and sizing.
*   **`GUILayout.Button()` / `EditorGUILayout.PropertyField()`**:
    *   `GUILayout.Button("Text")`: Creates a button.
    *   `EditorGUILayout.PropertyField(mySerializedProperty)`: This is the most common way to draw a field. It automatically draws the appropriate UI control based on the `SerializedProperty`'s type (e.g., a text field for string, a dropdown for enum, a float field for float).
*   **Direct Access (`gameItem.UseItem()` / `gameItem.IsItemNameValid()`)**: While you use `SerializedProperty` for data fields, you can directly call public methods on your `target` `MonoBehaviour` for actions or querying its state.

By following this pattern, you can create highly specialized and intuitive tools within the Unity Editor, significantly enhancing productivity and reducing potential errors in your project.