// Unity Design Pattern Example: CharacterCustomization
// This script demonstrates the CharacterCustomization pattern in Unity
// Generated automatically - ready to use in your Unity project

The Character Customization design pattern in Unity allows for a flexible, data-driven system to change a character's appearance. It typically involves:

1.  **Customization Slots:** Defined areas on the character (e.g., Head, Torso, Legs) that can be altered.
2.  **Customization Options/Parts:** The actual assets (e.g., meshes, materials, textures) that can be applied to a slot.
3.  **Character Customizer:** A central component that manages the character's slots, applies selected options, and handles persistence.

This example uses `ScriptableObject` for customization options, making them easily creatable assets in your Unity project, and a `MonoBehaviour` to manage the application of these options to a character's `SkinnedMeshRenderer` components.

---

### `CharacterCustomization.cs`

This single script file contains all the necessary classes and enums.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for LINQ queries like FirstOrDefault, Where

/*
 * ====================================================================================================
 * CHARACTER CUSTOMIZATION DESIGN PATTERN EXAMPLE FOR UNITY
 * ====================================================================================================
 *
 * This script demonstrates a practical implementation of a Character Customization system
 * using Scriptable Objects for customization data and a central MonoBehaviour for management.
 *
 * Pattern Elements:
 * 1.  CustomizationSlotType (Enum): Defines the categories of customizable body parts.
 * 2.  CustomizationOptionSO (Abstract ScriptableObject): Base class for all customization options.
 *     It acts as a data container and defines the interface for applying/clearing options.
 * 3.  MeshCustomizationOptionSO (Concrete ScriptableObject): An example implementation for
 *     options that change a SkinnedMeshRenderer's mesh and materials.
 * 4.  CharacterCustomizationSlot (Serializable Class): A helper class for configuring
 *     individual slots directly in the Unity Inspector.
 * 5.  CharacterCustomizer (MonoBehaviour): The main component attached to the character.
 *     It orchestrates the customization process, manages the character's renderers,
 *     and handles saving/loading of customization configurations.
 *
 * Why this pattern?
 * - Data-Driven: Customization options are ScriptableObjects, easily created and managed by designers.
 * - Extensible: New types of customization (e.g., color, texture, particle effects) can be added
 *   by creating new `CustomizationOptionSO` subclasses without modifying existing code.
 * - Decoupled: Customization options encapsulate their own application logic, separating it from
 *   the central `CharacterCustomizer`.
 * - Persistence: Includes basic saving/loading of configurations using PlayerPrefs.
 */

// --- 1. Customization Slot Types (Enum) ---
// Defines the different categories or "slots" on a character that can be customized.
// Using an enum makes it type-safe and easy to manage in the Inspector and code.
public enum CustomizationSlotType
{
    Head,
    Torso,
    Legs,
    Hair,
    Eyes,
    Weapon,
    Accessory // Example for other types
}

// --- 2. Base Customization Option (Abstract ScriptableObject) ---
// This is the abstract base class for all character customization options.
// ScriptableObjects are ideal here as they allow designers to create
// numerous assets (e.g., "Warrior Helmet", "Leather Tunic") in the Project window.
// They store data and the logic of how to apply/clear themselves.
[CreateAssetMenu(fileName = "NewCustomizationOption", menuName = "Character Customization/Base Option", order = 0)]
public abstract class CustomizationOptionSO : ScriptableObject
{
    [Tooltip("The type of slot this option belongs to (e.g., Head, Torso).")]
    public CustomizationSlotType slotType;

    [Tooltip("The display name of this customization option for UI.")]
    public string optionName = "New Option";

    [Tooltip("An optional icon for UI display (e.g., in a customization menu).")]
    public Sprite icon;

    [Tooltip("A unique ID for this option, critical for saving/loading configurations. Automatically generated.")]
    public string optionID = System.Guid.NewGuid().ToString();

    // Abstract method to apply the specific option.
    // The CharacterCustomizer will call this, passing the relevant SkinnedMeshRenderer.
    public abstract void ApplyTo(SkinnedMeshRenderer targetRenderer);

    // Abstract method to clear the specific option, reverting the renderer to its default state.
    public abstract void ClearFrom(SkinnedMeshRenderer targetRenderer, Mesh defaultMesh, Material[] defaultMaterials);

    // This method is called when the ScriptableObject is first created in the editor
    private void OnValidate()
    {
        // Ensure optionID is set if it's empty, primarily for new assets.
        if (string.IsNullOrEmpty(optionID) || optionID == "00000000-0000-0000-0000-000000000000")
        {
            optionID = System.Guid.NewGuid().ToString();
        }
    }
}

// --- 3. Concrete Customization Option: Mesh & Material ---
// This ScriptableObject handles options that change a character's mesh and materials.
// This is common for changing armor, body parts, hair styles, etc.
[CreateAssetMenu(fileName = "NewMeshOption", menuName = "Character Customization/Mesh Option", order = 1)]
public class MeshCustomizationOptionSO : CustomizationOptionSO
{
    [Tooltip("The Mesh to apply to the target SkinnedMeshRenderer.")]
    public Mesh mesh;

    [Tooltip("The Materials to apply to the target SkinnedMeshRenderer.")]
    public Material[] materials;

    [Tooltip("If true, the renderer will be disabled when this option is applied (e.g., for 'no helmet' option).")]
    public bool disableRenderer = false;

    // Implementation for applying a mesh/material option.
    // It directly manipulates the SkinnedMeshRenderer provided by the CharacterCustomizer.
    public override void ApplyTo(SkinnedMeshRenderer targetRenderer)
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"Target SkinnedMeshRenderer for slot {slotType} is null. Cannot apply '{optionName}'.", this);
            return;
        }

        targetRenderer.enabled = !disableRenderer;
        if (!disableRenderer)
        {
            targetRenderer.sharedMesh = mesh;
            targetRenderer.sharedMaterials = materials;
        }
        else // If disabling, ensure mesh and materials are cleared to avoid rendering issues if re-enabled later without an option
        {
            targetRenderer.sharedMesh = null;
            targetRenderer.sharedMaterials = new Material[0];
        }
    }

    // Implementation for clearing a mesh/material option.
    // Resets the renderer to its default mesh and materials, and restores its default enabled state.
    public override void ClearFrom(SkinnedMeshRenderer targetRenderer, Mesh defaultMesh, Material[] defaultMaterials)
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"Target SkinnedMeshRenderer for slot {slotType} is null during clear.", this);
            return;
        }

        // Revert to default state
        targetRenderer.enabled = true; // Assume default is enabled, or CharacterCustomizer will handle based on defaultEnabledState
        targetRenderer.sharedMesh = defaultMesh;
        targetRenderer.sharedMaterials = defaultMaterials;
    }
}

// --- 4. Character Customization Slot Data Structure (Serializable Class) ---
// A serializable helper class to define each customizable slot directly in the Inspector
// of the CharacterCustomizer MonoBehaviour. This makes configuration very designer-friendly.
[System.Serializable]
public class CharacterCustomizationSlot
{
    [Tooltip("The type of slot (e.g., Head, Torso). This must be unique for each slot defined.")]
    public CustomizationSlotType slotType;

    [Tooltip("The SkinnedMeshRenderer component that this slot will control. " +
             "Drag the GameObject containing the SMR here.")]
    public SkinnedMeshRenderer targetRenderer;

    [Tooltip("If true, this slot can be completely empty/hidden. Otherwise, it will revert to a default mesh when cleared.")]
    public bool canBeEmpty = false;

    // These fields store the initial state of the renderer when the game starts.
    // This allows us to revert to the character's default appearance.
    [HideInInspector] public Mesh defaultMesh;
    [HideInInspector] public Material[] defaultMaterials;
    [HideInInspector] public bool defaultEnabledState; // Stores initial 'enabled' state of the renderer
}

// --- 5. The Main CharacterCustomizer Component (MonoBehaviour) ---
// This is the central manager for all character customization.
// Attach this script to your character's root GameObject.
public class CharacterCustomizer : MonoBehaviour
{
    [Header("Customization Slots")]
    [Tooltip("Define all customizable slots and link them to their SkinnedMeshRenderers here.")]
    [SerializeField]
    private List<CharacterCustomizationSlot> _slots = new List<CharacterCustomizationSlot>();

    // Internal dictionary for quick lookup of slots by type (initialized from _slots list).
    private Dictionary<CustomizationSlotType, CharacterCustomizationSlot> _slotMap = new Dictionary<CustomizationSlotType, CharacterCustomizationSlot>();

    // Stores the currently applied customization option for each slot.
    // A null value indicates the slot is currently showing its default appearance or is empty.
    private Dictionary<CustomizationSlotType, CustomizationOptionSO> _currentCustomizations = new Dictionary<CustomizationSlotType, CustomizationOptionSO>();

    // All available customization options in the game.
    // This list is crucial for loading configurations (to find options by their ID).
    // Designers should drag ALL `CustomizationOptionSO` assets here.
    [Header("Available Options (Required for Persistence)")]
    [Tooltip("Drag ALL possible CustomizationOptionSO assets (Mesh Options, etc.) here. " +
             "This list is used to find options when loading saved configurations.")]
    [SerializeField]
    private List<CustomizationOptionSO> _allAvailableOptions = new List<CustomizationOptionSO>();

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        InitializeSlots();
    }

    // Initializes the slot map and stores the default state of each renderer.
    private void InitializeSlots()
    {
        _slotMap.Clear();
        _currentCustomizations.Clear();

        foreach (var slot in _slots)
        {
            if (_slotMap.ContainsKey(slot.slotType))
            {
                Debug.LogError($"Duplicate CustomizationSlotType '{slot.slotType}' found in CharacterCustomizer on '{gameObject.name}'. " +
                               $"Please ensure each slot type is unique.", this);
                continue;
            }

            _slotMap.Add(slot.slotType, slot);

            // Store the default state of the renderer
            if (slot.targetRenderer != null)
            {
                slot.defaultMesh = slot.targetRenderer.sharedMesh;
                // Clone materials array to prevent modification of the original array by later assignments.
                slot.defaultMaterials = (Material[])slot.targetRenderer.sharedMaterials.Clone();
                slot.defaultEnabledState = slot.targetRenderer.enabled;
            }
            else
            {
                Debug.LogWarning($"Slot '{slot.slotType}' has no targetRenderer assigned on '{gameObject.name}'. " +
                                 $"This slot will not be customizable.", this);
            }

            // Initially, no specific custom option is applied; it's showing its default.
            _currentCustomizations.Add(slot.slotType, null);
        }
        Debug.Log($"CharacterCustomizer on '{gameObject.name}' initialized with {_slots.Count} slots.", this);
    }

    // --- Public API for Customization ---

    /// <summary>
    /// Applies a given customization option to the character.
    /// This is the primary method to change a character's appearance.
    /// </summary>
    /// <param name="option">The CustomizationOptionSO asset to apply.</param>
    public void ApplyCustomization(CustomizationOptionSO option)
    {
        if (option == null)
        {
            Debug.LogWarning("Attempted to apply a null customization option.", this);
            return;
        }

        // Find the corresponding CharacterCustomizationSlot for the option's slot type.
        if (!_slotMap.TryGetValue(option.slotType, out CharacterCustomizationSlot slot))
        {
            Debug.LogWarning($"No slot configured for type: {option.slotType} on character {gameObject.name}. " +
                             $"Cannot apply '{option.optionName}'.", this);
            return;
        }

        if (slot.targetRenderer == null)
        {
            Debug.LogWarning($"Slot {slot.slotType} has no target renderer assigned. " +
                             $"Cannot apply '{option.optionName}'.", this);
            return;
        }

        // Delegate the actual application logic to the CustomizationOptionSO.
        // This is where the 'Strategy' or 'Command' pattern aspect comes in:
        // the option knows *how* to apply itself.
        option.ApplyTo(slot.targetRenderer);

        // Update the internal dictionary to reflect the new current customization.
        _currentCustomizations[option.slotType] = option;

        Debug.Log($"Applied '{option.optionName}' to slot '{option.slotType}'.", this);
    }

    /// <summary>
    /// Clears the customization from a specific slot, reverting it to its default state
    /// or disabling its renderer if 'canBeEmpty' is true for that slot.
    /// </summary>
    /// <param name="slotType">The type of slot to clear.</param>
    public void ClearSlot(CustomizationSlotType slotType)
    {
        if (!_slotMap.TryGetValue(slotType, out CharacterCustomizationSlot slot))
        {
            Debug.LogWarning($"No slot configured for type: {slotType} on character {gameObject.name}. Cannot clear.", this);
            return;
        }

        if (slot.targetRenderer == null)
        {
            Debug.LogWarning($"Slot {slotType} has no target renderer assigned. Cannot clear slot.", this);
            return;
        }

        // Check if the slot allows being completely empty/hidden.
        if (slot.canBeEmpty)
        {
            slot.targetRenderer.enabled = false; // Disable the renderer
            slot.targetRenderer.sharedMesh = null; // Clear mesh
            slot.targetRenderer.sharedMaterials = new Material[0]; // Clear materials
            _currentCustomizations[slotType] = null; // Mark as empty/cleared
            Debug.Log($"Cleared slot '{slotType}' and disabled its renderer (canBeEmpty is true).", this);
        }
        else
        {
            // Revert to the stored default mesh and materials.
            // We use the `ClearFrom` method of the *current* option if it exists,
            // otherwise directly set defaults if no custom option was ever applied.
            CustomizationOptionSO currentOption = _currentCustomizations[slotType];
            if (currentOption != null)
            {
                currentOption.ClearFrom(slot.targetRenderer, slot.defaultMesh, slot.defaultMaterials);
            }
            else
            {
                // If no custom option was ever set, just restore the direct defaults.
                slot.targetRenderer.enabled = slot.defaultEnabledState;
                slot.targetRenderer.sharedMesh = slot.defaultMesh;
                slot.targetRenderer.sharedMaterials = slot.defaultMaterials;
            }
            _currentCustomizations[slotType] = null; // Mark as default
            Debug.Log($"Cleared slot '{slotType}' and reverted to default appearance.", this);
        }
    }

    /// <summary>
    /// Clears all customizations from the character, reverting all slots to their default states
    /// or disabling them if 'canBeEmpty' is true.
    /// </summary>
    public void ClearAllCustomizations()
    {
        // Iterate through a copy of keys to avoid collection modification issues if ClearSlot changes dictionary state.
        foreach (var slotType in _slotMap.Keys.ToList())
        {
            ClearSlot(slotType);
        }
        Debug.Log("All character customizations cleared.", this);
    }

    /// <summary>
    /// Retrieves the currently applied customization options for all slots.
    /// Returns a read-only dictionary to prevent external modification.
    /// </summary>
    /// <returns>A dictionary mapping CustomizationSlotType to its currently applied CustomizationOptionSO.</returns>
    public IReadOnlyDictionary<CustomizationSlotType, CustomizationOptionSO> GetCurrentCustomizations()
    {
        return _currentCustomizations;
    }

    /// <summary>
    /// Retrieves a list of all available customization options for a specific slot type,
    /// based on the `_allAvailableOptions` list configured in the Inspector.
    /// Useful for populating UI menus.
    /// </summary>
    /// <param name="slotType">The type of slot to get options for.</param>
    /// <returns>A list of CustomizationOptionSO assets for the specified slot type.</returns>
    public List<CustomizationOptionSO> GetOptionsForSlot(CustomizationSlotType slotType)
    {
        return _allAvailableOptions
            .Where(option => option != null && option.slotType == slotType)
            .OrderBy(option => option.optionName) // Optional: order alphabetically
            .ToList();
    }


    // --- Persistence Example (using Unity's PlayerPrefs) ---
    // In a real game, you might use a more robust saving system (e.g., JSON, binary serialization).

    private const string CUSTOMIZATION_PREFIX = "CharacterCustomization_";

    /// <summary>
    /// Saves the current character customization configuration to PlayerPrefs.
    /// Each slot's currently applied option's unique ID is stored.
    /// </summary>
    /// <param name="configName">A unique name for this saved configuration (e.g., "PlayerOutfit1", "DefaultLoadout").</param>
    public void SaveCustomizationConfiguration(string configName)
    {
        foreach (var slotType in _slotMap.Keys)
        {
            CustomizationOptionSO currentOption = _currentCustomizations.ContainsKey(slotType) ? _currentCustomizations[slotType] : null;
            string optionID = currentOption?.optionID; // Get ID if an option is applied, otherwise null.

            // Use "NONE" string to indicate an empty or default slot.
            PlayerPrefs.SetString(CUSTOMIZATION_PREFIX + configName + "_" + slotType.ToString(), optionID ?? "NONE");
            Debug.Log($"Saved '{slotType}': '{optionID ?? "NONE"}' for config '{configName}'", this);
        }
        PlayerPrefs.Save(); // Ensure changes are written to disk.
        Debug.Log($"Customization configuration '{configName}' saved successfully.", this);
    }

    /// <summary>
    /// Loads a character customization configuration from PlayerPrefs and applies it.
    /// This will clear all current customizations before applying the loaded ones.
    /// Requires `_allAvailableOptions` to be populated in the Inspector.
    /// </summary>
    /// <param name="configName">The unique name of the configuration to load.</param>
    public void LoadCustomizationConfiguration(string configName)
    {
        ClearAllCustomizations(); // Start with a clean slate to ensure correct loading.

        foreach (var slotType in _slotMap.Keys)
        {
            string key = CUSTOMIZATION_PREFIX + configName + "_" + slotType.ToString();
            string savedOptionID = PlayerPrefs.GetString(key, "NONE"); // Default to "NONE" if key doesn't exist.

            if (savedOptionID != "NONE")
            {
                // Find the CustomizationOptionSO asset by its unique ID.
                CustomizationOptionSO optionToApply = _allAvailableOptions.FirstOrDefault(opt => opt != null && opt.optionID == savedOptionID);
                if (optionToApply != null)
                {
                    ApplyCustomization(optionToApply);
                }
                else
                {
                    Debug.LogWarning($"Option with ID '{savedOptionID}' for slot '{slotType}' not found in '_allAvailableOptions'. " +
                                     $"This slot will remain at its default/cleared state. Check if the asset exists and is in the list.", this);
                    ClearSlot(slotType); // Ensure it's cleared if the option is missing
                }
            }
            else
            {
                // If "NONE" was saved, it means that slot should be cleared/defaulted.
                // ClearSlot already handles reverting to default or disabling if canBeEmpty.
                ClearSlot(slotType);
            }
        }
        Debug.Log($"Customization configuration '{configName}' loaded successfully.", this);
    }

    /// <summary>
    /// Checks if a customization configuration with the given name exists in PlayerPrefs.
    /// (Checks for at least one slot's key for simplicity).
    /// </summary>
    /// <param name="configName">The name of the configuration.</param>
    /// <returns>True if the configuration exists, false otherwise.</returns>
    public bool HasSavedConfiguration(string configName)
    {
        // For simplicity, we just check if the first slot's key exists.
        // A more robust check might iterate all slots or save a separate "config exists" flag.
        if (_slotMap.Count > 0)
        {
            CustomizationSlotType firstSlotType = _slotMap.Keys.First();
            string key = CUSTOMIZATION_PREFIX + configName + "_" + firstSlotType.ToString();
            return PlayerPrefs.HasKey(key);
        }
        return false;
    }
}
```

---

### How to Use and Set Up in Unity

Here's a step-by-step guide to integrate and use the Character Customization system in your Unity project:

**1. Create Customization Slot Types:**
   - The `CustomizationSlotType` enum in `CharacterCustomization.cs` defines the customizable areas of your character (e.g., Head, Torso, Legs). Extend this enum to fit your game's specific needs (e.g., `LeftHand`, `RightHand`, `Shoes`).

**2. Create Customization Option ScriptableObjects:**
   - In your Unity Project window, right-click -> Create -> Character Customization -> **Mesh Option**.
   - Create several of these assets (e.g., `Head_WarriorHelmet`, `Torso_LeatherTunic`, `Hair_LongBlonde`, `Head_Bald`).
   - For each `MeshCustomizationOptionSO`:
     - **Slot Type:** Select the appropriate enum value (e.g., `Head`, `Torso`, `Hair`).
     - **Option Name:** Give it a descriptive name (e.g., "Warrior Helmet", "Leather Tunic", "Bald Head").
     - **Mesh:** Drag your desired `Mesh` asset here (e.g., a `.fbx` mesh for a helmet).
     - **Materials:** Drag your `Material` assets into this array.
     - **Disable Renderer:** If this option means the slot should be invisible (e.g., `Head_Bald` to hide a separate hair mesh, or "No Armor" option), check this box.

**3. Prepare Your Character Model:**
   - Your character model should typically be a GameObject hierarchy.
   - Each *customizable part* (Head, Torso, Legs, Hair, etc.) should have its own GameObject, and crucially, its own `SkinnedMeshRenderer` component.
   - **Example Hierarchy:**
     ```
     CharacterRoot (GameObject)
       - Body (GameObject, contains the base body mesh, e.g., default skin `SkinnedMeshRenderer`)
       - HeadSlot (GameObject, contains a `SkinnedMeshRenderer` for head gear/hair)
       - TorsoSlot (GameObject, contains a `SkinnedMeshRenderer` for torso armor)
       - LegsSlot (GameObject, contains a `SkinnedMeshRenderer` for leg armor)
       - RightHandWeapon (GameObject, contains a `SkinnedMeshRenderer` or `MeshRenderer` for a weapon)
     ```
   - The `SkinnedMeshRenderer` components on these "slot" GameObjects will be targeted by the customizer. They should initially contain placeholder meshes/materials or the character's default appearance.

**4. Attach the `CharacterCustomizer` Script:**
   - Create an empty GameObject (or use your character's root GameObject).
   - Add the `CharacterCustomizer` component to it.

**5. Configure `CharacterCustomizer` in the Inspector:**
   - **`Slots` List:**
     - Increase the size of the `Slots` list to match your customizable parts.
     - For each element:
       - **Slot Type:** Select the corresponding enum value (e.g., `Head`, `Torso`, `Hair`). Make sure each `slotType` is unique.
       - **Target Renderer:** Drag the `SkinnedMeshRenderer` component (from the "HeadSlot" GameObject, "TorsoSlot" GameObject, etc.) into this field.
       - **Can Be Empty:** Check this if the slot can be completely hidden (e.g., for a "no helmet" option on a separate headgear `SkinnedMeshRenderer`).
   - **`All Available Options` List (Crucial for Persistence):**
     - Drag *all* the `CustomizationOptionSO` assets (e.g., `MeshCustomizationOptionSO` assets) you created in step 2 into this list.
     - This list is used by the `LoadCustomizationConfiguration` method to find options by their `optionID`. If an option isn't in this list, it cannot be loaded.

**6. Interact with the Customizer (Example Script):**
   - Create another script, e.g., `CharacterCustomizationUIController.cs`, to handle UI interactions or programmatic changes.
   - Attach it to a GameObject in your scene (e.g., a UI manager, or even the `CharacterRoot` itself).
   - Get a reference to the `CharacterCustomizer` component.

   ```csharp
   using UnityEngine;
   using System.Collections.Generic;
   using System.Linq; // For List<T> methods if needed
   // You would typically use Unity UI components (Button, Text) here
   // using UnityEngine.UI; 

   public class CharacterCustomizationUIController : MonoBehaviour
   {
       [Header("References")]
       [SerializeField] private CharacterCustomizer _characterCustomizer;

       [Header("Example Customization Options (Drag ScriptableObjects Here)")]
       [SerializeField] private MeshCustomizationOptionSO _warriorHelmet;
       [SerializeField] private MeshCustomizationOptionSO _leatherTunic;
       [SerializeField] private MeshCustomizationOptionSO _plateArmor;
       [SerializeField] private MeshCustomizationOptionSO _baldHeadOption; // An option that might disable renderer for hair/head

       [Header("Persistence Settings")]
       [SerializeField] private string _savedOutfitName = "MyDefaultWarriorOutfit";

       void Start()
       {
           // Attempt to find the CharacterCustomizer if not assigned in Inspector
           if (_characterCustomizer == null)
           {
               _characterCustomizer = FindObjectOfType<CharacterCustomizer>();
               if (_characterCustomizer == null)
               {
                   Debug.LogError("CharacterCustomizer not found in scene! Please assign it or ensure one exists.");
                   enabled = false; // Disable this script if no customizer is found
                   return;
               }
           }

           Debug.Log("CharacterCustomizationUIController initialized. Press keys to customize!");

           // Example: Load default outfit on start if it exists
           if (_characterCustomizer.HasSavedConfiguration(_savedOutfitName))
           {
               Debug.Log($"Loading default outfit '{_savedOutfitName}' on start...");
               _characterCustomizer.LoadCustomizationConfiguration(_savedOutfitName);
           }
       }

       void Update()
       {
           // --- Example UI Input (replace with actual UI buttons/sliders in a real project) ---

           // Apply Head Customizations
           if (Input.GetKeyDown(KeyCode.Alpha1))
           {
               Debug.Log("Applying Warrior Helmet...");
               _characterCustomizer.ApplyCustomization(_warriorHelmet);
           }
           if (Input.GetKeyDown(KeyCode.Alpha2))
           {
               Debug.Log("Applying Bald Head option...");
               _characterCustomizer.ApplyCustomization(_baldHeadOption);
           }

           // Apply Torso Customizations
           if (Input.GetKeyDown(KeyCode.Alpha3))
           {
               Debug.Log("Applying Leather Tunic...");
               _characterCustomizer.ApplyCustomization(_leatherTunic);
           }
           if (Input.GetKeyDown(KeyCode.Alpha4))
           {
               Debug.Log("Applying Plate Armor...");
               _characterCustomizer.ApplyCustomization(_plateArmor);
           }

           // Clear specific slots
           if (Input.GetKeyDown(KeyCode.Z))
           {
               Debug.Log("Clearing Head slot (reverts to default or hides if canBeEmpty)...");
               _characterCustomizer.ClearSlot(CustomizationSlotType.Head);
           }
           if (Input.GetKeyDown(KeyCode.X))
           {
               Debug.Log("Clearing Torso slot...");
               _characterCustomizer.ClearSlot(CustomizationSlotType.Torso);
           }

           // Clear all customizations
           if (Input.GetKeyDown(KeyCode.C))
           {
               Debug.Log("Clearing ALL customizations from character...");
               _characterCustomizer.ClearAllCustomizations();
           }

           // Persistence (Save/Load)
           if (Input.GetKeyDown(KeyCode.S))
           {
               Debug.Log($"Saving current outfit as '{_savedOutfitName}'...");
               _characterCustomizer.SaveCustomizationConfiguration(_savedOutfitName);
           }
           if (Input.GetKeyDown(KeyCode.L))
           {
               Debug.Log($"Loading outfit '{_savedOutfitName}'...");
               _characterCustomizer.LoadCustomizationConfiguration(_savedOutfitName);
           }

           // Inspect current state
           if (Input.GetKeyDown(KeyCode.P))
           {
               Debug.Log("--- Current Customizations ---");
               foreach (var entry in _characterCustomizer.GetCurrentCustomizations())
               {
                   string optionName = entry.Value != null ? entry.Value.optionName : "DEFAULT/NONE";
                   Debug.Log($"- {entry.Key}: {optionName}");
               }
           }
       }

       // --- Example for populating a UI customization menu ---
       public void PopulateUISlotOptions(CustomizationSlotType slotType, Transform parentTransformForButtons)
       {
           // Clear existing buttons first
           foreach (Transform child in parentTransformForButtons)
           {
               Destroy(child.gameObject);
           }

           List<CustomizationOptionSO> options = _characterCustomizer.GetOptionsForSlot(slotType);

           // Add an option to clear the slot
           GameObject clearButtonGO = new GameObject($"Clear {slotType} Button");
           clearButtonGO.transform.SetParent(parentTransformForButtons);
           // Example: Attach a Button component and set its text and click listener
           // Button clearButton = clearButtonGO.AddComponent<Button>();
           // clearButton.GetComponentInChildren<Text>().text = $"Clear {slotType}";
           // clearButton.onClick.AddListener(() => _characterCustomizer.ClearSlot(slotType));
           Debug.Log($"Created UI 'Clear {slotType}' button.");


           foreach (var option in options)
           {
               GameObject optionButtonGO = new GameObject(option.optionName + " Button");
               optionButtonGO.transform.SetParent(parentTransformForButtons);
               // Example: Attach a Button component and set its text and click listener
               // Button optionButton = optionButtonGO.AddComponent<Button>();
               // optionButton.GetComponentInChildren<Text>().text = option.optionName;
               // optionButton.onClick.AddListener(() => _characterCustomizer.ApplyCustomization(option));
               // If you have icons, set: optionButton.GetComponent<Image>().sprite = option.icon;
               Debug.Log($"Created UI button for '{option.optionName}'.");
           }
           Debug.Log($"Populated UI for {slotType} with {options.Count} options.");
       }
   }
   ```

**7. Assign References in `CharacterCustomizationUIController`:**
   - In the Inspector for your `CharacterCustomizationUIController` component:
     - Drag your `CharacterCustomizer` GameObject to the `_characterCustomizer` field.
     - Drag your created `MeshCustomizationOptionSO` assets (e.g., `_warriorHelmet`, `_leatherTunic`) to their respective fields.
     - (Optional) Assign a UI parent transform if you want to test `PopulateUISlotOptions`.

**Run your Unity scene!** Press the assigned keys to apply, clear, save, and load customizations. Observe how your character's meshes and materials change dynamically.