// Unity Design Pattern Example: MemoryBudgetSystem
// This script demonstrates the MemoryBudgetSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Memory Budget System** design pattern. This pattern is crucial for managing memory in games, especially on platforms with limited resources or when dealing with many dynamic assets (textures, audio, models, etc.). It helps prevent out-of-memory errors by defining a budget and evicting older/less important resources when new ones are requested.

---

### **MemoryBudgetSystem.cs**

This script implements the core logic of the Memory Budget System, using a Least Recently Used (LRU) eviction strategy.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Used for some LINQ operations like ToList() for display purposes

/// <summary>
/// Implements the Memory Budget System design pattern using a Least Recently Used (LRU) eviction strategy.
/// This system allows you to define a maximum memory budget for a specific category of resources
/// (e.g., textures, audio, specific data types). When a new resource is requested, it checks
/// if there's enough budget. If not, it evicts the least recently used resources until space
/// is available or the budget cannot be met.
///
/// This is a MonoBehaviour singleton, making it easy to configure in the Unity Editor
/// and globally accessible throughout your game.
/// </summary>
public class MemoryBudgetSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    public static MemoryBudgetSystem Instance { get; private set; }

    // --- Configuration (visible in Unity Inspector) ---
    [Header("Memory Budget Settings")]
    [Tooltip("The maximum memory budget in Megabytes (MB) for all tracked resources.")]
    [SerializeField]
    private long _maxMemoryBudgetMB = 512; // Default budget of 512 MB

    // --- Private Internal State ---
    private long _maxMemoryBudgetBytes; // Converted budget from MB to bytes
    private long _currentMemoryUsageBytes = 0; // Current total memory consumed by tracked resources

    // Dictionary to store information about each tracked resource, keyed by its unique ID.
    // This allows for quick lookup of resource details (size, eviction callback, LRU node).
    private Dictionary<string, BudgetedResourceInfo> _resources = new Dictionary<string, BudgetedResourceInfo>();

    // LinkedList to maintain the order of resource usage for LRU eviction.
    // The head of the list contains the Least Recently Used (LRU) resource.
    // The tail of the list contains the Most Recently Used (MRU) resource.
    private LinkedList<string> _lruOrder = new LinkedList<string>();

    /// <summary>
    /// Private helper class to encapsulate all relevant information for a resource
    /// being managed by the Memory Budget System.
    /// </summary>
    private class BudgetedResourceInfo
    {
        public string Id;                  // Unique identifier for the resource (e.g., "Texture_Forest", "Audio_Level1Music")
        public long SizeBytes;             // The memory size (in bytes) that this resource consumes
        public Action OnEvicted;           // A callback function to be executed when this resource is evicted.
                                           // This action should contain the actual logic to unload/free the resource's memory.
        public LinkedListNode<string> LruNode; // A direct reference to its node in the _lruOrder LinkedList,
                                           // allowing O(1) removal when updating LRU status.
    }

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the singleton pattern for global access and initializes the budget.
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one to ensure only one singleton.
            Debug.LogWarning($"MemoryBudgetSystem: Duplicate instance found, destroying '{gameObject.name}'.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            _maxMemoryBudgetBytes = _maxMemoryBudgetMB * 1024 * 1024; // Convert MB to bytes
            // Ensure this GameObject persists across scene loads if memory budget needs to be global.
            DontDestroyOnLoad(gameObject);
            Debug.Log($"MemoryBudgetSystem Initialized: Max Budget = {_maxMemoryBudgetMB} MB ({_maxMemoryBudgetBytes} bytes)");
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed.
    /// Cleans up the singleton instance.
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Public API for Memory Budget Management ---

    /// <summary>
    /// Attempts to allocate memory for a new resource or registers access to an existing one.
    /// If a new resource is requested and the budget is insufficient, the system will attempt
    /// to evict Least Recently Used (LRU) resources until enough space is available.
    /// </summary>
    /// <param name="resourceId">A unique string identifier for the resource (e.g., its asset path).</param>
    /// <param name="requestedSizeBytes">The amount of memory (in bytes) this resource requires.</param>
    /// <param name="onEvictedCallback">An Action to be invoked if this resource is evicted.
    ///                                   This callback should contain the logic to free the actual memory
    ///                                   associated with the resource (e.g., `texture.UnloadImmediate()`).</param>
    /// <returns>True if the resource was successfully allocated/accessed (and potentially loaded)
    ///          or already tracked, false if allocation failed even after eviction attempts.</returns>
    public bool TryAllocateOrAccess(string resourceId, long requestedSizeBytes, Action onEvictedCallback)
    {
        // --- Step 1: Check if the resource is already tracked ---
        // If the resource is already in our system, it means its memory is already allocated.
        // We just need to update its LRU status to mark it as Most Recently Used (MRU).
        if (_resources.TryGetValue(resourceId, out BudgetedResourceInfo existingResource))
        {
            // Move the resource's node to the end of the LRU list (MRU position).
            _lruOrder.Remove(existingResource.LruNode);
            _lruOrder.AddLast(existingResource.LruNode);
            Debug.Log($"<color=teal>MemoryBudgetSystem:</color> Accessed existing resource '{resourceId}'. Current usage: {CurrentMemoryUsageMB:F2}MB / {MaxMemoryBudgetMB:F2}MB");
            return true; // Resource is available and LRU updated
        }

        // --- Step 2: Check if new allocation fits immediately ---
        // If the resource is new, check if there's enough space in the budget
        // without needing to evict any existing resources.
        if (_currentMemoryUsageBytes + requestedSizeBytes <= _maxMemoryBudgetBytes)
        {
            AddNewResource(resourceId, requestedSizeBytes, onEvictedCallback);
            Debug.Log($"<color=green>MemoryBudgetSystem:</color> Allocated new resource '{resourceId}' ({requestedSizeBytes / (1024f*1024f):F2}MB). Current usage: {CurrentMemoryUsageMB:F2}MB / {MaxMemoryBudgetMB:F2}MB");
            return true; // Allocation successful
        }

        // --- Step 3: Not enough space, attempt to evict LRU resources ---
        Debug.LogWarning($"<color=orange>MemoryBudgetSystem:</color> Insufficient budget for '{resourceId}' ({requestedSizeBytes / (1024f*1024f):F2}MB). Attempting eviction of LRU resources...");
        if (EvictResourcesUntilSpaceAvailable(requestedSizeBytes))
        {
            // If eviction successfully freed up enough space, add the new resource.
            AddNewResource(resourceId, requestedSizeBytes, onEvictedCallback);
            Debug.Log($"<color=green>MemoryBudgetSystem:</color> Allocated new resource '{resourceId}' after eviction. Current usage: {CurrentMemoryUsageMB:F2}MB / {MaxMemoryBudgetMB:F2}MB");
            return true; // Allocation successful after eviction
        }

        // --- Step 4: Still not enough space ---
        // Even after evicting all possible LRU resources, there isn't enough budget.
        Debug.LogError($"<color=red>MemoryBudgetSystem:</color> Failed to allocate '{resourceId}' ({requestedSizeBytes / (1024f*1024f):F2}MB). " +
                       $"Not enough budget even after evicting all possible resources. " +
                       $"Current usage: {CurrentMemoryUsageMB:F2}MB / {MaxMemoryBudgetMB:F2}MB.");
        return false; // Allocation failed
    }

    /// <summary>
    /// Explicitly removes a resource from the budget system.
    /// This should be called when a resource is intentionally unloaded or no longer needed
    /// (e.g., when a scene changes, an object is destroyed, or a temporary asset is disposed).
    /// </summary>
    /// <param name="resourceId">The unique identifier of the resource to free.</param>
    /// <returns>True if the resource was found and successfully freed, false otherwise.</returns>
    public bool FreeResource(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out BudgetedResourceInfo resourceInfo))
        {
            // Remove the resource's memory from the current usage count.
            _currentMemoryUsageBytes -= resourceInfo.SizeBytes;
            // Remove the resource from the tracking dictionary.
            _resources.Remove(resourceId);
            // Remove the resource's node from the LRU list.
            _lruOrder.Remove(resourceInfo.LruNode);
            // Invoke the resource's specific callback to actually free its data.
            resourceInfo.OnEvicted?.Invoke();

            Debug.Log($"<color=blue>MemoryBudgetSystem:</color> Explicitly freed resource '{resourceId}'. Current usage: {CurrentMemoryUsageMB:F2}MB / {MaxMemoryBudgetMB:F2}MB");
            return true;
        }
        Debug.LogWarning($"<color=yellow>MemoryBudgetSystem:</color> Attempted to free unknown resource '{resourceId}'.");
        return false;
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Adds a new resource to the tracking system and updates internal state.
    /// Assumes budget checks have already been performed and space is available.
    /// </summary>
    private void AddNewResource(string resourceId, long sizeBytes, Action onEvictedCallback)
    {
        _currentMemoryUsageBytes += sizeBytes; // Add resource size to current usage
        
        // Create a new BudgetedResourceInfo object for the resource.
        BudgetedResourceInfo newResource = new BudgetedResourceInfo
        {
            Id = resourceId,
            SizeBytes = sizeBytes,
            OnEvicted = onEvictedCallback
        };

        // Add the resource's ID to the end of the LRU list (making it MRU).
        LinkedListNode<string> lruNode = _lruOrder.AddLast(resourceId);
        newResource.LruNode = lruNode; // Store reference to the node for O(1) removal

        // Add the resource to the tracking dictionary.
        _resources.Add(resourceId, newResource);
    }

    /// <summary>
    /// Evicts Least Recently Used (LRU) resources until the required space is available
    /// or there are no more resources to evict.
    /// </summary>
    /// <param name="requiredSpace">The minimum amount of space (in bytes) needed.</param>
    /// <returns>True if enough space was freed up, false otherwise.</returns>
    private bool EvictResourcesUntilSpaceAvailable(long requiredSpace)
    {
        // Continue evicting as long as current usage + required space exceeds budget
        // AND there are resources left in the LRU list to evict.
        while (_currentMemoryUsageBytes + requiredSpace > _maxMemoryBudgetBytes && _lruOrder.Count > 0)
        {
            // Get the ID of the Least Recently Used resource (from the head of the list).
            string lruResourceId = _lruOrder.First.Value;

            // Retrieve its info from the dictionary.
            if (_resources.TryGetValue(lruResourceId, out BudgetedResourceInfo lruResource))
            {
                Debug.Log($"<color=orange>MemoryBudgetSystem:</color> Evicting LRU resource '{lruResourceId}' ({lruResource.SizeBytes / (1024f*1024f):F2}MB) to free up space.");
                // Call FreeResource, which handles updating _currentMemoryUsageBytes,
                // removing from _resources, removing from _lruOrder, and invoking OnEvicted.
                FreeResource(lruResourceId);
            }
            else
            {
                // This scenario indicates a bug where _lruOrder and _resources are out of sync.
                Debug.LogError($"<color=red>MemoryBudgetSystem Error:</color> LRU list out of sync! Resource ID '{lruResourceId}' found in LRU list but not in resource dictionary. Removing from LRU list.");
                _lruOrder.RemoveFirst(); // Clean up the malformed entry to prevent infinite loop.
            }
        }
        // Return true if, after eviction, there's now enough space for the requested allocation.
        return _currentMemoryUsageBytes + requiredSpace <= _maxMemoryBudgetBytes;
    }

    // --- Public Properties for Monitoring and Debugging ---

    /// <summary>
    /// Gets the maximum memory budget configured in Megabytes.
    /// </summary>
    public long MaxMemoryBudgetMB => _maxMemoryBudgetMB;

    /// <summary>
    /// Gets the maximum memory budget configured in Bytes.
    /// </summary>
    public long MaxMemoryBudgetBytes => _maxMemoryBudgetBytes;

    /// <summary>
    /// Gets the current total memory usage of all tracked resources in Bytes.
    /// </summary>
    public long CurrentMemoryUsageBytes => _currentMemoryUsageBytes;

    /// <summary>
    /// Gets the current total memory usage of all tracked resources in Megabytes (float for precision).
    /// </summary>
    public float CurrentMemoryUsageMB => _currentMemoryUsageBytes / (1024f * 1024f);

    /// <summary>
    /// Gets the remaining available memory within the budget in Megabytes (float for precision).
    /// </summary>
    public float RemainingMemoryMB => (_maxMemoryBudgetBytes - _currentMemoryUsageBytes) / (1024f * 1024f);

    /// <summary>
    /// Gets the number of resources currently being tracked by the system.
    /// </summary>
    public int TrackedResourceCount => _resources.Count;

    /// <summary>
    /// Returns a list of IDs for all currently tracked resources.
    /// (Useful for debugging/display).
    /// </summary>
    public List<string> GetTrackedResourceIds()
    {
        return _resources.Keys.ToList();
    }

    /// <summary>
    /// Returns a string representation of the LRU order (Least Recently Used to Most Recently Used).
    /// </summary>
    public string GetLruOrderString()
    {
        return string.Join(" -> ", _lruOrder);
    }

    /// <summary>
    /// Attempts to retrieve basic info about a tracked resource.
    /// </summary>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="info">Tuple containing ID and SizeBytes if found.</param>
    /// <returns>True if resource was found, false otherwise.</returns>
    public bool TryGetResourceInfo(string resourceId, out (string Id, long SizeBytes) info)
    {
        if (_resources.TryGetValue(resourceId, out var resInfo))
        {
            info = (resInfo.Id, resInfo.SizeBytes);
            return true;
        }
        info = (null, 0);
        return false;
    }
}
```

---

### **MemoryBudgetDemo.cs**

This script demonstrates how to use the `MemoryBudgetSystem` in a practical scenario. It simulates loading various "assets" of different sizes and shows how the budget system manages their memory, including eviction. It includes a simple `OnGUI` for real-time feedback.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random; // Use Unity's Random for better game dev consistency

/// <summary>
/// This script demonstrates the usage of the MemoryBudgetSystem.
/// It simulates a game loading various assets (like textures or models)
/// that consume memory and interact with the budget system.
///
/// Drop this script on an empty GameObject in your scene along with the MemoryBudgetSystem.
/// </summary>
public class MemoryBudgetDemo : MonoBehaviour
{
    [Header("Demo Settings")]
    [Tooltip("The total number of unique demo assets that can be generated.")]
    [SerializeField] private int _numberOfDemoAssets = 15;

    [Tooltip("Min and Max size (in MB) for simulated assets. Actual memory will be allocated.")]
    [SerializeField] private Vector2 _minMaxAssetSizeMB = new Vector2(5, 50); // Assets can range from 5MB to 50MB

    [Tooltip("Time interval in seconds to automatically attempt to load a new random asset.")]
    [SerializeField] private float _autoLoadIntervalSeconds = 1.0f;

    [Tooltip("If true, the demo will automatically load assets at regular intervals.")]
    [SerializeField] private bool _autoLoadAssets = true;

    private List<DemoAsset> _allDemoAssets = new List<DemoAsset>(); // List of all possible assets
    private float _lastLoadTime; // Timer for automatic loading

    /// <summary>
    /// A simple class to simulate an actual game asset (e.g., Texture2D, AudioClip, Mesh data).
    /// It keeps track of its ID, size, and whether its data is actually loaded into memory.
    /// The `_simulatedData` byte array is crucial to mimic actual memory consumption.
    /// </summary>
    public class DemoAsset
    {
        public string Id { get; private set; }
        public long SizeBytes { get; private set; }
        public bool IsLoaded { get; private set; } // Tracks if the asset's data is currently in memory
        private byte[] _simulatedData; // This array simulates the actual memory consumed by the asset

        public DemoAsset(string id, long sizeBytes)
        {
            Id = id;
            SizeBytes = sizeBytes;
            IsLoaded = false;
        }

        /// <summary>
        /// Simulates loading the asset's data into memory.
        /// In a real game, this would be loading an actual Unity Asset.
        /// </summary>
        public void Load()
        {
            if (!IsLoaded)
            {
                Debug.Log($"<color=green>DEMO ASSET:</color> Loading actual data for '{Id}' ({SizeBytes / (1024f * 1024f):F2}MB)...");
                _simulatedData = new byte[SizeBytes]; // Allocate actual managed memory
                IsLoaded = true;
            }
        }

        /// <summary>
        /// Simulates unloading the asset's data from memory.
        /// In a real game, this would be `Texture2D.DestroyImmediate(texture)` or similar.
        /// </summary>
        public void Unload()
        {
            if (IsLoaded)
            {
                Debug.Log($"<color=red>DEMO ASSET:</color> Unloading actual data for '{Id}'...");
                _simulatedData = null; // Release the memory
                GC.Collect(); // Force garbage collection to free the byte array memory immediately (for demo clarity)
                IsLoaded = false;
            }
        }
    }

    /// <summary>
    /// Initializes the demo by creating a set of simulated assets.
    /// </summary>
    void Start()
    {
        // Ensure the MemoryBudgetSystem is present in the scene.
        if (MemoryBudgetSystem.Instance == null)
        {
            Debug.LogError("MemoryBudgetSystem not found! Please add it to your scene (e.g., on an empty GameObject).");
            enabled = false; // Disable this demo script if the budget system isn't found
            return;
        }

        InitializeDemoAssets();
        _lastLoadTime = Time.time;
    }

    /// <summary>
    /// Automatically attempts to load assets at intervals if _autoLoadAssets is enabled.
    /// </summary>
    void Update()
    {
        if (_autoLoadAssets && Time.time - _lastLoadTime >= _autoLoadIntervalSeconds)
        {
            _lastLoadTime = Time.time;
            SimulateAssetLoading();
        }
    }

    /// <summary>
    /// Displays the current memory budget status and interaction buttons using Unity's OnGUI.
    /// </summary>
    void OnGUI()
    {
        // Setup a basic GUI box for display
        GUI.Box(new Rect(10, 10, 400, 650), "Memory Budget System Demo");

        float yOffset = 40; // Initial Y position for UI elements

        // Display current budget status
        GUI.Label(new Rect(20, yOffset, 380, 20), $"<size=18>Budget: <color=cyan>{MemoryBudgetSystem.Instance.MaxMemoryBudgetMB:F2} MB</color></size>"); yOffset += 25;
        GUI.Label(new Rect(20, yOffset, 380, 20), $"<size=18>Usage: <color=yellow>{MemoryBudgetSystem.Instance.CurrentMemoryUsageMB:F2} MB</color></size>"); yOffset += 25;
        GUI.Label(new Rect(20, yOffset, 380, 20), $"<size=18>Remaining: <color=lime>{MemoryBudgetSystem.Instance.RemainingMemoryMB:F2} MB</color></size>"); yOffset += 25;
        GUI.Label(new Rect(20, yOffset, 380, 20), $"<size=18>Tracked Assets: {MemoryBudgetSystem.Instance.TrackedResourceCount}</size>"); yOffset += 30;

        // Display the LRU order of tracked resources
        GUI.Label(new Rect(20, yOffset, 380, 20), "<size=16>Tracked Resources (LRU First):</size>"); yOffset += 25;

        // Iterate through the LRU order string (Least Recently Used to Most Recently Used)
        string lruOrderString = MemoryBudgetSystem.Instance.GetLruOrderString();
        string[] trackedIds = lruOrderString.Split(new string[] { " -> " }, StringSplitOptions.RemoveEmptyEntries);

        if (trackedIds.Length == 0)
        {
            GUI.Label(new Rect(30, yOffset, 370, 20), "- No assets currently loaded.");
            yOffset += 20;
        }
        else
        {
            foreach (string resourceId in trackedIds)
            {
                // Try to get detailed info for each tracked resource
                if (MemoryBudgetSystem.Instance.TryGetResourceInfo(resourceId, out var info))
                {
                    GUI.Label(new Rect(30, yOffset, 370, 20), $"- {info.Id} ({info.SizeBytes / (1024f*1024f):F2}MB)");
                    yOffset += 20;
                }
                else
                {
                    // This case should ideally not happen if _resources and _lruOrder are in sync
                    GUI.Label(new Rect(30, yOffset, 370, 20), $"- {resourceId} (Info Missing - Bug?)");
                    yOffset += 20;
                }
            }
        }
        
        yOffset += 20;

        // Manual interaction buttons
        if (GUI.Button(new Rect(20, yOffset, 170, 40), "<size=18>Load Random Asset</size>"))
        {
            SimulateAssetLoading();
        }
        if (GUI.Button(new Rect(210, yOffset, 170, 40), "<size=18>Free Random Asset</size>"))
        {
            FreeRandomAsset();
        }
        yOffset += 50;

        // Toggle for automatic loading
        _autoLoadAssets = GUI.Toggle(new Rect(20, yOffset, 360, 30), _autoLoadAssets, "<size=18>Auto Load Assets</size>");
    }

    /// <summary>
    /// Creates a list of `_numberOfDemoAssets` with random sizes.
    /// These are the potential assets that the budget system will manage.
    /// </summary>
    private void InitializeDemoAssets()
    {
        for (int i = 0; i < _numberOfDemoAssets; i++)
        {
            // Generate a random size within the specified range (in MB, then convert to Bytes)
            long sizeBytes = (long)(Random.Range(_minMaxAssetSizeMB.x, _minMaxAssetSizeMB.y) * 1024 * 1024);
            _allDemoAssets.Add(new DemoAsset($"Asset_{i + 1:00}", sizeBytes));
        }
        Debug.Log($"<color=grey>DEMO:</color> Initialized {_numberOfDemoAssets} demo assets.");
    }

    /// <summary>
    /// Simulates the process of a game wanting to load or access an asset.
    /// It picks a random asset from the pool and attempts to register it with the MemoryBudgetSystem.
    /// </summary>
    private void SimulateAssetLoading()
    {
        if (_allDemoAssets.Count == 0) return;

        // Pick a random asset from our pool. This asset might already be loaded or not.
        DemoAsset assetToLoad = _allDemoAssets[Random.Range(0, _allDemoAssets.Count)];

        // IMPORTANT: The `onEvictedCallback` is the bridge between the MemoryBudgetSystem
        // and your actual asset management. When the budget system decides to evict this resource,
        // it will invoke this callback, which then tells our `DemoAsset` to actually free its memory.
        Action onEvictedCallback = () => assetToLoad.Unload();

        Debug.Log($"\n<color=cyan>DEMO:</color> Attempting to load/access '{assetToLoad.Id}' ({assetToLoad.SizeBytes / (1024f * 1024f):F2}MB)...");

        // Attempt to allocate or access the asset through the MemoryBudgetSystem.
        if (MemoryBudgetSystem.Instance.TryAllocateOrAccess(assetToLoad.Id, assetToLoad.SizeBytes, onEvictedCallback))
        {
            // If the budget system gives the green light, then we actually load the asset data.
            assetToLoad.Load();
            Debug.Log($"<color=green>DEMO:</color> Successfully handled '{assetToLoad.Id}'. Asset is now loaded.");
        }
        else
        {
            Debug.Log($"<color=red>DEMO:</color> Failed to load '{assetToLoad.Id}'. Budget system denied allocation.");
        }
    }

    /// <summary>
    /// Simulates a scenario where a game explicitly decides to unload an asset,
    /// regardless of the LRU status (e.g., when moving to a new scene where certain assets are no longer needed).
    /// </summary>
    private void FreeRandomAsset()
    {
        if (MemoryBudgetSystem.Instance.TrackedResourceCount == 0)
        {
            Debug.Log("<color=grey>DEMO:</color> No assets currently tracked by the MemoryBudgetSystem to free.");
            return;
        }

        // Get a list of IDs for assets currently being tracked by the budget system.
        List<string> trackedIds = MemoryBudgetSystem.Instance.GetTrackedResourceIds();
        string assetIdToFree = trackedIds[Random.Range(0, trackedIds.Count)];

        // Find the actual DemoAsset object corresponding to the ID.
        DemoAsset assetToFree = _allDemoAssets.Find(a => a.Id == assetIdToFree);
        if (assetToFree != null)
        {
            Debug.Log($"\n<color=magenta>DEMO:</color> Attempting to explicitly free '{assetIdToFree}'...");
            
            // First, tell the MemoryBudgetSystem to stop tracking this resource and free its budget.
            // This will also trigger the onEvictedCallback associated with the resource.
            MemoryBudgetSystem.Instance.FreeResource(assetIdToFree);

            // Redundantly ensure the actual asset is unloaded. (The callback already does this, but good for clarity).
            assetToFree.Unload(); 
            Debug.Log($"<color=magenta>DEMO:</color> Explicitly freed '{assetIdToFree}'.");
        }
        else
        {
            Debug.LogError($"<color=red>DEMO Error:</color> Demo asset '{assetIdToFree}' not found in _allDemoAssets. This shouldn't happen if IDs are consistent.");
        }
    }
}
```

---

### **How to Use in Unity**

1.  **Create C# Scripts:**
    *   Create a new C# script named `MemoryBudgetSystem.cs` and paste the first code block into it.
    *   Create another C# script named `MemoryBudgetDemo.cs` and paste the second code block into it.

2.  **Setup in a Unity Scene:**
    *   Create an empty GameObject in your scene (e.g., named `GameManager`).
    *   Attach the `MemoryBudgetSystem.cs` script to this `GameManager` GameObject.
    *   You can adjust the `Max Memory Budget MB` directly in the Inspector for `MemoryBudgetSystem` (e.g., 256MB for a quick demo).
    *   Create another empty GameObject (e.g., named `MemoryDemo`).
    *   Attach the `MemoryBudgetDemo.cs` script to this `MemoryDemo` GameObject.
    *   Adjust `_numberOfDemoAssets`, `_minMaxAssetSizeMB`, and `_autoLoadIntervalSeconds` in the `MemoryBudgetDemo`'s Inspector to control the simulation.

3.  **Run the Scene:**
    *   Play the Unity scene.
    *   Observe the `Debug.Log` messages in the Console, which will show assets being loaded, accessed, and evicted.
    *   The `OnGUI` overlay will provide a real-time summary of the memory budget, current usage, and the LRU order of tracked assets.
    *   Try different budget sizes and asset sizes to see how eviction patterns change.

### **Explanation and Practical Applications**

**1. The Memory Budget System Pattern:**
    *   **Goal:** To prevent out-of-memory issues and manage system memory efficiently by setting a cap on memory usage for certain resource types.
    *   **Core Components:**
        *   **Budget:** A defined maximum memory limit.
        *   **Current Usage:** Tracks how much memory is currently consumed.
        *   **Resource Tracking:** Keeps a record of each loaded resource and its size.
        *   **Eviction Policy:** A strategy to decide *which* resources to unload when the budget is exceeded (here, LRU - Least Recently Used).
        *   **Eviction Callback:** A mechanism (like `Action OnEvicted`) to notify the actual resource management system to free up the resource's memory.

**2. Key Design Choices in this Example:**

    *   **Singleton (`MemoryBudgetSystem.Instance`):** Provides easy global access to the memory manager from anywhere in your game.
    *   **Configurable Budget:** `_maxMemoryBudgetMB` allows designers or developers to easily set the budget from the Inspector without changing code.
    *   **`BudgetedResourceInfo` Class:** A private nested class to encapsulate all relevant data for a resource being managed: its ID, size, and the crucial `OnEvicted` callback.
    *   **LRU Eviction with `LinkedList<string>`:**
        *   The `_lruOrder` `LinkedList` is highly efficient for LRU.
        *   When a resource is accessed or newly added, its corresponding node is moved to the *end* of the list (Most Recently Used - MRU).
        *   When eviction is needed, resources are taken from the *beginning* of the list (Least Recently Used - LRU).
        *   Storing `LinkedListNode<string> LruNode` in `BudgetedResourceInfo` allows `O(1)` (constant time) removal from the `LinkedList` when updating LRU status, instead of `O(N)` for searching.
    *   **`TryAllocateOrAccess` Method:** This is the primary entry point for game code.
        *   It first checks if the resource is *already loaded*. If so, it just updates its LRU status.
        *   If new, it checks if it *fits directly*.
        *   If not, it calls `EvictResourcesUntilSpaceAvailable` to free up space.
        *   Finally, it allocates if space is made, or reports failure.
    *   **`onEvictedCallback` (`Action` delegate):** This is the most vital part of the pattern. The `MemoryBudgetSystem` *doesn't know* how to free a specific `Texture2D` or `AudioClip`. It only knows *when* one needs to be freed. The `onEvictedCallback` provides that instruction back to the actual asset loader/manager. In our demo, it calls `assetToLoad.Unload()`. In a real project, this might be `Resources.UnloadAsset(texture)` or `assetBundle.UnloadAsset(assetName)`.
    *   **`DemoAsset` Class:** Simulates real assets. It allocates a `byte[]` to realistically consume memory, and its `Load()`/`Unload()` methods mimic real asset loading/unloading.

**3. Real-World Unity Integration:**

*   **Asset Bundles / Addressables:** This pattern is perfectly suited for managing assets loaded via Asset Bundles or Unity's Addressables system. You would typically register an asset with the `MemoryBudgetSystem` *after* its `AssetBundle.LoadAssetAsync()` or `Addressables.LoadAssetAsync()` completes, providing its estimated memory footprint. The `onEvictedCallback` would then call `assetHandle.Release()` or `assetBundle.Unload(true)`.
*   **Dynamic Content:** Games with large open worlds, many characters, or procedurally generated content can use this to stream in/out assets based on player proximity, importance, or recent usage.
*   **Platform Constraints:** Essential for mobile games or consoles with limited RAM.
*   **Texture Streaming (Custom):** While Unity has built-in texture streaming, you might use a budget system for custom texture atlases or specific texture groups that Unity's system doesn't handle.
*   **Audio Management:** Similar to textures, large audio clips (especially ambient or music tracks) can be managed to ensure only relevant ones are in memory.

**4. Further Enhancements / Considerations:**

*   **Priority Levels:** Extend `BudgetedResourceInfo` with a `Priority` enum (e.g., `High`, `Medium`, `Low`). Eviction could prioritize `Low` priority assets first, then `Medium`, etc., before resorting to LRU within the same priority.
*   **Asynchronous Eviction:** For very large assets, `onEvictedCallback` might trigger an asynchronous unload operation to prevent frame hitches.
*   **Thread Safety:** If `TryAllocateOrAccess` or `FreeResource` can be called from multiple threads, you'd need `lock` statements to protect the `_resources` dictionary and `_lruOrder` list.
*   **Memory Profiler:** Use Unity's Memory Profiler (Window > Analysis > Memory Profiler) to verify that memory is actually being freed when `DemoAsset.Unload()` is called and garbage collected. (Note: `GC.Collect()` is used in the demo for immediate feedback, but generally avoided in production due to performance impact).
*   **Error Handling:** More robust error handling for cases like `resourceId` collisions or invalid sizes.
*   **Integration with UI:** A more sophisticated UI to visualize the budget, usage, and tracked assets.
*   **Dynamic Budget Adjustment:** The `_maxMemoryBudgetMB` could be adjusted at runtime based on game settings or detected hardware capabilities.