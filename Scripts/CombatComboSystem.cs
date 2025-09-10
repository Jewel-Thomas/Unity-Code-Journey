// Unity Design Pattern Example: CombatComboSystem
// This script demonstrates the CombatComboSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example provides a complete and practical implementation of a **Combat Combo System** design pattern. It allows you to define various combat move sequences (combos) in the Unity Editor and have the system detect and trigger specific actions when those sequences are performed within a defined time window.

This pattern is highly educational as it demonstrates:
*   **Data-driven design:** Combos are defined as data objects (`Combo` class).
*   **State management:** Tracking the `_currentInputSequence` and `_lastInputTime`.
*   **Event-driven programming:** Using `UnityEvent` for flexible callback actions.
*   **Timer management:** Using `Coroutine` for precise time windows.
*   **Input processing:** Handling sequences of player actions.
*   **Unity best practices:** Serialization, `MonoBehaviour`, `Update` for input, `Coroutine` for time-dependent logic, `Tooltip` attributes.

```csharp
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a distinct action that can be part of a combo sequence.
/// Extend this enum with all possible combat actions in your game.
/// </summary>
public enum ComboAction
{
    None,       // Default or unassigned
    LightAttack,
    HeavyAttack,
    Dodge,
    Special1,
    Special2,
    Block
    // Add more actions as needed, e.g., SkillA, SkillB, UpArrow, DownArrow
}

/// <summary>
/// Defines a single combat combo.
/// This class is [Serializable] so it can be configured directly in the Unity Inspector.
/// </summary>
[Serializable]
public class Combo
{
    [Tooltip("A unique name for this combo (e.g., 'Uppercut', 'Spin Attack').")]
    public string comboName;

    [Tooltip("The sequence of actions required to perform this combo.")]
    public ComboAction[] actions;

    [Tooltip("The maximum time allowed between *each* input for this specific combo to remain active. " +
             "If 0, the 'Default Max Time Between Inputs' from the CombatComboSystem will be used.")]
    public float maxTimeBetweenInputs = 0f; // If 0, use system's default

    [Tooltip("UnityEvent invoked when this specific combo is successfully completed.")]
    public UnityEvent OnComboSuccess;

    /// <summary>
    /// Checks if a given input sequence exactly matches this combo's action sequence.
    /// </summary>
    public bool Matches(List<ComboAction> inputSequence)
    {
        if (inputSequence.Count != actions.Length)
            return false;

        for (int i = 0; i < inputSequence.Count; i++)
        {
            if (inputSequence[i] != actions[i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if a given input sequence is a prefix of this combo's action sequence.
    /// This is used to determine if the combo is still potentially achievable.
    /// </summary>
    public bool IsPrefixOf(List<ComboAction> inputSequence)
    {
        if (inputSequence.Count > actions.Length)
            return false;

        for (int i = 0; i < inputSequence.Count; i++)
        {
            if (inputSequence[i] != actions[i])
                return false;
        }
        return true;
    }
}

/// <summary>
/// The core CombatComboSystem MonoBehaviour.
/// Manages player input sequences, detects completed combos, and triggers associated events.
/// Attach this script to a GameObject in your scene (e.g., your Player object or a dedicated GameManager object).
/// </summary>
public class CombatComboSystem : MonoBehaviour
{
    [Header("Combo System Settings")]
    [Tooltip("The default maximum time allowed between *any* two consecutive inputs " +
             "for them to be considered part of the same potential combo sequence. " +
             "Individual combos can override this with their own 'Max Time Between Inputs'.")]
    [SerializeField] private float defaultMaxTimeBetweenInputs = 0.5f;

    [Tooltip("A list of all possible combos that the system should track.")]
    [SerializeField] private List<Combo> combos = new List<Combo>();

    [Header("System Events")]
    [Tooltip("Invoked when any combo is successfully completed, passing the combo's name.")]
    public UnityEvent<string> OnComboCompleted = new UnityEvent<string>();

    [Tooltip("Invoked when the current combo sequence times out or becomes invalid.")]
    public UnityEvent OnComboFailed = new UnityEvent();

    // Internal state variables for tracking the current combo attempt
    private List<ComboAction> _currentInputSequence = new List<ComboAction>();
    private float _lastInputTime;
    private Coroutine _comboTimeoutCoroutine;

    // A dictionary for quick lookup of combos by their first action (optional optimization)
    // private Dictionary<ComboAction, List<Combo>> _combosByFirstAction; 

    private void Awake()
    {
        // Optional: Pre-process combos for faster lookup if you have a very large number of combos.
        // For this example, direct iteration through the 'combos' list is sufficient and clearer.
        // _combosByFirstAction = new Dictionary<ComboAction, List<Combo>>();
        // foreach (var combo in combos)
        // {
        //     if (combo.actions.Length > 0)
        //     {
        //         if (!_combosByFirstAction.ContainsKey(combo.actions[0]))
        //         {
        //             _combosByFirstAction[combo.actions[0]] = new List<Combo>();
        //         }
        //         _combosByFirstAction[combo.actions[0]].Add(combo);
        //     }
        // }
    }

    /// <summary>
    /// This method is the primary entry point for feeding player actions into the combo system.
    /// Call this whenever a relevant combat action occurs (e.g., a button press for an attack).
    /// </summary>
    /// <param name="action">The ComboAction performed by the player.</param>
    public void PerformAction(ComboAction action)
    {
        // If this is the first action in a new potential sequence, start the timeout coroutine.
        if (_currentInputSequence.Count == 0)
        {
            _comboTimeoutCoroutine = StartCoroutine(ComboTimeoutCoroutine());
        }
        // If there's an existing sequence, stop its timer and restart to extend the window for the new action.
        else if (_comboTimeoutCoroutine != null)
        {
            StopCoroutine(_comboTimeoutCoroutine);
            _comboTimeoutCoroutine = StartCoroutine(ComboTimeoutCoroutine());
        }

        _currentInputSequence.Add(action);
        _lastInputTime = Time.time;

        Debug.Log($"CombatComboSystem: Action '{action}' performed. Current sequence: {string.Join(", ", _currentInputSequence)}");

        TryMatchCombos();
    }

    /// <summary>
    /// Attempts to match the current input sequence against defined combos.
    /// Checks for both completed combos and if the sequence is still a valid prefix for any combo.
    /// </summary>
    private void TryMatchCombos()
    {
        // Step 1: Check for exact combo matches (a combo has been completed!)
        // Prioritizes the first combo in the list if multiple match the same sequence.
        // You might want to sort your 'combos' list in the editor (e.g., by length)
        // if prioritization is important for overlapping sequences (e.g., A,B vs A,B,C).
        foreach (var combo in combos)
        {
            if (combo.Matches(_currentInputSequence))
            {
                Debug.Log($"CombatComboSystem: Combo '{combo.comboName}' completed!");
                combo.OnComboSuccess?.Invoke();
                OnComboCompleted?.Invoke(combo.comboName);
                ResetComboState();
                return; // Only one combo can succeed at a time for a given exact match
            }
        }

        // Step 2: Check if the current input sequence is still a *prefix* of any defined combo.
        // If it's not a prefix of ANY combo, then the sequence is invalid, and we should reset.
        bool isAnyComboStillPossible = false;
        foreach (var combo in combos)
        {
            if (combo.IsPrefixOf(_currentInputSequence))
            {
                isAnyComboStillPossible = true;
                break; // Found at least one potential combo, no need to check further
            }
        }

        if (!isAnyComboStillPossible && _currentInputSequence.Count > 0)
        {
            // The current input sequence does not match any complete combo,
            // AND it's not a prefix of any other combo. This means the sequence is invalid.
            Debug.Log("CombatComboSystem: Current input sequence is no longer a prefix of any combo. Resetting.");
            OnComboFailed?.Invoke();
            ResetComboState();
            // Optional: You could re-process the 'last action' here as the start of a *new* combo
            // if you want to allow players to recover from a mistyped intermediate action.
            // Example: PerformAction(_currentInputSequence.Last()); // If you decide to do this, clear _currentInputSequence *before* this line.
        }
        // If `isAnyComboStillPossible` is true, the system simply waits for the next input or timeout.
    }

    /// <summary>
    /// Resets the combo system's state, clearing the current input sequence and stopping any active timers.
    /// </summary>
    private void ResetComboState()
    {
        if (_comboTimeoutCoroutine != null)
        {
            StopCoroutine(_comboTimeoutCoroutine);
            _comboTimeoutCoroutine = null;
        }
        _currentInputSequence.Clear();
        _lastInputTime = 0f; // Reset last input time as well
    }

    /// <summary>
    /// Coroutine that monitors the time between inputs. If the time exceeds the allowed window,
    /// the current combo sequence is considered failed and is reset.
    /// The allowed time window dynamically adjusts based on the longest remaining possible combo step.
    /// </summary>
    private IEnumerator ComboTimeoutCoroutine()
    {
        // Wait one frame to ensure _lastInputTime is set correctly after the current action.
        yield return null; 

        while (_currentInputSequence.Count > 0)
        {
            float currentMaxAllowedTime = defaultMaxTimeBetweenInputs;
            bool anyPotentialCombosFound = false;

            // Find the maximum allowed time for the *next* input based on all currently possible combos.
            // This ensures we don't prematurely time out a longer combo just because a shorter one has a tighter window.
            foreach (var combo in combos)
            {
                // If the current sequence is a prefix of this combo AND the combo still expects more actions
                if (_currentInputSequence.Count < combo.actions.Length && combo.IsPrefixOf(_currentInputSequence))
                {
                    anyPotentialCombosFound = true;
                    // Use the combo's specific time window if set, otherwise use the system default.
                    float comboWindow = combo.maxTimeBetweenInputs > 0 ? combo.maxTimeBetweenInputs : defaultMaxTimeBetweenInputs;
                    currentMaxAllowedTime = Mathf.Max(currentMaxAllowedTime, comboWindow);
                }
            }

            // If no potential combos were found (meaning _currentInputSequence is invalid),
            // or if it's an empty sequence, use the default max time.
            if (!anyPotentialCombosFound && _currentInputSequence.Count > 0)
            {
                // This state should ideally be caught by TryMatchCombos() if the sequence isn't a prefix of anything.
                // However, this provides a fallback timeout for any active (but maybe unmatchable) sequence.
                currentMaxAllowedTime = defaultMaxTimeBetweenInputs;
            }
            else if (_currentInputSequence.Count == 0) // Should not happen often due to how coroutine starts/stops
            {
                yield break; // If sequence is empty, nothing to time out.
            }

            // Check if the time since the last input has exceeded the allowed window.
            if (Time.time - _lastInputTime > currentMaxAllowedTime)
            {
                Debug.Log($"CombatComboSystem: Combo sequence timed out after {currentMaxAllowedTime}s. Last input was at {_lastInputTime}. Current time is {Time.time}.");
                OnComboFailed?.Invoke();
                ResetComboState();
                yield break; // Exit the coroutine
            }

            yield return null; // Wait for the next frame
        }

        _comboTimeoutCoroutine = null; // Ensure the reference is null when coroutine naturally ends
    }

    /// <summary>
    /// Example of how to integrate with Unity's input system (Legacy Input Manager).
    /// You would typically replace this with your game's specific input handling (e.g., using the new Input System,
    /// or from a PlayerController script calling PerformAction()).
    /// </summary>
    private void Update()
    {
        // Example: Map keyboard keys or mouse clicks to ComboActions
        if (Input.GetButtonDown("Fire1")) // Default Unity Input for Left Mouse Click or Left Ctrl
        {
            PerformAction(ComboAction.LightAttack);
        }
        if (Input.GetButtonDown("Fire2")) // Default Unity Input for Right Mouse Click or Left Alt
        {
            PerformAction(ComboAction.HeavyAttack);
        }
        if (Input.GetButtonDown("Jump")) // Default Unity Input for Spacebar
        {
            PerformAction(ComboAction.Dodge);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PerformAction(ComboAction.Special1);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformAction(ComboAction.Special2);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            PerformAction(ComboAction.Block);
        }
    }
}


/*
/// --- EXAMPLE USAGE IN YOUR UNITY PROJECT --- ///

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., "CombatSystemManager").
2.  **Attach CombatComboSystem:** Drag and drop this `CombatComboSystem.cs` script onto the "CombatSystemManager" GameObject.

3.  **Configure Combos in the Inspector:**
    *   Select "CombatSystemManager" in the Hierarchy.
    *   In the Inspector, adjust "Default Max Time Between Inputs" (e.g., 0.7 seconds).
    *   Expand the "Combos" list.
    *   Add new elements to define your combos:

    **Example Combo 1: "Smash Attack"**
    *   **Combo Name:** Smash Attack
    *   **Actions:** (Size: 2)
        *   Element 0: `LightAttack`
        *   Element 1: `HeavyAttack`
    *   **Max Time Between Inputs:** 0.4 (This combo will have a tighter window than the default)
    *   **On Combo Success:**
        *   Click the '+' button to add an event listener.
        *   Drag your Player GameObject (or any GameObject with a relevant script) into the `None (Object)` slot.
        *   From the `No Function` dropdown, select your Player's script (e.g., `PlayerController`) and then a public method like `PlaySmashAnimation()` or `DealHeavyDamage()`.

    **Example Combo 2: "Quick Dodge"**
    *   **Combo Name:** Quick Dodge
    *   **Actions:** (Size: 2)
        *   Element 0: `Dodge`
        *   Element 1: `Dodge`
    *   **Max Time Between Inputs:** 0.3
    *   **On Combo Success:**
        *   Add another event.
        *   Maybe drag a particle system GameObject and select `ParticleSystem.Play()`, or a sound manager and `SoundManager.PlaySFX("DodgeSound")`.

    **Example Combo 3: "Special Finisher"**
    *   **Combo Name:** Special Finisher
    *   **Actions:** (Size: 3)
        *   Element 0: `LightAttack`
        *   Element 1: `LightAttack`
        *   Element 2: `Special1`
    *   **Max Time Between Inputs:** 0.6 (Uses a slightly more lenient window)
    *   **On Combo Success:**
        *   Set up an event for a powerful final move.

4.  **Listen to Global Events (Optional):**
    *   In the Inspector, you can also configure `OnComboCompleted` and `OnComboFailed` events.
    *   `OnComboCompleted` can be used to update UI (e.g., "Combo Executed: [Combo Name]!"), or trigger general combat effects.
    *   `OnComboFailed` can be used to play a sound effect indicating a mistimed input.

5.  **Implement Player Input (or AI):**
    *   The `Update()` method in `CombatComboSystem.cs` provides a basic example using `Input.GetButtonDown`.
    *   In a real game, your `PlayerController` script (or an AI behavior script) would call `FindObjectOfType<CombatComboSystem>().PerformAction(ComboAction.LightAttack);` whenever the player presses the appropriate button or an AI decides to execute an action.

    **Example PlayerController.cs snippet:**
    ```csharp
    // Assuming you have a reference to the CombatComboSystem
    private CombatComboSystem _comboSystem; 

    void Start() {
        _comboSystem = FindObjectOfType<CombatComboSystem>(); // Or get it via dependency injection
        if (_comboSystem == null) {
            Debug.LogError("CombatComboSystem not found in scene!");
            enabled = false;
        }

        // Optional: Subscribe to global combo events from other scripts
        _comboSystem.OnComboCompleted.AddListener(OnAnyComboCompleted);
        _comboSystem.OnComboFailed.AddListener(OnAnyComboFailed);
    }

    void OnDestroy() {
        if (_comboSystem != null) {
            _comboSystem.OnComboCompleted.RemoveListener(OnAnyComboCompleted);
            _comboSystem.OnComboFailed.RemoveListener(OnAnyComboFailed);
        }
    }

    void Update() {
        // Player input for attacks
        if (Input.GetMouseButtonDown(0)) { // Left click
            _comboSystem.PerformAction(ComboAction.LightAttack);
        }
        if (Input.GetMouseButtonDown(1)) { // Right click
            _comboSystem.PerformAction(ComboAction.HeavyAttack);
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            _comboSystem.PerformAction(ComboAction.Dodge);
        }
        // ... more input
    }

    void OnAnyComboCompleted(string comboName) {
        Debug.Log($"PlayerController: Heard that '{comboName}' was completed!");
        // Play general combo success effects, update score, etc.
    }

    void OnAnyComboFailed() {
        Debug.Log("PlayerController: A combo attempt failed.");
        // Play a "miss" sound or reset player animation state
    }
    ```

This setup provides a highly flexible and extensible system for managing combat combos in your Unity game.
*/
```