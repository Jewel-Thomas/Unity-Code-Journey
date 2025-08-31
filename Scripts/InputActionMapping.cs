// Unity Design Pattern Example: InputActionMapping
// This script demonstrates the InputActionMapping pattern in Unity
// Generated automatically - ready to use in your Unity project

The `InputActionMapping` design pattern is crucial in game development for creating flexible and user-friendly control schemes. It decouples the *concept* of a game action (e.g., "Jump", "Fire", "Move Forward") from the *specific physical input* used to trigger it (e.g., `Spacebar`, `Left Mouse Button`, `W key`).

This pattern allows:
1.  **Player Customization:** Players can easily remap controls to their preference without altering core game logic.
2.  **Input Device Agnosticism:** The game logic interacts with abstract actions, not specific keys, making it easier to support different keyboards, gamepads, or input methods.
3.  **Maintainability:** Changes to input devices or default mappings don't require changes throughout the codebase where actions are used.

This example provides a complete, practical implementation using Unity's older Input Manager (`Input.GetKey`), which is simpler to demonstrate the pattern's core principles. The same pattern can be applied with Unity's new Input System, where `GameAction` would map to named actions within an `InputActionAsset`.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like .FirstOrDefault()

/// <summary>
/// Defines the abstract game actions that the player can perform.
/// This enum represents the 'what' of the action, independent of 'how' it's triggered.
/// </summary>
public enum GameAction
{
    None, // Default or unassigned action
    MoveForward,
    MoveBackward,
    StrafeLeft,
    StrafeRight,
    Jump,
    Interact,
    FirePrimary,
    FireSecondary,
    Crouch,
    PauseGame
}

/// <summary>
/// A serializable struct to store a single mapping between a GameAction and a KeyCode.
/// This allows Unity to save and load mappings in the Inspector.
/// Each action can have a primary and an optional alternate key.
/// </summary>
[Serializable]
public struct ActionMap
{
    public GameAction Action;
    public KeyCode PrimaryKey;
    public KeyCode AlternateKey; // For actions that might have two default keys

    public ActionMap(GameAction action, KeyCode primary, KeyCode alternate = KeyCode.None)
    {
        Action = action;
        PrimaryKey = primary;
        AlternateKey = alternate;
    }
}

/// <summary>
/// The core component implementing the InputActionMapping design pattern.
/// It manages the mappings between abstract GameActions and concrete KeyCodes.
/// Other scripts query this manager to check if a logical action is being performed,
/// completely decoupled from the specific input keys.
/// </summary>
public class InputActionMapper : MonoBehaviour
{
    [Header("Default Mappings")]
    [Tooltip("Define the default input mappings for game actions.")]
    [SerializeField]
    private List<ActionMap> defaultMappings = new List<ActionMap>()
    {
        new ActionMap(GameAction.MoveForward, KeyCode.W),
        new ActionMap(GameAction.MoveBackward, KeyCode.S),
        new ActionMap(GameAction.StrafeLeft, KeyCode.A),
        new ActionMap(GameAction.StrafeRight, KeyCode.D),
        new ActionMap(GameAction.Jump, KeyCode.Space),
        new ActionMap(GameAction.Interact, KeyCode.E),
        new ActionMap(GameAction.FirePrimary, KeyCode.Mouse0), // Left mouse button
        new ActionMap(GameAction.FireSecondary, KeyCode.Mouse1), // Right mouse button
        new ActionMap(GameAction.Crouch, KeyCode.C),
        new ActionMap(GameAction.PauseGame, KeyCode.Escape)
    };

    // Runtime storage for current mappings.
    // Dictionary where key is GameAction and value is a list of KeyCodes mapped to that action.
    private Dictionary<GameAction, List<KeyCode>> currentMappings;

    // --- Rebinding State ---
    private GameAction? actionToRebind = null; // Stores the action currently being rebound
    private bool rebindIsAlternateKey = false; // True if rebinding the alternate key

    // Event fired when an action is remapped. Useful for UI updates.
    public event Action<GameAction, KeyCode, bool> OnActionRemapped;

    // Singleton pattern for easy access from other scripts (optional but common for input managers)
    public static InputActionMapper Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep mapper alive across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeMappings();
    }

    /// <summary>
    /// Initializes the current mappings from the default list.
    /// In a real project, this would also load saved mappings from PlayerPrefs or a save file.
    /// </summary>
    private void InitializeMappings()
    {
        currentMappings = new Dictionary<GameAction, List<KeyCode>>();

        foreach (var map in defaultMappings)
        {
            if (!currentMappings.ContainsKey(map.Action))
            {
                currentMappings.Add(map.Action, new List<KeyCode>());
            }
            if (map.PrimaryKey != KeyCode.None && !currentMappings[map.Action].Contains(map.PrimaryKey))
            {
                currentMappings[map.Action].Add(map.PrimaryKey);
            }
            if (map.AlternateKey != KeyCode.None && !currentMappings[map.Action].Contains(map.AlternateKey))
            {
                currentMappings[map.Action].Add(map.AlternateKey);
            }
        }

        Debug.Log("Input Action Mapper initialized with default mappings.");
        // Example: Log current mappings
        foreach (var entry in currentMappings)
        {
            Debug.Log($"Action: {entry.Key}, Keys: {string.Join(", ", entry.Value)}");
        }
    }

    private void Update()
    {
        // Handle rebinding input capture
        if (actionToRebind.HasValue)
        {
            HandleRebindingInput();
        }
    }

    /// <summary>
    /// Checks if any of the keys mapped to the given action were pressed down this frame.
    /// </summary>
    /// <param name="action">The GameAction to check.</param>
    /// <returns>True if any mapped key was pressed down, false otherwise.</returns>
    public bool IsActionPressed(GameAction action)
    {
        if (currentMappings.TryGetValue(action, out List<KeyCode> keys))
        {
            foreach (KeyCode key in keys)
            {
                if (Input.GetKeyDown(key))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if any of the keys mapped to the given action are currently held down.
    /// </summary>
    /// <param name="action">The GameAction to check.</param>
    /// <returns>True if any mapped key is held down, false otherwise.</returns>
    public bool IsActionHeld(GameAction action)
    {
        if (currentMappings.TryGetValue(action, out List<KeyCode> keys))
        {
            foreach (KeyCode key in keys)
            {
                if (Input.GetKey(key))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if any of the keys mapped to the given action were released this frame.
    /// </summary>
    /// <param name="action">The GameAction to check.</param>
    /// <returns>True if any mapped key was released, false otherwise.</returns>
    public bool IsActionReleased(GameAction action)
    {
        if (currentMappings.TryGetValue(action, out List<KeyCode> keys))
        {
            foreach (KeyCode key in keys)
            {
                if (Input.GetKeyUp(key))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Initiates the rebinding process for a specific action.
    /// Call this when a player clicks a 'rebind' button in the options menu.
    /// </summary>
    /// <param name="action">The GameAction to rebind.</param>
    /// <param name="isAlternate">If true, rebinds the alternate key; otherwise, the primary key.</param>
    public void StartRebinding(GameAction action, bool isAlternate = false)
    {
        if (!currentMappings.ContainsKey(action))
        {
            Debug.LogWarning($"Attempted to rebind unmapped action: {action}. Adding it first.");
            currentMappings.Add(action, new List<KeyCode>());
        }

        actionToRebind = action;
        rebindIsAlternateKey = isAlternate;
        Debug.Log($"Started rebinding for {action} ({(isAlternate ? "Alternate" : "Primary")} Key)... Press a key.");
    }

    /// <summary>
    /// Handles capturing the next key press for rebinding.
    /// </summary>
    private void HandleRebindingInput()
    {
        if (Input.anyKeyDown)
        {
            // Iterate through all possible KeyCodes to find which one was pressed.
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    // Avoid binding 'None' or system-level keys if not desired (e.g., escape for rebind cancelling).
                    // Here we allow all key codes to demonstrate.
                    if (keyCode == KeyCode.Escape && actionToRebind.HasValue)
                    {
                        Debug.Log("Rebinding cancelled for " + actionToRebind.Value);
                        EndRebinding();
                        return;
                    }

                    PerformRebind(actionToRebind.Value, keyCode, rebindIsAlternateKey);
                    EndRebinding();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Completes the rebinding process by assigning the new key to the action.
    /// </summary>
    /// <param name="action">The GameAction to rebind.</param>
    /// <param name="newKey">The new KeyCode to assign.</param>
    /// <param name="isAlternate">If true, rebinds the alternate key; otherwise, the primary key.</param>
    private void PerformRebind(GameAction action, KeyCode newKey, bool isAlternate)
    {
        // Remove the old key first to avoid duplicates or orphaned keys
        // Find the specific ActionMap entry for this action.
        int indexInDefaultMappings = defaultMappings.FindIndex(m => m.Action == action);
        if (indexInDefaultMappings != -1)
        {
            ActionMap map = defaultMappings[indexInDefaultMappings];
            
            // Remove the old key from the runtime dictionary if it exists
            if (currentMappings.TryGetValue(action, out List<KeyCode> keysList))
            {
                if (isAlternate && map.AlternateKey != KeyCode.None)
                {
                    keysList.Remove(map.AlternateKey);
                }
                else if (!isAlternate && map.PrimaryKey != KeyCode.None)
                {
                    keysList.Remove(map.PrimaryKey);
                }
            }

            // Update the ActionMap struct in the defaultMappings list
            // This is important because defaultMappings acts as our 'source of truth'
            // which we modify at runtime, and can be saved later.
            if (isAlternate)
            {
                map.AlternateKey = newKey;
            }
            else
            {
                map.PrimaryKey = newKey;
            }
            defaultMappings[indexInDefaultMappings] = map; // Reassign struct after modification

            // Add the new key to the runtime dictionary if it's not already there
            if (!keysList.Contains(newKey))
            {
                keysList.Add(newKey);
            }
            else
            {
                Debug.LogWarning($"Key {newKey} already mapped to {action}. No change needed.");
            }

            Debug.Log($"Action {action} remapped to {newKey} ({(isAlternate ? "Alternate" : "Primary")} Key).");
            OnActionRemapped?.Invoke(action, newKey, isAlternate);
            
            // In a real game, you would save these updated mappings to PlayerPrefs or a file here.
            // Example: SaveMappingsToPlayerPrefs();
        }
        else
        {
            Debug.LogError($"Cannot find default mapping for action {action} to rebind.");
        }
    }
    
    /// <summary>
    /// Resets the rebinding state.
    /// </summary>
    private void EndRebinding()
    {
        actionToRebind = null;
        rebindIsAlternateKey = false;
    }

    /// <summary>
    /// Retrieves the currently mapped primary or alternate key for a given action.
    /// Useful for displaying current key bindings in a UI.
    /// </summary>
    /// <param name="action">The GameAction to query.</param>
    /// <param name="primary">If true, returns the primary key; otherwise, returns the alternate key.</param>
    /// <returns>The KeyCode mapped, or KeyCode.None if not found.</returns>
    public KeyCode GetMappedKey(GameAction action, bool primary = true)
    {
        // We retrieve from defaultMappings because it's the authoritative list that gets updated
        var map = defaultMappings.FirstOrDefault(m => m.Action == action);
        if (map.Action == action) // Check if an entry was found
        {
            return primary ? map.PrimaryKey : map.AlternateKey;
        }
        return KeyCode.None;
    }

    /// <summary>
    /// Resets all mappings to their initial default values.
    /// </summary>
    public void ResetToDefaults()
    {
        Debug.Log("Resetting input mappings to defaults.");
        // Re-initialize from the 'original' default mappings (if stored separately)
        // For this example, we'll re-run InitializeMappings() assuming defaultMappings
        // reflects the desired initial state. If defaultMappings itself was modified
        // at runtime, you'd need a separate static/const list of initial defaults.
        InitializeMappings(); 
        // In a full game, you'd also overwrite any saved mappings here.
    }
}

/// <summary>
/// EXAMPLE USAGE: A simple Player Controller demonstrating how to use InputActionMapper.
/// This script does not directly check KeyCodes like W, A, S, D.
/// Instead, it queries the InputActionMapper for abstract game actions.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody component!");
            enabled = false;
        }

        // Ensure the InputActionMapper exists and is initialized
        if (InputActionMapper.Instance == null)
        {
            Debug.LogError("InputActionMapper instance not found. Make sure it's in the scene.");
            enabled = false;
        }
    }

    void Update()
    {
        // Ground check for jumping
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);

        // --- Movement ---
        Vector3 moveDirection = Vector3.zero;

        if (InputActionMapper.Instance.IsActionHeld(GameAction.MoveForward))
        {
            moveDirection += transform.forward;
        }
        if (InputActionMapper.Instance.IsActionHeld(GameAction.MoveBackward))
        {
            moveDirection -= transform.forward;
        }
        if (InputActionMapper.Instance.IsActionHeld(GameAction.StrafeLeft))
        {
            moveDirection -= transform.right;
        }
        if (InputActionMapper.Instance.IsActionHeld(GameAction.StrafeRight))
        {
            moveDirection += transform.right;
        }

        // Normalize movement to prevent faster diagonal speed
        if (moveDirection.magnitude > 1)
        {
            moveDirection.Normalize();
        }

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // --- Jump ---
        if (InputActionMapper.Instance.IsActionPressed(GameAction.Jump) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("Jump action performed!");
        }

        // --- Interaction ---
        if (InputActionMapper.Instance.IsActionPressed(GameAction.Interact))
        {
            Debug.Log("Interact action performed!");
            // Perform raycast or trigger interaction logic here
        }

        // --- Fire Primary ---
        if (InputActionMapper.Instance.IsActionHeld(GameAction.FirePrimary))
        {
            // Debug.Log("Primary Fire action held!"); // Too spammy, only log once
            // Example: Call a weapon script's Fire method
        }
        if (InputActionMapper.Instance.IsActionPressed(GameAction.FirePrimary))
        {
            Debug.Log("Primary Fire action pressed!");
        }
        if (InputActionMapper.Instance.IsActionReleased(GameAction.FirePrimary))
        {
            Debug.Log("Primary Fire action released!");
        }
        
        // --- Pause Game ---
        if (InputActionMapper.Instance.IsActionPressed(GameAction.PauseGame))
        {
            // Toggle pause state
            bool isPaused = Time.timeScale == 0;
            Time.timeScale = isPaused ? 1 : 0;
            Debug.Log($"Game {(isPaused ? "unpaused" : "paused")} via PauseGame action.");
        }
    }
    
    // Simple Gizmo to show ground check sphere
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}

/// <summary>
/// EXAMPLE USAGE: A simple UI manager to demonstrate rebinding.
/// In a real project, this would be a more sophisticated options menu.
/// </summary>
public class RebindUIManager : MonoBehaviour
{
    // You would typically link UI Text elements to display current keys
    // and UI Buttons to trigger the rebind process.
    // For simplicity, we'll use Debug.Log and provide methods.

    void Start()
    {
        if (InputActionMapper.Instance == null)
        {
            Debug.LogError("InputActionMapper instance not found. Cannot manage rebind UI.");
            enabled = false;
            return;
        }
        
        // Subscribe to remapping event to update UI elements (e.g., button text)
        InputActionMapper.Instance.OnActionRemapped += HandleActionRemapped;
        
        LogCurrentKeyBindings();
    }

    void OnDestroy()
    {
        if (InputActionMapper.Instance != null)
        {
            InputActionMapper.Instance.OnActionRemapped -= HandleActionRemapped;
        }
    }

    private void HandleActionRemapped(GameAction action, KeyCode newKey, bool isAlternate)
    {
        Debug.Log($"UI: Action '{action}' remapped to '{newKey}' ({(isAlternate ? "Alternate" : "Primary")}). Update UI button text!");
        // Here you would find the specific UI Text component associated with 'action'
        // and update its text to newKey.ToString().
    }

    // --- Methods callable from UI Buttons ---

    public void StartRebindMoveForwardPrimary()
    {
        Debug.Log("UI: Requesting rebind for MoveForward (Primary).");
        InputActionMapper.Instance.StartRebinding(GameAction.MoveForward, false);
    }

    public void StartRebindJumpPrimary()
    {
        Debug.Log("UI: Requesting rebind for Jump (Primary).");
        InputActionMapper.Instance.StartRebinding(GameAction.Jump, false);
    }
    
    public void StartRebindInteractPrimary()
    {
        Debug.Log("UI: Requesting rebind for Interact (Primary).");
        InputActionMapper.Instance.StartRebinding(GameAction.Interact, false);
    }

    public void StartRebindFirePrimary()
    {
        Debug.Log("UI: Requesting rebind for FirePrimary (Primary).");
        InputActionMapper.Instance.StartRebinding(GameAction.FirePrimary, false);
    }
    
    public void StartRebindFireSecondaryAlternate()
    {
        Debug.Log("UI: Requesting rebind for FireSecondary (Alternate).");
        // Example of rebinding an alternate key
        InputActionMapper.Instance.StartRebinding(GameAction.FireSecondary, true);
    }

    public void ResetAllToDefaults()
    {
        Debug.Log("UI: Resetting all mappings to defaults.");
        InputActionMapper.Instance.ResetToDefaults();
        LogCurrentKeyBindings(); // Refresh display after reset
    }

    // Helper to log all current key bindings to console
    private void LogCurrentKeyBindings()
    {
        Debug.Log("--- Current Key Bindings ---");
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
        {
            if (action == GameAction.None) continue;

            KeyCode primary = InputActionMapper.Instance.GetMappedKey(action, true);
            KeyCode alternate = InputActionMapper.Instance.GetMappedKey(action, false);
            
            string log = $"  {action}: Primary = {primary}";
            if (alternate != KeyCode.None)
            {
                log += $", Alternate = {alternate}";
            }
            Debug.Log(log);
        }
        Debug.Log("--------------------------");
    }
}
```

### How to Use This Example in Unity:

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create C# Scripts:**
    *   Create a new C# script named `InputActionMapper.cs`. Copy and paste the entire content of the code block above into it.
    *   Create a new C# script named `PlayerController.cs`. Copy and paste the entire content of the code block above into it (make sure it replaces the previous PlayerController content).
    *   Create a new C# script named `RebindUIManager.cs`. Copy and paste the entire content of the code block above into it (make sure it replaces the previous RebindUIManager content).
3.  **Setup the Scene:**
    *   **Create an Empty GameObject:** Name it `_InputManager`.
    *   **Add `InputActionMapper` Component:** Drag the `InputActionMapper.cs` script onto the `_InputManager` GameObject.
    *   **Inspect `InputActionMapper`:** You'll see the `Default Mappings` list in the Inspector. You can customize these initial bindings directly here.
    *   **Create a Player GameObject:** Create a Capsule, Cube, or Sphere, name it `Player`.
        *   Add a `Rigidbody` component to the `Player` (ensure 'Use Gravity' is checked).
        *   Add the `PlayerController` component to the `Player`.
        *   **Create a GroundCheck GameObject:** As a child of `Player`, create an empty GameObject named `GroundCheck` and position it slightly below the Player's feet (e.g., Y position like -0.6 for a Capsule).
        *   **Create a Ground:** Create a Plane or a Cube, scale it up, and name it `Ground`. Ensure it has a `Collider`.
        *   **Set PlayerController Properties:**
            *   Drag the `GroundCheck` child GameObject to the `Ground Check` slot on the `PlayerController`.
            *   Set the `Ground Layer` to `Default` (or create a specific `Ground` layer).
    *   **Create an Empty GameObject for UI:** Name it `_UIManager`.
    *   **Add `RebindUIManager` Component:** Drag the `RebindUIManager.cs` script onto the `_UIManager` GameObject.
    *   **Test Controls:** Play the scene. The `PlayerController` should respond to the default W, A, S, D for movement, Space for jump, E for interact, Mouse0 for primary fire, Escape for pause.
    *   **Test Rebinding (via `RebindUIManager`):**
        *   Look at the Console while the game is running.
        *   You can call the public methods on `RebindUIManager` from another script, or even by adding temporary UI Buttons in the scene and linking their `OnClick()` events to these methods for a true UI experience.
        *   For example, if you wanted to rebind Jump, you would call `RebindUIManager.StartRebindJumpPrimary()`. The console will prompt you to press a key. Press a key (e.g., `Z`), and the jump action will now be bound to `Z`.

### Explanation of the Pattern and Code:

1.  **`GameAction` Enum:**
    *   This is the core of the pattern. It defines abstract, high-level actions (e.g., `Jump`, `FirePrimary`) that your game logic understands.
    *   Crucially, it *doesn't* know anything about specific key presses (`KeyCode.Space`, `KeyCode.Mouse0`).

2.  **`ActionMap` Struct:**
    *   A simple data structure that pairs a `GameAction` with one or two `KeyCode`s.
    *   `[Serializable]` allows it to be configured in the Unity Inspector.
    *   This is where the *mapping* happens: `GameAction` X maps to `KeyCode` Y.

3.  **`InputActionMapper` (The "Mapper" Component):**
    *   This is the central hub. Other scripts will never directly query `Input.GetKeyDown(KeyCode.Space)`. Instead, they'll ask `InputActionMapper.Instance.IsActionPressed(GameAction.Jump)`.
    *   **`defaultMappings`:** A `List` of `ActionMap` structs configured in the Inspector. These are the initial settings.
    *   **`currentMappings` (Dictionary):** This is the runtime storage. It converts the `List<ActionMap>` into a `Dictionary<GameAction, List<KeyCode>>` for efficient lookup. A `List<KeyCode>` is used as the value to allow for multiple keys (e.g., `W` and `UpArrow` both move forward).
    *   **`InitializeMappings()`:** Sets up `currentMappings` from `defaultMappings` on startup. In a full game, this would also load user-saved preferences.
    *   **`IsActionPressed`/`IsActionHeld`/`IsActionReleased`:** These are the public methods game logic uses. They iterate through the `KeyCode`s mapped to a given `GameAction` and check Unity's `Input` class. The game logic doesn't care *which* key, just *if* the action is happening.
    *   **Rebinding Logic (`StartRebinding`, `HandleRebindingInput`, `PerformRebind`):**
        *   This demonstrates how players can change mappings at runtime.
        *   `StartRebinding` puts the mapper into a state where it's waiting for any key press.
        *   `HandleRebindingInput` in `Update` listens for `Input.anyKeyDown` and identifies which specific `KeyCode` was pressed.
        *   `PerformRebind` updates the internal `defaultMappings` list (which also updates `currentMappings` indirectly or directly if you implement it that way) and fires an `OnActionRemapped` event.
        *   **Important:** `defaultMappings` is treated as the source of truth that gets modified at runtime and would be saved/loaded.
    *   **Singleton Pattern:** `Instance` provides easy global access, preventing other scripts from needing a direct reference to the `InputActionMapper` GameObject.

4.  **`PlayerController` (Example Consumer):**
    *   This script clearly shows how to *use* the `InputActionMapper`.
    *   Notice that it never references a `KeyCode`. All input checks are made against `GameAction` enums via `InputActionMapper.Instance`.
    *   This makes the `PlayerController` entirely independent of the specific input device or key bindings. If the player remaps "Jump" from `Space` to `Z`, the `PlayerController`'s code doesn't change at all.

5.  **`RebindUIManager` (Example UI Integration):**
    *   This script demonstrates how an options menu might interact with the `InputActionMapper` to allow remapping.
    *   It uses the `StartRebinding` method and listens to the `OnActionRemapped` event to update display text (though in this example, it just logs to the console).
    *   `GetMappedKey` is used to retrieve the current binding for display purposes.

This complete setup provides a robust and educational example of the `InputActionMapping` design pattern, ready for use and extension in your Unity projects.