// Unity Design Pattern Example: LocalCoopSystem
// This script demonstrates the LocalCoopSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'LocalCoopSystem' pattern in Unity. This pattern acts as a central manager for all aspects of local cooperative gameplay, including player management, shared resources, and coordinating interactions between multiple local players.

**Core Idea of the LocalCoopSystem Pattern:**

The LocalCoopSystem pattern centralizes the management of local cooperative gameplay elements. It typically involves:

1.  **Central Manager (Service):** A dedicated class (often a Singleton) that acts as the single point of contact for local co-op related functionalities.
2.  **Player Registration/Tracking:** It keeps track of all active local players, usually by responding to Unity's `PlayerInputManager` events.
3.  **Shared State/Resources:** It manages game state or resources that are common to all local players (e.g., a collective score, a shared health pool, game progression).
4.  **Coordinated Interactions:** It provides methods for individual players to interact with the shared system or for the system to react to global co-op events.
5.  **Input Abstraction:** Leverages Unity's Input System (`PlayerInputManager`) to abstract away individual player input devices, presenting a unified interface for player actions.

**Why use it?**
*   **Decoupling:** Player scripts don't need to know about each other; they only interact with the `LocalCoopSystem`.
*   **Centralized Logic:** All co-op specific logic (scoring, health, objectives) is in one place, making it easier to manage and modify.
*   **Scalability:** Easily add more players or shared resources without significantly altering existing player logic.
*   **Clear Responsibility:** The `LocalCoopSystem` has a clear, singular responsibility for the co-op experience.

---

### **Project Setup Guide (Before running the code):**

1.  **Create a new Unity Project.**
2.  **Install Input System Package:**
    *   Go to `Window > Package Manager`.
    *   Select `Unity Registry`.
    *   Search for "Input System" and install it.
    *   Unity will ask to enable the new input backend; click "Yes".
3.  **Create an Input Action Asset:**
    *   In your `Project` window, right-click `Create > Input Actions`. Name it `PlayerControls`.
    *   Double-click `PlayerControls` to open the Input Action Editor.
    *   **Action Maps:** Create one `Action Map` named `Player`.
    *   **Actions:**
        *   Add `Move` (Type: `Value`, Control Type: `Vector2`).
            *   Bindings:
                *   `<Gamepad>/leftStick`
                *   `<Keyboard>/w` (Up)
                *   `<Keyboard>/s` (Down)
                *   `<Keyboard>/a` (Left)
                *   `<Keyboard>/d` (Right)
                *   `<Keyboard>/upArrow` (Up)
                *   `<Keyboard>/downArrow` (Down)
                *   `<Keyboard>/leftArrow` (Left)
                *   `<Keyboard>/rightArrow` (Right)
        *   Add `Collect` (Type: `Button`).
            *   Bindings:
                *   `<Gamepad>/buttonSouth` (e.g., A on Xbox, X on PlayStation)
                *   `<Keyboard>/space`
    *   Click `Save Asset`.
    *   Check `Generate C# Class` in the Input Action Editor's properties panel and click `Apply`.
4.  **Create UI for Shared Score:**
    *   Right-click in Hierarchy `UI > Canvas`.
    *   Right-click on Canvas `UI > Text - TextMeshPro`. (If prompted, import TMP Essentials).
    *   Name the Text object `SharedScoreText`.
    *   Set its `Rect Transform` to anchor at `Top-Left` and position it clearly (e.g., X: 10, Y: -10).
    *   Set its `Text` to "Shared Score: 0".
    *   Increase `Font Size` for visibility (e.g., 24).
5.  **Create Prefabs:**
    *   **Player Prefab:**
        *   Create `3D Object > Cube`. Name it `PlayerPrefab`.
        *   Add `Rigidbody` component.
        *   Add `PlayerInput` component:
            *   Set `Actions` to your `PlayerControls` asset.
            *   Set `Default Control Scheme` to `Keyboard&Mouse`.
            *   Set `Behavior` to `Invoke Unity Events`.
        *   Create a C# script named `LocalCoopPlayer` and attach it to `PlayerPrefab`.
        *   Drag `PlayerPrefab` from Hierarchy into your `Project` window to make it a prefab, then delete it from the Hierarchy.
    *   **Orb Prefab:**
        *   Create `3D Object > Sphere`. Name it `OrbPrefab`.
        *   Set `Is Trigger` on its `Sphere Collider`.
        *   Create a C# script named `SharedOrb` and attach it to `OrbPrefab`.
        *   Drag `OrbPrefab` from Hierarchy into your `Project` window to make it a prefab, then delete it from the Hierarchy.
6.  **Create Game Manager:**
    *   Create an empty `GameObject` in your Hierarchy. Name it `_GameManager`.
    *   Create a C# script named `LocalCoopSystem` and attach it to `_GameManager`.
    *   **Crucially, add a `PlayerInputManager` component to `_GameManager` as well:**
        *   Set its `Player Prefab` to your `PlayerPrefab`.
        *   Set `Join Behavior` to `Join Players When Button Is Pressed`.
        *   Set `Joining Button` to `Any Button` on `Any Device`.
        *   Set `Max Players` to `4` (or desired number).

7.  **Final Scene Setup:**
    *   Drag the `LocalCoopSystem` script onto the `_GameManager` GameObject.
    *   Drag the `SharedScoreText` UI element from the Canvas to the `Shared Score Text` field on the `LocalCoopSystem` component in the Inspector.
    *   Add a `Plane` for players to walk on.
    *   Place several `OrbPrefab` instances around the scene.
    *   Add a `Main Camera` and adjust its position.

---

### **1. `LocalCoopSystem.cs` (The Central Manager)**

This script is the heart of the LocalCoopSystem pattern. It manages player joining/leaving, tracks a shared score, and provides services for players and other game objects.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro; // Required for TextMeshProUGUI
using UnityEngine.Events; // Required for UnityEvent

/// <summary>
///     LocalCoopSystem: The central manager implementing the LocalCoopSystem design pattern.
///     This class acts as a service for all local cooperative gameplay elements.
///     It manages player registration, shared game state (like score), and provides
///     an API for player-specific actions to affect the global co-op experience.
/// </summary>
public class LocalCoopSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    // This makes the LocalCoopSystem globally accessible.
    // There should only be one instance of this system in the scene.
    public static LocalCoopSystem Instance { get; private set; }

    // --- References ---
    [Header("UI References")]
    [Tooltip("TextMeshProUGUI component to display the shared score.")]
    public TextMeshProUGUI sharedScoreText;

    // --- Events ---
    // UnityEvents allow other components to subscribe to changes without direct coupling.
    // This is good practice for UI updates or other game logic.
    [Header("Game Events")]
    [Tooltip("Event invoked when the shared score changes.")]
    public UnityEvent<int> OnSharedScoreChanged;

    // --- Internal State ---
    private readonly List<LocalCoopPlayer> _activePlayers = new List<LocalCoopPlayer>();
    private int _sharedScore = 0;

    // --- Initialization ---
    private void Awake()
    {
        // Singleton enforcement: Ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("LocalCoopSystem: Multiple instances found! Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize shared score display
        UpdateSharedScoreUI();
    }

    private void OnEnable()
    {
        // Subscribe to PlayerInputManager events.
        // This is how the system automatically tracks players joining/leaving.
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined += HandlePlayerJoined;
            PlayerInputManager.instance.onPlayerLeft += HandlePlayerLeft;
        }
        else
        {
            Debug.LogError("LocalCoopSystem: PlayerInputManager not found! Make sure it's on the same GameObject as LocalCoopSystem or in the scene.", this);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks and errors when the object is destroyed.
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= HandlePlayerJoined;
            PlayerInputManager.instance.onPlayerLeft -= HandlePlayerLeft;
        }
    }

    // --- Player Management Callbacks (from PlayerInputManager) ---

    /// <summary>
    /// Called by PlayerInputManager when a new player joins the game.
    /// This method is responsible for integrating the new player into the co-op system.
    /// </summary>
    /// <param name="playerInput">The PlayerInput component of the newly joined player.</param>
    private void HandlePlayerJoined(PlayerInput playerInput)
    {
        Debug.Log($"LocalCoopSystem: Player {playerInput.playerIndex + 1} ({playerInput.devices[0].displayName}) joined.");

        // Attempt to get the LocalCoopPlayer component from the spawned player object.
        LocalCoopPlayer coopPlayer = playerInput.GetComponent<LocalCoopPlayer>();
        if (coopPlayer != null)
        {
            _activePlayers.Add(coopPlayer);
            coopPlayer.InitializePlayer(playerInput.playerIndex + 1, playerInput.devices[0].displayName);
            Debug.Log($"LocalCoopSystem: Player {coopPlayer.PlayerID} registered.");
        }
        else
        {
            Debug.LogWarning($"LocalCoopSystem: Player {playerInput.playerIndex} joined, but no LocalCoopPlayer component found on its GameObject.", playerInput.gameObject);
        }
    }

    /// <summary>
    /// Called by PlayerInputManager when a player leaves the game.
    /// This removes the player from the active players list.
    /// </summary>
    /// <param name="playerInput">The PlayerInput component of the player who left.</param>
    private void HandlePlayerLeft(PlayerInput playerInput)
    {
        Debug.Log($"LocalCoopSystem: Player {playerInput.playerIndex + 1} left.");

        LocalCoopPlayer coopPlayer = playerInput.GetComponent<LocalCoopPlayer>();
        if (coopPlayer != null)
        {
            _activePlayers.Remove(coopPlayer);
            Debug.Log($"LocalCoopSystem: Player {coopPlayer.PlayerID} unregistered.");
        }
    }

    // --- Public API for LocalCoopSystem ---

    /// <summary>
    /// Increases the shared score for all local players.
    /// This is a cooperative action, affecting the collective progress.
    /// </summary>
    /// <param name="amount">The amount to add to the shared score.</param>
    public void AddSharedScore(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("LocalCoopSystem: Attempted to add negative score. Use 'RemoveSharedScore' if needed.", this);
            return;
        }
        _sharedScore += amount;
        Debug.Log($"LocalCoopSystem: Shared score increased by {amount}. New score: {_sharedScore}");
        UpdateSharedScoreUI();
        OnSharedScoreChanged.Invoke(_sharedScore); // Notify subscribers
    }

    /// <summary>
    /// Retrieves the current shared score.
    /// </summary>
    /// <returns>The current integer value of the shared score.</returns>
    public int GetSharedScore()
    {
        return _sharedScore;
    }

    /// <summary>
    /// Retrieves a list of all currently active LocalCoopPlayers.
    /// This could be used for global effects or UI specific to players.
    /// </summary>
    /// <returns>A read-only list of active LocalCoopPlayer instances.</returns>
    public IReadOnlyList<LocalCoopPlayer> GetActivePlayers()
    {
        return _activePlayers;
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Updates the UI Text component to display the current shared score.
    /// </summary>
    private void UpdateSharedScoreUI()
    {
        if (sharedScoreText != null)
        {
            sharedScoreText.text = $"Shared Score: {_sharedScore}";
        }
    }

    // You could add more shared cooperative game state here, for example:
    // private int _sharedHealth;
    // public void TakeSharedDamage(int amount) { /* ... */ }
    // public UnityEvent<int> OnSharedHealthChanged;
}
```

---

### **2. `LocalCoopPlayer.cs` (Player Entity)**

This script is attached to each player character. It handles individual player movement, input, and uses the `LocalCoopSystem` to perform cooperative actions (like collecting resources).

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Required for TextMeshProUGUI

/// <summary>
///     LocalCoopPlayer: Represents an individual player in the local cooperative system.
///     Each player has their own input, movement, and a unique ID.
///     They interact with the game world and the LocalCoopSystem to contribute to shared goals.
/// </summary>
[RequireComponent(typeof(PlayerInput))] // Ensures PlayerInput component is present
[RequireComponent(typeof(Rigidbody))]   // Ensures Rigidbody for physics movement
public class LocalCoopPlayer : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Movement speed of the player.")]
    public float moveSpeed = 5f;
    [Tooltip("Optional: TextMeshPro to display player ID above their head.")]
    public TextMeshPro playerIDText; // Use regular TextMeshPro for world space UI

    private PlayerInput _playerInput;
    private Rigidbody _rb;
    private Vector2 _moveInput;
    private int _playerID; // Unique identifier for this local player
    private string _deviceDisplayName; // Name of the input device (e.g., "Keyboard", "Gamepad")

    // Public property to easily get the player's ID
    public int PlayerID => _playerID;
    public string DeviceDisplayName => _deviceDisplayName;

    // --- Initialization ---
    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();

        // Ensure playerIDText is not null if assigned, and hide it initially
        if (playerIDText != null)
        {
            playerIDText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Called by the LocalCoopSystem to initialize this player with their unique ID and device.
    /// This is where player-specific setup (like color or name) can occur.
    /// </summary>
    /// <param name="id">The unique ID for this player (e.g., 1, 2, 3).</param>
    /// <param name="device">The display name of the input device controlling this player.</param>
    public void InitializePlayer(int id, string device)
    {
        _playerID = id;
        _deviceDisplayName = device;

        // Set player color based on ID for visual distinction
        SetPlayerColor(id);

        // Display player ID above their head
        if (playerIDText != null)
        {
            playerIDText.text = $"P{_playerID}\n({_deviceDisplayName})";
            playerIDText.gameObject.SetActive(true);
        }
    }

    // --- Input System Callbacks ---
    // These methods are hooked up to the PlayerInput component's Unity Events in the Inspector.

    /// <summary>
    /// Callback for the 'Move' input action. Reads the Vector2 input for movement.
    /// </summary>
    /// <param name="context">The InputValue context containing the move vector.</param>
    public void OnMove(InputValue context)
    {
        _moveInput = context.Get<Vector2>();
    }

    /// <summary>
    /// Callback for the 'Collect' input action (button press).
    /// This demonstrates a player action that interacts with the LocalCoopSystem.
    /// </summary>
    /// <param name="context">The InputValue context for the button press.</param>
    public void OnCollect(InputValue context)
    {
        if (context.isPressed)
        {
            Debug.Log($"Player {_playerID} ({_deviceDisplayName}) pressed Collect!");
            // Example: Trigger an effect or action, e.g., interact with a nearby object.
            // For now, it's just a debug log. Actual interaction handled by trigger for Orb.
        }
    }

    // --- Movement Physics ---
    private void FixedUpdate()
    {
        // Apply movement using Rigidbody for physics-based movement.
        Vector3 movement = new Vector3(_moveInput.x, 0, _moveInput.y) * moveSpeed;
        _rb.velocity = new Vector3(movement.x, _rb.velocity.y, movement.z);

        // Simple rotation to face movement direction
        if (movement.magnitude > 0.1f)
        {
            Quaternion toRotation = Quaternion.LookRotation(new Vector3(movement.x, 0, movement.z), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime);
        }
    }

    // --- Interaction with Shared Orb ---
    private void OnTriggerEnter(Collider other)
    {
        // Check if the player collides with a SharedOrb.
        SharedOrb orb = other.GetComponent<SharedOrb>();
        if (orb != null)
        {
            // The player (LocalCoopPlayer) has initiated collection.
            // The Orb itself will handle notifying the LocalCoopSystem for the shared score.
            // This design promotes the orb handling its own destruction and score contribution.
            orb.Collect(this); // Pass this player to the orb, in case the orb needs player-specific info.
        }
    }

    // --- Helper for visual distinction ---
    private void SetPlayerColor(int id)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock);

            switch (id)
            {
                case 1: propBlock.SetColor("_Color", Color.red); break;
                case 2: propBlock.SetColor("_Color", Color.blue); break;
                case 3: propBlock.SetColor("_Color", Color.green); break;
                case 4: propBlock.SetColor("_Color", Color.yellow); break;
                default: propBlock.SetColor("_Color", Color.gray); break;
            }
            renderer.SetPropertyBlock(propBlock);
        }
    }
}
```

---

### **3. `SharedOrb.cs` (Interactable Object)**

This script represents an object in the world that players can interact with to contribute to the shared goal (increasing the shared score).

```csharp
using UnityEngine;

/// <summary>
///     SharedOrb: An interactable object in the game world that contributes to the shared score.
///     When collected by a LocalCoopPlayer, it notifies the LocalCoopSystem to update the score.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures a Collider component is present
public class SharedOrb : MonoBehaviour
{
    [Header("Orb Settings")]
    [Tooltip("The amount of score this orb contributes to the shared score when collected.")]
    public int scoreValue = 10;
    [Tooltip("Visual effect to play when the orb is collected (e.g., particles).")]
    public GameObject collectEffectPrefab;

    private bool _isCollected = false;

    private void Awake()
    {
        // Ensure the collider is a trigger for collection.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// Called by a LocalCoopPlayer when they interact with (trigger) this orb.
    /// This method ensures the orb is collected only once and updates the shared score.
    /// </summary>
    /// <param name="collector">The LocalCoopPlayer that collected this orb.</param>
    public void Collect(LocalCoopPlayer collector)
    {
        if (_isCollected) return; // Prevent multiple collections

        _isCollected = true;
        Debug.Log($"Orb collected by Player {collector.PlayerID} ({collector.DeviceDisplayName}). Value: {scoreValue}");

        // Notify the LocalCoopSystem about the score contribution.
        // This is a key interaction point with the central co-op manager.
        if (LocalCoopSystem.Instance != null)
        {
            LocalCoopSystem.Instance.AddSharedScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("SharedOrb: LocalCoopSystem instance not found! Cannot add score.", this);
        }

        // Play visual effect (if any)
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the orb after collection
        Destroy(gameObject);
    }
}
```

---

### **Example Usage in Unity Inspector (after setting up scripts and prefabs):**

**1. `_GameManager` GameObject:**

*   **`LocalCoopSystem` Component:**
    *   `Shared Score Text`: Drag your `SharedScoreText` (TextMeshProUGUI) from your Canvas here.
    *   `On Shared Score Changed`: (Optional) You can hook up other events here, e.g., an audio cue.
*   **`PlayerInputManager` Component:**
    *   `Player Prefab`: Drag your `PlayerPrefab` from your Project window here.
    *   `Join Behavior`: `Join Players When Button Is Pressed`.
    *   `Joining Button`: `Any Button` (or specify, e.g., 'Start' button on gamepad).
    *   `Max Players`: `4` (or desired number).

**2. `PlayerPrefab`:**

*   **`LocalCoopPlayer` Component:**
    *   `Move Speed`: `5` (or adjust).
    *   `Player ID Text`: (Optional) If you have a TextMeshPro component above the player for their ID, drag it here. You can create a new 3D Object > Text - TextMeshPro, set its Rect Transform, and parent it to your PlayerPrefab.
*   **`PlayerInput` Component:**
    *   `Actions`: Drag your `PlayerControls` Input Action Asset here.
    *   `Default Control Scheme`: `Keyboard&Mouse`.
    *   `Behavior`: `Invoke Unity Events`.
    *   **Events Section (Click the small '+' on `Player` map):**
        *   `Move`: Drag `PlayerPrefab` itself (or the GameObject with `LocalCoopPlayer`) onto the Object field, then select `LocalCoopPlayer.OnMove`.
        *   `Collect`: Drag `PlayerPrefab` itself, then select `LocalCoopPlayer.OnCollect`.
*   **`Rigidbody` Component:**
    *   `Constraints`: Freeze `Rotation X` and `Rotation Z` to prevent tipping.

**3. `OrbPrefab`:**

*   **`SharedOrb` Component:**
    *   `Score Value`: `10` (or desired value).
    *   `Collect Effect Prefab`: (Optional) If you have a particle effect prefab, drag it here.

---

### **How to Run and Test:**

1.  Start the game in the Unity Editor.
2.  Press any key or gamepad button. A Player 1 will spawn.
3.  Press another key or gamepad button (if you have multiple devices connected, or different keys on the keyboard like WASD and Arrows). A Player 2 will spawn.
4.  Repeat for Player 3, Player 4, etc., up to `Max Players`.
5.  Move your players around using their respective inputs.
6.  Have players touch the `OrbPrefab` instances.
7.  Observe the `Shared Score` in the top-left corner updating as orbs are collected by *any* player.

This setup provides a fully functional demonstration of the `LocalCoopSystem` pattern, showcasing how to manage multiple local players, handle their inputs, and coordinate their actions towards a shared game goal within a centralized system.