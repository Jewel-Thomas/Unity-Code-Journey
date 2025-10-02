// Unity Design Pattern Example: BookmarkSystem
// This script demonstrates the BookmarkSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The BookmarkSystem design pattern, while not one of the "Gang of Four" patterns, is a common and practical pattern in game development (and other applications) for saving and restoring the state of specific objects or the entire system at designated points. It's closely related to the Memento pattern.

**Core Idea:**
The system allows you to "bookmark" (capture) the current state of one or more objects at a given moment and later "revert" (restore) to that bookmarked state. This is incredibly useful for:
*   Saving/loading player positions, camera views, or character states.
*   Implementing "undo/redo" functionality for editor tools.
*   Creating "checkpoints" or "save points" in games.
*   Resetting objects to a known default state.

---

### BookmarkSystem Pattern in Unity

This example demonstrates a `BookmarkSystem` in Unity by allowing you to bookmark the `Transform` (position, rotation, scale) of game objects and then restore them to those bookmarked states.

**Components:**

1.  **`IBookmarkState`**: A marker interface for any class that represents a capturable state.
2.  **`TransformBookmarkState`**: A concrete implementation of `IBookmarkState` that captures a `GameObject`'s `Transform` data.
3.  **`IBookmarkable`**: An interface for any `MonoBehaviour` that can have its state bookmarked and restored. It defines methods to `SaveState()` and `TryLoadState()`.
4.  **`Bookmark`**: A simple class that encapsulates a name (identifier) and an `IBookmarkState` object.
5.  **`BookmarkManager` (Singleton)**: The central hub that manages a collection of `Bookmark` objects. It provides methods to create, apply, retrieve, and remove bookmarks.
6.  **`BookmarkableGameObject`**: An example `MonoBehaviour` that implements `IBookmarkable`. It automatically registers itself with the `BookmarkManager` and provides context menu options for easy testing in the editor.

---

### Complete C# Unity Script: `BookmarkSystem.cs`

You can create a C# script named `BookmarkSystem.cs` in your Unity project, copy the entire content below into it, and then attach `BookmarkManager` to an empty `GameObject` in your scene.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like .ToList()

namespace DesignPatterns.BookmarkSystem
{
    // ======================================================================================
    // 1. IBookmarkState Interface: Defines what a capturable state is.
    //    This is a marker interface. Concrete state classes will implement this.
    // ======================================================================================
    /// <summary>
    /// Marker interface for any class that represents a capturable state in the BookmarkSystem.
    /// Concrete state classes (e.g., TransformBookmarkState, PlayerHealthState) will implement this.
    /// </summary>
    public interface IBookmarkState { }

    // ======================================================================================
    // 2. TransformBookmarkState Class: Concrete state for a GameObject's Transform.
    // ======================================================================================
    /// <summary>
    /// A concrete implementation of IBookmarkState that captures the position, rotation,
    /// and local scale of a GameObject's Transform.
    /// </summary>
    [Serializable]
    public class TransformBookmarkState : IBookmarkState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public bool isActive; // Also store whether the GameObject was active

        public TransformBookmarkState(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
            localScale = transform.localScale;
            isActive = transform.gameObject.activeSelf;
        }

        public override string ToString()
        {
            return $"Pos: {position}, Rot: {rotation.eulerAngles}, Scale: {localScale}, Active: {isActive}";
        }
    }

    // ======================================================================================
    // 3. IBookmarkable Interface: Defines what an object that can be bookmarked must do.
    // ======================================================================================
    /// <summary>
    /// Interface for any MonoBehaviour that can have its state captured and restored.
    /// </summary>
    public interface IBookmarkable
    {
        /// <summary>
        /// Captures the current state of the object into an IBookmarkState object.
        /// </summary>
        /// <returns>An object representing the current state.</returns>
        IBookmarkState SaveState();

        /// <summary>
        /// Attempts to load and apply a given bookmark state to the object.
        /// The implementing class must cast the state to its expected concrete type.
        /// </summary>
        /// <param name="state">The state to apply.</param>
        /// <returns>True if the state was successfully applied, false otherwise (e.g., wrong state type).</returns>
        bool TryLoadState(IBookmarkState state);

        /// <summary>
        /// A unique identifier for this bookmarkable object within the scene.
        /// This is crucial for the BookmarkManager to know which object to apply a bookmark to.
        /// </summary>
        string UniqueId { get; }
    }

    // ======================================================================================
    // 4. Bookmark Class: Container for a named state.
    // ======================================================================================
    /// <summary>
    /// Represents a single bookmark, holding a name (identifier) and the actual state data.
    /// The IBookmarkState is stored as its concrete serialized type.
    /// </summary>
    [Serializable]
    public class Bookmark
    {
        public string objectId; // The unique ID of the IBookmarkable object this state belongs to.
        public string bookmarkName; // A human-readable name for this specific bookmark.

        // Store the concrete state type and its JSON string.
        // This allows for different types of IBookmarkState to be stored.
        [SerializeField] private string _stateJson;
        [SerializeField] private string _stateTypeFullName;

        // Constructor to create a Bookmark from an IBookmarkable object and a name.
        public Bookmark(string objectId, string bookmarkName, IBookmarkState state)
        {
            this.objectId = objectId;
            this.bookmarkName = bookmarkName;
            _stateTypeFullName = state.GetType().FullName;
            _stateJson = JsonUtility.ToJson(state);
        }

        /// <summary>
        /// Reconstructs the IBookmarkState object from its stored JSON and type information.
        /// </summary>
        /// <returns>The reconstructed IBookmarkState object.</returns>
        public IBookmarkState GetState()
        {
            if (string.IsNullOrEmpty(_stateJson) || string.IsNullOrEmpty(_stateTypeFullName))
            {
                Debug.LogError($"Bookmark '{bookmarkName}' for '{objectId}' has no state data.");
                return null;
            }

            Type stateType = Type.GetType(_stateTypeFullName);
            if (stateType == null)
            {
                Debug.LogError($"Could not find type '{_stateTypeFullName}' for bookmark '{bookmarkName}'. " +
                               $"Has the class been moved or renamed?");
                return null;
            }

            try
            {
                // Use JsonUtility to deserialize the state back into its concrete type.
                return JsonUtility.FromJson(_stateJson, stateType) as IBookmarkState;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize state for bookmark '{bookmarkName}' (Type: {_stateTypeFullName}). Error: {e.Message}");
                return null;
            }
        }
    }

    // ======================================================================================
    // 5. BookmarkManager (Singleton): Manages all bookmarks.
    // ======================================================================================
    /// <summary>
    /// The central manager for all bookmarks in the game.
    /// It's a singleton, meaning there's only one instance throughout the application.
    /// </summary>
    public class BookmarkManager : MonoBehaviour
    {
        public static BookmarkManager Instance { get; private set; }

        // Stores bookmarks using a composite key: "objectId_bookmarkName"
        [SerializeField]
        private List<Bookmark> _allBookmarks = new List<Bookmark>();
        private Dictionary<string, Bookmark> _bookmarkLookup = new Dictionary<string, Bookmark>();

        // Tracks all IBookmarkable objects currently registered in the scene.
        private Dictionary<string, IBookmarkable> _bookmarkableObjects = new Dictionary<string, IBookmarkable>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("BookmarkManager already exists, destroying duplicate.", this);
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep manager alive across scene loads if needed

                // Initialize lookup dictionary from serialized list
                RebuildLookupDictionary();
            }
        }

        private void OnEnable()
        {
            // Re-subscribe if manager was enabled/disabled
            RebuildLookupDictionary();
        }

        /// <summary>
        /// Rebuilds the internal dictionary lookup from the serialized list.
        /// This is important for editor persistence and after deserialization.
        /// </summary>
        private void RebuildLookupDictionary()
        {
            _bookmarkLookup.Clear();
            foreach (var bookmark in _allBookmarks)
            {
                string key = GetBookmarkKey(bookmark.objectId, bookmark.bookmarkName);
                if (_bookmarkLookup.ContainsKey(key))
                {
                    Debug.LogWarning($"Duplicate bookmark key found: {key}. Overwriting existing bookmark.");
                }
                _bookmarkLookup[key] = bookmark;
            }
        }

        /// <summary>
        /// Generates a unique key for a bookmark based on the object's ID and the bookmark's name.
        /// </summary>
        private string GetBookmarkKey(string objectId, string bookmarkName)
        {
            return $"{objectId}_{bookmarkName}";
        }

        /// <summary>
        /// Registers an IBookmarkable object with the manager.
        /// This allows the manager to find and apply bookmarks to specific objects.
        /// Objects should call this during their OnEnable.
        /// </summary>
        public void RegisterBookmarkable(IBookmarkable bookmarkable)
        {
            if (string.IsNullOrEmpty(bookmarkable.UniqueId))
            {
                Debug.LogError($"Bookmarkable object {((MonoBehaviour)bookmarkable).name} has an empty UniqueId! Cannot register.", (MonoBehaviour)bookmarkable);
                return;
            }
            if (_bookmarkableObjects.ContainsKey(bookmarkable.UniqueId))
            {
                Debug.LogWarning($"Bookmarkable object with UniqueId '{bookmarkable.UniqueId}' already registered. " +
                                 $"Overwriting registration. This could indicate a duplicate ID or incorrect lifecycle management.", (MonoBehaviour)bookmarkable);
                _bookmarkableObjects[bookmarkable.UniqueId] = bookmarkable;
            }
            else
            {
                _bookmarkableObjects.Add(bookmarkable.UniqueId, bookmarkable);
            }
            // Debug.Log($"Registered bookmarkable object: {bookmarkable.UniqueId}");
        }

        /// <summary>
        /// Unregisters an IBookmarkable object from the manager.
        /// Objects should call this during their OnDisable or OnDestroy.
        /// </summary>
        public void UnregisterBookmarkable(IBookmarkable bookmarkable)
        {
            if (_bookmarkableObjects.ContainsKey(bookmarkable.UniqueId))
            {
                _bookmarkableObjects.Remove(bookmarkable.UniqueId);
                // Debug.Log($"Unregistered bookmarkable object: {bookmarkable.UniqueId}");
            }
        }

        /// <summary>
        /// Creates and stores a new bookmark for a specified IBookmarkable object.
        /// </summary>
        /// <param name="target">The object whose state is to be bookmarked.</param>
        /// <param name="bookmarkName">A unique name for this specific bookmark.</param>
        public void CreateBookmark(IBookmarkable target, string bookmarkName)
        {
            if (target == null)
            {
                Debug.LogError("Cannot create bookmark: Target object is null.");
                return;
            }
            if (string.IsNullOrWhiteSpace(bookmarkName))
            {
                Debug.LogError("Cannot create bookmark: Bookmark name cannot be empty.");
                return;
            }
            if (string.IsNullOrEmpty(target.UniqueId))
            {
                Debug.LogError($"Cannot create bookmark for {((MonoBehaviour)target).name}: Target object has no UniqueId.", (MonoBehaviour)target);
                return;
            }

            IBookmarkState state = target.SaveState();
            if (state == null)
            {
                Debug.LogError($"Failed to save state for object '{target.UniqueId}'. Bookmark not created.", (MonoBehaviour)target);
                return;
            }

            Bookmark newBookmark = new Bookmark(target.UniqueId, bookmarkName, state);
            string key = GetBookmarkKey(target.UniqueId, bookmarkName);

            // Add or update the bookmark
            if (_bookmarkLookup.ContainsKey(key))
            {
                Debug.Log($"Updating existing bookmark '{bookmarkName}' for object '{target.UniqueId}'.");
                _allBookmarks.Remove(_bookmarkLookup[key]); // Remove old entry from list
            }
            _bookmarkLookup[key] = newBookmark;
            _allBookmarks.Add(newBookmark); // Add new entry to list

            Debug.Log($"Bookmark '{bookmarkName}' created/updated for object '{target.UniqueId}'. State: {state.ToString()}");
        }

        /// <summary>
        /// Applies a previously saved bookmark to its corresponding IBookmarkable object.
        /// </summary>
        /// <param name="objectId">The UniqueId of the object to apply the bookmark to.</param>
        /// <param name="bookmarkName">The name of the bookmark to apply.</param>
        public void ApplyBookmark(string objectId, string bookmarkName)
        {
            string key = GetBookmarkKey(objectId, bookmarkName);
            if (!_bookmarkLookup.TryGetValue(key, out Bookmark bookmark))
            {
                Debug.LogWarning($"Bookmark '{bookmarkName}' for object '{objectId}' not found.");
                return;
            }

            if (!_bookmarkableObjects.TryGetValue(objectId, out IBookmarkable targetObject))
            {
                Debug.LogError($"Target object with UniqueId '{objectId}' is not registered with the BookmarkManager. " +
                               $"Make sure it's active in the scene and registers itself.");
                return;
            }

            IBookmarkState state = bookmark.GetState();
            if (state == null)
            {
                Debug.LogError($"Failed to retrieve state for bookmark '{bookmarkName}' for object '{objectId}'.");
                return;
            }

            if (targetObject.TryLoadState(state))
            {
                Debug.Log($"Bookmark '{bookmarkName}' applied to object '{objectId}'. State: {state.ToString()}");
            }
            else
            {
                Debug.LogError($"Failed to apply bookmark '{bookmarkName}' to object '{objectId}'. " +
                               $"Object '{objectId}' could not load the provided state type.");
            }
        }

        /// <summary>
        /// Removes a bookmark from the system.
        /// </summary>
        /// <param name="objectId">The UniqueId of the object the bookmark belongs to.</param>
        /// <param name="bookmarkName">The name of the bookmark to remove.</param>
        public void RemoveBookmark(string objectId, string bookmarkName)
        {
            string key = GetBookmarkKey(objectId, bookmarkName);
            if (_bookmarkLookup.ContainsKey(key))
            {
                Bookmark bookmarkToRemove = _bookmarkLookup[key];
                _bookmarkLookup.Remove(key);
                _allBookmarks.Remove(bookmarkToRemove); // Also remove from the serialized list
                Debug.Log($"Bookmark '{bookmarkName}' for object '{objectId}' removed.");
            }
            else
            {
                Debug.LogWarning($"Bookmark '{bookmarkName}' for object '{objectId}' not found, cannot remove.");
            }
        }

        /// <summary>
        /// Gets a list of all bookmark names for a specific object.
        /// </summary>
        /// <param name="objectId">The UniqueId of the object.</param>
        /// <returns>A list of bookmark names.</returns>
        public List<string> GetBookmarkNamesForObject(string objectId)
        {
            return _allBookmarks
                    .Where(b => b.objectId == objectId)
                    .Select(b => b.bookmarkName)
                    .ToList();
        }

        /// <summary>
        /// Gets a list of all unique object IDs that have bookmarks.
        /// </summary>
        /// <returns>A list of object IDs.</returns>
        public List<string> GetAllBookmarkedObjectIds()
        {
            return _allBookmarks.Select(b => b.objectId).Distinct().ToList();
        }

        /// <summary>
        /// Clears all bookmarks from the manager.
        /// </summary>
        [ContextMenu("Clear All Bookmarks")]
        public void ClearAllBookmarks()
        {
            _allBookmarks.Clear();
            _bookmarkLookup.Clear();
            Debug.Log("All bookmarks cleared from BookmarkManager.");
        }
    }


    // ======================================================================================
    // 6. BookmarkableGameObject Class: An example concrete MonoBehaviour that uses the system.
    // ======================================================================================
    /// <summary>
    /// An example MonoBehaviour that implements IBookmarkable for its Transform component.
    /// Attach this script to any GameObject you want to be able to bookmark its position/rotation/scale.
    /// </summary>
    public class BookmarkableGameObject : MonoBehaviour, IBookmarkable
    {
        [Tooltip("A unique identifier for this object. Crucial for bookmarking.")]
        [SerializeField] private string _uniqueId;

        [Tooltip("The name for the bookmark when using the context menu actions.")]
        public string ContextBookmarkName = "DefaultBookmark";

        public string UniqueId => _uniqueId;

        void OnValidate()
        {
            // Ensure a unique ID exists, or generate one if empty in editor
            if (string.IsNullOrEmpty(_uniqueId))
            {
                _uniqueId = Guid.NewGuid().ToString();
                Debug.Log($"Generated new UniqueId for {gameObject.name}: {_uniqueId}", this);
            }
        }

        void OnEnable()
        {
            // Register with the BookmarkManager when enabled
            if (BookmarkManager.Instance != null)
            {
                BookmarkManager.Instance.RegisterBookmarkable(this);
            }
            else
            {
                Debug.LogWarning("BookmarkManager not found in scene. Bookmarking will not work.", this);
            }
        }

        void OnDisable()
        {
            // Unregister when disabled to prevent stale references
            if (BookmarkManager.Instance != null && !ReferenceEquals(BookmarkManager.Instance, null)) // Check for null before accessing Instance
            {
                BookmarkManager.Instance.UnregisterBookmarkable(this);
            }
        }

        // Implementation of IBookmarkable.SaveState()
        public IBookmarkState SaveState()
        {
            // Capture the current Transform state
            return new TransformBookmarkState(transform);
        }

        // Implementation of IBookmarkable.TryLoadState()
        public bool TryLoadState(IBookmarkState state)
        {
            // Check if the provided state is of the expected type
            if (state is TransformBookmarkState transformState)
            {
                // Apply the saved Transform state
                transform.position = transformState.position;
                transform.rotation = transformState.rotation;
                transform.localScale = transformState.localScale;
                gameObject.SetActive(transformState.isActive);
                return true;
            }
            Debug.LogWarning($"Attempted to load incompatible state type {state.GetType().Name} into {gameObject.name}.", this);
            return false;
        }

        // ======================================================================================
        // Editor Context Menu for easy testing
        // ======================================================================================

        [ContextMenu("Save Current Transform as Bookmark")]
        private void EditorSaveBookmark()
        {
            if (BookmarkManager.Instance != null)
            {
                BookmarkManager.Instance.CreateBookmark(this, ContextBookmarkName);
            }
            else
            {
                Debug.LogError("BookmarkManager not found in scene.");
            }
        }

        [ContextMenu("Load Transform from Bookmark")]
        private void EditorLoadBookmark()
        {
            if (BookmarkManager.Instance != null)
            {
                BookmarkManager.Instance.ApplyBookmark(UniqueId, ContextBookmarkName);
            }
            else
            {
                Debug.LogError("BookmarkManager not found in scene.");
            }
        }

        [ContextMenu("Remove Bookmark")]
        private void EditorRemoveBookmark()
        {
            if (BookmarkManager.Instance != null)
            {
                BookmarkManager.Instance.RemoveBookmark(UniqueId, ContextBookmarkName);
            }
            else
            {
                Debug.LogError("BookmarkManager not found in scene.");
            }
        }
    }
}
```

---

### How to Use in Unity

1.  **Create the Script:** Save the code above as `BookmarkSystem.cs` in your Unity project's Assets folder.
2.  **Create BookmarkManager:** Create an empty `GameObject` in your scene, rename it to `_BookmarkManager`, and attach the `BookmarkManager` script to it.
3.  **Make Objects Bookmarkable:**
    *   Create a few primitive `GameObject`s (e.g., Cubes, Spheres).
    *   Attach the `BookmarkableGameObject` script to each of them.
    *   **Crucially:** Ensure each `BookmarkableGameObject` has a unique `_uniqueId`. The `OnValidate` method will auto-generate one if it's empty, but you can also set them manually (e.g., "Player", "CameraView1", "PuzzlePieceA").
4.  **Test in Editor (Context Menu):**
    *   Select one of your `BookmarkableGameObject`s.
    *   In the Inspector, change its `ContextBookmarkName` (e.g., to "MyFirstSpot").
    *   Move, rotate, and scale the `GameObject` to a desired position.
    *   Right-click on the `BookmarkableGameObject` script component in the Inspector (or click the gear icon) and select "Save Current Transform as Bookmark".
    *   Move the `GameObject` to a different position.
    *   Right-click again and select "Load Transform from Bookmark". The object should snap back to the saved position.
    *   Repeat with different `ContextBookmarkName`s for the same object to save multiple bookmarks.
    *   You can also save bookmarks for different `BookmarkableGameObject`s.
5.  **Test Programmatically (Runtime):**

    ```csharp
    // Example usage in another script (e.g., a PlayerController or UI Manager)
    using UnityEngine;
    using DesignPatterns.BookmarkSystem; // Don't forget this using statement!

    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private BookmarkableGameObject playerBookmarkable;
        [SerializeField] private BookmarkableGameObject cameraBookmarkable;

        void Update()
        {
            // Example: Save player position on 'S' key
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (playerBookmarkable != null)
                {
                    BookmarkManager.Instance.CreateBookmark(playerBookmarkable, "PlayerCheckpoint1");
                    Debug.Log("Player checkpoint saved!");
                }
            }

            // Example: Load player position on 'L' key
            if (Input.GetKeyDown(KeyCode.L))
            {
                if (playerBookmarkable != null)
                {
                    BookmarkManager.Instance.ApplyBookmark(playerBookmarkable.UniqueId, "PlayerCheckpoint1");
                    Debug.Log("Player checkpoint loaded!");
                }
            }

            // Example: Save current camera view on 'C' key
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (cameraBookmarkable != null)
                {
                    BookmarkManager.Instance.CreateBookmark(cameraBookmarkable, "MainCameraView");
                    Debug.Log("Camera view saved!");
                }
            }

            // Example: Load camera view on 'V' key
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (cameraBookmarkable != null)
                {
                    BookmarkManager.Instance.ApplyBookmark(cameraBookmarkable.UniqueId, "MainCameraView");
                    Debug.Log("Camera view loaded!");
                }
            }

            // Example: List all bookmarks for the player
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (playerBookmarkable != null)
                {
                    List<string> playerBookmarks = BookmarkManager.Instance.GetBookmarkNamesForObject(playerBookmarkable.UniqueId);
                    Debug.Log($"Player Bookmarks: {string.Join(", ", playerBookmarks)}");
                }
            }
        }
    }
    ```
    *   Create a new script named `GameStateManager.cs`.
    *   Copy the above example usage into it.
    *   Attach `GameStateManager` to an empty `GameObject`.
    *   Drag your `BookmarkableGameObject` (e.g., your player) to the `playerBookmarkable` slot in the Inspector.
    *   Drag your `Main Camera` (after adding `BookmarkableGameObject` to it) to the `cameraBookmarkable` slot.
    *   Run the game and use the 'S', 'L', 'C', 'V', 'P' keys.

---

### Key Design Pattern Concepts Demonstrated:

*   **Memento (closely related):** The `IBookmarkState` and `TransformBookmarkState` act as memento objects, storing the internal state of `IBookmarkable` objects without exposing their internal structure.
*   **Command (could be combined):** While not explicitly using the Command pattern here, creating/applying bookmarks could easily be wrapped into `Command` objects for more complex undo/redo systems or action queuing.
*   **Observer/Publish-Subscribe (optional but useful):** The `BookmarkManager` could notify UI elements when bookmarks are added/removed, allowing for dynamic updates.
*   **Singleton:** The `BookmarkManager` uses the Singleton pattern to provide a single, globally accessible point of control for bookmark operations.
*   **Strategy (implicitly):** Different `IBookmarkable` implementations can define their own strategies for saving and loading their specific states.
*   **Encapsulation:** The state itself (`TransformBookmarkState`) is a plain data object, separate from the `BookmarkableGameObject` that creates and consumes it. The `BookmarkManager` doesn't need to know the *details* of the state, only that it implements `IBookmarkState`.
*   **Polymorphism:** The `BookmarkManager` works with `IBookmarkable` and `IBookmarkState` interfaces, allowing it to manage bookmarks for any type of object that implements these interfaces, promoting flexibility and extensibility.

This example provides a robust and extensible foundation for a bookmarking system in your Unity projects. You can easily extend it by creating new `IBookmarkState` implementations (e.g., for player health, inventory, quest progress) and new `IBookmarkable` components.