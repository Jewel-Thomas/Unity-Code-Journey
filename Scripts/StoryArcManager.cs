// Unity Design Pattern Example: StoryArcManager
// This script demonstrates the StoryArcManager pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'StoryArcManager' design pattern, while not as formally defined as some others (like Singleton or Observer), typically refers to a central system responsible for managing the high-level narrative progression and state of story arcs within a game. This pattern is crucial for games with branching narratives, multiple questlines, character development paths, or any scenario where the overall story needs to be tracked and influenced by player actions.

It acts as a single source of truth for the game's narrative state, allowing different game systems (UI, NPCs, level loaders, event triggers) to query the story's progress and react accordingly, without needing to know the intricate details of how each arc is progressing internally.

Here's a breakdown of its core components and how they'll be implemented in Unity:

1.  **`StoryArcData` (ScriptableObject):** Represents a single story arc. This is where we define its unique ID, name, description, and any prerequisites. Using `ScriptableObject` allows us to create and manage these arcs easily in the Unity Editor as data assets.
2.  **`StoryArcState` (Enum):** Defines the possible states a story arc can be in (e.g., `NotStarted`, `Active`, `Completed`, `Failed`, `Locked`).
3.  **`StoryArcManager` (Singleton MonoBehaviour):** The central hub.
    *   It holds a dictionary of all registered `StoryArcData` objects and their current states.
    *   It provides public methods for other game systems to request starting, completing, or failing arcs.
    *   It implements a `Singleton` pattern for easy global access.
    *   It uses C# events (`Action`) to notify interested listeners whenever an arc's state changes. This promotes loose coupling.
4.  **`StoryArcTestClient` (Example MonoBehaviour):** A simple script to demonstrate how other game components would interact with the `StoryArcManager`.

---

### Step 1: Create the `StoryArcData` ScriptableObject

This defines what a story arc is.

**File: `Assets/Scripts/DesignPatterns/StoryArcManager/StoryArcData.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List

namespace StoryArcPattern
{
    // Enum to define the possible states of a Story Arc.
    public enum StoryArcState
    {
        NotStarted, // The arc has not yet begun.
        Locked,     // The arc cannot be started due to unmet prerequisites.
        Active,     // The arc is currently in progress.
        Completed,  // The arc has been successfully finished.
        Failed      // The arc has been failed (e.g., critical objective missed).
    }

    /// <summary>
    /// ScriptableObject representing a single Story Arc in the game's narrative.
    /// This allows us to define story arcs as data assets in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStoryArcData", menuName = "StoryArc/Story Arc Data", order = 1)]
    public class StoryArcData : ScriptableObject
    {
        [Tooltip("A unique identifier for this story arc (e.g., 'MAIN_QUEST_01', 'SIDE_QUEST_LOST_ITEM').")]
        [SerializeField] private string arcID = System.Guid.NewGuid().ToString();

        [Tooltip("The display name of the story arc.")]
        [SerializeField] private string arcName = "New Story Arc";

        [Tooltip("A brief description of what this story arc entails.")]
        [SerializeField] private string description = "A narrative segment of the game.";

        [Tooltip("List of other Story Arc IDs that must be in the 'Completed' state before this arc can be started.")]
        [SerializeField] private List<string> prerequisiteArcIDs = new List<string>();

        // Public properties to access the arc's data.
        public string ArcID => arcID;
        public string ArcName => arcName;
        public string Description => description;
        public IReadOnlyList<string> PrerequisiteArcIDs => prerequisiteArcIDs;

        // Ensure the ArcID is unique when creating new assets, especially in editor.
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(arcID))
            {
                arcID = System.Guid.NewGuid().ToString();
            }
        }
    }
}
```

---

### Step 2: Create the `StoryArcManager` Singleton

This is the core manager that tracks and controls all story arcs.

**File: `Assets/Scripts/DesignPatterns/StoryArcManager/StoryArcManager.cs`**

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like ToDictionary

namespace StoryArcPattern
{
    /// <summary>
    /// The StoryArcManager is a central Singleton responsible for managing the state and progression
    /// of all high-level narrative Story Arcs in the game.
    ///
    /// It provides an API for other game systems (e.g., Quest Givers, NPCs, UI, Event Triggers)
    /// to query arc states, and to initiate/complete/fail story arcs.
    ///
    /// It uses a Singleton pattern for easy global access.
    /// It uses events to notify listeners about arc state changes, promoting loose coupling.
    /// </summary>
    public class StoryArcManager : MonoBehaviour
    {
        // --- Singleton Instance ---
        private static StoryArcManager _instance;
        public static StoryArcManager Instance
        {
            get
            {
                // If the instance doesn't exist, try to find it in the scene.
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StoryArcManager>();

                    // If still null, create a new GameObject and add the manager to it.
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(StoryArcManager).Name);
                        _instance = singletonObject.AddComponent<StoryArcManager>();
                        Debug.Log($"[StoryArcManager] Created new singleton instance on GameObject: {singletonObject.name}");
                    }
                }
                return _instance;
            }
        }

        // --- Configuration ---
        [Tooltip("Drag and drop all Story Arc ScriptableObjects here to register them with the manager.")]
        [SerializeField] private List<StoryArcData> registeredStoryArcs = new List<StoryArcData>();

        // --- Internal State ---
        // Stores all StoryArcData for quick lookup by ArcID.
        private Dictionary<string, StoryArcData> _allArcData;
        // Stores the current state of each story arc.
        private Dictionary<string, StoryArcState> _currentArcStates;

        // --- Events ---
        /// <summary>
        /// Event triggered whenever a Story Arc's state changes.
        /// Parameters: (string arcID, StoryArcState newState, StoryArcState oldState)
        /// </summary>
        public static event Action<string, StoryArcState, StoryArcState> OnArcStateChanged;

        /// <summary>
        /// Event specifically for when an arc successfully starts.
        /// Parameters: (string arcID)
        /// </summary>
        public static event Action<string> OnArcStarted;

        /// <summary>
        /// Event specifically for when an arc is completed.
        /// Parameters: (string arcID)
        /// </summary>
        public static event Action<string> OnArcCompleted;

        /// <summary>
        /// Event specifically for when an arc is failed.
        /// Parameters: (string arcID)
        /// </summary>
        public static event Action<string> OnArcFailed;

        // --- Unity Lifecycle Methods ---
        private void Awake()
        {
            // Enforce Singleton pattern: ensure only one instance exists.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                Debug.LogWarning($"[StoryArcManager] Duplicate instance found. Destroying {gameObject.name}.");
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject); // Keep manager alive across scene changes.

            InitializeManager();
        }

        /// <summary>
        /// Initializes the StoryArcManager by loading all registered arcs and setting their initial states.
        /// This should only be called once, typically in Awake.
        /// </summary>
        private void InitializeManager()
        {
            Debug.Log("[StoryArcManager] Initializing...");

            // Populate _allArcData dictionary for quick lookup.
            _allArcData = registeredStoryArcs.ToDictionary(arc => arc.ArcID, arc => arc);

            // Initialize _currentArcStates.
            // All arcs start as NotStarted, or Locked if they have unmet prerequisites.
            _currentArcStates = new Dictionary<string, StoryArcState>();
            foreach (var arcData in registeredStoryArcs)
            {
                // Check if this arc is locked initially.
                // Note: At initialization, no arcs are completed yet, so any arc with prerequisites
                // will initially be 'Locked' or 'NotStarted' if no prereqs.
                StoryArcState initialState = StoryArcState.NotStarted;
                if (arcData.PrerequisiteArcIDs != null && arcData.PrerequisiteArcIDs.Count > 0)
                {
                    // For more dynamic locking, you'd re-evaluate this when an arc completes.
                    // For initial setup, we assume if prereqs exist, it's potentially locked.
                    // The 'StartArc' method will do a proper check.
                }
                _currentArcStates[arcData.ArcID] = initialState;
                Debug.Log($"[StoryArcManager] Registered Arc: '{arcData.ArcName}' ({arcData.ArcID}) - Initial State: {initialState}");
            }
            Debug.Log($"[StoryArcManager] Initialized with {_allArcData.Count} story arcs.");
        }

        // --- Public API Methods ---

        /// <summary>
        /// Attempts to start a story arc.
        /// Checks if the arc exists, is not already active/completed/failed, and if all prerequisites are met.
        /// </summary>
        /// <param name="arcID">The unique ID of the story arc to start.</param>
        /// <returns>True if the arc was successfully started or was already active, false otherwise.</returns>
        public bool StartArc(string arcID)
        {
            if (!DoesArcExist(arcID))
            {
                Debug.LogWarning($"[StoryArcManager] Cannot start arc '{arcID}'. Arc does not exist.");
                return false;
            }

            StoryArcState currentState = GetArcState(arcID);
            if (currentState == StoryArcState.Active)
            {
                Debug.Log($"[StoryArcManager] Arc '{arcID}' is already active.");
                return true; // Already active, so consider it "successfully started".
            }
            if (currentState == StoryArcState.Completed)
            {
                Debug.Log($"[StoryArcManager] Arc '{arcID}' is already completed.");
                return false;
            }
            if (currentState == StoryArcState.Failed)
            {
                Debug.Log($"[StoryArcManager] Arc '{arcID}' has already failed.");
                return false;
            }

            // Check prerequisites
            StoryArcData arcData = _allArcData[arcID];
            if (arcData.PrerequisiteArcIDs != null && arcData.PrerequisiteArcIDs.Count > 0)
            {
                foreach (string prereqID in arcData.PrerequisiteArcIDs)
                {
                    if (!IsArcCompleted(prereqID))
                    {
                        Debug.LogWarning($"[StoryArcManager] Cannot start arc '{arcID}'. Prerequisite arc '{prereqID}' is not completed.");
                        SetArcState(arcID, StoryArcState.Locked); // Optionally set to locked
                        return false;
                    }
                }
            }

            // All checks passed, start the arc.
            SetArcState(arcID, StoryArcState.Active);
            OnArcStarted?.Invoke(arcID);
            Debug.Log($"[StoryArcManager] Story Arc '{arcID}' ('{arcData.ArcName}') has started!");
            return true;
        }

        /// <summary>
        /// Marks a story arc as completed.
        /// </summary>
        /// <param name="arcID">The unique ID of the story arc to complete.</param>
        /// <returns>True if the arc was successfully completed, false otherwise (e.g., arc doesn't exist or already completed).</returns>
        public bool CompleteArc(string arcID)
        {
            if (!DoesArcExist(arcID))
            {
                Debug.LogWarning($"[StoryArcManager] Cannot complete arc '{arcID}'. Arc does not exist.");
                return false;
            }

            StoryArcState currentState = GetArcState(arcID);
            if (currentState == StoryArcState.Completed)
            {
                Debug.Log($"[StoryArcManager] Arc '{arcID}' is already completed.");
                return true; // Already completed, so consider it "successfully completed".
            }
            if (currentState == StoryArcState.Failed)
            {
                Debug.Log($"[StoryArcManager] Arc '{arcID}' has already failed and cannot be completed.");
                return false;
            }

            SetArcState(arcID, StoryArcState.Completed);
            OnArcCompleted?.Invoke(arcID);
            Debug.Log($"[StoryArcManager] Story Arc '{arcID}' ('{_allArcData[arcID].ArcName}') has been COMPLETED!");
            return true;
        }

        /// <summary>
        /// Marks a story arc as failed.
        /// </summary>
        /// <param name="arcID">The unique ID of the story arc to fail.</param>
        /// <returns>True if the arc was successfully failed, false otherwise (e.g., arc doesn't exist or already failed).</returns>
        public bool FailArc(string arcID)
        {
            if (!DoesArcExist(arcID))
            {
                Debug.LogWarning($"[StoryArcManager] Cannot fail arc '{arcID}'. Arc does not exist.");
                return false;
            }

            StoryArcState currentState = GetArcState(arcID);
            if (currentState == StoryArcState.Failed)
            {
                Debug.Log($"[StoryArcManager] Arc '{arcID}' is already failed.");
                return true; // Already failed, so consider it "successfully failed".
            }
            if (currentState == StoryArcState.Completed)
            {
                Debug.Log($"[StoryArcManager] Arc '{arcID}' is already completed and cannot be failed.");
                return false;
            }

            SetArcState(arcID, StoryArcState.Failed);
            OnArcFailed?.Invoke(arcID);
            Debug.Log($"[StoryArcManager] Story Arc '{arcID}' ('{_allArcData[arcID].ArcName}') has FAILED!");
            return true;
        }

        /// <summary>
        /// Retrieves the current state of a specified story arc.
        /// </summary>
        /// <param name="arcID">The unique ID of the story arc.</param>
        /// <returns>The current StoryArcState, or NotStarted if the arc ID is not found (and logs a warning).</returns>
        public StoryArcState GetArcState(string arcID)
        {
            if (_currentArcStates.TryGetValue(arcID, out StoryArcState state))
            {
                return state;
            }
            Debug.LogWarning($"[StoryArcManager] Story Arc with ID '{arcID}' not found in current states. Returning NotStarted.");
            return StoryArcState.NotStarted;
        }

        /// <summary>
        /// Checks if a story arc is currently active.
        /// </summary>
        /// <param name="arcID">The unique ID of the story arc.</param>
        /// <returns>True if the arc exists and is in the 'Active' state, false otherwise.</returns>
        public bool IsArcActive(string arcID) => GetArcState(arcID) == StoryArcState.Active;

        /// <summary>
        /// Checks if a story arc is completed.
        /// </summary>
        /// <param name="arcID">The unique ID of the story arc.</param>
        /// <returns>True if the arc exists and is in the 'Completed' state, false otherwise.</returns>
        public bool IsArcCompleted(string arcID) => GetArcState(arcID) == StoryArcState.Completed;

        /// <summary>
        /// Checks if a story arc exists in the manager's registered arcs.
        /// </summary>
        /// <param name="arcID">The unique ID of the story arc.</param>
        /// <returns>True if the arc exists, false otherwise.</returns>
        public bool DoesArcExist(string arcID) => _allArcData.ContainsKey(arcID);

        /// <summary>
        /// Internal method to change an arc's state and trigger the state change event.
        /// </summary>
        /// <param name="arcID">The ID of the arc.</param>
        /// <param name="newState">The new state for the arc.</param>
        private void SetArcState(string arcID, StoryArcState newState)
        {
            if (!_currentArcStates.ContainsKey(arcID))
            {
                Debug.LogError($"[StoryArcManager] Attempted to set state for non-existent arc '{arcID}'.");
                return;
            }

            StoryArcState oldState = _currentArcStates[arcID];
            if (oldState == newState) return; // State hasn't changed.

            _currentArcStates[arcID] = newState;
            Debug.Log($"[StoryArcManager] Arc '{arcID}' state changed from {oldState} to {newState}.");
            OnArcStateChanged?.Invoke(arcID, newState, oldState);
        }

        // --- Debug/Editor Helper ---
        [ContextMenu("Log All Arc States")]
        private void LogAllArcStates()
        {
            Debug.Log("--- Current Story Arc States ---");
            if (_currentArcStates == null || _currentArcStates.Count == 0)
            {
                Debug.Log("No arcs registered or manager not initialized.");
                return;
            }

            foreach (var entry in _currentArcStates)
            {
                string arcName = _allArcData.TryGetValue(entry.Key, out StoryArcData data) ? data.ArcName : "UNKNOWN NAME";
                Debug.Log($"  - '{arcName}' ({entry.Key}): {entry.Value}");
            }
            Debug.Log("----------------------------------");
        }
    }
}
```

---

### Step 3: Create an Example `StoryArcTestClient`

This script demonstrates how other game objects would interact with the `StoryArcManager`.

**File: `Assets/Scripts/DesignPatterns/StoryArcManager/StoryArcTestClient.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List

namespace StoryArcPattern
{
    /// <summary>
    /// This client script demonstrates how other game components would interact with the StoryArcManager.
    /// It subscribes to events and provides buttons to trigger arc state changes for testing purposes.
    /// </summary>
    public class StoryArcTestClient : MonoBehaviour
    {
        [Header("Arc IDs to interact with (copy from StoryArcData assets)")]
        [SerializeField] private string arcID_MainQuest01 = "MAIN_QUEST_01";
        [SerializeField] private string arcID_SideQuestHealing = "SIDE_QUEST_HEALING";
        [SerializeField] private string arcID_CharacterDevRedemption = "CHAR_DEV_REDEMPTION";

        private void OnEnable()
        {
            // Subscribe to the global arc state change event.
            // This is how any game system can react to story progression.
            StoryArcManager.OnArcStateChanged += HandleArcStateChanged;
            StoryArcManager.OnArcStarted += HandleArcStarted;
            StoryArcManager.OnArcCompleted += HandleArcCompleted;
            StoryArcManager.OnArcFailed += HandleArcFailed;

            Debug.Log("[StoryArcTestClient] Subscribed to StoryArcManager events.");
        }

        private void OnDisable()
        {
            // Always unsubscribe to prevent memory leaks and unexpected behavior.
            StoryArcManager.OnArcStateChanged -= HandleArcStateChanged;
            StoryArcManager.OnArcStarted -= HandleArcStarted;
            StoryArcManager.OnArcCompleted -= HandleArcCompleted;
            StoryArcManager.OnArcFailed -= HandleArcFailed;

            Debug.Log("[StoryArcTestClient] Unsubscribed from StoryArcManager events.");
        }

        // --- Event Handlers ---
        private void HandleArcStateChanged(string arcID, StoryArcState newState, StoryArcState oldState)
        {
            Debug.Log($"<color=cyan>[TestClient]</color> Event: Arc '{arcID}' changed from {oldState} to {newState}.");

            // Example reactions based on state changes:
            if (newState == StoryArcState.Active)
            {
                // UI: Update quest log, show new objective.
                // NPC: Change dialogue options.
                // Game World: Spawn new enemies, unlock a new area.
                Debug.Log($"<color=cyan>[TestClient]</color> -> UI: '{arcID}' is now active. Update quest log!");
            }
            else if (newState == StoryArcState.Completed)
            {
                // UI: Show completion message, award XP/items.
                // Game World: Trigger an end-of-arc cutscene, despawn temporary quest objects.
                // StoryArcManager: Potentially auto-start a follow-up arc if defined.
                Debug.Log($"<color=cyan>[TestClient]</color> -> UI: '{arcID}' completed! Show rewards!");

                // Example: If MainQuest01 completed, try to start CharacterDevRedemption
                if (arcID == arcID_MainQuest01)
                {
                    Debug.Log($"<color=cyan>[TestClient]</color> -> MainQuest01 completed, trying to start '{arcID_CharacterDevRedemption}'!");
                    StoryArcManager.Instance.StartArc(arcID_CharacterDevRedemption);
                }
            }
            else if (newState == StoryArcState.Failed)
            {
                // UI: Show failure message, load last checkpoint or game over screen.
                Debug.Log($"<color=cyan>[TestClient]</color> -> UI: '{arcID}' failed! Oh no!");
            }
        }

        private void HandleArcStarted(string arcID)
        {
            Debug.Log($"<color=green>[TestClient]</color> Event: Arc '{arcID}' has STARTED!");
        }

        private void HandleArcCompleted(string arcID)
        {
            Debug.Log($"<color=green>[TestClient]</color> Event: Arc '{arcID}' has COMPLETED!");
        }

        private void HandleArcFailed(string arcID)
        {
            Debug.Log($"<color=red>[TestClient]</color> Event: Arc '{arcID}' has FAILED!");
        }

        // --- Test UI Buttons (for Inspector) ---
        [ContextMenu("Start Main Quest 01")]
        public void TestStartMainQuest01()
        {
            Debug.Log($"<color=yellow>[TestClient]</color> Requesting to Start Arc: {arcID_MainQuest01}");
            StoryArcManager.Instance.StartArc(arcID_MainQuest01);
        }

        [ContextMenu("Complete Main Quest 01")]
        public void TestCompleteMainQuest01()
        {
            Debug.Log($"<color=yellow>[TestClient]</color> Requesting to Complete Arc: {arcID_MainQuest01}");
            StoryArcManager.Instance.CompleteArc(arcID_MainQuest01);
        }

        [ContextMenu("Fail Main Quest 01")]
        public void TestFailMainQuest01()
        {
            Debug.Log($"<color=yellow>[TestClient]</color> Requesting to Fail Arc: {arcID_MainQuest01}");
            StoryArcManager.Instance.FailArc(arcID_MainQuest01);
        }

        [ContextMenu("Start Side Quest Healing")]
        public void TestStartSideQuestHealing()
        {
            Debug.Log($"<color=yellow>[TestClient]</color> Requesting to Start Arc: {arcID_SideQuestHealing}");
            StoryArcManager.Instance.StartArc(arcID_SideQuestHealing);
        }

        [ContextMenu("Complete Side Quest Healing")]
        public void TestCompleteSideQuestHealing()
        {
            Debug.Log($"<color=yellow>[TestClient]</color> Requesting to Complete Arc: {arcID_SideQuestHealing}");
            StoryArcManager.Instance.CompleteArc(arcID_SideQuestHealing);
        }

        [ContextMenu("Fail Side Quest Healing")]
        public void TestFailSideQuestHealing()
        {
            Debug.Log($"<color=yellow>[TestClient]</color> Requesting to Fail Arc: {arcID_SideQuestHealing}");
            StoryArcManager.Instance.FailArc(arcID_SideQuestHealing);
        }

        [ContextMenu("Check Main Quest 01 State")]
        public void TestCheckMainQuest01State()
        {
            StoryArcState state = StoryArcManager.Instance.GetArcState(arcID_MainQuest01);
            Debug.Log($"<color=yellow>[TestClient]</color> Current state of '{arcID_MainQuest01}': {state}");
        }
    }
}
```

---

### How to Set Up in Unity:

1.  **Create Folders:**
    *   In your Unity Project window, create a folder structure like `Assets/Scripts/DesignPatterns/StoryArcManager`.
2.  **Add Scripts:**
    *   Place `StoryArcData.cs`, `StoryArcManager.cs`, and `StoryArcTestClient.cs` into the `StoryArcManager` folder.
3.  **Create Story Arc Data Assets:**
    *   In the Project window, right-click -> `Create` -> `StoryArc` -> `Story Arc Data`.
    *   Create three of these, name them meaningfully (e.g., `MainQuest_01_CallToAdventure`, `SideQuest_HealingPotions`, `CharacterDev_RedemptionArc`).
    *   **Configure each `StoryArcData` asset in the Inspector:**
        *   **`MainQuest_01_CallToAdventure`**:
            *   **Arc ID**: `MAIN_QUEST_01`
            *   **Arc Name**: "The Hero's Call to Adventure"
            *   **Description**: "The journey begins, find the Elder."
            *   **Prerequisite Arc IDs**: (Leave empty)
        *   **`SideQuest_HealingPotions`**:
            *   **Arc ID**: `SIDE_QUEST_HEALING`
            *   **Arc Name**: "Gathering Healing Herbs"
            *   **Description**: "Help the village alchemist."
            *   **Prerequisite Arc IDs**: (Leave empty or add `MAIN_QUEST_01` if it needs to be available after the main quest starts)
        *   **`CharacterDev_RedemptionArc`**:
            *   **Arc ID**: `CHAR_DEV_REDEMPTION`
            *   **Arc Name**: "Path to Redemption"
            *   **Description**: "A personal journey to atone for past mistakes."
            *   **Prerequisite Arc IDs**: Add `MAIN_QUEST_01` (This arc can only *start* after the main quest is *completed*).
            *   *Note:* The `StoryArcTestClient` demonstrates starting this arc when `MAIN_QUEST_01` completes, but the `StoryArcManager`'s `StartArc` method also enforces this prerequisite.
4.  **Create the StoryArcManager GameObject:**
    *   In your scene Hierarchy, create an empty GameObject (e.g., `_StoryArcManager`).
    *   Add the `StoryArcManager.cs` component to it.
    *   In the Inspector of `_StoryArcManager`, drag all your `StoryArcData` assets (`MainQuest_01_CallToAdventure`, `SideQuest_HealingPotions`, `CharacterDev_RedemptionArc`) into the `Registered Story Arcs` list.
5.  **Create the Test Client GameObject:**
    *   Create another empty GameObject (e.g., `_StoryArcTestClient`).
    *   Add the `StoryArcTestClient.cs` component to it.
    *   **Crucially, copy the `Arc IDs` from your `StoryArcData` assets into the `Arc ID Main Quest 01`, `Arc ID Side Quest Healing`, and `Arc ID Character Dev Redemption` fields in the `StoryArcTestClient`'s Inspector.** This ensures the test client interacts with the correct arcs.

---

### How to Use and Test:

1.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   Observe the Console window. You'll see the `StoryArcManager` initializing and logging the initial state of all registered arcs.
2.  **Interact with the `_StoryArcTestClient` in the Inspector:**
    *   Select the `_StoryArcTestClient` GameObject in the Hierarchy.
    *   In its Inspector, you'll see several buttons created by `[ContextMenu]`.
    *   **Scenario 1: Basic Arc Flow**
        *   Click `Start Main Quest 01`. Observe the console output: `MAIN_QUEST_01` changes from `NotStarted` to `Active`.
        *   Click `Complete Main Quest 01`. Observe the console: `MAIN_QUEST_01` changes to `Completed`, and importantly, the `StoryArcTestClient`'s event handler will try to `Start Arc: CHAR_DEV_REDEMPTION`.
        *   You'll see `CHAR_DEV_REDEMPTION` also changes from `NotStarted` to `Active` (because its prerequisite `MAIN_QUEST_01` is now `Completed`).
    *   **Scenario 2: Prerequisite Failure**
        *   If you stop and restart the scene, all arcs reset to `NotStarted`.
        *   Try to `Start Character Dev Redemption` directly without completing `Main Quest 01`.
        *   The console will log a warning: `[StoryArcManager] Cannot start arc 'CHAR_DEV_REDEMPTION'. Prerequisite arc 'MAIN_QUEST_01' is not completed.`
    *   **Scenario 3: Side Quest**
        *   Click `Start Side Quest Healing`. It should immediately go `Active`.
        *   Click `Fail Side Quest Healing`. It will go `Failed`.
        *   Try to `Complete Side Quest Healing` now. It will refuse because it's already `Failed`.

This example provides a robust and extensible foundation for managing your game's narrative. You can expand `StoryArcData` with more specific properties (e.g., required player level, associated reward IDs, image for UI), and enhance `StoryArcManager` with methods for saving/loading arc states, or more complex internal progression steps for each arc.