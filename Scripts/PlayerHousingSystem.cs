// Unity Design Pattern Example: PlayerHousingSystem
// This script demonstrates the PlayerHousingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a robust, extensible **Player Housing System** for Unity, structured to be practical and educational. While "PlayerHousingSystem" isn't a formal design pattern like "Singleton" or "Observer," this implementation leverages several common patterns and best practices to create a maintainable and scalable module.

**Key Design Pattern Concepts Used:**

1.  **Singleton/Facade:** `PlayerHousingManager` acts as a central access point (Facade) for all housing-related operations, and is implemented as a Singleton for easy global access.
2.  **Scriptable Objects:** Used to define "types" of houses (`HouseTypeSO`) and "types" of placeable items (`PlaceableItemSO`). This promotes data-driven design, separates data from behavior, and allows designers to create new house/item assets easily without touching code.
3.  **Observer (via C# Events):** The `PlayerHousingManager` dispatches events (e.g., `OnPlotAcquired`, `OnItemPlaced`). Other systems (UI, analytics, player inventory) can subscribe to these events without direct coupling to the housing manager.
4.  **Data-Driven Design:** The system emphasizes separating runtime data (`PlacedItemData`, `HousingPlotSaveData`) from core logic, making persistence straightforward.
5.  **Component-Based (Unity Style):** `HousingPlot` is a `MonoBehaviour` component attached to physical house objects in the scene, encapsulating the behavior and state of an individual plot.

---

### **Project Setup in Unity:**

1.  Create a new Unity Project.
2.  Create a folder named `Scripts`.
3.  Create subfolders inside `Scripts`: `HousingSystem`, `ScriptableObjects`, `SaveData`.
4.  Create the following C# scripts in their respective folders:

    *   `Scripts/ScriptableObjects/HouseTypeSO.cs`
    *   `Scripts/ScriptableObjects/PlaceableItemSO.cs`
    *   `Scripts/SaveData/PlacedItemData.cs`
    *   `Scripts/SaveData/HousingPlotSaveData.cs`
    *   `Scripts/SaveData/PlayerHousingSaveData.cs`
    *   `Scripts/HousingSystem/HousingPlot.cs`
    *   `Scripts/HousingSystem/PlayerHousingManager.cs`

---

### **1. ScriptableObjects: Definitions of House Types and Items**

These define the *blueprints* or *archetypes* for houses and items.

#### **`HouseTypeSO.cs`**

```csharp
using UnityEngine;

/// <summary>
/// ScriptableObject defining a type of house.
/// This allows designers to create different house archetypes as assets.
/// </summary>
[CreateAssetMenu(fileName = "NewHouseType", menuName = "Housing System/House Type")]
public class HouseTypeSO : ScriptableObject
{
    [Tooltip("Unique identifier for this house type. Should match the asset name.")]
    public string houseTypeName; // Used for saving/loading, typically same as asset name
    [Tooltip("Display name for this house type.")]
    public string displayName;
    [Tooltip("Prefab associated with this house type (e.g., the visual model of the house).")]
    public GameObject housePrefab;
    [Tooltip("Max items that can be placed inside this house.")]
    public int maxItemCapacity = 10;
    [Tooltip("Cost to acquire this type of house.")]
    public int acquisitionCost = 1000;

    void OnValidate()
    {
        // Ensure the houseTypeName matches the asset name for consistency, especially important for saving/loading.
        if (string.IsNullOrEmpty(houseTypeName))
        {
            houseTypeName = name;
        }
    }
}
```

#### **`PlaceableItemSO.cs`**

```csharp
using UnityEngine;

/// <summary>
/// ScriptableObject defining a type of placeable item (e.g., furniture, decoration).
/// This allows designers to create different item archetypes as assets.
/// </summary>
[CreateAssetMenu(fileName = "NewPlaceableItem", menuName = "Housing System/Placeable Item")]
public class PlaceableItemSO : ScriptableObject
{
    [Tooltip("Unique identifier for this item type. Should match the asset name.")]
    public string itemTypeName; // Used for saving/loading, typically same as asset name
    [Tooltip("Display name for this item.")]
    public string displayName;
    [Tooltip("Prefab to instantiate when this item is placed.")]
    public GameObject itemPrefab;
    [Tooltip("Cost to purchase this item.")]
    public int purchaseCost = 50;

    void OnValidate()
    {
        // Ensure the itemTypeName matches the asset name for consistency, especially important for saving/loading.
        if (string.IsNullOrEmpty(itemTypeName))
        {
            itemTypeName = name;
        }
    }
}
```

---

### **2. Save Data Structures**

These plain C# classes define the structure of data that needs to be saved and loaded to persist the housing system's state. They are `[System.Serializable]` so Unity's JSON utility or other serializers can handle them.

#### **`PlacedItemData.cs`**

```csharp
using UnityEngine;
using System;

/// <summary>
/// Serializable data structure for a single item placed within a house.
/// Stores enough information to recreate the item later.
/// </summary>
[Serializable]
public class PlacedItemData
{
    public string itemTypeName; // Reference to the PlaceableItemSO by its name
    public Vector3 localPosition;
    public Quaternion localRotation;
    // Potentially add other item-specific data here (e.g., color, material, durability)

    public PlacedItemData(string typeName, Vector3 pos, Quaternion rot)
    {
        itemTypeName = typeName;
        localPosition = pos;
        localRotation = rot;
    }
}
```

#### **`HousingPlotSaveData.cs`**

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// Serializable data structure for a single housing plot.
/// Stores who owns it and what items are placed inside.
/// </summary>
[Serializable]
public class HousingPlotSaveData
{
    public string plotID; // Unique identifier for this specific plot instance in the game world
    public string ownerPlayerID; // ID of the player who owns this plot
    public string houseTypeName; // Reference to the HouseTypeSO by its name
    public List<PlacedItemData> placedItems; // List of items placed in this house

    public HousingPlotSaveData(string id, string ownerID, string houseType, List<PlacedItemData> items)
    {
        plotID = id;
        ownerPlayerID = ownerID;
        houseTypeName = houseType;
        placedItems = items ?? new List<PlacedItemData>(); // Ensure it's never null
    }

    public HousingPlotSaveData(HousingPlot plot)
    {
        plotID = plot.PlotID;
        ownerPlayerID = plot.CurrentOwnerID;
        houseTypeName = plot.HouseType.houseTypeName;

        placedItems = new List<PlacedItemData>();
        foreach (Transform child in plot.transform)
        {
            // Assuming placed items are children of the HousingPlot's GameObject
            // and we can identify them as instances of PlaceableItemSO prefabs.
            // In a real scenario, you might have a dedicated PlacedItem MonoBehaviour to get this data.
            PlacedItem instance = child.GetComponent<PlacedItem>();
            if (instance != null)
            {
                placedItems.Add(new PlacedItemData(instance.ItemType.itemTypeName, child.localPosition, child.localRotation));
            }
        }
    }
}
```

#### **`PlayerHousingSaveData.cs`**

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// The top-level save data structure for the entire player housing system.
/// Contains a dictionary of all housing plots in the game, mapped by their unique ID.
/// </summary>
[Serializable]
public class PlayerHousingSaveData
{
    // Dictionary is not directly serializable by Unity's JsonUtility,
    // so we use a list of wrapper objects or separate keys/values lists.
    // For simplicity with JsonUtility, we'll use a list and convert to dictionary on load.
    public List<HousingPlotSaveData> allPlots;

    public PlayerHousingSaveData()
    {
        allPlots = new List<HousingPlotSaveData>();
    }
}
```

---

### **3. Runtime Components: The Building Blocks in the Scene**

These `MonoBehaviour` scripts are attached to GameObjects in your scene.

#### **`PlacedItem.cs`**
This script will be attached to the actual GameObject of an item once it's placed. It helps identify it and link it back to its `PlaceableItemSO`.

```csharp
using UnityEngine;

/// <summary>
/// Component attached to an item GameObject that has been placed inside a house.
/// Links the placed instance back to its ScriptableObject definition.
/// </summary>
public class PlacedItem : MonoBehaviour
{
    [Tooltip("The ScriptableObject definition for this item type.")]
    public PlaceableItemSO ItemType { get; private set; }

    /// <summary>
    /// Initializes the placed item with its ScriptableObject definition.
    /// </summary>
    /// <param name="itemType">The PlaceableItemSO instance.</param>
    public void Initialize(PlaceableItemSO itemType)
    {
        ItemType = itemType;
        // Optionally apply visuals/materials based on itemType here if not handled by prefab
    }
}
```

#### **`HousingPlot.cs`**

This `MonoBehaviour` is placed on a GameObject that represents a housing plot in the scene.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MonoBehaviour representing an individual housing plot in the game world.
/// Manages its state (owner, placed items) and provides interaction points.
/// </summary>
public class HousingPlot : MonoBehaviour
{
    [Header("Plot Configuration")]
    [Tooltip("Unique identifier for this specific plot. Must be unique across all plots in the game.")]
    [SerializeField] private string plotID;
    public string PlotID => plotID;

    [Tooltip("The ScriptableObject defining the type of house this plot represents.")]
    [SerializeField] private HouseTypeSO houseType;
    public HouseTypeSO HouseType => houseType;

    [Header("Runtime Data")]
    [Tooltip("Current owner's player ID. Null or empty if unowned.")]
    [SerializeField] private string currentOwnerID;
    public string CurrentOwnerID => currentOwnerID;
    public bool IsOwned => !string.IsNullOrEmpty(currentOwnerID);

    // Dictionary to hold references to actual placed item GameObjects
    // Key could be unique ID of the placed item, or a simple index for this example.
    private List<PlacedItem> placedItems = new List<PlacedItem>();
    public List<PlacedItem> PlacedItems => new List<PlacedItem>(placedItems); // Return a copy

    private void Awake()
    {
        if (string.IsNullOrEmpty(plotID))
        {
            Debug.LogError($"HousingPlot on '{gameObject.name}' has no Plot ID. Please assign a unique ID.", this);
            #if UNITY_EDITOR
            // Generate a GUID in editor if missing for convenience
            plotID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this); // Mark as dirty to save change
            #endif
        }
        if (houseType == null)
        {
            Debug.LogError($"HousingPlot '{plotID}' on '{gameObject.name}' has no HouseTypeSO assigned!", this);
        }
    }

    /// <summary>
    /// Initializes the plot with save data. Should be called by PlayerHousingManager.
    /// </summary>
    /// <param name="saveData">The saved data for this plot.</param>
    /// <param name="allItemTypes">A dictionary of all available PlaceableItemSOs for lookup.</param>
    public void LoadPlotState(HousingPlotSaveData saveData, Dictionary<string, PlaceableItemSO> allItemTypes)
    {
        if (saveData == null) return;

        currentOwnerID = saveData.ownerPlayerID;

        // Clear existing items if any (e.g., from scene)
        foreach (var item in placedItems)
        {
            Destroy(item.gameObject);
        }
        placedItems.Clear();

        // Instantiate saved items
        foreach (var itemData in saveData.placedItems)
        {
            if (allItemTypes.TryGetValue(itemData.itemTypeName, out PlaceableItemSO itemSO))
            {
                if (itemSO.itemPrefab != null)
                {
                    GameObject itemGO = Instantiate(itemSO.itemPrefab, transform);
                    itemGO.transform.localPosition = itemData.localPosition;
                    itemGO.transform.localRotation = itemData.localRotation;

                    PlacedItem placedItemComponent = itemGO.GetComponent<PlacedItem>();
                    if (placedItemComponent == null)
                    {
                        placedItemComponent = itemGO.AddComponent<PlacedItem>();
                    }
                    placedItemComponent.Initialize(itemSO);
                    placedItems.Add(placedItemComponent);
                }
                else
                {
                    Debug.LogWarning($"Prefab for item type '{itemData.itemTypeName}' is missing. Cannot instantiate.");
                }
            }
            else
            {
                Debug.LogWarning($"PlaceableItemSO '{itemData.itemTypeName}' not found during load for plot '{plotID}'.");
            }
        }

        // Inform manager that this plot has been loaded (if manager needs to do something)
        PlayerHousingManager.Instance.NotifyPlotLoaded(this);
    }

    /// <summary>
    /// Assigns an owner to this plot.
    /// </summary>
    /// <param name="playerID">The ID of the player acquiring the plot.</param>
    public void AcquirePlot(string playerID)
    {
        if (IsOwned)
        {
            Debug.LogWarning($"Plot {plotID} is already owned by {currentOwnerID}. Cannot acquire for {playerID}.");
            return;
        }

        currentOwnerID = playerID;
        Debug.Log($"Plot {plotID} acquired by player {playerID}.");
        // Trigger visual changes, events etc.
    }

    /// <summary>
    /// Releases ownership of this plot.
    /// </summary>
    public void ReleasePlot()
    {
        if (!IsOwned)
        {
            Debug.LogWarning($"Plot {plotID} is not owned. Cannot release.");
            return;
        }

        string previousOwner = currentOwnerID;
        currentOwnerID = null;
        Debug.Log($"Plot {plotID} released by player {previousOwner}.");

        // Clear all placed items when releasing a plot
        foreach (var item in placedItems)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        placedItems.Clear();

        // Trigger visual changes, events etc.
    }

    /// <summary>
    /// Places an item inside this house.
    /// </summary>
    /// <param name="itemType">The type of item to place.</param>
    /// <param name="position">Local position relative to the house's transform.</param>
    /// <param name="rotation">Local rotation relative to the house's transform.</param>
    /// <returns>The GameObject of the placed item, or null if placement failed.</returns>
    public GameObject PlaceItem(PlaceableItemSO itemType, Vector3 position, Quaternion rotation)
    {
        if (itemType == null || itemType.itemPrefab == null)
        {
            Debug.LogError($"Cannot place item: ItemType or its prefab is null for plot {plotID}.");
            return null;
        }

        if (!IsOwned)
        {
            Debug.LogWarning($"Plot {plotID} is not owned. Cannot place items.");
            return null;
        }

        if (placedItems.Count >= houseType.maxItemCapacity)
        {
            Debug.LogWarning($"Plot {plotID} has reached its max item capacity ({houseType.maxItemCapacity}). Cannot place more items.");
            return null;
        }

        // Instantiate the item as a child of the housing plot
        GameObject itemGO = Instantiate(itemType.itemPrefab, transform);
        itemGO.transform.localPosition = position;
        itemGO.transform.localRotation = rotation;

        PlacedItem placedItemComponent = itemGO.GetComponent<PlacedItem>();
        if (placedItemComponent == null)
        {
            placedItemComponent = itemGO.AddComponent<PlacedItem>();
        }
        placedItemComponent.Initialize(itemType);
        placedItems.Add(placedItemComponent);

        Debug.Log($"Item '{itemType.displayName}' placed in plot {plotID}.");
        return itemGO;
    }

    /// <summary>
    /// Removes a specific placed item from the house.
    /// </summary>
    /// <param name="itemToRemove">The PlacedItem component of the item to remove.</param>
    /// <returns>True if item was removed, false otherwise.</returns>
    public bool RemoveItem(PlacedItem itemToRemove)
    {
        if (itemToRemove == null) return false;

        if (placedItems.Remove(itemToRemove))
        {
            Destroy(itemToRemove.gameObject);
            Debug.Log($"Item '{itemToRemove.ItemType.displayName}' removed from plot {plotID}.");
            return true;
        }
        Debug.LogWarning($"Attempted to remove item not found in plot {plotID}.");
        return false;
    }

    /// <summary>
    /// Removes an item by its GameObject reference.
    /// </summary>
    /// <param name="itemGameObject">The GameObject of the item to remove.</param>
    /// <returns>True if item was removed, false otherwise.</returns>
    public bool RemoveItem(GameObject itemGameObject)
    {
        if (itemGameObject == null) return false;
        PlacedItem itemToRemove = itemGameObject.GetComponent<PlacedItem>();
        if (itemToRemove != null)
        {
            return RemoveItem(itemToRemove);
        }
        Debug.LogWarning($"GameObject '{itemGameObject.name}' is not a valid PlacedItem in plot {plotID}.");
        return false;
    }

    // You might add methods here for entering/exiting house, triggering UI, etc.
}
```

---

### **4. The Manager: PlayerHousingManager**

This is the central hub for the entire housing system. It uses the Singleton pattern to be globally accessible.

#### **`PlayerHousingManager.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// The central manager for the Player Housing System.
/// Implemented as a Singleton for easy global access.
/// Manages all housing plots, player ownership, item placement, and data persistence.
/// </summary>
public class PlayerHousingManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    private static PlayerHousingManager _instance;
    public static PlayerHousingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerHousingManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("PlayerHousingManager");
                    _instance = obj.AddComponent<PlayerHousingManager>();
                    Debug.Log("Created new PlayerHousingManager GameObject.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Duplicate PlayerHousingManager found, destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Keep the manager alive across scene changes
    }

    // --- Events (Observer Pattern) ---
    public event Action<HousingPlot, string> OnPlotAcquired;
    public event Action<HousingPlot, string> OnPlotReleased;
    public event Action<HousingPlot, PlacedItemSO, GameObject> OnItemPlaced;
    public event Action<HousingPlot, PlacedItemSO, GameObject> OnItemRemoved;
    public event Action OnHousingDataLoaded;
    public event Action OnHousingDataSaved;

    // --- Internal State ---
    [Tooltip("List of all HouseType ScriptableObjects in the project.")]
    [SerializeField] private List<HouseTypeSO> allHouseTypes;
    private Dictionary<string, HouseTypeSO> houseTypeMap = new Dictionary<string, HouseTypeSO>();

    [Tooltip("List of all PlaceableItem ScriptableObjects in the project.")]
    [SerializeField] private List<PlaceableItemSO> allPlaceableItems;
    private Dictionary<string, PlaceableItemSO> itemTypeMap = new Dictionary<string, PlaceableItemSO>();

    // Dictionary to hold all active HousingPlot MonoBehaviours in the current scene/game
    private Dictionary<string, HousingPlot> allPlotsInScene = new Dictionary<string, HousingPlot>();

    // --- Initialization ---
    private void Start()
    {
        InitializeAssetMaps();
        // In a real game, you would load housing data here based on the player's saved game.
        // For this example, we'll demonstrate loading explicitly.
    }

    private void OnEnable()
    {
        // Register all currently existing HousingPlots in the scene
        // This is important if plots are pre-placed in a scene.
        RegisterAllPlotsInScene();
    }

    private void InitializeAssetMaps()
    {
        houseTypeMap.Clear();
        foreach (var type in allHouseTypes)
        {
            if (type != null && !houseTypeMap.ContainsKey(type.houseTypeName))
            {
                houseTypeMap.Add(type.houseTypeName, type);
            }
            else if (type != null)
            {
                Debug.LogWarning($"Duplicate HouseTypeSO '{type.houseTypeName}' found in 'allHouseTypes' list.", type);
            }
        }

        itemTypeMap.Clear();
        foreach (var item in allPlaceableItems)
        {
            if (item != null && !itemTypeMap.ContainsKey(item.itemTypeName))
            {
                itemTypeMap.Add(item.itemTypeName, item);
            }
            else if (item != null)
            {
                Debug.LogWarning($"Duplicate PlaceableItemSO '{item.itemTypeName}' found in 'allPlaceableItems' list.", item);
            }
        }
    }

    /// <summary>
    /// Finds and registers all HousingPlot components currently present in the scene.
    /// This should be called when scenes load or when plots are dynamically spawned.
    /// </summary>
    public void RegisterAllPlotsInScene()
    {
        HousingPlot[] foundPlots = FindObjectsOfType<HousingPlot>(true); // include inactive
        allPlotsInScene.Clear(); // Clear existing to prevent duplicates if called multiple times on scene load
        foreach (var plot in foundPlots)
        {
            if (allPlotsInScene.ContainsKey(plot.PlotID))
            {
                Debug.LogError($"Duplicate HousingPlot ID '{plot.PlotID}' found in scene! This will cause issues.", plot);
            }
            else
            {
                allPlotsInScene.Add(plot.PlotID, plot);
                Debug.Log($"Registered HousingPlot: {plot.PlotID} (Owner: {plot.CurrentOwnerID})");
            }
        }
    }

    /// <summary>
    /// Called internally by HousingPlot after it has loaded its state.
    /// </summary>
    /// <param name="plot">The HousingPlot instance that just finished loading.</param>
    public void NotifyPlotLoaded(HousingPlot plot)
    {
        // Optional: Perform any manager-level actions after a plot has loaded.
        // E.g., check for inconsistencies, update UI that relies on plot data.
    }

    // --- Public API for Housing System Operations ---

    /// <summary>
    /// Acquires a specific housing plot for a player.
    /// </summary>
    /// <param name="plotID">The unique ID of the plot to acquire.</param>
    /// <param name="playerID">The ID of the player acquiring the plot.</param>
    /// <returns>True if acquisition was successful, false otherwise.</returns>
    public bool AcquirePlot(string plotID, string playerID)
    {
        if (string.IsNullOrEmpty(playerID))
        {
            Debug.LogError("Player ID cannot be null or empty when acquiring a plot.");
            return false;
        }

        if (allPlotsInScene.TryGetValue(plotID, out HousingPlot plot))
        {
            if (plot.IsOwned)
            {
                Debug.LogWarning($"Plot '{plotID}' is already owned by '{plot.CurrentOwnerID}'. Cannot acquire for '{playerID}'.");
                return false;
            }

            plot.AcquirePlot(playerID);
            OnPlotAcquired?.Invoke(plot, playerID); // Notify subscribers
            return true;
        }
        Debug.LogError($"Plot with ID '{plotID}' not found in scene.");
        return false;
    }

    /// <summary>
    /// Releases a housing plot, making it available again.
    /// </summary>
    /// <param name="plotID">The unique ID of the plot to release.</param>
    /// <returns>True if release was successful, false otherwise.</returns>
    public bool ReleasePlot(string plotID)
    {
        if (allPlotsInScene.TryGetValue(plotID, out HousingPlot plot))
        {
            if (!plot.IsOwned)
            {
                Debug.LogWarning($"Plot '{plotID}' is not owned. Cannot release.");
                return false;
            }

            string previousOwner = plot.CurrentOwnerID;
            plot.ReleasePlot();
            OnPlotReleased?.Invoke(plot, previousOwner); // Notify subscribers
            return true;
        }
        Debug.LogError($"Plot with ID '{plotID}' not found in scene.");
        return false;
    }

    /// <summary>
    /// Places an item into a specified housing plot.
    /// </summary>
    /// <param name="plotID">The unique ID of the plot.</param>
    /// <param name="itemTypeName">The type name (ID) of the item to place.</param>
    /// <param name="localPosition">Local position within the plot.</param>
    /// <param name="localRotation">Local rotation within the plot.</param>
    /// <returns>The GameObject of the placed item, or null if failed.</returns>
    public GameObject PlaceItemInPlot(string plotID, string itemTypeName, Vector3 localPosition, Quaternion localRotation)
    {
        if (allPlotsInScene.TryGetValue(plotID, out HousingPlot plot))
        {
            if (!plot.IsOwned)
            {
                Debug.LogWarning($"Plot '{plotID}' is not owned. Cannot place items.");
                return null;
            }

            if (itemTypeMap.TryGetValue(itemTypeName, out PlaceableItemSO itemType))
            {
                GameObject placedItemGO = plot.PlaceItem(itemType, localPosition, localRotation);
                if (placedItemGO != null)
                {
                    OnItemPlaced?.Invoke(plot, itemType, placedItemGO); // Notify subscribers
                    return placedItemGO;
                }
            }
            else
            {
                Debug.LogError($"Item type '{itemTypeName}' not found.");
            }
        }
        else
        {
            Debug.LogError($"Plot with ID '{plotID}' not found in scene.");
        }
        return null;
    }

    /// <summary>
    /// Removes a specific placed item from a housing plot.
    /// </summary>
    /// <param name="plotID">The unique ID of the plot.</param>
    /// <param name="itemGameObject">The GameObject of the item to remove.</param>
    /// <returns>True if removal was successful, false otherwise.</returns>
    public bool RemoveItemFromPlot(string plotID, GameObject itemGameObject)
    {
        if (itemGameObject == null) return false;

        if (allPlotsInScene.TryGetValue(plotID, out HousingPlot plot))
        {
            PlacedItem placedItemComponent = itemGameObject.GetComponent<PlacedItem>();
            if (placedItemComponent == null)
            {
                Debug.LogError($"GameObject '{itemGameObject.name}' is not a valid PlacedItem.");
                return false;
            }

            if (plot.RemoveItem(placedItemComponent))
            {
                OnItemRemoved?.Invoke(plot, placedItemComponent.ItemType, itemGameObject); // Notify subscribers
                return true;
            }
        }
        else
        {
            Debug.LogError($"Plot with ID '{plotID}' not found in scene.");
        }
        return false;
    }

    /// <summary>
    /// Gets a HousingPlot by its ID.
    /// </summary>
    /// <param name="plotID">The unique ID of the plot.</param>
    /// <returns>The HousingPlot MonoBehaviour, or null if not found.</returns>
    public HousingPlot GetPlotByID(string plotID)
    {
        allPlotsInScene.TryGetValue(plotID, out HousingPlot plot);
        return plot;
    }

    /// <summary>
    /// Gets all plots currently owned by a specific player.
    /// </summary>
    /// <param name="playerID">The ID of the player.</param>
    /// <returns>A list of HousingPlot instances owned by the player.</returns>
    public List<HousingPlot> GetPlotsOwnedByPlayer(string playerID)
    {
        return allPlotsInScene.Values.Where(p => p.CurrentOwnerID == playerID).ToList();
    }

    // --- Persistence (Load/Save) ---

    /// <summary>
    /// Loads housing data from a PlayerHousingSaveData object and applies it to the scene.
    /// </summary>
    /// <param name="saveData">The PlayerHousingSaveData object containing all saved plot data.</param>
    public void LoadHousingData(PlayerHousingSaveData saveData)
    {
        if (saveData == null || saveData.allPlots == null)
        {
            Debug.Log("No housing save data to load or data is empty.");
            return;
        }

        // First, ensure all plots in the scene are registered
        RegisterAllPlotsInScene();

        // Clear ownership/items of any existing plots in scene that are not in save data,
        // or just reset them before applying save data.
        foreach (var plot in allPlotsInScene.Values)
        {
            if (!saveData.allPlots.Any(p => p.plotID == plot.PlotID))
            {
                // If a plot exists in scene but not in save data, ensure it's unowned and empty.
                if (plot.IsOwned) plot.ReleasePlot(); // This also clears items
            }
        }

        foreach (var plotData in saveData.allPlots)
        {
            if (allPlotsInScene.TryGetValue(plotData.plotID, out HousingPlot plotInstance))
            {
                // Load the state into the existing plot instance
                plotInstance.LoadPlotState(plotData, itemTypeMap);
                Debug.Log($"Loaded state for plot: {plotData.plotID}");
            }
            else
            {
                Debug.LogWarning($"Plot with ID '{plotData.plotID}' found in save data but not in current scene. Skipping.", this);
                // Optionally, instantiate a new plot GameObject if it's dynamic
                // This would require storing prefab reference and position/rotation in HousingPlotSaveData
            }
        }
        OnHousingDataLoaded?.Invoke();
        Debug.Log("Housing data loaded successfully.");
    }

    /// <summary>
    /// Gathers all current housing data into a PlayerHousingSaveData object.
    /// </summary>
    /// <returns>A PlayerHousingSaveData object ready for serialization.</returns>
    public PlayerHousingSaveData GetCurrentHousingData()
    {
        PlayerHousingSaveData saveData = new PlayerHousingSaveData();
        foreach (var plot in allPlotsInScene.Values)
        {
            if (plot.IsOwned) // Only save owned plots and their contents
            {
                saveData.allPlots.Add(new HousingPlotSaveData(plot));
            }
            // Optionally, save unowned plots too if they have some inherent state (e.g. damaged)
        }
        OnHousingDataSaved?.Invoke();
        Debug.Log("Housing data gathered for saving.");
        return saveData;
    }

    /// <summary>
    /// Helper method to get a HouseTypeSO by its name.
    /// </summary>
    public HouseTypeSO GetHouseType(string typeName)
    {
        houseTypeMap.TryGetValue(typeName, out HouseTypeSO type);
        return type;
    }

    /// <summary>
    /// Helper method to get a PlaceableItemSO by its name.
    /// </summary>
    public PlaceableItemSO GetItemType(string typeName)
    {
        itemTypeMap.TryGetValue(typeName, out PlaceableItemSO type);
        return type;
    }
}
```

---

### **Example Usage in a Unity Scene and a Test Script**

To make this immediately runnable, you'll need to create some assets and scene objects.

#### **1. Create ScriptableObject Assets:**

*   In your Unity Project window, navigate to `Assets/Scripts/ScriptableObjects`.
*   Right-click -> Create -> Housing System -> House Type. Name it `SmallHouse`.
    *   Assign `SmallHouse` to its `House Type Name` (auto-filled by `OnValidate`).
    *   Set `Display Name` to "Small Cottage".
    *   Leave `House Prefab` empty for now, or create a simple cube prefab.
    *   Set `Max Item Capacity` to 5.
    *   Set `Acquisition Cost` to 100.
*   Right-click -> Create -> Housing System -> Placeable Item. Name it `WoodenChair`.
    *   Assign `WoodenChair` to its `Item Type Name`.
    *   Set `Display Name` to "Wooden Chair".
    *   Leave `Item Prefab` empty or create a small cube prefab.
    *   Set `Purchase Cost` to 20.
*   Repeat for another item: `DiningTable`.
    *   Assign `DiningTable` to `Item Type Name`.
    *   Set `Display Name` to "Dining Table".
    *   Create a simple cube prefab (larger than the chair).
    *   Set `Purchase Cost` to 50.

#### **2. Create Prefabs:**

*   Create simple 3D Cube GameObjects in your scene.
    *   Rename one to `House_Prefab_Small`. Drag it to `Assets/Prefabs`. Delete from scene.
    *   Rename one to `Item_Prefab_Chair`. Drag it to `Assets/Prefabs`. Delete from scene.
    *   Rename one to `Item_Prefab_Table`. Drag it to `Assets/Prefabs`. Delete from scene.
*   Go back to your `HouseTypeSO` and `PlaceableItemSO` assets and assign these prefabs to their respective `housePrefab` or `itemPrefab` fields.

#### **3. Set up the Scene:**

*   Create an empty GameObject in your scene named `HousingManager`.
*   Attach the `PlayerHousingManager.cs` script to it.
*   In the Inspector of `HousingManager`:
    *   Drag your `SmallHouse` `HouseTypeSO` asset into the `All House Types` list.
    *   Drag your `WoodenChair` and `DiningTable` `PlaceableItemSO` assets into the `All Placeable Items` list.
*   Create an empty GameObject in your scene named `HousingPlot_1`.
    *   Position it at (0, 0, 0).
    *   Attach the `HousingPlot.cs` script to it.
    *   In its Inspector:
        *   Set `Plot ID` to "Plot_Alpha". **This must be unique!**
        *   Drag your `SmallHouse` `HouseTypeSO` asset into the `House Type` field.
*   Create another empty GameObject named `HousingPlot_2`.
    *   Position it at (10, 0, 0).
    *   Attach the `HousingPlot.cs` script.
    *   Set `Plot ID` to "Plot_Beta".
    *   Drag your `SmallHouse` `HouseTypeSO` asset into the `House Type` field.

#### **4. Create a Test Script (e.g., `HousingTestClient.cs`)**

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class HousingTestClient : MonoBehaviour
{
    public string playerID_A = "Player_001";
    public string playerID_B = "Player_002";

    private HousingPlot plotAlpha;
    private HousingPlot plotBeta;

    private const string SaveFileName = "playerHousingSave.json";

    void Start()
    {
        // Subscribe to events (demonstrates Observer pattern usage)
        PlayerHousingManager.Instance.OnPlotAcquired += HandlePlotAcquired;
        PlayerHousingManager.Instance.OnPlotReleased += HandlePlotReleased;
        PlayerHousingManager.Instance.OnItemPlaced += HandleItemPlaced;
        PlayerHousingManager.Instance.OnItemRemoved += HandleItemRemoved;
        PlayerHousingManager.Instance.OnHousingDataLoaded += HandleHousingDataLoaded;
        PlayerHousingManager.Instance.OnHousingDataSaved += HandleHousingDataSaved;

        // Get references to plots (the manager should have registered them)
        plotAlpha = PlayerHousingManager.Instance.GetPlotByID("Plot_Alpha");
        plotBeta = PlayerHousingManager.Instance.GetPlotByID("Plot_Beta");

        if (plotAlpha == null || plotBeta == null)
        {
            Debug.LogError("Housing plots not found. Ensure Plot_Alpha and Plot_Beta exist and have HousingPlot component with correct IDs.");
            return;
        }

        Debug.Log("Housing Test Client initialized. Press keys for actions:");
        Debug.Log("L: Load saved data (if any)");
        Debug.Log("S: Save current housing data");
        Debug.Log("A: Player A acquires Plot Alpha");
        Debug.Log("R: Player A releases Plot Alpha");
        Debug.Log("P: Player A places items in Plot Alpha");
        Debug.Log("U: Player A removes an item from Plot Alpha");
        Debug.Log("O: Player B tries to acquire Plot Alpha (should fail)");
        Debug.Log("V: View current ownership and items");
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (PlayerHousingManager.Instance != null)
        {
            PlayerHousingManager.Instance.OnPlotAcquired -= HandlePlotAcquired;
            PlayerHousingManager.Instance.OnPlotReleased -= HandlePlotReleased;
            PlayerHousingManager.Instance.OnItemPlaced -= HandleItemPlaced;
            PlayerHousingManager.Instance.OnItemRemoved -= HandleItemRemoved;
            PlayerHousingManager.Instance.OnHousingDataLoaded -= HandleHousingDataLoaded;
            PlayerHousingManager.Instance.OnHousingDataSaved -= HandleHousingDataSaved;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadHousingData();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveHousingData();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log($"--- Player {playerID_A} attempts to ACQUIRE Plot Alpha ---");
            PlayerHousingManager.Instance.AcquirePlot("Plot_Alpha", playerID_A);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log($"--- Player {playerID_A} attempts to RELEASE Plot Alpha ---");
            PlayerHousingManager.Instance.ReleasePlot("Plot_Alpha");
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"--- Player {playerID_A} attempts to PLACE ITEMS in Plot Alpha ---");
            // Place a chair
            PlayerHousingManager.Instance.PlaceItemInPlot("Plot_Alpha", "WoodenChair", new Vector3(0, 0.5f, 0), Quaternion.identity);
            // Place a table
            PlayerHousingManager.Instance.PlaceItemInPlot("Plot_Alpha", "DiningTable", new Vector3(1, 0.5f, 1), Quaternion.Euler(0, 45, 0));
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log($"--- Player {playerID_A} attempts to REMOVE an item from Plot Alpha ---");
            if (plotAlpha != null && plotAlpha.PlacedItems.Count > 0)
            {
                // Remove the first item placed
                PlayerHousingManager.Instance.RemoveItemFromPlot("Plot_Alpha", plotAlpha.PlacedItems[0].gameObject);
            }
            else
            {
                Debug.Log("No items to remove from Plot Alpha.");
            }
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log($"--- Player {playerID_B} attempts to ACQUIRE Plot Alpha (should fail if owned) ---");
            PlayerHousingManager.Instance.AcquirePlot("Plot_Alpha", playerID_B);
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("--- Viewing Current Housing State ---");
            ViewHousingState();
        }
    }

    void ViewHousingState()
    {
        Debug.Log($"Plot Alpha ({plotAlpha.PlotID}) - Owner: {(plotAlpha.IsOwned ? plotAlpha.CurrentOwnerID : "None")}");
        if (plotAlpha.PlacedItems.Count > 0)
        {
            Debug.Log($"  Items in Plot Alpha:");
            foreach (var item in plotAlpha.PlacedItems)
            {
                Debug.Log($"    - {item.ItemType.displayName} at {item.transform.localPosition}");
            }
        }
        else
        {
            Debug.Log("  No items in Plot Alpha.");
        }

        Debug.Log($"Plot Beta ({plotBeta.PlotID}) - Owner: {(plotBeta.IsOwned ? plotBeta.CurrentOwnerID : "None")}");
        if (plotBeta.PlacedItems.Count > 0)
        {
            Debug.Log($"  Items in Plot Beta:");
            foreach (var item in plotBeta.PlacedItems)
            {
                Debug.Log($"    - {item.ItemType.displayName} at {item.transform.localPosition}");
            }
        }
        else
        {
            Debug.Log("  No items in Plot Beta.");
        }
    }


    // --- Persistence Helper Functions ---

    void SaveHousingData()
    {
        PlayerHousingSaveData data = PlayerHousingManager.Instance.GetCurrentHousingData();
        string json = JsonUtility.ToJson(data, true); // true for pretty print
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        File.WriteAllText(path, json);
        Debug.Log($"Housing data saved to: {path}");
    }

    void LoadHousingData()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            PlayerHousingSaveData data = JsonUtility.FromJson<PlayerHousingSaveData>(json);
            PlayerHousingManager.Instance.LoadHousingData(data);
        }
        else
        {
            Debug.LogWarning("No save file found at: " + path);
        }
    }

    // --- Event Handlers ---
    void HandlePlotAcquired(HousingPlot plot, string playerID)
    {
        Debug.Log($"[EVENT] Plot '{plot.PlotID}' was ACQUIRED by '{playerID}'.");
    }

    void HandlePlotReleased(HousingPlot plot, string playerID)
    {
        Debug.Log($"[EVENT] Plot '{plot.PlotID}' was RELEASED by '{playerID}'.");
    }

    void HandleItemPlaced(HousingPlot plot, PlaceableItemSO itemType, GameObject itemGO)
    {
        Debug.Log($"[EVENT] Item '{itemType.displayName}' placed in plot '{plot.PlotID}'.");
    }

    void HandleItemRemoved(HousingPlot plot, PlaceableItemSO itemType, GameObject itemGO)
    {
        Debug.Log($"[EVENT] Item '{itemType.displayName}' removed from plot '{plot.PlotID}'.");
    }

    void HandleHousingDataLoaded()
    {
        Debug.Log("[EVENT] Housing data finished loading.");
        ViewHousingState(); // Show updated state
    }

    void HandleHousingDataSaved()
    {
        Debug.Log("[EVENT] Housing data finished saving.");
    }
}
```

*   Create an empty GameObject in your scene (e.g., named `TestClient`).
*   Attach the `HousingTestClient.cs` script to it.
*   Run the scene and press the keys as indicated in the debug log. Observe the Console output and the GameObjects in the scene.
*   You can acquire Plot Alpha, place items, save, stop the game, run again, and load to see the state restored.

This comprehensive example provides a robust foundation for a Player Housing System in Unity, demonstrating common design patterns and best practices.