// Unity Design Pattern Example: PortalTransitionSystem
// This script demonstrates the PortalTransitionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PortalTransitionSystem' design pattern in Unity provides a robust and flexible way to manage transitions between different scenes or areas within a game, often involving a visual "portal" or entry/exit point. It centralizes the logic for loading scenes, placing players at specific destinations, and handling transitional effects.

This example demonstrates the pattern using three core components:

1.  **`PortalTransitionManager` (Singleton):** The central authority for initiating and managing scene transitions. It persists across scene loads, holds the destination information, and orchestrates the loading process, including any loading screens or fade effects.
2.  **`Portal` (MonoBehaviour):** A component attached to specific GameObjects in a scene that act as the entry/exit points. Each portal has a unique ID and knows which scene and which portal ID to target for its destination. When a player enters its trigger, it instructs the `PortalTransitionManager` to initiate a transition.
3.  **`PlayerPortalHandler` (MonoBehaviour):** A component on the player character that reacts to a new scene being loaded via a portal transition. It queries the `PortalTransitionManager` for its intended spawn point and repositions the player accordingly.

---

### Key Concepts of the PortalTransitionSystem Pattern:

*   **Centralized Control:** The `PortalTransitionManager` acts as a singleton, ensuring there's one consistent way to handle all portal-based scene transitions.
*   **Decoupling:** Portals only know *what* scene and *which* portal ID to go to; they don't directly load scenes or move the player. They delegate this responsibility to the manager. The player only knows how to position itself based on information from the manager.
*   **Data Persistence Across Scenes:** The `PortalTransitionManager` holds the `TargetPortalID` across scene loads, making it accessible to the player in the new scene.
*   **Extensibility:** Easily add more complex loading animations, transition types, or additional data transfer (e.g., player state, quest progress) without altering the core `Portal` logic.
*   **Scene Management Integration:** Leverages Unity's `SceneManager` for asynchronous scene loading.

---

### Unity Setup Instructions for this Example:

1.  **Create New Project:** Start a new Unity 3D project.
2.  **Create Scenes:**
    *   Go to `File > New Scene`. Save it as `Assets/Scenes/SceneA.unity`.
    *   Repeat, saving another as `Assets/Scenes/SceneB.unity`.
3.  **Add Scenes to Build Settings:**
    *   Go to `File > Build Settings...`.
    *   Drag `SceneA` and `SceneB` from the `Assets/Scenes` folder into the "Scenes In Build" list. Ensure `SceneA` is index 0 and `SceneB` is index 1 (or any order, but note their names).
4.  **Create Loading Screen Prefab:**
    *   Create a new UI Canvas: `GameObject > UI > Canvas`. Rename it `LoadingScreenCanvas`.
    *   Set its `Render Mode` to `Screen Space - Overlay`.
    *   Add a `CanvasGroup` component to `LoadingScreenCanvas` (`Add Component > Layout > Canvas Group`). This is used for fading.
    *   Add a UI Panel as a child: `Right-click LoadingScreenCanvas > UI > Panel`. Set its `color` to black (or any dark color). Maximize its rect transform to cover the screen.
    *   Add a UI Text as a child of the Panel: `Right-click Panel > UI > Text - TextMeshPro`. Import TMP Essentials if prompted. Change text to "LOADING..." and center it, make it larger/white.
    *   Disable the `LoadingScreenCanvas` GameObject (uncheck it in the Inspector).
    *   Drag `LoadingScreenCanvas` from the Hierarchy into `Assets/Resources` folder (create if it doesn't exist). This allows the manager to load it by name.
    *   Delete `LoadingScreenCanvas` from the Hierarchy.
5.  **Create Scripts:**
    *   Create a new folder `Assets/Scripts`.
    *   Create three C# scripts inside: `PortalTransitionManager.cs`, `Portal.cs`, `PlayerPortalHandler.cs`.
    *   Copy and paste the code provided below into the respective files.
6.  **Setup SceneA:**
    *   **Player:**
        *   Create a `Capsule` (`GameObject > 3D Object > Capsule`). Rename it `Player`.
        *   Give it the tag "Player" (`Tag > Add Tag...` if "Player" doesn't exist, then select it).
        *   Add a `Rigidbody` component to `Player` (`Add Component > Physics > Rigidbody`). Uncheck `Use Gravity` for simplicity, or add basic player movement.
        *   Add `PlayerPortalHandler.cs` to `Player`.
    *   **Portal_A_to_B:**
        *   Create an `Empty GameObject` (`GameObject > Create Empty`). Rename it `Portal_A_to_B`.
        *   Add a `Box Collider` (`Add Component > Physics > Box Collider`).
        *   Check `Is Trigger` on the Box Collider.
        *   Adjust `Size` (e.g., X=2, Y=3, Z=1) to make it a visible "doorway".
        *   Add `Portal.cs` to `Portal_A_to_B`.
        *   Set its properties in the Inspector:
            *   `Portal ID`: `EntryA` (This portal's unique ID in SceneA)
            *   `Target Scene Name`: `SceneB`
            *   `Target Portal ID`: `EntryB` (The ID of the portal it connects to in SceneB)
        *   **Spawn Point:** Create an `Empty GameObject` as a child of `Portal_A_to_B`. Rename it `SpawnPoint`. Position it slightly *in front* of `Portal_A_to_B` where you want the player to land. Assign this `SpawnPoint` transform to the `Spawn Point` field in the `Portal_A_to_B`'s Inspector.
    *   **Manager:**
        *   Create an `Empty GameObject`. Rename it `_PortalTransitionManager`.
        *   Add `PortalTransitionManager.cs` to it.
        *   Drag the `LoadingScreenCanvas` prefab from `Assets/Resources` to the `Loading Screen Prefab` field in the Inspector.
        *   **Important:** Place `_PortalTransitionManager` at the root of your scene.
    *   **Ground:** Add a simple `Cube` as ground.
    *   Save `SceneA`.
7.  **Setup SceneB:**
    *   Open `SceneB`.
    *   **Player:** (The manager will move the player from SceneA, but for initial testing, you might place one. For the actual system, the player from SceneA will be moved here.)
    *   **Portal_B_to_A:**
        *   Create an `Empty GameObject`. Rename it `Portal_B_to_A`.
        *   Add a `Box Collider`. Check `Is Trigger`. Adjust `Size`.
        *   Add `Portal.cs` to `Portal_B_to_A`.
        *   Set its properties:
            *   `Portal ID`: `EntryB`
            *   `Target Scene Name`: `SceneA`
            *   `Target Portal ID`: `EntryA`
        *   **Spawn Point:** Create an `Empty GameObject` as a child of `Portal_B_to_A`. Rename it `SpawnPoint`. Position it slightly *in front* of `Portal_B_to_A`. Assign this `SpawnPoint` transform to the `Spawn Point` field in the `Portal_B_to_A`'s Inspector.
    *   **Manager:**
        *   Create an `Empty GameObject`. Rename it `_PortalTransitionManager`.
        *   Add `PortalTransitionManager.cs` to it.
        *   Drag the `LoadingScreenCanvas` prefab from `Assets/Resources` to the `Loading Screen Prefab` field.
        *   **Important:** This `_PortalTransitionManager` will be destroyed by the singleton pattern when `_PortalTransitionManager` from `SceneA` loads. It's only placed in `SceneB` to assign the prefab easily. *Alternatively, you can skip adding it to SceneB, the one from SceneA will persist.*
    *   Save `SceneB`.
8.  **Play:** Start `SceneA`. Move your player into `Portal_A_to_B`. You should transition to `SceneB` and appear at `Portal_B_to_A`. Move back to `Portal_A_to_B`.

---

### `PortalTransitionManager.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq; // For LINQ queries to find portals

/// <summary>
/// PortalTransitionManager: A singleton responsible for managing scene transitions via portals.
/// This script persists across scene loads and orchestrates the loading process,
/// including displaying a loading screen and storing the target portal ID.
/// </summary>
public class PortalTransitionManager : MonoBehaviour
{
    // Singleton instance for easy global access
    public static PortalTransitionManager Instance { get; private set; }

    [Header("Loading Screen Settings")]
    [Tooltip("Prefab of the UI Canvas for the loading screen.")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [Tooltip("Duration of the fade-in/out effect for the loading screen.")]
    [SerializeField] private float fadeDuration = 0.5f;

    // Internal reference to the instantiated loading screen
    private GameObject _currentLoadingScreenInstance;
    private CanvasGroup _loadingCanvasGroup;

    // Stores the ID of the portal to spawn at in the *next* scene.
    // This value is set before loading a new scene and cleared after the player has spawned.
    public string TargetPortalID { get; private set; } = null;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern to ensure only one instance exists and persists.
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one.
            Debug.LogWarning("Duplicate PortalTransitionManager found, destroying this one.");
            Destroy(gameObject);
        }
        else
        {
            // Set this as the singleton instance and ensure it persists across scene loads.
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("PortalTransitionManager initialized.");
        }
    }

    /// <summary>
    /// Initiates a scene transition to a specified scene, targeting a specific portal ID.
    /// </summary>
    /// <param name="targetSceneName">The name of the scene to load.</param>
    /// <param name="targetPortalID">The ID of the Portal in the target scene where the player should spawn.</param>
    public void InitiateTransition(string targetSceneName, string targetPortalID)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene name cannot be empty for portal transition.");
            return;
        }
        if (string.IsNullOrEmpty(targetPortalID))
        {
            Debug.LogError("Target portal ID cannot be empty for portal transition.");
            return;
        }

        Debug.Log($"Initiating transition to scene: {targetSceneName}, targeting portal: {targetPortalID}");

        // Store the target portal ID for the PlayerPortalHandler in the new scene to pick up.
        TargetPortalID = targetPortalID;

        // Start the asynchronous scene loading and transition process.
        StartCoroutine(LoadSceneAsyncAndTransition(targetSceneName));
    }

    /// <summary>
    /// Coroutine to handle asynchronous scene loading and loading screen effects.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    private IEnumerator LoadSceneAsyncAndTransition(string sceneName)
    {
        // 1. Display and fade in loading screen
        yield return StartCoroutine(DisplayLoadingScreen());

        // 2. Asynchronously load the new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // While the scene is loading, keep the loading screen active.
        while (!asyncLoad.isDone)
        {
            // You could update a loading progress bar here if needed.
            // Debug.Log($"Loading progress: {asyncLoad.progress * 100}%");
            yield return null;
        }

        Debug.Log($"Scene '{sceneName}' loaded successfully.");

        // After the scene is loaded, the PlayerPortalHandler in the new scene
        // will pick up the TargetPortalID from this manager and reposition the player.

        // 3. Fade out and hide loading screen
        yield return StartCoroutine(HideLoadingScreen());

        // 4. Clear the target portal ID after the transition is complete
        // This is crucial to prevent unintended spawns if a scene is loaded directly later
        // or if another system attempts to query TargetPortalID without a portal transition.
        TargetPortalID = null;
        Debug.Log("Portal transition complete.");
    }

    /// <summary>
    /// Displays the loading screen and fades it in.
    /// </summary>
    private IEnumerator DisplayLoadingScreen()
    {
        if (loadingScreenPrefab == null)
        {
            Debug.LogWarning("Loading Screen Prefab is not assigned. Skipping loading screen display.");
            yield break;
        }

        // Instantiate the loading screen if it doesn't exist.
        if (_currentLoadingScreenInstance == null)
        {
            _currentLoadingScreenInstance = Instantiate(loadingScreenPrefab);
            // Ensure the loading screen is a child of this manager so it also persists.
            _currentLoadingScreenInstance.transform.SetParent(transform); 
            _loadingCanvasGroup = _currentLoadingScreenInstance.GetComponent<CanvasGroup>();
            if (_loadingCanvasGroup == null)
            {
                Debug.LogError("Loading Screen Prefab must have a CanvasGroup component!");
                yield break;
            }
        }

        // Set initial state for fading in
        _currentLoadingScreenInstance.SetActive(true);
        _loadingCanvasGroup.alpha = 0f;
        _loadingCanvasGroup.blocksRaycasts = true; // Block input while loading

        float timer = 0f;
        while (timer < fadeDuration)
        {
            _loadingCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        _loadingCanvasGroup.alpha = 1f; // Ensure fully opaque
    }

    /// <summary>
    /// Hides the loading screen by fading it out.
    /// </summary>
    private IEnumerator HideLoadingScreen()
    {
        if (_currentLoadingScreenInstance == null || _loadingCanvasGroup == null)
        {
            yield break; // Nothing to hide
        }

        float timer = 0f;
        while (timer < fadeDuration)
        {
            _loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        _loadingCanvasGroup.alpha = 0f; // Ensure fully transparent

        _loadingCanvasGroup.blocksRaycasts = false; // Allow input again
        _currentLoadingScreenInstance.SetActive(false); // Deactivate the loading screen GameObject
    }
}
```

### `Portal.cs`

```csharp
using UnityEngine;

/// <summary>
/// Portal: A component attached to GameObjects that act as entry/exit points between scenes.
/// It defines its own unique ID and specifies its target scene and target portal ID.
/// When a player enters its trigger, it instructs the PortalTransitionManager to initiate a transition.
/// </summary>
[RequireComponent(typeof(Collider))] // Portals need a collider to detect player entry
public class Portal : MonoBehaviour
{
    [Header("Portal Identification")]
    [Tooltip("Unique ID for this portal within its scene. Must match 'Target Portal ID' in connecting portals.")]
    [SerializeField] private string portalID;

    [Header("Destination Settings")]
    [Tooltip("The name of the scene to transition to.")]
    [SerializeField] private string targetSceneName;
    [Tooltip("The ID of the portal in the target scene where the player should spawn.")]
    [SerializeField] private string targetPortalID;

    [Tooltip("The Transform that defines where the player will spawn when arriving via this portal.")]
    [SerializeField] private Transform spawnPoint;

    // Public properties to allow other scripts (like PlayerPortalHandler) to access this portal's data.
    public string PortalID => portalID;
    public string TargetSceneName => targetSceneName;
    public string TargetPortalID => targetPortalID;
    public Transform SpawnPoint => spawnPoint;

    /// <summary>
    /// Ensures the collider is a trigger and provides a visual cue in the editor.
    /// </summary>
    void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"Collider on Portal '{gameObject.name}' is not set to 'Is Trigger'. Setting it automatically.");
            col.isTrigger = true;
        }

        // Warn if essential fields are not set
        if (string.IsNullOrEmpty(portalID))
        {
            Debug.LogError($"Portal '{gameObject.name}' has an empty Portal ID. This is required for unique identification.");
        }
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"Portal '{gameObject.name}' has an empty Target Scene Name.");
        }
        if (string.IsNullOrEmpty(targetPortalID))
        {
            Debug.LogError($"Portal '{gameObject.name}' has an empty Target Portal ID.");
        }
        if (spawnPoint == null)
        {
            Debug.LogWarning($"Portal '{gameObject.name}' has no Spawn Point assigned. Player will spawn at the portal's position.");
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        // Check if the entering collider is the player
        // (Assumes player GameObject has the tag "Player")
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered Portal '{portalID}'. Initiating transition to '{targetSceneName}'...");

            // Instruct the PortalTransitionManager to handle the scene transition.
            // The manager will store 'targetPortalID' and load 'targetSceneName'.
            PortalTransitionManager.Instance.InitiateTransition(targetSceneName, targetPortalID);
        }
    }

    /// <summary>
    /// Draw gizmos in the editor to visualize the portal's trigger and spawn point.
    /// </summary>
    void OnDrawGizmos()
    {
        // Visualize the portal's trigger area
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan, semi-transparent
            if (col is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity; // Reset matrix
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
            // Add other collider types if needed
        }

        // Visualize the spawn point
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green; // Green for spawn point
            Gizmos.DrawSphere(spawnPoint.position, 0.3f); // Small sphere at spawn point
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 1f); // Arrow indicating forward direction
        }
        else
        {
            // If no explicit spawn point, use the portal's position as a fallback hint
            Gizmos.color = Color.yellow; // Yellow for unassigned spawn point
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawRay(transform.position, transform.forward * 1f);
        }
    }
}
```

### `PlayerPortalHandler.cs`

```csharp
using UnityEngine;
using System.Linq; // For LINQ queries

/// <summary>
/// PlayerPortalHandler: A component attached to the player character.
/// It's responsible for repositioning the player when a new scene is loaded
/// via a portal transition. It queries the PortalTransitionManager for the
/// intended spawn point and moves the player there.
/// </summary>
public class PlayerPortalHandler : MonoBehaviour
{
    [Tooltip("The tag assigned to the player GameObject. Used for trigger checks.")]
    [SerializeField] private string playerTag = "Player";

    // Reference to the CharacterController if one is used for player movement.
    private CharacterController _characterController;

    void Awake()
    {
        // Get the CharacterController component if it exists on the player.
        _characterController = GetComponent<CharacterController>();

        // Ensure the GameObject has the correct tag.
        if (!gameObject.CompareTag(playerTag))
        {
            Debug.LogWarning($"PlayerPortalHandler on '{gameObject.name}' expects tag '{playerTag}', but has tag '{gameObject.tag}'. Please correct the tag.");
        }
    }

    /// <summary>
    /// Called when the script starts (after Awake).
    /// This is where the player checks if it's arriving from a portal transition
    /// and repositions itself if necessary.
    /// </summary>
    void Start()
    {
        // Check if there's an active PortalTransitionManager instance and if a target portal ID is set.
        // This indicates that the current scene was loaded via a portal transition.
        if (PortalTransitionManager.Instance != null && !string.IsNullOrEmpty(PortalTransitionManager.Instance.TargetPortalID))
        {
            string targetPortalID = PortalTransitionManager.Instance.TargetPortalID;
            Debug.Log($"Player arrived in new scene. Looking for spawn portal with ID: {targetPortalID}");

            // Find all Portal components in the current scene.
            // Using FindObjectsOfType<T>() can be slow if there are many objects,
            // but for a few portals per scene, it's generally acceptable.
            Portal[] portalsInScene = FindObjectsOfType<Portal>();

            // Find the specific Portal that matches the targetPortalID.
            Portal destinationPortal = portalsInScene.FirstOrDefault(p => p.PortalID == targetPortalID);

            if (destinationPortal != null)
            {
                // Determine the actual spawn position and rotation.
                // If a specific SpawnPoint Transform is assigned to the portal, use that.
                // Otherwise, use the portal's own transform.
                Transform spawnTransform = destinationPortal.SpawnPoint != null ? destinationPortal.SpawnPoint : destinationPortal.transform;

                Debug.Log($"Found destination portal '{destinationPortal.PortalID}'. Spawning player at {spawnTransform.position}");
                
                // Reposition the player.
                // If using CharacterController, disable it temporarily for direct position set.
                if (_characterController != null)
                {
                    _characterController.enabled = false;
                    transform.position = spawnTransform.position;
                    transform.rotation = spawnTransform.rotation;
                    _characterController.enabled = true;
                }
                else
                {
                    // For other player movement systems (e.g., Rigidbody or direct Transform manipulation)
                    transform.position = spawnTransform.position;
                    transform.rotation = spawnTransform.rotation;
                    
                    // If using Rigidbody, ensure its velocity is reset to prevent carrying over momentum.
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
            else
            {
                Debug.LogError($"Could not find target portal with ID '{targetPortalID}' in the current scene. Player might be in the wrong location!");
                // Optionally, fall back to a default spawn point if the target portal isn't found.
            }

            // The TargetPortalID is cleared by the PortalTransitionManager after the transition coroutine finishes.
            // So, no need to clear it here.
        }
        else
        {
            Debug.Log("Player did not arrive via portal transition or manager not available.");
            // This case handles initial scene loading or non-portal scene changes.
            // The player remains at its initial position for this scene.
        }
    }
}
```