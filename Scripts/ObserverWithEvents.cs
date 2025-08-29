// Unity Design Pattern Example: ObserverWithEvents
// This script demonstrates the ObserverWithEvents pattern in Unity
// Generated automatically - ready to use in your Unity project

The Observer design pattern is a behavioral pattern where an object (the **Subject** or **Publisher**) maintains a list of its dependents (the **Observers** or **Subscribers**) and notifies them of any state changes, usually by calling one of their methods. In C#, this is elegantly implemented using **events** and **delegates** (often `System.Action` or `System.Func`).

This example demonstrates how to implement the ObserverWithEvents pattern in Unity using a `PlayerHealth` component as the Subject and `UIHealthBar`, `GameSoundManager`, and `AchievementManager` as Observers.

---

### ObserverWithEventsExample.cs

```csharp
using UnityEngine;
using UnityEngine.UI; // For UI elements like Slider and Text
using System;       // For Action delegate (a common alternative to custom delegates)

// --- OVERVIEW OF THE OBSERVER WITH EVENTS PATTERN ---
//
// 1.  The Subject (Publisher):
//     - Declares a public event (e.g., 'OnHealthChanged', 'OnPlayerDied').
//     - The event is based on a delegate type (like `System.Action<T1, T2>`).
//     - When its state changes, it 'invokes' or 'raises' the event.
//     - It does NOT know who is listening or how they will react.
//
// 2.  The Observer (Subscriber):
//     - Implements a method that matches the signature of the Subject's event delegate.
//     - Subscribes to the Subject's event using the `+=` operator, linking its method
//       to the event.
//     - Unsubscribes from the event using the `-=` operator when it no longer needs
//       to listen or when it is destroyed (CRUCIAL for preventing memory leaks!).
//     - When the Subject invokes the event, all subscribed Observer methods are called.
//
// Benefits:
// - Loose Coupling: Subject and Observers are independent.
// - Extensibility: Easily add new Observers without modifying the Subject.
// - Modularity: Each component has a single responsibility.
//
// Use Cases in Unity:
// - Player health changes affecting UI, sound, and VFX.
// - Game state changes (e.g., game over, level loaded) notifying various managers.
// - Item pickups affecting inventory, score, and achievement systems.
// - Input events notifying player controllers or UI elements.

// ====================================================================================
// --- 1. The Subject (Publisher): PlayerHealth ---
// This class is the entity whose state changes, and it notifies other objects of these changes.
// In our example, it's the Player's health.
// ====================================================================================
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100; // Player's maximum health
    [SerializeField] private int currentHealth;     // Player's current health

    // --- Events for Observers ---
    // Instead of custom delegates (like `delegate void OnHealthChanged(int currentHealth, int maxHealth);`),
    // we commonly use `System.Action` delegates, which are very convenient for events.
    // Action<T1, T2> defines a delegate that takes two arguments and returns void.

    // Event raised when the player's health changes.
    // Any method subscribed to this event will be called when it's raised.
    public event Action<int, int> OnHealthChanged; // Parameters: (currentHealth, maxHealth)

    // Event raised when the player's health drops to 0 or below.
    public event Action OnPlayerDied; // No parameters, just a notification

    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        currentHealth = maxHealth; // Initialize health at the start
        // Immediately notify any initial observers about the current health state.
        // The '?' (null-conditional operator) ensures we don't try to invoke if no one is subscribed.
        // This is good practice to ensure initial UI setup, for example.
        OnHealthChanged?.Invoke(currentHealth, maxHealth); 
    }

    // --- Public Methods to Modify Health (and trigger events) ---

    /// <summary>
    /// Reduces the player's current health by the specified amount.
    /// This method is the entry point for other systems to interact with player health.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(int amount)
    {
        if (amount < 0) return;             // Damage should not be negative
        if (currentHealth <= 0) return;     // Cannot take damage if already dead

        int oldHealth = currentHealth;
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // Health cannot go below 0

        Debug.Log($"Player took {amount} damage. Current Health: {currentHealth}/{maxHealth}");

        // --- IMPORTANT: Raise the events ---
        // Only notify if health actually changed or if it was the killing blow
        if (currentHealth != oldHealth)
        {
            // Notify all subscribed observers that the health has changed.
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        // Check for death condition *after* health potentially changed
        if (currentHealth <= 0 && oldHealth > 0) // Ensure we only trigger OnPlayerDied once
        {
            Debug.Log("Player has died!");
            // Notify all subscribed observers that the player has died.
            OnPlayerDied?.Invoke();
        }
    }

    /// <summary>
    /// Increases the player's current health by the specified amount.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(int amount)
    {
        if (amount < 0) return;             // Healing should not be negative
        if (currentHealth >= maxHealth) return; // Cannot heal if already full health
        if (currentHealth <= 0) // Can we heal from death? Game specific. For this example, let's allow it.
        {
            Debug.LogWarning("Healing a dead player. Consider game design for this scenario.");
        }

        int oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Health cannot exceed maxHealth

        Debug.Log($"Player healed {amount} health. Current Health: {currentHealth}/{maxHealth}");

        // --- IMPORTANT: Raise the event ---
        // Only notify if health actually changed
        if (currentHealth != oldHealth)
        {
            // Notify all subscribed observers that the health has changed.
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    // A simple method to reset health for demonstration purposes
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        Debug.Log("Player health reset.");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}


// ====================================================================================
// --- 2. Observer (Subscriber) Examples ---
// These classes react to changes in the PlayerHealth's state by subscribing to its events.
// Each observer focuses on a specific task.
// ====================================================================================

/// <summary>
/// An Observer responsible for updating a UI Health Bar and Text.
/// This component doesn't know *how* health changes, only that it *did* change.
/// </summary>
public class UIHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider; // Reference to a Unity UI Slider
    [SerializeField] private Text healthText;     // Reference to a Unity UI Text (or TextMeshProUGUI)

    [Header("Observer Target")]
    [SerializeField] private PlayerHealth playerHealth; // Reference to the PlayerHealth Subject

    // --- Unity Lifecycle Methods for Subscription/Unsubscription ---

    // OnEnable is called when the object becomes enabled and active.
    // This is the ideal place to subscribe to events to ensure we only listen when active.
    private void OnEnable()
    {
        if (playerHealth != null)
        {
            // Subscribe the 'UpdateHealthDisplay' method to the 'OnHealthChanged' event.
            // This means whenever 'playerHealth.OnHealthChanged' is invoked,
            // 'UpdateHealthDisplay' will automatically be called with the same arguments.
            playerHealth.OnHealthChanged += UpdateHealthDisplay;
            Debug.Log("UIHealthBar subscribed to PlayerHealth.OnHealthChanged.");
            // Also call it once to set initial state if OnHealthChanged wasn't called in Awake
            // or if this UI element is enabled later. PlayerHealth's Awake already calls it.
        }
        else
        {
            Debug.LogError("PlayerHealth reference not set in UIHealthBar.", this);
        }
    }

    // OnDisable is called when the object becomes disabled or inactive.
    // This is CRUCIAL for unsubscribing from events to prevent memory leaks and unexpected behavior.
    // If you don't unsubscribe, the UIHealthBar object might still exist in memory and try
    // to update UI elements even if it's inactive/destroyed, leading to NullReferenceExceptions.
    private void OnDisable()
    {
        if (playerHealth != null)
        {
            // Unsubscribe to prevent memory leaks and ensure clean shutdown.
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
            Debug.Log("UIHealthBar unsubscribed from PlayerHealth.OnHealthChanged.");
        }
    }

    // --- Event Handler Methods ---

    /// <summary>
    /// This method is called by the PlayerHealth.OnHealthChanged event.
    /// It updates the UI elements to reflect the current health.
    /// </summary>
    /// <param name="currentHealth">The player's current health.</param>
    /// <param name="maxHealth">The player's maximum health.</param>
    private void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"HP: {currentHealth}/{maxHealth}";
        }
        Debug.Log($"UIHealthBar updated: {currentHealth}/{maxHealth}");
    }
}

/// <summary>
/// An Observer responsible for playing sounds based on player health events.
/// This component doesn't know *who* or *what* caused the health change/death.
/// </summary>
public class GameSoundManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // AudioSource component to play sounds
    [SerializeField] private AudioClip hitSound;      // Sound played when player takes damage (general health change)
    [SerializeField] private AudioClip deathSound;    // Sound played when player dies

    [Header("Observer Target")]
    [SerializeField] private PlayerHealth playerHealth; // Reference to the PlayerHealth Subject

    private int _lastKnownHealth; // To detect if health actually decreased for a "hit" sound

    // --- Unity Lifecycle Methods for Subscription/Unsubscription ---
    private void OnEnable()
    {
        if (playerHealth != null)
        {
            // Store initial health for hit sound logic
            _lastKnownHealth = playerHealth.GetComponent<PlayerHealth>().currentHealth; 

            // Subscribe to health changes (e.g., to play a hit sound)
            playerHealth.OnHealthChanged += PlayHealthChangeSound;
            // Subscribe to player death
            playerHealth.OnPlayerDied += PlayDeathSound;
            Debug.Log("GameSoundManager subscribed to PlayerHealth events.");
        }
        else
        {
            Debug.LogError("PlayerHealth reference not set in GameSoundManager.", this);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("No AudioSource found on GameSoundManager, adding one.", this);
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            // Unsubscribe from both events
            playerHealth.OnHealthChanged -= PlayHealthChangeSound;
            playerHealth.OnPlayerDied -= PlayDeathSound;
            Debug.Log("GameSoundManager unsubscribed from PlayerHealth events.");
        }
    }

    // --- Event Handler Methods ---

    /// <summary>
    /// This method is called by PlayerHealth.OnHealthChanged.
    /// Plays a hit sound if health decreased.
    /// </summary>
    /// <param name="currentHealth">Current health.</param>
    /// <param name="maxHealth">Max health.</param>
    private void PlayHealthChangeSound(int currentHealth, int maxHealth)
    {
        // Only play a "hit" sound if health actually decreased.
        // A more advanced system might have a separate 'OnPlayerDamaged' event.
        if (currentHealth < _lastKnownHealth && audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
            Debug.Log("GameSoundManager played hit sound.");
        }
        // Update last known health for the next comparison
        _lastKnownHealth = currentHealth;
    }

    /// <summary>
    /// This method is called by PlayerHealth.OnPlayerDied.
    /// Plays the death sound.
    /// </summary>
    private void PlayDeathSound()
    {
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
            Debug.Log("GameSoundManager played death sound.");
        }
    }
}

/// <summary>
/// An Observer responsible for managing achievements based on player events.
/// This component doesn't care about health values, only about the death event.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    [Header("Observer Target")]
    [SerializeField] private PlayerHealth playerHealth; // Reference to the PlayerHealth Subject

    private bool hasDiedAchievement = false; // Internal state for achievement

    // --- Unity Lifecycle Methods for Subscription/Unsubscription ---
    private void OnEnable()
    {
        if (playerHealth != null)
        {
            // Subscribe to player death event
            playerHealth.OnPlayerDied += UnlockDeathAchievement;
            Debug.Log("AchievementManager subscribed to PlayerHealth.OnPlayerDied.");
        }
        else
        {
            Debug.LogError("PlayerHealth reference not set in AchievementManager.", this);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            // Unsubscribe
            playerHealth.OnPlayerDied -= UnlockDeathAchievement;
            Debug.Log("AchievementManager unsubscribed from PlayerHealth.OnPlayerDied.");
        }
    }

    // --- Event Handler Methods ---

    /// <summary>
    /// This method is called by PlayerHealth.OnPlayerDied.
    /// Unlocks an achievement if the player dies for the first time.
    /// </summary>
    private void UnlockDeathAchievement()
    {
        if (!hasDiedAchievement) // Check if achievement is already unlocked
        {
            hasDiedAchievement = true;
            Debug.Log("<color=green>Achievement Unlocked: 'First Death'!</color>");
            // In a real game, you would interact with a platform-specific API here
            // e.g., SteamAPI.UnlockAchievement("FIRST_DEATH");
        }
    }
}


// ====================================================================================
// --- 3. Demonstrator / Client Code ---
// A simple controller to demonstrate the PlayerHealth component by simulating damage and healing.
// This acts as another 'client' that interacts with the Subject (PlayerHealth).
// ====================================================================================
public class PlayerHealthDemonstrator : MonoBehaviour
{
    [Header("Player Health Subject")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Damage/Heal Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private int healAmount = 15;

    void Update()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth reference not set in PlayerHealthDemonstrator!");
            return;
        }

        // Simulate taking damage on 'D' key press
        if (Input.GetKeyDown(KeyCode.D))
        {
            playerHealth.TakeDamage(damageAmount);
        }

        // Simulate healing on 'H' key press
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerHealth.Heal(healAmount);
        }

        // Simulate resetting health on 'R' key press
        if (Input.GetKeyDown(KeyCode.R))
        {
            playerHealth.ResetHealth();
        }
    }
}

/*
 * ====================================================================================
 * --- HOW TO SET UP THIS EXAMPLE IN UNITY ---
 * ====================================================================================
 *
 * 1.  Create an empty C# script named "ObserverWithEventsExample.cs" and copy all the code above into it.
 *     (Ensure the file name matches the primary class name if you were to split it, but for this example,
 *      it's one file containing multiple classes).
 *
 * 2.  Create an empty GameObject in your scene:
 *     - Rename it to "Player".
 *     - Add the 'PlayerHealth' component to it.
 *
 * 3.  Create a UI Canvas:
 *     - Right-click in Hierarchy -> UI -> Canvas.
 *     - Inside the Canvas, Right-click -> UI -> Slider. Rename it "HealthSlider".
 *       - Adjust its size and position (e.g., anchor to top-left, set width/height).
 *       - You might want to remove the "Handle Slide Area" child if you just want a simple bar.
 *     - Inside the Canvas, Right-click -> UI -> Text (Legacy) or TextMeshPro - Text (requires TMP import).
 *       - Rename it "HealthText".
 *       - Adjust its size and position, place it near the slider.
 *
 * 4.  Create an empty GameObject for the UI Observer:
 *     - Rename it "HealthUI_Observer".
 *     - Add the 'UIHealthBar' component to it.
 *     - In the Inspector:
 *       - Drag the "Player" GameObject (which has PlayerHealth) into the 'Player Health' slot.
 *       - Drag the "HealthSlider" UI Slider into the 'Health Slider' slot.
 *       - Drag the "HealthText" UI Text into the 'Health Text' slot.
 *
 * 5.  Create an empty GameObject for the Sound Observer:
 *     - Rename it "SoundManager_Observer".
 *     - Add the 'GameSoundManager' component to it.
 *     - Add an 'AudioSource' component to "SoundManager_Observer" (required by GameSoundManager).
 *     - In the Inspector:
 *       - Drag the "Player" GameObject into the 'Player Health' slot.
 *       - Assign some `AudioClip` assets to 'Hit Sound' and 'Death Sound' (e.g., from Unity's standard assets or create simple ones).
 *         (If you don't have sound clips, you can still run it and see the console logs).
 *
 * 6.  Create an empty GameObject for the Achievement Observer:
 *     - Rename it "AchievementManager_Observer".
 *     - Add the 'AchievementManager' component to it.
 *     - In the Inspector:
 *       - Drag the "Player" GameObject into the 'Player Health' slot.
 *
 * 7.  Create an empty GameObject for the demonstration input:
 *     - Rename it "Demonstrator".
 *     - Add the 'PlayerHealthDemonstrator' component to it.
 *     - In the Inspector:
 *       - Drag the "Player" GameObject into the 'Player Health Subject' slot.
 *
 * 8.  Run the scene!
 *     - Press 'D' to deal damage to the player.
 *     - Press 'H' to heal the player.
 *     - Press 'R' to reset player health.
 *     - Observe the Console, the UI Health Bar, and listen for sounds.
 *     - Notice how 'PlayerHealth' doesn't directly know about the UI, sounds, or achievements.
 *       It just broadcasts events, and other systems (observers) pick them up if they care.
 *       This is the power of the Observer pattern with C# events!
 *
 * ====================================================================================
 * --- KEY TAKEAWAYS AND BEST PRACTICES ---
 * ====================================================================================
 *
 * - Use `System.Action` or `System.Func` for events when possible; they are common and reduce boilerplate.
 * - Always use the `event` keyword for public events to restrict access (only `+=` and `-=`, not direct invocation from outside).
 * - Always subscribe in `OnEnable()` and unsubscribe in `OnDisable()` (or `OnDestroy()`) for MonoBehaviours.
 *   This prevents memory leaks and ensures observers only listen when they are active and valid.
 * - The null-conditional operator (`?.Invoke()`) is crucial to safely raise events when no one is subscribed.
 * - This pattern promotes a highly modular and extensible architecture, making it easy to add new features
 *   without modifying existing core systems.
 */
```