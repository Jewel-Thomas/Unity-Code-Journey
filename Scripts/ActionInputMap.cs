// Unity Design Pattern Example: ActionInputMap
// This script demonstrates the ActionInputMap pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ActionInputMap' design pattern is a powerful way to decouple a player's *intent* (e.g., "Jump", "Fire", "MoveForward") from the specific physical input that triggers it (e.g., `KeyCode.Space`, `Input.GetButton("Jump")`, `Input.GetAxis("Vertical")`).

This pattern brings several benefits:
1.  **Flexibility:** Easily change keybindings without modifying game logic.
2.  **Runtime Remapping:** Players can customize controls in-game.
3.  **Readability:** Game logic focuses on abstract actions, making it cleaner.
4.  **Platform Agnostic:** Adapt inputs for different platforms (keyboard, gamepad, touch) without altering core game code.

Below is a complete C# Unity example demonstrating the ActionInputMap pattern. It includes:
*   An `enum` for abstract `GameAction`s.
*   A `struct` to define physical `InputDefinition`s.
*   The `ActionInputMap` MonoBehaviour as a central manager.
*   An example `PlayerController` that uses the `ActionInputMap`.
*   Detailed comments explaining each part and how the pattern works.
*   Instructions on how to set it up and run in a Unity project.

---

```csharp
using UnityEngine;
using System.Collections.Generic; // For List and Dictionary
using System; // For Serializable attribute

// ========================================================================================================================
// SECTION 1: Game Actions - Defining the abstract player intentions
// ========================================================================================================================

/// <summary>
/// Defines all possible actions a player can perform in the game.
/// This enum represents the *intent* of the player, decoupled from how that intent is expressed (input).
///
/// By using an enum, we get type safety and a clear, centralized list of all player-triggerable actions.
/// </summary>
public enum GameAction
{
    /// <summary>No action specified. Used for default/unassigned states.</summary>
    None,

    /// <summary>Player wants to move forward.</summary>
    MoveForward,
    /// <summary>Player wants to move backward.</summary>
    MoveBackward,
    /// <summary>Player wants to move left.</summary>
    MoveLeft,
    /// <summary>Player wants to move right.</summary>
    MoveRight,

    /// <summary>Player wants to jump.</summary>
    Jump,
    /// <summary>Player wants to fire their weapon/ability.</summary>
    Fire,
    /// <summary>Player wants to interact with an object.</summary>
    Interact,
    /// <summary>Player wants to open the pause menu.</summary>
    Pause,

    // Add more game-specific actions here as needed, e.g.,
    // Crouch, Sprint, Reload, UseItem, OpenMap, etc.
}

// ========================================================================================================================
// SECTION 2: Input Definition - Representing physical inputs
// ========================================================================================================================

/// <summary>
/// Specifies the type of physical input Unity's Input Manager can handle.
/// </summary>
public enum InputType
{
    /// <summary>A standard keyboard key (e.g., W, A, Space).</summary>
    Key,
    /// <summary>A named button configured in Unity's Project Settings -> Input Manager (e.g., "Jump", "Fire1").
    /// These can map to keyboard keys, mouse buttons, or gamepad buttons.</summary>
    Button,
    /// <summary>A named axis configured in Unity's Project Settings -> Input Manager (e.g., "Horizontal", "Vertical", "Mouse X").
    /// These typically return a float value between -1 and 1.</summary>
    Axis
}

/// <summary>
/// A serializable struct that defines a single physical input.
/// This allows us to configure inputs directly in the Unity Inspector for easy remapping.
/// </summary>
[Serializable]
public struct InputDefinition
{
    /// <summary>The type of input this definition represents.</summary>
    public InputType inputType;

    /// <summary>The specific KeyCode for Key inputs.</summary>
    public KeyCode keyCode;

    /// <summary>The name of the button for Button inputs (e.g., "Jump", "Fire1").</summary>
    /// <remarks>Matches names configured in Edit -> Project Settings -> Input Manager.</remarks>
    public string buttonName;

    /// <summary>The name of the axis for Axis inputs (e.g., "Horizontal", "Vertical", "Mouse X").</summary>
    /// <remarks>Matches names configured in Edit -> Project Settings -> Input Manager.</remarks>
    public string axisName;

    /// <summary>
    /// Checks if the input was pressed down (started) in the current frame.
    /// Relevant for Key and Button types. Axes do not typically have a "down" state in this context.
    /// </summary>
    /// <returns>True if the input was pressed down.</returns>
    public bool GetInputDown()
    {
        // Basic validation for common misconfigurations
        if (inputType == InputType.Key && keyCode == KeyCode.None) return false;
        if (inputType == InputType.Button && string.IsNullOrEmpty(buttonName)) return false;
        // Axes don't have a "down" state, so return false for them.

        switch (inputType)
        {
            case InputType.Key:
                return Input.GetKeyDown(keyCode);
            case InputType.Button:
                return Input.GetButtonDown(buttonName);
            case InputType.Axis:
                // Axes typically provide continuous values, not discrete "down" events.
                // If you need a "down" for an axis, consider mapping it as a Button in Input Manager
                // or defining a custom threshold for the axis value.
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the input is currently held down.
    /// Relevant for Key and Button types. For Axis types, it checks if the value is above a small threshold.
    /// </summary>
    /// <returns>True if the input is currently held down or the axis value is significant.</returns>
    public bool GetInput()
    {
        // Basic validation
        if (inputType == InputType.Key && keyCode == KeyCode.None) return false;
        if (inputType == InputType.Button && string.IsNullOrEmpty(buttonName)) return false;
        if (inputType == InputType.Axis && string.IsNullOrEmpty(axisName)) return false;

        switch (inputType)
        {
            case InputType.Key:
                return Input.GetKey(keyCode);
            case InputType.Button:
                return Input.GetButton(buttonName);
            case InputType.Axis:
                // For axes, we consider it "held" if its absolute value is above a small epsilon.
                return Mathf.Abs(Input.GetAxis(axisName)) > 0.01f;
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the input was released (pressed up) in the current frame.
    /// Relevant for Key and Button types. Axes do not typically have an "up" state.
    /// </summary>
    /// <returns>True if the input was released.</returns>
    public bool GetInputUp()
    {
        // Basic validation
        if (inputType == InputType.Key && keyCode == KeyCode.None) return false;
        if (inputType == InputType.Button && string.IsNullOrEmpty(buttonName)) return false;
        // Axes don't have an "up" state, so return false for them.

        switch (inputType)
        {
            case InputType.Key:
                return Input.GetKeyUp(keyCode);
            case InputType.Button:
                return Input.GetButtonUp(buttonName);
            case InputType.Axis:
                // Axes typically provide continuous values, not discrete "up" events.
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Gets the raw value of an axis input.
    /// Relevant only for Axis types. Returns 0 for Key/Button types.
    /// </summary>
    /// <returns>The axis value (typically -1 to 1) or 0 if not an axis or invalid.</returns>
    public float GetAxis()
    {
        if (inputType == InputType.Axis && !string.IsNullOrEmpty(axisName))
        {
            return Input.GetAxis(axisName);
        }
        return 0f;
    }

    /// <summary>
    /// Gets the raw unscaled value of an axis input.
    /// Relevant only for Axis types. Returns 0 for Key/Button types.
    /// This bypasses any smoothing configured in the Input Manager.
    /// </summary>
    /// <returns>The axis value (typically -1 to 1) or 0 if not an axis or invalid.</returns>
    public float GetAxisRaw()
    {
        if (inputType == InputType.Axis && !string.IsNullOrEmpty(axisName))
        {
            return Input.GetAxisRaw(axisName);
        }
        return 0f;
    }
}

// ========================================================================================================================
// SECTION 3: ActionInputMap - The core mapping and manager
// ========================================================================================================================

/// <summary>
/// Represents a binding between a <see cref="GameAction"/> and an <see cref="InputDefinition"/>.
/// This class is serializable, allowing Unity to display and edit it directly in the Inspector.
/// </summary>
[Serializable]
public class ActionBinding
{
    /// <summary>The abstract game action (e.g., Jump, Fire).</summary>
    public GameAction action;
    /// <summary>The physical input definition that triggers this action (e.g., Space key, "Fire1" button).</summary>
    public InputDefinition input;
}

/// <summary>
/// The central manager for the ActionInputMap design pattern.
/// It holds the mappings between abstract <see cref="GameAction"/>s and concrete <see cref="InputDefinition"/>s.
///
/// This component should exist once in your scene (typically attached to a GameObject like "InputManager").
/// Player controllers and other game logic should query this manager for action states,
/// rather than directly checking <see cref="UnityEngine.Input"/>.
/// </summary>
public class ActionInputMap : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Makes the InputManager easily accessible from anywhere without needing a direct reference.
    public static ActionInputMap Instance { get; private set; }

    // --- Inspector-Configurable Default Bindings ---
    [Header("Default Input Bindings")]
    [Tooltip("Configure the default mapping between GameActions and physical InputDefinitions.")]
    public List<ActionBinding> defaultBindings = new List<ActionBinding>();

    // --- Runtime Action Map ---
    // The dictionary used internally for fast lookups during gameplay.
    // This holds the currently active mappings, which can be modified at runtime.
    private Dictionary<GameAction, InputDefinition> currentActionMap;

    private void Awake()
    {
        // Enforce singleton pattern:
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ActionInputMap instances found. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        // Otherwise, this is the one true instance.
        Instance = this;
        // Make sure the InputManager persists across scene changes.
        DontDestroyOnLoad(gameObject); 

        // Initialize the runtime action map from the default bindings configured in the Inspector.
        InitializeActionMap();
    }

    /// <summary>
    /// Initializes or re-initializes the internal action map dictionary
    /// from the `defaultBindings` list configured in the Inspector.
    /// This method is called at startup and can be used to reset bindings.
    /// </summary>
    private void InitializeActionMap()
    {
        currentActionMap = new Dictionary<GameAction, InputDefinition>();
        foreach (var binding in defaultBindings)
        {
            // Basic validation to prevent mapping 'None' or accidental duplicate actions
            if (binding.action == GameAction.None)
            {
                Debug.LogWarning($"Attempted to bind 'GameAction.None' for input type '{binding.input.inputType}'. Skipping this binding.", this);
                continue;
            }
            if (currentActionMap.ContainsKey(binding.action))
            {
                Debug.LogWarning($"Duplicate binding for action '{binding.action}' found. Overwriting with the last one specified in the list.", this);
            }
            currentActionMap[binding.action] = binding.input;
        }

        Debug.Log($"ActionInputMap initialized with {currentActionMap.Count} unique actions from default bindings.");
    }

    /// <summary>
    /// Retrieves the <see cref="InputDefinition"/> associated with a given <see cref="GameAction"/>.
    /// This is a private helper method used by the public `GetAction...` methods.
    /// </summary>
    /// <param name="action">The abstract game action.</param>
    /// <returns>The <see cref="InputDefinition"/> mapped to the action, or a default empty struct if not found.</returns>
    private InputDefinition GetInputDefinitionForAction(GameAction action)
    {
        if (currentActionMap.TryGetValue(action, out InputDefinition input))
        {
            return input;
        }
        // Log a warning if an action is queried but not mapped, which can indicate a configuration error.
        Debug.LogWarning($"Action '{action}' is not mapped to any input in the current ActionInputMap. " +
                         "Please ensure it's configured in the Inspector or remapped at runtime.", this);
        return new InputDefinition(); // Return an empty/default InputDefinition that will return false/0
    }

    // --- Public API for Querying Action States ---
    // These methods allow any script to ask about a GameAction's state without knowing the underlying input.

    /// <summary>
    /// Checks if a specific <see cref="GameAction"/> was triggered (pressed down) in the current frame.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the mapped input for the action was pressed down.</returns>
    public bool GetActionDown(GameAction action)
    {
        return GetInputDefinitionForAction(action).GetInputDown();
    }

    /// <summary>
    /// Checks if a specific <see cref="GameAction"/> is currently held down.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the mapped input for the action is held down.</returns>
    public bool GetAction(GameAction action)
    {
        return GetInputDefinitionForAction(action).GetInput();
    }

    /// <summary>
    /// Checks if a specific <see cref="GameAction"/> was released (pressed up) in the current frame.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the mapped input for the action was released.</returns>
    public bool GetActionUp(GameAction action)
    {
        return GetInputDefinitionForAction(action).GetInputUp();
    }

    /// <summary>
    /// Gets the raw axis value for a specific <see cref="GameAction"/>.
    /// This is useful for continuous actions like movement or camera rotation (e.g., gamepad stick input).
    /// </summary>
    /// <param name="action">The action to check (should be mapped to an Axis).</param>
    /// <returns>The axis value (typically -1 to 1) or 0 if not an axis or action is not mapped.</returns>
    public float GetActionAxis(GameAction action)
    {
        return GetInputDefinitionForAction(action).GetAxis();
    }

    /// <summary>
    /// Gets the unscaled raw axis value for a specific <see cref="GameAction"/>.
    /// Useful for direct input, ignoring smoothing applied in Unity's Input Manager.
    /// </summary>
    /// <param name="action">The action to check (should be mapped to an Axis).</param>
    /// <returns>The axis value (typically -1 to 1) or 0 if not an axis or action is not mapped.</returns>
    public float GetActionAxisRaw(GameAction action)
    {
        return GetInputDefinitionForAction(action).GetAxisRaw();
    }


    // --- Runtime Remapping Functionality (Optional but highly recommended for user-configurable controls) ---

    /// <summary>
    /// Remaps a <see cref="GameAction"/> to a new <see cref="InputDefinition"/> at runtime.
    /// This is essential for implementing in-game control customization menus.
    /// </summary>
    /// <param name="actionToRemap">The action whose input binding should be changed.</param>
    /// <param name="newInput">The new physical input to map to the action.</param>
    public void RemapAction(GameAction actionToRemap, InputDefinition newInput)
    {
        if (actionToRemap == GameAction.None)
        {
            Debug.LogWarning("Cannot remap 'GameAction.None'.", this);
            return;
        }

        // Update the dictionary with the new mapping.
        // If the action already exists, its binding is updated. If not, a new binding is added.
        currentActionMap[actionToRemap] = newInput;

        Debug.Log($"Action '{actionToRemap}' remapped to " +
                  $"{newInput.inputType} " +
                  (newInput.inputType == InputType.Key ? $"KeyCode.{newInput.keyCode}" : "") +
                  (newInput.inputType == InputType.Button ? $"Button: '{newInput.buttonName}'" : "") +
                  (newInput.inputType == InputType.Axis ? $"Axis: '{newInput.axisName}'" : ""));

        // In a real game, after remapping, you would typically save these changes
        // to a persistent storage like PlayerPrefs or a configuration file.
        // Example: SaveCurrentBindings(); (see placeholder below)
    }

    /// <summary>
    /// Placeholder for saving the current action map to persistent storage.
    /// In a real game, this would serialize `currentActionMap` (or a derived serializable list)
    /// to JSON, XML, or PlayerPrefs to store user-customized controls.
    /// </summary>
    public void SaveCurrentBindings()
    {
        Debug.Log("Saving current action map... (Functionality not fully implemented in this example.)");
        // Example implementation:
        // List<ActionBinding> savableBindings = new List<ActionBinding>();
        // foreach (var entry in currentActionMap)
        // {
        //     savableBindings.Add(new ActionBinding { action = entry.Key, input = entry.Value });
        // }
        // string json = JsonUtility.ToJson(new SerializableBindingList { Bindings = savableBindings });
        // PlayerPrefs.SetString("CustomKeyBindings", json);
        // PlayerPrefs.Save();
    }

    /// <summary>
    /// Placeholder for loading an action map from persistent storage.
    /// In a real game, this would deserialize from a saved source (e.g., PlayerPrefs)
    /// and then update `currentActionMap` accordingly.
    /// </summary>
    public void LoadSavedBindings()
    {
        Debug.Log("Loading saved action map... (Functionality not fully implemented in this example.)");
        // Example implementation:
        // if (PlayerPrefs.HasKey("CustomKeyBindings"))
        // {
        //     string json = PlayerPrefs.GetString("CustomKeyBindings");
        //     SerializableBindingList loadedData = JsonUtility.FromJson<SerializableBindingList>(json);
        //     currentActionMap.Clear();
        //     foreach (var loadedBinding in loadedData.Bindings)
        //     {
        //         currentActionMap[loadedBinding.action] = loadedBinding.input;
        //     }
        //     Debug.Log($"Loaded {currentActionMap.Count} custom bindings.");
        // } else {
        //     Debug.Log("No saved custom bindings found. Using default.");
        //     InitializeActionMap(); // Fallback to defaults if no saved bindings
        // }
    }

    /// <summary>
    /// Resets the action map to the default bindings defined in the Inspector.
    /// </summary>
    public void ResetToDefaultBindings()
    {
        Debug.Log("Resetting action map to default bindings.");
        InitializeActionMap(); // Re-initialize from the defaultBindings list
        // Optionally, save the default bindings as the new active bindings here.
        // SaveCurrentBindings();
    }

    // A helper class for JSON serialization of the Dictionary if needed for saving/loading.
    // [Serializable]
    // private class SerializableBindingList { public List<ActionBinding> Bindings; }
}

// ========================================================================================================================
// SECTION 4: Example Player Controller - Demonstrating usage of ActionInputMap
//
// IMPORTANT: In a real project, this class (PlayerController) should be in its own C# file
// named `PlayerController.cs`. For this example, it's included here for self-containment.
// ========================================================================================================================

/// <summary>
/// An example player controller that uses the <see cref="ActionInputMap"/> to react to player intents.
/// This script *does not* directly check <see cref="UnityEngine.Input.GetKeyDown"/> etc.
/// Instead, it queries the <see cref="ActionInputMap"/> for the state of abstract <see cref="GameAction"/>s.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Player needs a Rigidbody for physics-based movement/jump
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the player moves.")]
    public float moveSpeed = 5f;
    [Tooltip("Force applied when the player jumps.")]
    public float jumpForce = 10f;

    private Rigidbody rb;
    private bool isGrounded; // Basic ground check for jumping

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on PlayerController. Please add a Rigidbody component.", this);
            enabled = false; // Disable script if no Rigidbody to prevent errors
        }
        // Freeze rotation to prevent player from falling over easily
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Always check if the ActionInputMap instance is available.
        // It's a singleton, so it should be present if configured correctly in the scene.
        if (ActionInputMap.Instance == null)
        {
            Debug.LogWarning("ActionInputMap.Instance is null. Is the ActionInputMap component in your scene and active?", this);
            return;
        }

        // Handle various player actions by querying the ActionInputMap
        HandleMovement();
        HandleJump();
        HandleFire();
        HandleInteraction();
        HandlePause();
    }

    /// <summary>
    /// Handles player movement based on mapped actions for directional input.
    /// </summary>
    private void HandleMovement()
    {
        float horizontalInput = 0f;
        float verticalInput = 0f;

        // Query ActionInputMap for discrete directional actions.
        // For gamepads, you might map GameAction.MoveHorizontal and GameAction.MoveVertical
        // directly to axes and use GetActionAxis.
        if (ActionInputMap.Instance.GetAction(GameAction.MoveForward)) { verticalInput += 1f; }
        if (ActionInputMap.Instance.GetAction(GameAction.MoveBackward)) { verticalInput -= 1f; }
        if (ActionInputMap.Instance.GetAction(GameAction.MoveRight)) { horizontalInput += 1f; }
        if (ActionInputMap.Instance.GetAction(GameAction.MoveLeft)) { horizontalInput -= 1f; }

        // Create a movement vector based on input. Normalize to prevent faster diagonal movement.
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        // Apply movement relative to the player's current forward direction.
        // For a top-down game, you might use `Vector3.forward * moveDirection.z + Vector3.right * moveDirection.x;`
        Vector3 movement = transform.forward * moveDirection.z + transform.right * moveDirection.x;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);

        // Optional: If you want the player character to rotate to face the direction of movement.
        if (moveDirection != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(new Vector3(movement.x, 0, movement.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, 0.15f); // Smooth rotation
        }
    }

    /// <summary>
    /// Handles player jump based on the mapped jump action.
    /// Includes a basic ground check to prevent mid-air jumps.
    /// </summary>
    private void HandleJump()
    {
        // Only allow jump if the jump action is pressed down AND the player is grounded.
        if (ActionInputMap.Instance.GetActionDown(GameAction.Jump) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; // Player is no longer grounded immediately after jumping
            Debug.Log("Player Jumped!", this);
        }
    }

    /// <summary>
    /// Handles player firing based on the mapped fire action.
    /// </summary>
    private void HandleFire()
    {
        if (ActionInputMap.Instance.GetActionDown(GameAction.Fire))
        {
            Debug.Log("Player fired their weapon! (Placeholder for weapon logic)", this);
            // Add logic here to instantiate a projectile, play animation, reduce ammo, etc.
        }
    }

    /// <summary>
    /// Handles player interaction based on the mapped interact action.
    /// </summary>
    private void HandleInteraction()
    {
        if (ActionInputMap.Instance.GetActionDown(GameAction.Interact))
        {
            Debug.Log("Player interacted with something! (Placeholder for interaction logic)", this);
            // Add logic here to interact with nearby objects (e.g., open door, pick up item, talk to NPC).
        }
    }

    /// <summary>
    /// Handles pausing/unpausing the game based on the mapped pause action.
    /// </summary>
    private void HandlePause()
    {
        if (ActionInputMap.Instance.GetActionDown(GameAction.Pause))
        {
            // Toggle Time.timeScale to pause/unpause the game.
            Time.timeScale = (Time.timeScale > 0) ? 0 : 1;
            Debug.Log($"Game {(Time.timeScale > 0 ? "resumed" : "paused")}!", this);
        }
    }

    // --- Basic Ground Check (for jumping) ---
    // Using OnCollisionStay for a simple ground check. Raycasting or sphere casting is often more robust.
    private void OnCollisionStay(Collision collision)
    {
        // Check if any contact point's normal is mostly upwards, indicating ground contact.
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.7f) // 0.7f corresponds to an angle of ~45 degrees
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(CollisionCollision collision)
    {
        // Simple heuristic: Assume not grounded if no longer colliding.
        // More sophisticated games might use a timer or a continuous raycast to confirm.
        isGrounded = false;
    }
}

// ========================================================================================================================
// SECTION 5: Example Usage Instructions (IMPORTANT: Read this to set up your Unity project!)
// ========================================================================================================================

/*
HOW TO USE THIS ACTIONINPUTMAP EXAMPLE IN UNITY:

1.  **Create C# Scripts:**
    *   Create a new C# script in your Unity project and name it `ActionInputMap.cs`.
        *   Copy all the code from SECTION 1, SECTION 2, and SECTION 3 into `ActionInputMap.cs`.
    *   Create another new C# script and name it `PlayerController.cs`.
        *   Copy all the code from SECTION 4 into `PlayerController.cs`.

2.  **Set up the Input Manager (Unity's Project Settings):**
    *   Go to `Edit > Project Settings > Input Manager`.
    *   Expand the `Axes` list.
    *   Ensure that default "Button" inputs like `Jump` (typically Spacebar) and `Fire1` (Left Ctrl or Left Mouse Button) are configured as you expect. This example relies on these default Unity button names.

3.  **Create an Input Manager GameObject:**
    *   In your current Unity Scene, create an empty GameObject (right-click in Hierarchy > `Create Empty`).
    *   Rename this new GameObject to `InputManager`.
    *   Drag and drop the `ActionInputMap.cs` script onto this `InputManager` GameObject in the Hierarchy or Inspector.

4.  **Configure Default Bindings in the Inspector:**
    *   Select the `InputManager` GameObject in your scene.
    *   In the Inspector, locate the `Action Input Map` component.
    *   Expand `Default Input Bindings`.
    *   Set the `Size` to the number of actions you want to map (e.g., 8 for this example).
    *   Fill in the bindings using the Inspector:

    **Example Bindings Setup:**
    ---
    Element 0:
        Action: MoveForward
        Input Type: Key
        Key Code: W

    Element 1:
        Action: MoveBackward
        Input Type: Key
        Key Code: S

    Element 2:
        Action: MoveLeft
        Input Type: Key
        Key Code: A

    Element 3:
        Action: MoveRight
        Input Type: Key
        Key Code: D

    Element 4:
        Action: Jump
        Input Type: Button
        Button Name: Jump       (This refers to the "Jump" button defined in Unity's Input Manager, usually Spacebar by default)

    Element 5:
        Action: Fire
        Input Type: Button
        Button Name: Fire1      (This refers to the "Fire1" button in Unity's Input Manager, often LeftCtrl or Left Mouse Button by default)

    Element 6:
        Action: Interact
        Input Type: Key
        Key Code: E

    Element 7:
        Action: Pause
        Input Type: Key
        Key Code: Escape
    ---
    *NOTE:* If you wanted to use an analog stick for movement, you would set `Input Type: Axis` and provide the `Axis Name` (e.g., "Horizontal" or "Vertical") for a `GameAction.MoveHorizontal` or `GameAction.MoveVertical`.

5.  **Create a Player GameObject:**
    *   In your scene, create a 3D Object (e.g., `GameObject > 3D Object > Cube`).
    *   Rename it to `Player`.
    *   Add a `Rigidbody` component to the `Player` (it's essential for physics-based movement and jumping).
        *   In the Rigidbody component, check `Freeze Rotation` for X, Y, and Z axes to prevent the player from tumbling.
    *   Drag and drop the `PlayerController.cs` script onto your `Player` GameObject.
    *   Adjust `Move Speed` and `Jump Force` in the `Player`'s Inspector if desired.
    *   (Optional) Add a simple `Plane` or `Cube` as ground for the player to stand on and jump from.

6.  **Run the Scene:**
    *   Press the `Play` button in the Unity Editor.
    *   Use the configured keys (W, A, S, D for movement, Space for Jump, Left Mouse Click/Ctrl for Fire, E for Interact, Esc for Pause) to control your `Player` Cube.
    *   Observe the `Debug.Log` messages in the Console window as you perform actions.

**Key Benefits of the ActionInputMap Pattern Demonstrated:**

*   **Decoupling:** Your `PlayerController` code doesn't directly check `Input.GetKeyDown(KeyCode.Space)`. Instead, it asks `ActionInputMap.Instance.GetActionDown(GameAction.Jump)`. This means the player logic is independent of the input method.
*   **Flexibility & Maintainability:** To change the jump key from Space to, say, Left Shift, you *only* need to update the `InputManager` GameObject's `ActionInputMap` component in the Inspector. No code changes required!
*   **Runtime Remapping:** The `RemapAction()` method shows how you could build an in-game options menu for players to customize their controls.
*   **Readability:** The game logic (`HandleJump()`, `HandleFire()`) is clear about the player's *intent*, rather than being cluttered with specific key/button checks.
*   **Scalability:** Adding new actions or supporting different input devices (gamepads, touch) is more organized. You just add to `GameAction` enum and configure new `InputDefinition`s in the map.
*/
```