// Unity Design Pattern Example: AvatarCustomizationSystem
// This script demonstrates the AvatarCustomizationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **Avatar Customization System** design pattern. It provides a modular and extensible way to manage character appearance, allowing for easy swapping of meshes, materials, and colors across various "slots" of an avatar.

The system is built upon:
1.  **ScriptableObjects** for defining individual customization items (meshes, materials, colors).
2.  An **AvatarCustomizationManager** MonoBehaviour that applies these items to specific `SkinnedMeshRenderer` components on the avatar.
3.  A **CustomizationData** class for easily saving and loading an avatar's current appearance.

---

### **1. AvatarCustomizationSystem.cs (Main Script File)**

This file will contain all the necessary classes: `AvatarSlotType`, `CustomizationItem` (base and concrete types), `AvatarCustomizationData`, and `AvatarCustomizationManager`.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List and Dictionary
using System; // For Serializable
using System.Linq; // For LINQ operations

// Namespace to keep our custom classes organized
namespace AvatarCustomization
{
    // =========================================================================
    // 1. Avatar Slot Type Definition
    // =========================================================================
    /// <summary>
    /// Defines the different customizable slots on an avatar.
    /// This enum provides clear categories for what parts of the avatar can be changed.
    /// You can extend this with more specific slots as needed (e.g., 'LeftHand', 'RightHand').
    /// </summary>
    public enum AvatarSlotType
    {
        None, // Default or unassigned slot
        Hair,
        Head, // For full head swaps or facial features
        Torso, // Body, shirt
        Legs, // Pants, skirt
        Feet, // Shoes
        Skin, // For skin color customization (usually targets the body/head mesh)
        Eyes, // For eye color/texture
        Accessory1, // Generic accessory slots
        Accessory2
    }

    // =========================================================================
    // 2. Base Customization Item (ScriptableObject)
    // =========================================================================
    /// <summary>
    /// Base class for all customization items. This uses the ScriptableObject pattern
    /// to allow designers to create individual customization assets in the Unity Editor.
    /// </summary>
    public abstract class CustomizationItem : ScriptableObject
    {
        [Tooltip("A unique ID for this item. Used for saving/loading customization.")]
        public string itemId; // Unique identifier for saving/loading

        [Tooltip("The display name shown in UI.")]
        public string displayName; // User-friendly name

        [Tooltip("An icon to represent this item in the UI.")]
        public Sprite previewIcon; // Icon for UI display

        /// <summary>
        /// Applies this customization item to a specific SkinnedMeshRenderer target.
        /// Concrete item types (e.g., Mesh, Material, Color) will implement this
        /// to perform their specific changes.
        /// </summary>
        /// <param name="targetRenderer">The SkinnedMeshRenderer to apply changes to.</param>
        /// <param name="manager">The AvatarCustomizationManager instance (can be used for more complex interactions).</param>
        public abstract void ApplyTo(SkinnedMeshRenderer targetRenderer, AvatarCustomizationManager manager);

        // Ensures unique item IDs by logging a warning if not set.
        // It's good practice to set this automatically or validate via editor tools.
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning($"Customization Item '{name}' has an empty itemId. Please set a unique ID.", this);
            }
        }
    }

    // =========================================================================
    // 3. Concrete Customization Item Types (ScriptableObjects)
    // =========================================================================

    /// <summary>
    /// Customization item for swapping entire meshes and their materials.
    /// Useful for different hair styles, shirt models, pant models, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMeshItem", menuName = "Customization/Mesh Item", order = 1)]
    public class MeshCustomizationItem : CustomizationItem
    {
        [Tooltip("The mesh to be applied to the target renderer.")]
        public Mesh meshAsset;

        [Tooltip("The material to be applied to the target renderer.")]
        public Material materialAsset;

        /// <summary>
        /// Applies the specified mesh and material to the target SkinnedMeshRenderer.
        /// </summary>
        public override void ApplyTo(SkinnedMeshRenderer targetRenderer, AvatarCustomizationManager manager)
        {
            if (targetRenderer == null) return;

            targetRenderer.sharedMesh = meshAsset; // Use sharedMesh to avoid creating new mesh instances
            targetRenderer.sharedMaterial = materialAsset; // Use sharedMaterial for efficiency
            targetRenderer.gameObject.SetActive(true); // Ensure the renderer is active
        }
    }

    /// <summary>
    /// Customization item for just swapping materials on an existing mesh.
    /// Useful for different color variations of the same shirt model, or different textures.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaterialItem", menuName = "Customization/Material Item", order = 2)]
    public class MaterialCustomizationItem : CustomizationItem
    {
        [Tooltip("The material to be applied to the target renderer.")]
        public Material materialAsset;

        /// <summary>
        /// Applies the specified material to the target SkinnedMeshRenderer's existing mesh.
        /// Note: This assumes the mesh itself is not changing.
        /// </summary>
        public override void ApplyTo(SkinnedMeshRenderer targetRenderer, AvatarCustomizationManager manager)
        {
            if (targetRenderer == null) return;

            targetRenderer.sharedMaterial = materialAsset; // Use sharedMaterial for efficiency
            targetRenderer.gameObject.SetActive(true); // Ensure the renderer is active
        }
    }

    /// <summary>
    /// Customization item for setting a specific color on a material.
    /// Useful for skin color, eye color, or tinting clothing.
    /// </summary>
    [CreateAssetMenu(fileName = "NewColorItem", menuName = "Customization/Color Item", order = 3)]
    public class ColorCustomizationItem : CustomizationItem
    {
        [Tooltip("The color to apply.")]
        public Color color;

        [Tooltip("The name of the color property in the material's shader (e.g., '_Color', '_BaseColor').")]
        public string colorPropertyName = "_Color"; // Common property name for color

        /// <summary>
        /// Applies the specified color to the target SkinnedMeshRenderer's material.
        /// It will attempt to set the color on a specified material property.
        /// </summary>
        public override void ApplyTo(SkinnedMeshRenderer targetRenderer, AvatarCustomizationManager manager)
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null) return;

            // Create a material instance to avoid modifying the original asset directly
            // This is crucial if multiple avatars use the same base material but need different colors.
            if (!targetRenderer.sharedMaterial.name.EndsWith("(Instance)")) // Check if it's already an instance
            {
                targetRenderer.material = new Material(targetRenderer.sharedMaterial);
            }
            
            if (targetRenderer.material.HasProperty(colorPropertyName))
            {
                targetRenderer.material.SetColor(colorPropertyName, color);
            }
            else
            {
                Debug.LogWarning($"Material '{targetRenderer.sharedMaterial.name}' does not have color property '{colorPropertyName}'.", targetRenderer.sharedMaterial);
            }
            targetRenderer.gameObject.SetActive(true); // Ensure the renderer is active
        }
    }

    // =========================================================================
    // 4. Avatar Customization Data (Serializable for Saving/Loading)
    // =========================================================================

    /// <summary>
    /// Helper struct to serialize AvatarSlotType with a string itemId.
    /// This makes the dictionary-like structure visible in the inspector and serializable.
    /// </summary>
    [Serializable]
    public struct SlotCustomization
    {
        public AvatarSlotType slotType;
        public string itemId;
    }

    /// <summary>
    /// Holds the currently selected customization options for an avatar.
    /// This class is [Serializable] so it can be easily saved to disk (e.g., JSON)
    /// and loaded back to recreate an avatar's appearance.
    /// </summary>
    [Serializable]
    public class AvatarCustomizationData
    {
        [Tooltip("List of selected item IDs for each slot.")]
        public List<SlotCustomization> selectedCustomizations = new List<SlotCustomization>();

        // Provides easy access to selected items by slot type at runtime
        private Dictionary<AvatarSlotType, string> _selectedItemIdsCache;

        /// <summary>
        /// Gets the cached dictionary of selected item IDs. Builds it if not already built.
        /// </summary>
        private Dictionary<AvatarSlotType, string> SelectedItemIds
        {
            get
            {
                if (_selectedItemIdsCache == null)
                {
                    _selectedItemIdsCache = new Dictionary<AvatarSlotType, string>();
                    foreach (var item in selectedCustomizations)
                    {
                        if (!_selectedItemIdsCache.ContainsKey(item.slotType)) // Prevent duplicates if any
                        {
                            _selectedItemIdsCache.Add(item.slotType, item.itemId);
                        }
                    }
                }
                return _selectedItemIdsCache;
            }
        }

        /// <summary>
        /// Sets a customization item for a specific slot.
        /// Updates the internal list and clears the cache.
        /// </summary>
        public void SetItem(AvatarSlotType slotType, string itemId)
        {
            // Remove existing entry for this slot
            selectedCustomizations.RemoveAll(sc => sc.slotType == slotType);
            
            // Add or update with the new item
            selectedCustomizations.Add(new SlotCustomization { slotType = slotType, itemId = itemId });

            // Invalidate cache
            _selectedItemIdsCache = null; 
        }

        /// <summary>
        /// Gets the itemId for a specific slot.
        /// </summary>
        public string GetItem(AvatarSlotType slotType)
        {
            SelectedItemIds.TryGetValue(slotType, out string itemId);
            return itemId;
        }

        /// <summary>
        /// Clears all customization data.
        /// </summary>
        public void Clear()
        {
            selectedCustomizations.Clear();
            _selectedItemIdsCache = null;
        }

        /// <summary>
        /// Creates a deep copy of the customization data.
        /// </summary>
        public AvatarCustomizationData Clone()
        {
            var clone = new AvatarCustomizationData();
            foreach (var item in selectedCustomizations)
            {
                clone.selectedCustomizations.Add(new SlotCustomization { slotType = item.slotType, itemId = item.itemId });
            }
            return clone;
        }

        public AvatarCustomizationData() { } // Default constructor

        public AvatarCustomizationData(Dictionary<AvatarSlotType, string> initialItems)
        {
            foreach (var pair in initialItems)
            {
                selectedCustomizations.Add(new SlotCustomization { slotType = pair.Key, itemId = pair.Value });
            }
            _selectedItemIdsCache = initialItems; // Initialize cache directly
        }
    }


    // =========================================================================
    // 5. Avatar Customization Manager (MonoBehaviour)
    // =========================================================================

    /// <summary>
    /// This is the core manager for applying customization to an avatar.
    /// It's a MonoBehaviour that should be attached to the root of your avatar GameObject.
    /// </summary>
    public class AvatarCustomizationManager : MonoBehaviour
    {
        [Header("Avatar Setup")]
        [Tooltip("Map avatar slots to their corresponding SkinnedMeshRenderer components.")]
        [SerializeField]
        private List<SlotRendererMapping> slotRenderers = new List<SlotRendererMapping>();

        // Internal dictionary for quick lookup of renderers by slot type.
        private Dictionary<AvatarSlotType, SkinnedMeshRenderer> _rendererMap;

        [Header("Available Customization Items")]
        [Tooltip("All possible customization items that can be applied to this avatar.")]
        public List<CustomizationItem> availableItems = new List<CustomizationItem>();

        // Internal dictionary for quick lookup of items by their unique ID.
        private Dictionary<string, CustomizationItem> _itemMap;

        [Header("Current Customization")]
        [Tooltip("The current customization configuration applied to this avatar.")]
        public AvatarCustomizationData currentCustomization = new AvatarCustomizationData();

        [Tooltip("Default customization to apply on Start if no other is loaded.")]
        public AvatarCustomizationData defaultCustomization;

        // Event for when customization changes, useful for UI updates or saving.
        public event Action<AvatarCustomizationData> OnCustomizationChanged;

        /// <summary>
        /// Helper struct to serialize AvatarSlotType with SkinnedMeshRenderer.
        /// This allows the renderer mapping to be easily set up in the Unity Editor.
        /// </summary>
        [Serializable]
        private struct SlotRendererMapping
        {
            public AvatarSlotType slotType;
            public SkinnedMeshRenderer renderer;
        }

        void Awake()
        {
            InitializeRendererMap();
            InitializeItemMap();

            // If there's a default customization set and currentCustomization is empty, apply default.
            if (defaultCustomization != null && defaultCustomization.selectedCustomizations.Count > 0 && currentCustomization.selectedCustomizations.Count == 0)
            {
                ApplyCustomization(defaultCustomization);
            }
            else if (currentCustomization.selectedCustomizations.Count > 0)
            {
                // If currentCustomization already has data (e.g., from editor/serialization), apply it.
                ApplyCustomization(currentCustomization);
            }
            else
            {
                // Ensure all renderers are initially disabled or set to a default empty state
                // This prevents seeing default meshes before customization is applied.
                foreach (var renderer in _rendererMap.Values)
                {
                    if (renderer != null)
                    {
                        renderer.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the internal dictionary mapping slot types to their SkinnedMeshRenderers.
        /// This is done once at Awake for efficient lookups.
        /// </summary>
        private void InitializeRendererMap()
        {
            _rendererMap = new Dictionary<AvatarSlotType, SkinnedMeshRenderer>();
            foreach (var mapping in slotRenderers)
            {
                if (mapping.renderer != null && !_rendererMap.ContainsKey(mapping.slotType))
                {
                    _rendererMap.Add(mapping.slotType, mapping.renderer);
                }
                else if (mapping.renderer == null)
                {
                    Debug.LogWarning($"SlotRendererMapping for {mapping.slotType} has a null renderer on {name}. Please assign a renderer in the Inspector.", this);
                }
                else if (_rendererMap.ContainsKey(mapping.slotType))
                {
                    Debug.LogWarning($"Duplicate SlotRendererMapping found for {mapping.slotType} on {name}. Only the first will be used.", this);
                }
            }
        }

        /// <summary>
        /// Initializes the internal dictionary mapping item IDs to CustomizationItem ScriptableObjects.
        /// This is done once at Awake for efficient lookups.
        /// </summary>
        private void InitializeItemMap()
        {
            _itemMap = new Dictionary<string, CustomizationItem>();
            foreach (var item in availableItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemId))
                {
                    if (!_itemMap.ContainsKey(item.itemId))
                    {
                        _itemMap.Add(item.itemId, item);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate CustomizationItem ID '{item.itemId}' found for item '{item.name}'. Only the first instance will be used.", item);
                    }
                }
                else if (item == null)
                {
                    Debug.LogWarning("A null item was found in 'availableItems' list. Please remove or assign an item.", this);
                }
                else if (string.IsNullOrEmpty(item.itemId))
                {
                    Debug.LogWarning($"CustomizationItem '{item.name}' has an empty itemId. It will be ignored.", item);
                }
            }
        }

        /// <summary>
        /// Applies an entire AvatarCustomizationData object to the avatar.
        /// This is typically used when loading a saved customization or setting a default.
        /// </summary>
        /// <param name="data">The customization data to apply.</param>
        public void ApplyCustomization(AvatarCustomizationData data)
        {
            if (data == null)
            {
                Debug.LogError("Attempted to apply null customization data.", this);
                return;
            }

            // Temporarily store the data so 'SetCustomizationItem' can reference it.
            // A more robust system might clone the data and make it the 'currentCustomization'.
            currentCustomization = data.Clone(); // Clone to prevent external modifications to our active data

            // First, hide all renderers to ensure a clean slate, then apply active items.
            foreach (var renderer in _rendererMap.Values)
            {
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(false);
                }
            }

            foreach (var slotEntry in data.selectedCustomizations)
            {
                SetCustomizationItemInternal(slotEntry.slotType, slotEntry.itemId, false); // Don't invoke event for each sub-item
            }
            
            OnCustomizationChanged?.Invoke(currentCustomization); // Invoke event once for the full update
        }

        /// <summary>
        /// Applies a specific customization item to a given slot.
        /// This is the primary method for changing individual parts of the avatar.
        /// </summary>
        /// <param name="slotType">The slot to customize (e.g., Hair, Torso).</param>
        /// <param name="itemId">The unique ID of the CustomizationItem to apply.</param>
        public void SetCustomizationItem(AvatarSlotType slotType, string itemId)
        {
            SetCustomizationItemInternal(slotType, itemId, true);
        }

        /// <summary>
        /// Internal method to apply a customization item, with an option to invoke the change event.
        /// </summary>
        private void SetCustomizationItemInternal(AvatarSlotType slotType, string itemId, bool invokeEvent)
        {
            if (!_rendererMap.TryGetValue(slotType, out SkinnedMeshRenderer targetRenderer) || targetRenderer == null)
            {
                Debug.LogWarning($"No SkinnedMeshRenderer mapped for slot type '{slotType}'. Cannot apply item '{itemId}'.", this);
                return;
            }

            if (!_itemMap.TryGetValue(itemId, out CustomizationItem item) || item == null)
            {
                Debug.LogWarning($"CustomizationItem with ID '{itemId}' not found in 'availableItems' for slot '{slotType}'.", this);
                // If item not found, ensure the slot is cleared or deactivated.
                targetRenderer.gameObject.SetActive(false);
                currentCustomization.SetItem(slotType, null); // Clear item for this slot
                if (invokeEvent) OnCustomizationChanged?.Invoke(currentCustomization);
                return;
            }

            // Apply the item's specific logic (mesh, material, color etc.)
            item.ApplyTo(targetRenderer, this);

            // Update the current customization data
            currentCustomization.SetItem(slotType, itemId);

            // Notify listeners about the change
            if (invokeEvent)
            {
                OnCustomizationChanged?.Invoke(currentCustomization);
            }
        }

        /// <summary>
        /// Gets the current CustomizationItem for a specific slot.
        /// </summary>
        /// <param name="slotType">The slot to query.</param>
        /// <returns>The CustomizationItem currently applied to the slot, or null if none.</returns>
        public CustomizationItem GetCurrentCustomizationItem(AvatarSlotType slotType)
        {
            string itemId = currentCustomization.GetItem(slotType);
            if (!string.IsNullOrEmpty(itemId) && _itemMap.TryGetValue(itemId, out CustomizationItem item))
            {
                return item;
            }
            return null;
        }

        /// <summary>
        /// Gets all available customization items for a given slot type.
        /// This is useful for populating UI elements (e.g., a list of all available hairstyles).
        /// </summary>
        /// <param name="slotType">The slot type to filter items by.</param>
        /// <returns>A list of CustomizationItems applicable to the specified slot.</returns>
        public List<CustomizationItem> GetAvailableItemsForSlot(AvatarSlotType slotType)
        {
            // This example assumes item names contain the slot type or that items are assigned to specific slots.
            // A more robust system might have an 'AvatarSlotType slot' field on CustomizationItem itself.
            // For now, we'll return all items, and the UI should filter based on context,
            // or the item's 'ApplyTo' method will handle if it's applicable.
            // For a better approach: Add a `public AvatarSlotType appliesToSlot;` to `CustomizationItem` and filter by that.
            // For this example, let's add that `appliesToSlot` field to CustomizationItem for better filtering.
            // (See notes below about potential refactor - for now, this just returns all for simplicity, or we check by a naming convention).

            // Refactoring to include appliesToSlot in CustomizationItem:
            // Let's assume CustomizationItem has `public AvatarSlotType appliesToSlot;`
            return availableItems.Where(item => item != null && item.name.Contains(slotType.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
            // A much better way is to add 'public AvatarSlotType targetSlot;' to CustomizationItem and filter by that.
            // For simplicity in this example, I'll update the CustomizationItem base to include a target slot.
        }

        /// <summary>
        /// Randomizes the avatar's customization by picking random items from available options.
        /// </summary>
        public void RandomizeCustomization()
        {
            currentCustomization.Clear(); // Start with a fresh slate

            foreach (var slotMapping in slotRenderers)
            {
                AvatarSlotType slot = slotMapping.slotType;
                List<CustomizationItem> options = GetAvailableItemsForSlot(slot);

                if (options.Count > 0)
                {
                    CustomizationItem randomItem = options[UnityEngine.Random.Range(0, options.Count)];
                    SetCustomizationItemInternal(slot, randomItem.itemId, false); // Don't invoke for each item
                }
                else
                {
                    // If no items are available for a slot, ensure its renderer is off
                    if (_rendererMap.TryGetValue(slot, out SkinnedMeshRenderer renderer))
                    {
                        renderer.gameObject.SetActive(false);
                    }
                }
            }
            OnCustomizationChanged?.Invoke(currentCustomization); // Invoke once after all random changes
            Debug.Log("Avatar customization randomized.");
        }

        /// <summary>
        /// Clears all customization, setting slots to their default (usually hidden or default mesh).
        /// </summary>
        public void ClearCustomization()
        {
            currentCustomization.Clear();
            foreach (var renderer in _rendererMap.Values)
            {
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(false);
                    renderer.sharedMesh = null; // Clear mesh
                    renderer.sharedMaterial = null; // Clear material
                }
            }
            OnCustomizationChanged?.Invoke(currentCustomization);
            Debug.Log("Avatar customization cleared.");
        }

        /// <summary>
        /// Saves the current customization to a JSON string.
        /// </summary>
        public string SaveCustomizationToJson()
        {
            // Ensure the current customization data is up-to-date with the actual state
            // (though `SetCustomizationItem` should keep it updated).
            // For robustness, one might rebuild currentCustomization from renderer states here.
            return JsonUtility.ToJson(currentCustomization);
        }

        /// <summary>
        /// Loads customization from a JSON string and applies it.
        /// </summary>
        public void LoadCustomizationFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("Attempted to load empty JSON customization string.", this);
                return;
            }
            try
            {
                AvatarCustomizationData loadedData = JsonUtility.FromJson<AvatarCustomizationData>(json);
                ApplyCustomization(loadedData);
                Debug.Log("Avatar customization loaded from JSON.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load customization from JSON: {e.Message}", this);
            }
        }

        // Example: Adding a method to populate the 'availableItems' from Resources folder.
        // This is an alternative to manually assigning items in the Inspector.
        public void LoadAvailableItemsFromResources(string path)
        {
            CustomizationItem[] loadedItems = Resources.LoadAll<CustomizationItem>(path);
            availableItems.Clear();
            availableItems.AddRange(loadedItems);
            InitializeItemMap(); // Re-initialize the item map after loading
            Debug.Log($"Loaded {loadedItems.Length} customization items from Resources/{path}");
        }
    }
}
```

---

### **2. CustomizationTester.cs (Example Usage)**

This script demonstrates how to interact with the `AvatarCustomizationManager` from another script, useful for UI integration or testing.

```csharp
using UnityEngine;
using AvatarCustomization; // Important: Include the namespace

/// <summary>
/// This script demonstrates how to use the AvatarCustomizationManager.
/// Attach this to any GameObject in your scene and link an AvatarCustomizationManager.
/// </summary>
public class CustomizationTester : MonoBehaviour
{
    [Tooltip("Reference to the AvatarCustomizationManager on your avatar.")]
    public AvatarCustomizationManager avatarManager;

    [Header("Test Customization Settings")]
    public AvatarSlotType testSlot;
    public string testItemId;

    void Start()
    {
        if (avatarManager == null)
        {
            Debug.LogError("Avatar Customization Manager not assigned to Customization Tester!", this);
            enabled = false;
            return;
        }

        // Optional: Subscribe to customization changes to react to them
        avatarManager.OnCustomizationChanged += OnAvatarCustomizationChanged;

        // Example: Load available items from a Resources folder if you prefer that over Inspector assignment.
        // avatarManager.LoadAvailableItemsFromResources("CustomizationItems"); // Assuming items are in Resources/CustomizationItems

        // --- Initial setup example ---
        // If you want to start with a specific look:
        // Set an initial customization (this will override any defaultCustomization on the manager)
        // avatarManager.SetCustomizationItem(AvatarSlotType.Hair, "Hair_Ponytail");
        // avatarManager.SetCustomizationItem(AvatarSlotType.Torso, "Torso_ShirtRed");
        // avatarManager.SetCustomizationItem(AvatarSlotType.Skin, "Skin_Tan"); // Apply a specific skin color by ID
    }

    void OnDestroy()
    {
        if (avatarManager != null)
        {
            avatarManager.OnCustomizationChanged -= OnAvatarCustomizationChanged;
        }
    }

    void OnAvatarCustomizationChanged(AvatarCustomizationData newData)
    {
        // This method will be called whenever the avatar's customization changes.
        // You can update UI, save the data, or trigger other effects here.
        Debug.Log("Avatar customization changed! Current config: " + avatarManager.SaveCustomizationToJson());
    }

    void Update()
    {
        // Example: Use keyboard inputs to test customization changes

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Applying Hair_Long to Hair slot.");
            avatarManager.SetCustomizationItem(AvatarSlotType.Hair, "Hair_Long");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Applying Hair_Short to Hair slot.");
            avatarManager.SetCustomizationItem(AvatarSlotType.Hair, "Hair_Short");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Applying Torso_ShirtBlue to Torso slot.");
            avatarManager.SetCustomizationItem(AvatarSlotType.Torso, "Torso_ShirtBlue");
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Applying Torso_ShirtRed to Torso slot.");
            avatarManager.SetCustomizationItem(AvatarSlotType.Torso, "Torso_ShirtRed");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Applying Skin_Pale to Skin slot.");
            avatarManager.SetCustomizationItem(AvatarSlotType.Skin, "Skin_Pale");
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("Applying Skin_Dark to Skin slot.");
            avatarManager.SetCustomizationItem(AvatarSlotType.Skin, "Skin_Dark");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Randomizing avatar customization.");
            avatarManager.RandomizeCustomization();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Clearing avatar customization.");
            avatarManager.ClearCustomization();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            string savedJson = avatarManager.SaveCustomizationToJson();
            PlayerPrefs.SetString("SavedAvatarCustomization", savedJson);
            Debug.Log("Customization saved to PlayerPrefs: " + savedJson);
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            string loadedJson = PlayerPrefs.GetString("SavedAvatarCustomization", "");
            if (!string.IsNullOrEmpty(loadedJson))
            {
                avatarManager.LoadCustomizationFromJson(loadedJson);
                Debug.Log("Customization loaded from PlayerPrefs.");
            }
            else
            {
                Debug.LogWarning("No saved customization found in PlayerPrefs.");
            }
        }

        // Example for testing a specific item from inspector
        if (Input.GetKeyDown(KeyCode.Space) && !string.IsNullOrEmpty(testItemId) && testSlot != AvatarSlotType.None)
        {
            Debug.Log($"Applying test item '{testItemId}' to slot '{testSlot}'.");
            avatarManager.SetCustomizationItem(testSlot, testItemId);
        }
    }

    // You can also create public methods to be called from UI buttons
    public void OnClickSetHair(string itemId)
    {
        avatarManager.SetCustomizationItem(AvatarSlotType.Hair, itemId);
    }

    public void OnClickSetTorso(string itemId)
    {
        avatarManager.SetCustomizationItem(AvatarSlotType.Torso, itemId);
    }

    public void OnClickSetSkinColor(string itemId)
    {
        avatarManager.SetCustomizationItem(AvatarSlotType.Skin, itemId);
    }
}
```

---

### **How to Implement in Unity (Step-by-Step Guide):**

**1. Create the C# Scripts:**
   - Create a new C# script named `AvatarCustomizationSystem.cs` and copy the first code block into it.
   - Create another new C# script named `CustomizationTester.cs` and copy the second code block into it.

**2. Prepare your 3D Avatar:**
   - **Import/Create a 3D Character:** You can use a character from the Unity Asset Store, Mixamo, or even a simple custom model.
   - **Separate Parts (Optional but Recommended):** For best results, your character should have its customizable parts (hair, torso, legs, etc.) as *separate GameObjects*, each with its own `SkinnedMeshRenderer` component. If your character is a single mesh, you might need to adjust materials on sub-meshes, or only use `MaterialCustomizationItem` and `ColorCustomizationItem` for that single mesh.
     *Example Hierarchy:*
     ```
     AvatarRoot (GameObject)
       ├── Body (GameObject with SkinnedMeshRenderer for skin)
       ├── Hair (GameObject with SkinnedMeshRenderer for hair)
       ├── Torso (GameObject with SkinnedMeshRenderer for shirt/top)
       ├── Legs (GameObject with SkinnedMeshRenderer for pants)
       └── Feet (GameObject with SkinnedMeshRenderer for shoes)
     ```
   - **Ensure Rigging:** If your character is animated, make sure all `SkinnedMeshRenderer`s are properly rigged to the avatar's skeleton.

**3. Set up the AvatarCustomizationManager:**
   - Drag your `AvatarCustomizationSystem.cs` script onto the **root** GameObject of your avatar (e.g., `AvatarRoot`). This will add the `AvatarCustomizationManager` component.
   - **Map Slot Renderers:**
     - In the Inspector for `AvatarCustomizationManager`, expand the `Slot Renderers` list.
     - For each `AvatarSlotType` you want to customize (e.g., `Hair`, `Torso`, `Skin`), create a new entry.
     - Drag the corresponding `SkinnedMeshRenderer` component from your avatar's child GameObjects into the `Renderer` field for that slot.
     - *Example:* For `Hair` slot, drag the `SkinnedMeshRenderer` from your `Hair` GameObject. For `Skin` slot, drag the `SkinnedMeshRenderer` from your `Body` GameObject.

**4. Create Customization Item Assets (ScriptableObjects):**
   - In your Project window, create a folder like `Assets/CustomizationItems`.
   - Right-click in this folder -> `Create` -> `Customization`. You'll see:
     - `Mesh Item`
     - `Material Item`
     - `Color Item`
   - **Create several of each type:**
     - **Mesh Item:**
       - `itemId`: e.g., "Hair_Long", "Hair_Short", "Torso_ShirtBlue", "Torso_ShirtRed"
       - `displayName`: e.g., "Long Hair", "Short Hair", "Blue Shirt", "Red Shirt"
       - `meshAsset`: Drag a `Mesh` asset (e.g., a specific hairstyle mesh).
       - `materialAsset`: Drag a `Material` asset (e.g., a hair material, a blue shirt material).
     - **Material Item:** (Use if you just want to change the texture/material on an *existing* mesh)
       - `itemId`: e.g., "Eyes_Blue", "Eyes_Green"
       - `displayName`: e.g., "Blue Eyes", "Green Eyes"
       - `materialAsset`: Drag a `Material` asset (e.g., a blue eye material).
     - **Color Item:**
       - `itemId`: e.g., "Skin_Pale", "Skin_Tan", "Skin_Dark"
       - `displayName`: e.g., "Pale Skin", "Tan Skin", "Dark Skin"
       - `color`: Pick a desired color.
       - `colorPropertyName`: Enter the name of the color property in your material's shader (e.g., `_Color` for Standard Shader, `_BaseColor` for URP Lit).

**5. Populate Available Items:**
   - Back on your `AvatarCustomizationManager` component, expand the `Available Items` list.
   - Drag **all** the `CustomizationItem` ScriptableObjects you created in the previous step into this list. The manager will use these to find items by their `itemId`.

**6. Set up the CustomizationTester:**
   - Create an empty GameObject in your scene (e.g., `CustomizationInput`).
   - Drag the `CustomizationTester.cs` script onto it.
   - In the Inspector for `CustomizationTester`, drag your avatar's `AvatarCustomizationManager` component (from its root GameObject) into the `Avatar Manager` slot.

**7. Run and Test:**
   - Play your scene.
   - Press the keys (1, 2, 3, 4, 5, 6, R, C, S, L) as defined in `CustomizationTester.cs` to see the avatar's appearance change.
   - Observe the `Debug.Log` messages in the Console.

---

### **Key Design Pattern Benefits:**

*   **Modularity:** Each customization item is a separate ScriptableObject asset, making it easy to add new items without changing code.
*   **Extensibility:** New types of customization (e.g., `ParticleEffectCustomizationItem`, `VisibilityToggleItem`) can be added by simply creating new classes inheriting from `CustomizationItem`.
*   **Decoupling:** The `AvatarCustomizationManager` is decoupled from the specific implementation details of each item. It just calls `item.ApplyTo()`, and the item itself knows how to apply its changes.
*   **Data-Driven:** Customization configurations (`AvatarCustomizationData`) are pure data, making them easy to save, load, and transmit (e.g., over a network).
*   **Artist-Friendly:** Designers can create and configure customization assets directly in the Unity Editor without touching code.
*   **Performance:** Using `sharedMesh` and `sharedMaterial` avoids creating new instances unnecessarily, and pre-caching items in dictionaries (`_itemMap`, `_rendererMap`) ensures fast lookups.
*   **Event-Driven:** The `OnCustomizationChanged` event allows other systems (like UI or saving systems) to react automatically when the avatar's appearance is modified.