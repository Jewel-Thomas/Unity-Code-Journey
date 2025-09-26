// Unity Design Pattern Example: StaminaSystem
// This script demonstrates the StaminaSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete and practical Stamina System design pattern implemented in Unity using C#. It focuses on encapsulation, clear API, and event-driven communication for robust game development.

## StaminaSystem Design Pattern Explanation

The Stamina System design pattern centralizes all logic related to a "stamina" resource within a single, dedicated component. This promotes:

1.  **Encapsulation:** All internal mechanics (current stamina, max stamina, regeneration rates, delays) are managed within the `StaminaSystem` class. Other scripts interact with it through a well-defined public API (e.g., `ConsumeStamina`, `CanPerformAction`, `GainStamina`).
2.  **Modularity & Reusability:** The system can be attached to any GameObject (Player, Enemy, even certain environment objects) that needs stamina management without modifying the core game logic.
3.  **Loose Coupling (via Events):** Instead of other scripts directly polling or modifying the stamina, the `StaminaSystem` broadcasts events when its state changes (e.g., `OnStaminaChanged`, `OnStaminaPercentageChanged`). UI elements, sound managers, or other game systems can "listen" to these events without knowing the internal workings of the stamina system, leading to a more flexible and maintainable codebase (Observer Pattern).
4.  **Configurability:** All key parameters are exposed in the Unity Inspector, allowing designers to easily tweak stamina values, regeneration rates, and delays without touching code.

---

## Complete C# Unity Script: `StaminaSystem.cs`

This script can be dropped directly into a Unity project.

1.  Create a new C# script named `StaminaSystem.cs`.
2.  Copy and paste the code below into the script.
3.  Attach this script to your player character or any GameObject that needs stamina.
4.  Adjust the parameters in the Inspector.
5.  Refer to the "Example Usage" section within the script's comments to see how to integrate it with other game logic (like a `PlayerController`).

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent, allowing Inspector-based event hookup
using System;             // Required for Action (an alternative for code-based event subscriptions)

// The StaminaSystem Design Pattern:
// This pattern encapsulates all logic related to a 'stamina' resource within a single,
// reusable component. It handles consumption, regeneration, maximum limits, and
// provides events for other systems (like UI, sound effects) to react to stamina
// changes without needing direct references to the StaminaSystem itself (Observer Pattern).
// This promotes loose coupling and modularity in your game's architecture.

/// <summary>
/// Manages a character's stamina, including consumption, regeneration, and maximum capacity.
/// Implements the Stamina System design pattern.
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Properties")]
    [Tooltip("The maximum stamina this system can hold.")]
    [SerializeField]
    private float maxStamina = 100f;
    /// <summary>Public read-only access to the maximum stamina.</summary>
    public float MaxStamina => maxStamina;

    [Tooltip("The current stamina level.")]
    [SerializeField]
    private float currentStamina;
    /// <summary>Public read-only access to the current stamina.</summary>
    public float CurrentStamina => currentStamina;

    [Header("Regeneration")]
    [Tooltip("How much stamina regenerates per second.")]
    [SerializeField]
    private float regenerationRate = 10f; // stamina per second

    [Tooltip("Time in seconds after stamina consumption before regeneration begins.")]
    [SerializeField]
    private float regenerationDelay = 2f; // seconds

    private float lastConsumptionTime; // Time.time when stamina was last consumed

    // --- Events ---
    // UnityEvents are used here as they allow direct hook-up in the Unity Inspector,
    // which is highly practical for connecting UI elements (like a stamina bar slider)
    // or other GameObjects/scripts without writing additional code.

    [Header("Events")]
    [Tooltip("Invoked when the current stamina value changes. Provides the new current stamina.")]
    public UnityEvent<float> OnStaminaChanged;

    [Tooltip("Invoked when the current stamina percentage (0-1) changes. Useful for UI bars.")]
    public UnityEvent<float> OnStaminaPercentageChanged;

    [Tooltip("Invoked when the maximum stamina value changes. Provides the new max stamina.")]
    public UnityEvent<float> OnMaxStaminaChanged;

    // --- Initialization & Validation ---

    private void Awake()
    {
        // Ensure current stamina starts at max when the game begins.
        InitializeStamina();
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed in the Inspector.
    /// Useful for validating input and initializing values safely in edit mode.
    /// </summary>
    private void OnValidate()
    {
        // Clamp values to ensure they are non-negative.
        maxStamina = Mathf.Max(0, maxStamina);
        regenerationRate = Mathf.Max(0, regenerationRate);
        regenerationDelay = Mathf.Max(0, regenerationDelay);

        // Ensure current stamina never exceeds max and is not negative,
        // especially if maxStamina was lowered in the editor.
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        // If currentStamina was 0 and maxStamina is now set to a positive value,
        // initialize currentStamina to max for convenience in the editor.
        if (currentStamina == 0f && maxStamina > 0f)
        {
            currentStamina = maxStamina;
        }

        // Notify events in editor for immediate feedback if relevant systems are listening.
        // This is primarily for runtime, but can help visualize state in editor if events
        // are hooked up to debug components.
        OnStaminaChanged?.Invoke(currentStamina);
        OnStaminaPercentageChanged?.Invoke(GetStaminaPercentage());
        OnMaxStaminaChanged?.Invoke(maxStamina);
    }

    /// <summary>
    /// Initializes or resets the stamina to its maximum value.
    /// This method can be called externally (e.g., from a 'refill potion' item).
    /// </summary>
    public void InitializeStamina()
    {
        currentStamina = maxStamina;
        lastConsumptionTime = Time.time; // Reset the regeneration delay counter
        NotifyStaminaChange();
    }

    // --- Core Logic ---

    private void Update()
    {
        RegenerateStamina();
    }

    /// <summary>
    /// Handles stamina regeneration over time based on `regenerationRate` and `regenerationDelay`.
    /// </summary>
    private void RegenerateStamina()
    {
        // Only regenerate if not at max stamina and enough time has passed since last consumption.
        if (currentStamina < maxStamina && Time.time - lastConsumptionTime >= regenerationDelay)
        {
            float amountToRegen = regenerationRate * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina + amountToRegen);
            NotifyStaminaChange(); // Inform listeners about the change
        }
    }

    /// <summary>
    /// Checks if there is enough stamina to perform an action with the given cost.
    /// </summary>
    /// <param name="cost">The stamina cost of the action.</param>
    /// <returns>True if enough stamina is available, false otherwise.</returns>
    public bool CanPerformAction(float cost)
    {
        return currentStamina >= cost;
    }

    /// <summary>
    /// Consumes a specified amount of stamina.
    /// If the cost is greater than current stamina, current stamina will drop to 0.
    /// Triggers the `regenerationDelay` countdown.
    /// </summary>
    /// <param name="cost">The amount of stamina to consume.</param>
    /// <returns>True if stamina was successfully consumed (even if partially), false if cost was negative or 0, or already at 0 stamina.</returns>
    public bool ConsumeStamina(float cost)
    {
        if (cost <= 0)
        {
            Debug.LogWarning($"Attempted to consume non-positive stamina cost ({cost}). Cost must be positive.", this);
            return false;
        }
        if (currentStamina <= 0)
        {
            // Already out of stamina, cannot consume more.
            return false;
        }

        currentStamina = Mathf.Max(0, currentStamina - cost); // Ensure stamina doesn't go below 0
        lastConsumptionTime = Time.time; // Reset regeneration delay timer
        NotifyStaminaChange();
        return true;
    }

    /// <summary>
    /// Adds a specified amount of stamina.
    /// Stamina will not exceed `maxStamina`.
    /// </summary>
    /// <param name="amount">The amount of stamina to gain.</param>
    /// <returns>True if stamina was gained, false if amount was negative or 0, or already at max stamina.</returns>
    public bool GainStamina(float amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Attempted to gain non-positive stamina amount ({amount}). Amount must be positive.", this);
            return false;
        }
        if (currentStamina == maxStamina)
        {
            // Already at max stamina, cannot gain more.
            return false;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + amount); // Ensure stamina doesn't exceed max
        NotifyStaminaChange();
        return true;
    }

    /// <summary>
    /// Sets a new maximum stamina value.
    /// If the new max is less than current stamina, current stamina is clamped down to the new max.
    /// </summary>
    /// <param name="newMax">The new maximum stamina value.</param>
    public void SetMaxStamina(float newMax)
    {
        if (newMax < 0)
        {
            Debug.LogError("Max stamina cannot be negative. Value clamped to 0.", this);
            newMax = 0;
        }

        if (Mathf.Approximately(newMax, maxStamina)) return; // No change

        maxStamina = newMax;
        // Ensure current stamina is clamped if max was reduced
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        NotifyStaminaChange();
        OnMaxStaminaChanged?.Invoke(maxStamina); // Notify listeners about max stamina change
    }

    /// <summary>
    /// Resets the stamina to its maximum value, effectively "refilling" it instantly.
    /// </summary>
    public void ResetStamina()
    {
        InitializeStamina();
        Debug.Log("Stamina fully reset to " + maxStamina, this);
    }

    /// <summary>
    /// Gets the current stamina as a percentage of max stamina (0.0 to 1.0).
    /// Returns 0 if `maxStamina` is 0 to avoid division by zero.
    /// </summary>
    /// <returns>Stamina percentage as a float between 0 and 1.</returns>
    public float GetStaminaPercentage()
    {
        if (maxStamina <= 0) return 0f; // Prevent division by zero
        return currentStamina / maxStamina;
    }

    /// <summary>
    /// Internal method to trigger all relevant stamina change events.
    /// This ensures all listeners are consistently updated.
    /// </summary>
    private void NotifyStaminaChange()
    {
        OnStaminaChanged?.Invoke(currentStamina);
        OnStaminaPercentageChanged?.Invoke(GetStaminaPercentage());
    }

    // --- Example Usage (How to integrate this into other scripts) ---
    /*
    // To use the StaminaSystem:
    // 1. Attach this StaminaSystem script to your Player GameObject (or any GameObject that needs stamina).
    // 2. Configure 'Max Stamina', 'Regeneration Rate', and 'Regeneration Delay' in the Unity Inspector.

    // Below is an example of a simple Player Controller script that utilizes the StaminaSystem.
    // Create a new C# script called "PlayerController" and attach it to the same GameObject as StaminaSystem.

    using UnityEngine;

    public class PlayerController : MonoBehaviour
    {
        private StaminaSystem staminaSystem; // Reference to the StaminaSystem component

        [Header("Action Costs")]
        [Tooltip("Stamina consumed per second while sprinting.")]
        [SerializeField] private float sprintStaminaCostPerSecond = 10f;
        [Tooltip("Stamina consumed for a single jump.")]
        [SerializeField] private float jumpStaminaCost = 20f;
        [Tooltip("Stamina consumed for a single dash.")]
        [SerializeField] private float dashStaminaCost = 30f;

        private bool isSprinting = false; // Internal state for sprinting

        void Awake()
        {
            // Get the StaminaSystem component from the same GameObject.
            staminaSystem = GetComponent<StaminaSystem>();
            if (staminaSystem == null)
            {
                Debug.LogError("StaminaSystem component not found on this GameObject. PlayerController requires it.", this);
                enabled = false; // Disable this script if StaminaSystem is missing
                return;
            }

            // --- Subscribing to Stamina Events ---
            // This demonstrates how other scripts can react to stamina changes.
            // For UI elements (like a Slider), you can often hook them up directly
            // in the Inspector via the UnityEvent fields of the StaminaSystem.

            staminaSystem.OnStaminaChanged.AddListener(OnStaminaUpdated);
            staminaSystem.OnStaminaPercentageChanged.AddListener(OnStaminaPercentageUpdated);
            staminaSystem.OnMaxStaminaChanged.AddListener(OnMaxStaminaUpdated);

            Debug.Log($"Player stamina initialized: {staminaSystem.CurrentStamina}/{staminaSystem.MaxStamina}", this);
        }

        void OnDestroy()
        {
            // It's crucial to unsubscribe from events when the object is destroyed
            // to prevent memory leaks and dangling references.
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChanged.RemoveListener(OnStaminaUpdated);
                staminaSystem.OnStaminaPercentageChanged.RemoveListener(OnStaminaPercentageUpdated);
                staminaSystem.OnMaxStaminaChanged.RemoveListener(OnMaxStaminaUpdated);
            }
        }

        void Update()
        {
            HandleInput();
            HandleSprinting(); // Continuous actions like sprinting are handled in Update
        }

        /// <summary>Handles player input for various actions.</summary>
        void HandleInput()
        {
            // --- Example: Jumping ---
            // Check for Jump input (e.g., Spacebar)
            if (Input.GetButtonDown("Jump"))
            {
                TryJump();
            }

            // --- Example: Dashing ---
            // Check for Dash input (e.g., 'E' key)
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryDash();
            }

            // --- Example: Sprinting (Toggle/Hold) ---
            // Start sprinting on LeftShift down
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isSprinting = true;
                Debug.Log("Attempting to sprint...");
            }
            // Stop sprinting on LeftShift up
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isSprinting = false;
                Debug.Log("Stopped sprinting.");
            }

            // --- Example: Developer Cheat / Item Usage ---
            // Press 'R' to instantly refill stamina
            if (Input.GetKeyDown(KeyCode.R))
            {
                staminaSystem.ResetStamina();
                Debug.Log("Stamina fully reset by cheat key!");
            }

            // Press 'T' to gain 20 stamina (e.g., from a stamina potion)
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (staminaSystem.GainStamina(20f))
                {
                    Debug.Log("Gained 20 stamina from potion!");
                }
                else
                {
                    Debug.Log("Could not gain stamina (already full or invalid amount).");
                }
            }

            // Press 'F' to temporarily increase max stamina (e.g., a buff)
            if (Input.GetKeyDown(KeyCode.F))
            {
                staminaSystem.SetMaxStamina(150f);
                Debug.Log("Max stamina increased to 150!");
            }
            // Press 'G' to revert max stamina
            if (Input.GetKeyDown(KeyCode.G))
            {
                staminaSystem.SetMaxStamina(100f); // Assuming initial max was 100
                Debug.Log("Max stamina reverted to 100!");
            }
        }

        /// <summary>Handles continuous stamina consumption for sprinting.</summary>
        void HandleSprinting()
        {
            if (isSprinting)
            {
                // Calculate stamina cost for this frame
                float currentSprintCost = sprintStaminaCostPerSecond * Time.deltaTime;

                // Check if enough stamina is available before consuming
                if (staminaSystem.CanPerformAction(currentSprintCost))
                {
                    staminaSystem.ConsumeStamina(currentSprintCost);
                    // Add actual player sprint movement logic here
                    // For demo, just log:
                    // Debug.Log($"Sprinting! Current stamina: {staminaSystem.CurrentStamina:F1}");
                }
                else
                {
                    // Not enough stamina to sprint, automatically stop sprinting
                    isSprinting = false;
                    Debug.Log("Ran out of stamina to sprint!");
                }
            }
        }

        /// <summary>Attempts to perform a jump action.</summary>
        void TryJump()
        {
            if (staminaSystem.CanPerformAction(jumpStaminaCost))
            {
                staminaSystem.ConsumeStamina(jumpStaminaCost);
                Debug.Log($"Jumped! Remaining stamina: {staminaSystem.CurrentStamina:F1}");
                // Add actual jump mechanics (e.g., rigidbody.AddForce) here
            }
            else
            {
                Debug.Log("Not enough stamina to jump!");
            }
        }

        /// <summary>Attempts to perform a dash action.</summary>
        void TryDash()
        {
            if (staminaSystem.CanPerformAction(dashStaminaCost))
            {
                staminaSystem.ConsumeStamina(dashStaminaCost);
                Debug.Log($"Dashed! Remaining stamina: {staminaSystem.CurrentStamina:F1}");
                // Add actual dash mechanics (e.g., temporary speed boost, animation trigger) here
            }
            else
            {
                Debug.Log("Not enough stamina to dash!");
            }
        }

        // --- Event Handlers for UI or Debugging ---
        // These methods are called automatically by the StaminaSystem when its state changes.

        void OnStaminaUpdated(float newStamina)
        {
            // Example: Update a UI text element displaying current stamina
            // Debug.Log($"Stamina changed to: {newStamina:F1}"); // F1 formats float to 1 decimal place
        }

        void OnStaminaPercentageUpdated(float percentage)
        {
            // Example: Update a UI Slider's value or an Image's fillAmount
            Debug.Log($"Stamina percentage: {percentage:P1}"); // P1 formats as percentage with 1 decimal place
        }

        void OnMaxStaminaUpdated(float newMaxStamina)
        {
            Debug.Log($"Max stamina changed to: {newMaxStamina:F1}");
            // If you have UI that shows "X / Y" stamina, you'd update 'Y' here.
        }

        // --- How to hook up a Unity UI Slider to the stamina percentage (No Code Needed for UI!) ---
        // 1. In your Unity scene, go to GameObject -> UI -> Slider.
        // 2. Select your Player GameObject (the one with StaminaSystem and PlayerController).
        // 3. In the Inspector for the StaminaSystem component, find the 'On Stamina Percentage Changed' event.
        // 4. Click the '+' button to add a new listener.
        // 5. Drag your Slider GameObject from the Hierarchy into the 'None (Object)' slot of the new listener.
        // 6. From the 'No Function' dropdown, select 'Slider -> value (float)'.
        // Now, your UI slider will automatically update its value (from 0 to 1) whenever the stamina percentage changes!
    }
    */
}
```