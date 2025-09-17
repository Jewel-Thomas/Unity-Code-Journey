// Unity Design Pattern Example: InputContextSystem
// This script demonstrates the InputContextSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `InputContextSystem` design pattern provides a robust way to manage input in games by organizing it into distinct 'contexts'. This prevents conflicting input actions (e.g., trying to move your character while in a menu) and simplifies the logic for different game states.

Here's a complete C# Unity example demonstrating this pattern, ready to be dropped into your project.

---

### How to Use This Example:

1.  **Create a new C# script** in your Unity project, name it `InputContextSystem.cs`, and copy all the code below into it.
2.  **Create an empty GameObject** in your scene and name it `InputManager`.
3.  **Attach the `InputContextSystem` script** to the `InputManager` GameObject.
    *   (Optional: If you want the InputContextSystem to persist across scene loads, leave `DontDestroyOnLoad(gameObject);` uncommented in its `Awake` method.)
4.  **Create another empty GameObject** for your player (e.g., `Player`).
5.  **Attach the `PlayerInputHandler` script** to the `Player` GameObject. You can drag your `Player` object around to see it move.
6.  **Create an empty GameObject** for your UI (e.g., `UIManager`).
7.  **Attach the `UIInventoryInputHandler` script** to the `UIManager` GameObject.
    *   Create a simple UI Panel (GameObject -> UI -> Panel), name it `InventoryPanel`, and drag it into the `_inventoryPanel` slot of the `UIInventoryInputHandler` component in the Inspector. Set the `InventoryPanel` to inactive by default.
8.  **Create an empty GameObject** for your cutscene manager (e.g., `CutsceneManager`).
9.  **Attach the `CutsceneInputHandler` script** to the `CutsceneManager` GameObject.
10. **Create a final empty GameObject** (e.g., `GameContextSwitcher`).
11. **Attach the `ContextSwitcherExample` script** to the `GameContextSwitcher` GameObject.
    *   Drag your `UIManager` GameObject (which has `UIInventoryInputHandler`) into the `_inventoryHandler` slot.
    *   Drag your `CutsceneManager` GameObject (which has `CutsceneInputHandler`) into the `_cutsceneHandler` slot.

**Run the scene:**
*   Initially, you are in `Gameplay` context. Use **WASD** to move the `Player` cube, and **Space** to simulate a shot/jump.
*   Press **I** to open the inventory. The context will switch to `UI_Inventory`. Now, **WASD/Space** will no longer affect the player. You can use **Arrow Keys** to simulate UI navigation, **Enter** to confirm, and **Escape** or **I** again to close the inventory and return to `Gameplay`.
*   Press **C** to start a cutscene. The context will switch to `Cutscene`. Now, only **Escape** will register (to skip the cutscene).
*   Observe the `Debug.Log` messages in the console to see the context changes and input processing.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action delegate

// This script demonstrates the InputContextSystem design pattern.
// It allows you to manage different sets of input actions based on the current game state (context).
// For example, character movement input should only be active during gameplay,
// while menu navigation input should only be active when a menu is open.

/// <summary>
/// Defines the different input contexts your game can have.
/// Each enum value represents a distinct state where a specific set of inputs is relevant.
/// </summary>
public enum InputContext
{
    None, // Default or invalid state, useful for initialization
    Gameplay, // Player character movement, combat, general interaction
    UI_Inventory, // Inventory menu navigation, item selection
    Cutscene, // Skipping cutscenes, dialogue choices
    Dialogue, // Responding to dialogue options
    PauseMenu, // Pause menu navigation, settings adjustment
    // Add more as your game needs different input states (e.g., Driving, Building, Spectator)
}

/// <summary>
/// Interface for any class that wishes to process input within a specific context.
/// By implementing this interface, a class agrees to provide a method for handling input.
/// </summary>
public interface IInputHandler
{
    /// <summary>
    /// Called by the InputContextSystem when this handler's context is active.
    /// Implement this method to define how input is processed for this specific handler.
    /// This method will typically contain checks for input events (e.g., Input.GetKeyDown).
    /// </summary>
    void HandleInput();
}

/// <summary>
/// The central system for managing and dispatching input based on the current context.
/// This class follows the Singleton pattern to ensure a single, globally accessible instance
/// throughout the game, making it easy for any other script to register handlers or change contexts.
/// </summary>
public class InputContextSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Ensures that there is only one instance of InputContextSystem.
    public static InputContextSystem Instance { get; private set; }

    // --- Internal State ---
    // A dictionary to store lists of IInputHandlers, grouped by their associated InputContext.
    // When a context becomes active, only handlers in its corresponding list will receive input.
    private Dictionary<InputContext, List<IInputHandler>> _contextHandlers;

    // The currently active input context. Only handlers registered to this context will receive input.
    [Header("Current Input Context")]
    [SerializeField]
    private InputContext _currentActiveContext = InputContext.None;
    public InputContext CurrentActiveContext => _currentActiveContext; // Public getter for current context

    // --- Events ---
    // An event that other scripts can subscribe to, to be notified when the input context changes.
    // Useful for UI elements that might need to update based on the current context (e.g., show/hide controls).
    public static event Action<InputContext> OnContextChanged;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used here to set up the Singleton instance and initialize the dictionary.
    /// </summary>
    private void Awake()
    {
        // Singleton enforcement: If an instance already exists and it's not this one, destroy this new one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make this GameObject persist across scene loads.
            // Useful for central systems like input managers that you want available globally.
            DontDestroyOnLoad(gameObject); 
            
            _contextHandlers = new Dictionary<InputContext, List<IInputHandler>>();
            Debug.Log("<color=green>InputContextSystem Initialized.</color>");
        }
    }

    /// <summary>
    /// Update is called once per frame.
    /// This is where the magic happens: input is dispatched to the active context's handlers.
    /// </summary>
    private void Update()
    {
        // Check if there are any handlers registered for the current active context.
        if (_contextHandlers.TryGetValue(_currentActiveContext, out List<IInputHandler> handlers))
        {
            // If handlers exist, iterate through them and call their HandleInput() method.
            // This ensures that only the relevant input logic is executed for the current game state.
            foreach (IInputHandler handler in handlers)
            {
                handler.HandleInput();
            }
        }
    }

    /// <summary>
    /// Registers an IInputHandler to a specific input context.
    /// Handlers should call this method (typically in OnEnable or Awake) to make themselves known to the system.
    /// </summary>
    /// <param name="context">The <see cref="InputContext"/> the handler should respond to.</param>
    /// <param name="handler">The instance of the <see cref="IInputHandler"/> to register.</param>
    public void RegisterHandler(InputContext context, IInputHandler handler)
    {
        // If the context isn't in our dictionary yet, add it with a new list of handlers.
        if (!_contextHandlers.ContainsKey(context))
        {
            _contextHandlers[context] = new List<IInputHandler>();
        }

        // Only add the handler if it's not already in the list for that context (prevents duplicates).
        if (!_contextHandlers[context].Contains(handler))
        {
            _contextHandlers[context].Add(handler);
            Debug.Log($"<color=blue>Registered handler '{handler.GetType().Name}' for context: {context}</color>");
        }
    }

    /// <summary>
    /// Deregisters an IInputHandler from a specific input context.
    /// Handlers should call this method (typically in OnDisable or OnDestroy) to clean up.
    /// </summary>
    /// <param name="context">The <see cref="InputContext"/> the handler was registered for.</param>
    /// <param name="handler">The instance of the <see cref="IInputHandler"/> to deregister.</param>
    public void DeregisterHandler(InputContext context, IInputHandler handler)
    {
        // If the context exists and contains the handler, remove it.
        if (_contextHandlers.TryGetValue(context, out List<IInputHandler> handlers))
        {
            if (handlers.Remove(handler))
            {
                Debug.Log($"<color=orange>Deregistered handler '{handler.GetType().Name}' from context: {context}</color>");
            }
        }
    }

    /// <summary>
    /// Changes the currently active input context.
    /// This is the core method for switching between different game states.
    /// Only handlers registered to the new active context will receive input from this point on.
    /// </summary>
    /// <param name="newContext">The <see cref="InputContext"/> to activate.</param>
    public void SetActiveContext(InputContext newContext)
    {
        // Only change if the new context is different from the current one.
        if (_currentActiveContext != newContext)
        {
            _currentActiveContext = newContext;
            Debug.Log($"<color=purple>Input Context changed to: {_currentActiveContext}</color>");
            // Notify any subscribers that the context has changed.
            OnContextChanged?.Invoke(newContext);
        }
    }

    /// <summary>
    /// (Optional) Returns a copy of the list of handlers for a given context.
    /// Useful for debugging or inspecting registered handlers.
    /// </summary>
    /// <param name="context">The context to retrieve handlers for.</param>
    /// <returns>A new List containing the <see cref="IInputHandler"/>s for the specified context.</returns>
    public List<IInputHandler> GetHandlersForContext(InputContext context)
    {
        if (_contextHandlers.TryGetValue(context, out List<IInputHandler> handlers))
        {
            return new List<IInputHandler>(handlers); // Return a copy to prevent external modification
        }
        return new List<IInputHandler>();
    }
}

// --- Example Implementations of IInputHandler ---

/// <summary>
/// Example Input Handler: Controls player movement and actions during gameplay.
/// This script should be attached to your Player GameObject.
/// </summary>
public class PlayerInputHandler : MonoBehaviour, IInputHandler
{
    [Tooltip("Movement speed of the player.")]
    public float moveSpeed = 5f;

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// This is the ideal place to register the handler with the InputContextSystem.
    /// </summary>
    private void OnEnable()
    {
        if (InputContextSystem.Instance != null)
        {
            InputContextSystem.Instance.RegisterHandler(InputContext.Gameplay, this);
        }
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// This is the ideal place to deregister the handler to prevent it from receiving input
    /// when it's not supposed to (e.g., player is dead, or player object is pooled).
    /// </summary>
    private void OnDisable()
    {
        if (InputContextSystem.Instance != null)
        {
            InputContextSystem.Instance.DeregisterHandler(InputContext.Gameplay, this);
        }
    }

    /// <summary>
    /// Implements the IInputHandler interface. This method contains the specific input logic
    /// for the player during the Gameplay context.
    /// This code will ONLY execute when the InputContextSystem's CurrentActiveContext is InputContext.Gameplay.
    /// </summary>
    public void HandleInput()
    {
        // Get axis input for movement (WASD or Arrow Keys)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Apply movement to the GameObject's position
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // Check for jump/shoot button (e.g., Spacebar by default in Unity's Input Manager)
        if (Input.GetButtonDown("Jump"))
        {
            Debug.Log("Player performs action (shoots/jumps)! (Gameplay Context)");
        }
    }
}

/// <summary>
/// Example Input Handler: Manages input for an inventory or pause menu UI.
/// This script should be attached to a UI Manager GameObject.
/// </summary>
public class UIInventoryInputHandler : MonoBehaviour, IInputHandler
{
    [Tooltip("Reference to the actual UI panel for the inventory.")]
    [SerializeField] private GameObject _inventoryPanel; // Reference to the actual UI panel

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures the inventory panel starts in a closed state.
    /// </summary>
    private void Awake()
    {
        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(false); // Start with inventory closed
        }
    }

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Registers this handler for the UI_Inventory context.
    /// </summary>
    private void OnEnable()
    {
        if (InputContextSystem.Instance != null)
        {
            InputContextSystem.Instance.RegisterHandler(InputContext.UI_Inventory, this);
        }
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// Deregisters this handler from the UI_Inventory context.
    /// </summary>
    private void OnDisable()
    {
        if (InputContextSystem.Instance != null)
        {
            InputContextSystem.Instance.DeregisterHandler(InputContext.UI_Inventory, this);
        }
    }

    /// <summary>
    /// Implements the IInputHandler interface. This method contains the specific input logic
    /// for UI navigation during the UI_Inventory context.
    /// This code will ONLY execute when the InputContextSystem's CurrentActiveContext is InputContext.UI_Inventory.
    /// </summary>
    public void HandleInput()
    {
        // UI navigation inputs (e.g., Arrow Keys)
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log("UI: Navigating Up (Inventory Context)");
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log("UI: Navigating Down (Inventory Context)");
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("UI: Confirm Selection (Inventory Context)");
        }

        // Close inventory input (e.g., Escape key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("UI: Closing Inventory via Escape.");
            CloseInventory(); // Call the unified close method
        }
    }

    /// <summary>
    /// Opens the inventory UI and switches the input context to UI_Inventory.
    /// </summary>
    public void OpenInventory()
    {
        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(true);
        }
        InputContextSystem.Instance.SetActiveContext(InputContext.UI_Inventory);
        Debug.Log("Opening Inventory. Context set to UI_Inventory.");
    }

    /// <summary>
    /// Closes the inventory UI and switches the input context back to Gameplay.
    /// </summary>
    public void CloseInventory()
    {
        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(false);
        }
        InputContextSystem.Instance.SetActiveContext(InputContext.Gameplay);
        Debug.Log("Closing Inventory. Context set to Gameplay.");
    }
}

/// <summary>
/// Example Input Handler: Manages input specific to cutscenes (e.g., skipping).
/// This script should be attached to a Cutscene Manager GameObject.
/// </summary>
public class CutsceneInputHandler : MonoBehaviour, IInputHandler
{
    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Registers this handler for the Cutscene context.
    /// </summary>
    private void OnEnable()
    {
        if (InputContextSystem.Instance != null)
        {
            InputContextSystem.Instance.RegisterHandler(InputContext.Cutscene, this);
        }
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// Deregisters this handler from the Cutscene context.
    /// </summary>
    private void OnDisable()
    {
        if (InputContextSystem.Instance != null)
        {
            InputContextSystem.Instance.DeregisterHandler(InputContext.Cutscene, this);
        }
    }

    /// <summary>
    /// Implements the IInputHandler interface. This method contains the specific input logic
    /// for cutscenes.
    /// This code will ONLY execute when the InputContextSystem's CurrentActiveContext is InputContext.Cutscene.
    /// </summary>
    public void HandleInput()
    {
        // Check for input to skip the cutscene (e.g., Escape key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Cutscene: Skipping cutscene. Switching to Gameplay.");
            // In a real game, this would stop the cutscene animation/video and transition to gameplay.
            InputContextSystem.Instance.SetActiveContext(InputContext.Gameplay);
        }
    }

    /// <summary>
    /// Initiates a cutscene and switches the input context to Cutscene.
    /// </summary>
    public void StartCutscene()
    {
        // Simulate cutscene start (e.g., playing a video, triggering an animation sequence)
        Debug.Log("Starting Cutscene. Context set to Cutscene.");
        InputContextSystem.Instance.SetActiveContext(InputContext.Cutscene);
    }
}

/// <summary>
/// Example: A simple game manager or controller that orchestrates context changes.
/// This script demonstrates how other parts of your game would interact with the InputContextSystem
/// to switch between different input states.
/// This script should be attached to a dedicated Game Manager GameObject.
/// </summary>
public class ContextSwitcherExample : MonoBehaviour
{
    [Tooltip("Reference to the UIInventoryInputHandler in the scene.")]
    [SerializeField] private UIInventoryInputHandler _inventoryHandler;
    [Tooltip("Reference to the CutsceneInputHandler in the scene.")]
    [SerializeField] private CutsceneInputHandler _cutsceneHandler;

    /// <summary>
    /// Called before the first frame update.
    /// Ensures the InputContextSystem is available and sets the initial game context.
    /// </summary>
    private void Start()
    {
        // Basic check to ensure the InputContextSystem exists in the scene.
        if (InputContextSystem.Instance == null)
        {
            Debug.LogError("InputContextSystem not found in scene! Please add it to a GameObject (e.g., 'InputManager').", this);
            enabled = false; // Disable this script if the system isn't found
            return;
        }

        // Set the initial context of the game to Gameplay.
        InputContextSystem.Instance.SetActiveContext(InputContext.Gameplay);
        Debug.Log("Game started. Initial context: Gameplay.");
    }

    /// <summary>
    /// Update is called once per frame.
    /// Contains logic for external events that trigger context changes (e.g., pressing 'I' for inventory).
    /// </summary>
    private void Update()
    {
        // --- Toggle Inventory (Press 'I') ---
        // This demonstrates how a separate system (like a GameManager) can trigger context changes.
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (_inventoryHandler == null)
            {
                Debug.LogWarning("Inventory Handler not assigned to ContextSwitcherExample.", this);
                return;
            }

            // Check the current context to decide whether to open or close the inventory.
            if (InputContextSystem.Instance.CurrentActiveContext == InputContext.Gameplay)
            {
                _inventoryHandler.OpenInventory();
            }
            else if (InputContextSystem.Instance.CurrentActiveContext == InputContext.UI_Inventory)
            {
                _inventoryHandler.CloseInventory();
            }
        }

        // --- Start Cutscene (Press 'C') ---
        // This demonstrates how a game event (like reaching a trigger) could start a cutscene
        // and change the input context.
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (_cutsceneHandler == null)
            {
                Debug.LogWarning("Cutscene Handler not assigned to ContextSwitcherExample.", this);
                return;
            }

            // Only allow starting a cutscene from gameplay context for this example.
            if (InputContextSystem.Instance.CurrentActiveContext == InputContext.Gameplay)
            {
                _cutsceneHandler.StartCutscene();
            }
            else
            {
                Debug.Log("Cannot start cutscene while not in Gameplay context.");
            }
        }
    }
}
```