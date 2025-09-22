// Unity Design Pattern Example: PlayerCustomization
// This script demonstrates the PlayerCustomization pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PlayerCustomization' pattern in Unity, while not a formal Gang of Four design pattern, refers to a structured approach for managing and applying customizable elements to a player character. It emphasizes **decoupling** the customization data from the character logic, **extensibility** for adding new items and categories, and **centralized management** of selected options.

This example demonstrates how to build a robust player customization system using ScriptableObjects for data definition and a MonoBehaviour for runtime management and application.

**Core Components of the Pattern:**

1.  **`CustomizationCategory` Enum:** Defines the distinct slots or parts of the character that can be customized (e.g., Head, Torso, Weapon).
2.  **`CustomizableItem` (Base ScriptableObject):** An abstract base class for all customizable assets. It holds common properties like name and icon.
3.  **Concrete `CustomizableItem` Types (ScriptableObjects):**
    *   `MeshCustomizableItem`: Represents an item that changes a character's mesh (e.g., a helmet, a piece of armor).
    *   `MaterialCustomizableItem`: Represents an item that changes a character's material (e.g., skin color, armor texture).
    *   (You could extend this with `GameObjectCustomizableItem` for swapping prefabs like weapons, or `ShaderCustomizableItem` for dynamic effects).
4.  **`PlayerCustomizationProfile` (ScriptableObject):** A central database that holds *all* available `CustomizableItem`s, organized by their `CustomizationCategory`. This makes the system data-driven and easy to manage content.
5.  **`PlayerCustomizationManager` (MonoBehaviour):** Attached to the player character, this component is the heart of the system.
    *   It references the `PlayerCustomizationProfile`.
    *   It stores the player's *current selections* for each category.
    *   It holds direct references to the `Renderer` components on the player character that will be affected.
    *   It provides methods to change selections, apply them visually, and handle persistence (saving/loading).

---

## 1. Project Setup in Unity

To use this example:

1.  Create a new Unity project (or open an existing one).
2.  Create a new C# Script named `PlayerCustomizationSystem`.
3.  Copy and paste **ALL** the code blocks below into this single `PlayerCustomizationSystem.cs` file. The file structure will be automatically managed by Unity (multiple classes/enums/structs in one file is fine).
4.  **Create Example Assets:**
    *   In your Unity Project window, right-click -> `Create` -> `Customization` -> `Player Customization Profile`. Name it `MyPlayerProfile`.
    *   Right-click -> `Create` -> `Customization` -> `Mesh Item`. Create a few: `Helmet_A`, `Helmet_B`, `Torso_Armor_A`, `Torso_Armor_B`, `Legs_Pants_A`, `Legs_Pants_B`, `Weapon_Sword`, `Weapon_Axe`.
    *   Right-click -> `Create` -> `Customization` -> `Material Item`. Create a few: `Skin_Light`, `Skin_Dark`.
    *   **Assign properties to these ScriptableObjects:** For each `Mesh Item`, drag a `Mesh` asset into its "Target Mesh" slot, and ensure its "Category" is set correctly (e.g., `Helmet_A` -> `Head`). For `Material Item`, drag a `Material` into its "Target Material" slot, and set its Category (e.g., `Skin_Light` -> `SkinColor`).
    *   **Populate `MyPlayerProfile`:** Select `MyPlayerProfile`. In the Inspector, drag all your created `Mesh Item` and `Material Item` ScriptableObjects into the `All Customizable Items` list.
5.  **Prepare a Player Character:**
    *   Create a simple 3D character (e.g., a capsule, or a basic rigged character if you have one). Name it `PlayerCharacter`.
    *   Add `SkinnedMeshRenderer` components for different body parts, or simple `MeshRenderer` for accessories:
        *   `PlayerCharacter` (base object)
            *   `Body` (SkinnedMeshRenderer, for overall body, apply skin materials here)
            *   `HeadSlot` (SkinnedMeshRenderer, to apply head meshes like helmets)
            *   `TorsoSlot` (SkinnedMeshRenderer, to apply torso meshes like armor)
            *   `LegsSlot` (SkinnedMeshRenderer, to apply leg meshes like pants)
            *   `WeaponSlot` (MeshRenderer, to apply weapon meshes like swords)
    *   **Crucially, attach the `PlayerCustomizationManager` component to your `PlayerCharacter` GameObject.**
    *   In the `PlayerCustomizationManager`'s Inspector:
        *   Drag `MyPlayerProfile` into the `Customization Profile` slot.
        *   Drag the respective `SkinnedMeshRenderer`s (`HeadSlot`, `TorsoSlot`, `LegsSlot`, `Body`) and `MeshRenderer` (`WeaponSlot`) into their corresponding slots (`Head Renderer`, `Torso Renderer`, etc.).

---

## 2. The Complete `PlayerCustomizationSystem.cs` Script

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq operations like .FirstOrDefault()

// This file contains all necessary components for the PlayerCustomization design pattern:
// 1. CustomizationCategory Enum
// 2. CustomizableItem (Base ScriptableObject)
// 3. Concrete CustomizableItem types (MeshCustomizableItem, MaterialCustomizableItem)
// 4. PlayerCustomizationProfile (ScriptableObject holding all options)
// 5. PlayerCustomizationManager (MonoBehaviour, the core logic)

namespace PlayerCustomizationSystem
{
    /// <summary>
    /// Represents different categories of customization slots on the player character.
    /// This enum provides a strongly-typed way to refer to specific customizable parts.
    /// </summary>
    public enum CustomizationCategory
    {
        None,      // Default or unassigned category
        Head,      // For helmets, hats, hair
        Torso,     // For chest armor, shirts
        Legs,      // For pants, leg armor
        Weapon,    // For main hand weapons
        SkinColor, // For changing skin material/color
        // Add more categories as needed, e.g., Shoulders, Feet, Back, OffHand, etc.
    }

    /// <summary>
    /// Base class for all customizable items.
    /// This is an abstract ScriptableObject, meaning you can't create an instance directly.
    /// Concrete types (like MeshCustomizableItem, MaterialCustomizableItem) will inherit from this.
    /// </summary>
    public abstract class CustomizableItem : ScriptableObject
    {
        [Tooltip("The unique name of this customization item.")]
        public string itemName;

        [Tooltip("The category this item belongs to (e.g., Head, Torso).")]
        public CustomizationCategory category;

        [Tooltip("An optional icon for UI display.")]
        public Sprite icon;

        /// <summary>
        /// Applies the specific customization item to the target renderer.
        /// This method must be implemented by concrete item types.
        /// </summary>
        /// <param name="targetRenderer">The renderer on the player character to modify.</param>
        public abstract void Apply(Renderer targetRenderer);
    }

    /// <summary>
    /// A concrete customizable item that changes a SkinnedMeshRenderer's mesh.
    /// Used for swapping out different body parts or armor pieces.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMeshCustomizableItem", menuName = "Customization/Mesh Item")]
    public class MeshCustomizableItem : CustomizableItem
    {
        [Tooltip("The Mesh to apply to the SkinnedMeshRenderer.")]
        public Mesh targetMesh;

        [Tooltip("The default materials to apply with this mesh.")]
        public Material[] defaultMaterials;

        /// <summary>
        /// Applies the targetMesh and defaultMaterials to the provided SkinnedMeshRenderer.
        /// </summary>
        public override void Apply(Renderer targetRenderer)
        {
            if (targetRenderer is SkinnedMeshRenderer skinnedRenderer)
            {
                skinnedRenderer.sharedMesh = targetMesh;
                skinnedRenderer.sharedMaterials = defaultMaterials;
                // Note: When swapping meshes on a SkinnedMeshRenderer,
                // ensure the new mesh is rigged to the same skeleton as the original.
                // If bones need to be re-mapped, more advanced logic would be required here.
                // For simplicity, we assume same skeleton or simple mesh swap.
            }
            else if (targetRenderer is MeshRenderer meshRenderer)
            {
                // For static meshes like weapons
                if (meshRenderer.gameObject.TryGetComponent(out MeshFilter meshFilter))
                {
                    meshFilter.sharedMesh = targetMesh;
                    meshRenderer.sharedMaterials = defaultMaterials;
                }
            }
            else
            {
                Debug.LogWarning($"MeshCustomizableItem '{itemName}' cannot be applied to unsupported renderer type: {targetRenderer.GetType().Name} for category {category}.");
            }
        }
    }

    /// <summary>
    /// A concrete customizable item that changes a Renderer's material(s).
    /// Used for changing colors, textures, or applying different skin materials.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaterialCustomizableItem", menuName = "Customization/Material Item")]
    public class MaterialCustomizableItem : CustomizableItem
    {
        [Tooltip("The primary material to apply to the renderer.")]
        public Material targetMaterial;

        [Tooltip("Optional: Index of the material slot to change if the renderer has multiple materials.")]
        public int materialIndex = 0; // Default to changing the first material

        /// <summary>
        /// Applies the targetMaterial to the specified materialIndex of the provided Renderer.
        /// </summary>
        public override void Apply(Renderer targetRenderer)
        {
            if (targetRenderer == null)
            {
                Debug.LogError($"MaterialCustomizableItem '{itemName}' failed to apply: Target Renderer is null.");
                return;
            }

            Material[] currentMaterials = targetRenderer.sharedMaterials;
            if (materialIndex < 0 || materialIndex >= currentMaterials.Length)
            {
                Debug.LogWarning($"MaterialCustomizableItem '{itemName}': Material index {materialIndex} is out of bounds for renderer {targetRenderer.name}. Applying to first slot instead.");
                materialIndex = 0; // Fallback to first material
            }

            if (currentMaterials[materialIndex] != targetMaterial)
            {
                currentMaterials[materialIndex] = targetMaterial;
                targetRenderer.sharedMaterials = currentMaterials;
            }
        }
    }

    /// <summary>
    /// ScriptableObject acting as a database for all available customizable items.
    /// The PlayerCustomizationManager will reference this profile to know what options exist.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerCustomizationProfile", menuName = "Customization/Player Customization Profile")]
    public class PlayerCustomizationProfile : ScriptableObject
    {
        [Tooltip("A list of all possible customizable items for the player.")]
        public List<CustomizableItem> allCustomizableItems = new List<CustomizableItem>();

        /// <summary>
        /// Retrieves all customizable items belonging to a specific category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>A list of CustomizableItems for the given category.</returns>
        public List<CustomizableItem> GetItemsByCategory(CustomizationCategory category)
        {
            return allCustomizableItems.Where(item => item.category == category).ToList();
        }

        /// <summary>
        /// Finds a specific customizable item by its name and category.
        /// </summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="itemName">The name of the item.</param>
        /// <returns>The found CustomizableItem, or null if not found.</returns>
        public CustomizableItem GetItemByName(CustomizationCategory category, string itemName)
        {
            return allCustomizableItems.FirstOrDefault(item => item.category == category && item.itemName == itemName);
        }
    }

    /// <summary>
    /// The core MonoBehaviour that manages and applies player customizations.
    /// It stores the current selections, interacts with the CustomizationProfile,
    /// and applies changes to the character's renderers.
    /// </summary>
    public class PlayerCustomizationManager : MonoBehaviour
    {
        [Tooltip("The profile containing all available customization options.")]
        public PlayerCustomizationProfile customizationProfile;

        [Header("Target Renderers for Customization")]
        [Tooltip("SkinnedMeshRenderer for the player's head (e.g., for helmets).")]
        public SkinnedMeshRenderer headRenderer;
        [Tooltip("SkinnedMeshRenderer for the player's torso (e.g., for armor).")]
        public SkinnedMeshRenderer torsoRenderer;
        [Tooltip("SkinnedMeshRenderer for the player's legs (e.g., for pants).")]
        public SkinnedMeshRenderer legsRenderer;
        [Tooltip("MeshRenderer for a weapon slot (e.g., for swords).")]
        public MeshRenderer weaponRenderer;
        [Tooltip("SkinnedMeshRenderer for the base body (e.g., for skin color material changes).")]
        public SkinnedMeshRenderer bodyBaseRenderer;

        // A dictionary to store the currently selected item for each category.
        private Dictionary<CustomizationCategory, CustomizableItem> _currentSelections = new Dictionary<CustomizationCategory, CustomizableItem>();

        // A dictionary to map categories to their respective target renderers.
        // This makes the ApplyCustomization logic cleaner.
        private Dictionary<CustomizationCategory, Renderer> _categoryToRendererMap = new Dictionary<CustomizationCategory, Renderer>();

        private void Awake()
        {
            if (customizationProfile == null)
            {
                Debug.LogError("PlayerCustomizationManager: Customization Profile is not assigned!", this);
                return;
            }

            // Initialize the category-to-renderer map
            _categoryToRendererMap[CustomizationCategory.Head] = headRenderer;
            _categoryToRendererMap[CustomizationCategory.Torso] = torsoRenderer;
            _categoryToRendererMap[CustomizationCategory.Legs] = legsRenderer;
            _categoryToRendererMap[CustomizationCategory.Weapon] = weaponRenderer;
            _categoryToRendererMap[CustomizationCategory.SkinColor] = bodyBaseRenderer;

            // Load saved customizations or set defaults
            LoadCustomizations();

            // Apply all currently selected items to the character
            ApplyAllCustomizations();
        }

        /// <summary>
        /// Applies all currently selected customization items to the player character.
        /// This is called on Awake and after loading new customizations.
        /// </summary>
        public void ApplyAllCustomizations()
        {
            foreach (var selection in _currentSelections)
            {
                ApplyCustomizationToSlot(selection.Key, selection.Value);
            }
        }

        /// <summary>
        /// Sets a specific customization option for a given category and applies it immediately.
        /// </summary>
        /// <param name="category">The category to customize (e.g., CustomizationCategory.Head).</param>
        /// <param name="item">The CustomizableItem to apply. Must belong to the specified category.</param>
        public void SetCustomizationOption(CustomizationCategory category, CustomizableItem item)
        {
            if (item == null)
            {
                Debug.LogWarning($"Attempted to set null item for category {category}. Clearing selection for this slot.");
                // Potentially reset to a default empty state or remove the current item
                if (_categoryToRendererMap.TryGetValue(category, out Renderer targetRenderer) && targetRenderer != null)
                {
                    if (targetRenderer is SkinnedMeshRenderer smr) smr.sharedMesh = null;
                    else if (targetRenderer is MeshRenderer mr) { if (mr.gameObject.TryGetComponent(out MeshFilter mf)) mf.sharedMesh = null; }
                }
                _currentSelections.Remove(category);
                return;
            }

            if (item.category != category)
            {
                Debug.LogWarning($"Item '{item.itemName}' (Category: {item.category}) does not match the target category '{category}'. Skipping application.");
                return;
            }

            _currentSelections[category] = item;
            ApplyCustomizationToSlot(category, item);
            SaveCustomizations(); // Save changes immediately
        }

        /// <summary>
        /// Sets a customization option by its index within a category.
        /// Useful for UI elements like "Next" / "Previous" buttons.
        /// </summary>
        /// <param name="category">The category to customize.</param>
        /// <param name="index">The index of the item within that category's available options.</param>
        public void SetCustomizationOptionByIndex(CustomizationCategory category, int index)
        {
            List<CustomizableItem> availableItems = customizationProfile.GetItemsByCategory(category);
            if (availableItems.Count == 0)
            {
                Debug.LogWarning($"No customizable items found for category: {category}.");
                return;
            }

            int safeIndex = index % availableItems.Count;
            if (safeIndex < 0) safeIndex += availableItems.Count; // Handle negative indices for looping backwards

            SetCustomizationOption(category, availableItems[safeIndex]);
        }

        /// <summary>
        /// Retrieves the currently selected item for a given category.
        /// </summary>
        /// <param name="category">The category to query.</param>
        /// <returns>The currently selected CustomizableItem, or null if nothing is selected for that category.</returns>
        public CustomizableItem GetCurrentSelection(CustomizationCategory category)
        {
            _currentSelections.TryGetValue(category, out CustomizableItem item);
            return item;
        }

        /// <summary>
        /// Retrieves the index of the currently selected item within its category's available options.
        /// </summary>
        /// <param name="category">The category to query.</param>
        /// <returns>The index of the current selection, or -1 if nothing is selected or no items exist.</returns>
        public int GetCurrentSelectionIndex(CustomizationCategory category)
        {
            CustomizableItem currentItem = GetCurrentSelection(category);
            if (currentItem == null) return -1;

            List<CustomizableItem> availableItems = customizationProfile.GetItemsByCategory(category);
            return availableItems.IndexOf(currentItem);
        }


        /// <summary>
        /// Internal method to apply a single customization item to its designated renderer.
        /// This is where the visual changes actually happen.
        /// </summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="item">The CustomizableItem to apply.</param>
        private void ApplyCustomizationToSlot(CustomizationCategory category, CustomizableItem item)
        {
            if (_categoryToRendererMap.TryGetValue(category, out Renderer targetRenderer))
            {
                if (targetRenderer != null)
                {
                    item.Apply(targetRenderer);
                }
                else
                {
                    Debug.LogWarning($"PlayerCustomizationManager: No renderer assigned for category {category}. Cannot apply item '{item.itemName}'.");
                }
            }
            else
            {
                Debug.LogWarning($"PlayerCustomizationManager: Category '{category}' is not mapped to any renderer.");
            }
        }

        /// <summary>
        /// Saves the current customization selections to PlayerPrefs.
        /// In a real game, you might use a more robust save system (JSON, binary, database).
        /// </summary>
        public void SaveCustomizations()
        {
            foreach (var selection in _currentSelections)
            {
                string key = $"Customization_{selection.Key}"; // e.g., "Customization_Head"
                PlayerPrefs.SetString(key, selection.Value.itemName);
            }
            PlayerPrefs.Save(); // Ensure changes are written to disk
            Debug.Log("Customizations saved.");
        }

        /// <summary>
        /// Loads customization selections from PlayerPrefs.
        /// If no saved data, it attempts to set the first available item in each category as default.
        /// </summary>
        public void LoadCustomizations()
        {
            _currentSelections.Clear(); // Clear previous selections before loading

            foreach (CustomizationCategory category in System.Enum.GetValues(typeof(CustomizationCategory)))
            {
                if (category == CustomizationCategory.None) continue; // Skip the 'None' category

                string key = $"Customization_{category}";
                string savedItemName = PlayerPrefs.GetString(key, string.Empty);

                CustomizableItem itemToApply = null;

                if (!string.IsNullOrEmpty(savedItemName))
                {
                    // Try to find the saved item
                    itemToApply = customizationProfile.GetItemByName(category, savedItemName);
                    if (itemToApply == null)
                    {
                        Debug.LogWarning($"PlayerCustomizationManager: Saved item '{savedItemName}' for category '{category}' not found in profile. Using default.");
                    }
                }

                if (itemToApply == null)
                {
                    // If no saved item, or saved item not found, try to apply the first available item as a default
                    itemToApply = customizationProfile.GetItemsByCategory(category).FirstOrDefault();
                }

                if (itemToApply != null)
                {
                    _currentSelections[category] = itemToApply;
                }
                else
                {
                    Debug.Log($"No default or saved item found for category {category}. Slot will remain empty.");
                }
            }
            Debug.Log("Customizations loaded.");
        }

        /// <summary>
        /// Clears all saved customizations from PlayerPrefs.
        /// </summary>
        public void ClearSavedCustomizations()
        {
            foreach (CustomizationCategory category in System.Enum.GetValues(typeof(CustomizationCategory)))
            {
                if (category == CustomizationCategory.None) continue;
                string key = $"Customization_{category}";
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.Save();
            Debug.Log("All saved customizations cleared.");
            // Reload to apply defaults or empty state after clearing
            LoadCustomizations();
            ApplyAllCustomizations();
        }


        // --- Example Usage (for testing/demonstration) ---
        // You might call these methods from UI buttons, game events, or other scripts.
        void Update()
        {
            // Example: Press 'H' to cycle through headgear
            if (Input.GetKeyDown(KeyCode.H))
            {
                CycleCustomization(CustomizationCategory.Head);
            }
            // Example: Press 'T' to cycle through torso armor
            if (Input.GetKeyDown(KeyCode.T))
            {
                CycleCustomization(CustomizationCategory.Torso);
            }
            // Example: Press 'W' to cycle through weapons
            if (Input.GetKeyDown(KeyCode.W))
            {
                CycleCustomization(CustomizationCategory.Weapon);
            }
            // Example: Press 'S' to cycle through skin colors
            if (Input.GetKeyDown(KeyCode.S))
            {
                CycleCustomization(CustomizationCategory.SkinColor);
            }
            // Example: Press 'L' to cycle through leg items
            if (Input.GetKeyDown(KeyCode.L))
            {
                CycleCustomization(CustomizationCategory.Legs);
            }

            // Example: Press 'R' to clear all saved customizations and reset to defaults
            if (Input.GetKeyDown(KeyCode.R))
            {
                ClearSavedCustomizations();
            }
        }

        /// <summary>
        /// Helper method to cycle through available items for a category.
        /// This would typically be triggered by UI buttons (e.g., "Next Hat").
        /// </summary>
        /// <param name="category">The category to cycle.</param>
        public void CycleCustomization(CustomizationCategory category)
        {
            List<CustomizableItem> items = customizationProfile.GetItemsByCategory(category);
            if (items.Count == 0)
            {
                Debug.LogWarning($"No items to cycle for category: {category}");
                return;
            }

            int currentIndex = GetCurrentSelectionIndex(category);
            int nextIndex = (currentIndex + 1) % items.Count;

            SetCustomizationOptionByIndex(category, nextIndex);
            Debug.Log($"Cycled {category}. New item: {GetCurrentSelection(category)?.itemName ?? "None"}");
        }
    }
}
```

---

## 3. How the Pattern Works (Detailed Comments & Explanation)

The comments within the code are quite thorough, but here's a summary of the pattern's benefits and design choices:

1.  **Decoupling with ScriptableObjects:**
    *   `CustomizableItem`, `MeshCustomizableItem`, `MaterialCustomizableItem`, and `PlayerCustomizationProfile` are all ScriptableObjects. This means customization data (which mesh, which material, icons, names) is **asset-based**, not hardcoded in scenes or MonoBehaviours.
    *   Assets can be created, modified, and managed in the Project window independently of the player character GameObject.
    *   The `PlayerCustomizationProfile` acts as a central registry, making it easy for the `Manager` to find all available options without knowing their specific file paths or types.

2.  **Extensibility:**
    *   **New Categories:** Simply add new entries to the `CustomizationCategory` enum and add a corresponding `Renderer` field (or `GameObject` field, etc.) in `PlayerCustomizationManager`. Update the `_categoryToRendererMap` and `ApplyCustomizationToSlot` logic.
    *   **New Item Types:** Create a new ScriptableObject class that inherits from `CustomizableItem` (e.g., `WeaponPrefabCustomizableItem`, `ParticleEffectCustomizableItem`). Implement its `Apply` method to handle its specific type of visual change. No changes are needed in `PlayerCustomizationManager` *unless* it requires a new type of `Renderer` or a different application strategy.
    *   **Adding new items:** Just create new instances of `MeshCustomizableItem` or `MaterialCustomizableItem` in the editor and drag them into the `PlayerCustomizationProfile`.

3.  **Centralized Management (`PlayerCustomizationManager`):**
    *   This single component on the player character handles *all* customization logic.
    *   It knows which items are currently selected (`_currentSelections`).
    *   It knows which physical `Renderer` on the character corresponds to each `CustomizationCategory` (`_categoryToRendererMap`).
    *   It provides a clear API (`SetCustomizationOption`, `SetCustomizationOptionByIndex`, `GetCurrentSelection`) for UI or other game systems to interact with it, without needing to know *how* the customization is applied.

4.  **Application Logic Encapsulation:**
    *   The `Apply(Renderer targetRenderer)` method within each `CustomizableItem` subclass encapsulates the specific logic for applying that item (e.g., `SkinnedMeshRenderer.sharedMesh = ...` for mesh items, `Renderer.sharedMaterials = ...` for material items).
    *   The `PlayerCustomizationManager` delegates the actual visual application to the item itself, promoting the **Strategy Pattern** principle: the manager uses a strategy (`item.Apply()`) without knowing its internal implementation.

5.  **Persistence:**
    *   The example uses `PlayerPrefs` for simplicity to save the `itemName` of selected items.
    *   This demonstrates how player choices can persist across game sessions. In a production game, you'd replace `PlayerPrefs` with a more robust JSON, binary, or database-backed save system.

## 4. Example Usage in Comments

The `PlayerCustomizationManager` includes an `Update()` method with example key presses (`H`, `T`, `W`, `S`, `L`, `R`) to demonstrate how you would interact with the system at runtime.

**How to integrate with UI (Conceptual):**

```csharp
// Example UI script (e.g., attached to a UI button "Next Helmet")
public class UIManager : MonoBehaviour
{
    public PlayerCustomizationManager playerCustomizationManager;
    public CustomizationCategory targetCategory; // Set this in Inspector (e.g., Head)

    void Start()
    {
        if (playerCustomizationManager == null)
        {
            Debug.LogError("PlayerCustomizationManager not assigned to UI Manager.");
        }
    }

    // Call this method when the "Next" button is clicked
    public void OnClickNextCustomization()
    {
        if (playerCustomizationManager != null)
        {
            playerCustomizationManager.CycleCustomization(targetCategory);
            // Optionally update a UI Text component to show the new item's name
            // UIManager.instance.UpdateItemNameText(playerCustomizationManager.GetCurrentSelection(targetCategory)?.itemName);
        }
    }

    // Call this method when the "Previous" button is clicked (requires a slight modification to CycleCustomization)
    public void OnClickPreviousCustomization()
    {
        if (playerCustomizationManager != null)
        {
            // You'd need a CycleCustomization(category, -1) or similar in the Manager
            int currentIndex = playerCustomizationManager.GetCurrentSelectionIndex(targetCategory);
            int totalItems = playerCustomizationManager.customizationProfile.GetItemsByCategory(targetCategory).Count;
            if (totalItems == 0) return;
            int prevIndex = (currentIndex - 1 + totalItems) % totalItems;
            playerCustomizationManager.SetCustomizationOptionByIndex(targetCategory, prevIndex);
        }
    }

    // Example to directly set an item (e.g., from a dropdown or specific button)
    public void SetSpecificItem(CustomizableItem item)
    {
        if (playerCustomizationManager != null && item != null)
        {
            playerCustomizationManager.SetCustomizationOption(item.category, item);
        }
    }
}
```

This complete system provides a flexible, extensible, and practical foundation for player customization in Unity, aligning with good design principles for game development.