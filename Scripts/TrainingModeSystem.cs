// Unity Design Pattern Example: TrainingModeSystem
// This script demonstrates the TrainingModeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'TrainingModeSystem' design pattern in Unity allows developers to create a dedicated environment within their game for players to practice mechanics, test features, or experiment without the usual game constraints (e.g., health limits, resource costs, time limits, penalties for failure).

It typically involves:
1.  **A Central Manager:** A `TrainingModeManager` that controls the overall state (active/inactive) and provides core training functionalities.
2.  **Observable/Reactable Systems:** Game systems (player health, enemy AI, timers, resource managers) that can *observe* the training mode's state and adjust their behavior accordingly. This can be achieved through events or interfaces.
3.  **Training Tools:** Specific actions or overrides available only in training mode (e.g., infinite health, enemy spawning, level resets, time manipulation).

This example demonstrates a practical implementation where the `TrainingModeManager` acts as a central hub, and other game components can subscribe to its events to change their behavior. It also shows direct manipulation for simple overrides.

---

### **1. Core TrainingModeManager Script (`TrainingModeManager.cs`)**

This script serves as the central hub for our training mode system. Create a new C# script named `TrainingModeManager.cs` in your Unity project and paste the following code:

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // For potential list of enemies, etc.

/// <summary>
/// The TrainingModeManager is a singleton that controls the game's training mode.
/// It provides functionality to activate/deactivate training mode and offers various
/// training tools (e.g., healing player, spawning enemies, toggling invincibility).
/// Other game systems can subscribe to its events to adjust their behavior accordingly.
/// </summary>
public class TrainingModeManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Ensures there's only one instance of the TrainingModeManager throughout the game.
    public static TrainingModeManager Instance { get; private set; }

    // --- Configuration via Inspector ---
    [Header("Training Mode Settings")]
    [SerializeField]
    [Tooltip("Key to toggle Training Mode ON/OFF.")]
    private KeyCode toggleTrainingModeKey = KeyCode.F1;

    [SerializeField]
    [Tooltip("Whether Training Mode should start active on game launch.")]
    private bool startActive = false;

    // --- Internal State ---
    private bool _isTrainingModeActive;
    public bool IsTrainingModeActive
    {
        get => _isTrainingModeActive;
        private set
        {
            if (_isTrainingModeActive != value)
            {
                _isTrainingModeActive = value;
                // Notify all subscribers that the training mode state has changed.
                OnTrainingModeToggled?.Invoke(_isTrainingModeActive);
                Debug.Log($"Training Mode Toggled: {_isTrainingModeActive}");
            }
        }
    }

    // --- Events for Other Systems to Subscribe ---
    /// <summary>
    /// Event triggered when Training Mode is toggled (activated or deactivated).
    /// Subscribers receive a boolean indicating if training mode is now active (true) or inactive (false).
    /// </summary>
    public event Action<bool> OnTrainingModeToggled;

    // --- Training Mode Tools (Examples) ---
    [Header("Training Mode Tools References")]
    [SerializeField]
    [Tooltip("Reference to the player's GameObject. Used for actions like healing or resetting position.")]
    private GameObject playerGameObject; // Assign your Player GameObject here

    [SerializeField]
    [Tooltip("Prefab of an enemy to spawn.")]
    private GameObject enemyPrefab; // Assign an Enemy Prefab here

    [SerializeField]
    [Tooltip("Point in the world where enemies will be spawned.")]
    private Transform enemySpawnPoint; // Assign an empty GameObject as a spawn point

    [SerializeField]
    [Tooltip("Reference to the PlayerHealth component for invincibility toggling.")]
    private PlayerHealth examplePlayerHealth; // Assign your Player's Health component here

    [SerializeField]
    [Tooltip("Reference to a GameTimer component if one exists, for pausing/resetting.")]
    private GameTimer exampleGameTimer; // Assign your Game Timer component here

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: Keep manager alive across scenes

        // Initialize training mode state
        _isTrainingModeActive = startActive;
    }

    private void Start()
    {
        // Fire the event on Start so components that initialize after this can get the initial state.
        OnTrainingModeToggled?.Invoke(_isTrainingModeActive);

        // Example: If PlayerHealth isn't assigned via inspector, try to find it.
        // In a real project, consider more robust dependency injection or manager systems.
        if (examplePlayerHealth == null && playerGameObject != null)
        {
            examplePlayerHealth = playerGameObject.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        // Listen for the toggle key
        if (Input.GetKeyDown(toggleTrainingModeKey))
        {
            ToggleTrainingMode();
        }
    }

    // --- Public Methods for Training Mode Control ---

    /// <summary>
    /// Toggles the training mode state (ON to OFF, or OFF to ON).
    /// </summary>
    public void ToggleTrainingMode()
    {
        IsTrainingModeActive = !IsTrainingModeActive;
    }

    /// <summary>
    /// Activates training mode.
    /// </summary>
    public void EnableTrainingMode()
    {
        IsTrainingModeActive = true;
    }

    /// <summary>
    /// Deactivates training mode.
    /// </summary>
    public void DisableTrainingMode()
    {
        IsTrainingModeActive = false;
    }

    // --- Training Mode Specific Actions (Callable from UI or other scripts) ---

    /// <summary>
    /// Heals the player to full health if a PlayerHealth component is available.
    /// </summary>
    public void HealPlayer()
    {
        if (!IsTrainingModeActive) return;

        if (examplePlayerHealth != null)
        {
            examplePlayerHealth.Heal(examplePlayerHealth.MaxHealth);
            Debug.Log("Training Mode: Player healed to full.");
        }
        else
        {
            Debug.LogWarning("Training Mode: PlayerHealth component not assigned or found.");
        }
    }

    /// <summary>
    /// Spawns an enemy at the designated spawn point.
    /// </summary>
    public void SpawnEnemy()
    {
        if (!IsTrainingModeActive) return;

        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            Instantiate(enemyPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
            Debug.Log("Training Mode: Enemy spawned.");
        }
        else
        {
            Debug.LogWarning("Training Mode: Enemy Prefab or Spawn Point not assigned.");
        }
    }

    /// <summary>
    /// Resets the player's position to a predefined point (e.g., start of a section).
    /// For this example, it just logs a message. In a real game, you'd set
    /// playerGameObject.transform.position.
    /// </summary>
    public void ResetPlayerPosition()
    {
        if (!IsTrainingModeActive) return;

        if (playerGameObject != null)
        {
            // Example: playerGameObject.transform.position = Vector3.zero;
            // You would likely have specific reset points for different scenarios.
            Debug.Log("Training Mode: Player position reset (conceptual).");
        }
        else
        {
            Debug.LogWarning("Training Mode: Player GameObject not assigned.");
        }
    }

    /// <summary>
    /// Toggles the player's invincibility state.
    /// This directly interacts with the PlayerHealth component.
    /// </summary>
    public void TogglePlayerInvincibility()
    {
        if (!IsTrainingModeActive) return;

        if (examplePlayerHealth != null)
        {
            examplePlayerHealth.SetInvincible(!examplePlayerHealth.IsInvincible);
            Debug.Log($"Training Mode: Player Invincibility: {examplePlayerHealth.IsInvincible}");
        }
        else
        {
            Debug.LogWarning("Training Mode: PlayerHealth component not assigned or found.");
        }
    }

    /// <summary>
    /// Slows down or resets time scale.
    /// </summary>
    public void ToggleSlowMotion()
    {
        if (!IsTrainingModeActive) return;

        if (Time.timeScale < 1.0f)
        {
            Time.timeScale = 1.0f; // Reset to normal
            Debug.Log("Training Mode: Time scale reset to normal.");
        }
        else
        {
            Time.timeScale = 0.2f; // Slow down
            Debug.Log("Training Mode: Time scale slowed down to 0.2.");
        }
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Adjust fixed delta time for physics
    }

    /// <summary>
    /// Resets the game timer, if a GameTimer component is assigned.
    /// </summary>
    public void ResetGameTimer()
    {
        if (!IsTrainingModeActive) return;

        if (exampleGameTimer != null)
        {
            exampleGameTimer.ResetTimer();
            Debug.Log("Training Mode: Game Timer reset.");
        }
        else
        {
            Debug.LogWarning("Training Mode: GameTimer component not assigned or found.");
        }
    }

    /// <summary>
    /// Toggles the game timer's pause state, if a GameTimer component is assigned.
    /// </summary>
    public void ToggleGameTimerPause()
    {
        if (!IsTrainingModeActive) return;

        if (exampleGameTimer != null)
        {
            exampleGameTimer.TogglePause();
            Debug.Log($"Training Mode: Game Timer Pause: {exampleGameTimer.IsPaused}");
        }
        else
        {
            Debug.LogWarning("Training Mode: GameTimer component not assigned or found.");
        }
    }


    // --- Debug UI (for demonstration purposes) ---
    private void OnGUI()
    {
        GUI.skin.label.fontSize = 20;
        GUI.skin.button.fontSize = 18;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));

        GUILayout.Label($"Training Mode: {(IsTrainingModeActive ? "<color=green>ACTIVE</color>" : "<color=red>INACTIVE</color>")}", new GUIStyle { richText = true, fontSize = 24 });
        GUILayout.Label($"Press '{toggleTrainingModeKey}' to Toggle", new GUIStyle { fontSize = 18 });

        if (!IsTrainingModeActive)
        {
            GUILayout.EndArea();
            return;
        }

        GUILayout.Space(10);
        GUILayout.Label("--- Training Tools ---");

        if (GUILayout.Button("Heal Player")) { HealPlayer(); }
        if (GUILayout.Button("Spawn Enemy")) { SpawnEnemy(); }
        if (GUILayout.Button("Reset Player Position (Conceptual)")) { ResetPlayerPosition(); }
        if (GUILayout.Button("Toggle Player Invincibility")) { TogglePlayerInvincibility(); }
        if (GUILayout.Button("Toggle Slow Motion")) { ToggleSlowMotion(); }
        if (GUILayout.Button("Reset Game Timer")) { ResetGameTimer(); }
        if (GUILayout.Button("Toggle Game Timer Pause")) { ToggleGameTimerPause(); }

        GUILayout.EndArea();
    }
}
```

---

### **2. Example Client Scripts (How other systems react)**

These scripts demonstrate how other game components would subscribe to `TrainingModeManager`'s events or be directly manipulated. Create these as separate C# scripts in your project.

#### **2.1. `PlayerHealth.cs`**

This script manages player health and can be made invincible by the training mode.

```csharp
using UnityEngine;
using System;

/// <summary>
/// Example PlayerHealth component that reacts to Training Mode.
/// It can become invincible when training mode is active.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    private int maxHealth = 100;
    public int MaxHealth => maxHealth;

    private int currentHealth;
    public int CurrentHealth => currentHealth;

    private bool _isInvincible = false;
    public bool IsInvincible => _isInvincible;

    public event Action<int, int> OnHealthChanged; // Current, Max
    public event Action OnPlayerDied;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void OnEnable()
    {
        // Subscribe to the TrainingModeManager's toggle event
        if (TrainingModeManager.Instance != null)
        {
            TrainingModeManager.Instance.OnTrainingModeToggled += OnTrainingModeToggled;
            // Apply initial state in case TrainingModeManager was already active
            SetInvincible(TrainingModeManager.Instance.IsTrainingModeActive);
        }
        else
        {
            Debug.LogWarning("PlayerHealth: TrainingModeManager.Instance is null. Cannot subscribe to events.");
        }
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and ensure clean shutdown
        if (TrainingModeManager.Instance != null)
        {
            TrainingModeManager.Instance.OnTrainingModeToggled -= OnTrainingModeToggled;
        }
    }

    /// <summary>
    /// Called when Training Mode is toggled.
    /// Sets invincibility based on training mode's active state.
    /// </summary>
    /// <param name="isActive">True if training mode is now active, false otherwise.</param>
    private void OnTrainingModeToggled(bool isActive)
    {
        // In this example, training mode automatically grants invincibility.
        // You could also have the TrainingModeManager explicitly call SetInvincible.
        // Or, more complex logic based on specific training mode sub-settings.
        _isInvincible = isActive;
        Debug.Log($"PlayerHealth: {(isActive ? "Entered" : "Exited")} Training Mode. Invincibility: {_isInvincible}");
    }

    /// <summary>
    /// Sets the player's invincibility state.
    /// This method is also called directly by TrainingModeManager's TogglePlayerInvincibility().
    /// </summary>
    /// <param name="invincible">True for invincible, false otherwise.</param>
    public void SetInvincible(bool invincible)
    {
        _isInvincible = invincible;
        Debug.Log($"PlayerHealth: Invincibility set to {_isInvincible}");
    }

    public void TakeDamage(int amount)
    {
        if (_isInvincible)
        {
            Debug.Log("Player is invincible, no damage taken.");
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // Ensure health doesn't go below 0
        Debug.Log($"Player took {amount} damage. Current Health: {currentHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Ensure health doesn't exceed max
        Debug.Log($"Player healed {amount}. Current Health: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        OnPlayerDied?.Invoke();
        // Handle game over logic
    }
}
```

#### **2.2. `GameTimer.cs`**

This script manages a simple game timer, which can be paused or reset by the training mode.

```csharp
using UnityEngine;
using System;

/// <summary>
/// Example GameTimer component that can be paused/reset by Training Mode.
/// </summary>
public class GameTimer : MonoBehaviour
{
    [SerializeField]
    private float initialTime = 60f; // Seconds
    private float currentTime;
    private bool isPaused = false;
    public bool IsPaused => isPaused;

    public event Action<float> OnTimerUpdated;
    public event Action OnTimerFinished;

    void Awake()
    {
        currentTime = initialTime;
    }

    void Update()
    {
        if (isPaused) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            OnTimerUpdated?.Invoke(currentTime);
        }
        else if (currentTime <= 0 && initialTime > 0) // Only fire once if it's a countdown
        {
            currentTime = 0; // Ensure it doesn't go negative
            OnTimerFinished?.Invoke();
            Debug.Log("Game Timer Finished!");
            // Optionally pause or disable timer after finishing
            isPaused = true;
        }
    }

    /// <summary>
    /// Toggles the pause state of the timer.
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        Debug.Log($"GameTimer: Pause state toggled to {isPaused}");
    }

    /// <summary>
    /// Resets the timer to its initial value and unpauses it.
    /// </summary>
    public void ResetTimer()
    {
        currentTime = initialTime;
        isPaused = false;
        OnTimerUpdated?.Invoke(currentTime);
        Debug.Log("GameTimer: Timer reset.");
    }

    /// <summary>
    /// Sets the timer to a specific value.
    /// </summary>
    public void SetTimer(float time)
    {
        currentTime = time;
        OnTimerUpdated?.Invoke(currentTime);
        Debug.Log($"GameTimer: Timer set to {time} seconds.");
    }
}
```

#### **2.3. `EnemyAI.cs` (Conceptual)**

This demonstrates how an enemy might react, for instance, by being "dumbed down" in training mode.

```csharp
using UnityEngine;

/// <summary>
/// Conceptual EnemyAI that could react to Training Mode (e.g., become passive).
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [SerializeField]
    private float normalSpeed = 3f;
    [SerializeField]
    private float trainingModeSpeed = 0.5f; // Slower in training mode
    [SerializeField]
    private bool aggressiveInTraining = false; // Maybe enemies are passive in training?

    private Rigidbody rb;
    private bool isTrainingModeActive = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (TrainingModeManager.Instance != null)
        {
            TrainingModeManager.Instance.OnTrainingModeToggled += OnTrainingModeToggled;
            // Apply initial state
            OnTrainingModeToggled(TrainingModeManager.Instance.IsTrainingModeActive);
        }
    }

    void OnDisable()
    {
        if (TrainingModeManager.Instance != null)
        {
            TrainingModeManager.Instance.OnTrainingModeToggled -= OnTrainingModeToggled;
        }
    }

    private void OnTrainingModeToggled(bool isActive)
    {
        isTrainingModeActive = isActive;
        if (isTrainingModeActive)
        {
            Debug.Log($"{gameObject.name}: Entered Training Mode. Adapting AI.");
            // Example: Make enemy passive, or slower, or stop attacking.
            // For now, it just changes speed.
            SetMovementSpeed(trainingModeSpeed);
        }
        else
        {
            Debug.Log($"{gameObject.name}: Exited Training Mode. Restoring AI.");
            SetMovementSpeed(normalSpeed);
        }
    }

    private void SetMovementSpeed(float speed)
    {
        if (rb != null)
        {
            // This is a simple example; actual AI would use navmesh or other movement.
            // rb.velocity = transform.forward * speed;
            Debug.Log($"{gameObject.name} movement speed set to {speed}");
        }
    }

    void FixedUpdate()
    {
        // Simple conceptual movement
        if (!isTrainingModeActive || (isTrainingModeActive && aggressiveInTraining))
        {
            // Move forward at current speed (normal or training mode speed)
            // Example: rb.MovePosition(rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime);
        }
    }
}
```

---

### **3. Setting up in Unity**

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject and name it `TrainingModeManager`.
2.  **Attach `TrainingModeManager.cs`:** Drag and drop the `TrainingModeManager.cs` script onto this new GameObject.
3.  **Configure `TrainingModeManager` in Inspector:**
    *   **Toggle Training Mode Key:** Set your desired key (e.g., `F1` is default).
    *   **Player GameObject:** Drag your Player's main GameObject into this slot.
    *   **Enemy Prefab:** Create a simple cube, add a `Rigidbody`, and turn it into a Prefab (e.g., `EnemyPrefab`). Drag this Prefab into the slot.
    *   **Enemy Spawn Point:** Create an empty GameObject, position it where you want enemies to spawn, and drag it into this slot.
    *   **Example Player Health:** Select your Player GameObject. Add the `PlayerHealth.cs` script to it. Then drag the `PlayerHealth` component from your Player GameObject into this slot on the `TrainingModeManager`.
    *   **Example Game Timer:** Create another empty GameObject, name it `GameTimer`, attach `GameTimer.cs` to it. Then drag this `GameTimer` component into the `TrainingModeManager`'s slot.
4.  **Add `PlayerHealth.cs` to your Player:** Make sure your player character GameObject has the `PlayerHealth.cs` script attached.
5.  **Add `GameTimer.cs` to a GameObject:** Ensure you have a GameObject with `GameTimer.cs` attached (as mentioned in step 3).
6.  **Create an Enemy Prefab:** Create a simple 3D object (e.g., Cube), add a `Rigidbody` (optional, for physics). Add the `EnemyAI.cs` script to it (optional for the manager to function, but good for demonstrating the `OnTrainingModeToggled` event). Convert this GameObject into a Prefab and assign it to the `TrainingModeManager`.

---

### **How the TrainingModeSystem Pattern Works Here**

1.  **Central Control (`TrainingModeManager`):**
    *   It's a **Singleton**, meaning there's only one instance globally accessible via `TrainingModeManager.Instance`. This makes it easy for any other script to interact with it.
    *   It holds the core state (`IsTrainingModeActive`).
    *   It provides an **Event (`OnTrainingModeToggled`)** that acts as a broadcast mechanism. Any other system interested in the training mode's state can subscribe to this event.
    *   It exposes public methods for **Training Tools** (`HealPlayer()`, `SpawnEnemy()`, `TogglePlayerInvincibility()`, etc.). These methods are usually only effective when `IsTrainingModeActive` is true.

2.  **Observable Components (`PlayerHealth`, `GameTimer`, `EnemyAI`):**
    *   These components **subscribe** to the `TrainingModeManager.Instance.OnTrainingModeToggled` event in their `OnEnable()` method.
    *   They **unsubscribe** in `OnDisable()` to prevent memory leaks.
    *   When the event is triggered, their respective `OnTrainingModeToggled(bool isActive)` method is called. This method then implements logic to alter their behavior based on the `isActive` state (e.g., `PlayerHealth` sets itself invincible, `EnemyAI` might slow down).
    *   Some components might also expose public methods (`SetInvincible`, `ResetTimer`, `TogglePause`) that the `TrainingModeManager` can call **directly** for specific, simple overrides. This is a mix of observer and direct control, common in practical Unity development.

3.  **Real-World Use Case:**
    *   **Fighting Games:** Practice combos, unlimited special meter, specific enemy AI behaviors, frame data display.
    *   **Survival Games:** Infinite resources, no hunger/thirst, disable hostile creatures.
    *   **Platformers:** Infinite jumps, invincibility to falling, no level timer.
    *   **Racing Games:** Ghost cars, track sections replay, tire wear disable.

This setup provides a robust and flexible way to add a training mode to your game without tightly coupling your core game logic to the training functionalities. Developers can easily add new training tools to `TrainingModeManager` or create new game components that react to `OnTrainingModeToggled` with minimal effort.